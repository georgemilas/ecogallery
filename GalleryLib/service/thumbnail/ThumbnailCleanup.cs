using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;

namespace GalleryLib.service.thumbnail;

/// <summary>
/// Factory for creating thumbnail cleanup strategies for FilePeriodicScanService
/// 
/// The cleanup processor monitors the thumbnail directories and deletes thumbnails that should no longer exist based on the configuration 
/// for example a folder called "pss_blog" used to be included in the website but now the 
/// configuration says "pss" is a skip prefix so "pss_blog" should be cleaned up.
/// </summary>
public class ThumbnailCleanup: EmptyProcessor
{

    public ThumbnailCleanup(PicturesDataConfiguration configuration, int height = 400): base(configuration)
    {
        _height = height;        
    }

    private readonly int _height;    

    /// <summary>
    /// Process files from the actual thumbnails directory in the picturesPath/_thumbnails/{height} folder
    /// </summary>
    public override DirectoryInfo RootFolder { get { return new DirectoryInfo(thumbDir); } }
    protected virtual string thumbDir { get { return _configuration.ThumbDir(_height); } }    
    
    public static FilePeriodicScanService CreateProcessor(PicturesDataConfiguration configuration, int height = 400, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new ThumbnailCleanup(configuration, height);
        return new FilePeriodicScanService(processor, intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);        
    }
    public static FilePeriodicScanServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, int height = 400)
    {
        IFileProcessor processor = new ThumbnailCleanup(configuration, height);
        return new FilePeriodicScanServiceNotParallel(processor,intervalMinutes: 2);
    }


    /// <summary>
    /// clean files as dictated by configuration
    /// for example a folder named "skip_folderName" or "folderName_skip" or a file named "imageName_skip.jpg" or "skip_imageName.jpg"
    /// </summary>
    private bool shouldCleanFile(string thumbnailPath)
    {
        //get the relative file path from the thumbnail folder
        string relativeFilePath = thumbnailPath.Replace(thumbDir, String.Empty);   
        string relativeFolder = Path.GetDirectoryName(relativeFilePath) ?? string.Empty;
        string fileName = Path.GetFileName(relativeFilePath);
        var res = _configuration.SkipSuffix.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)   || 
                                                        relativeFolder.Contains(suffix, StringComparison.OrdinalIgnoreCase)) ||                    
                _configuration.SkipPrefix.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || 
                                                        relativeFolder.Contains(prefix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipContains.Any(skipPart => relativeFilePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));
        //Console.WriteLine($"Check for cleanup ({res}): {filePath}");
        return res;
    }

    private void deleteEmptyFolder(string thumbnailPath, bool logIfCreated = false)
    {
        // Clean up empty directories
        var directory = Path.GetDirectoryName(thumbnailPath);
        if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory, recursive: true);
            if (logIfCreated)
            {
                Console.WriteLine($"Deleted empty thumbnail directory: {directory}");
            }
            deleteEmptyFolder(directory, logIfCreated); //recursively delete parent if empty
        }
    }        

    public override bool ShouldSkipFile(string thumbnailPath)
    {
        var res =  !shouldCleanFile(thumbnailPath);
        //Console.WriteLine($"Check for skip ({res}): {filePath}");
        return res;
    }

    public override async Task<int> OnFileCreated(string thumbnailPath, bool logIfCreated = false)
    {
        //This thumbnail exists and it shouldn't so we are going to delete it 
        if (File.Exists(thumbnailPath))
        {
            //shouldCleanFile(thumbnailPath);
            File.Delete(thumbnailPath);
            if (logIfCreated)
            {
                Console.WriteLine($"Deleted thumbnail: {thumbnailPath}");
            }
            deleteEmptyFolder(thumbnailPath, logIfCreated);
            return 1;
        }
        return 0;       
    }    

    public override async Task<int> OnFileDeleted(string thumbnailPath)
    {
        //the thumbnail was deleted either manualy or by the cleanup process so no action needed
        return 0;
    }

    public override Task OnFileChanged(string thumbnailPath)
    {
        //thumbnails should not be manually changed/renamed/moved etc.
        return Task.CompletedTask;
    }

    public override async Task OnFileRenamed(string oldThumbnailPath, string newThumbnailPath,  bool newValid)
    {
        //thumbnails should not be manually changed/renamed/moved etc.
        await Task.CompletedTask;
    }
     
    public override async Task<int> OnEnsureCleanup(string thumbnailPath, bool logIfCleaned = false)
    {
        //no additional cleanup needed on the _thumbnail folder itself as 
        //thumbnails should not be manually changed/renamed/moved etc.
        return 0;
    }

   
    
}
