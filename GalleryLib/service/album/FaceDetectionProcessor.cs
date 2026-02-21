using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.service.fileProcessor;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GalleryLib.service.album;


/// <summary>
/// Processor for detecting faces in images, extracting embeddings, and grouping similar faces.
/// Uses UltraFace for detection and ArcFace for embeddings.
/// </summary>
public class FaceDetectionProcessor : EmptyProcessor, IFaceDetectionProcessor
{
    private FaceDetectionService _faceDettectionService;

    public FaceDetectionProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
        : base(configuration)
    {        
        _faceDettectionService = new FaceDetectionService(configuration, dbConfig);        
    }

    public static PeriodicScanService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1, bool planMode = false, bool logIfProcessed = false)
    {
        IFileProcessor processor = new FaceDetectionProcessor(configuration, dbConfig);
        return new DbFacePeriodicScanService(processor, configuration, dbConfig, intervalMinutes: 5, degreeOfParallelism: degreeOfParallelism, logIfProcessed);
    }

    public override bool ShouldCleanFile(FileData dbPath, bool logIfProcess = false)
    {
        return false;
    }

    public string GetOriginalFilePath(FileData dbPath)
    {
        var relativeDbPath = dbPath.FilePath.TrimStart('\\', '/');
        return Path.Combine(RootFolder.FullName, relativeDbPath);
    }
    public AlbumImage? GetAlbumImage(FileData dbPath)
    {
        var albumImage = dbPath.Data as AlbumImage;
        return albumImage;
    }

    public override async Task<int> OnFileCreated(FileData dbPath, bool logIfCreated = false)
    {
        var res = await _faceDettectionService.ProcessFile(dbPath, this, logIfCreated);
        return res;
    }

    public override async Task<int> OnFileDeleted(FileData dbPath, bool logIfCreated = false)
    {
        // Face embeddings are deleted via CASCADE when album_image is deleted
        return 0;
    }

    public override Task OnFileChanged(FileData dbPath)
    {
        return Task.CompletedTask;
    }

    public override async Task OnFileRenamed(FileData oldDbPath, FileData newDbPath, bool newValid)
    {
        await Task.CompletedTask;
    }

    public override async Task<int> OnEnsureCleanupFile(FileData dbPath, bool logIfCleaned = false)
    {
        return 0;
    }


}