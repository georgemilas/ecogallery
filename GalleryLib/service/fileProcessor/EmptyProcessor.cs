using GalleryLib.model.configuration;

namespace GalleryLib.service.fileProcessor;

public class EmptyProcessor : IFileProcessor
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
    protected virtual string thumbnailsBase { get { return _configuration.ThumbnailsBase; } }

    /// <summary>
    /// skip files already in _thumbnails directory and as dictated by configuration
    /// for example a folder named "skip_folderName" or "folderName_skip" or a file named "imageName_skip.jpg" or "skip_imageName.jpg"
    /// </summary>
    private bool ShouldSkipFile(string filePath)
    {
        string folder = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileName(filePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        return filePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase) ||
                _configuration.SkipSuffix.Any(suffix => fileNameWithoutExt.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                                                        folder.Contains(suffix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipPrefix.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                                                        folder.Contains(prefix, StringComparison.OrdinalIgnoreCase)) ||
                _configuration.SkipContains.Any(skipPart => filePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));
    }

    ///<summary>
    /// Definition: a file is to be processed if 
    ///   1. it has an extension which is in the valid Extensions to be processed
    ///   2. is not prefixed or suffixed with a skip indicator (SkipSuffix & SkipPrefix as defined in configuration)
    ///   3. does not contain any of the SkipContain indicators as defined in configuration   
    ///   4. is not a thumbnail ("_thumbnails" folder is created in the same root folder as the pictures themselves, so they need to be excluded)
    ///   Note: from a definition staindpoint this should also check if a file was not already processed in a previous iteration but 
    ///         that is a state that will be handeled in the processor main logic so is not part of this function
    /// </summary> 
    public virtual bool ShouldProcessFile(FileData filePath)
    {
        if (ShouldSkipFile(filePath.FilePath)) return false;        
        if (Extensions != null && !Extensions.Contains(Path.GetExtension(filePath.FilePath).ToLowerInvariant())) return false;
        return true;
    }

    ///<summary>
    /// Definition: a file to be cleaned-up is a file that matches the following scenario
    /// Scenario: identify if we have a file or a folder that was renamed to now be skipped from being included in the destination and before was not
    /// (aka before ShouldProcessFile was true but now is false), 
    /// therefor we need to clean up the originals that previously were validly included in the destination and now should be excluded  
    /// </summary> 
    public virtual bool ShouldCleanFile(FileData filePath)
    {
        if (Extensions != null && !Extensions.Contains(Path.GetExtension(filePath.FilePath).ToLowerInvariant())) return false;  //don't attempt to clean non image types files
        if (_configuration.SkipContains.Any(skipPart => filePath.FilePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase))) return false; //don't attempt to clean files thar are in the SkipContains folders
        if (ShouldSkipFile(filePath.FilePath)) return true;  //ex: for files/folders renamed to now be skipped and before were not we need to clean up the originals
        return false;
    }

    public virtual async Task<int> OnFileCreated(FileData filePath, bool logIfCreated = false)
    {
        return 0;       
    }
    
    /// <summary>
    /// Used By periodic scan to process files.
    /// Basically any new file the processor found that needs to be processed which in the previous iteration was not there.
    /// In the first iteration all files are new therefor subject to be processed as dictated by the rules in ShouldProcessFile 
    /// </summary>
    public virtual async Task<int>  OnEnsureProcessFile(FileData filePath, bool logIfProcessed = false)
    {
        //OnEnsureProcessFile is basically an alias for a new file the processor found that needs to be processed 
        return await OnFileCreated(filePath, logIfProcessed);
    }
    
    public virtual async Task<int> OnFileDeleted(FileData filePath, bool logIfDeleted = false)
    {
        return 0;
    }

    public virtual async Task OnFileChanged(FileData filePath)
    {
        await Task.CompletedTask;;
    }

    public virtual async Task OnFileRenamed(FileData oldFilePath, FileData newFilePath,  bool newValid)
    {
        await Task.CompletedTask;
    }
     
    public virtual async Task<int> OnEnsureCleanupFile(FileData skipFilePath, bool logIfCleaned = false)
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
    
    protected async Task ExecuteWithRetryAttemptsAsync(FileData filePath, string callingMethod, Func<Task>callback)
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
