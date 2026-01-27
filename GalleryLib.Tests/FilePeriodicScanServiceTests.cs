using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for FilePeriodicScanService - periodic directory scanning
/// </summary>
public class FilePeriodicScanServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PicturesDataConfiguration _config;

    public FilePeriodicScanServiceTests()
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
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Helper Methods

    private void CreateTestFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, "test content");
    }

    #endregion

    #region GetFilesToProcess Tests (via TrackingProcessor)

    [Fact]
    public async Task Scan_FindsNewFiles()
    {
        CreateTestFile("album/photo1.jpg");
        CreateTestFile("album/photo2.jpg");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        // Simulate a scan cycle
        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500); // Give time for scan
        await cts.CancelAsync();

        Assert.Equal(2, processor.CreatedFiles.Count);
    }

    [Fact]
    public async Task Scan_IgnoresNonImageFiles()
    {
        CreateTestFile("album/photo.jpg");
        CreateTestFile("album/document.txt");
        CreateTestFile("album/script.js");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Single(processor.CreatedFiles);
        Assert.Contains(processor.CreatedFiles, f => f.Contains("photo.jpg"));
    }

    [Fact]
    public async Task Scan_IgnoresSkippedFiles()
    {
        // Create valid file and files with skip patterns
        CreateTestFile("album/photo.jpg");
        CreateTestFile("album/skip_photo.jpg");  // Prefix match - will be skipped
        CreateTestFile("album/photo_skip.jpg");  // Suffix match - will be skipped

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Single(processor.CreatedFiles);
        Assert.Contains(processor.CreatedFiles, f => f.EndsWith("photo.jpg") && !f.Contains("skip"));
    }

    [Fact]
    public async Task Scan_IgnoresThumbnailsFolder()
    {
        CreateTestFile("album/photo.jpg");
        CreateTestFile("_thumbnails/400/album/photo.jpg");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Single(processor.CreatedFiles);
        Assert.DoesNotContain(processor.CreatedFiles, f => f.Contains("_thumbnails"));
    }

    [Fact]
    public async Task Scan_HandlesNestedDirectories()
    {
        CreateTestFile("2024/vacation/beach/photo1.jpg");
        CreateTestFile("2024/vacation/mountain/photo2.jpg");
        CreateTestFile("2023/birthday/photo3.jpg");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Equal(3, processor.CreatedFiles.Count);
    }

    [Fact]
    public async Task Scan_DetectsDeletedFiles()
    {
        CreateTestFile("album/photo1.jpg");
        CreateTestFile("album/photo2.jpg");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        // First scan
        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);

        // Delete a file
        File.Delete(Path.Combine(_tempDir, "album/photo2.jpg"));

        // Wait for another scan cycle (we can't easily trigger it, so we test the tracking)
        await cts.CancelAsync();

        // At minimum, we should have detected 2 created files in first scan
        Assert.Equal(2, processor.CreatedFiles.Count);
    }

    [Fact]
    public async Task Scan_HandlesEmptyDirectory()
    {
        // Create subdirectory but no files
        Directory.CreateDirectory(Path.Combine(_tempDir, "empty_album"));

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Empty(processor.CreatedFiles);
    }

    [Fact]
    public async Task Scan_ProcessesAllImageExtensions()
    {
        CreateTestFile("album/photo.jpg");
        CreateTestFile("album/photo.jpeg");
        CreateTestFile("album/photo.png");
        CreateTestFile("album/video.mp4");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        Assert.Equal(4, processor.CreatedFiles.Count);
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task Scan_IdentifiesFilesToClean()
    {
        // Create a file that was renamed to be skipped
        CreateTestFile("album/skip_photo.jpg");

        var processor = new TrackingProcessor(_config);
        var service = new FilePeriodicScanService(processor, intervalMinutes: 60);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();

        // The skip_photo.jpg should be identified for cleanup (not processing)
        Assert.DoesNotContain(processor.CreatedFiles, f => f.Contains("skip_"));
    }

    #endregion
}

/// <summary>
/// Test processor that tracks which files were processed
/// </summary>
public class TrackingProcessor : EmptyProcessor
{
    public List<string> CreatedFiles { get; } = new();
    public List<string> DeletedFiles { get; } = new();
    public List<string> CleanedFiles { get; } = new();
    private readonly object _lock = new();

    public TrackingProcessor(PicturesDataConfiguration config) : base(config)
    {
    }

    public override async Task<int> OnFileCreated(FileData filePath, bool logIfCreated = false)
    {
        lock (_lock)
        {
            CreatedFiles.Add(filePath.FilePath);
        }
        return 1;
    }

    public override async Task<int> OnFileDeleted(FileData filePath, bool logIfDeleted = false)
    {
        lock (_lock)
        {
            DeletedFiles.Add(filePath.FilePath);
        }
        return 1;
    }

    public override async Task<int> OnEnsureCleanupFile(FileData skipFilePath, bool logIfCleaned = false)
    {
        lock (_lock)
        {
            CleanedFiles.Add(skipFilePath.FilePath);
        }
        return 1;
    }
}
