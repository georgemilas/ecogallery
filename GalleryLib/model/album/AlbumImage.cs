namespace GalleryLib.model.album;

using System.Data.Common;
using System.IO;

public record AlbumImage
{
    public long Id { get; set; }    
    public string ImageName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ImageType { get; set; } = ".jpg";    
    public DateTimeOffset LastUpdatedUtc { get; set; }     //record last update time UTC
    public string AlbumName { get; set; } = string.Empty;
    public long AlbumId { get; set; } = 0;
    public DateTimeOffset ImageTimestampUtc { get; set; }  //file last write time UTC


    public static AlbumImage CreateFromFilePath(string filePath, string rootFolder)
    {

        FileInfo fi = new FileInfo(filePath);    
        DateTimeOffset lastWriteTimeUtc = fi.LastWriteTimeUtc;

        var path = filePath.Replace(rootFolder, string.Empty);
        return new AlbumImage
        {
            ImageName = Path.GetFileName(path),
            ImagePath = path,
            ImageType  = Path.GetExtension(path),   //includes the dot, e.g. ".jpg"             
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            AlbumName = Path.GetDirectoryName(path) ?? string.Empty,   //includes the entire folder path  ex: 2025/vacation/Florida
            ImageTimestampUtc = lastWriteTimeUtc
        };
    }

    public static AlbumImage CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumImage
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ImageName = reader.GetString(reader.GetOrdinal("image_name")),
            ImagePath = reader.GetString(reader.GetOrdinal("image_path")),
            ImageType = reader.GetString(reader.GetOrdinal("image_type")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            AlbumId = reader.GetInt64(reader.GetOrdinal("album_id")),
            ImageTimestampUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("image_timestamp_utc")) 
        };
    }


}


    