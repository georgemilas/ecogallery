using System.Data.Common;

namespace GalleryLib.model.album;

public record LocationCluster
{
    public long Id { get; set; }
    public int TierMeters { get; set; }
    public string? Name { get; set; }
    public double CentroidLatitude { get; set; }
    public double CentroidLongitude { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }

    public static LocationCluster CreateFromDataReader(DbDataReader reader)
    {
        var nameOrdinal = reader.GetOrdinal("name");
        return new LocationCluster
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            TierMeters = reader.GetInt32(reader.GetOrdinal("tier_meters")),
            Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
            CentroidLatitude = reader.GetDouble(reader.GetOrdinal("centroid_latitude")),
            CentroidLongitude = reader.GetDouble(reader.GetOrdinal("centroid_longitude")),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_utc")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }
}

public record LocationClusterItem
{
    public long Id { get; set; }
    public long ClusterId { get; set; }
    public long AlbumImageId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }

    public static LocationClusterItem CreateFromDataReader(DbDataReader reader)
    {
        return new LocationClusterItem
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ClusterId = reader.GetInt64(reader.GetOrdinal("cluster_id")),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_utc"))
        };
    }
}


public record ImageLocationCluster
{
    public long ClusterId { get; set; }
    public int TierMeters { get; set; }
    public string TierName { 
        get {
            if (TierMeters <= 300)
            {
                return "Location";
            }
            else if (TierMeters <= 2000)
            {
                return "Neighborhood";
            }
            else
            {
                return "Area";
            }
        } 
    } 
    public string? Name { get; set; }
    public long AlbumImageId { get; set; }
    public double CentroidLatitude { get; set; }
    public double CentroidLongitude { get; set; }    
    
    public static ImageLocationCluster CreateFromDataReader(DbDataReader reader)
    {
        var nameOrdinal = reader.GetOrdinal("name");
        return new ImageLocationCluster
        {
            ClusterId = reader.GetInt64(reader.GetOrdinal("cluster_id")),
            TierMeters = reader.GetInt32(reader.GetOrdinal("tier_meters")),
            Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            CentroidLatitude = reader.GetDouble(reader.GetOrdinal("centroid_latitude")),
            CentroidLongitude = reader.GetDouble(reader.GetOrdinal("centroid_longitude")),            
        };
    }
}
