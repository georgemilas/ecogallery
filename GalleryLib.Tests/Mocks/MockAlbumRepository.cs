using GalleryLib.model.album;
using GalleryLib.repository;

namespace GalleryLib.Tests.Mocks;

/// <summary>
/// Mock implementation of IAlbumRepository for testing
/// </summary>
public class MockAlbumRepository : IAlbumRepository
{
    private readonly Dictionary<string, Album> _albums = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _albumImageCounts = new(StringComparer.OrdinalIgnoreCase);
    private long _nextId = 1;

    // Track method calls for verification
    public List<string> EnsuredAlbums { get; } = new();
    public List<string> DeletedAlbums { get; } = new();
    public List<Album> AddedAlbums { get; } = new();

    private string? _rootFolder;

    public void SetRootFolder(string rootFolder)
    {
        _rootFolder = rootFolder;
    }

    public Task<Album> EnsureAlbumExistsAsync(string filePath)
    {
        var albumName = Path.GetDirectoryName(filePath)?.Replace('/', Path.DirectorySeparatorChar) ?? string.Empty;

        if (_albums.TryGetValue(albumName, out var existing))
        {
            EnsuredAlbums.Add(filePath);
            return Task.FromResult(existing);
        }

        var album = new Album
        {
            Id = _nextId++,
            AlbumName = albumName,
            AlbumType = "folder",
            FeatureImagePath = filePath,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            AlbumTimestampUtc = DateTimeOffset.UtcNow,
            ParentAlbum = Path.GetDirectoryName(albumName) ?? string.Empty
        };

        // Ensure parent album exists recursively
        if (!string.IsNullOrEmpty(album.ParentAlbum) && !_albums.ContainsKey(album.ParentAlbum))
        {
            var parentAlbum = EnsureAlbumExistsAsync(albumName).Result;
            album.ParentAlbumId = parentAlbum.Id;
        }

        _albums[albumName] = album;
        _albumImageCounts[albumName] = 0;
        EnsuredAlbums.Add(filePath);
        return Task.FromResult(album);
    }

    public Task<Album?> GetAlbumByNameAsync(string albumName)
    {
        _albums.TryGetValue(albumName, out var album);
        return Task.FromResult(album);
    }

    public Task<Album?> GetAlbumByImageAsync(string filePath)
    {
        var albumName = Path.GetDirectoryName(filePath) ?? string.Empty;
        _albums.TryGetValue(albumName, out var album);
        return Task.FromResult(album);
    }

    public Task<bool> AlbumHasContentAsync(string filePath)
    {
        var albumName = Path.GetDirectoryName(filePath) ?? string.Empty;
        return AlbumHasContentAsync(new Album { AlbumName = albumName });
    }

    public Task<bool> AlbumHasContentAsync(Album album)
    {
        // Check if this album or any sub-album has images
        var hasContent = _albumImageCounts.Any(kvp =>
            kvp.Key.StartsWith(album.AlbumName, StringComparison.OrdinalIgnoreCase) && kvp.Value > 0);
        return Task.FromResult(hasContent);
    }

    public Task<Album> AddNewAlbumAsync(Album album)
    {
        if (album.Id == 0)
        {
            album.Id = _nextId++;
        }
        _albums[album.AlbumName] = album;
        if (!_albumImageCounts.ContainsKey(album.AlbumName))
        {
            _albumImageCounts[album.AlbumName] = 0;
        }
        AddedAlbums.Add(album);
        return Task.FromResult(album);
    }

    public Task<int> DeleteAlbumAsync(string filePath, bool logIfCleaned = false)
    {
        var albumName = Path.GetDirectoryName(filePath) ?? string.Empty;
        albumName = GetRelativePath(albumName);
        return DeleteAlbumAsync(new Album { AlbumName = albumName, ParentAlbum = Path.GetDirectoryName(albumName) ?? string.Empty }, logIfCleaned);
    }

    public Task<int> DeleteAlbumAsync(Album album, bool logIfCleaned = false)
    {
        int deleted = 0;
        if (_albums.Remove(album.AlbumName))
        {
            _albumImageCounts.Remove(album.AlbumName);
            DeletedAlbums.Add(album.AlbumName);
            deleted = 1;

            // Recursively delete parent if empty
            if (!string.IsNullOrEmpty(album.ParentAlbum) && _albums.ContainsKey(album.ParentAlbum))
            {
                if (!AlbumHasContentAsync(new Album { AlbumName = album.ParentAlbum }).Result)
                {
                    deleted += DeleteAlbumAsync(new Album
                    {
                        AlbumName = album.ParentAlbum,
                        ParentAlbum = Path.GetDirectoryName(album.ParentAlbum) ?? string.Empty
                    }, logIfCleaned).Result;
                }
            }
        }
        return Task.FromResult(deleted);
    }

    // Helper methods for tests
    public void AddExistingAlbum(Album album)
    {
        _albums[album.AlbumName] = album;
        if (!_albumImageCounts.ContainsKey(album.AlbumName))
        {
            _albumImageCounts[album.AlbumName] = 0;
        }
        if (album.Id >= _nextId) _nextId = album.Id + 1;
    }

    public void SetAlbumImageCount(string albumName, int count)
    {
        _albumImageCounts[albumName] = count;
    }

    public void IncrementAlbumImageCount(string albumName)
    {
        if (_albumImageCounts.ContainsKey(albumName))
        {
            _albumImageCounts[albumName]++;
        }
        else
        {
            _albumImageCounts[albumName] = 1;
        }
    }

    public void DecrementAlbumImageCount(string albumName)
    {
        if (_albumImageCounts.ContainsKey(albumName) && _albumImageCounts[albumName] > 0)
        {
            _albumImageCounts[albumName]--;
        }
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
        _albums.Clear();
        _albumImageCounts.Clear();
        EnsuredAlbums.Clear();
        DeletedAlbums.Clear();
        AddedAlbums.Clear();
        _nextId = 1;
    }
}
