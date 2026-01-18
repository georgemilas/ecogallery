using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace GalleryLib.service.fileProcessor; 

/// <summary>
/// Abstract Service that performs periodic scans based on GetFilesToProcess and GetFilesToClean.
/// Build a local state of files processed the first time and next iterations compares against it for deltas
/// </summary>
public abstract class PeriodicScanService : BackgroundService 
{
    public PeriodicScanService(IFileProcessor processor, int intervalMinutes = 2, int degreeOfParallelism = -1, bool logIfProcessed = false)        
    {
        this._processor = processor;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
        _degreeOfParallelism = degreeOfParallelism == -1 ? Environment.ProcessorCount : degreeOfParallelism; 
        _logIfProcessed = logIfProcessed;
    }

    protected readonly IFileProcessor _processor;
    protected readonly TimeSpan _interval;
    protected readonly int _degreeOfParallelism;
    protected bool _processing = false;
    protected readonly bool _logIfProcessed;
    protected HashSet<FileData> _currentSourceFiles = new();
    protected HashSet<FileData> _currentSourceFilesCleanup = new();
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
        Console.WriteLine($"{_processor.GetType().Name} Performing periodic scan (Parallel {_degreeOfParallelism}) ...");
        _processing = true;
        await _processor.OnScanStart();
        try
        {
            Stopwatch sw = Stopwatch.StartNew();    
            HashSet<FileData> previousFiles;
            lock (_setLock)
            {
                previousFiles = [.. _currentSourceFiles];
            }

            var options = new ParallelOptions { MaxDegreeOfParallelism = _degreeOfParallelism, CancellationToken = stoppingToken };
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// new files processing - OnFileCreated
            /////////////////////////////////////////////////////////////////////////////////////////////////////// 
            Console.Write($"{_processor.GetType().Name} Enumerating files to process ...");
            var currentFiles = (await GetFilesToProcess()).ToHashSet();
            var elapsed = sw.Elapsed;
                        
            var newFiles = currentFiles.Except(previousFiles).ToList();
            Console.Write($"{_processor.GetType().Name} Enumerating files to process ... {currentFiles.Count} files in {elapsed:hh\\:mm\\:ss}.");
            Console.WriteLine();

            long actualNew = 0;
            await Parallel.ForEachAsync(newFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(async () =>
                { 
                    int delta = await _processor.OnEnsureProcessFile(file, _logIfProcessed);
                    Interlocked.Add(ref actualNew, delta);                    
                }, $"created (scan): {file}");
                if (!created) { lock (_setLock) { currentFiles.Remove(file); } }   //did nor run so we add it back for next time             

                
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
                    int delta = await _processor.OnFileDeleted(file, _logIfProcessed);
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
            HashSet<FileData> previousFilesCleanup;
            lock (_setLock)
            {
                previousFilesCleanup = [.. _currentSourceFilesCleanup];
            }

            var currentFilesToClenup = (await GetFilesToClean()).ToHashSet();
            var newFilesCleanup = currentFilesToClenup.Except(previousFilesCleanup).ToList();
            Console.WriteLine($"{_processor.GetType().Name} Enumerating possible candidate files to clean ... {newFilesCleanup.Count()} files");
            
            long actualCleanup = 0;
            await Parallel.ForEachAsync(newFilesCleanup, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                var created = await InvokeHandlerSafe(async () => 
                { 
                    int cleaned = await _processor.OnEnsureCleanupFile(file, _logIfProcessed);
                    Interlocked.Add(ref actualCleanup, cleaned);                    
                }, $"cleanup (scan): {file}"); 
                if (!created) { lock (_setLock) { currentFilesToClenup.Remove(file); } }   //did nor run so we add it back for next time             

                
                if (actualCleanup % 10 == 0) 
                {
                    var elapsed = sw.Elapsed;
                    var rate = actualCleanup / elapsed.TotalSeconds;
                    var cntSkipFiles = currentFilesToClenup.Count();
                    if (rate != 0 && cntSkipFiles > 0 ) 
                    {
                        var eta = TimeSpan.FromSeconds((cntSkipFiles - actualCleanup) / rate);
                        Console.Write($"\r {_processor.GetType().Name} Cleanup: {actualCleanup}/{cntSkipFiles} files ({actualCleanup * 100 / cntSkipFiles}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed:{elapsed:hh\\:mm\\:ss}");
                    }
                }
                
            });
            if (actualCleanup > 0) Console.WriteLine(); // New line after progress output
            lock (_setLock)
            {
                _currentSourceFilesCleanup = currentFilesToClenup;
            }
            
            sw.Stop();
            Console.WriteLine($"{_processor.GetType().Name} Periodic scan (Parallel {_degreeOfParallelism}) completed in {sw.Elapsed:hh\\:mm\\:ss}. New files: {actualNew}/{newFiles.Count}, Deleted files: {actualDeleted}/{deletedFiles.Count}, Cleanup: {actualCleanup}/{currentFilesToClenup.Count()}");

        }
        finally
        {
            await _processor.OnScanEnd();
            _processing = false;
        }
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
            Console.WriteLine($"{_processor.GetType().Name} Error handling file event [{context}]: {ex}");
            return false;
        }
    }

    protected abstract Task<IEnumerable<FileData>> GetFilesToProcess();
    protected abstract Task<IEnumerable<FileData>> GetFilesToClean();


}
