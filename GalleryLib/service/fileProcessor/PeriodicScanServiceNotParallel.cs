using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace GalleryLib.service.fileProcessor;

/// <summary>
/// Use the Parallel version of the FilePeriodicScanService to process files faster using multiple threads
/// - this is a backup version that processes files sequentially without parallelism 
/// </summary>
public abstract class PeriodicScanServiceNotParallel : PeriodicScanService 
{
    public PeriodicScanServiceNotParallel(IFileProcessor processor, int intervalMinutes = 2, bool logIfProcessed = false): base(processor, intervalMinutes, -1, logIfProcessed)        
    {
        
    }

    protected override async Task PerformScan(CancellationToken stoppingToken)
    {
        if (_processing) return;
        Console.WriteLine($"{_processor.GetType().Name} Performing periodic scan (Not Parallel)...");
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

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// new files processing - OnFileCreated
            /////////////////////////////////////////////////////////////////////////////////////////////////////// 
            Console.Write($"{_processor.GetType().Name} Enumerating files to process ...");
            var currentFiles = (await GetFilesToProcess()).ToHashSet();            
            var elapsed = sw.Elapsed;
            Console.Write($"{_processor.GetType().Name} Enumerating files to process ... {currentFiles.Count} files in {elapsed:hh\\:mm\\:ss}.");
            Console.WriteLine();
            
            var newFiles = currentFiles.Except(previousFiles).ToList();
            long actualNew = 0;
            foreach (var file in newFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(async () =>
                { 
                    int delta = await _processor.OnEnsureProcessFile(file, _logIfProcessed);
                    actualNew += delta;
                }, $"created (scan): {file}");
                if (!created) { lock (_setLock) { currentFiles.Remove(file); } }
            }


            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// deleted files processing - OnFileDeleted
            /////////////////////////////////////////////////////////////////////////////////////////////////////// 
            var deletedFiles = previousFiles.Except(currentFiles).ToList();
            long actualDeleted = 0;
            
            foreach (var file in deletedFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int delta = await _processor.OnFileDeleted(file, _logIfProcessed);
                    actualDeleted += delta;                    
                }, $"deleted (scan): {file}");
            }

            lock (_setLock)
            {
                _currentSourceFiles = currentFiles;
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// skip files cleanup processing - OnEnsureCleanup
            /// identify if we have a scenario where files or folders were renamed to now be skipped and before were not
            /// so now we need to clean up the originals that were include and now should be excluded   
            ///////////////////////////////////////////////////////////////////////////////////////////////////////  
            var skipFiles = await GetFilesToClean();    
            long actualCleanup = 0;
            
            foreach (var file in skipFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int cleaned = await _processor.OnEnsureCleanupFile(file, _logIfProcessed);
                    actualCleanup += cleaned;                      
                }, $"cleanup (scan): {file}"); 
            }

            sw.Stop();
            Console.WriteLine($"{_processor.GetType().Name} Periodic scan (Not Parallel) completed in {sw.Elapsed.TotalSeconds} seconds ({sw.Elapsed.TotalMinutes} minutes). New files: {actualNew}/{newFiles.Count}, Deleted files: {actualDeleted}/{deletedFiles.Count}, Cleanup: {actualCleanup}/{skipFiles.Count()}");

        }
        finally
        {
            await _processor.OnScanEnd();
            _processing = false;
        }
    }

}
