using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace GalleryLib.service.fileProcessor;

/// <summary>
/// Service that performs periodic scans of a directory to detect file additions and deletions.
/// When it runs the first scan, all files are treated as new files so it calls OnFileCreated for each file found
/// and on subsequent scans it detects new files and deleted files and calls the appropriate handlers.
/// It also identifies files or folders that should be skipped based on the processor's ShouldSkipFile method so 
/// renaming a file or folder to now be skipped will trigger a cleanup of the original files that were included and now should be excluded.
/// </summary>
public class FilePeriodicScanService : BackgroundService 
{
    public FilePeriodicScanService(IFileProcessor processor, int intervalMinutes = 2, int degreeOfParallelism = -1)        
    {
        this._processor = processor;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
        _degreeOfParallelism = degreeOfParallelism == -1 ? Environment.ProcessorCount : degreeOfParallelism; 
    }

    protected readonly IFileProcessor _processor;
    protected readonly TimeSpan _interval;
    protected readonly int _degreeOfParallelism;
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

    protected virtual async Task PerformScan(CancellationToken stoppingToken)
    {
        if (_processing) return;
        Console.WriteLine($"Performing periodic scan (Parallel {_degreeOfParallelism}) ...");
        _processing = true;
        await _processor.OnScanStart();
        try
        {
            Stopwatch sw = Stopwatch.StartNew();    
            HashSet<string> previousFiles;
            lock (_setLock)
            {
                previousFiles = new HashSet<string>(_currentSourceFiles);
            }

            var options = new ParallelOptions { MaxDegreeOfParallelism = _degreeOfParallelism, CancellationToken = stoppingToken };
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// new files processing - OnFileCreated
            /////////////////////////////////////////////////////////////////////////////////////////////////////// 
            var currentFiles = GetSourceFiles().ToHashSet();            
            var newFiles = currentFiles.Except(previousFiles).ToList();
            long actualNew = 0;
            await Parallel.ForEachAsync(newFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(async () =>
                { 
                    int delta = await _processor.OnFileCreated(file, false);
                    Interlocked.Add(ref actualNew, delta);                    
                }, $"created (scan): {file}");
                if (!created) { lock (_setLock) { currentFiles.Remove(file); } }                

                
                if (actualNew % 10 == 0) 
                {
                    var elapsed = sw.Elapsed;
                    var rate = actualNew / elapsed.TotalSeconds;
                    if (rate != 0 && newFiles.Count > 0 ) 
                    {                            
                        var eta = TimeSpan.FromSeconds((newFiles.Count - actualNew) / rate);
                        Console.Write($"\r New: {actualNew}/{newFiles.Count} files ({actualNew * 100 / newFiles.Count}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed: {elapsed:hh\\:mm\\:ss}");
                    }
                }
                
            });
            if (actualNew > 0) Console.WriteLine(); // New line after progress output

            

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// deleted files processing - OnFileDeleted
            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            var deletedFiles = previousFiles.Except(currentFiles).ToList();
            long actualDeleted = 0;
            await Parallel.ForEachAsync(deletedFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int delta = await _processor.OnFileDeleted(file);
                    Interlocked.Add(ref actualDeleted, delta);                    
                }, $"deleted (scan): {file}");

                
                if (actualDeleted % 10 == 0) 
                {   
                    var elapsed = sw.Elapsed;
                    var rate = actualDeleted / elapsed.TotalSeconds;
                    if (rate != 0 && deletedFiles.Count > 0 ) 
                    {
                        var eta = TimeSpan.FromSeconds((deletedFiles.Count - actualDeleted) / rate);
                        Console.Write($"\r Deleted: {actualDeleted}/{deletedFiles.Count} files ({actualDeleted * 100 / deletedFiles.Count}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed: {elapsed:hh\\:mm\\:ss}");
                    }
                }
                
            });
            if (actualDeleted > 0) Console.WriteLine(); // New line after progress output
            lock (_setLock)
            {
                _currentSourceFiles = currentFiles;
            }


            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// skip files cleanup processing - OnEnsureCleanup
            /// identify if we have a scenario where files or folders were renamed to now be skipped and before were not
            /// so now we need to clean up the originals that were include and now should be excluded   
            ///////////////////////////////////////////////////////////////////////////////////////////////////////            
            var skipFiles = GetFilesToClean();    
            long actualCleanup = 0;
            await Parallel.ForEachAsync(skipFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int cleaned = await _processor.OnEnsureCleanup(file, false);
                    Interlocked.Add(ref actualCleanup, cleaned);                    
                }, $"cleanup (scan): {file}"); 

                
                if (actualCleanup % 10 == 0) 
                {
                    var elapsed = sw.Elapsed;
                    var rate = actualCleanup / elapsed.TotalSeconds;
                    if (rate != 0 && skipFiles.Count() > 0 ) 
                    {
                        var eta = TimeSpan.FromSeconds((skipFiles.Count() - actualCleanup) / rate);
                        Console.Write($"\r Cleanup: {actualCleanup}/{skipFiles.Count()} files ({actualCleanup * 100 / skipFiles.Count()}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed:{elapsed:hh\\:mm\\:ss}");
                    }
                }
                
            });
            if (actualCleanup > 0) Console.WriteLine(); // New line after progress output
            
            sw.Stop();
            Console.WriteLine($"Periodic scan (Parallel {_degreeOfParallelism}) completed in {sw.Elapsed:hh\\:mm\\:ss}. New files: {actualNew}/{newFiles.Count}, Deleted files: {actualDeleted}/{deletedFiles.Count}, Cleanup: {actualCleanup}/{skipFiles.Count()}");

        }
        finally
        {
            await _processor.OnScanEnd();
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
            Console.WriteLine($"Error handling file event [{context}]: {ex}");
            return false;
        }
    }

    protected IEnumerable<string> GetSourceFiles()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => ShouldProcessFile(f));
    }

    /// <summary>
    /// for files or folders that are to be skipped (ex renamed from blog to skip_blog) we need to clean the original   
    /// </summary>
    protected IEnumerable<string> GetFilesToClean()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => ShouldCleanFile(f));
    }


}
