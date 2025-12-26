namespace GalleryLib.model.album;

public record AlbumSearch
{
    public string Expression { get; set; } = string.Empty;    
    public long Limit { get; set; } = 1500;
    public long Offset { get; set; } = 0;
    public long Count { get; set; } = 0;
    public bool GroupByPHash { get; set; } = true;  // If true, group results by image hash to show only one image per duplicate group
}


