using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using SixLabors.ImageSharp;

namespace GalleryLib.service.album;

/// <summary>
/// Syncronized the pictures folder and add/edit/delete the coresponding database images exif metadata 
/// in addition to what AlbumProcessor does
/// </summary>
public class ImageExifProcessor: AlbumProcessor
{

    public ImageExifProcessor(PicturesDataConfiguration configuration):base(configuration)
    {
        
    }

    public static new FileObserverService CreateProcessor(PicturesDataConfiguration configuration, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new ImageExifProcessor(configuration);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
    public static new FileObserverServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration)
    {
        IFileProcessor processor = new ImageExifProcessor(configuration);
        return new FileObserverServiceNotParallel(processor,intervalMinutes: 2);
    }


    /// <summary>
    /// create image record and ensure album record exists
    /// </summary>
    protected override async Task<Tuple<AlbumImage, int>> CreateImageAndAlbumRecords(string filePath, bool logIfCreated)
    {
        var (albumImage, count) = await base.CreateImageAndAlbumRecords(filePath, logIfCreated);
        if (_configuration.IsMovieFile(filePath))
        {
            return Tuple.Create(albumImage, count); //skip exif for movie files
        }
        var dbExif = await imageRepository.GetImageExifAsync(albumImage);
        if (dbExif == null)
        {
            ImageExif? exif = await ExtractExif(filePath); 
            if (exif != null)
            {
                exif.AlbumImageId = albumImage.Id;
                exif.FilePath = albumImage.ImagePath;
                exif.LastUpdatedUtc = DateTimeOffset.UtcNow;
                await imageRepository.AddNewImageExifAsync(exif);            
                if (logIfCreated)
                {
                    Console.WriteLine($"Extracted and stored EXIF data in image_exif table: {filePath}");
                }
                return Tuple.Create(albumImage, count + 1);
            }
        }
        return Tuple.Create(albumImage, count);

        // Note: ON DELETE CASCADE should take care of efix data removal from album_image_exif table
        // so no need to override CleanupImageAndAlbumRecords 
    }

    
    public async Task<ImageExif?> ExtractExif(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var directories = ImageMetadataReader.ReadMetadata(filePath);
            
            var exif = new ImageExif
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
                
                // Image dimensions
                if (ifd0Directory.TryGetInt32(ExifDirectoryBase.TagImageWidth, out int width))
                    exif.ImageWidth = width;
                if (ifd0Directory.TryGetInt32(ExifDirectoryBase.TagImageHeight, out int height))
                    exif.ImageHeight = height;
                if (exif.ImageWidth == null || exif.ImageHeight == null)
                {
                    // Fallback to loading the image if dimensions are not in EXIF                
                    using var image = await Image.LoadAsync(filePath);
                    exif.ImageWidth = image.Width;
                    exif.ImageHeight = image.Height;
                }
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
}
