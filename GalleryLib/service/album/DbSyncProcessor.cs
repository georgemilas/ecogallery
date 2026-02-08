using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.fileProcessor;
using SixLabors.ImageSharp;

namespace GalleryLib.service.album;

/// <summary>
/// Syncronized the pictures folder and add/edit/delete the coresponding database information 
/// </summary>
public class DbSyncProcessor:  ImageMetadataProcessor
{
    public DbSyncProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, bool reprocess = false):base(configuration, dbConfig, reprocess)
    {
        
    }

    public static new FileObserverService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1, bool reprocess = false)
    {
        IFileProcessor processor = new DbSyncProcessor(configuration, dbConfig, reprocess);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }

}
