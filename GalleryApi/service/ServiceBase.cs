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


    protected ImageItemContent GetImageItemContent(string? albumName, GalleryLib.model.album.AlbumContentHierarchical item)
    {
        var image = new ImageItemContent();
        image.Id = item.Id;
        image.Name = item.ItemName;
        image.Description = item.ItemDescription;
        
        string path = item.FeatureItemPath ?? string.Empty;     //get the relative path first                                
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        image.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        image.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));    
        image.ImageUHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.UHD));
        image.ImageOriginalPath = GetUrl(path);
        image.IsMovie = _picturesConfig.IsMovieFile(path);
        image.NavigationPathSegments = albumName != null ? albumName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                                         : new List<string>();
        image.LastUpdatedUtc = item.LastUpdatedUtc;
        image.ItemTimestampUtc = item.ItemTimestampUtc;
        image.ImageExif = item.ImageExif;
        return image;
    }

    protected string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
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

    protected string GetUrl(string path)
    {
        var baseUrl = GetBaseUrl();
        path = path.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");  //make it url
        path = path.Replace("\\", "/");        //normalize to forward slashes            
        return path;
    }
}