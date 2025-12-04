namespace PicturesLib.model.album;

using System.Data.Common;
using System.IO;

public record AlbumImage
{
    public long Id { get; set; }
    public string AlbumName { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ImageType { get; set; } = ".jpg";    
    public DateTimeOffset LastUpdated { get; set; }


    public static AlbumImage CreateFromPath(string filePath, string rootFolder)
    {

        var path = filePath.Replace(rootFolder, string.Empty);
        return new AlbumImage
        {
            AlbumName = Path.GetDirectoryName(path) ?? string.Empty,   //includes the entire folder path  ex: 2025/vacation/Florida
            ImageName = Path.GetFileName(path),
            ImagePath = path,
            ImageType  = Path.GetExtension(path),   //includes the dot, e.g. ".jpg"             
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public static AlbumImage CreateFromDataReader(DbDataReader reader)
    {
        return new AlbumImage
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            ImageName = reader.GetString(reader.GetOrdinal("image_name")),
            ImagePath = reader.GetString(reader.GetOrdinal("image_path")),
            ImageType = reader.GetString(reader.GetOrdinal("image_type")),
            LastUpdated = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated"))
        };
    }


}


    