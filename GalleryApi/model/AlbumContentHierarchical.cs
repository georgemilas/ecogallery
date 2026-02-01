using System.Data.Common;
using GalleryLib.model.album;
using System.IO;

namespace GalleryApi.model;




public record AlbumPathElement
{
    public string Name { get; set; } = string.Empty;
    public long Id { get; set; } = 0;
}   


public record VirtualAlbumContent: AlbumContentHierarchical
{
    public string Expression { get; set; } = string.Empty;    
}

public record AlbumContentHierarchical: AlbumItemContent
{
    public List<AlbumItemContent> Albums { get; set; } = new List<AlbumItemContent>(); 
    public List<ImageItemContent> Images { get; set; } = new List<ImageItemContent>(); 
    public AlbumSettings Settings { get; set; } = null!;
    public AlbumSearch? SearchInfo { get; set; } = null;
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
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public ImageMetadata? ImageMetadata { get; set; }
    public VideoMetadata? VideoMetadata { get; set; }
    public List<FaceBoxInfo> Faces { get; set; } = new List<FaceBoxInfo>();

}

public record ItemContent
{
    public long Id { get; set; }   //Int64
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; 
    public List<AlbumPathElement> NavigationPathSegments { get; set; } = new List<AlbumPathElement>();
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
