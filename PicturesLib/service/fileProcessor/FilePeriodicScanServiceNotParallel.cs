using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace PicturesLib.service.fileProcessor;

/// <summary>
/// Use the Parallel version of the FilePeriodicScanService to process files faster using multiple threads
/// - this is a backup version that processes files sequentially without parallelism 
/// </summary>
public class FilePeriodicScanServiceNotParallel : FilePeriodicScanService 
{
    public FilePeriodicScanServiceNotParallel(IFileProcessor processor, int intervalMinutes = 2): base(processor, intervalMinutes)        
    {
        
    }

    protected override async Task PerformScan(CancellationToken stoppingToken)
    {
        if (_processing) return;
        Console.WriteLine("Performing periodic scan (Not Parallel)...");
        _processing = true;
        try
        {
            Stopwatch sw = Stopwatch.StartNew();    
            HashSet<string> previousFiles;
            lock (_setLock)
            {
                previousFiles = new HashSet<string>(_currentSourceFiles);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// new files processing - OnFileCreated
            /////////////////////////////////////////////////////////////////////////////////////////////////////// 
            var currentFiles = GetSourceFiles().ToHashSet();            
            var newFiles = currentFiles.Except(previousFiles).ToList();

            long actualNew = 0;
            foreach (var file in newFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                bool created = await InvokeHandlerSafe(async () =>
                { 
                    int delta = await _processor.OnFileCreated(file, false);
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
                    int delta = await _processor.OnFileDeleted(file);
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
            var skipFiles = GetFilesToClean();    
            long actualCleanup = 0;
            
            foreach (var file in skipFiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await InvokeHandlerSafe(async () => 
                { 
                    int cleaned = await _processor.OnEnsureCleanup(file);
                    actualCleanup += cleaned;                      
                }, $"cleanup (scan): {file}"); 
            }

            sw.Stop();
            Console.WriteLine($"Periodic scan (Not Parallel) completed in {sw.Elapsed.TotalSeconds} seconds ({sw.Elapsed.TotalMinutes} minutes). New files: {actualNew}/{newFiles.Count}, Deleted files: {actualDeleted}/{deletedFiles.Count}, Cleanup: {actualCleanup}/{skipFiles.Count()}");

        }
        finally
        {
            _processing = false;
        }
    }


    

}
