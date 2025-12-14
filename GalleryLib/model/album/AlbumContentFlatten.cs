using System.Data.Common;

namespace GalleryLib.model.album;

public record AlbumContentFlatten
{
    public long Id { get; set; }   //Int64
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;    //could be image or video
    public string ItemPath { get; set; } = string.Empty; 
    public string AlbumName { get; set; } = string.Empty; 
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public DateTimeOffset ItemTimestampUtc { get; set; }    
    

    public static AlbumContentFlatten CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumContentFlatten
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ItemName = reader.GetString(reader.GetOrdinal("item_name")),
            ItemType = reader.GetString(reader.GetOrdinal("item_type")),
            ItemPath = reader.GetString(reader.GetOrdinal("item_path")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc")),
            ItemTimestampUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("item_timestamp_utc"))
        };
    }

}