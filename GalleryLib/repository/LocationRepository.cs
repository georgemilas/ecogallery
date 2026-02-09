using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.repository;

public class LocationRepository(DatabaseConfiguration dbConfig) : IDisposable, IAsyncDisposable
{
    private readonly PostgresDatabaseService _db = new(dbConfig.ToConnectionString());

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<LocationCluster?> FindNearestClusterAsync(double latitude, double longitude, int tierMeters)
    {
                var sql = @"
                        SELECT
                            id,
                            tier_meters,
                            name,
                            ST_Y(centroid) AS centroid_latitude,
                            ST_X(centroid) AS centroid_longitude,
                            created_utc,
                            last_updated_utc
                        FROM public.location_cluster
                        WHERE tier_meters = @tier_meters
                            AND ST_DWithin(
                                ST_Transform(centroid, 3857),
                                ST_Transform(ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326), 3857),
                                @tier_meters
                            )
                        ORDER BY ST_Distance(
                                ST_Transform(centroid, 3857),
                                ST_Transform(ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326), 3857)
                        )
                        LIMIT 1";

        var results = await _db.QueryAsync(sql, LocationCluster.CreateFromDataReader, new
        {
            latitude,
            longitude,
            tierMeters
        });

        return results.FirstOrDefault();
    }

    public async Task<long> CreateClusterAsync(double latitude, double longitude, int tierMeters)
    {
        var sql = @"
            INSERT INTO public.location_cluster
                (tier_meters, name, centroid, created_utc, last_updated_utc)
            VALUES
                (@tier_meters, NULL, ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326), @created_utc, @last_updated_utc)
            RETURNING id";

        var now = DateTimeOffset.UtcNow;
        var id = await _db.ExecuteScalarAsync<long>(sql, new
        {
            tierMeters,
            latitude,
            longitude,
            createdUtc = now,
            lastUpdatedUtc = now
        });

        return id;
    }

    public async Task<long?> GetClusterIdForImageAsync(long albumImageId, int tierMeters)
    {
                var sql = @"
                        SELECT lc.id
                        FROM public.location_cluster_item lci
                        JOIN public.location_cluster lc ON lc.id = lci.cluster_id
                        WHERE lci.album_image_id = @album_image_id
                            AND lc.tier_meters = @tier_meters
                        LIMIT 1";

        return await _db.ExecuteScalarAsync<long?>(sql, new
        {
            albumImageId,
            tierMeters
        });
    }

    public async Task AddImageToClusterAsync(long clusterId, long albumImageId)
    {
        var sql = @"
            INSERT INTO public.location_cluster_item
                (cluster_id, album_image_id, created_utc)
            VALUES
                (@cluster_id, @album_image_id, @created_utc)
            ON CONFLICT (cluster_id, album_image_id) DO NOTHING";

        await _db.ExecuteAsync(sql, new
        {
            clusterId,
            albumImageId,
            createdUtc = DateTimeOffset.UtcNow
        });
    }

    public async Task UpdateClusterCentroidAsync(long clusterId)
    {
                var sql = @"
                        WITH points AS (
                                SELECT ST_SetSRID(ST_MakePoint(im.gps_longitude, im.gps_latitude), 4326) AS geom
                                FROM public.location_cluster_item lci
                                JOIN public.image_metadata im ON im.album_image_id = lci.album_image_id
                                WHERE lci.cluster_id = @cluster_id
                                    AND im.gps_latitude IS NOT NULL AND im.gps_longitude IS NOT NULL
                                UNION ALL
                                SELECT ST_SetSRID(ST_MakePoint(vm.gps_longitude, vm.gps_latitude), 4326) AS geom
                                FROM public.location_cluster_item lci
                                JOIN public.video_metadata vm ON vm.album_image_id = lci.album_image_id
                                WHERE lci.cluster_id = @cluster_id
                                    AND vm.gps_latitude IS NOT NULL AND vm.gps_longitude IS NOT NULL
                        )
                        UPDATE public.location_cluster
                        SET centroid = (SELECT ST_Centroid(ST_Collect(geom)) FROM points),
                                last_updated_utc = @last_updated_utc
                        WHERE id = @cluster_id";

        await _db.ExecuteAsync(sql, new
        {
            clusterId,
            lastUpdatedUtc = DateTimeOffset.UtcNow
        });
    }
}
