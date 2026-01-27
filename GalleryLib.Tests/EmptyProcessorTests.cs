using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for EmptyProcessor - the base file processor implementation
/// </summary>
public class EmptyProcessorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PicturesDataConfiguration _config;
    private readonly EmptyProcessor _processor;

    public EmptyProcessorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _config = new PicturesDataConfiguration
        {
            Folder = _tempDir,
            ImageExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".webp" },
            MovieExtensions = new List<string> { ".mp4", ".mov" },
            SkipSuffix = new List<string> { "_skip", "_pss" },
            SkipPrefix = new List<string> { "skip_", "pss_" },
            SkipContains = new List<string> { "DCIM", "_thumbnails" }
        };

        _processor = new EmptyProcessor(_config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region ShouldProcessFile Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.webp", true)]
    [InlineData("video.mp4", true)]
    [InlineData("video.mov", true)]
    public void ShouldProcessFile_ValidExtensions_ReturnsTrue(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.Equal(expected, _processor.ShouldProcessFile(fileData));
    }

    [Theory]
    [InlineData("document.txt", false)]
    [InlineData("document.pdf", false)]
    [InlineData("script.js", false)]
    [InlineData("file.exe", false)]
    public void ShouldProcessFile_InvalidExtensions_ReturnsFalse(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.Equal(expected, _processor.ShouldProcessFile(fileData));
    }

    [Theory]
    [InlineData("skip_photo.jpg")]  // Prefix match: fileName starts with "skip_"
    [InlineData("pss_photo.jpg")]   // Prefix match: fileName starts with "pss_"
    [InlineData("photo_skip.jpg")]  // Suffix match: filename (without ext) ends with "_skip"
    [InlineData("photo_pss.jpg")]   // Suffix match: filename (without ext) ends with "_pss"
    public void ShouldProcessFile_SkippedFiles_ReturnsFalse(string fileName)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_FileInSkipSuffixFolder_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "album_skip", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_FileInSkipPrefixFolder_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "skip_album", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_FileInSkipContainsPath_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "DCIM", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_FileInThumbnailsFolder_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "_thumbnails", "400", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_NestedValidFile_ReturnsTrue()
    {
        var filePath = Path.Combine(_tempDir, "2024", "vacation", "beach", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.True(_processor.ShouldProcessFile(fileData));
    }

    [Fact]
    public void ShouldProcessFile_CaseInsensitiveExtension_ReturnsTrue()
    {
        var filePath = Path.Combine(_tempDir, "photo.JPG");
        var fileData = new FileData(filePath, filePath);

        Assert.True(_processor.ShouldProcessFile(fileData));
    }

    #endregion

    #region ShouldCleanFile Tests

    [Fact]
    public void ShouldCleanFile_SkippedPrefixFile_ReturnsTrue()
    {
        // Files that match skip prefix patterns should be cleaned (their original versions)
        var filePath = Path.Combine(_tempDir, "skip_photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.True(_processor.ShouldCleanFile(fileData));
    }

    [Fact]
    public void ShouldCleanFile_SkippedSuffixFile_ReturnsTrue()
    {
        // Files that match skip suffix patterns should be cleaned (their original versions)
        var filePath = Path.Combine(_tempDir, "photo_skip.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.True(_processor.ShouldCleanFile(fileData));
    }

    [Fact]
    public void ShouldCleanFile_FileInSkipFolder_ReturnsTrue()
    {
        var filePath = Path.Combine(_tempDir, "skip_album", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.True(_processor.ShouldCleanFile(fileData));
    }

    [Fact]
    public void ShouldCleanFile_NormalFile_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "album", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldCleanFile(fileData));
    }

    [Fact]
    public void ShouldCleanFile_NonImageFile_ReturnsFalse()
    {
        // Non-image files should not be cleaned even if they match skip patterns
        var filePath = Path.Combine(_tempDir, "skip_document.txt");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldCleanFile(fileData));
    }

    [Fact]
    public void ShouldCleanFile_FileInSkipContainsPath_ReturnsFalse()
    {
        // Files in SkipContains paths should not be cleaned
        var filePath = Path.Combine(_tempDir, "DCIM", "photo.jpg");
        var fileData = new FileData(filePath, filePath);

        Assert.False(_processor.ShouldCleanFile(fileData));
    }

    #endregion

    #region Handler Tests

    [Fact]
    public async Task OnFileCreated_ReturnsZero()
    {
        var fileData = new FileData(Path.Combine(_tempDir, "photo.jpg"), "data");

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnFileDeleted_ReturnsZero()
    {
        var fileData = new FileData(Path.Combine(_tempDir, "photo.jpg"), "data");

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnEnsureProcessFile_CallsOnFileCreated()
    {
        var fileData = new FileData(Path.Combine(_tempDir, "photo.jpg"), "data");

        var result = await _processor.OnEnsureProcessFile(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnEnsureCleanupFile_ReturnsZero()
    {
        var fileData = new FileData(Path.Combine(_tempDir, "skip_photo.jpg"), "data");

        var result = await _processor.OnEnsureCleanupFile(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnFileChanged_CompletesSuccessfully()
    {
        var fileData = new FileData(Path.Combine(_tempDir, "photo.jpg"), "data");

        await _processor.OnFileChanged(fileData);

        // Should complete without exception
        Assert.True(true);
    }

    [Fact]
    public async Task OnFileRenamed_CompletesSuccessfully()
    {
        var oldPath = new FileData(Path.Combine(_tempDir, "old.jpg"), "data");
        var newPath = new FileData(Path.Combine(_tempDir, "new.jpg"), "data");

        await _processor.OnFileRenamed(oldPath, newPath, true);

        // Should complete without exception
        Assert.True(true);
    }

    [Fact]
    public async Task OnScanStart_CompletesSuccessfully()
    {
        await _processor.OnScanStart();

        Assert.True(true);
    }

    [Fact]
    public async Task OnScanEnd_CompletesSuccessfully()
    {
        await _processor.OnScanEnd();

        Assert.True(true);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void RootFolder_ReturnsConfiguredFolder()
    {
        Assert.Equal(_tempDir, _processor.RootFolder.FullName);
    }

    [Fact]
    public void Extensions_ReturnsCombinedExtensions()
    {
        var extensions = _processor.Extensions;

        Assert.Contains(".jpg", extensions);
        Assert.Contains(".mp4", extensions);
    }

    [Fact]
    public void Extensions_IsCaseInsensitive()
    {
        Assert.Contains(".jpg", _processor.Extensions);
        Assert.Contains(".JPG", _processor.Extensions);
    }

    #endregion
}
