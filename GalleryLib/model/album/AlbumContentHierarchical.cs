using System.Data.Common;
using System.IO;
using System.Text.Json;

namespace GalleryLib.model.album;

public record AlbumContentHierarchical
{
    public long Id { get; set; }   //Int64
    public string ItemName { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;   //could be media (aka image or movie) or folder
    public long ParentAlbumId { get; set; }   //Int64
    public string ParentAlbumName { get; set; } = string.Empty;
    public string? FeatureItemType { get; set; } = string.Empty; //could be media or null (if there is no media for the folder) in which case we need to look at inner 
    public string FeatureItemPath { get; set; } = string.Empty; 
    public string? InnerFeatureItemType { get; set; } = string.Empty;   //not null only if FeatureItemType is null
    public string? InnerFeatureItemPath { get; set; } = string.Empty;   //not null only if FeatureItemType is null
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public DateTimeOffset ItemTimestampUtc { get; set; }
    public ImageMetadata? ImageExif { get; set; }  
    public VideoMetadata? VideoMetadata { get; set; }

    public static AlbumContentHierarchical CreateFromDataReader(DbDataReader reader)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        return new AlbumContentHierarchical
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ItemName = reader.GetString(reader.GetOrdinal("item_name")),
            ItemDescription = reader.IsDBNull(reader.GetOrdinal("item_description")) ? string.Empty :  reader.GetString(reader.GetOrdinal("item_description")),
            ItemType = reader.GetString(reader.GetOrdinal("item_type")),
            ParentAlbumId = reader.GetInt64(reader.GetOrdinal("parent_album_id")),
            ParentAlbumName = reader.GetString(reader.GetOrdinal("parent_album_name")),
            FeatureItemType = reader.IsDBNull(reader.GetOrdinal("feature_item_type")) ? null : reader.GetString(reader.GetOrdinal("feature_item_type")),
            FeatureItemPath = reader.GetString(reader.GetOrdinal("feature_item_path")),
            InnerFeatureItemType = reader.IsDBNull(reader.GetOrdinal("inner_feature_item_type")) ? null :  reader.GetString(reader.GetOrdinal("inner_feature_item_type")),
            InnerFeatureItemPath = reader.IsDBNull(reader.GetOrdinal("inner_feature_item_path")) ? null : reader.GetString(reader.GetOrdinal("inner_feature_item_path")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc")),
            ItemTimestampUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("item_timestamp_utc")),
            ImageExif = reader.IsDBNull(reader.GetOrdinal("image_exif")) 
                                        ? null 
                                        : JsonSerializer.Deserialize<ImageMetadata>(reader.GetString(reader.GetOrdinal("image_exif")), options),
            VideoMetadata = reader.IsDBNull(reader.GetOrdinal("video_metadata")) 
                                        ? null 
                                        : JsonSerializer.Deserialize<VideoMetadata>(reader.GetString(reader.GetOrdinal("video_metadata")), options)
            

        };
    }


    
}
