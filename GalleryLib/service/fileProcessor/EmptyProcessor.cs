using GalleryLib.model.configuration;

namespace GalleryLib.service.fileProcessor;

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

    public virtual async Task<int> OnFileCreated(string thumbnailPath, bool logIfCreated = false)
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
     
    public virtual async Task<int> OnEnsureCleanup(string skipFilePath, bool logIfCleaned = false)
    {
        return 0;
    }

    public virtual async Task OnScanStart()
    {
        await Task.CompletedTask;
    }
    public virtual async Task OnScanEnd()
    {
        await Task.CompletedTask;
    }       
    
    protected async Task ExecuteWithRetryAttemptsAsync(string filePath, string callingMethod, Func<Task>callback)
    {
        
        //FileSystemWatcher may have kicked this off before the file is fully written to disk (ex: a large file being copied), so we may get an Exception
        //therefor we retry with exponential backoff on failure to give time for the file to be fully written to disk and ready
        //and if we still fail after all attempts is ok because the next iteration of FilePeriodicScanService will try again later
        const int maxAttempts = 5;
        int attempt = 0;
        Exception? lastError = null;
        while (attempt < maxAttempts)
        {
            try
            {
                await callback();      
                return; // Success - exit immediately without Task.Delay
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{callingMethod} IOException on attempt {attempt + 1} for file {filePath}: {ex.Message}");
                lastError = ex;                                
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"{callingMethod} UnauthorizedAccessException on attempt {attempt + 1} for file {filePath}: {ex.Message}");
                lastError = ex;                
            }

            attempt++;
            if (attempt < maxAttempts)
            {
                await Task.Delay(attempt switch { 1 => 100, 2 => 250, 3 => 500, 4 => 1000, _ => 1500 });
            }
        }

        
        if (lastError != null) 
        {
            throw new Exception($"Failed to build thumbnail for file {filePath} after {maxAttempts} attempts.", lastError);    
        }                        
    }
    
}
