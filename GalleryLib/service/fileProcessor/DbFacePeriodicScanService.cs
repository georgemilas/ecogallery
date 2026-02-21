using System.Diagnostics;
using System.Net;
using GalleryLib.model.configuration;
using GalleryLib.model.album;
using GalleryLib.repository;
using Microsoft.Extensions.Hosting;

namespace GalleryLib.service.fileProcessor;

/// <summary>
/// Service that performs periodic scans of a directory to detect file additions and deletions.
/// When it runs the first scan, all files are treated as new files so it calls OnFileCreated for each file found
/// and on subsequent scans it detects new files and deleted files and calls the appropriate handlers.
/// It also identifies files or folders that should be skipped based on the processor's ShouldSkipFile method so 
/// renaming a file or folder to now be skipped will trigger a cleanup of the original files that were included and now should be excluded.
/// </summary>
public class DbFacePeriodicScanService : DbPeriodicScanService
{
    public DbFacePeriodicScanService(IFileProcessor processor, PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int intervalMinutes = 2, int degreeOfParallelism = -1, bool logIfProcessed = false): base(processor, configuration, dbConfig, intervalMinutes, degreeOfParallelism, logIfProcessed)        
    {
    }
    
    protected override async Task<IEnumerable<FileData>> GetFilesToProcess()    
    {
        //only process files that have not been face processed yet
        var allImages = (await imageRepository.GetFromFaceAlbumImageAttributesAsync(false)).Select(f => new FileData(f.ImagePath, f));
        return allImages.Where(f => _processor.ShouldProcessFile(f));   //check if file rules have changed
    }

    protected override async Task<IEnumerable<FileData>> GetFilesToClean()
    {
        //clean files that have been face processed already if rules have changed 
        var allImages = (await imageRepository.GetFromFaceAlbumImageAttributesAsync(true)).Select(f => new FileData(f.ImagePath, f));
        return allImages.Where(f => _processor.ShouldProcessFile(f));   //check if file rules have changed 
    }
}

