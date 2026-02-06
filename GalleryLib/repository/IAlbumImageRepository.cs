using GalleryLib.model.album;

namespace GalleryLib.repository;

public interface IAlbumImageRepository
{
    Task<AlbumImage?> GetAlbumImageAsync(string filePath);
    Task<AlbumImage> AddNewImageAsync(string filePath, Album? album = null);
    Task<long> UpdateImageHash(AlbumImage image);
    Task<long> UpdateImageDimensionsAndDateTaken(AlbumImage image);
    Task<int> DeleteAlbumImageAsync(string filePath);
    Task<ImageMetadata?> GetImageMetadataAsync(AlbumImage albumImage);
    Task<VideoMetadata?> GetVideoMetadataAsync(AlbumImage albumImage);
    Task<ImageMetadata> UpsertImageMetadataAsync(ImageMetadata exif);
    Task<VideoMetadata> UpsertVideoMetadataAsync(VideoMetadata videoMetadata);
    Task<List<AlbumImage>> GetAllAlbumImagesAsync();
}
