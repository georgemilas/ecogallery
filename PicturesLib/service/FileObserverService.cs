using Microsoft.Extensions.Hosting;

namespace PicturesLib.service;

public class FileObserverService : BackgroundService
{
    public FileObserverService(
        DirectoryInfo rootFolder, 
        Func<string, bool> shouldSkipFile,
        Func<string, Task> onFileCreated,
        Func<string, Task> onFileChanged,
        Func<string, Task> onFileDeleted,
        Func<string, string, Task> onFileRenamed,
        HashSet<string>? extensions = null, 
        int intervalMinutes = 2)
    {
        _rootFolder = rootFolder;
        _shouldSkipFile = shouldSkipFile;
        _onFileCreated = onFileCreated;
        _onFileChanged = onFileChanged;
        _onFileDeleted = onFileDeleted;
        _onFileRenamed = onFileRenamed;
        _extensions = extensions;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }
    
    private readonly DirectoryInfo _rootFolder;
    private readonly Func<string, bool> _shouldSkipFile;
    private readonly Func<string, Task> _onFileCreated;
    private readonly Func<string, Task> _onFileChanged;
    private readonly Func<string, Task> _onFileDeleted;
    private readonly Func<string, string, Task> _onFileRenamed;
    private readonly TimeSpan _interval;
    private readonly HashSet<string>? _extensions;
    private bool _processing = false;
    private FileSystemWatcher? _watcher;
    private HashSet<string> _currentSourceFiles = new();
    private readonly object _setLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Starting FileObserverService on folder: {_rootFolder.FullName}");
        SetupFileSystemWatcher();
        await PerformScan(stoppingToken);
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformScan(stoppingToken);
        }
    }

    private void SetupFileSystemWatcher()
    {
        _watcher = new FileSystemWatcher(_rootFolder.FullName)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
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

    private async Task PerformScan(CancellationToken stoppingToken)
    {
        if (_processing) return;
        Console.WriteLine("Performing periodic scan...");
        _processing = true;
        try
        {
            HashSet<string> previousFiles;
            lock (_setLock)
            {
                previousFiles = new HashSet<string>(_currentSourceFiles);
            }
            var currentFiles = GetSourceFiles().ToHashSet();            
            var newFiles = currentFiles.Except(previousFiles).ToList();

            foreach (var file in newFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(() => _onFileCreated(file), $"created (scan): {file}");
                if (!created) currentFiles.Remove(file);
            }
            var deletedFiles = previousFiles.Except(currentFiles).ToList();
            foreach (var file in deletedFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(() => _onFileDeleted(file), $"deleted (scan): {file}");
            }
            lock (_setLock)
            {
                _currentSourceFiles = currentFiles;
            }
        }
        finally
        {
            _processing = false;
        }
    }

    private async void OnWatcherFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        Console.WriteLine($"FileSystemWatcher: File created - {e.FullPath}");
        bool created = await InvokeHandlerSafe(() => _onFileCreated(e.FullPath), $"created: {e.FullPath}");
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
        await InvokeHandlerSafe(() => _onFileChanged(e.FullPath), $"changed: {e.FullPath}");
    }

    private async void OnWatcherFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        Console.WriteLine($"FileSystemWatcher: File deleted - {e.FullPath}");
        bool deleted = await InvokeHandlerSafe(() => _onFileDeleted(e.FullPath), $"deleted: {e.FullPath}");
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
        bool renamed = await InvokeHandlerSafe(() => _onFileRenamed(e.OldFullPath, e.FullPath), $"renamed: {e.OldFullPath} -> {e.FullPath}");
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

    private bool ShouldProcessFile(string filePath)
    {
        if (_shouldSkipFile(filePath)) return false;
        if (_extensions != null && !_extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant())) return false;
        return true;
    }

    private async Task<bool> InvokeHandlerSafe(Func<Task> handler, string context)
    {
        try
        {
            await handler();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling file event [{context}]: {ex.Message}");
            return false;
        }
    }

    private IEnumerable<string> GetSourceFiles()
    {
        return Directory.EnumerateFiles(_rootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => ShouldProcessFile(f));
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
