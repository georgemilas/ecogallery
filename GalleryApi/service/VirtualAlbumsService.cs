using GalleryApi.model;
using GalleryLib.model.album;
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


     public async Task<VirtualAlbumContent> GetRandomImages()
    {
        var album = await PickImagesFromAll((all) =>
        {
            var random = new Random();
            var nRandom = all.OrderBy(x => random.Next()).Take(100).ToList();
            return nRandom;
        });
        album.Name = "Random Images";
        album.Description = album.Images.Any() ? $"{album.Images.Count} random images" : "No images found";
        return album;
    }

    public async Task<VirtualAlbumContent> GetRecentImages()
    {
        var album = await PickImagesFromAll((all) =>
        {
            var nRecent = all.OrderByDescending(x => x.ItemTimestampUtc).Take(100).ToList();
            return nRecent;
        });
        album.Name = "Recent Images";
        album.Description = album.Images.Any() ? $"{album.Images.Count} recent images" : "No images found";
        return album;
    }

    private async Task<VirtualAlbumContent> PickImagesFromAll(Func<List<GalleryLib.model.album.AlbumContentHierarchical>, List<GalleryLib.model.album.AlbumContentHierarchical>> filter)
    {
        var albums = await _albumRepository.GetAllVirtualAlbumsAsync();
        var all = new List<GalleryLib.model.album.AlbumContentHierarchical>();
        
        foreach (var album in albums)
        {
            var (content, _) = !String.IsNullOrWhiteSpace(album.AlbumExpression) ?
                       await _albumRepository.GetAlbumContentHierarchicalByExpression(new AlbumSearch() { Limit = 0, Expression = album.AlbumExpression }) :   //no limit
                       (await _albumRepository.GetAlbumContentHierarchicalByName(album.AlbumFolder), null);
            all.AddRange(content);
        }
        all = all.GroupBy(i => i.ImageSha256).Select(g => g.First()).ToList(); //remove duplicates based on SHA256
        var filteredContent = filter(all);
        
        var valbum = new VirtualAlbumContent();
        valbum.Expression = "";
        valbum.NavigationPathSegments = new List<AlbumPathElement>();
        valbum.LastUpdatedUtc = DateTimeOffset.UtcNow;
        valbum.ItemTimestampUtc = DateTimeOffset.UtcNow;
        var item = filteredContent.FirstOrDefault();
        string path = item?.FeatureItemPath ?? string.Empty;     //get the relative path first                                
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        valbum.ThumbnailPath = item != null ? GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb)) : string.Empty;
        valbum.ImageHDPath = item != null ? GetUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD)) : string.Empty;    //save space, did not create hd 1080 path

        var settings = await _albumRepository.GetAlbumSettingsByAlbumIdAsync(valbum.Id, AuthenticatedUser?.Id ?? 1, true);    //get admin settings if no user
        valbum.Settings = settings ?? new GalleryLib.model.album.AlbumSettings
        {
            AlbumId = valbum.Id,
            IsVirtual = true,
            UserId = AuthenticatedUser?.Id ?? 1
        };

        valbum.Albums = new List<AlbumItemContent>();
        valbum.Images = new List<ImageItemContent>();
        foreach (var image in filteredContent.Where(i => !i.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase)))
        {
            var contentImage = GetImageItemContent(image);
            contentImage.NavigationPathSegments = valbum.NavigationPathSegments;
            valbum.Images.Add(contentImage);
        }
        return valbum;
    }





    private async Task<VirtualAlbumContent> GetFromVirtualAlbum(GalleryLib.model.album.VirtualAlbum valbum)
    {
        var album = new VirtualAlbumContent();
        album.Id = valbum.Id;
        album.Name = valbum.AlbumName;
        album.Description = valbum.AlbumDescription;
        album.Expression = valbum.AlbumExpression;
        var parents = await _albumRepository.GetVirtualAlbumParentsAsync(valbum.Id);
        album.NavigationPathSegments = parents.Select(p => new AlbumPathElement { Id = p.Id, Name = p.Path }).Reverse().ToList();
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
        var (content, _) =  !String.IsNullOrWhiteSpace(album.Expression) ?
                       await _albumRepository.GetAlbumContentHierarchicalByExpression(new AlbumSearch() { Limit = 0, Expression = valbum.AlbumExpression }) :   //no limit for virtual albums
                       (await _albumRepository.GetAlbumContentHierarchicalByName(valbum.AlbumFolder), null);       
        Console.WriteLine($"Debug: Virtual album '{valbum.AlbumName}' returned {content.Count} items.");
        foreach (var image in content.Where(i => !i.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase)))
        {
            var contentImage = GetImageItemContent(image);
            contentImage.NavigationPathSegments = album.NavigationPathSegments;
            album.Images.Add(contentImage);                
        }

        var albumContent = await _albumRepository.GetVirtualAlbumContentByIdAsync(valbum.Id);
        foreach (var calbum in albumContent)
        {   
            var albumItem = new AlbumItemContent();
            albumItem.Id = calbum.Id;
            albumItem.Name = calbum.AlbumName;
            albumItem.Description = calbum.AlbumDescription;
            var cparents = await _albumRepository.GetAlbumParentsAsync(calbum.Id);
            albumItem.NavigationPathSegments = cparents.Select(p => new AlbumPathElement { Id = p.Id, Name = p.Path }).Reverse().ToList();
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