using GalleryLib.model.album;

namespace GalleryLib.repository;

public interface IAlbumRepository
{
    Task<Album> EnsureAlbumExistsAsync(string filePath);
    Task<Album?> GetAlbumByNameAsync(string albumName);
    Task<Album?> GetAlbumByImageAsync(string filePath);
    Task<bool> AlbumHasContentAsync(string filePath);
    Task<bool> AlbumHasContentAsync(Album album);
    Task<Album> AddNewAlbumAsync(Album album);
    Task<int> DeleteAlbumAsync(string filePath, bool logIfCleaned = false);
    Task<int> DeleteAlbumAsync(Album album, bool logIfCleaned = false);
}
