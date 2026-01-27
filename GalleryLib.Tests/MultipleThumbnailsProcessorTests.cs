using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using GalleryLib.service.thumbnail;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for MultipleThumbnailsProcessor - thumbnail generation
/// </summary>
public class MultipleThumbnailsProcessorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PicturesDataConfiguration _config;
    private readonly MultipleThumbnailsProcessor _processor;
    private readonly int[] _heights = { 400, 1440 };

    public MultipleThumbnailsProcessorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _config = new PicturesDataConfiguration
        {
            Folder = _tempDir,
            ImageExtensions = new List<string> { ".jpg", ".jpeg", ".png" },
            MovieExtensions = new List<string> { ".mp4" },
            SkipSuffix = new List<string> { "_skip" },
            SkipPrefix = new List<string> { "skip_" },
            SkipContains = new List<string> { "_thumbnails" }
        };

        _processor = new MultipleThumbnailsProcessor(_config, _heights);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Helper Methods

    private async Task<string> CreateTestImageAsync(string relativePath, int width, int height)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var image = new Image<Rgba32>(width, height, new Rgba32(255, 0, 0));
        await image.SaveAsJpegAsync(fullPath);
        return fullPath;
    }

    private async Task<string> CreateTestPngAsync(string relativePath, int width, int height)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var image = new Image<Rgba32>(width, height, new Rgba32(0, 255, 0));
        await image.SaveAsPngAsync(fullPath);
        return fullPath;
    }

    #endregion

    #region OnFileCreated Tests

    [Fact]
    public async Task OnFileCreated_CreatesThumbnailsForAllHeights()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 2000, 1500);
        var fileData = new FileData(imagePath, imagePath);

        var result = await _processor.OnFileCreated(fileData, logIfCreated: false);

        Assert.Equal(1, result);
        foreach (var height in _heights)
        {
            var thumbPath = _config.GetThumbnailPath(imagePath, height);
            Assert.True(File.Exists(thumbPath), $"Thumbnail at height {height} should exist");
        }
    }

    [Fact]
    public async Task OnFileCreated_ThumbnailsHaveCorrectHeight()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 2000, 1500);
        var fileData = new FileData(imagePath, imagePath);

        await _processor.OnFileCreated(fileData);

        var thumb400Path = _config.GetThumbnailPath(imagePath, 400);
        using var thumb400 = await Image.LoadAsync(thumb400Path);
        Assert.Equal(400, thumb400.Height);
    }

    [Fact]
    public async Task OnFileCreated_PreservesAspectRatio()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 2000, 1000); // 2:1 aspect ratio
        var fileData = new FileData(imagePath, imagePath);

        await _processor.OnFileCreated(fileData);

        var thumbPath = _config.GetThumbnailPath(imagePath, 400);
        using var thumb = await Image.LoadAsync(thumbPath);

        // Height should be 400, width should be ~800 (2:1 ratio)
        Assert.Equal(400, thumb.Height);
        Assert.True(thumb.Width >= 790 && thumb.Width <= 810, $"Width should be ~800, was {thumb.Width}");
    }

    [Fact]
    public async Task OnFileCreated_SmallImage_DoesNotUpscale()
    {
        // Create image smaller than target thumbnail height
        var imagePath = await CreateTestImageAsync("album/small.jpg", 300, 200);
        var fileData = new FileData(imagePath, imagePath);

        await _processor.OnFileCreated(fileData);

        var thumb400Path = _config.GetThumbnailPath(imagePath, 400);
        using var thumb = await Image.LoadAsync(thumb400Path);

        // Should not upscale - height should remain 200
        Assert.Equal(200, thumb.Height);
    }

    [Fact]
    public async Task OnFileCreated_ExistingThumbnails_SkipsCreation()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 2000, 1500);
        var fileData = new FileData(imagePath, imagePath);

        // First creation
        await _processor.OnFileCreated(fileData);
        var thumb400Path = _config.GetThumbnailPath(imagePath, 400);
        var originalModTime = File.GetLastWriteTime(thumb400Path);

        // Wait a bit and try again
        await Task.Delay(100);
        var result = await _processor.OnFileCreated(fileData);

        // Should skip since thumbnails exist
        Assert.Equal(0, result);
        Assert.Equal(originalModTime, File.GetLastWriteTime(thumb400Path));
    }

    [Fact]
    public async Task OnFileCreated_CreatesThumbnailDirectories()
    {
        var imagePath = await CreateTestImageAsync("nested/album/photo.jpg", 1000, 800);
        var fileData = new FileData(imagePath, imagePath);

        await _processor.OnFileCreated(fileData);

        foreach (var height in _heights)
        {
            var thumbPath = _config.GetThumbnailPath(imagePath, height);
            var thumbDir = Path.GetDirectoryName(thumbPath);
            Assert.True(Directory.Exists(thumbDir), $"Thumbnail directory should be created for height {height}");
        }
    }

    [Fact]
    public async Task OnFileCreated_PngImage_CreatesThumbnails()
    {
        var imagePath = await CreateTestPngAsync("album/photo.png", 1000, 800);
        var fileData = new FileData(imagePath, imagePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        foreach (var height in _heights)
        {
            var thumbPath = _config.GetThumbnailPath(imagePath, height);
            Assert.True(File.Exists(thumbPath));
        }
    }

    #endregion

    #region OnFileDeleted Tests

    [Fact]
    public async Task OnFileDeleted_RemovesThumbnails()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 1000, 800);
        var fileData = new FileData(imagePath, imagePath);

        // Create thumbnails first
        await _processor.OnFileCreated(fileData);
        foreach (var height in _heights)
        {
            Assert.True(File.Exists(_config.GetThumbnailPath(imagePath, height)));
        }

        // Delete the source file and call OnFileDeleted
        File.Delete(imagePath);
        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(1, result);
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(imagePath, height)));
        }
    }

    [Fact]
    public async Task OnFileDeleted_NoThumbnails_ReturnsZero()
    {
        var imagePath = Path.Combine(_tempDir, "album/nonexistent.jpg");
        var fileData = new FileData(imagePath, imagePath);

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnFileDeleted_CleansEmptyDirectories()
    {
        var imagePath = await CreateTestImageAsync("deep/nested/album/photo.jpg", 1000, 800);
        var fileData = new FileData(imagePath, imagePath);

        await _processor.OnFileCreated(fileData);

        // Verify thumbnail directory exists
        var thumbDir = Path.GetDirectoryName(_config.GetThumbnailPath(imagePath, 400));
        Assert.True(Directory.Exists(thumbDir));

        // Delete
        await _processor.OnFileDeleted(fileData);

        // Empty directories should be cleaned up
        Assert.False(Directory.Exists(thumbDir));
    }

    #endregion

    #region OnFileChanged Tests

    [Fact]
    public async Task OnFileChanged_RegeneratesThumbnails()
    {
        var imagePath = await CreateTestImageAsync("album/photo.jpg", 1000, 800);
        var fileData = new FileData(imagePath, imagePath);

        // Create initial thumbnails
        await _processor.OnFileCreated(fileData);
        var thumb400Path = _config.GetThumbnailPath(imagePath, 400);
        var originalContent = await File.ReadAllBytesAsync(thumb400Path);

        // Modify the source image
        await Task.Delay(100);
        using (var newImage = new Image<Rgba32>(1000, 800, new Rgba32(0, 0, 255))) // Blue instead of red
        {
            await newImage.SaveAsJpegAsync(imagePath);
        }

        // Trigger change
        await _processor.OnFileChanged(fileData);

        var newContent = await File.ReadAllBytesAsync(thumb400Path);
        Assert.NotEqual(originalContent, newContent);
    }

    #endregion

    #region OnFileRenamed Tests

    [Fact]
    public async Task OnFileRenamed_MovesToNewLocation()
    {
        var oldPath = await CreateTestImageAsync("album/old.jpg", 1000, 800);
        var newPath = Path.Combine(_tempDir, "album/new.jpg");
        var oldFileData = new FileData(oldPath, oldPath);

        // Create thumbnails for old path
        await _processor.OnFileCreated(oldFileData);

        // Rename the file
        File.Move(oldPath, newPath);
        var newFileData = new FileData(newPath, newPath);

        await _processor.OnFileRenamed(oldFileData, newFileData, true);

        // Old thumbnails should be gone
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(oldPath, height)));
        }

        // New thumbnails should exist
        foreach (var height in _heights)
        {
            Assert.True(File.Exists(_config.GetThumbnailPath(newPath, height)));
        }
    }

    [Fact]
    public async Task OnFileRenamed_ToSkippedName_OnlyDeletes()
    {
        var oldPath = await CreateTestImageAsync("album/photo.jpg", 1000, 800);
        var newPath = Path.Combine(_tempDir, "album/skip_photo.jpg");
        var oldFileData = new FileData(oldPath, oldPath);

        await _processor.OnFileCreated(oldFileData);

        File.Move(oldPath, newPath);
        var newFileData = new FileData(newPath, newPath);

        // newValid = false because new name has skip prefix
        await _processor.OnFileRenamed(oldFileData, newFileData, false);

        // Old thumbnails should be gone
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(oldPath, height)));
        }

        // New thumbnails should NOT be created
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(newPath, height)));
        }
    }

    #endregion

    #region ShouldProcessFile Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("video.mp4", true)]
    [InlineData("document.txt", false)]
    [InlineData("skip_photo.jpg", false)]  // Prefix match: fileName starts with "skip_"
    [InlineData("photo_skip.jpg", false)]  // Suffix match: filename (without ext) ends with "_skip"
    public void ShouldProcessFile_CorrectlyFilters(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, "album", fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.Equal(expected, _processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_FileInSkipSuffixFolder_ReturnsFalse()
    {
        // Folder suffix is matched via folder.Contains check
        var filePath = Path.Combine(_tempDir, "album_skip", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    #endregion

    #region OnEnsureCleanupFile Tests

    [Fact]
    public async Task OnEnsureCleanupFile_FileWithSkipPrefix_CleansOriginal()
    {
        // First create thumbnails for "photo.jpg"
        var originalPath = await CreateTestImageAsync("album/photo.jpg", 1000, 800);
        var originalFileData = new FileData(originalPath, originalPath);
        await _processor.OnFileCreated(originalFileData);

        // Verify thumbnails exist
        foreach (var height in _heights)
        {
            Assert.True(File.Exists(_config.GetThumbnailPath(originalPath, height)));
        }

        // Now simulate renaming to skip_photo.jpg
        var skipPath = Path.Combine(_tempDir, "album/skip_photo.jpg");
        File.Move(originalPath, skipPath);
        var skipFileData = new FileData(skipPath, skipPath);

        // This should clean up the original thumbnails
        var result = await _processor.OnEnsureCleanupFile(skipFileData, logIfCleaned: true);

        Assert.Equal(1, result);
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(originalPath, height)));
        }
    }

    [Fact]
    public async Task OnEnsureCleanupFile_FolderWithSkipSuffix_CleansOriginal()
    {
        // Create thumbnails for file in "album" folder
        var originalPath = await CreateTestImageAsync("album/photo.jpg", 1000, 800);
        var originalFileData = new FileData(originalPath, originalPath);
        await _processor.OnFileCreated(originalFileData);

        // Simulate folder rename to "album_skip"
        var skipFolderPath = Path.Combine(_tempDir, "album_skip");
        Directory.Move(Path.Combine(_tempDir, "album"), skipFolderPath);

        var skipFilePath = Path.Combine(skipFolderPath, "photo.jpg");
        var skipFileData = new FileData(skipFilePath, skipFilePath);

        var result = await _processor.OnEnsureCleanupFile(skipFileData);

        Assert.Equal(1, result);
        foreach (var height in _heights)
        {
            Assert.False(File.Exists(_config.GetThumbnailPath(originalPath, height)));
        }
    }

    #endregion
}
