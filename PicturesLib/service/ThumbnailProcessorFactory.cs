using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PicturesLib.service;

/// <summary>
/// Factory for creating thumbnail processing strategies for FileObserverService
/// </summary>
public static class ThumbnailProcessorFactory
{
    public static FileObserverService CreateThumbnailProcessor(DirectoryInfo rootFolder, int height = 300)
    {
        // Base thumbnails directory (without height) used for skip filtering
        string thumbnailsBase = Path.Combine(rootFolder.FullName, "_thumbnails");
        string thumbDir = Path.Combine(thumbnailsBase, height.ToString());
        string GetThumbnailPath(string sourceFilePath) => sourceFilePath.Replace(rootFolder.FullName, thumbDir);
        //skip files already in thumbnails directory
        Func<string, bool> shouldSkipFile = (filePath) => filePath.StartsWith(thumbnailsBase, StringComparison.OrdinalIgnoreCase);

        Func<string, Task> onFileCreated = async (filePath) =>
        {
            string thumbPath = GetThumbnailPath(filePath);            
            if (File.Exists(thumbPath)) return;
            
            await BuildThumbnailAsync(height, filePath, thumbPath);            
            Console.WriteLine($"Created Thumbnail: {thumbPath}");
        };

        Func<string, Task> onFileChanged = async (filePath) =>
        {
            string thumbPath = GetThumbnailPath(filePath);
            await BuildThumbnailAsync(height, filePath, thumbPath);
            Console.WriteLine($"Updated Thumbnail: {thumbPath}");
        };

        
        Func<string, Task> onFileDeleted = (filePath) =>
        {
            string thumbnailPath = GetThumbnailPath(filePath);
            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
                Console.WriteLine($"Deleted thumbnail: {thumbnailPath}");
                
                // Clean up empty directories
                var directory = Path.GetDirectoryName(thumbnailPath);
                if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, recursive: true);
                    Console.WriteLine($"Deleted empty thumbnail directory: {directory}");
                }
            }
            return Task.CompletedTask;
        };

        Func<string, string, Task> onFileRenamed = async (oldPath, newPath) =>
        {
            string oldThumbPath = GetThumbnailPath(oldPath);
            if (File.Exists(oldThumbPath))
            {
                File.Delete(oldThumbPath);
                Console.WriteLine($"Deleted old thumbnail: {oldThumbPath}");
            }
            
            string newThumbPath = GetThumbnailPath(newPath);
            await BuildThumbnailAsync(height, newPath, newThumbPath);
            Console.WriteLine($"Created new thumbnail: {newThumbPath}");
        };

        HashSet<string> extensions = new() { ".jpg", ".jpeg", ".png", ".webp" };
        
        return new FileObserverService(
            rootFolder, 
            shouldSkipFile, 
            onFileCreated, 
            onFileChanged, 
            onFileDeleted, 
            onFileRenamed, 
            extensions, 
            intervalMinutes: 2);
    }

    private static async Task BuildThumbnailAsync(int height, string filePath, string thumbPath)
    {
        var thumbPathFolder = Path.GetDirectoryName(thumbPath);
        if (!string.IsNullOrEmpty(thumbPathFolder))
        {
            Directory.CreateDirectory(thumbPathFolder);
        }

        // Retry open in case the file is still being written
        const int maxAttempts = 5;
        int attempt = 0;
        Exception? lastError = null;
        while (attempt < maxAttempts)
        {
            try
            {
                //FileSystemWatcher may have kicked this off before the file is fully written to disk (ex: a large file being copied), so we may get an Exception
                //therefor we retry with exponential backoff on failure to give time for the file to be fully written to disk and ready
                using var image = await Image.LoadAsync(filePath);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(0, height),
                    Mode = ResizeMode.Max
                }));
                await image.SaveAsync(thumbPath);
                return;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            attempt++;
            await Task.Delay(attempt switch { 0 => 100, 1 => 250, 2 => 500, 3 => 1000, _ => 1500 });
        }

        throw lastError ?? new IOException("Failed to build thumbnail due to unknown error.");
    }
}
