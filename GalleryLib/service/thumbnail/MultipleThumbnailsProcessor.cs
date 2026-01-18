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

    
    public override async Task<int> OnFileCreated(FileData filePath, bool logIfCreated = false)
    {
        bool created = false;
        
        // Check if any thumbnails need to be created
        var heightsToCreate = _heights.Where(h => !File.Exists(GetThumbnailPath(filePath.FilePath, h))).ToArray();
        if (heightsToCreate.Length == 0) return 0;

        if (_configuration.IsMovieFile(filePath.FilePath))
        {
            // Video files - process separately unlike images where we can load once and write multiple sizes
            foreach (var height in heightsToCreate)
            {
                string thumbPath = GetThumbnailPath(filePath.FilePath, height);
                await BuildVideoThumbnailAsync(height, filePath, thumbPath, () =>
                {
                    if (logIfCreated)
                    {
                        Console.WriteLine($"Created Thumbnail for video/height {height}: {GetThumbnailPath(filePath.FilePath, height)}");
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
                    Console.WriteLine($"Created Thumbnail for image/height {height}: {GetThumbnailPath(filePath.FilePath, height)}");
                }
            });
            created = true;
        }
        
        return created ? 1 : 0;
    }

    public override async Task OnFileChanged(FileData filePath)
    {
        if (_configuration.IsMovieFile(filePath.FilePath))
        {
            // Video files - process separately
            foreach (var height in _heights)
            {
                string thumbPath = GetThumbnailPath(filePath.FilePath, height);
                await BuildVideoThumbnailAsync(height, filePath, thumbPath, () => Console.WriteLine($"Updated Thumbnail: {thumbPath}"));                
            }
        }
        else
        {
            await BuildAllImageThumbnailsAsync(_heights, filePath, (height) => Console.WriteLine($"Updated Thumbnail: {GetThumbnailPath(filePath.FilePath, height)}"));            
        }
    }

    public override async Task<int> OnFileDeleted(FileData filePath, bool logIfDeleted = false)
    {
        bool deleted = false;
        foreach (var height in _heights)
        {
            string thumbPath = GetThumbnailPath(filePath.FilePath, height);
            int result = await CleanupThumbnail(thumbPath, true);
            if (result > 0) deleted = true;
        }   
        return deleted ? 1 : 0;
    }

    public override async Task OnFileRenamed(FileData oldPath, FileData newPath, bool newValid)
    {
        foreach (var height in _heights)
        {
            string oldThumbPath = GetThumbnailPath(oldPath.FilePath, height);
            await CleanupThumbnail(oldThumbPath, true);            
        }

        if (newValid)
        {
            if (_configuration.IsMovieFile(newPath.FilePath))
            {
                // Video files - process separately
                foreach (var height in _heights)
                {
                    string thumbPath = GetThumbnailPath(newPath.FilePath, height);
                    await BuildVideoThumbnailAsync(height, newPath, thumbPath, () => Console.WriteLine($"Created new thumbnail: {thumbPath}"));                
                }
            }
            else
            {
                await BuildAllImageThumbnailsAsync(_heights, newPath, (height) => Console.WriteLine($"Created new thumbnail: {GetThumbnailPath(newPath.FilePath, height)}"));            
            }
            
        }
        
    }

    public override async Task<int> OnEnsureCleanupFile(FileData skipFilePath, bool logIfCleaned = false)
    {
        string skipFolder = Path.GetDirectoryName(skipFilePath.FilePath) ?? string.Empty;
        string skipFileName = Path.GetFileName(skipFilePath.FilePath);

        //avoid recursion into created thumbnails
        if (skipFilePath.FilePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase)) return 0;

        //identify the type of prefix or suffix we are dealing with 
        var fileNameStartWith = _configuration.SkipPrefix.Where(prefix => skipFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        var fileNameEndsWith = _configuration.SkipSuffix.Where(suffix => skipFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        //using contains instead of startwith or endwith to cactch not just the last folder but any parent folder being affected as well
        var folderContainsPrefix = _configuration.SkipPrefix.Where(prefix => skipFolder.Contains(prefix, StringComparison.OrdinalIgnoreCase));
        var folderContainsSuffix = _configuration.SkipSuffix.Where(suffix => skipFolder.Contains(suffix, StringComparison.OrdinalIgnoreCase));
        var filePathContains = _configuration.SkipContains.Where(skipPart => skipFilePath.FilePath.Contains(skipPart, StringComparison.OrdinalIgnoreCase));

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
    
    private async Task BuildVideoThumbnailAsync(int height, FileData filePath, string thumbPath, Action onThumbnailCreated)
    {        
        await ExecuteWithRetryAttemptsAsync(filePath, "BuildVideoThumbnailAsync", async () => 
        {
            var thumbPathFolder = Path.GetDirectoryName(thumbPath);
            if (!string.IsNullOrEmpty(thumbPathFolder))
            {
                Directory.CreateDirectory(thumbPathFolder);
            }

            // Get video info to determine actual dimensions considering rotation
            var videoInfo = await FFProbe.AnalyseAsync(filePath.FilePath);
            var videoStream = videoInfo.VideoStreams.FirstOrDefault();
            if (videoStream != null)
            {
                // var rotation = videoStream.Rotation; // Keep the sign - direction matters!                
                // if (Math.Abs(rotation) == 180)  // Ignore 180/-180 rotations (often incorrect metadata) but respect 90/270 degree rotations
                // {
                //     rotation = 0; 
                // }
                
                //Use actual video dimensions without rotation adjustments
                var actualVideoHeight = videoStream.Height; // Use original height
                // var actualVideoHeight = (Math.Abs(rotation) == 90 || Math.Abs(rotation) == 270) 
                //     ? videoStream.Width    // Rotated 90/270 degrees - width becomes height
                //     : videoStream.Height;  // No rotation or 180 degrees - height stays height
                
                // Use min to ensure thumbnail is not bigger than the actual video
                var thumbnailHeight = Math.Min(height, actualVideoHeight);
                
                var rotationFilter = "";                
                // var rotationFilter = rotation switch
                // {
                //     90 => "transpose=1",       // Rotate 90° clockwise
                //     -90 => "transpose=2",      // Rotate 90° counter-clockwise  
                //     270 => "transpose=2",      // Rotate 270° clockwise = 90° counter-clockwise
                //     -270 => "transpose=1",     // Rotate 270° counter-clockwise = 90° clockwise
                //     _ => ""                    // No rotation needed (includes 0, 180, -180)
                // };

                //rotate first and then scale based on height to preserve intended viewing aspect ratio
                var filterChain = string.IsNullOrEmpty(rotationFilter) 
                    ? $"scale=-2:{thumbnailHeight}" 
                    : $"{rotationFilter},scale=-2:{thumbnailHeight}";
                
                await FFMpegArguments.FromFileInput(filePath.FilePath)
                                    .OutputToFile(thumbPath, true, options => options
                                        .WithCustomArgument($"-vf \"{filterChain}\"")
                                        .WithFrameOutputCount(1)
                                        .Seek(TimeSpan.Zero))
                                    .ProcessAsynchronously();
            }
            else
            {
                // Fallback if we can't get video info - use scale filter to maintain aspect ratio
                await FFMpegArguments.FromFileInput(filePath.FilePath)
                                    .OutputToFile(thumbPath, true, options => options
                                        .WithCustomArgument($"-vf \"scale=-2:{height}\"")
                                        .WithFrameOutputCount(1)
                                        .Seek(TimeSpan.Zero))
                                    .ProcessAsynchronously();
            }
            
            onThumbnailCreated();
        });        
    }


    private async Task BuildAllImageThumbnailsAsync(int[] heights, FileData filePath, Action<int> onThumbnailCreated)
    {
        await ExecuteWithRetryAttemptsAsync(filePath, "BuildAllImageThumbnailsAsync", async () => 
        {
            // Load original image once
            using var originalImage = await Image.LoadAsync(filePath.FilePath);
            
            foreach (var height in heights)
            {
                string thumbPath = GetThumbnailPath(filePath.FilePath, height);
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
