using System.Data.Common;
using System.IO;

namespace PicturesLib.model.album;

public record Album
{
    public long Id { get; set; }   //Int64
    public string AlbumName { get; set; } = string.Empty;
    public string AlbumType { get; set; } = "folder";
    public string? FeatureImagePath { get; set; } = null; 
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public string ParentAlbum { get; set; } = string.Empty;        
    public long ParentAlbumId { get; set; } = 0;
    public bool HasParentAlbum => !string.IsNullOrEmpty(ParentAlbum);


    public static Album CreateFromAlbumPath(string albumName, string rootFolder)
    {
        var relativePath = albumName.Replace(rootFolder, string.Empty);
        return new Album
        {
            AlbumName = relativePath,   //includes the entire relative folder path  ex: 2025/vacation/Florida
            AlbumType  = "folder",   
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ParentAlbum = Path.GetDirectoryName(relativePath) ?? string.Empty
        };
    }
    public static Album CreateFromFilePath(string filePath, string rootFolder)
    {
        var relativePath = filePath.Replace(rootFolder, string.Empty);
        //exclude the file name or current folder 
        //2025/vacation/florida/image.jpg  =>  albumName = 2025/vacation/florida  
        //2025/vacation/florida  =>  albumName = 2025/vacation  
        var albumName = Path.GetDirectoryName(relativePath) ?? string.Empty;  
        return new Album
        {
            AlbumName = albumName,   //includes the entire relative folder path  ex: 2025/vacation/Florida
            AlbumType  = "folder",   
            FeatureImagePath = relativePath,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ParentAlbum = Path.GetDirectoryName(albumName) ?? string.Empty
        };
    }

    public static Album CreateFromDataReader(DbDataReader reader)
    {
        return new Album
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            AlbumType = reader.GetString(reader.GetOrdinal("album_type")),
            FeatureImagePath = reader.GetString(reader.GetOrdinal("feature_image_path")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc")),
            ParentAlbum = reader.GetString(reader.GetOrdinal("parent_album")),
            ParentAlbumId = reader.GetInt64(reader.GetOrdinal("parent_album_id"))
        };
    }



}


