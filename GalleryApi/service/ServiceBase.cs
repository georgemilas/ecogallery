using System.Security.Cryptography;
using System.Text;
using GalleryApi.model;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryApi.service;  
public class ServiceBase
{
    
    protected readonly AlbumRepository _albumRepository;
    protected readonly PicturesDataConfiguration _picturesConfig;
    protected readonly IHttpContextAccessor _httpContextAccessor;   

    public ServiceBase(AlbumRepository albumRepository, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
    {
        _albumRepository = albumRepository;
        _picturesConfig = picturesConfig;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Access the authenticated user (if any) 
    /// </summary>
    protected GalleryLib.Model.Auth.UserInfo? AuthenticatedUser
    {
        get
        {
            return _httpContextAccessor.HttpContext?.Items["User"] as GalleryLib.Model.Auth.UserInfo;
        }
    }

    public async Task<GalleryLib.model.album.AlbumSettings> SaveAlbumSettingsAsync(GalleryLib.model.album.AlbumSettings settings)
    {
        settings.LastUpdatedUtc = DateTimeOffset.UtcNow;
        var savedSettings = await _albumRepository.AddOrUpdateAlbumSettingsAsync(settings);
        return savedSettings;
    }


    protected ImageItemContent GetImageItemContent(GalleryLib.model.album.AlbumContentHierarchical item)
    {
        var image = new ImageItemContent();
        image.Id = item.Id;
        image.Name = item.ItemName;
        image.Description = item.ItemDescription;
        image.RoleId = item.RoleId;
        
        string path = item.FeatureItemPath ?? string.Empty;     //get the relative path first                                
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        image.ThumbnailPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        image.ImageSmallHDPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.SmallHD));
        image.ImageHDPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));    
        image.ImageUHDPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.UHD));
        image.ImageOriginalPath = GetPicturesUrl(path);
        image.IsMovie = _picturesConfig.IsMovieFile(path);
        
        image.LastUpdatedUtc = item.LastUpdatedUtc;
        image.ItemTimestampUtc = item.ItemTimestampUtc;
        image.ImageWidth = item.ImageWidth;
        image.ImageHeight = item.ImageHeight;
        image.ImageMetadata = item.ImageMetadata;
        image.VideoMetadata = item.VideoMetadata;
        image.Faces = item.Faces; //.Select(f => GalleryLib.model.album.FaceBoxInfo.FromFaceEmbedding(f)).ToList();
        return image;
    }

    public string GetBaseUrl()
    {
        return GetBaseUrl(_httpContextAccessor);
    }
    public static string GetBaseUrl(IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;        
        
        // Respect forwarded headers when the app is behind a proxy        
        //Console.WriteLine($"Debug: {request.Headers["X-Forwarded-Proto"]}/{request.Headers["X-Forwarded-Host"]} | {request.Headers["Origin"]} | {request.Scheme}://{request.Host.Value}");
        var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ;
        var host = request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (scheme != null && host != null)
        {
            return $"{scheme}://{host}".TrimEnd('/');
        }
        var origin = request.Headers["Origin"].FirstOrDefault();
        if (origin != null)
        {
            origin = origin.Replace(":3000", ":5001");  //adjust for frontend dev server port
            return origin.TrimEnd('/');
        }   
        return $"{request.Scheme}://{request.Host.Value}";        
    }

    protected string GetPicturesUrl(string path)
    {
        var baseUrl = GetBaseUrl();
        path = path.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");  //make it url
        path = path.Replace("\\", "/");        //normalize to forward slashes            
        return path;
    }

    /// <summary>
    /// Generate a short hash from a string (for search_id)
    /// </summary>
    protected static string GenerateSearchId(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant(); // First 16 chars = 64 bits
    }

}