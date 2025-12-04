using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace PicturesLib.service.fileProcessor;

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
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _changeDebounce = new();
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
        if (!ShouldProcessFile(e.FullPath)) return;
        Console.WriteLine($"FileSystemWatcher: File created - {e.FullPath}");
        bool created = await InvokeHandlerSafe(() =>  _processor.OnFileCreated(e.FullPath), $"created: {e.FullPath}");
        if (created)
        {
            lock (_setLock)
            {
                _currentSourceFiles.Add(e.FullPath);
            }
        }
    }

    private async void OnWatcherFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        Console.WriteLine($"FileSystemWatcher: File changed - {e.FullPath}");
        ScheduleDebouncedChange(e.FullPath);
    }

    private async void OnWatcherFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        Console.WriteLine($"FileSystemWatcher: File deleted - {e.FullPath}");
        CancelDebounceForPath(e.FullPath);
        bool deleted = await InvokeHandlerSafe(() => _processor.OnFileDeleted(e.FullPath), $"deleted: {e.FullPath}");
        if (deleted)
        {
            lock (_setLock)
            {
                _currentSourceFiles.Remove(e.FullPath);
            }
        }
    }

    private async void OnWatcherFileRenamed(object sender, RenamedEventArgs e)
    {
        bool oldValid = ShouldProcessFile(e.OldFullPath);
        bool newValid = ShouldProcessFile(e.FullPath);
        Console.WriteLine($"FileSystemWatcher: File renamed from {e.OldFullPath} to {e.FullPath}");
        CancelDebounceForPath(e.OldFullPath);
        CancelDebounceForPath(e.FullPath);
        bool renamed = await InvokeHandlerSafe(() => _processor.OnFileRenamed(e.OldFullPath, e.FullPath, newValid), $"renamed: {e.OldFullPath} -> {e.FullPath}");
        if (renamed)
        {
            lock (_setLock)
            {
                if (oldValid) _currentSourceFiles.Remove(e.OldFullPath);
                if (newValid) _currentSourceFiles.Add(e.FullPath);
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

    private void ScheduleDebouncedChange(string path)
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

    private async Task DebouncedInvokeChange(string path, CancellationToken token)
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

    private void CancelDebounceForPath(string path)
    {
        if (_changeDebounce.TryRemove(path, out var cts))
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
        }
    }
}
