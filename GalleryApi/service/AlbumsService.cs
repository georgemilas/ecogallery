using GalleryApi.model;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryApi.service;  
public class AlbumsService
{
    
    private readonly AlbumRepository _albumRepository;
    private readonly PicturesDataConfiguration _picturesConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AlbumsService(AlbumRepository albumRepository, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
    {
        _albumRepository = albumRepository;
        _picturesConfig = picturesConfig;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetBaseUrl()
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


    public async Task<VirtualAlbumContent> SearchContentByExpression(AlbumSearch albumSearch)
    {
        var expr = albumSearch.Expression;        
        var content = await _albumRepository.GetAlbumContentHierarchicalByExpression(expr, groupByPHash: albumSearch.GroupByPHash);    
        //Console.WriteLine($"Debug: Search expression '{expr}' returned {content.Count} items.");

        var valbum = new VirtualAlbumContent();
        valbum.Id = 0;
        valbum.Name = "Search Result";
        valbum.Expression = albumSearch.Expression;
        valbum.Description = content.Any() ? "Search Result" : "No images found";
        valbum.NavigationPathSegments = new List<string>();
        valbum.LastUpdatedUtc = DateTimeOffset.UtcNow;
        valbum.ItemTimestampUtc = DateTimeOffset.UtcNow;
        var item = content.FirstOrDefault();
        string path = item?.FeatureItemPath ?? string.Empty;     //get the relative path first                                
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        valbum.ThumbnailPath = item != null ? GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb)) : string.Empty;
        valbum.ImageHDPath = item != null ? GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD)) : string.Empty;    //save space, did not create hd 1080 path
        
        //Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");            
        valbum.Images = new List<ImageItemContent>();
        foreach (var image in content)
        {            
            var contentImage = GetImageItemContent(null, image);
            valbum.Images.Add(contentImage);
            continue;                        
        }
        return valbum;
    }    




    public async Task<AlbumContentHierarchical> GetAlbumContentHierarchical(long albumId)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalById(albumId);        
        return await GetContent(albumContent.First().ParentAlbumName, albumContent);
    }
    public async Task<AlbumContentHierarchical> GetAlbumContentHierarchical(string? albumName = null)
    {
        var albumContent = albumName != null ? await _albumRepository.GetAlbumContentHierarchicalByName(albumName)
                                             : await _albumRepository.GetRootAlbumContentHierarchical();
        return await GetContent(albumName ?? albumContent.First().ParentAlbumName, albumContent);
    }

    private async Task<AlbumContentHierarchical> GetContent(string? albumName, List<GalleryLib.model.album.AlbumContentHierarchical> albumContent)
    {

        var libAlbum = await _albumRepository.GetAlbumHierarchicalByNameAsync(albumName ?? string.Empty);
        if (libAlbum == null)
        {
            throw new AlbumNotFoundException(albumName);
        }    
        var album = new AlbumContentHierarchical();
        SetAlbumItemContent(album, albumName, libAlbum);
        
        //Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");            
        album.Albums = new List<AlbumItemContent>();
        album.Images = new List<ImageItemContent>();
        foreach (var item in albumContent)
        {
            if (item.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase))
            {
                var contentAlbum = new AlbumItemContent();
                SetAlbumItemContent(contentAlbum, albumName, item);
                album.Albums.Add(contentAlbum);
                continue;
            }
            else {
                var contentImage = GetImageItemContent(albumName, item);
                album.Images.Add(contentImage);
                continue;
            }            
        }
        return album;
    }

    private void SetAlbumItemContent(AlbumItemContent album, string? albumName, GalleryLib.model.album.AlbumContentHierarchical item)
    {
        album.Id = item.Id;
        album.Name = item.ItemName;
        
        string defaultFolderImage = "";
        //get the relative path first
        string path = item.FeatureItemType != null 
                                    ? item.FeatureItemPath ?? item.InnerFeatureItemPath ?? defaultFolderImage
                                    : item.InnerFeatureItemPath ?? defaultFolderImage;
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        
        album.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        album.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));    
        album.NavigationPathSegments = albumName != null ? albumName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                                         : new List<string>();
        album.LastUpdatedUtc = item.LastUpdatedUtc;
        album.ItemTimestampUtc = item.ItemTimestampUtc;                
    }

    private ImageItemContent GetImageItemContent(string? albumName, GalleryLib.model.album.AlbumContentHierarchical item)
    {
        var image = new ImageItemContent();
        image.Id = item.Id;
        image.Name = item.ItemName;
        
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


    private string GetUrl(string path)
    {
        var baseUrl = GetBaseUrl();
        path = path.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");  //make it url
        path = path.Replace("\\", "/");        //normalize to forward slashes            
        return path;
    }
}