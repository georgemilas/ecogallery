using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using PicturesLib.model.configuration;
using PicturesLib.service.fileProcessor;
using System.Text;
using FFMpegCore;

namespace PicturesLib.service.thumbnail;

/// <summary>
/// Factory for creating thumbnail processing strategies for FileObserverService
/// Process files from the pictures folder (picturesPath) and create thumbnails in picturesPath/_thumbnails/{height} folder
/// </summary>
public class ThumbnailProcessor : EmptyProcessor
{
    public ThumbnailProcessor(PicturesDataConfiguration configuration, int height = 300): base(configuration)
    {
        _height = height;        
    }

    private readonly int _height;
    protected readonly object _setLock = new();
    /// <summary>
    /// Process files from the pictures folder (picturesPath) and create thumbnails in picturesPath/_thumbnails/{height} folder
    /// </summary>
    public override DirectoryInfo RootFolder { get { return _configuration.RootFolder; } }
    protected virtual string thumbnailsBase { get { return Path.Combine(RootFolder.FullName, "_thumbnails"); } }
    protected virtual string thumbDir { get { return Path.Combine(thumbnailsBase, _height.ToString()); } }
    protected virtual string GetThumbnailPath(string sourceFilePath)
    {
        if (Path.GetExtension(sourceFilePath).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            sourceFilePath = Path.ChangeExtension(sourceFilePath, ".jpg");
        }
        return sourceFilePath.Replace(RootFolder.FullName, thumbDir);
    }
    
    public static FileObserverService CreateProcessor(PicturesDataConfiguration configuration, int height = 300, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new ThumbnailProcessor(configuration, height);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
    public static FileObserverServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, int height = 300)
    {
        IFileProcessor processor = new ThumbnailProcessor(configuration, height);
        return new FileObserverServiceNotParallel(processor,intervalMinutes: 2);
    }

    
    /// <summary>
    /// skip files already in thumbnails directory and as dictated by configuration 
    /// for example a folder named "skip_folderName" or "folderName_skip" or a file named "imageName_skip.jpg" or "skip_imageName.jpg"
    /// </summary>    
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
    public override async Task<int> OnFileCreated(string filePath)
    {
        string thumbPath = GetThumbnailPath(filePath);
        if (File.Exists(thumbPath)) return 0;

        await BuildThumbnailAsync(_height, filePath, thumbPath);
        //Console.WriteLine($"Created Thumbnail: {thumbPath}");
        return 1;
    }

    public override async Task OnFileChanged(string filePath)
    {
        string thumbPath = GetThumbnailPath(filePath);
        await BuildThumbnailAsync(_height, filePath, thumbPath);
        Console.WriteLine($"Updated Thumbnail: {thumbPath}");
    }

    public override async Task<int> OnFileDeleted(string filePath)
    {
        string thumbnailPath = GetThumbnailPath(filePath);
        return await CleanupThumbnail(thumbnailPath);
    }

    public override async Task OnFileRenamed(string oldPath, string newPath, bool newValid)
    {
        string oldThumbPath = GetThumbnailPath(oldPath);
        await CleanupThumbnail(oldThumbPath);

        if (newValid)
        {
            string newThumbPath = GetThumbnailPath(newPath);
            await BuildThumbnailAsync(_height, newPath, newThumbPath);
            Console.WriteLine($"Created new thumbnail: {newThumbPath}");
        }
    }

    public override async Task<int> OnEnsureCleanup(string skipFilePath)
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
            string thumbnailPath = GetThumbnailPath(Path.Combine(skipFolder, originalName));
            totalDeleted += await CleanupThumbnail(thumbnailPath);
        }

        if (fileNameEndsWith.Any())
        {
            //it's a suffix to a file
            var suffix = fileNameEndsWith.First();
            var originalName = skipFileName.Replace(suffix, string.Empty);
            string thumbnailPath = GetThumbnailPath(Path.Combine(skipFolder, originalName));
            totalDeleted += await CleanupThumbnail(thumbnailPath);
        }

        //Console.WriteLine($"Ensuring cleanup for folder contains suffixOrPrefix {suffixOrPrefix}: {skipFileName}");                
        if (folderContainsSuffix.Any())
        {
            //it's a suffix to a folder 
            var suffix = folderContainsSuffix.First();
            var originalPath = skipFolder.Replace(suffix, string.Empty);
            string thumbnailPath = GetThumbnailPath(Path.Combine(originalPath, skipFileName));
            totalDeleted += await CleanupThumbnail(thumbnailPath);
        }

        if (folderContainsPrefix.Any())
        {
            //it's a prefix to a folder 
            var prefix = folderContainsPrefix.First();
            var originalPath = skipFolder.Replace(prefix, string.Empty);
            string thumbnailPath = GetThumbnailPath(Path.Combine(originalPath, skipFileName));
            totalDeleted += await CleanupThumbnail(thumbnailPath);
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

    private async Task<int> CleanupThumbnail(string thumbnailPath)
    {
        if (File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
            //Console.WriteLine($"Deleted thumbnail: {thumbnailPath}");

            // Clean up empty directories
            var directory = Path.GetDirectoryName(thumbnailPath);
            if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory, recursive: true);
                //Console.WriteLine($"Deleted empty thumbnail directory: {directory}");
            }
            return 1;
        }
        return 0;
    }

    private async Task BuildThumbnailAsync(int height, string filePath, string thumbPath)
    {
        var thumbPathFolder = Path.GetDirectoryName(thumbPath);
        if (!string.IsNullOrEmpty(thumbPathFolder))
        {
            Directory.CreateDirectory(thumbPathFolder);
        }

        //FileSystemWatcher may have kicked this off before the file is fully written to disk (ex: a large file being copied), so we may get an Exception
        //therefor we retry with exponential backoff on failure to give time for the file to be fully written to disk and ready
        const int maxAttempts = 5;
        int attempt = 0;
        Exception? lastError = null;
        while (attempt < maxAttempts)
        {
            try
            {
                //Retry open in case the file is still being written   
                if (Path.GetExtension(filePath).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    await BuildVideoThumbnail(height, filePath, thumbPath);
                }
                else 
                {
                    await BuildImageThumbnail(height, filePath, thumbPath);
                }      
                return; // Success - exit immediately without Task.Delay
            }
            catch (IOException ex)
            {
                Console.WriteLine($"BuildThumbnailAsync IOException on attempt {attempt + 1} for file {filePath}: {ex.Message}");
                lastError = ex;                                
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"BuildThumbnailAsync UnauthorizedAccessException on attempt {attempt + 1} for file {filePath}: {ex.Message}");
                lastError = ex;                
            }

            attempt++;
            if (attempt < maxAttempts)
            {
                await Task.Delay(attempt switch { 1 => 100, 2 => 250, 3 => 500, 4 => 1000, _ => 1500 });
            }
        }

        
        if (lastError == null) 
        {
            return;
        }
        var lastErrorLocal = lastError;
        lastError = null;
        throw new Exception($"Failed to build thumbnail for file {filePath} after {maxAttempts} attempts.", lastErrorLocal);                
    }

    private async Task BuildImageThumbnail(int height, string filePath, string thumbPath)
    {
        using var image = await Image.LoadAsync(filePath);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(0, height),
            Mode = ResizeMode.Max
        }));
        await image.SaveAsync(thumbPath);        
    }

    private async Task BuildVideoThumbnail(int height, string filePath, string thumbPath)
    {
        //thumbPath = Path.ChangeExtension(thumbPath, ".jpg");    //already done in GetThumbnailPath
        await FFMpeg.SnapshotAsync(filePath, thumbPath, new System.Drawing.Size(-1, height), TimeSpan.Zero);  
        
        // // Get video info
        // var mediaInfo = await FFProbe.AnalyseAsync(filePath);
        // var duration = mediaInfo.Duration;

        // // Extract frame from middle
        // var midpoint = mediaInfo.Duration / 2;
        // await FFMpeg.SnapshotAsync(filePath, thumbPath, new System.Drawing.Size(-1, height), midpoint);
    }
}
