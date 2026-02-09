using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.fileProcessor;

namespace GalleryLib.service.album;

/// <summary>
/// Groups images and videos into location clusters based on GPS coordinates.
/// Maintains clusters for 300m, 2km, and 25km tiers and updates centroids as new items arrive.
/// </summary>
public class GeospatialLocationProcessor : EmptyProcessor
{
    private readonly AlbumImageRepository _imageRepository;
    private readonly LocationRepository _locationRepository;
    private static readonly int[] TierMeters = { 300, 2000, 25000 };

    public GeospatialLocationProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
        : base(configuration)
    {
        _imageRepository = new AlbumImageRepository(configuration, dbConfig);
        _locationRepository = new LocationRepository(dbConfig);
    }

    public static PeriodicScanService CreateProcessor(
        PicturesDataConfiguration configuration,
        DatabaseConfiguration dbConfig,
        int degreeOfParallelism = -1,
        bool planMode = false,
        bool logIfProcessed = false)
    {
        IFileProcessor processor = new GeospatialLocationProcessor(configuration, dbConfig);
        return new DbPeriodicScanService(processor, configuration, dbConfig, intervalMinutes: 10, degreeOfParallelism: degreeOfParallelism, logIfProcessed);
    }

    public override bool ShouldCleanFile(FileData dbPath, bool logIfProcess = false)
    {
        return false;
    }

    public override async Task<int> OnFileCreated(FileData dbPath, bool logIfCreated = false)
    {
        var albumImage = dbPath.Data as AlbumImage;
        if (albumImage == null)
        {
            return 0;
        }

        var gps = await TryGetGpsAsync(albumImage);
        if (gps.Latitude == null || gps.Longitude == null)
        {
            return 0;
        }

        int assignedCount = 0;
        foreach (var tier in TierMeters)
        {
            var existingClusterId = await _locationRepository.GetClusterIdForImageAsync(albumImage.Id, tier);
            if (existingClusterId.HasValue)
            {
                continue;
            }

            var cluster = await _locationRepository.FindNearestClusterAsync(gps.Latitude.Value, gps.Longitude.Value, tier);
            var clusterId = cluster?.Id ?? await _locationRepository.CreateClusterAsync(gps.Latitude.Value, gps.Longitude.Value, tier);

            await _locationRepository.AddImageToClusterAsync(clusterId, albumImage.Id);
            await _locationRepository.UpdateClusterCentroidAsync(clusterId);

            assignedCount++;
            if (logIfCreated)
            {
                Console.WriteLine($"Geo cluster: image {albumImage.Id} -> cluster {clusterId} ({tier}m)");
            }
        }

        return assignedCount;
    }

    private async Task<(double? Latitude, double? Longitude)> TryGetGpsAsync(AlbumImage albumImage)
    {
        if (_configuration.IsMovieFile(albumImage.ImagePath))
        {
            var videoMetadata = await _imageRepository.GetVideoMetadataAsync(albumImage);
            if (videoMetadata?.GpsLatitude != null && videoMetadata.GpsLongitude != null)
            {
                return ((double)videoMetadata.GpsLatitude.Value, (double)videoMetadata.GpsLongitude.Value);
            }
        }

        var imageMetadata = await _imageRepository.GetImageMetadataAsync(albumImage);
        if (imageMetadata?.GpsLatitude != null && imageMetadata.GpsLongitude != null)
        {
            return ((double)imageMetadata.GpsLatitude.Value, (double)imageMetadata.GpsLongitude.Value);
        }

        // var fallbackVideoMetadata = await _imageRepository.GetVideoMetadataAsync(albumImage);
        // if (fallbackVideoMetadata?.GpsLatitude != null && fallbackVideoMetadata.GpsLongitude != null)
        // {
        //     return ((double)fallbackVideoMetadata.GpsLatitude.Value, (double)fallbackVideoMetadata.GpsLongitude.Value);
        // }

        return (null, null);
    }
}
