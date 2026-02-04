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
                return NotFound(new { error = $"Person {personId} not found" });
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
    /// Delete a face person by ID
    /// </summary>
    [HttpDelete("person/{personId}")]
    public async Task<ActionResult<FacePerson>> DeletePerson(long personId)
    {
        try
        {
            var cnt = await _faceRepository.DeleteFacePersonByIdAsync(personId);
            if (cnt == 0)
            {
                return NotFound(new { error = $"Person {personId} not found" });
            }
            return Ok(new { success = true, personId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top N named persons ordered by image count (descending).
    /// </summary>
    [HttpGet("persons/top")]
    public async Task<ActionResult<List<PersonWithImageCountResponse>>> GetTopNamedPersons([FromQuery] int limit = 20)
    {
        try
        {
            var baseUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
            var persons = await _faceRepository.GetTopNamedPersonsAsync(limit);

            var response = persons.Select(p => new PersonWithImageCountResponse
            {
                Name = p.Name,
                ImageCount = p.ImageCount,
                ThumbnailPath = p.ThumbnailPath != null
                    ? GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(p.ThumbnailPath), (int)ThumbnailHeights.Thumb))
                    : null,
                ImageWidth = p.ImageWidth,
                ImageHeight = p.ImageHeight,
                BoundingBoxX = p.BoundingBoxX,
                BoundingBoxY = p.BoundingBoxY,
                BoundingBoxWidth = p.BoundingBoxWidth,
                BoundingBoxHeight = p.BoundingBoxHeight
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete face embedding record by ID
    /// </summary>
    [HttpDelete("{faceId}")]
    public async Task<ActionResult<FaceBoxInfo>> DeleteFace(long faceId)
    {
        try
        {
            var cnt = await _faceRepository.DeleteFaceEmbeddingByIdAsync(faceId);
            if (cnt == 0)
            {
                return NotFound(new { error = $"Face {faceId} not found" });
            }
            return Ok(new { success = true, faceId });            
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
            Console.WriteLine(ex);
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

public record PersonWithImageCountResponse
{
    public required string Name { get; init; }
    public int ImageCount { get; init; }
    public string? ThumbnailPath { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public float? BoundingBoxX { get; init; }
    public float? BoundingBoxY { get; init; }
    public float? BoundingBoxWidth { get; init; }
    public float? BoundingBoxHeight { get; init; }
}