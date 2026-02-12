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


/*

Face Recognition Pipeline Overview:

1. Face Detection (UltraFace ONNX):
   - Input: Image resized to 320x240 (or 640x480 for higher accuracy)
   - Output: Bounding boxes with confidence scores
   - For each image, detect all face bounding boxes

2. Face Embedding (ArcFace ONNX):
   - Input: Cropped and aligned face, resized to 112x112, normalized
   - Output: 512-dimensional feature vector (embedding)
   - For each detected face, extract embedding

3. Face Grouping/Clustering:
   - Compare embeddings using cosine similarity
   - Threshold: ~0.5-0.6 for same person (tune based on your needs)
   - Group faces with similar embeddings as same person
   - Optionally use DBSCAN or Agglomerative Clustering for automatic grouping

Model Files Required (place in models/ directory next to the executable):
   - ultraface-RFB-320.onnx (or ultraface.onnx)
   - arcface-w600k-r50.onnx (or arcface.onnx)

Download models from:
   - UltraFace: https://github.com/onnx/models/tree/main/validated/vision/body_analysis/ultraface
   - ArcFace: https://github.com/onnx/models/tree/main/validated/vision/body_analysis/arcface

*/


/// <summary>
/// Processor for detecting faces in images, extracting embeddings, and grouping similar faces.
/// Uses UltraFace for detection and ArcFace for embedding generation.
/// </summary>
public class FaceDetectionProcessor : EmptyProcessor
{
    private readonly InferenceSession? _faceDetectorSession;
    private readonly InferenceSession? _faceEmbeddingSession;
    private readonly FaceRepository _faceRepository;
    private readonly bool _modelsAvailable;

    // UltraFace model parameters (320x240 version)
    private const int DetectionWidth = 320;
    private const int DetectionHeight = 240;
    private const float ConfidenceThreshold = 0.7f;
    private const float NmsThreshold = 0.3f;

    // ArcFace model parameters
    private const int EmbeddingInputSize = 112;
    private const int EmbeddingDimension = 512;

    // Similarity threshold for matching faces to existing persons
    private const float SimilarityThreshold = 0.5f;

    public FaceDetectionProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
        : base(configuration)
    {
        _faceRepository = new FaceRepository(dbConfig);

        // Try to load ONNX models - they should be in the models/ subdirectory
        //var modelsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
        var modelsPath = FindDataFolder() ?? ".";

        var detectorPath = Path.Combine(modelsPath, "models", "ultraface-version-RFB-320.onnx");
        var embeddingPath = Path.Combine(modelsPath, "models", "arcface_w600k_r50.onnx");

        try
        {
            if (File.Exists(detectorPath) && File.Exists(embeddingPath))
            {
                var sessionOptions = CreateSessionOptionsWithGpuIfAvailable();

                _faceDetectorSession = new InferenceSession(detectorPath, sessionOptions);
                _faceEmbeddingSession = new InferenceSession(embeddingPath, sessionOptions);
                _modelsAvailable = true;
                Console.WriteLine($"Face detection models loaded successfully from {modelsPath}/models/");
            }
            else
            {
                Console.WriteLine($"Face detection models not found. Expected:");
                Console.WriteLine($"  - {detectorPath}");
                Console.WriteLine($"  - {embeddingPath}");
                _modelsAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load face detection models: {ex.Message}");
            _modelsAvailable = false;
        }
    }

    public static PeriodicScanService CreateProcessor(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig, int degreeOfParallelism = -1, bool planMode = false, bool logIfProcessed = false)
    {
        IFileProcessor processor = new FaceDetectionProcessor(configuration, dbConfig);
        return new DbPeriodicScanService(processor, configuration, dbConfig, intervalMinutes: 5, degreeOfParallelism: degreeOfParallelism, logIfProcessed);
    }

    /// <summary>
    /// Creates SessionOptions with GPU acceleration if available, falling back to CPU.
    /// Tries DirectML first (works with any GPU on Windows), then CUDA for NVIDIA GPUs.
    /// </summary>
    private static SessionOptions CreateSessionOptionsWithGpuIfAvailable()
    {
        var sessionOptions = new SessionOptions();
        // Suppress ONNX Runtime warnings about initializers in graph inputs
        sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;

        // Try DirectML first (Windows, works with any GPU: NVIDIA, AMD, Intel)
        try
        {
            sessionOptions.AppendExecutionProvider_DML(0);
            Console.WriteLine("Face detection: Using DirectML GPU acceleration");
            return sessionOptions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Face detection: DirectML not available ({ex.Message})");
        }

        // Try CUDA for NVIDIA GPUs
        try
        {
            sessionOptions = new SessionOptions { LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR };
            sessionOptions.AppendExecutionProvider_CUDA(0);
            Console.WriteLine("Face detection: Using CUDA GPU acceleration");
            return sessionOptions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Face detection: CUDA not available ({ex.Message})");
        }

        // Fall back to CPU
        Console.WriteLine("Face detection: Using CPU (no GPU acceleration available)");
        return new SessionOptions { LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR };
    }

    /// <summary>
    /// Find the models folder by searching up the directory tree for the solution root
    /// </summary>
    private static string? FindDataFolder()
    {
        // Start from the current application directory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        
        // Search up the directory tree for the solution root (containing .sln file)
        while (currentDir != null)
        {
            // Look for .sln file to identify solution root
            if (currentDir.GetFiles("*.sln").Any())
            {
                var modelsPath = Path.Combine(currentDir.FullName, "data");
                if (Directory.Exists(modelsPath))
                {
                    return modelsPath;
                }
            }
            currentDir = currentDir.Parent;
        }
        
        // Fallback: try relative to current working directory
        var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        if (Directory.Exists(fallbackPath))
        {
            return fallbackPath;
        }
        
        // Final fallback: try relative to project (when running from GalleryService)
        fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "data");
        if (Directory.Exists(fallbackPath))
        {
            return Path.GetFullPath(fallbackPath);
        }
        
        return null;
    }

    public override bool ShouldCleanFile(FileData dbPath, bool logIfProcess = false)
    {
        return false;
    }

    private string GetOriginalFilePath(string dbPath)
    {
        var relativeDbPath = dbPath.TrimStart('\\', '/');
        return Path.Combine(RootFolder.FullName, relativeDbPath);
    }

    public override async Task<int> OnFileCreated(FileData dbPath, bool logIfCreated = false)
    {
        if (!_modelsAvailable) return 0;
        if (_configuration.IsMovieFile(dbPath.FilePath)) return 0;

        var file = GetOriginalFilePath(dbPath.FilePath);
        if (!File.Exists(file)) return 0;

        // Get album_image from FileData (DbPeriodicScanService passes AlbumImage as Data)
        var albumImage = dbPath.Data as AlbumImage;
        if (albumImage == null)
        {
            if (logIfCreated) Console.WriteLine($"Face detection: No album_image data for {dbPath.FilePath}");
            return 0;
        }

        // Check if we already processed this image
        if (await _faceRepository.HasFaceEmbeddingsForImageAsync(albumImage.Id))
        {
            if (logIfCreated) Console.WriteLine($"Face detection: Already processed {dbPath.FilePath}");
            return 0;
        }

        try
        {
            return await ProcessImageForFaces(file, albumImage.Id, logIfCreated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Face detection error for {file}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Main processing pipeline: detect faces, extract embeddings, match/create persons, store results.
    /// </summary>
    private async Task<int> ProcessImageForFaces(string imagePath, long albumImageId, bool log = false)
    {
        // Step 1: Detect faces in the image
        var detections = DetectFaces(imagePath);
        if (detections.Count == 0)
        {
            if (log) Console.WriteLine($"No faces detected in {imagePath}");
            return 0;
        }

        if (log) Console.WriteLine($"Detected {detections.Count} face(s) in {imagePath}");

        var facesProcessed = 0;

        // Load the original image for face cropping
        using var image = await Image.LoadAsync<Rgb24>(imagePath);

        foreach (var detection in detections)
        {
            try
            {
                // Step 2: Crop and align the face for embedding extraction
                var faceImage = CropAndAlignFace(image, detection);
                if (faceImage == null) continue;

                // Step 3: Extract embedding using ArcFace
                var embedding = GetFaceEmbedding(faceImage);
                faceImage.Dispose();

                if (embedding == null || embedding.Length != EmbeddingDimension)
                {
                    if (log) Console.WriteLine($"  Failed to extract embedding for face at {detection.BoundingBox}");
                    continue;
                }

                // Step 4: Find matching person or create new one
                var (matchedPerson, similarity) = await _faceRepository.FindMostSimilarPersonAsync(embedding, SimilarityThreshold);

                long? personId = null;
                if (matchedPerson != null)
                {
                    // Match found - assign to existing person
                    personId = matchedPerson.Id;
                    await _faceRepository.IncrementFaceCountAsync(matchedPerson.Id);

                    // Update representative embedding with running average
                    if (matchedPerson.RepresentativeEmbedding != null)
                    {
                        var updatedEmbedding = FaceRepository.AverageEmbedding(
                            new[] { matchedPerson.RepresentativeEmbedding, embedding });
                        matchedPerson = matchedPerson with
                        {
                            RepresentativeEmbedding = updatedEmbedding,
                            FaceCount = matchedPerson.FaceCount + 1
                        };
                        await _faceRepository.UpdateFacePersonAsync(matchedPerson);
                    }

                    if (log) Console.WriteLine($"  Face matched to person {matchedPerson.Id} " +
                        $"(name: {matchedPerson.Name ?? "unlabeled"}, similarity: {similarity:F3})");
                }
                else
                {
                    // No match - create new person cluster
                    var newPerson = new FacePerson
                    {
                        Name = null, // User can label later
                        RepresentativeEmbedding = embedding,
                        FaceCount = 1
                    };
                    personId = await _faceRepository.InsertFacePersonAsync(newPerson);
                    if (log) Console.WriteLine($"  Created new person cluster {personId}");
                }

                // Step 5: Store the face embedding with reference to person
                var faceEmbedding = new FaceEmbedding
                {
                    AlbumImageId = albumImageId,
                    FacePersonId = personId,
                    Embedding = embedding,
                    BoundingBoxX = detection.BoundingBox.X,
                    BoundingBoxY = detection.BoundingBox.Y,
                    BoundingBoxWidth = detection.BoundingBox.Width,
                    BoundingBoxHeight = detection.BoundingBox.Height,
                    Confidence = detection.Confidence,
                    IsConfirmed = false
                };

                await _faceRepository.InsertFaceEmbeddingAsync(faceEmbedding);
                facesProcessed++;
            }
            catch (Exception ex)
            {
                if (log) Console.WriteLine($"  Error processing face: {ex.Message}");
            }
        }

        return facesProcessed > 0 ? 1 : 0; // count if image has faces not how many faces 
    }

    #region Face Detection (UltraFace)

    /// <summary>
    /// Detect faces in an image using UltraFace ONNX model.
    /// UltraFace outputs: boxes (N, 4) and scores (N, 2) where N is number of prior boxes.
    /// </summary>
    private List<FaceDetectionResult> DetectFaces(string imagePath)
    {
        if (_faceDetectorSession == null) return new List<FaceDetectionResult>();

        using var image = Image.Load<Rgb24>(imagePath);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Resize to model input size (320x240 for UltraFace-RFB-320)
        image.Mutate(x => x.Resize(DetectionWidth, DetectionHeight));

        // Convert to tensor [1, 3, H, W] with normalization to [-1, 1] range
        // UltraFace expects: (pixel - 127) / 128
        var tensor = new DenseTensor<float>(new[] { 1, 3, DetectionHeight, DetectionWidth });
        for (int y = 0; y < DetectionHeight; y++)
        {
            for (int x = 0; x < DetectionWidth; x++)
            {
                var pixel = image[x, y];
                tensor[0, 0, y, x] = (pixel.R - 127f) / 128f;
                tensor[0, 1, y, x] = (pixel.G - 127f) / 128f;
                tensor[0, 2, y, x] = (pixel.B - 127f) / 128f;
            }
        }

        // Run inference
        var inputName = _faceDetectorSession.InputNames.FirstOrDefault() ?? "input";
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        using var results = _faceDetectorSession.Run(inputs);
        var resultList = results.ToList();

        // UltraFace outputs: "scores" (N, 2) and "boxes" (N, 4)
        // boxes are in format [x1, y1, x2, y2] normalized to [0, 1]
        var scoresOutput = resultList.FirstOrDefault(r =>
                r.Name.Contains("score", StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains("confidence", StringComparison.OrdinalIgnoreCase))
            ?? resultList.ElementAtOrDefault(0);

        var boxesOutput = resultList.FirstOrDefault(r =>
                r.Name.Contains("box", StringComparison.OrdinalIgnoreCase))
            ?? resultList.ElementAtOrDefault(1);

        if (scoresOutput == null || boxesOutput == null)
        {
            Console.WriteLine($"UltraFace: Could not find scores/boxes outputs. Available: {string.Join(", ", resultList.Select(r => r.Name))}");
            return new List<FaceDetectionResult>();
        }

        var scores = scoresOutput.AsEnumerable<float>().ToArray();
        var boxes = boxesOutput.AsEnumerable<float>().ToArray();

        // Parse detections
        var detections = new List<FaceDetectionResult>();
        var numBoxes = boxes.Length / 4;

        for (int i = 0; i < numBoxes; i++)
        {
            // Score for "face" class (index 1, index 0 is background)
            var scoreIndex = i * 2 + 1;
            if (scoreIndex >= scores.Length) break;

            var confidence = scores[scoreIndex];
            if (confidence < ConfidenceThreshold) continue;

            // Box coordinates (normalized to [0, 1])
            var boxIndex = i * 4;
            var x1 = boxes[boxIndex];
            var y1 = boxes[boxIndex + 1];
            var x2 = boxes[boxIndex + 2];
            var y2 = boxes[boxIndex + 3];

            // Convert to original image coordinates
            var x = (int)(x1 * originalWidth);
            var y = (int)(y1 * originalHeight);
            var width = (int)((x2 - x1) * originalWidth);
            var height = (int)((y2 - y1) * originalHeight);

            // Clamp to image bounds
            x = Math.Max(0, Math.Min(x, originalWidth - 1));
            y = Math.Max(0, Math.Min(y, originalHeight - 1));
            width = Math.Min(width, originalWidth - x);
            height = Math.Min(height, originalHeight - y);

            // Filter out tiny faces (minimum 20x20 pixels)
            if (width > 20 && height > 20)
            {
                detections.Add(new FaceDetectionResult
                {
                    BoundingBox = new model.album.Rectangle(x, y, width, height),
                    Confidence = confidence
                });
            }
        }

        // Apply Non-Maximum Suppression to remove overlapping detections
        return ApplyNms(detections, NmsThreshold);
    }

    /// <summary>
    /// Non-Maximum Suppression to remove overlapping face detections.
    /// </summary>
    private static List<FaceDetectionResult> ApplyNms(List<FaceDetectionResult> detections, float threshold)
    {
        if (detections.Count == 0) return detections;

        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();
        var keep = new List<FaceDetectionResult>();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            keep.Add(best);
            sorted.RemoveAt(0);

            // Remove all boxes with high overlap with the best box
            sorted = sorted.Where(d => IoU(best.BoundingBox, d.BoundingBox) < threshold).ToList();
        }

        return keep;
    }

    /// <summary>
    /// Calculate Intersection over Union (IoU) for two bounding boxes.
    /// </summary>
    private static float IoU(model.album.Rectangle a, model.album.Rectangle b)
    {
        var xA = Math.Max(a.X, b.X);
        var yA = Math.Max(a.Y, b.Y);
        var xB = Math.Min(a.Right, b.Right);
        var yB = Math.Min(a.Bottom, b.Bottom);

        var interArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);
        var boxAArea = a.Width * a.Height;
        var boxBArea = b.Width * b.Height;

        var unionArea = boxAArea + boxBArea - interArea;
        return unionArea > 0 ? (float)interArea / unionArea : 0;
    }

    #endregion

    #region Face Embedding (ArcFace)

    /// <summary>
    /// Crop and optionally align face for ArcFace input.
    /// Adds padding around the bounding box for better embedding quality.
    /// </summary>
    private Image<Rgb24>? CropAndAlignFace(Image<Rgb24> image, FaceDetectionResult detection)
    {
        var box = detection.BoundingBox;

        // Add padding (20% on each side) to include forehead and chin
        var padX = (int)(box.Width * 0.2);
        var padY = (int)(box.Height * 0.2);

        var x = Math.Max(0, box.X - padX);
        var y = Math.Max(0, box.Y - padY);
        var width = Math.Min(box.Width + 2 * padX, image.Width - x);
        var height = Math.Min(box.Height + 2 * padY, image.Height - y);

        // Skip faces that are too small after padding
        if (width < 30 || height < 30) return null;

        // Crop face region
        var cropRect = new SixLabors.ImageSharp.Rectangle(x, y, width, height);
        var faceImage = image.Clone(ctx => ctx.Crop(cropRect));

        // Resize to ArcFace input size (112x112)
        faceImage.Mutate(ctx => ctx.Resize(EmbeddingInputSize, EmbeddingInputSize));

        return faceImage;
    }

    /// <summary>
    /// Extract 512-dimensional face embedding using ArcFace model.
    /// ArcFace expects input normalized to [-1, 1] range.
    /// </summary>
    private float[]? GetFaceEmbedding(Image<Rgb24> faceImage)
    {
        if (_faceEmbeddingSession == null) return null;

        // Create tensor [1, 3, 112, 112]
        var tensor = new DenseTensor<float>(new[] { 1, 3, EmbeddingInputSize, EmbeddingInputSize });

        for (int y = 0; y < EmbeddingInputSize; y++)
        {
            for (int x = 0; x < EmbeddingInputSize; x++)
            {
                var pixel = faceImage[x, y];
                // ArcFace normalization: (pixel - 127.5) / 127.5 to range [-1, 1]
                tensor[0, 0, y, x] = (pixel.R - 127.5f) / 127.5f;
                tensor[0, 1, y, x] = (pixel.G - 127.5f) / 127.5f;
                tensor[0, 2, y, x] = (pixel.B - 127.5f) / 127.5f;
            }
        }

        // Run inference
        var inputName = _faceEmbeddingSession.InputNames.FirstOrDefault() ?? "input";
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        using var results = _faceEmbeddingSession.Run(inputs);
        var embedding = results.First().AsEnumerable<float>().ToArray();

        // L2 normalize the embedding (required for cosine similarity)
        return L2Normalize(embedding);
    }

    /// <summary>
    /// L2 normalize a vector to unit length.
    /// </summary>
    private static float[] L2Normalize(float[] vector)
    {
        var sumSquares = vector.Sum(v => v * v);
        var norm = MathF.Sqrt(sumSquares);
        if (norm < 1e-10f) return vector;
        return vector.Select(v => v / norm).ToArray();
    }

    #endregion

    #region Processor Overrides

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

    #endregion
}