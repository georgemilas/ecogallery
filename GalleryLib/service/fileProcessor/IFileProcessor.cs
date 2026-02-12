namespace GalleryLib.service.fileProcessor;

public interface IFileProcessor
{
    DirectoryInfo RootFolder { get; }
    HashSet<string> Extensions { get; }         
    ///<summary>
    /// Definition: a file is to be processed if 
    ///   1. is a "valid" media file (it has an extension which is in the valid Extensions to be processed)
    ///   2. is not prefixed or suffixed with a skip indicator (SkipSuffix & SkipPrefix as defined in configuration)
    ///   3. does not contain any of the SkipContain indicators as defined in configuration   
    ///   4. it was not already processed in a previous iteration
    ///   5. is not a thumbnail ("_thumbnails" folder is created in the same root folder as the pictures themselves, so they need to be excluded)
    /// </summary> 
    public bool ShouldProcessFile(FileData fileData, bool logIfProcess = false);
    ///<summary>
    /// Definition: a file to be cleaned-up if a file that matches the following scenario
    /// Scenario: identify if we have a file or a folder that was renamed to now be skipped from being included in the destination and before was not
    /// (aka before ShouldProcessFile was true but now is false), 
    /// therefor we need to clean up the originals that previously were validly included in the destination and now should be excluded  
    /// </summary> 
    public bool ShouldCleanFile(FileData fileData, bool logIfProcess = false);

    Task<int> OnFileCreated(FileData fileData, bool logIfCreated = false);
    Task OnFileChanged(FileData fileData);
    Task<int> OnFileDeleted(FileData fileData, bool logIfDeleted = false);
    Task OnFileRenamed(FileData oldFileData, FileData newFileData, bool newValid);
    /// <summary>
    /// Used By periodic scan to process files as dictated by ShouldProcessFile
    /// </summary>
    Task<int> OnEnsureProcessFile(FileData fileData, bool logIfProcessed = false);
    
    ///<summary>
    /// This will get called during periodic scans to ensure any files that were previously included but now match skip criteria are cleaned up
    /// See ShouldCleanFile for the definition of files to be cleand up
    /// </summary> 
    Task<int> OnEnsureCleanupFile(FileData fileData, bool logIfCleaned = false);


    Task OnScanStart();
    Task OnScanEnd();
}