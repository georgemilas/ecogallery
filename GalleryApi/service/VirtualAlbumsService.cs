using GalleryApi.model;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryApi.service;  
public class VirtualAlbumsService: ServiceBase
{
    
    public VirtualAlbumsService(AlbumRepository albumRepository, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
        : base(albumRepository, picturesConfig, httpContextAccessor)    
    {
        
    }    

    public async Task<VirtualAlbumContent> GetVirtualAlbumContentById(long albumId)
    {
        var valbum = await _albumRepository.GetVirtualAlbumContentById(albumId); 
        if (valbum == null)
        {
            throw new AlbumNotFoundException($"Virtual album with id {albumId} not found.");
        }              
        var album = new VirtualAlbumContent();
        album.Id = valbum.Id;
        album.Name = valbum.AlbumName;
        album.Description = valbum.AlbumDescription; 
        album.Expression = valbum.AlbumExpression;
        album.NavigationPathSegments = new List<string>();
        album.LastUpdatedUtc = valbum.LastUpdatedUtc;
        album.ItemTimestampUtc = valbum.LastUpdatedUtc;
        string path = valbum.FeatureImagePath ?? "";                                        
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute             
        album.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        album.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));     
        album.Albums = new List<AlbumItemContent>();
        album.Images = new List<ImageItemContent>();

                
        var content = await _albumRepository.GetAlbumContentHierarchicalByExpression(album.Expression);    
        foreach (var image in content)
        {            
            var contentImage = GetImageItemContent(null, image);
            album.Images.Add(contentImage);
            continue;                        
        }        
        return album;
    }

    public async Task<VirtualAlbumContent> GetRootVirtualAlbumsContent()
    {
        var albumContent = await _albumRepository.GetRootVirtualAlbumsContent();        
        var album = new VirtualAlbumContent();
        album.Id = 0;
        album.Name = "Albums";
        album.Description = "Albums"; 
        album.NavigationPathSegments = new List<string>();
        album.LastUpdatedUtc = DateTimeOffset.UtcNow;
        album.ItemTimestampUtc = DateTimeOffset.UtcNow;
        album.Albums = new List<AlbumItemContent>();
        string path = albumContent[0].FeatureImagePath ?? "";                                        
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute             
        album.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        album.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));     

        foreach (var valbum in albumContent)
        {   
            var albumItem = new AlbumItemContent();
            albumItem.Id = valbum.Id;
            albumItem.Name = valbum.AlbumName;
            albumItem.NavigationPathSegments = new List<string>();
            albumItem.LastUpdatedUtc = valbum.LastUpdatedUtc;
            albumItem.ItemTimestampUtc = valbum.LastUpdatedUtc;
            path = valbum.FeatureImagePath ?? "";                                        
            path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
            path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute             
            albumItem.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
            albumItem.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));                
            album.Albums.Add(albumItem);
        }
        album.Images = new List<ImageItemContent>();
        return album;
    }


}