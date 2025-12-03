using Microsoft.Extensions.Hosting;

namespace PicturesLib.service.fileProcessor;

/// <summary>
/// Service that performs periodic scans of a directory to detect file additions and deletions.
/// When it runs the first scan, all files are treated as new files so it calls OnFileCreated for each file found
/// and on subsequent scans it detects new files and deleted files and calls the appropriate handlers.
/// It also identifies files or folders that should be skipped based on the processor's ShouldSkipFile method so 
/// renaming a file or folder to now be skipped will trigger a cleanup of the original files that were included and now should be excluded.
/// </summary>
public class FilePeriodicScanService : BackgroundService 
{
    public FilePeriodicScanService(IFileProcessor processor, int intervalMinutes = 2)        
    {
        this._processor = processor;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected readonly IFileProcessor _processor;
    protected readonly TimeSpan _interval;
    protected bool _processing = false;
    protected HashSet<string> _currentSourceFiles = new();
    protected readonly object _setLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PerformScan(stoppingToken);
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformScan(stoppingToken);
        }
    }

    protected async Task PerformScan(CancellationToken stoppingToken)
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
                bool created = await InvokeHandlerSafe(() => _processor.OnFileCreated(file), $"created (scan): {file}");
                if (!created) currentFiles.Remove(file);
            }
            var deletedFiles = previousFiles.Except(currentFiles).ToList();
            foreach (var file in deletedFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(() => _processor.OnFileDeleted(file), $"deleted (scan): {file}");
            }
            lock (_setLock)
            {
                _currentSourceFiles = currentFiles;
            }

            //identify if we have a scenario where files or folders were renamed to now be skipped and before were not
            //so now we need to clean up the originals that were include and now should be excluded   
            var skip = GetFilesToClean();    
            foreach (var file in skip)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(() => _processor.OnEnsureCleanup(file), $"cleanup (scan): {file}"); 
            }

        }
        finally
        {
            _processing = false;
        }
    }


    protected bool ShouldProcessFile(string filePath)
    {
        if (_processor.ShouldSkipFile(filePath)) return false;        
        if (_processor.Extensions != null && !_processor.Extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant())) return false;
        return true;
    }

    protected bool ShouldCleanFile(string filePath)
    {
        if (_processor.Extensions != null && !_processor.Extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant())) return false;  //don't attemp to clean non image types files
        if (_processor.ShouldSkipFile(filePath)) return true;  //ex: for files/folders renamed to now be skipped and before were not we need to clean up the originals
        return false;
    }

    protected async Task<bool> InvokeHandlerSafe(Func<Task> handler, string context)
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

    protected IEnumerable<string> GetSourceFiles()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => ShouldProcessFile(f));
    }

    /// <summary>
    /// for files or folders that are to be skipped (ex renamed from blog to skip_blog) we need to be clean the original   
    /// </summary>
    protected IEnumerable<string> GetFilesToClean()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => ShouldCleanFile(f));
    }


}
