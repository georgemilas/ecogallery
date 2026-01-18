using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.fileProcessor;

namespace GalleryLib.service.album;

/// <summary>
/// Factory for creating thumbnail cleanup strategies for FilePeriodicScanService
/// 
/// The cleanup processor monitors the thumbnail directories and deletes thumbnails that should no longer exist based on the configuration 
/// for example a folder called "pss_blog" used to be included in the website but now the 
/// configuration says "pss" is a skip prefix so "pss_blog" should be cleaned up.
/// </summary>
public class DbCleanupProcessor: EmptyProcessor
{
    public DbCleanupProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, bool isPlan = false): base(configuration)
    {
        _dbConfig = dbConfig;
        imageRepository = new AlbumImageRepository(configuration, dbConfig);
        albumRepository = new AlbumRepository(configuration, dbConfig);
        _isPlan = isPlan;
    }
    private readonly DatabaseConfiguration _dbConfig;
    protected AlbumImageRepository imageRepository;
    protected AlbumRepository albumRepository;
    private readonly bool _isPlan;

    public static PeriodicScanService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1, bool isPlan = false, bool logIfProcessed = false)
    {
        IFileProcessor processor = new DbCleanupProcessor(configuration, dbConfig, isPlan);
        return new DbPeriodicScanService(processor, configuration, dbConfig, intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism, logIfProcessed);        
    }
    public static PeriodicScanServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, bool isPlan = false, bool logIfProcessed = false)
    {
        IFileProcessor processor = new DbCleanupProcessor(configuration, dbConfig, isPlan);
        return new DbPeriodicScanServiceNonParallel(processor, configuration, dbConfig, 2, logIfProcessed);
    }

    /// <summary>
    /// process invalid files as dictated by configuration
    /// for example a folder named "skip_folderName" or "folderName_skip" or a file named "imageName_skip.jpg" or "skip_imageName.jpg" 
    /// should not be in the database
    /// </summary>
    private bool isInvalidFile(string dbPath)
    {
        //the file path in db is already a relative path
        string relativeFolder = Path.GetDirectoryName(dbPath) ?? string.Empty;
        string fileName = Path.GetFileName(dbPath);
        var res = _configuration.SkipSuffix.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)   || 
                                                        relativeFolder.Contains(suffix, StringComparison.OrdinalIgnoreCase)) ||                    
                _configuration.SkipPrefix.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || 
                                                        relativeFolder.Contains(prefix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipContains.Any(skipPart => dbPath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));
        //Console.WriteLine($"Check for cleanup ({res}): {dbPath}");
        return res;
    }

    private async Task<int> deleteEmptyFolderAsync(AlbumImage dbPath, bool logIfCreated = false)
    {
        Album album = Album.CreateFromAlbumPath(dbPath.AlbumName, base.RootFolder.FullName);
        bool hasContent = await albumRepository.AlbumHasContentAsync(album);    
        int cnt = 0;
        if (!hasContent)
        {
            if (!_isPlan)
            {
                cnt = await albumRepository.DeleteAlbumAsync(album, logIfCreated);                        
            }
            if (logIfCreated)
            {
                Console.WriteLine($"{( _isPlan ? "Fake" : "" )} Deleted empty album: {album.AlbumName}");
            }                        
        }
        return cnt;
    }        
    
    /// <summary>
    /// Since this is a cleanup processor, a file to be processed means a file which is invalid as dictated by configuration. 
    /// For example the name of the file or folder matches a "skip" definition but before did not
    /// </summary>
    public override bool ShouldProcessFile(FileData dbPath)
    {
        return isInvalidFile(dbPath.FilePath);
    }

    /// <summary>
    /// Since this is a cleanup processor, a file to be cleaned up is a file that exists in the thumbnails folder 
    /// but is not in the originals pictures folder, therefor needs to be cleaned up
    /// </summary>
    public override bool ShouldCleanFile(FileData dbPath)
    {
        var res = !isInvalidFile(dbPath.FilePath); //is a good file (not matching skip criteria)
        string originalFilePath = GetOriginalFilePath(dbPath.FilePath);
        if (!File.Exists(originalFilePath))
        {
            return true;
        }
        return false;
    }

    private string GetOriginalFilePath(string dbPath)
    {
        var originalsFolders = base.RootFolder.FullName;
        var relativeDbPath = dbPath.TrimStart('\\', '/');  // Remove leading directory separator to ensure proper path combination
        var originalFilePath = Path.Combine(originalsFolders, relativeDbPath);
        return originalFilePath;
    }

    public override async Task<int> OnFileCreated(FileData dbPath, bool logIfCreated = false)
    {
        //This db record exists and it shouldn't so we are going to delete it 
        var originalFilePath = GetOriginalFilePath(dbPath.FilePath);
        int cnt = 0;
        if (!_isPlan)
        {
            cnt = await imageRepository.DeleteAlbumImageAsync(originalFilePath);
        }
        if (cnt > 0 && logIfCreated)
        {
            Console.WriteLine($"{(_isPlan ? "Fake" : "")} Deleted album_image record: {dbPath.FilePath}, id: {((AlbumImage)dbPath.Data).Id} ");
        }
        await deleteEmptyFolderAsync((AlbumImage)dbPath.Data, logIfCreated);
        return cnt;
        
    }    

    public override async Task<int> OnFileDeleted(FileData dbPath, bool logIfCreated = false)
    {
        //images should not be manually deleted from the database
        return 0;
    }

    public override Task OnFileChanged(FileData dbPath)
    {
        //images in the database should not be manually changed/renamed/moved etc.
        return Task.CompletedTask;
    }

    public override async Task OnFileRenamed(FileData oldDbPath, FileData newDbPath,  bool newValid)
    {
        //images in the database should not be manually changed/renamed/moved etc.
        await Task.CompletedTask;
    }
     
    public override async Task<int> OnEnsureCleanupFile(FileData dbPath, bool logIfCleaned = false)
    {
        //delete files from database that are no longer in the originals
        return await OnFileCreated(dbPath, logIfCleaned);  
    }

   
    
}
