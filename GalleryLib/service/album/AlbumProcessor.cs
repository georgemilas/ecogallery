using GalleryLib.model.configuration;
using GalleryLib.model.album;
using GalleryLib.service.database;
using GalleryLib.service.fileProcessor;
using GalleryLib.repository;
using System.Collections.Concurrent;

namespace GalleryLib.service.album;

/// <summary>
/// Syncronized the pictures folder and add/edit/delete the coresponding database images & albums 
/// </summary>
public class AlbumProcessor: EmptyProcessor
{
    public AlbumProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig):base(configuration)
    {
        _dbConfig = dbConfig;
        imageRepository = new AlbumImageRepository(configuration, dbConfig);
        albumRepository = new AlbumRepository(configuration, dbConfig);
    }
    private readonly DatabaseConfiguration _dbConfig;
    protected AlbumImageRepository imageRepository;
    protected AlbumRepository albumRepository;

    public override DirectoryInfo RootFolder { get { return _configuration.RootFolder; } }
    protected virtual string thumbnailsBase { get { return _configuration.ThumbnailsBase; } }


    public static FileObserverService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new AlbumProcessor(configuration, dbConfig);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
    public static FileObserverServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
    {
        IFileProcessor processor = new AlbumProcessor(configuration, dbConfig);
        return new FileObserverServiceNotParallel(processor,intervalMinutes: 2);
    }
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _albumLocks = new();
    // Helper to get per-key aka per-album semaphore 
    private SemaphoreSlim GetAlbumLock(string albumName)
    {
        return _albumLocks.GetOrAdd(albumName, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// create image record and ensure album record exists
    /// </summary>
    protected virtual async Task<Tuple<AlbumImage, int>> CreateImageAndAlbumRecords(string filePath, bool logIfCreated )
    {
        AlbumImage? albumImage = await imageRepository.GetAlbumImageAsync(filePath);
        if (albumImage == null)
        {
            Album album = Album.CreateFromFilePath(filePath, RootFolder.FullName);
            //Console.WriteLine($"ran image get db: {filePath}");

            var semaphore = GetAlbumLock(album.AlbumName);
            await semaphore.WaitAsync();  //can't call await in a lock so using semaphore instead
            try
            {
                album = await albumRepository.EnsureAlbumExistsAsync(filePath);
            }
            finally
            {
                semaphore.Release();
            }

            albumImage = await imageRepository.AddNewImageAsync(filePath, album);
            await UpdateImageHashAsync(filePath, logIfCreated, albumImage);

            if (logIfCreated)
            {
                Console.WriteLine($"Created album_image record for changed file: {filePath}");
            }
            return Tuple.Create(albumImage, 1);
        }
        if (string.IsNullOrWhiteSpace(albumImage.ImageSha256))
        {
            await UpdateImageHashAsync(filePath, logIfCreated, albumImage);
        }
        return Tuple.Create(albumImage, 0);
    }

    protected async Task UpdateImageHashAsync(string filePath, bool logIfCreated, AlbumImage albumImage)
    {
        // Compute perceptual hash from 400px thumbnail if it exists
        var thumbnailPath = _configuration.GetThumbnailPath(filePath, 400);
        if (File.Exists(thumbnailPath))
        {
            var thumbStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read);
            albumImage.ImageSha256 = await ImageHash.ComputeSha256Async(thumbStream);
            var id = await imageRepository.UpdateImageHash(albumImage);
            if (logIfCreated)
            {
                string isOk = id == albumImage.Id ? "successfully" : "unsuccessfully";
                Console.WriteLine($"Computed and stored {isOk} SHA-256 hash for image thumbnail: {filePath}");
            }
        }
    }

    /// <summary>
    /// delete image record and if album is empty delete album records recursively as well
    /// </summary>
    protected virtual async Task<int> CleanupImageAndAlbumRecords(string filePath, bool logIfCleaned = false)
    {
        int deletedCount = await imageRepository.DeleteAlbumImageAsync(filePath);
        if (deletedCount > 0 && logIfCleaned)
        {
            Console.WriteLine($"Deleted album_image record: {filePath}");
        }
        if (deletedCount > 0 && !await albumRepository.AlbumHasContentAsync(filePath))
        {        
            await albumRepository.DeleteAlbumAsync(filePath, logIfCleaned);
        }
        return deletedCount;    
    }

    public override bool ShouldSkipFile(string filePath)
    {
        string folder = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileName(filePath);
        return filePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase) ||
                _configuration.SkipSuffix.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                                                        folder.Contains(suffix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipPrefix.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                                                        folder.Contains(prefix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipContains.Any(skipPart => filePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));
    }

    
    public override async Task<int> OnFileCreated(string filePath, bool logIfCreated = false)
    {
        //don't log creation here during the main PeriodicScanService, only for main FileObserverService methods
        var (albumImage, count) = await CreateImageAndAlbumRecords(filePath, logIfCreated);
        return count;  //number of records created (0 or 1)
    }

    public override async Task OnFileChanged(string filePath)
    {
        var (albumImage, count) = await CreateImageAndAlbumRecords(filePath, true);           
    }

    public override async Task<int> OnFileDeleted(string filePath)
    {
        return await CleanupImageAndAlbumRecords(filePath, true);
    }
    
    public override async Task OnFileRenamed(string oldPath, string newPath,  bool newValid)
    {
        await CleanupImageAndAlbumRecords(oldPath);
        if (newValid)
        {
            var (albumImage, count) = await CreateImageAndAlbumRecords(newPath, true);                
        }
    }

    ///<summary>
    /// need to find the original file path from the skip file path and ensure its records are deleted
    /// </summary> 
    public override async Task<int> OnEnsureCleanup(string skipFilePath, bool logIfCleaned = false)
    {
        string skipFolder = Path.GetDirectoryName(skipFilePath) ?? string.Empty;
        string skipFileName = Path.GetFileName(skipFilePath);

        //avoid recursion into created thumbnails
        if (skipFilePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase)) return 0;

        //identify the type of prefix or suffix we are dealing with 
        var fileNameStartWith = _configuration.SkipPrefix.Where(prefix => skipFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        var fileNameEndsWith = _configuration.SkipSuffix.Where(suffix => skipFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        //using contains instead of startwith or endwith to cactch not just the last folder but any parent folder being affected as well
        var folderContainsPrefix = _configuration.SkipPrefix.Where(prefix => skipFolder.Contains(prefix, StringComparison.OrdinalIgnoreCase));
        var folderContainsSuffix = _configuration.SkipSuffix.Where(suffix => skipFolder.Contains(suffix, StringComparison.OrdinalIgnoreCase));
        var filePathContains = _configuration.SkipContains.Where(skipPart => skipFilePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));

        int totalDeleted = 0;
        if (fileNameStartWith.Any())
        {
            //it's a prefix to a file 
            var prefix = fileNameStartWith.First();
            var originalName = skipFileName.Replace(prefix, string.Empty);
            string originalPath = Path.Combine(skipFolder, originalName);
            totalDeleted += await CleanupImageAndAlbumRecords(originalPath, logIfCleaned);
        }

        if (fileNameEndsWith.Any())
        {
            //it's a suffix to a file
            var suffix = fileNameEndsWith.First();
            var originalName = skipFileName.Replace(suffix, string.Empty);
            string originalPath = Path.Combine(skipFolder, originalName);
            totalDeleted += await CleanupImageAndAlbumRecords(originalPath, logIfCleaned);
        }

        //Console.WriteLine($"Ensuring cleanup for folder contains suffixOrPrefix {suffixOrPrefix}: {skipFileName}");                
        if (folderContainsSuffix.Any())
        {
            //it's a suffix to a folder 
            var suffix = folderContainsSuffix.First();
            var originalFolderPath = skipFolder.Replace(suffix, string.Empty);
            string originalPath = Path.Combine(originalFolderPath, skipFileName);
            totalDeleted += await CleanupImageAndAlbumRecords(originalPath, logIfCleaned);
        }

        if (folderContainsPrefix.Any())
        {
            //it's a prefix to a folder 
            var prefix = folderContainsPrefix.First();
            var originalFolderPath = skipFolder.Replace(prefix, string.Empty);
            string originalPath = Path.Combine(originalFolderPath, skipFileName);
            totalDeleted += await CleanupImageAndAlbumRecords(originalPath, logIfCleaned);
        }

        // if (filePathContains.Any())
        // {
        //     //it's a part of the path 
        //     /*
        //             This scenario is not supported because we manualy renamed something to a path that contains one of the skip parts
        //             for example we renamed "blog" to "DCIM" and there is no way to identify that the original was "blog" 

        //             we only support the other senarios (see above) where we can identify the original file or folder name
        //             for example "blog" was renamed to "skip_blog" so we can identify the original was "blog" and we can clean up thumbnails for the "blog" 
        //             which used to be included in website but now is "skip_blog" and it should not be on the website anymore
        //     */
        // }

        return totalDeleted;
    }

    public override async Task OnScanStart()
    {
        // Don't dispose repositories - they may be in use by file watchers
        // Just reinitialize if needed
    }
    
    public override async Task OnScanEnd()
    {
        // Repository connections use pooling, no disposing is needed
    }         
    
}


