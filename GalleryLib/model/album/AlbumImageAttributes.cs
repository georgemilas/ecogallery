namespace GalleryLib.model.album;

using System.Data.Common;

public record AlbumImageAttributes
{
    public long Id { get; set; }
    public long AlbumImageId { get; set; }
    public bool FaceProcessed { get; set; } = false;   
    public int? TotalFaces { get; set; } = null;  //total number of faces detected in the image, null if not processed yet
    public DateTimeOffset? FaceProcessedUtc { get; set; }  
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    
    public static AlbumImageAttributes CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumImageAttributes
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            FaceProcessed = reader.GetBoolean(reader.GetOrdinal("face_processed")),
            TotalFaces = reader.IsDBNull(reader.GetOrdinal("total_faces")) ? null : reader.GetInt32(reader.GetOrdinal("total_faces")),
            FaceProcessedUtc = reader.IsDBNull(reader.GetOrdinal("face_processed_utc")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("face_processed_utc")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }
}


    