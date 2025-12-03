using System.Data.Common;
using System.IO;

namespace PicturesLib.model.album;

public record Album
{
    public long Id;   //Int64
    public string AlbumName = string.Empty;
    public string AlbumType = "folder";
    public string FeatureImagePath = string.Empty; 
    public DateTimeOffset LastUpdated;    
    public string ParentAlbum => Path.GetDirectoryName(AlbumName) ?? string.Empty;        
    public bool HasParentAlbum => ParentAlbum != string.Empty;        

}


