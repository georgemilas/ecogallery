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

    public async Task<VirtualAlbumContent> GetVirtualAlbumContentByIdAsync(long albumId)
    {
        var valbum = await _albumRepository.GetVirtualAlbumByIdAsync(albumId);
        if (valbum == null)
        {
            throw new AlbumNotFoundException($"Virtual album with id {albumId} not found.");
        }
        VirtualAlbumContent album = await GetFromVirtualAlbum(valbum);
                
        return album;
    }

    public async Task<VirtualAlbumContent> GetRootVirtualAlbumsContentAsync()
    {
        var valbum = await _albumRepository.GetRootVirtualAlbumsAsync();        
        if (valbum == null)
        {
            throw new AlbumNotFoundException("No root virtual albums found.");
        }
        VirtualAlbumContent album = await GetFromVirtualAlbum(valbum);                
        return album;
    }



    private async Task<VirtualAlbumContent> GetFromVirtualAlbum(GalleryLib.model.album.VirtualAlbum valbum)
    {
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
        
        var settings = await _albumRepository.GetAlbumSettingsByAlbumIdAsync(valbum.Id, AuthenticatedUser?.Id ?? 1, true);    //get admin settings if no user
        album.Settings = settings ?? new GalleryLib.model.album.AlbumSettings
        {
            AlbumId = valbum.Id,
            IsVirtual = true,
            UserId = AuthenticatedUser?.Id ?? 1
        };        
        
        album.Albums = new List<AlbumItemContent>();
        album.Images = new List<ImageItemContent>();
        Console.WriteLine($"Debug: Loading virtual album '{valbum.AlbumName}' with expression '{valbum.AlbumExpression}' and folder '{valbum.AlbumFolder}'");         
        var content =  !String.IsNullOrWhiteSpace(album.Expression) ?
                       await _albumRepository.GetAlbumContentHierarchicalByExpression(album.Expression) :
                       await _albumRepository.GetAlbumContentHierarchicalByName(valbum.AlbumFolder);       
        Console.WriteLine($"Debug: Virtual album '{valbum.AlbumName}' returned {content.Count} items.");
        foreach (var image in content.Where(i => !i.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase)))
        {
            var contentImage = GetImageItemContent(null, image);
            album.Images.Add(contentImage);                
        }

        var albumContent = await _albumRepository.GetVirtualAlbumContentByIdAsync(valbum.Id);
        foreach (var calbum in albumContent)
        {   
            var albumItem = new AlbumItemContent();
            albumItem.Id = calbum.Id;
            albumItem.Name = calbum.AlbumName;
            albumItem.Description = calbum.AlbumDescription;
            albumItem.NavigationPathSegments = new List<string>();
            albumItem.LastUpdatedUtc = calbum.LastUpdatedUtc;
            albumItem.ItemTimestampUtc = calbum.LastUpdatedUtc;
            path = calbum.FeatureImagePath ?? "";                                        
            path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
            path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute             
            albumItem.ThumbnailPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
            albumItem.ImageHDPath = GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));                
            album.Albums.Add(albumItem);
        }
        return album;
    }

    


}