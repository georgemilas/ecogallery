using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace GalleryLib.service.fileProcessor;

/// <summary>
/// Use the Parallel version of the FilePeriodicScanService to process files faster using multiple threads
/// - this is a backup version that processes files sequentially without parallelism 
/// </summary>
public class FilePeriodicScanServiceNotParallel : PeriodicScanServiceNotParallel
{
    public FilePeriodicScanServiceNotParallel(IFileProcessor processor, int intervalMinutes = 2, bool logIfCreated = false): base(processor, intervalMinutes, logIfCreated)        
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
