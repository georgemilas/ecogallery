using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace GalleryLib.service.album;

/// <summary>
/// Compute SHA-256 hash for exact duplicate detection
///
/// When we want to detect small edits of an image as similar images
/// Also compute perceptual hash (aHash) on 8x8 grayscale thumbnail for near-duplicate detection.
/// Returns ulong for fast Hamming distance comparison.
/// 
/// </summary>
public static class ImageHash
{
    public static async Task<string> ComputeSha256Async(Stream imageStream, CancellationToken ct = default)
    {
        imageStream.Position = 0;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = await sha.ComputeHashAsync(imageStream, ct);
        return Convert.ToHexString(hash);
    }

    
    /// <summary>
    /// Compute aHash from an image stream. Downscales to 8x8, converts to grayscale, averages luminance.
    /// Returns 64-bit hash where each bit represents if pixel >= average luminance.
    /// </summary>
    public static async Task<ulong> ComputePerceptualHashAsync(Stream imageStream, int perceptualHashSize = 8)
    {
        imageStream.Position = 0;
        using var img = Image.Load<Rgba32>(imageStream);
        
        // Resize to exact perceptualHashSize while preserving aspect ratio via padding.
        using var small = img.Clone(x =>
            x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Pad,
                PadColor = new Rgba32(128, 128, 128, 255),
                Position = AnchorPositionMode.Center,
                Size = new Size(perceptualHashSize, perceptualHashSize)
            })
        );

        // Extract grayscale values
        Span<byte> gray = stackalloc byte[perceptualHashSize * perceptualHashSize];
        int idx = 0;
        for (int y = 0; y < perceptualHashSize; y++)
        {
            for (int x = 0; x < perceptualHashSize; x++)
            {
                // Guard against unexpected dimensions
                var px = Math.Min(x, small.Width - 1);
                var py = Math.Min(y, small.Height - 1);
                var p = small[px, py];
                // Luminance formula: 0.299*R + 0.587*G + 0.114*B
                gray[idx++] = (byte)((p.R * 0.299) + (p.G * 0.587) + (p.B * 0.114));
            }
        }

        // Compute average
        double avg = gray.ToArray().Average(b => (double)b);

        // Build hash: bit = 1 if pixel >= average, 0 otherwise
        ulong hash = 0;
        for (int b = 0; b < perceptualHashSize * perceptualHashSize; b++)
        {
            if (gray[b] >= avg) 
            {
                hash |= 1UL << b;
            }
        }

        return hash;
    }

    /// <summary>
    /// Hamming distance: count differing bits. 0 = exact match, higher = more different.
    /// For 64-bit hashes, typical threshold for "near-duplicate" is 5-10.
    /// </summary>
    public static int HammingDistance(ulong h1, ulong h2)
    {
        return BitOperations.PopCount(h1 ^ h2);
    }

    /// <summary>
    /// Check if two hashes are equal (exact match).
    /// </summary>
    public static bool IsExactMatch(ulong h1, ulong h2) 
    { 
        return h1 == h2; 
    }

    /// <summary>
    /// Check if hashes are similar within threshold.
    /// </summary>
    public static bool IsSimilar(ulong h1, ulong h2, int hammingThreshold = 5) 
    {
        return HammingDistance(h1, h2) <= hammingThreshold;
    }
    
}
