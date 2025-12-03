using PicturesLib.model.configuration;
using PicturesLib.model.album;
using PicturesLib.service.database;
using PicturesLib.service.fileProcessor;
using PicturesLib.repository;

namespace PicturesLib.service.album;

/// <summary>
/// 
/// </summary>
public class AlbumProcessor: EmptyProcessor
{

    public AlbumProcessor(PicturesDataConfiguration configuration):base(configuration)
    {
        imageRepository = new AlbumImageRepository(configuration);
        albumRepository = new AlbumRepository(configuration);
    }
    private AlbumImageRepository imageRepository; 
    private AlbumRepository albumRepository;

    public override DirectoryInfo RootFolder { get { return _configuration.RootFolder; } }
    protected virtual string thumbnailsBase { get { return Path.Combine(RootFolder.FullName, "_thumbnails"); } }

    /// <summary>
    /// create image record and ensure album record exists
    /// </summary>
    private async Task CreateImageAndAlbumRecords(string filePath)
    {
        if (!await imageRepository.AlbumImageExistsAsync(filePath))
        {
            await albumRepository.EnsureAlbumExistsAsync(filePath);
            await imageRepository.AddNewImageAsync(filePath);
        }
    }

    /// <summary>
    /// delete image record and if album is empty delete album records recursively as well 
    /// </summary>
    private async Task CleanupImageAndAlbumRecords(string filePath)
    {
        if (await imageRepository.DeleteAlbumImageAsync(filePath) > 0 && !await albumRepository.AlbumHasContentAsync(filePath))
        {
            await albumRepository.DeleteAlbumAsync(filePath);
        }
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

    
    public override async Task OnFileCreated(string filePath)
    {
        await CreateImageAndAlbumRecords(filePath);
    }

    public override async Task OnFileChanged(string filePath)
    {
        await CreateImageAndAlbumRecords(filePath);
    }

    public override async Task OnFileDeleted(string filePath)
    {
        await CleanupImageAndAlbumRecords(filePath);
    }
    
    public override async Task OnFileRenamed(string oldPath, string newPath,  bool newValid)
    {
        await CleanupImageAndAlbumRecords(oldPath);
        if (newValid)
        {
            await CreateImageAndAlbumRecords(newPath);            
        }
    }

    ///<summary>
    /// need to find the original file path from the skip file path and ensure its records are deleted
    /// </summary> 
    public override async Task OnEnsureCleanup(string skipFilePath)
    {
        string skipFolder = Path.GetDirectoryName(skipFilePath) ?? string.Empty;
        string skipFileName = Path.GetFileName(skipFilePath);

        //avoid recursion into created thumbnails
        if (skipFilePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase)) return;

        //identify the type of prefix or suffix we are dealing with 
        var fileNameStartWith = _configuration.SkipPrefix.Where(prefix => skipFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        var fileNameEndsWith = _configuration.SkipSuffix.Where(suffix => skipFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        //using contains instead of startwith or endwith to cactch not just the last folder but any parent folder being affected as well
        var folderContainsPrefix = _configuration.SkipPrefix.Where(prefix => skipFolder.Contains(prefix, StringComparison.OrdinalIgnoreCase));
        var folderContainsSuffix = _configuration.SkipSuffix.Where(suffix => skipFolder.Contains(suffix, StringComparison.OrdinalIgnoreCase));
        var filePathContains = _configuration.SkipContains.Where(skipPart => skipFilePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));

        if (fileNameStartWith.Any())
        {
            //it's a prefix to a file 
            var prefix = fileNameStartWith.First();
            var originalName = skipFileName.Replace(prefix, string.Empty);
            string originalPath = Path.Combine(skipFolder, originalName);
            await CleanupImageAndAlbumRecords(originalPath);
        }

        if (fileNameEndsWith.Any())
        {
            //it's a suffix to a file
            var suffix = fileNameEndsWith.First();
            var originalName = skipFileName.Replace(suffix, string.Empty);
            string originalPath = Path.Combine(skipFolder, originalName);
            await CleanupImageAndAlbumRecords(originalPath);
        }

        //Console.WriteLine($"Ensuring cleanup for folder contains suffixOrPrefix {suffixOrPrefix}: {skipFileName}");                
        if (folderContainsSuffix.Any())
        {
            //it's a suffix to a folder 
            var suffix = folderContainsSuffix.First();
            var originalFolderPath = skipFolder.Replace(suffix, string.Empty);
            string originalPath = Path.Combine(originalFolderPath, skipFileName);
            await CleanupImageAndAlbumRecords(originalPath);
        }

        if (folderContainsPrefix.Any())
        {
            //it's a prefix to a folder 
            var prefix = folderContainsPrefix.First();
            var originalFolderPath = skipFolder.Replace(prefix, string.Empty);
            string originalPath = Path.Combine(originalFolderPath, skipFileName);
            await CleanupImageAndAlbumRecords(originalPath);
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
    }

    public override async Task OnScanStart(string skipFilePath)
    {
        //only keep repositories open during the scan
        await imageRepository.DisposeAsync(); 
        await albumRepository.DisposeAsync(); 
        imageRepository = new AlbumImageRepository(_configuration);
        albumRepository = new AlbumRepository(_configuration);        
    }
    public override async Task OnScanEnd(string skipFilePath)
    {
        await imageRepository.DisposeAsync(); 
        await albumRepository.DisposeAsync();
    }         
    
}
