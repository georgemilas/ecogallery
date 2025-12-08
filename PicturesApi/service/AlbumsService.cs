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

    private async Task<AlbumContentHierarchical> GetContent(string? albumName, List<PicturesLib.model.album.AlbumContentHierarchical> albumContent)
    {

        var libAlbum = await _albumRepository.GetAlbumHierarchicalByNameAsync(albumName ?? string.Empty);
        if (libAlbum == null)
        {
            throw new Exception($"Album not found: '{albumName}'");
        }    
        var album = GetServiceAlbum(albumName, libAlbum);
        
        //Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");            
        album.Content = new List<AlbumContentHierarchical>();
        foreach (var item in albumContent)
        {
            var contentAlbum = GetServiceAlbum(albumName, item);
            album.Content.Add(contentAlbum);
        }
        return album;
    }

    private AlbumContentHierarchical GetServiceAlbum(string? albumName, PicturesLib.model.album.AlbumContentHierarchical item)
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
        //Console.WriteLine($"Debug: relative path {path}");
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        //Console.WriteLine($"Debug: absolute path {path}");

        var thumbPath = _picturesConfig.GetThumbnailPath(path, 400);   //get the thumbnail path from the absolute path
        album.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 400));
        album.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 1080));
        album.ImageUHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 1440));
        album.ImageOriginalPath = GetUrl(path);
        album.IsMovie = _picturesConfig.IsMovieFile(path);
        //Console.WriteLine($"Debug: thumbnail path {album.ThumbnailPath}");
        
        album.NavigationPathSegments = albumName != null ? albumName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                                         : new List<string>();
        album.LastUpdatedUtc = item.LastUpdatedUtc;
        album.ItemTimestampUtc = item.ItemTimestampUtc;
        album.ImageExif = item.ImageExif;
        return album;
    }

    private string GetUrl(string path)
    {
        var baseUrl = GetBaseUrl();
        path = path.Replace(_picturesConfig.RootFolder.FullName, $"{baseUrl}/pictures");  //make it url
        path = path.Replace("\\", "/");        //normalize to forward slashes            
        return path;
    }
}