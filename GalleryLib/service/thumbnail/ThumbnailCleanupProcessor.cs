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
public class ThumbnailCleanupProcessor : EmptyProcessor
{

    public ThumbnailCleanupProcessor(PicturesDataConfiguration configuration, int height = 400, bool isPlan = false): base(configuration)
    {
        _height = height;        
        _isPlan = isPlan;        
    }

    private readonly int _height;    
    private readonly bool _isPlan;

    /// <summary>
    /// Process files from the actual thumbnails directory in the picturesPath/_thumbnails/{height} folder
    /// </summary>
    public override DirectoryInfo RootFolder { get { return new DirectoryInfo(thumbDir); } }
    protected virtual string thumbDir { get { return _configuration.ThumbDir(_height); } }    
    
    public static FilePeriodicScanService CreateProcessor(PicturesDataConfiguration configuration, int height = 400, int degreeOfParallelism = -1, bool isPlan = false, bool logIfProcessed = false)
    {
        IFileProcessor processor = new ThumbnailCleanupProcessor(configuration, height, isPlan);
        return new FilePeriodicScanService(processor, intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism, logIfProcessed);        
    }


    /// <summary>
    /// process invalid files as dictated by configuration
    /// for example a folder named "skip_folderName" or "folderName_skip" or a file named "imageName_skip.jpg" or "skip_imageName.jpg" 
    /// should not be in the _thumbnails folder
    /// </summary>
    private bool isInvalidFile(string thumbnailPath)
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

    private void deleteEmptyFolder(string thumbnailPath, bool logIfDeleted = false)
    {
        // Clean up empty directories
        var directory = Path.GetDirectoryName(thumbnailPath);
        if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            if (!_isPlan) {
                Directory.Delete(directory, recursive: true);
            }

            if (logIfDeleted)
            {
                Console.WriteLine($"{(_isPlan ? "Fake" : "")} Deleted empty thumbnail directory: {directory}");
            }
            deleteEmptyFolder(directory, logIfDeleted); //recursively delete parent if empty
        }
    }        
    
    /// <summary>
    /// Since this is a cleanup processor, a file to be processed means a file which is invalid as dictated by configuration. 
    /// For example the name of the file or folder matches a "skip" definition but before did not
    /// </summary>
    public override bool ShouldProcessFile(FileData thumbnailPath, bool logIfProcess = false)
    {
        return isInvalidFile(thumbnailPath.FilePath);
    }

    public IEnumerable<string> GetAllPossibleFiles(string sourceFilePath)
    {
        var fileExt = Path.GetExtension(sourceFilePath);
        string folder = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);

        return _configuration.Extensions.Select(ext => Path.Combine(folder, fileName + ext));
        //return fileExt.Equals(".mp4", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Since this is a cleanup processor, a file to be cleaned up is a file that exists in the thumbnails folder 
    /// but is not in the originals pictures folder, therefor needs to be cleaned up
    /// </summary>
    public override bool ShouldCleanFile(FileData thumbnailPath, bool logIfProcess = false)
    {
        var res =  !isInvalidFile(thumbnailPath.FilePath); //is a good file (not matching skip criteria)
        var originalsFolders = base.RootFolder.FullName;
        var originalFilePath = thumbnailPath.FilePath.Replace(this.thumbDir, originalsFolders);
        var allPossibleFiles = GetAllPossibleFiles(originalFilePath);  //may be a movie file in the original but thumbnail is a jpg
        if (!allPossibleFiles.Any(File.Exists))
        {
            if (logIfProcess) {
                Console.WriteLine($"No original was found with any extension: {originalFilePath}, [{string.Join(", ", _configuration.Extensions)}]");
            }
            //None of the possible original files exist, so this thumbnail should be cleaned up
            //Console.WriteLine($"No original was found {string.Join(", ", allPossibleFiles)}");
            return true;
        }
        return false;
    }


    public override async Task<int> OnFileCreated(FileData thumbnailPath, bool logIfCreated = false)
    {
        //This thumbnail exists and it shouldn't so we are going to delete it 
        if (File.Exists(thumbnailPath.FilePath))
        {
            if (!_isPlan) {
                File.Delete(thumbnailPath.FilePath);
            }

            if (logIfCreated)
            {
                Console.WriteLine($"{(_isPlan ? "Fake" : "")} Deleted thumbnail: {thumbnailPath.FilePath}");
            }
            deleteEmptyFolder(thumbnailPath.FilePath, logIfCreated);
            return 1;
        }
        return 0;       
    }    

    public override async Task<int> OnFileDeleted(FileData thumbnailPath, bool logIfCreated = false)
    {
        //the thumbnail was deleted either manualy or by the cleanup process so no action needed
        return 0;
    }

    public override Task OnFileChanged(FileData thumbnailPath)
    {
        //thumbnails should not be manually changed/renamed/moved etc.
        return Task.CompletedTask;
    }

    public override async Task OnFileRenamed(FileData oldThumbnailPath, FileData newThumbnailPath,  bool newValid)
    {
        //thumbnails should not be manually changed/renamed/moved etc.
        await Task.CompletedTask;
    }
     
    public override async Task<int> OnEnsureCleanupFile(FileData thumbnailPath, bool logIfCleaned = false)
    {
        //delete files from thumbnails that are no longer in the originals
        return await OnFileCreated(thumbnailPath, logIfCleaned);  
    }

   
    
}
