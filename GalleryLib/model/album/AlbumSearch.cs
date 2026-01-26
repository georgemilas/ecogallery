using System.Reflection.Metadata;

namespace GalleryLib.model.album;

public record AlbumSearch
{
    public const int DefaultLimit = 2000;
    public string Expression { get; set; } = string.Empty;    
    public long Limit { get; set; } = DefaultLimit;
    public long Offset { get; set; } = 0;
    public long Count { get; set; } = 0;
    public bool GroupByPHash { get; set; } = true;  // If true, group results by image hash to show only one image per duplicate group
}


