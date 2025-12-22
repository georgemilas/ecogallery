using System.Data.Common;

namespace GalleryLib.model.album;

/// <summary>
/// Video metadata extracted from video files
/// </summary>
public class VideoMetadata
{
    public long Id { get; set; }
    public long AlbumImageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public DateTime? DateTaken { get; set; }
    public DateTime? DateModified { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? VideoWidth { get; set; }
    public int? VideoHeight { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public string? PixelFormat { get; set; }
    public decimal? FrameRate { get; set; }
    public long? VideoBitRate { get; set; }
    public int? AudioSampleRate { get; set; }
    public int? AudioChannels { get; set; }
    public long? AudioBitRate { get; set; }
    public string? FormatName { get; set; }
    public string? Software { get; set; }
    public string? Camera { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }     //record last update time UTC


    public static VideoMetadata CreateFromDataReader(DbDataReader reader)
    {
        return new VideoMetadata
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumImageId = reader.GetInt64(reader.GetOrdinal("album_image_id")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            FileSizeBytes = reader.IsDBNull(reader.GetOrdinal("file_size_bytes")) ? null : reader.GetInt64(reader.GetOrdinal("file_size_bytes")),
            DateTaken = reader.IsDBNull(reader.GetOrdinal("date_taken")) ? null : reader.GetDateTime(reader.GetOrdinal("date_taken")),
            DateModified = reader.IsDBNull(reader.GetOrdinal("date_modified")) ? null : reader.GetDateTime(reader.GetOrdinal("date_modified")),
            Duration = reader.IsDBNull(reader.GetOrdinal("duration")) ? null : reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("duration")),
            VideoWidth = reader.IsDBNull(reader.GetOrdinal("video_width")) ? null : reader.GetInt32(reader.GetOrdinal("video_width")),
            VideoHeight = reader.IsDBNull(reader.GetOrdinal("video_height")) ? null : reader.GetInt32(reader.GetOrdinal("video_height")),
            VideoCodec = reader.IsDBNull(reader.GetOrdinal("video_codec")) ? null : reader.GetString(reader.GetOrdinal("video_codec")),
            AudioCodec = reader.IsDBNull(reader.GetOrdinal("audio_codec")) ? null : reader.GetString(reader.GetOrdinal("audio_codec")),
            PixelFormat = reader.IsDBNull(reader.GetOrdinal("pixel_format")) ? null : reader.GetString(reader.GetOrdinal("pixel_format")),
            FrameRate = reader.IsDBNull(reader.GetOrdinal("frame_rate")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("frame_rate")),
            VideoBitRate = reader.IsDBNull(reader.GetOrdinal("video_bit_rate")) ? null : reader.GetInt64(reader.GetOrdinal("video_bit_rate")),
            AudioSampleRate = reader.IsDBNull(reader.GetOrdinal("audio_sample_rate")) ? null : reader.GetInt32(reader.GetOrdinal("audio_sample_rate")),
            AudioChannels = reader.IsDBNull(reader.GetOrdinal("audio_channels")) ? null : reader.GetInt32(reader.GetOrdinal("audio_channels")),
            AudioBitRate = reader.IsDBNull(reader.GetOrdinal("audio_bit_rate")) ? null : reader.GetInt64(reader.GetOrdinal("audio_bit_rate")),
            FormatName = reader.IsDBNull(reader.GetOrdinal("format_name")) ? null :  reader.GetString(reader.GetOrdinal("format_name")),
            Software =  reader.IsDBNull(  reader.GetOrdinal("software")) ? null : reader.GetString(reader.GetOrdinal("software")),
            Camera = reader.IsDBNull(reader.GetOrdinal("camera")) ? null : reader.GetString(reader.GetOrdinal("camera")),
            LastUpdatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated_utc"))    
        };  
    }    
}
