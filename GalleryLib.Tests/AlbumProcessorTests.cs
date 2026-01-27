using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.album;
using GalleryLib.service.fileProcessor;
using GalleryLib.Tests.Mocks;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for AlbumProcessor - database sync for albums and images
/// </summary>
public class AlbumProcessorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PicturesDataConfiguration _config;
    private readonly MockAlbumImageRepository _mockImageRepo;
    private readonly MockAlbumRepository _mockAlbumRepo;
    private readonly TestableAlbumProcessor _processor;

    public AlbumProcessorTests()
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
        _mockAlbumRepo.SetRootFolder(_tempDir);
        _processor = new TestableAlbumProcessor(_config, _mockImageRepo, _mockAlbumRepo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Helper Methods

    private string CreateTestFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, "test content");
        return fullPath;
    }

    #endregion

    #region OnFileCreated Tests

    [Fact]
    public async Task OnFileCreated_NewImage_CreatesAlbumAndImageRecords()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImages);
        Assert.Contains(_mockAlbumRepo.EnsuredAlbums, p => p.Contains("photo.jpg"));
    }

    [Fact]
    public async Task OnFileCreated_ExistingImage_ReturnsZero()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        // Pre-populate with existing image
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg",
            ImageSha256 = "existinghash"
        });

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnFileCreated_NewImageInNestedAlbum_CreatesAlbumHierarchy()
    {
        var filePath = CreateTestFile("2024/vacation/beach/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileCreated(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.AddedImages);
        // Album should be ensured
        Assert.NotEmpty(_mockAlbumRepo.EnsuredAlbums);
    }

    [Fact]
    public async Task OnFileCreated_MultipleImagesInSameAlbum_ReusesAlbum()
    {
        var filePath1 = CreateTestFile("album/photo1.jpg");
        var filePath2 = CreateTestFile("album/photo2.jpg");
        var fileData1 = new FileData(filePath1, filePath1);
        var fileData2 = new FileData(filePath2, filePath2);

        await _processor.OnFileCreated(fileData1);
        await _processor.OnFileCreated(fileData2);

        Assert.Equal(2, _mockImageRepo.AddedImages.Count);
    }

    #endregion

    #region OnFileDeleted Tests

    [Fact]
    public async Task OnFileDeleted_ExistingImage_DeletesImageRecord()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        // Pre-populate
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg",
            AlbumName = Path.GetDirectoryName(relativePath) ?? string.Empty
        });
        _mockAlbumRepo.SetAlbumImageCount(Path.GetDirectoryName(relativePath) ?? string.Empty, 1);

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.DeletedImages);
    }

    [Fact]
    public async Task OnFileDeleted_NonExistingImage_ReturnsZero()
    {
        var filePath = Path.Combine(_tempDir, "album/nonexistent.jpg");
        var fileData = new FileData(filePath, filePath);

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task OnFileDeleted_LastImageInAlbum_DeletesAlbumToo()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        // Pre-populate with image and empty album (no other images)
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        var albumName = Path.GetDirectoryName(relativePath) ?? string.Empty;
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg",
            AlbumName = albumName
        });
        _mockAlbumRepo.AddExistingAlbum(new Album
        {
            Id = 1,
            AlbumName = albumName
        });
        _mockAlbumRepo.SetAlbumImageCount(albumName, 1); // Will be empty after deletion

        var result = await _processor.OnFileDeleted(fileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.DeletedImages);
        // Album should be deleted since it's empty
        Assert.Contains(_mockAlbumRepo.DeletedAlbums, a => a == albumName);
    }

    #endregion

    #region OnFileChanged Tests

    [Fact]
    public async Task OnFileChanged_ExistingImage_UpdatesRecord()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        // Pre-populate
        var relativePath = filePath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = relativePath,
            ImageName = "photo.jpg"
        });

        await _processor.OnFileChanged(fileData);

        // Should not add new image since it exists
        Assert.Empty(_mockImageRepo.AddedImages);
    }

    [Fact]
    public async Task OnFileChanged_NewImage_CreatesRecord()
    {
        var filePath = CreateTestFile("album/photo.jpg");
        var fileData = new FileData(filePath, filePath);

        await _processor.OnFileChanged(fileData);

        Assert.Single(_mockImageRepo.AddedImages);
    }

    #endregion

    #region OnFileRenamed Tests

    [Fact]
    public async Task OnFileRenamed_ValidRename_DeletesOldAndCreatesNew()
    {
        var oldPath = CreateTestFile("album/old.jpg");
        var newPath = Path.Combine(_tempDir, "album/new.jpg");
        File.Move(oldPath, newPath);

        var oldFileData = new FileData(oldPath, oldPath);
        var newFileData = new FileData(newPath, newPath);

        // Pre-populate old image
        var oldRelativePath = oldPath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = oldRelativePath,
            ImageName = "old.jpg"
        });

        await _processor.OnFileRenamed(oldFileData, newFileData, true);

        Assert.Single(_mockImageRepo.DeletedImages);
        Assert.Single(_mockImageRepo.AddedImages);
    }

    [Fact]
    public async Task OnFileRenamed_ToSkippedName_OnlyDeletes()
    {
        var oldPath = CreateTestFile("album/photo.jpg");
        var newPath = Path.Combine(_tempDir, "album/skip_photo.jpg");
        File.Move(oldPath, newPath);

        var oldFileData = new FileData(oldPath, oldPath);
        var newFileData = new FileData(newPath, newPath);

        // Pre-populate old image
        var oldRelativePath = oldPath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = oldRelativePath,
            ImageName = "photo.jpg"
        });

        await _processor.OnFileRenamed(oldFileData, newFileData, false);

        Assert.Single(_mockImageRepo.DeletedImages);
        Assert.Empty(_mockImageRepo.AddedImages);
    }

    [Fact]
    public async Task OnFileRenamed_ToNewAlbum_CreatesInNewAlbum()
    {
        var oldPath = CreateTestFile("album1/photo.jpg");
        Directory.CreateDirectory(Path.Combine(_tempDir, "album2"));
        var newPath = Path.Combine(_tempDir, "album2/photo.jpg");
        File.Move(oldPath, newPath);

        var oldFileData = new FileData(oldPath, oldPath);
        var newFileData = new FileData(newPath, newPath);

        // Pre-populate old image
        var oldRelativePath = oldPath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = oldRelativePath,
            ImageName = "photo.jpg",
            AlbumName = "\\album1"
        });

        await _processor.OnFileRenamed(oldFileData, newFileData, true);

        Assert.Single(_mockImageRepo.DeletedImages);
        Assert.Single(_mockImageRepo.AddedImages);
        Assert.Contains(_mockImageRepo.AddedImages, p => p.Contains("album2"));
    }

    #endregion

    #region OnEnsureCleanupFile Tests

    [Fact]
    public async Task OnEnsureCleanupFile_SkipPrefixFile_CleansOriginal()
    {
        var originalPath = CreateTestFile("album/photo.jpg");
        var skipPath = Path.Combine(_tempDir, "album/skip_photo.jpg");
        File.Move(originalPath, skipPath);

        var skipFileData = new FileData(skipPath, skipPath);

        // Pre-populate original image (the one that should be cleaned)
        var originalRelativePath = originalPath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = originalRelativePath,
            ImageName = "photo.jpg"
        });

        var result = await _processor.OnEnsureCleanupFile(skipFileData);

        Assert.Equal(1, result);
        Assert.Single(_mockImageRepo.DeletedImages);
    }

    [Fact]
    public async Task OnEnsureCleanupFile_SkipSuffixFile_CleansOriginal()
    {
        var originalPath = CreateTestFile("album/photo.jpg");
        var skipPath = Path.Combine(_tempDir, "album/photo_skip.jpg");
        File.Move(originalPath, skipPath);

        var skipFileData = new FileData(skipPath, skipPath);

        // Pre-populate original image
        var originalRelativePath = originalPath.Replace(_tempDir, string.Empty);
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = originalRelativePath,
            ImageName = "photo.jpg"
        });

        var result = await _processor.OnEnsureCleanupFile(skipFileData);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task OnEnsureCleanupFile_FileInSkipFolder_CleansOriginal()
    {
        // Simulate: album was renamed to album_skip
        var skipPath = Path.Combine(_tempDir, "album_skip/photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(skipPath)!);
        File.WriteAllText(skipPath, "test");

        var skipFileData = new FileData(skipPath, skipPath);

        // Pre-populate original image (in original "album" folder)
        _mockImageRepo.AddExistingImage(new AlbumImage
        {
            Id = 1,
            ImagePath = "\\album\\photo.jpg",
            ImageName = "photo.jpg",
            AlbumName = "\\album"
        });

        var result = await _processor.OnEnsureCleanupFile(skipFileData);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task OnEnsureCleanupFile_ThumbnailFile_ReturnsZero()
    {
        var thumbPath = Path.Combine(_tempDir, "_thumbnails/400/album/photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbPath)!);
        File.WriteAllText(thumbPath, "test");

        var thumbFileData = new FileData(thumbPath, thumbPath);

        var result = await _processor.OnEnsureCleanupFile(thumbFileData);

        Assert.Equal(0, result);
        Assert.Empty(_mockImageRepo.DeletedImages);
    }

    #endregion

    #region ShouldProcessFile Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.png", true)]
    [InlineData("video.mp4", true)]
    [InlineData("document.txt", false)]
    [InlineData("skip_photo.jpg", false)]
    [InlineData("photo_skip.jpg", false)]
    public void ShouldProcessFile_CorrectlyFilters(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, "album", fileName);
        var fileData = new FileData(filePath, filePath);

        Assert.Equal(expected, _processor.ShouldProcessFile(fileData));
    }

    #endregion
}

/// <summary>
/// Testable version of AlbumProcessor that exposes the test constructor
/// </summary>
public class TestableAlbumProcessor : AlbumProcessor
{
    public TestableAlbumProcessor(PicturesDataConfiguration configuration, IAlbumImageRepository imageRepo, IAlbumRepository albumRepo)
        : base(configuration, imageRepo, albumRepo)
    {
    }
}
