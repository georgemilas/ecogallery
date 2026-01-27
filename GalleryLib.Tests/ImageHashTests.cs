using GalleryLib.service.album;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace GalleryLib.Tests;

/// <summary>
/// Tests for ImageHash - SHA-256 and perceptual hashing
/// </summary>
public class ImageHashTests : IDisposable
{
    private readonly string _tempDir;

    public ImageHashTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Helper Methods

    private async Task<MemoryStream> CreateTestImageStreamAsync(int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height, color);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task<string> CreateTestImageFileAsync(string fileName, int width, int height, Rgba32 color)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        using var image = new Image<Rgba32>(width, height, color);
        await image.SaveAsPngAsync(filePath);
        return filePath;
    }

    private async Task<MemoryStream> CreatePatternedImageStreamAsync(int width, int height, Rgba32 topHalfColor, Rgba32 bottomHalfColor)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            var color = y < height / 2 ? topHalfColor : bottomHalfColor;
            for (int x = 0; x < width; x++)
            {
                image[x, y] = color;
            }
        }
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    #endregion

    #region SHA-256 Tests

    [Fact]
    public async Task ComputeSha256Async_ReturnsSameHashForSameContent()
    {
        using var stream1 = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));
        using var stream2 = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));

        var hash1 = await ImageHash.ComputeSha256Async(stream1);
        var hash2 = await ImageHash.ComputeSha256Async(stream2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsDifferentHashForDifferentContent()
    {
        using var stream1 = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0)); // Red
        using var stream2 = await CreateTestImageStreamAsync(100, 100, new Rgba32(0, 255, 0)); // Green

        var hash1 = await ImageHash.ComputeSha256Async(stream1);
        var hash2 = await ImageHash.ComputeSha256Async(stream2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsHexString()
    {
        using var stream = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));

        var hash = await ImageHash.ComputeSha256Async(stream);

        Assert.Equal(64, hash.Length); // SHA-256 = 32 bytes = 64 hex chars
        Assert.True(hash.All(c => char.IsAsciiHexDigit(c)));
    }

    [Fact]
    public async Task ComputeSha256Async_CanBeCalledMultipleTimesOnSameStream()
    {
        using var stream = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));

        var hash1 = await ImageHash.ComputeSha256Async(stream);
        var hash2 = await ImageHash.ComputeSha256Async(stream);

        Assert.Equal(hash1, hash2);
    }

    #endregion

    #region Perceptual Hash Tests

    [Fact]
    public async Task ComputePerceptualHashAsync_ReturnsSameHashForIdenticalImages()
    {
        using var stream1 = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));
        using var stream2 = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));

        var hash1 = await ImageHash.ComputePerceptualHashAsync(stream1);
        var hash2 = await ImageHash.ComputePerceptualHashAsync(stream2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_ReturnsDifferentHashForDifferentImages()
    {
        // Note: Solid color images will produce the same hash because all pixels have the same
        // luminance, making all pixels equal to the average. Use patterned images instead.
        using var stream1 = await CreatePatternedImageStreamAsync(100, 100, topHalfColor: new Rgba32(255, 255, 255), bottomHalfColor: new Rgba32(0, 0, 0));
        using var stream2 = await CreatePatternedImageStreamAsync(100, 100, topHalfColor: new Rgba32(0, 0, 0), bottomHalfColor: new Rgba32(255, 255, 255));

        var hash1 = await ImageHash.ComputePerceptualHashAsync(stream1);
        var hash2 = await ImageHash.ComputePerceptualHashAsync(stream2);

        // These should be different since one has white top/black bottom and other has black top/white bottom
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_SimilarHashForScaledImages()
    {
        using var stream1 = await CreateTestImageStreamAsync(200, 200, new Rgba32(128, 128, 128));
        using var stream2 = await CreateTestImageStreamAsync(100, 100, new Rgba32(128, 128, 128));

        var hash1 = await ImageHash.ComputePerceptualHashAsync(stream1);
        var hash2 = await ImageHash.ComputePerceptualHashAsync(stream2);

        // Same solid color should produce same hash regardless of size
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_Returns64BitHash()
    {
        using var stream = await CreateTestImageStreamAsync(100, 100, new Rgba32(255, 0, 0));

        var hash = await ImageHash.ComputePerceptualHashAsync(stream);

        // Just verify it's a valid ulong (no exception)
        Assert.True(hash >= 0);
    }

    #endregion

    #region Hamming Distance Tests

    [Fact]
    public void HammingDistance_IdenticalHashes_ReturnsZero()
    {
        ulong h1 = 0b1010101010101010;
        ulong h2 = 0b1010101010101010;

        var distance = ImageHash.HammingDistance(h1, h2);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void HammingDistance_OneBitDifferent_ReturnsOne()
    {
        ulong h1 = 0b1010101010101010;
        ulong h2 = 0b1010101010101011;

        var distance = ImageHash.HammingDistance(h1, h2);

        Assert.Equal(1, distance);
    }

    [Fact]
    public void HammingDistance_AllBitsDifferent_ReturnsMaxBits()
    {
        ulong h1 = 0;
        ulong h2 = ulong.MaxValue;

        var distance = ImageHash.HammingDistance(h1, h2);

        Assert.Equal(64, distance);
    }

    [Theory]
    [InlineData(0b1111, 0b0000, 4)]
    [InlineData(0b1100, 0b0011, 4)]
    [InlineData(0b1010, 0b0101, 4)]
    [InlineData(0b1111_0000, 0b0000_1111, 8)]
    public void HammingDistance_VariousInputs_ReturnsCorrectDistance(ulong h1, ulong h2, int expected)
    {
        var distance = ImageHash.HammingDistance(h1, h2);

        Assert.Equal(expected, distance);
    }

    #endregion

    #region Similarity Tests

    [Fact]
    public void IsExactMatch_IdenticalHashes_ReturnsTrue()
    {
        ulong h1 = 12345678UL;
        ulong h2 = 12345678UL;

        Assert.True(ImageHash.IsExactMatch(h1, h2));
    }

    [Fact]
    public void IsExactMatch_DifferentHashes_ReturnsFalse()
    {
        ulong h1 = 12345678UL;
        ulong h2 = 12345679UL;

        Assert.False(ImageHash.IsExactMatch(h1, h2));
    }

    [Theory]
    [InlineData(0b1111_1111, 0b1111_1110, 5, true)]  // 1 bit diff, threshold 5
    [InlineData(0b1111_1111, 0b1111_0000, 5, true)]  // 4 bits diff, threshold 5
    [InlineData(0b1111_1111, 0b1110_0000, 5, true)]  // 5 bits diff, threshold 5 (5 <= 5 is true)
    [InlineData(0b1111_1111, 0b0000_0000, 5, false)] // 8 bits diff, threshold 5
    public void IsSimilar_VariousThresholds_ReturnsExpected(ulong h1, ulong h2, int threshold, bool expected)
    {
        var result = ImageHash.IsSimilar(h1, h2, threshold);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSimilar_DefaultThreshold_IsFive()
    {
        ulong h1 = 0b1111_1111;
        ulong h2 = 0b1111_1000; // 3 bits different

        Assert.True(ImageHash.IsSimilar(h1, h2)); // Default threshold is 5
    }

    #endregion
}
