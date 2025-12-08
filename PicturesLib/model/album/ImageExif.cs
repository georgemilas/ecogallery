namespace PicturesLib.model.album;

public class ImageExif
{
    public long Id { get; set; }
    public long AlbumImageId { get; set; }
    public string? Camera { get; set; }
    public string? Lens { get; set; }
    public string? FocalLength { get; set; }
    public string? Aperture { get; set; }
    public string? ExposureTime { get; set; }
    public int? Iso { get; set; }
    public DateTime? DateTaken { get; set; }
    public int? Rating { get; set; }
    public DateTime? DateModified { get; set; }
    public string? Flash { get; set; }
    public string? MeteringMode { get; set; }
    public string? ExposureProgram { get; set; }
    public string? ExposureBias { get; set; }
    public string? ExposureMode { get; set; }
    public string? WhiteBalance { get; set; }
    public string? ColorSpace { get; set; }
    public string? SceneCaptureType { get; set; }
    public decimal? CircleOfConfusion { get; set; }
    public decimal? FieldOfView { get; set; }
    public decimal? DepthOfField { get; set; }
    public decimal? HyperfocalDistance { get; set; }
    public decimal? NormalizedLightValue { get; set; }
    public string? Software { get; set; }
    public string? SerialNumber { get; set; }
    public string? LensSerialNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }     //record last update time UTC
}
