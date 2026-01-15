using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using System.Text;
using FFMpegCore;

namespace GalleryLib.service.thumbnail;

/// <summary>
/// Factory for creating thumbnail processing strategies for FileObserverService
/// Process files from the pictures folder (picturesPath) and create thumbnails in picturesPath/_thumbnails/{height} folder
/// </summary>
public class MultipleThumbnailsProcessor : EmptyProcessor
{
    public MultipleThumbnailsProcessor(PicturesDataConfiguration configuration, int[] heights): base(configuration)
    {
        _heights = heights;        
    }

    private readonly int[] _heights;
    protected readonly object _setLock = new();
    
    protected virtual string GetThumbnailPath(string sourceFilePath, int height)
    {
        return _configuration.GetThumbnailPath(sourceFilePath, height);
    }

    public static FileObserverService CreateProcessor(PicturesDataConfiguration configuration, int[] heights, int degreeOfParallelism = -1)
    {
        Console.WriteLine($"Running MultipleThumbnailsProcessor with heights=[{string.Join(", ", heights)}]");
        IFileProcessor processor = new MultipleThumbnailsProcessor(configuration, heights);
        return new FileObserverService(processor, intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
    public static FileObserverServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, int[] heights)
    {
        Console.WriteLine($"Running MultipleThumbnailsProcessor with heights=[{string.Join(", ", heights)}]");
        IFileProcessor processor = new MultipleThumbnailsProcessor(configuration, heights);
        return new FileObserverServiceNotParallel(processor, intervalMinutes: 2);
    }
    
    public override async Task<int> OnFileCreated(string filePath, bool logIfCreated = false)
    {
        bool created = false;
        
        // Check if any thumbnails need to be created
        var heightsToCreate = _heights.Where(h => !File.Exists(GetThumbnailPath(filePath, h))).ToArray();
        if (heightsToCreate.Length == 0) return 0;

        if (_configuration.IsMovieFile(filePath))
        {
            // Video files - process separately unlike images where we can load once and write multiple sizes
            foreach (var height in heightsToCreate)
            {
                string thumbPath = GetThumbnailPath(filePath, height);
                await BuildVideoThumbnailAsync(height, filePath, thumbPath, () =>
                {
                    if (logIfCreated)
                    {
                        Console.WriteLine($"Created Thumbnail for video/height {height}: {GetThumbnailPath(filePath, height)}");
                    }
                });
                created = true;
            }
        }
        else
        {
            await BuildAllImageThumbnailsAsync(heightsToCreate, filePath, (height) =>
            {
                if (logIfCreated)
                {
                    Console.WriteLine($"Created Thumbnail for image/height {height}: {GetThumbnailPath(filePath, height)}");
                }
            });
            created = true;
        }
        
        return created ? 1 : 0;
    }

    public override async Task OnFileChanged(string filePath)
    {
        if (_configuration.IsMovieFile(filePath))
        {
            // Video files - process separately
            foreach (var height in _heights)
            {
                string thumbPath = GetThumbnailPath(filePath, height);
                await BuildVideoThumbnailAsync(height, filePath, thumbPath, () => Console.WriteLine($"Updated Thumbnail: {thumbPath}"));                
            }
        }
        else
        {
            await BuildAllImageThumbnailsAsync(_heights, filePath, (height) => Console.WriteLine($"Updated Thumbnail: {GetThumbnailPath(filePath, height)}"));            
        }
    }

    public override async Task<int> OnFileDeleted(string filePath)
    {
        bool deleted = false;
        foreach (var height in _heights)
        {
            string thumbPath = GetThumbnailPath(filePath, height);
            int result = await CleanupThumbnail(thumbPath, true);
            if (result > 0) deleted = true;
        }   
        return deleted ? 1 : 0;
    }

    public override async Task OnFileRenamed(string oldPath, string newPath, bool newValid)
    {
        foreach (var height in _heights)
        {
            string oldThumbPath = GetThumbnailPath(oldPath, height);
            await CleanupThumbnail(oldThumbPath, true);            
        }

        if (newValid)
        {
            if (_configuration.IsMovieFile(newPath))
            {
                // Video files - process separately
                foreach (var height in _heights)
                {
                    string thumbPath = GetThumbnailPath(newPath, height);
                    await BuildVideoThumbnailAsync(height, newPath, thumbPath, () => Console.WriteLine($"Created new thumbnail: {thumbPath}"));                
                }
            }
            else
            {
                await BuildAllImageThumbnailsAsync(_heights, newPath, (height) => Console.WriteLine($"Created new thumbnail: {GetThumbnailPath(newPath, height)}"));            
            }
            
        }
        
    }

    public override async Task<int> OnEnsureCleanup(string skipFilePath, bool logIfCleaned = false)
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


        async Task<int> attemptClenup(string f, string n, int[] heights) 
        {
            bool deleted = false;
            foreach (var height in heights) 
            {
                string thumbnailPath = GetThumbnailPath(Path.Combine(f, n), height);
                int result = await CleanupThumbnail(thumbnailPath, logIfCleaned);
                if (result > 0) deleted = true;
            }   
            return deleted ? 1 : 0 ;                    
        };

        int totalDeleted = 0;
        if (fileNameStartWith.Any())
        {
            //it's a prefix to a file 
            var prefix = fileNameStartWith.First();
            var originalName = skipFileName.Replace(prefix, string.Empty);
            totalDeleted += await attemptClenup(skipFolder, originalName, _heights);                    
        }

        if (fileNameEndsWith.Any())
        {
            //it's a suffix to a file
            var suffix = fileNameEndsWith.First();
            var originalName = skipFileName.Replace(suffix, string.Empty);
            totalDeleted += await attemptClenup(skipFolder, originalName, _heights);                                
        }

        //Console.WriteLine($"Ensuring cleanup for folder contains suffixOrPrefix {suffixOrPrefix}: {skipFileName}");                
        if (folderContainsSuffix.Any())
        {
            //it's a suffix to a folder 
            var suffix = folderContainsSuffix.First();
            var originalPath = skipFolder.Replace(suffix, string.Empty);
            totalDeleted += await attemptClenup(originalPath, skipFileName, _heights);              
        }

        if (folderContainsPrefix.Any())
        {
            //it's a prefix to a folder 
            var prefix = folderContainsPrefix.First();
            var originalPath = skipFolder.Replace(prefix, string.Empty);
            totalDeleted += await attemptClenup(originalPath, skipFileName, _heights);
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

    private async Task<int> CleanupThumbnail(string thumbnailPath, bool logIfCleaned = false)
    {
        if (File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
            if (logIfCleaned)
            {
                Console.WriteLine($"Deleted thumbnail: {thumbnailPath}");
            }

            // Clean up empty directories
            var directory = Path.GetDirectoryName(thumbnailPath);
            if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory, recursive: true);
                if (logIfCleaned)
                {
                    Console.WriteLine($"Deleted empty thumbnail directory: {directory}");
                }
            }
            return 1;
        }
        return 0;
    }
    
    private async Task BuildVideoThumbnailAsync(int height, string filePath, string thumbPath, Action onThumbnailCreated)
    {        
        await ExecuteWithRetryAttemptsAsync(filePath, "BuildVideoThumbnailAsync", async () => 
        {
            var thumbPathFolder = Path.GetDirectoryName(thumbPath);
            if (!string.IsNullOrEmpty(thumbPathFolder))
            {
                Directory.CreateDirectory(thumbPathFolder);
            }

            await FFMpeg.SnapshotAsync(filePath, thumbPath, new System.Drawing.Size(-1, height), TimeSpan.Zero);  
                        
            // // Extract frame from middle
            // var midpoint = mediaInfo.Duration / 2;
            // await FFMpeg.SnapshotAsync(filePath, thumbPath, new System.Drawing.Size(-1, height), midpoint);      
            onThumbnailCreated();
        });        
    }


    private async Task BuildAllImageThumbnailsAsync(int[] heights, string filePath, Action<int> onThumbnailCreated)
    {
        await ExecuteWithRetryAttemptsAsync(filePath, "BuildAllImageThumbnailsAsync", async () => 
        {
            // Load original image once
            using var originalImage = await Image.LoadAsync(filePath);
            
            foreach (var height in heights)
            {
                string thumbPath = GetThumbnailPath(filePath, height);
                var thumbPathFolder = Path.GetDirectoryName(thumbPath);
                if (!string.IsNullOrEmpty(thumbPathFolder))
                {
                    Directory.CreateDirectory(thumbPathFolder);
                }

                //Clone the original for each size (preserves quality and faster than re-loading from disk)
                using var imageClone = originalImage.Clone(ctx => {
                    if (originalImage.Height > height)
                    {
                        ctx.Resize(new ResizeOptions
                        {
                            Size = new Size(0, height),
                            Mode = ResizeMode.Max
                        });
                    }
                });
                
                await imageClone.SaveAsync(thumbPath);
                onThumbnailCreated(height);
            }
        });        
    }    

}
