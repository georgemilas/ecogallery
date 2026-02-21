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
public class DbPeriodicScanService : PeriodicScanService
{
    public DbPeriodicScanService(IFileProcessor processor, PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int intervalMinutes = 2, int degreeOfParallelism = -1, bool logIfProcessed = false): base(processor, intervalMinutes, degreeOfParallelism, logIfProcessed)        
     {
        _dbConfig = dbConfig;
        imageRepository = new AlbumImageRepository(configuration, dbConfig);
        albumRepository = new AlbumRepository(configuration, dbConfig);
    }
    protected readonly DatabaseConfiguration _dbConfig;
    protected AlbumImageRepository imageRepository;
    protected AlbumRepository albumRepository;  

    protected override async Task<IEnumerable<FileData>> GetFilesToProcess()
    {
        var allImages = (await imageRepository.GetAllAlbumImagesAsync()).Select(f => new FileData(f.ImagePath, f));
        return allImages.Where(f => _processor.ShouldProcessFile(f));   //check if file rules have changed
    }

    /// <summary>
    /// for files or folders that are to be skipped (ex renamed from blog to skip_blog) we need to clean the original   
    /// </summary>
    protected override async Task<IEnumerable<FileData>> GetFilesToClean()
    {
        var allImages = (await imageRepository.GetAllAlbumImagesAsync()).Select(f => new FileData(f.ImagePath, f));
        return allImages.Where(f => _processor.ShouldCleanFile(f));   //check if file rules have changed
    }
}

