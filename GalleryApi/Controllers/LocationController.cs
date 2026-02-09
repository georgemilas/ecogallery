using Microsoft.AspNetCore.Mvc;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryApi.model;
using GalleryApi.service;
using AlbumContentHierarchical = GalleryLib.model.album.AlbumContentHierarchical;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/locations")]
public class LocationController : ControllerBase
{
    private readonly LocationRepository _locationRepository;
    private readonly AlbumRepository _albumRepository;
    private readonly PicturesDataConfiguration _picturesConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocationController(DatabaseConfiguration dbConfig, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
    {
        _locationRepository = new LocationRepository(dbConfig);
        _albumRepository = new AlbumRepository(picturesConfig, dbConfig);
        _picturesConfig = picturesConfig;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Update the name of a location cluster
    /// </summary>
    [HttpPut("cluster/{clusterId}/name")]
    public async Task<ActionResult> UpdateClusterName(long clusterId, [FromBody] UpdateClusterNameRequest request)
    {
        try
        {
            await _locationRepository.UpdateClusterNameAsync(clusterId, request.Name);
            return Ok(new { success = true, clusterId, name = request.Name });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search for images in a specific location cluster by cluster ID.
    /// </summary>
    [HttpGet("search/cluster/{clusterId}")]
    public async Task<ActionResult<VirtualAlbumContent>> SearchByClusterId(long clusterId)
    {
        try
        {
            var imageIds = await _locationRepository.GetImageIdsByClusterIdAsync(clusterId);
            if (imageIds.Count == 0)
            {
                return Ok(CreateEmptyResult($"No images found for cluster #{clusterId}"));
            }

            var content = await _albumRepository.GetAlbumContentByImageIdsAsync(imageIds);
            var uniqueDataId = $"locations/search/cluster/id:{clusterId}:{AuthenticatedUser?.Id ?? 1}";
            var res = await CreateVirtualAlbumResult(content, $"Cluster #{clusterId}", $"cluster:{clusterId}", uniqueDataId);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search for images in location clusters with a specific name.
    /// </summary>
    [HttpGet("search/name/{clusterName}")]
    public async Task<ActionResult<VirtualAlbumContent>> SearchByClusterName(string clusterName)
    {
        try
        {
            var imageIds = await _locationRepository.GetImageIdsByClusterNameAsync(clusterName);
            if (imageIds.Count == 0)
            {
                return Ok(CreateEmptyResult($"No images found for '{clusterName}'"));
            }

            var content = await _albumRepository.GetAlbumContentByImageIdsAsync(imageIds);
            var uniqueDataId = $"locations/search/name:{clusterName}:{AuthenticatedUser?.Id ?? 1}";
            var res = await CreateVirtualAlbumResult(content, clusterName, $"name:{clusterName}", uniqueDataId);
            return Ok(res);
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
            Name = "Location Search",
            RoleId = 2,
            Description = description,
            NavigationPathSegments = new List<AlbumPathElement>(),
            Images = new List<ImageItemContent>(),
            Albums = new List<AlbumItemContent>(),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ItemTimestampUtc = DateTimeOffset.UtcNow,
            Expression = ""
        };
    }

    private async Task<VirtualAlbumContent> CreateVirtualAlbumResult(List<AlbumContentHierarchical> content, string title, string expression, string uniqueDataId)
    {
        var baseUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
        var images = content.Select(item => new ImageItemContent
        {
            Id = item.Id,
            Name = item.ItemName,
            Description = item.ItemDescription,
            RoleId = item.RoleId,
            ThumbnailPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.Thumb)),
            ImageSmallHDPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.SmallHD)),
            ImageHDPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.HD)),
            ImageUHDPath = GetPicturesUrl(baseUrl, _picturesConfig.GetThumbnailPath(GetFullPath(item.FeatureItemPath), (int)ThumbnailHeights.UHD)),
            ImageOriginalPath = GetPicturesUrl(baseUrl, GetFullPath(item.FeatureItemPath)),
            IsMovie = _picturesConfig.IsMovieFile(item.FeatureItemPath),
            ImageWidth = item.ImageWidth,
            ImageHeight = item.ImageHeight,
            ImageMetadata = item.ImageMetadata,
            VideoMetadata = item.VideoMetadata,
            Faces = item.Faces,
            Locations = item.Locations,
            LastUpdatedUtc = item.LastUpdatedUtc,
            ItemTimestampUtc = item.ItemTimestampUtc,
            NavigationPathSegments = new List<AlbumPathElement>()
        }).ToList();

        var settings = await _albumRepository.GetAlbumSettingsByUniqueDataIdAsync(uniqueDataId);

        return new VirtualAlbumContent
        {
            Id = 0,
            Name = $"Location: {title}",
            Description = $"{images.Count} images found",
            RoleId = 2,
            NavigationPathSegments = new List<AlbumPathElement>(),
            Images = images,
            Albums = new List<AlbumItemContent>(),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ItemTimestampUtc = DateTimeOffset.UtcNow,
            ThumbnailPath = images.FirstOrDefault()?.ThumbnailPath ?? "",
            ImageHDPath = images.FirstOrDefault()?.ImageHDPath ?? "",
            Expression = expression,
            Settings = settings ?? new GalleryLib.model.album.AlbumSettings
            {
                AlbumId = 0,
                UniqueDataId = uniqueDataId,
                IsVirtual = true,
                UserId = AuthenticatedUser?.Id ?? 1
            }
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

    protected GalleryLib.Model.Auth.UserInfo? AuthenticatedUser
    {
        get
        {
            return _httpContextAccessor.HttpContext?.Items["User"] as GalleryLib.Model.Auth.UserInfo;
        }
    }
}

public record UpdateClusterNameRequest
{
    public string? Name { get; init; }
}
