using GalleryApi.model;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.repository.auth;

namespace GalleryApi.service;
public class AlbumsService: ServiceBase
{

    public AlbumsService(AlbumRepository albumRepository, AuthRepository authRepository, PicturesDataConfiguration picturesConfig, IHttpContextAccessor httpContextAccessor)
        : base(albumRepository, authRepository, picturesConfig, httpContextAccessor)
    {

    }  

    public async Task<VirtualAlbumContent> GetRandomImages()
    {
        var content = await _albumRepository.GetRandomImages();
        var searchId = GenerateSearchId("__random__");
        var uniqueDataId = UniqueDataId<string>.New(DataIdPrefix.AlbumRandom, searchId, AuthenticatedUser?.Id ?? 1);

        var valbum = await GetVirtualContent(content, uniqueDataId);
        valbum.Name = "Random Images";
        valbum.Expression = "";
        valbum.Description = content.Any() ? $"{content.Count} random images" : "No images found";
        return valbum;
    }

    public async Task<VirtualAlbumContent> GetRecentImages()
    {
        var content = await _albumRepository.GetRecentImages();
        var searchId = GenerateSearchId("__recent__");
        var uniqueDataId = UniqueDataId<string>.New(DataIdPrefix.AlbumRecent, searchId, AuthenticatedUser?.Id ?? 1);

        var valbum = await GetVirtualContent(content, uniqueDataId);
        valbum.Name = "Recent Images";
        valbum.Expression = "";
        valbum.Description = content.Any() ? $"{content.Count} recent images" : "No images found";
        return valbum;
    }

    public async Task<VirtualAlbumContent> SearchContentByExpression(GalleryLib.model.album.AlbumSearch albumSearch)
    {
        var (content, search) = await _albumRepository.GetAlbumContentHierarchicalByExpression(albumSearch);
        var searchId = GenerateSearchId(albumSearch.Expression);
        var uniqueDataId = UniqueDataId<string>.New(DataIdPrefix.AlbumSearch, searchId, AuthenticatedUser?.Id ?? 1);

        var valbum = await GetVirtualContent(content, uniqueDataId);
        valbum.Name = "Search Result";
        valbum.Expression = albumSearch.Expression;
        valbum.SearchInfo = search;
        if (albumSearch.Count > albumSearch.Limit)
        {
            valbum.Description = $"{albumSearch.Offset+1}-{albumSearch.Offset+content.Count}/{albumSearch.Count} images matching '{albumSearch.Expression}'";
        }
        else
        {
            valbum.Description = content.Any() ? $"{content.Count} images matching '{albumSearch.Expression}'" : "No images found";
        }
        return valbum;
    }

    private async Task<VirtualAlbumContent> GetVirtualContent(List<GalleryLib.model.album.AlbumContentHierarchical> content, IUniqueDataId uniqueDataId)
    {
        var valbum = new VirtualAlbumContent();
        valbum.Id = 0;
        valbum.NavigationPathSegments = new List<AlbumPathElement>();
        valbum.LastUpdatedUtc = DateTimeOffset.UtcNow;
        valbum.ItemTimestampUtc = DateTimeOffset.UtcNow;
        var item = content.FirstOrDefault();
        string path = item?.FeatureItemPath ?? string.Empty;     //get the relative path first
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute
        valbum.ThumbnailPath = item != null ? GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb)) : string.Empty;
        valbum.ImageHDPath = item != null ? GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD)) : string.Empty;    //save space, did not create hd 1080 path

        // Load or create settings for this search
        var settings = await _albumRepository.GetAlbumSettingsByUniqueDataIdAsync(uniqueDataId);
        valbum.Settings = settings ?? new GalleryLib.model.album.AlbumSettings
        {
            AlbumId = 0,
            UniqueDataId = uniqueDataId.DataId,
            IsVirtual = false,
            UserId = uniqueDataId.UserId
        };

        //Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");
        valbum.Images = new List<ImageItemContent>();
        foreach (var image in content)
        {
            var contentImage = GetImageItemContent(image);
            contentImage.NavigationPathSegments = valbum.NavigationPathSegments;
            valbum.Images.Add(contentImage);
            continue;
        }
        return valbum;
    }

    public async Task<model.AlbumContentHierarchical> GetAlbumContentHierarchicalById(long albumId)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalById(albumId);        
        var uniqueDataId = UniqueDataId<string>.New(DataIdPrefix.Album, albumId.ToString(), AuthenticatedUser?.Id ?? 1);
        return await GetContent(albumContent.First().ParentAlbumName, albumContent, uniqueDataId);
    }
    public async Task<model.AlbumContentHierarchical> GetAlbumContentHierarchicalByName(string? albumName = null)
    {
        var albumContent = albumName != null ? await _albumRepository.GetAlbumContentHierarchicalByName(albumName)
                                             : await _albumRepository.GetRootAlbumContentHierarchical();
        var uniqueDataId = UniqueDataId<string>.New(DataIdPrefix.Album, albumName ?? albumContent.First().ParentAlbumName, AuthenticatedUser?.Id ?? 1);                                             
        return await GetContent(albumName ?? albumContent.First().ParentAlbumName, albumContent, uniqueDataId);
    }

    private async Task<model.AlbumContentHierarchical> GetContent(string? albumName, List<GalleryLib.model.album.AlbumContentHierarchical> albumContent, IUniqueDataId uniqueDataId)
    {

        var libAlbum = await _albumRepository.GetAlbumHierarchicalByNameAsync(albumName ?? string.Empty);
        if (libAlbum == null)
        {
            throw new AlbumNotFoundException(albumName);
        }    
        var album = new model.AlbumContentHierarchical();
        await SetAlbumItemContent(album, albumName, libAlbum);
        var settings = await _albumRepository.GetAlbumSettingsByUniqueDataIdAsync(uniqueDataId);
        album.Settings = settings ?? new GalleryLib.model.album.AlbumSettings
        {
            AlbumId = libAlbum.Id,
            UniqueDataId = uniqueDataId.DataId,
            IsVirtual = false,
            UserId = uniqueDataId.UserId
        };

        
        //Console.WriteLine($"Debug: Config Mapping {_picturesConfig.Folder}, {_picturesConfig.RootFolder}, {_picturesConfig.ThumbnailsBase}, {_picturesConfig.ThumbDir(500)}");            
        album.Albums = new List<AlbumItemContent>();
        album.Images = new List<ImageItemContent>();
        foreach (var item in albumContent)
        {
            if (item.ItemType.Equals("folder", StringComparison.OrdinalIgnoreCase))
            {
                var contentAlbum = new AlbumItemContent();
                await SetAlbumItemContent(contentAlbum, albumName, item);
                album.Albums.Add(contentAlbum);
                continue;
            }
            else {
                var contentImage = GetImageItemContent(item);
                contentImage.NavigationPathSegments = album.NavigationPathSegments;
                album.Images.Add(contentImage);
                continue;
            }            
        }
        return album;
    }

    private async Task SetAlbumItemContent(AlbumItemContent album, string? albumName, GalleryLib.model.album.AlbumContentHierarchical item)
    {
        album.Id = item.Id;
        album.Name = item.ItemName;
        album.Description = item.ItemDescription;
        album.RoleId = item.RoleId;
        
        string defaultFolderImage = "";
        //get the relative path first
        string path = item.FeatureItemType != null 
                                    ? item.FeatureItemPath ?? item.InnerFeatureItemPath ?? defaultFolderImage
                                    : item.InnerFeatureItemPath ?? defaultFolderImage;
        path = path.StartsWith("\\") || path.StartsWith("/") ? path.Substring(1) : path; //make sure it's relative
        path = Path.Combine(_picturesConfig.RootFolder.FullName, path);                  //then make it absolute 
        
        album.ThumbnailPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.Thumb));
        album.ImageHDPath = GetPicturesUrl(_picturesConfig.GetThumbnailPath(path, (int)ThumbnailHeights.HD));    
        
        var parents = await _albumRepository.GetAlbumParentsAsync(item.Id);
        album.NavigationPathSegments = parents.Select(p => new AlbumPathElement { Id = p.Id, Name = p.Path }).Reverse().ToList();
        // album.NavigationPathSegments = albumName != null ? albumName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList()
        //                                                  : new List<string>();
        
        album.LastUpdatedUtc = item.LastUpdatedUtc;
        album.ItemTimestampUtc = item.ItemTimestampUtc;                
    }

    

 
}