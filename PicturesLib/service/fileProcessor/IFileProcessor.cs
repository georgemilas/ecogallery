namespace PicturesLib.service.fileProcessor;

public interface IFileProcessor
{
    DirectoryInfo RootFolder { get; }
    HashSet<string> Extensions { get; }         
    bool ShouldSkipFile(string filePath);
    Task OnFileCreated(string filePath);
    Task OnFileChanged(string filePath);
    Task OnFileDeleted(string filePath);
    Task OnFileRenamed(string oldPath, string newPath, bool newValid);
    Task OnEnsureCleanup(string skipFilePath);
    Task OnScanStart(string skipFilePath);
    Task OnScanEnd(string skipFilePath);
}