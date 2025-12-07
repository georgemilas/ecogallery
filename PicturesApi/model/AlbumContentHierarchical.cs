using System.Data.Common;
using PicturesLib.model.album;
using System.IO;

namespace PicturesApi.model;

public record AlbumContentHierarchical
{
    public long Id { get; set; }   //Int64
    public string Name { get; set; } = string.Empty;
    public bool IsAlbum { get; set; } = false;   
    public List<string> NavigationPathSegments { get; set; } = new List<string>();
    public string ImagePath { get; set; } = string.Empty;     
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public DateTimeOffset ItemTimestampUtc { get; set; }
    public List<AlbumContentHierarchical> Content { get; set; } = new List<AlbumContentHierarchical>();
    
}
