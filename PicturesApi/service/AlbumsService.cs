using PicturesApi.model;
using PicturesLib.model.configuration;
using PicturesLib.repository;
using Microsoft.AspNetCore.Http;

namespace PicturesApi.service;  
public class AlbumsService
{
    
    private readonly AlbumRepository _albumRepository;
    private readonly PicturesDataConfiguration _picturesConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AlbumsService(PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
    {
        _picturesConfig = picturesConfig;
        _httpContextAccessor = httpContextAccessor;
        _albumRepository = new AlbumRepository(_picturesConfig);    
    }

    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;
        
        return $"{request.Scheme}://{request.Host.Value}";
    }

    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchical(long albumId)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalById(albumId);        
        return GetContent(albumContent.First().ParentAlbumName, albumContent);
    }
    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchical(string? albumName = null)
    {
        var albumContent = albumName != null ? await _albumRepository.GetAlbumContentHierarchicalByName(albumName)
                                             : await _albumRepository.GetRootAlbumContentHierarchical();
        return GetContent(albumName, albumContent);
    }

    private List<AlbumContentHierarchical> GetContent(string? albumName, List<PicturesLib.model.album.AlbumContentHierarchical> albumContent)
    {
        Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");            
        var result = new List<AlbumContentHierarchical>();
        foreach (var item in albumContent)
        {
            var album = new AlbumContentHierarchical();
            album.Id = item.Id;
            album.Name = item.ItemName;
            album.IsAlbum = item.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase);

            string defaultFolderImage = "\\andr si anth.jpg";
            string path = album.IsAlbum     //get the relative path first
                    ? (item.FeatureItemType != null ? item.FeatureItemPath ?? item.InnerFeatureItemPath ?? defaultFolderImage
                                                    : item.InnerFeatureItemPath ?? defaultFolderImage)
                    : (item.FeatureItemPath ?? string.Empty);
            Console.WriteLine($"Debug: relative path {path}");
            path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
            path = Path.Combine(_picturesConfig.RootFolder.FullName, path);  //then make it absolute 
            Console.WriteLine($"Debug: absolute path {path}");
            var thumbPath = _picturesConfig.GetThumbnailPath(path, 500);   //get the thumbnail path from the absolute path

            var baseUrl = GetBaseUrl();
            thumbPath = thumbPath.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");  //make it url
            thumbPath = thumbPath.Replace("\\", "/");        //normalize to forward slashes            
            album.ImagePath =  thumbPath;
            Console.WriteLine($"Debug: thumbnail path {album.ImagePath}");

            album.NavigationPathSegments = albumName != null ? albumName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                                             : new List<string>();
            album.LastUpdatedUtc = item.LastUpdatedUtc;
            result.Add(album);
        }
        return result;
    }
}