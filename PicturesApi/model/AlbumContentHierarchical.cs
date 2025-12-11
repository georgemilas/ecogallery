using System.Data.Common;
using PicturesLib.model.album;
using System.IO;

namespace PicturesApi.model;

public record AlbumContentHierarchical: AlbumItemContent
{
    public List<AlbumItemContent> Albums { get; set; } = new List<AlbumItemContent>(); 
    public List<ImageItemContent> Images { get; set; } = new List<ImageItemContent>(); 
}

public record AlbumItemContent: ItemContent
{
    public string ImageHDPath { get; set; } = string.Empty;         
}

public record ImageItemContent: ItemContent
{
    public string ImageHDPath { get; set; } = string.Empty;     
    public string ImageUHDPath { get; set; } = string.Empty;     
    public string ImageOriginalPath { get; set; } = string.Empty;     
    public bool IsMovie { get; set; }
    public ImageExif? ImageExif { get; set; }    
}

public record ItemContent
{
    public long Id { get; set; }   //Int64
    public string Name { get; set; } = string.Empty;
    public List<string> NavigationPathSegments { get; set; } = new List<string>();
    public string ThumbnailPath { get; set; } = string.Empty;         
    public DateTimeOffset LastUpdatedUtc { get; set; }    
    public DateTimeOffset ItemTimestampUtc { get; set; }
    
}

public class AlbumNotFoundException : Exception
{
    public AlbumNotFoundException(string? albumName) : base($"Album not found: '{albumName}'")
    {
        AlbumName = albumName;
    }
    public string? AlbumName { get; set; }
}
