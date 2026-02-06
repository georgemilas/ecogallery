using System.Data.Common;

namespace GalleryLib.model.album;

public class ImageMetadata   //EXIF, IPTC etc. 
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
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int? Orientation { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public decimal? GpsAltitude { get; set; }
    //public DateTimeOffset? GpsTimestamp { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }     //record last update time UTC



    public static ImageMetadata CreateFromDataReader(DbDataReader reader)
    {
        return new ImageMetadata
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            Camera = reader.IsDBNull(reader.GetOrdinal("camera")) ? null : reader.GetString(reader.GetOrdinal("camera")),
            Lens = reader.IsDBNull(reader.GetOrdinal("lens")) ? null : reader.GetString(reader.GetOrdinal("lens")),
            FocalLength = reader.IsDBNull(reader.GetOrdinal("focal_length")) ? null : reader.GetString(reader.GetOrdinal("focal_length")),
            Aperture = reader.IsDBNull(reader.GetOrdinal("aperture")) ? null : reader.GetString(reader.GetOrdinal("aperture")),
            ExposureTime = reader.IsDBNull(reader.GetOrdinal("exposure_time")) ? null : reader.GetString(reader.GetOrdinal("exposure_time")),
            Iso = reader.IsDBNull(reader.GetOrdinal("iso")) ? null : reader.GetInt32(reader.GetOrdinal("iso")),
            DateTaken = reader.IsDBNull(reader.GetOrdinal("date_taken")) ? null : reader.GetDateTime(reader.GetOrdinal("date_taken")),
            Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? null : reader.GetInt32(reader.GetOrdinal("rating")),
            DateModified = reader.IsDBNull(reader.GetOrdinal("date_modified")) ? null : reader.GetDateTime(reader.GetOrdinal("date_modified")),
            Flash = reader.IsDBNull(reader.GetOrdinal("flash")) ? null : reader.GetString(reader.GetOrdinal("flash")),
            MeteringMode = reader.IsDBNull(reader.GetOrdinal("metering_mode")) ? null : reader.GetString(reader.GetOrdinal("metering_mode")),
            ExposureProgram = reader.IsDBNull(reader.GetOrdinal("exposure_program")) ? null : reader.GetString(reader.GetOrdinal("exposure_program")),
            ExposureBias = reader.IsDBNull(reader.GetOrdinal("exposure_bias")) ? null : reader.GetString(reader.GetOrdinal("exposure_bias")),
            ExposureMode = reader.IsDBNull(reader.GetOrdinal("exposure_mode")) ? null : reader  .GetString(reader.GetOrdinal("exposure_mode")),
            WhiteBalance = reader.IsDBNull(reader.GetOrdinal("white_balance")) ? null : reader.GetString(reader.GetOrdinal("white_balance")),
            ColorSpace = reader.IsDBNull(reader.GetOrdinal("color_space")) ? null : reader.GetString(reader.GetOrdinal("color_space")),      
            SceneCaptureType = reader.IsDBNull(reader.GetOrdinal("scene_capture_type")) ? null : reader.GetString(reader.GetOrdinal("scene_capture_type")),
            CircleOfConfusion = reader.IsDBNull(reader.GetOrdinal("circle_of_confusion")) ? null : reader.GetDecimal(reader.GetOrdinal("circle_of_confusion")),
            FieldOfView = reader.IsDBNull(reader.GetOrdinal("field_of_view")) ? null : reader.GetDecimal(reader.GetOrdinal("field_of_view")),
            DepthOfField = reader.IsDBNull(reader.GetOrdinal("depth_of_field")) ? null : reader.GetDecimal(reader.GetOrdinal("depth_of_field")),
            HyperfocalDistance = reader.IsDBNull(reader.GetOrdinal("hyperfocal_distance")) ? null : reader.GetDecimal(reader.GetOrdinal("hyperfocal_distance")),
            NormalizedLightValue = reader.IsDBNull(reader.GetOrdinal("normalized_light_value")) ? null : reader.GetDecimal(reader.GetOrdinal("normalized_light_value")),
            Software = reader.IsDBNull(reader.GetOrdinal("software")) ? null : reader.GetString(reader.GetOrdinal("software")),
            SerialNumber = reader.IsDBNull(reader.GetOrdinal("serial_number")) ? null : reader.GetString(reader.GetOrdinal("serial_number")),
            LensSerialNumber = reader.IsDBNull(reader.GetOrdinal("lens_serial_number")) ? null : reader.GetString(reader.GetOrdinal("lens_serial_number")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            FileSizeBytes = reader.IsDBNull(reader.GetOrdinal("file_size_bytes")) ? null : reader.GetInt64(reader.GetOrdinal("file_size_bytes")),
            ImageWidth = reader.GetInt32(reader.GetOrdinal("image_width")),
            ImageHeight = reader.GetInt32(reader.GetOrdinal("image_height")),
            Orientation = reader.IsDBNull(reader.GetOrdinal("orientation")) ? null : reader.GetInt32(reader.GetOrdinal("orientation")),
            GpsLatitude = reader.IsDBNull(reader.GetOrdinal("gps_latitude")) ? null : reader.GetDecimal(reader.GetOrdinal("gps_latitude")),
            GpsLongitude = reader.IsDBNull(reader.GetOrdinal("gps_longitude")) ? null : reader.GetDecimal(reader.GetOrdinal("gps_longitude")),
            GpsAltitude = reader.IsDBNull(reader.GetOrdinal("gps_altitude")) ? null : reader.GetDecimal(reader.GetOrdinal("gps_altitude")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))    
        };
    }



}
