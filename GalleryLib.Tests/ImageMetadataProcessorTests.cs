using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.album;
using GalleryLib.service.fileProcessor;
using GalleryLib.Tests.Mocks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for ImageMetadataProcessor - EXIF/metadata extraction and storage
/// </summary>
public class ImageMetadataProcessorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PicturesDataConfiguration _config;
    private readonly MockAlbumImageRepository _mockImageRepo;
    private readonly MockAlbumRepository _mockAlbumRepo;
    private readonly TestableImageMetadataProcessor _processor;

    public ImageMetadataProcessorTests()
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

        _mockImageRepo = new MockAlbumImageRepository();
        _mockImageRepo.SetRootFolder(_tempDir);
        _mockAlbumRepo = new MockAlbumRepository();
        _processor = new TestableImageMetadataProcessor(_config, _mockImageRepo, _mockAlbumRepo);
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

    #region OnFileCreated Tests - Image Metadata

    [Fact]
    public async Task OnFileCreated_NewImage_CreatesImageAndMetadataRecords()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImages);
        Assert.Single(_mockImageRepo.AddedImageMetadata);
    }

    [Fact]
    public async Task OnFileCreated_NewImage_ExtractsDimensions()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        await _processor.OnFileCreated(fileData);

        Assert.Single(_mockImageRepo.AddedImageMetadata);
        var metadata = _mockImageRepo.AddedImageMetadata[0];
        Assert.Equal(1920, metadata.ImageWidth);
        Assert.Equal(1080, metadata.ImageHeight);
    }

    [Fact]
    public async Task OnFileCreated_NewImage_UpdatesAlbumImageDimensions()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        await _processor.OnFileCreated(fileData);

        Assert.Single(_mockImageRepo.UpdatedDimensions);
    }

    [Fact]
    public async Task OnFileCreated_ExistingImageWithMetadata_SkipsMetadataExtraction()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        // Pre-populate with existing image and metadata
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        var existingImage = new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg",
            ImageSha256 = "existinghash"
        };
        _mockImageRepo.AddExistingImage(existingImage);
        _mockImageRepo.AddExistingImageMetadata(new ImageMetadata
        {
            Id = 1,
            AlbumImageId = 1,
            ImageWidth = 1920,
            ImageHeight = 1080
        });

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(0, result);
        Assert.Empty(_mockImageRepo.AddedImageMetadata);
    }

    [Fact]
    public async Task OnFileCreated_ExistingImageWithoutMetadata_ExtractsMetadata()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        // Pre-populate with existing image but NO metadata
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg",
            ImageSha256 = "existinghash"
        });

        var result = await _processor.OnFileCreated(fileData);

        // Should extract metadata even though image exists
        Assert.Single(_mockImageRepo.AddedImageMetadata);
    }

    [Fact]
    public async Task OnFileCreated_PngImage_ExtractsMetadata()
    {
        var filePath = await CreateTestPngAsync("album/photo.png", 800, 600);
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImageMetadata);
        var metadata = _mockImageRepo.AddedImageMetadata[0];
        Assert.Equal(800, metadata.ImageWidth);
        Assert.Equal(600, metadata.ImageHeight);
    }

    [Fact]
    public async Task OnFileCreated_NestedAlbum_CreatesRecordsCorrectly()
    {
        var filePath = await CreateTestImageAsync("2024/vacation/beach/sunset.jpg", 4000, 3000);
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImages);
        Assert.Single(_mockImageRepo.AddedImageMetadata);
    }

    #endregion

    #region ExtractImageMetadata Tests

    [Fact]
    public async Task ExtractImageMetadata_ValidJpeg_ReturnsMetadata()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);

        var metadata = await _processor.ExtractImageMetadata(filePath);

        Assert.NotNull(metadata);
        Assert.Equal(1920, metadata.ImageWidth);
        Assert.Equal(1080, metadata.ImageHeight);
        Assert.Equal("photo.jpg", metadata.FileName);
    }

    [Fact]
    public async Task ExtractImageMetadata_ValidPng_ReturnsMetadata()
    {
        var filePath = await CreateTestPngAsync("album/photo.png", 800, 600);

        var metadata = await _processor.ExtractImageMetadata(filePath);

        Assert.NotNull(metadata);
        Assert.Equal(800, metadata.ImageWidth);
        Assert.Equal(600, metadata.ImageHeight);
    }

    [Fact]
    public async Task ExtractImageMetadata_NonExistentFile_ReturnsNull()
    {
        var filePath = Path.Combine(_tempDir, "nonexistent.jpg");

        var metadata = await _processor.ExtractImageMetadata(filePath);

        Assert.Null(metadata);
    }

    [Fact]
    public async Task ExtractImageMetadata_IncludesFileInfo()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);

        var metadata = await _processor.ExtractImageMetadata(filePath);

        Assert.NotNull(metadata);
        Assert.Equal("photo.jpg", metadata.FileName);
        Assert.True(metadata.FileSizeBytes > 0);
        Assert.NotNull(metadata.DateModified);
    }

    #endregion

    #region OnFileDeleted Tests

    [Fact]
    public async Task OnFileDeleted_ImageWithMetadata_DeletesBothRecords()
    {
        var filePath = await CreateTestImageAsync("album/photo.jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        // Pre-populate
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        var image = new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg"
        };
        _mockImageRepo.AddExistingImage(image);
        _mockImageRepo.AddExistingImageMetadata(new ImageMetadata
        {
            Id = 1,
            AlbumImageId = 1
        });
        _mockAlbumRepo.SetAlbumImageCount(Path.GetDirectoryName(relativePath) ?? string.Empty, 1);

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.DeletedImages);
        // Metadata is deleted via CASCADE, so mock handles this
    }

    #endregion

    #region Multiple Images Tests

    [Fact]
    public async Task OnFileCreated_MultipleImages_CreatesAllRecords()
    {
        var filePath1 = await CreateTestImageAsync("album/photo1.jpg", 1920, 1080);
        var filePath2 = await CreateTestImageAsync("album/photo2.jpg", 1280, 720);
        var filePath3 = await CreateTestPngAsync("album/photo3.png", 800, 600);

        await _processor.OnFileCreated(new FileData(filePath1, filePath1));
        await _processor.OnFileCreated(new FileData(filePath2, filePath2));
        await _processor.OnFileCreated(new FileData(filePath3, filePath3));

        Assert.Equal(3, _mockImageRepo.AddedImages.Count);
        Assert.Equal(3, _mockImageRepo.AddedImageMetadata.Count);
    }

    [Fact]
    public async Task OnFileCreated_ImagesWithDifferentDimensions_TracksCorrectly()
    {
        var filePath1 = await CreateTestImageAsync("album/landscape.jpg", 1920, 1080);
        var filePath2 = await CreateTestImageAsync("album/portrait.jpg", 1080, 1920);
        var filePath3 = await CreateTestImageAsync("album/square.jpg", 1000, 1000);

        await _processor.OnFileCreated(new FileData(filePath1, filePath1));
        await _processor.OnFileCreated(new FileData(filePath2, filePath2));
        await _processor.OnFileCreated(new FileData(filePath3, filePath3));

        var dimensions = _mockImageRepo.AddedImageMetadata.Select(m => (m.ImageWidth, m.ImageHeight)).ToList();
        Assert.Contains((1920, 1080), dimensions);
        Assert.Contains((1080, 1920), dimensions);
        Assert.Contains((1000, 1000), dimensions);
    }

    #endregion

    #region ShouldProcessFile Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("video.mp4", true)]
    [InlineData("document.txt", false)]
    [InlineData("skip_photo.jpg", false)]
    public void ShouldProcessFile_CorrectlyFilters(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, "album", fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.Equal(expected, _processor.ShouldProcessFile(fileData));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task OnFileCreated_SmallImage_ExtractsCorrectDimensions()
    {
        var filePath = await CreateTestImageAsync("album/tiny.jpg", 10, 10);
        var fileData = new FileData(filePath, filePath);

        await _processor.OnFileCreated(fileData);

        Assert.Single(_mockImageRepo.AddedImageMetadata);
        var metadata = _mockImageRepo.AddedImageMetadata[0];
        Assert.Equal(10, metadata.ImageWidth);
        Assert.Equal(10, metadata.ImageHeight);
    }

    [Fact]
    public async Task OnFileCreated_LargeImage_ExtractsCorrectDimensions()
    {
        var filePath = await CreateTestImageAsync("album/large.jpg", 8000, 6000);
        var fileData = new FileData(filePath, filePath);

        await _processor.OnFileCreated(fileData);

        Assert.Single(_mockImageRepo.AddedImageMetadata);
        var metadata = _mockImageRepo.AddedImageMetadata[0];
        Assert.Equal(8000, metadata.ImageWidth);
        Assert.Equal(6000, metadata.ImageHeight);
    }

    [Fact]
    public async Task OnFileCreated_FileWithSpecialCharactersInName_Works()
    {
        var filePath = await CreateTestImageAsync("album/photo (1).jpg", 1920, 1080);
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImages);
    }

    #endregion
}

/// <summary>
/// Testable version of ImageMetadataProcessor that exposes test constructor and methods
/// </summary>
public class TestableImageMetadataProcessor : ImageMetadataProcessor
{
    public TestableImageMetadataProcessor(PicturesDataConfiguration configuration, IAlbumImageRepository imageRepo, IAlbumRepository albumRepo)
        : base(configuration, imageRepo, albumRepo)
    {
    }

    // Expose protected method for testing
    public new Task<ImageMetadata?> ExtractImageMetadata(FileData fileData)
    {
        return base.ExtractImageMetadata(fileData);
    }

    public Task<ImageMetadata?> ExtractImageMetadata(string filePath)
    {
        return base.ExtractImageMetadata(new FileData(filePath, filePath));
    }
}
