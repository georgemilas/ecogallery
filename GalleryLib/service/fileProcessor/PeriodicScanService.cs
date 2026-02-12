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
            var currentFiles = (await GetFilesToProcess()).ToHashSet();
            var elapsed = sw.Elapsed;
                        
            var newFiles = currentFiles.Except(previousFiles).ToList();
            Console.WriteLine($"{_processor.GetType().Name} Enumerated files to process ... {newFiles.Count} files in {elapsed:hh\\:mm\\:ss}.");
            
            long actualNew = 0;
            long totalProcessed = 0;
            await Parallel.ForEachAsync(newFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(async () =>
                { 
                    int delta = await _processor.OnEnsureProcessFile(file, _logIfProcessed);
                    Interlocked.Add(ref actualNew, delta);                    
                }, $"created (scan): {file}");
                if (!created) { lock (_setLock) { currentFiles.Remove(file); } }   //did nor run so we add it back for next time             

                Interlocked.Add(ref totalProcessed, 1);
                if (totalProcessed % 10 == 0) 
                {
                    var elapsed = sw.Elapsed;
                    var rate = totalProcessed / elapsed.TotalSeconds;
                    if (rate != 0  ) 
                    {                            
                        var eta = TimeSpan.FromSeconds((newFiles.Count - totalProcessed) / rate);
                        Console.Write($"\r New: {actualNew} Processed:{totalProcessed}/{newFiles.Count} files ({totalProcessed * 100 / newFiles.Count}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed: {elapsed:hh\\:mm\\:ss}");
                    }
                    else 
                    {
                        Console.Write($"\r New: {actualNew} Processed:{totalProcessed}/{newFiles.Count} files ({totalProcessed * 100 / newFiles.Count}%) - calculating rate... - elapsed: {elapsed:hh\\:mm\\:ss}");
                    }                    
                }
                
            });
            if (actualNew > 0) Console.WriteLine(); // New line after progress output

            

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// deleted files processing - OnFileDeleted
            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            var deletedFiles = previousFiles.Except(currentFiles).ToList();
            long actualDeleted = 0;
            var totalDelProcessed = 0;
            await Parallel.ForEachAsync(deletedFiles, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int delta = await _processor.OnFileDeleted(file, _logIfProcessed);
                    Interlocked.Add(ref actualDeleted, delta);                    
                }, $"deleted (scan): {file}");

                Interlocked.Add(ref totalDelProcessed, 1);
                if (totalProcessed % 10 == 0) 
                {   
                    var elapsed = sw.Elapsed;
                    var rate = totalDelProcessed / elapsed.TotalSeconds;
                    if (rate != 0 && deletedFiles.Count > 0 ) 
                    {
                        var eta = TimeSpan.FromSeconds((deletedFiles.Count - totalDelProcessed) / rate);
                        Console.Write($"\r Deleted: {actualDeleted} Processed: {totalDelProcessed}/{deletedFiles.Count} files ({totalDelProcessed * 100 / deletedFiles.Count}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed: {elapsed:hh\\:mm\\:ss}");
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
            Console.WriteLine($"{_processor.GetType().Name} Enumerated possible candidate files to clean ... {newFilesCleanup.Count()} files");
            
            long actualCleanup = 0;
            var totalCleanupProcessed = 0;
            await Parallel.ForEachAsync(newFilesCleanup, options, async (file, ct) =>
            {
                //if (ct.IsCancellationRequested) break;
                var created = await InvokeHandlerSafe(async () => 
                { 
                    int cleaned = await _processor.OnEnsureCleanupFile(file, _logIfProcessed);
                    Interlocked.Add(ref actualCleanup, cleaned);                    
                }, $"cleanup (scan): {file}"); 
                if (!created) { lock (_setLock) { currentFilesToClenup.Remove(file); } }   //did nor run so we add it back for next time             
                
                Interlocked.Add(ref totalCleanupProcessed, 1);                
                if (totalCleanupProcessed % 10 == 0) 
                {
                    var elapsed = sw.Elapsed;
                    var rate = totalCleanupProcessed / elapsed.TotalSeconds;
                    var cntSkipFiles = currentFilesToClenup.Count();
                    if (rate != 0 && cntSkipFiles > 0 ) 
                    {
                        var eta = TimeSpan.FromSeconds((cntSkipFiles - totalCleanupProcessed) / rate);
                        Console.Write($"\r {_processor.GetType().Name} Cleanup: {actualCleanup} Processed: {totalCleanupProcessed}/{cntSkipFiles} files ({totalCleanupProcessed * 100 / cntSkipFiles}%) - {rate:F1}/s - ETA: {eta:hh\\:mm\\:ss} - elapsed:{elapsed:hh\\:mm\\:ss}");
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
