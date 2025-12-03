namespace PicturesLib.service.fileProcessor;

public interface IFileProcessor
{
    DirectoryInfo RootFolder { get; }
    HashSet<string> Extensions { get; }         
    bool ShouldSkipFile(string filePath);
    Task<int> OnFileCreated(string filePath);
    Task OnFileChanged(string filePath);
    Task<int> OnFileDeleted(string filePath);
    Task OnFileRenamed(string oldPath, string newPath, bool newValid);

    ///<summary>
    /// need to find the original file path from the skip file path and ensure its records are deleted
    /// This will get called during periodic scans to ensure any files that were previously included but now match skip criteria are cleaned up
    /// </summary> 
    Task<int> OnEnsureCleanup(string skipFilePath);

    Task OnScanStart(string skipFilePath);
    Task OnScanEnd(string skipFilePath);
}