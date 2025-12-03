using System.Data.Common;
using System.IO;

namespace PicturesLib.model.album;

public record Album
{
    public long Id { get; set; }   //Int64
    public string AlbumName { get; set; } = string.Empty;
    public string AlbumType { get; set; } = "folder";
    public string FeatureImagePath { get; set; } = string.Empty; 
    public DateTimeOffset LastUpdated { get; set; }    
    public string ParentAlbum { get; set; } = string.Empty;        
    public bool HasParentAlbum => !string.IsNullOrEmpty(ParentAlbum);
}


