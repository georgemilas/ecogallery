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
public class DbSyncProcessor:  ImageExifProcessor
{
    public DbSyncProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig):base(configuration, dbConfig)
    {
        
    }

    public static new FileObserverService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1)
    {
        IFileProcessor processor = new DbSyncProcessor(configuration, dbConfig);
        return new FileObserverService(processor,intervalMinutes: 2, degreeOfParallelism: degreeOfParallelism);
    }
    public static new FileObserverServiceNotParallel CreateProcessorNotParallel(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
    {
        IFileProcessor processor = new DbSyncProcessor(configuration, dbConfig);
        return new FileObserverServiceNotParallel(processor,intervalMinutes: 2);
    }
}
