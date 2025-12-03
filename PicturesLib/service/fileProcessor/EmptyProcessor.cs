using PicturesLib.model.configuration;

namespace PicturesLib.service.fileProcessor;

/// <summary>
/// Factory for creating thumbnail cleanup strategies for FilePeriodicScanService
/// 
/// The cleanup processor monitors the thumbnail directories and deletes thumbnails that should no longer exist based on the configuration 
/// for example a folder called "pss_blog" used to be included in the website but now the 
/// configuration says "pss" is a skip prefix so "pss_blog" should be cleaned up.
/// </summary>
public class EmptyProcessor: IFileProcessor
{

    public EmptyProcessor(PicturesDataConfiguration configuration)
    {
        _configuration = configuration;
        _extensions = configuration.Extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected readonly PicturesDataConfiguration _configuration;
    private HashSet<string> _extensions;
    public virtual DirectoryInfo RootFolder { get { return _configuration.RootFolder; } }
    public virtual HashSet<string> Extensions { get { return _extensions; } }    

    public virtual bool ShouldSkipFile(string filePath)
    {
        return false;
    }

    public virtual async Task<int> OnFileCreated(string thumbnailPath)
    {
        return 0;       
    }
    
    public virtual async Task<int> OnFileDeleted(string thumbnailPath)
    {
        return 0;
    }

    public virtual async Task OnFileChanged(string thumbnailPath)
    {
        await Task.CompletedTask;;
    }

    public virtual async Task OnFileRenamed(string oldThumbnailPath, string newThumbnailPath,  bool newValid)
    {
        await Task.CompletedTask;
    }
     
    public virtual async Task<int> OnEnsureCleanup(string thumbnailPath)
    {
        return 0;
    }

    public virtual async Task OnScanStart(string skipFilePath)
    {
        await Task.CompletedTask;
    }
    public virtual async Task OnScanEnd(string skipFilePath)
    {
        await Task.CompletedTask;
    }       
    

    
}
