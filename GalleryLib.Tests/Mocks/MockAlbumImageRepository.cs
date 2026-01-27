using GalleryLib.model.album;
using GalleryLib.repository;

namespace GalleryLib.Tests.Mocks;

/// <summary>
/// Mock implementation of IAlbumImageRepository for testing
/// </summary>
public class MockAlbumImageRepository : IAlbumImageRepository
{
    private readonly Dictionary<string, AlbumImage> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, ImageMetadata> _imageMetadata = new();
    private readonly Dictionary<long, VideoMetadata> _videoMetadata = new();
    private long _nextId = 1;
    private long _nextMetadataId = 1;

    // Track method calls for verification
    public List<string> AddedImages { get; } = new();
    public List<string> DeletedImages { get; } = new();
    public List<long> UpdatedHashes { get; } = new();
    public List<long> UpdatedDimensions { get; } = new();
    public List<ImageMetadata> AddedImageMetadata { get; } = new();
    public List<VideoMetadata> AddedVideoMetadata { get; } = new();

    private string? _rootFolder;

    public void SetRootFolder(string rootFolder)
    {
        _rootFolder = rootFolder;
    }

    public Task<AlbumImage?> GetAlbumImageAsync(string filePath)
    {
        // Convert to relative path (mimics real repository behavior)
        var relativePath = GetRelativePath(filePath);
        _images.TryGetValue(relativePath, out var image);
        return Task.FromResult(image);
    }

    public Task<AlbumImage> AddNewImageAsync(string filePath, Album? album = null)
    {
        var relativePath = GetRelativePath(filePath);

        if (_images.TryGetValue(relativePath, out var existing))
        {
            existing.LastUpdatedUtc = DateTimeOffset.UtcNow;
            return Task.FromResult(existing);
        }

        var image = new AlbumImage
        {
            Id = _nextId++,
            ImageName = Path.GetFileName(filePath),
            ImagePath = relativePath,
            ImageType = Path.GetExtension(filePath),
            AlbumId = album?.Id ?? 0,
            AlbumName = album?.AlbumName ?? Path.GetDirectoryName(relativePath) ?? string.Empty,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ImageTimestampUtc = DateTimeOffset.UtcNow
        };

        _images[relativePath] = image;
        AddedImages.Add(filePath);
        return Task.FromResult(image);
    }

    public Task<long> UpdateImageHash(AlbumImage image)
    {
        if (_images.Values.Any(i => i.Id == image.Id))
        {
            var existing = _images.Values.First(i => i.Id == image.Id);
            existing.ImageSha256 = image.ImageSha256;
            existing.LastUpdatedUtc = image.LastUpdatedUtc;
            UpdatedHashes.Add(image.Id);
            return Task.FromResult(image.Id);
        }
        return Task.FromResult(0L);
    }

    public Task<long> UpdateImageDimensions(AlbumImage image)
    {
        if (_images.Values.Any(i => i.Id == image.Id))
        {
            var existing = _images.Values.First(i => i.Id == image.Id);
            existing.ImageWidth = image.ImageWidth;
            existing.ImageHeight = image.ImageHeight;
            existing.LastUpdatedUtc = image.LastUpdatedUtc;
            UpdatedDimensions.Add(image.Id);
            return Task.FromResult(image.Id);
        }
        return Task.FromResult(0L);
    }

    public Task<int> DeleteAlbumImageAsync(string filePath)
    {
        var relativePath = GetRelativePath(filePath);
        if (_images.Remove(relativePath, out var removed))
        {
            _imageMetadata.Remove(removed.Id);
            _videoMetadata.Remove(removed.Id);
            DeletedImages.Add(filePath);
            return Task.FromResult(1);
        }
        return Task.FromResult(0);
    }

    public Task<ImageMetadata?> GetImageMetadataAsync(AlbumImage albumImage)
    {
        _imageMetadata.TryGetValue(albumImage.Id, out var metadata);
        return Task.FromResult(metadata);
    }

    public Task<VideoMetadata?> GetVideoMetadataAsync(AlbumImage albumImage)
    {
        _videoMetadata.TryGetValue(albumImage.Id, out var metadata);
        return Task.FromResult(metadata);
    }

    public Task<ImageMetadata> AddNewImageMetadataAsync(ImageMetadata exif)
    {
        exif.Id = _nextMetadataId++;
        _imageMetadata[exif.AlbumImageId] = exif;
        AddedImageMetadata.Add(exif);
        return Task.FromResult(exif);
    }

    public Task<VideoMetadata> AddNewVideoMetadataAsync(VideoMetadata videoMetadata)
    {
        videoMetadata.Id = _nextMetadataId++;
        _videoMetadata[videoMetadata.AlbumImageId] = videoMetadata;
        AddedVideoMetadata.Add(videoMetadata);
        return Task.FromResult(videoMetadata);
    }

    public Task<List<AlbumImage>> GetAllAlbumImagesAsync()
    {
        return Task.FromResult(_images.Values.ToList());
    }

    // Helper to pre-populate data for tests
    public void AddExistingImage(AlbumImage image)
    {
        // Normalize the path to ensure consistent lookups
        var normalizedPath = image.ImagePath.Replace('/', Path.DirectorySeparatorChar)
                                            .Replace('\\', Path.DirectorySeparatorChar);
        image.ImagePath = normalizedPath;
        _images[normalizedPath] = image;
        if (image.Id >= _nextId) _nextId = image.Id + 1;
    }

    public void AddExistingImageMetadata(ImageMetadata metadata)
    {
        _imageMetadata[metadata.AlbumImageId] = metadata;
    }

    public void AddExistingVideoMetadata(VideoMetadata metadata)
    {
        _videoMetadata[metadata.AlbumImageId] = metadata;
    }

    private string GetRelativePath(string filePath)
    {
        // Mimic real repository behavior: convert to relative path
        var path = filePath;
        if (!string.IsNullOrEmpty(_rootFolder) && path.StartsWith(_rootFolder, StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(_rootFolder.Length);
        }
        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
    }

    public void Clear()
    {
        _images.Clear();
        _imageMetadata.Clear();
        _videoMetadata.Clear();
        AddedImages.Clear();
        DeletedImages.Clear();
        UpdatedHashes.Clear();
        UpdatedDimensions.Clear();
        AddedImageMetadata.Clear();
        AddedVideoMetadata.Clear();
        _nextId = 1;
        _nextMetadataId = 1;
    }
}
