using GalleryApi.model;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using Microsoft.AspNetCore.Http;

namespace GalleryApi.service;  
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
        
        string defaultFolderImage = "\\andr si anth.jpg";
        //get the relative path first
        string path = item.FeatureItemType != null 
                                    ? item.FeatureItemPath ?? item.InnerFeatureItemPath ?? defaultFolderImage
                                    : item.InnerFeatureItemPath ?? defaultFolderImage;
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        
        album.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 400));
        album.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 1440));    //save space, did not create hd 1080 path        
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
        image.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 400));
        image.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 1440));    //save space, did not create hd 1080 path
        image.ImageUHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, 1440));
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