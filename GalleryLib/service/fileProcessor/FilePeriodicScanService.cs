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
public class FilePeriodicScanService : PeriodicScanService
{
    public FilePeriodicScanService(IFileProcessor processor, int intervalMinutes = 2, int degreeOfParallelism = -1, bool logIfCreated = false): base(processor, intervalMinutes, degreeOfParallelism, logIfCreated)        
    {
        
    }    

    protected override async Task<IEnumerable<FileData>> GetFilesToProcess()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => _processor.ShouldProcessFile(new FileData(f, f)))
            .Select(f => new FileData(f, f));  
    }

    /// <summary>
    /// for files or folders that are to be skipped (ex renamed from blog to skip_blog) we need to clean the original   
    /// </summary>
    protected override async Task<IEnumerable<FileData>> GetFilesToClean()
    {
        return Directory.EnumerateFiles(_processor.RootFolder.FullName, "*.*", SearchOption.AllDirectories)
            .Where(f => _processor.ShouldCleanFile(new FileData(f, f)))
            .Select(f => new FileData(f, f));
    }
}
