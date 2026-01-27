using GalleryLib.model.configuration;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for PicturesDataConfiguration model
/// </summary>
public class PicturesDataConfigurationTests
{
    private readonly PicturesDataConfiguration _config;
    private readonly string _tempDir;

    public PicturesDataConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _config = new PicturesDataConfiguration
        {
            Folder = _tempDir,
            ImageExtensions = new List<string> { ".jpg", ".jpeg", ".png" },
            MovieExtensions = new List<string> { ".mp4", ".mov" }
        };
    }

    [Fact]
    public void Extensions_CombinesImageAndMovieExtensions()
    {
        var extensions = _config.Extensions;

        Assert.Contains(".jpg", extensions);
        Assert.Contains(".jpeg", extensions);
        Assert.Contains(".png", extensions);
        Assert.Contains(".mp4", extensions);
        Assert.Contains(".mov", extensions);
        Assert.Equal(5, extensions.Count);
    }

    [Fact]
    public void Extensions_CachesResult()
    {
        var extensions1 = _config.Extensions;
        var extensions2 = _config.Extensions;

        Assert.Same(extensions1, extensions2);
    }

    [Fact]
    public void Extensions_ContainsLowercaseExtensions()
    {
        // Note: Extensions is a List<string> which is case-sensitive
        // Case-insensitivity is handled by EmptyProcessor when creating the HashSet
        Assert.Contains(".jpg", _config.Extensions);
        Assert.Contains(".mp4", _config.Extensions);
    }

    [Fact]
    public void RootFolder_ReturnsDirectoryInfo()
    {
        var rootFolder = _config.RootFolder;

        Assert.Equal(_tempDir, rootFolder.FullName);
    }

    [Fact]
    public void ThumbnailsBase_ReturnsThumbnailsPath()
    {
        var expected = Path.Combine(_tempDir, "_thumbnails");

        Assert.Equal(expected, _config.ThumbnailsBase);
    }

    [Fact]
    public void ThumbDir_ReturnsCorrectPath()
    {
        var expected = Path.Combine(_tempDir, "_thumbnails", "400");

        Assert.Equal(expected, _config.ThumbDir(400));
    }

    [Theory]
    [InlineData(".mp4", true)]
    [InlineData(".MP4", true)]
    [InlineData(".mov", true)]
    [InlineData(".MOV", true)]
    [InlineData(".jpg", false)]
    [InlineData(".png", false)]
    [InlineData(".txt", false)]
    public void IsMovieFile_CorrectlyIdentifiesMovies(string extension, bool expected)
    {
        var filePath = Path.Combine(_tempDir, $"test{extension}");

        Assert.Equal(expected, _config.IsMovieFile(filePath));
    }

    [Theory]
    [InlineData("photo_label.jpg", true)]
    [InlineData("photo_feature.jpg", true)]
    [InlineData("label_photo.jpg", true)]
    [InlineData("feature_photo.jpg", true)]
    [InlineData("photo.jpg", false)]
    [InlineData("labeled_photo.jpg", false)]
    public void IsFeatureFile_CorrectlyIdentifiesFeaturePhotos(string fileName, bool expected)
    {
        var filePath = Path.Combine(_tempDir, fileName);

        Assert.Equal(expected, _config.IsFeatureFile(filePath));
    }

    [Fact]
    public void GetThumbnailPath_ForImage_ReplacesRootWithThumbDir()
    {
        var sourceFile = Path.Combine(_tempDir, "album", "photo.jpg");
        var expected = Path.Combine(_tempDir, "_thumbnails", "400", "album", "photo.jpg");

        var result = _config.GetThumbnailPath(sourceFile, 400);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetThumbnailPath_ForMovie_ChangesExtensionToJpg()
    {
        var sourceFile = Path.Combine(_tempDir, "album", "video.mp4");
        var expected = Path.Combine(_tempDir, "_thumbnails", "400", "album", "video.jpg");

        var result = _config.GetThumbnailPath(sourceFile, 400);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DefaultSkipSuffix_ContainsExpectedValues()
    {
        var config = new PicturesDataConfiguration();

        Assert.Contains("_skip", config.SkipSuffix);
        Assert.Contains("_pss", config.SkipSuffix);
        Assert.Contains("_noW", config.SkipSuffix);
    }

    [Fact]
    public void DefaultSkipPrefix_ContainsExpectedValues()
    {
        var config = new PicturesDataConfiguration();

        Assert.Contains("skip_", config.SkipPrefix);
        Assert.Contains("pss_", config.SkipPrefix);
        Assert.Contains("noW_", config.SkipPrefix);
    }

    [Fact]
    public void DefaultSkipContains_ContainsExpectedValues()
    {
        var config = new PicturesDataConfiguration();

        Assert.Contains("_thumbnails", config.SkipContains);
    }
}
