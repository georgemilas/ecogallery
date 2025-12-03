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
}


    