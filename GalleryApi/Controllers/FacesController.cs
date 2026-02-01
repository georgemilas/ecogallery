using Microsoft.AspNetCore.Mvc;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryApi.model;
using GalleryApi.service;
using AlbumContentHierarchical = GalleryLib.model.album.AlbumContentHierarchical;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/faces")]
public class FacesController : ControllerBase
{
    private readonly FaceRepository _faceRepository;
    private readonly AlbumRepository _albumRepository;
    private readonly PicturesDataConfiguration _picturesConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FacesController(DatabaseConfiguration dbConfig, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
    {
        _faceRepository = new FaceRepository(dbConfig);
        _albumRepository = new AlbumRepository(picturesConfig, dbConfig);
        _picturesConfig = picturesConfig;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Update the name of a face person
    /// </summary>
    [HttpPut("person/{personId}/name")]
    public async Task<ActionResult> UpdatePersonName(long personId, [FromBody] UpdatePersonNameRequest request)
    {
        try
        {
            var person = await _faceRepository.GetFacePersonByIdAsync(personId);
            if (person == null)
            {
                return NotFound(new { error = $"Person with ID {personId} not found" });
            }

            var updatedPerson = person with { Name = request.Name };
            await _faceRepository.UpdateFacePersonAsync(updatedPerson);

            return Ok(new { success = true, personId, name = request.Name });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a face person by ID
    /// </summary>
    [HttpGet("person/{personId}")]
    public async Task<ActionResult<FacePerson>> GetPerson(long personId)
    {
        try
        {
            var person = await _faceRepository.GetFacePersonByIdAsync(personId);
            if (person == null)
            {
                return NotFound(new { error = $"Person with ID {personId} not found" });
            }

            return Ok(person);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all face persons
    /// </summary>
    [HttpGet("persons")]
    public async Task<ActionResult<List<FacePerson>>> GetAllPersons()
    {
        try
        {
            var persons = await _faceRepository.GetAllFacePersonsAsync();
            return Ok(persons);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get face embedding by ID (useful for getting person info from a face)
    /// </summary>
    [HttpGet("{faceId}")]
    public async Task<ActionResult<FaceBoxInfo>> GetFace(long faceId)
    {
        try
        {
            var face = await _faceRepository.GetFaceEmbeddingByIdAsync(faceId);
            if (face == null)
            {
                return NotFound(new { error = $"Face with ID {faceId} not found" });
            }

            // Get person name if available
            string? personName = null;
            if (face.FacePersonId.HasValue)
            {
                var person = await _faceRepository.GetFacePersonByIdAsync(face.FacePersonId.Value);
                personName = person?.Name;
            }

            var faceBox = FaceBoxInfo.FromFaceEmbedding(face, personName);
            return Ok(faceBox);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search for images containing a specific person by person ID.
    /// </summary>
    [HttpGet("search/person/{personId}")]
    public async Task<ActionResult<VirtualAlbumContent>> SearchByPersonId(long personId)
    {
        try
        {
            var person = await _faceRepository.GetFacePersonByIdAsync(personId);
            var personName = person?.Name ?? $"Person #{personId}";

            var imageIds = await _faceRepository.GetImageIdsByPersonIdAsync(personId);
            if (imageIds.Count == 0)
            {
                return Ok(CreateEmptyResult($"No images found for {personName}"));
            }

            var content = await _albumRepository.GetAlbumContentByImageIdsAsync(imageIds);
            return Ok(CreateVirtualAlbumResult(content, personName, $"person:{personId}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search for images containing persons with a specific name.
    /// This searches across all person IDs that share the same name.
    /// </summary>
    [HttpGet("search/name/{personName}")]
    public async Task<ActionResult<VirtualAlbumContent>> SearchByPersonName(string personName)
    {
        try
        {
            var imageIds = await _faceRepository.GetImageIdsByPersonNameAsync(personName);
            if (imageIds.Count == 0)
            {
                return Ok(CreateEmptyResult($"No images found for '{personName}'"));
            }

            var content = await _albumRepository.GetAlbumContentByImageIdsAsync(imageIds);
            return Ok(CreateVirtualAlbumResult(content, personName, $"name:{personName}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private VirtualAlbumContent CreateEmptyResult(string description)
    {
        return new VirtualAlbumContent
        {
            Id = 0,
            Name = "Face Search",
            Description = description,
            NavigationPathSegments = new List<AlbumPathElement>(),
            Images = new List<ImageItemContent>(),
            Albums = new List<AlbumItemContent>(),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ItemTimestampUtc = DateTimeOffset.UtcNow,
            Expression = ""
        };
    }

    private VirtualAlbumContent CreateVirtualAlbumResult(List<AlbumContentHierarchical> content, string personName, string expression)
    {
        var baseUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
        var images = content.Select(item => new ImageItemContent
        {
            Id = item.Id,
            Name = item.ItemName,
            Description = item.ItemDescription,
            ThumbnailPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.Thumb)),
            ImageHDPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.HD)),
            ImageUHDPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.UHD)),
            ImageOriginalPath = GetPicturesUrl(baseUrl, GetFullPath(item.FeatureItemPath)),
            IsMovie = _picturesConfig.IsMovieFile(item.FeatureItemPath),
            ImageWidth = item.ImageWidth,
            ImageHeight = item.ImageHeight,
            ImageMetadata = item.ImageMetadata,
            VideoMetadata = item.VideoMetadata,
            Faces = item.Faces,  //.Select(f => FaceBoxInfo.FromFaceEmbedding(f)).ToList(),
            LastUpdatedUtc = item.LastUpdatedUtc,
            ItemTimestampUtc = item.ItemTimestampUtc,
            NavigationPathSegments = new List<AlbumPathElement>()
        }).ToList();

        return new VirtualAlbumContent
        {
            Id = 0,
            Name = $"Photos of {personName}",
            Description = $"{images.Count} images found",
            NavigationPathSegments = new List<AlbumPathElement>(),
            Images = images,
            Albums = new List<AlbumItemContent>(),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ItemTimestampUtc = DateTimeOffset.UtcNow,
            ThumbnailPath = images.FirstOrDefault()?.ThumbnailPath ?? "",
            ImageHDPath = images.FirstOrDefault()?.ImageHDPath ?? "",
            Expression = expression
        };
    }

    private string GetFullPath(string relativePath)
    {
        var path = relativePath.TrimStart('\\', '/');
        return Path.Combine(_picturesConfig.RootFolder.FullName, path);
    }

    private string GetPicturesUrl(string baseUrl, string path)
    {
        path = path.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");
        path = path.Replace("\\", "/");
        return path;
    }
}

public record UpdatePersonNameRequest
{
    public string? Name { get; init; }
}