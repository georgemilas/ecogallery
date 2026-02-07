using System.Data.Common;
using System.IO;
using GalleryLib.service.album;

namespace GalleryLib.model.album;


public record VirtualAlbum
{
    public long Id { get; set; }   //Int64
    public string AlbumName { get; set; } = string.Empty;
    public string AlbumDescription { get; set; } = string.Empty;
    public string AlbumExpression { get; set; } = string.Empty;
    public string AlbumFolder { get; set; } = string.Empty;
    public string AlbumType { get; set; } = "expression";    //one of "expression", "folder"
    public bool PersistentExpression { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    public string? FeatureImagePath { get; set; } = null; 
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public DateTimeOffset CreatedTimestampUtc { get; set; }
    public string ParentAlbum { get; set; } = string.Empty;        
    public long ParentAlbumId { get; set; } = 0;
    public long RoleId { get; set; } = 1;  //defaults to public role
    public bool HasParentAlbum => !string.IsNullOrEmpty(ParentAlbum);
    


    public static VirtualAlbum CreateFromYaml(string albumName, VirtualAlbumYml yml)
    {
        return new VirtualAlbum
        {
            AlbumName = albumName,
            AlbumDescription = yml.Description,
            AlbumExpression = yml.Expression,
            AlbumType = yml.AlbumType,
            PersistentExpression = yml.Persistent,
            IsPublic = yml.IsPublic,    
            FeatureImagePath = yml.Feature,
            LastUpdatedUtc = DateTimeOffset.UtcNow,            
            CreatedTimestampUtc = DateTimeOffset.UtcNow,
            AlbumFolder = yml.Folder,
            ParentAlbum = yml.Parent
        };
    }
    public static VirtualAlbum CreateFromDataReader(DbDataReader reader)
    {
        return new VirtualAlbum
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            AlbumDescription = reader.GetString(reader.GetOrdinal("album_description")), 
            AlbumExpression = reader.GetString(reader.GetOrdinal("album_expression")),
            AlbumType = reader.GetString(reader.GetOrdinal("album_type")),
            PersistentExpression = reader.GetBoolean(reader.GetOrdinal("persistent_expression")),
            IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
            AlbumFolder = reader.GetString(reader.GetOrdinal("album_folder")),
            FeatureImagePath = reader.GetString(reader.GetOrdinal("feature_image_path")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc")),
            CreatedTimestampUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_timestamp_utc")),
            ParentAlbum = reader.GetString(reader.GetOrdinal("parent_album")),
            ParentAlbumId = reader.GetInt64(reader.GetOrdinal("parent_album_id")),
            RoleId = reader.GetInt64(reader.GetOrdinal("role_id"))
        };
    }



}


