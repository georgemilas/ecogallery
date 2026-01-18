using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace GalleryLib.service.fileProcessor;

/// <summary>
/// Use the Parallel version of the FileObserverService to process files faster using multiple threads
/// - this is a backup version that processes files sequentially without parallelism 
/// </summary>
public class FileObserverServiceNotParallel : FilePeriodicScanServiceNotParallel
{
    
    public FileObserverServiceNotParallel(IFileProcessor processor, int intervalMinutes = 2)
        : base(processor, intervalMinutes)
    {
            
    }
    
    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<FileData, CancellationTokenSource> _changeDebounce = new();
    private const int ChangeDebounceMs = 300;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Starting FileObserverService (Not Parallel) on folder: {_processor.RootFolder.FullName}");
        SetupFileSystemWatcher();
        await base.ExecuteAsync(stoppingToken);        
    }

    private void SetupFileSystemWatcher()
    {
        _watcher = new FileSystemWatcher(_processor.RootFolder.FullName)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            Filter = "*.*"
        };
        _watcher.Created += OnWatcherFileCreated;
        _watcher.Changed += OnWatcherFileChanged;
        _watcher.Deleted += OnWatcherFileDeleted;
        _watcher.Renamed += OnWatcherFileRenamed;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;
        Console.WriteLine("FileSystemWatcher enabled for real-time change detection");
    }

    private async void OnWatcherFileCreated(object sender, FileSystemEventArgs e)
    {
        var data = new FileData(e.FullPath, e.FullPath);
        if (!_processor.ShouldProcessFile(data)) return;
        bool success = await InvokeHandlerSafe(() =>  _processor.OnFileCreated(data, true), $"created: {e.FullPath}");
        if (success)
        {
            lock (_setLock)
            {
                _currentSourceFiles.Add(data);
            }
        }
    }

    private async void OnWatcherFileChanged(object sender, FileSystemEventArgs e)
    {
        var data = new FileData(e.FullPath, e.FullPath);
        if (!_processor.ShouldProcessFile(data)) return;
        Console.WriteLine($"FileSystemWatcher: File changed - {e.FullPath}");
        ScheduleDebouncedChange(data);
    }

    private async void OnWatcherFileDeleted(object sender, FileSystemEventArgs e)
    {
        var data = new FileData(e.FullPath, e.FullPath);
        if (!_processor.ShouldProcessFile(data)) return;
        Console.WriteLine($"FileSystemWatcher: File deleted - {e.FullPath}");
        CancelDebounceForPath(data);
        bool deleted = await InvokeHandlerSafe(() => _processor.OnFileDeleted(data, _logIfProcessed), $"deleted: {e.FullPath}");
        if (deleted)
        {
            lock (_setLock)
            {
                _currentSourceFiles.Remove(data);
            }
        }
    }

    private async void OnWatcherFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldData = new FileData(e.OldFullPath, e.OldFullPath);
        var newData = new FileData(e.FullPath, e.FullPath);
        bool oldValid = _processor.ShouldProcessFile(oldData);
        bool newValid = _processor.ShouldProcessFile(newData);
        Console.WriteLine($"FileSystemWatcher: File renamed from {e.OldFullPath} to {e.FullPath}");
        CancelDebounceForPath(oldData);
        CancelDebounceForPath(newData);
        bool renamed = await InvokeHandlerSafe(() => _processor.OnFileRenamed(oldData, newData, newValid), $"renamed: {e.OldFullPath} -> {e.FullPath}");
        if (renamed)
        {
            lock (_setLock)
            {
                if (oldValid) _currentSourceFiles.Remove(oldData);
                if (newValid) _currentSourceFiles.Add(newData);
            }
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine($"FileSystemWatcher error: {e.GetException()?.Message}");
    }

    public override void Dispose()
    {
        foreach (var kvp in _changeDebounce)
        {
            try { kvp.Value.Cancel(); kvp.Value.Dispose(); } catch { }
        }
        _changeDebounce.Clear();
        _watcher?.Dispose();
        base.Dispose();
    }

    private void ScheduleDebouncedChange(FileData path)
    {
        var ctsNew = new CancellationTokenSource();
        var existing = _changeDebounce.AddOrUpdate(path, ctsNew, (k, oldCts) =>
        {
            try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            return ctsNew;
        });

        if (existing == ctsNew)
        {
            _ = DebouncedInvokeChange(path, ctsNew.Token);
        }
    }

    private async Task DebouncedInvokeChange(FileData path, CancellationToken token)
    {
        try
        {
            await Task.Delay(ChangeDebounceMs, token);
            if (token.IsCancellationRequested) return;
            await InvokeHandlerSafe(() => _processor.OnFileChanged(path), $"changed (debounced): {path}");
        }
        catch (TaskCanceledException) { }
        finally
        {
            if (_changeDebounce.TryRemove(path, out var cts))
            {
                try { cts.Dispose(); } catch { }
            }
        }
    }

    private void CancelDebounceForPath(FileData path)
    {
        if (_changeDebounce.TryRemove(path, out var cts))
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
        }
    }
}
