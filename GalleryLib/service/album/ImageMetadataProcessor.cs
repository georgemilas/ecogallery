using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.fileProcessor;
using SixLabors.ImageSharp;
using FFMpegCore;

namespace GalleryLib.service.album;

/// <summary>
/// Syncronized the pictures folder and add/edit/delete the coresponding database images exif metadata 
/// in addition to what AlbumProcessor does
/// </summary>
public class ImageMetadataProcessor: AlbumProcessor
{

    public ImageMetadataProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, bool reprocess = false):base(configuration, dbConfig)
    {
        _reprocessMetadata = reprocess;
    }

    /// <summary>
    /// Constructor for testing with mock repositories
    /// </summary>
    public ImageMetadataProcessor(PicturesDataConfiguration configuration, IAlbumImageRepository imageRepo, IAlbumRepository albumRepo, bool reprocess = false)
        : base(configuration, imageRepo, albumRepo)
    {
        _reprocessMetadata = reprocess;
    }


    public static FileObserverService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1, bool reprocessMetadata = false)
    {
        IFileProcessor processor = new ImageMetadataProcessor(configuration, dbConfig, reprocessMetadata);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }

    private readonly bool _reprocessMetadata;

    /// <summary>
    /// create image record and ensure album record exists, extract EXIF and compute thumbnail perceptual hash
    /// </summary>
    protected override async Task<Tuple<AlbumImage, int>> CreateImageAndAlbumRecords(string filePath, bool logIfCreated)
    {
        var (albumImage, count) = await base.CreateImageAndAlbumRecords(filePath, logIfCreated);                       

        if (_configuration.IsMovieFile(filePath))
        {
            var dbVideoMetadata = await imageRepository.GetVideoMetadataAsync(albumImage);
            if (dbVideoMetadata == null || _reprocessMetadata)
            {
                VideoMetadata videoMetadata = await ExtractVideoMetadata(filePath);
                videoMetadata.AlbumImageId = albumImage.Id;
                videoMetadata.FilePath = albumImage.ImagePath;
                videoMetadata.LastUpdatedUtc = DateTimeOffset.UtcNow;
                await imageRepository.UpsertVideoMetadataAsync(videoMetadata);

                // Update album_image dimensions for fast aspect ratio access
                albumImage.ImageWidth = videoMetadata.VideoWidth;
                albumImage.ImageHeight = videoMetadata.VideoHeight;
                albumImage.ImageTimestampUtc = videoMetadata.DateTaken ?? albumImage.ImageTimestampUtc;  // DateTaken or FileLastWrittenDateTime
                albumImage.LastUpdatedUtc = DateTimeOffset.UtcNow;
                await imageRepository.UpdateImageDimensionsAndDateTaken(albumImage);

                if (logIfCreated)
                {
                    Console.WriteLine($"Extracted and stored video metadata in video_metadata table: {filePath}");
                }
                return Tuple.Create(albumImage, count > 0 ? count : 1);
            }
            return Tuple.Create(albumImage, count);
            //TODO: movie hash?
        }                        

        var dbExif = await imageRepository.GetImageMetadataAsync(albumImage);
        if (dbExif == null || _reprocessMetadata)
        {
            ImageMetadata? exif = await ExtractImageMetadata(filePath);
            if (exif != null)
            {
                exif.AlbumImageId = albumImage.Id;
                exif.FilePath = albumImage.ImagePath;
                exif.LastUpdatedUtc = DateTimeOffset.UtcNow;
                await imageRepository.UpsertImageMetadataAsync(exif);

                // Update album_image dimensions for fast aspect ratio access
                albumImage.ImageWidth = exif.ImageWidth;
                albumImage.ImageHeight = exif.ImageHeight;
                albumImage.ImageTimestampUtc = exif.DateTaken ?? albumImage.ImageTimestampUtc;  // DateTaken or FileLastWrittenDateTime
                albumImage.LastUpdatedUtc = DateTimeOffset.UtcNow;
                await imageRepository.UpdateImageDimensionsAndDateTaken(albumImage);

                if (logIfCreated)
                {
                    Console.WriteLine($"Extracted and stored EXIF data in image_metadata table: {filePath}");
                }
                return Tuple.Create(albumImage, count > 0 ? count : 1);
            }
        }
        return Tuple.Create(albumImage, count);

        // Note: ON DELETE CASCADE should take care of exif data removal from album_image_metadata table
        // so no need to override CleanupImageAndAlbumRecords 
    }

    
    public async Task<VideoMetadata> ExtractVideoMetadata(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var videoMetadata = new VideoMetadata
        {
            FileName = fileInfo.Name,
            FilePath = filePath,
            FileSizeBytes = fileInfo.Length,
            DateModified = fileInfo.LastWriteTimeUtc
        };

        try
        {
            // Get comprehensive video information
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);

            // Duration
            videoMetadata.Duration = mediaInfo.Duration;

            // Video stream info (dimensions, codec)
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            if (videoStream != null)
            {
                videoMetadata.VideoWidth = videoStream.Width;
                videoMetadata.VideoHeight = videoStream.Height;
                videoMetadata.PixelFormat = videoStream.PixelFormat;
                videoMetadata.VideoCodec = videoStream.CodecName;
                videoMetadata.FrameRate = (decimal?)videoStream.FrameRate;
                videoMetadata.VideoBitRate = videoStream.BitRate;
                
                // Check for rotation metadata and swap dimensions if rotated 90° or 270°
                // Rotation is often stored in video stream side data
                if (videoStream.Rotation != 0)
                {
                    int rotation = Math.Abs(videoStream.Rotation);
                    videoMetadata.Rotation = rotation;
                    if (rotation == 90 || rotation == 270)
                    {
                        // Swap width and height for rotated videos
                        (videoMetadata.VideoWidth, videoMetadata.VideoHeight) = (videoMetadata.VideoHeight, videoMetadata.VideoWidth);
                    }
                }
            }

            // Audio stream info (codec)
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
            if (audioStream != null)
            {
                videoMetadata.AudioCodec = audioStream.CodecName;
                videoMetadata.AudioSampleRate = audioStream.SampleRateHz;
                videoMetadata.AudioChannels = audioStream.Channels;
                videoMetadata.AudioBitRate = audioStream.BitRate;
            }

            // Metadata tags (creation_time, date taken, camera info, etc.)
            if (mediaInfo.Format.Tags != null)
            {
                TryApplyVideoGpsTags(mediaInfo.Format.Tags, videoMetadata);

                // Creation time from metadata
                if (mediaInfo.Format.Tags.TryGetValue("creation_time", out string? creationTime))
                {
                    if (DateTime.TryParse(creationTime, out var createdDate))
                    {
                        videoMetadata.DateTaken = DateTime.SpecifyKind(createdDate, DateTimeKind.Utc);
                    }
                }

                // Camera information (make and model)
                string? make = null;
                string? model = null;
                
                // Try standard tags
                mediaInfo.Format.Tags.TryGetValue("make", out make);
                mediaInfo.Format.Tags.TryGetValue("model", out model);
                
                // Try Apple QuickTime tags
                if (string.IsNullOrEmpty(make))
                    mediaInfo.Format.Tags.TryGetValue("com.apple.quicktime.make", out make);
                if (string.IsNullOrEmpty(model))
                    mediaInfo.Format.Tags.TryGetValue("com.apple.quicktime.model", out model);
                
                // Build camera string
                if (!string.IsNullOrEmpty(make) && !string.IsNullOrEmpty(model))
                    videoMetadata.Camera = $"{make} {model}";
                else if (!string.IsNullOrEmpty(model))
                    videoMetadata.Camera = model;
                else if (!string.IsNullOrEmpty(make))
                    videoMetadata.Camera = make;
                
                // Fallback to encoder tag if no camera info found
                if (string.IsNullOrEmpty(videoMetadata.Camera) &&
                    mediaInfo.Format.Tags.TryGetValue("encoder", out string? encoder))
                {
                    videoMetadata.Camera = encoder;
                }
            }

            if ((videoMetadata.GpsLatitude == null || videoMetadata.GpsLongitude == null) && videoStream?.Tags != null)
            {
                TryApplyVideoGpsTags(videoStream.Tags, videoMetadata);
            }

            // Format/container info
            videoMetadata.FormatName = mediaInfo.Format.FormatName;
            videoMetadata.Software = mediaInfo.Format.FormatLongName;

            return videoMetadata;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting video metadata from {filePath}: {ex.Message}");
            return videoMetadata;
        }
    }


    public async Task<ImageMetadata?> ExtractImageMetadata(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var directories = ImageMetadataReader.ReadMetadata(filePath);
            
            var exif = new ImageMetadata
            {
                FileName = fileInfo.Name,
                FileSizeBytes = fileInfo.Length,
                DateModified = fileInfo.LastWriteTimeUtc
            };
            
            // Get IFD0 directory (main image info)
            var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0Directory != null)
            {
                exif.Camera = GetTag(ifd0Directory, ExifDirectoryBase.TagModel);
                exif.Software = GetTag(ifd0Directory, ExifDirectoryBase.TagSoftware);
                exif.DateTaken = GetDateTimeTag(ifd0Directory, ExifDirectoryBase.TagDateTime);

                // Image dimensions - try EXIF first, track source for orientation handling
                bool dimensionsFromExif = false;
                int? imageWidth = null;
                int? imageHeight = null;
                if (ifd0Directory.TryGetInt32(ExifDirectoryBase.TagImageWidth, out int width))
                    imageWidth = width;
                if (ifd0Directory.TryGetInt32(ExifDirectoryBase.TagImageHeight, out int height))
                    imageHeight = height;

                dimensionsFromExif = imageWidth != null && imageHeight != null && imageWidth.Value > 0 && imageHeight.Value > 0;
                if (!dimensionsFromExif)
                {
                    // Fallback to loading the image if dimensions are not in EXIF                    
                    using var image = await Image.LoadAsync(filePath);
                    exif.ImageWidth = image.Width;
                    exif.ImageHeight = image.Height;
                }
                else
                {
                    if (imageWidth.HasValue && imageHeight.HasValue)
                    {
                        exif.ImageWidth = imageWidth.Value;
                        exif.ImageHeight = imageHeight.Value;
                    }
                }

                // Check orientation and swap dimensions if needed
                // IMPORTANT: Only swap if dimensions came from EXIF (raw/stored orientation)
                // ImageSharp dimensions are already in display orientation, no swap needed
                // Orientation values 5, 6, 7, 8 require width/height swap (90° or 270° rotation)
                // 1: Normal
                // 2: Flip horizontal
                // 3: Rotate 180°
                // 4: Flip vertical
                // 5: Transpose (flip horizontal + rotate 270°)
                // 6: Rotate 90° clockwise ← Common for vertical photos
                // 7: Transverse (flip horizontal + rotate 90°)
                // 8: Rotate 270° clockwise ← Common for vertical photos
                if (ifd0Directory.TryGetInt32(ExifDirectoryBase.TagOrientation, out int orientation))
                {
                    exif.Orientation = orientation;
                    if (dimensionsFromExif && orientation >= 5 && orientation <= 8)
                    {
                        (exif.ImageWidth, exif.ImageHeight) = (exif.ImageHeight, exif.ImageWidth);
                    }
                }
            }
            else
            {            
                // Fallback to loading the image if no EXIF
                using var image = await Image.LoadAsync(filePath);
                exif.ImageWidth = image.Width;
                exif.ImageHeight = image.Height;                                
            }            

            // Get SubIFD directory (detailed photo info)
            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory != null)
            {
                exif.Lens = GetTag(subIfdDirectory, ExifDirectoryBase.TagLensModel);
                exif.DateTaken = GetDateTimeTag(subIfdDirectory, ExifDirectoryBase.TagDateTimeOriginal) ?? exif.DateTaken;
                
                // Exposure settings
                exif.ExposureTime = GetTag(subIfdDirectory, ExifDirectoryBase.TagExposureTime);
                exif.Aperture = GetTag(subIfdDirectory, ExifDirectoryBase.TagFNumber);
                
                if (subIfdDirectory.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out int iso))
                    exif.Iso = iso;
                
                exif.FocalLength = GetTag(subIfdDirectory, ExifDirectoryBase.TagFocalLength);
                exif.ExposureProgram = GetTag(subIfdDirectory, ExifDirectoryBase.TagExposureProgram);
                exif.ExposureBias = GetTag(subIfdDirectory, ExifDirectoryBase.TagExposureBias);
                exif.ExposureMode = GetTag(subIfdDirectory, ExifDirectoryBase.TagExposureMode);
                exif.MeteringMode = GetTag(subIfdDirectory, ExifDirectoryBase.TagMeteringMode);
                exif.Flash = GetTag(subIfdDirectory, ExifDirectoryBase.TagFlash);
                exif.WhiteBalance = GetTag(subIfdDirectory, ExifDirectoryBase.TagWhiteBalance);
                exif.ColorSpace = GetTag(subIfdDirectory, ExifDirectoryBase.TagColorSpace);
                exif.SceneCaptureType = GetTag(subIfdDirectory, ExifDirectoryBase.TagSceneCaptureType);
                
                // Advanced calculations
                if (subIfdDirectory.TryGetDouble(ExifDirectoryBase.TagSubjectDistance, out double subjectDistance))
                {
                    exif.DepthOfField = (decimal?)subjectDistance;
                }
            }

            // Get maker notes for serial numbers (varies by manufacturer)
            var makernoteDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (makernoteDirectory != null)
            {
                exif.SerialNumber = GetTag(makernoteDirectory, ExifDirectoryBase.TagBodySerialNumber);
                exif.LensSerialNumber = GetTag(makernoteDirectory, ExifDirectoryBase.TagLensSerialNumber);
            }

            var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDir?.TryGetGeoLocation(out var location) == true)
            {
                exif.GpsLatitude = (decimal)location.Latitude;
                exif.GpsLongitude = (decimal)location.Longitude;
                // Altitude in rational format (meters above sea level)
                if (gpsDir.TryGetRational(GpsDirectory.TagAltitude, out var altRational))
                {
                    exif.GpsAltitude = (decimal)altRational.ToDouble();
                }
                // // GPS timestamp
                // if (gpsDir.GetGpsDate(GpsDirectory.TagGpsDateStamp, GpsDirectory.TagGpsTimeStamp) is { } gpsDateTime)
                // {
                //     exif.GpsTimestamp = gpsDateTime;
                // }
            }

            return exif;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting EXIF from {filePath}: {ex.Message}");
            return null;
        }
    }

    private string? GetTag(MetadataExtractor.Directory directory, int tagType)
    {
        try
        {
            return directory.GetDescription(tagType);
        }
        catch
        {
            return null;
        }
    }

    private DateTime? GetDateTimeTag(MetadataExtractor.Directory directory, int tagType)
    {
        try
        {
            if (directory.TryGetDateTime(tagType, out DateTime dateTime))
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void TryApplyVideoGpsTags(IReadOnlyDictionary<string, string> tags, VideoMetadata videoMetadata)
    {
        if (videoMetadata.GpsLatitude != null && videoMetadata.GpsLongitude != null)
        {
            return;
        }

        if (!tags.TryGetValue("location", out var location) || string.IsNullOrWhiteSpace(location))
        {
            tags.TryGetValue("com.apple.quicktime.location.ISO6709", out location);
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            tags.TryGetValue("location-eng", out location);
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        if (TryParseIso6709(location, out var lat, out var lon, out var alt))
        {
            videoMetadata.GpsLatitude = lat;
            videoMetadata.GpsLongitude = lon;
            videoMetadata.GpsAltitude = alt;
        }
    }

    private static bool TryParseIso6709(string value, out decimal? latitude, out decimal? longitude, out decimal? altitude)
    {
        latitude = null;
        longitude = null;
        altitude = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.EndsWith("/", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1];
        }

        if (trimmed.Length < 2 || (trimmed[0] != '+' && trimmed[0] != '-'))
        {
            return false;
        }

        var index = 1;
        var nextSignIndex = FindNextSign(trimmed, index);
        if (nextSignIndex <= 0)
        {
            return false;
        }

        if (!decimal.TryParse(trimmed[..nextSignIndex], out var lat))
        {
            return false;
        }

        index = nextSignIndex;
        nextSignIndex = FindNextSign(trimmed, index + 1);

        if (nextSignIndex > 0)
        {
            if (!decimal.TryParse(trimmed[index..nextSignIndex], out var lon))
            {
                return false;
            }

            latitude = lat;
            longitude = lon;

            if (decimal.TryParse(trimmed[nextSignIndex..], out var alt))
            {
                altitude = alt;
            }

            return true;
        }

        if (!decimal.TryParse(trimmed[index..], out var lonOnly))
        {
            return false;
        }

        latitude = lat;
        longitude = lonOnly;
        return true;
    }

    private static int FindNextSign(string value, int startIndex)
    {
        for (var i = startIndex; i < value.Length; i++)
        {
            if (value[i] == '+' || value[i] == '-')
            {
                return i;
            }
        }

        return -1;
    }
}
