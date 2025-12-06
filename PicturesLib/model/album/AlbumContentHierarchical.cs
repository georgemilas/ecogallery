using System.Data.Common;
using System.IO;

namespace PicturesLib.model.album;

public record AlbumContentHierarchical
{
    public long Id { get; set; }   //Int64
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;   //could be media (aka image or movie) or folder
    public string? FeatureItemType { get; set; } = string.Empty; //could be media or null (if there is no media for the folder) in which case we need to look at inner 
    public string FeatureItemPath { get; set; } = string.Empty; 
    public string? InnerFeatureItemType { get; set; } = string.Empty;   //not null only if FeatureItemType is null
    public string? InnerFeatureItemPath { get; set; } = string.Empty;   //not null only if FeatureItemType is null
    public DateTimeOffset LastUpdatedUtc { get; set; }    

    public static AlbumContentHierarchical CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumContentHierarchical
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ItemName = reader.GetString(reader.GetOrdinal("item_name")),
            ItemType = reader.GetString(reader.GetOrdinal("item_type")),
            FeatureItemType = reader.IsDBNull(reader.GetOrdinal("feature_item_type")) ? null : reader.GetString(reader.GetOrdinal("feature_item_type")),
            FeatureItemPath = reader.GetString(reader.GetOrdinal("feature_item_path")),
            InnerFeatureItemType = reader.IsDBNull(reader.GetOrdinal("inner_feature_item_type")) ? null :  reader.GetString(reader.GetOrdinal("inner_feature_item_type")),
            InnerFeatureItemPath = reader.IsDBNull(reader.GetOrdinal("inner_feature_item_path")) ? null : reader.GetString(reader.GetOrdinal("inner_feature_item_path")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))
        };
    }


    
}
