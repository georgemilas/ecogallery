using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Runtime.Serialization;
using PicturesLib.model.album;
using PicturesLib.model.configuration;
using PicturesLib.service.database;

namespace PicturesLib.repository;

public record AlbumRepository: IDisposable, IAsyncDisposable
{
    public AlbumRepository(PicturesDataConfiguration configuration)
    {
        _configuration = configuration;
        var dbconfig = DatabaseConfiguration.CreateLocal("gmpictures", "postgres", "Dtututu7&");
        _db = new PostgresDatabaseService(dbconfig.ToConnectionString());
    }

    private IDatabaseService _db;
    private PicturesDataConfiguration _configuration;
    private string RootFolder => _configuration.RootFolder.FullName;

    public void Dispose()
    {
        _db.Dispose();
    }
    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    


    public async Task<Album> EnsureAlbumExistsAsync(string filePath)
    {
        var album = Album.CreateFromFilePath(filePath, RootFolder);
        var dbalbum = await GetAlbumAsync(filePath);
        //Console.WriteLine($"ran album get db: {filePath}");
        if (dbalbum == null)
        {            
            if (album.HasParentAlbum)
            {
                var parent = await EnsureAlbumExistsAsync(album.AlbumName);  //esure a parent Album record exists for this current Album
                album.ParentAlbumId = parent.Id;
            }
            return await AddNewAlbumAsync(album);      
        }
        return dbalbum;
    }       

    public async Task<Album?> GetAlbumAsync(string filePath)
    {
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        var sql = "SELECT * FROM album WHERE album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => Album.CreateFromDataReader(reader), album);
        return albums.FirstOrDefault();                 
    }

    public async Task<bool> AlbumHasContentAsync(string filePath)
    {
        //this checks if there are any images in this album or any sub-albums
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        var sql = "SELECT count(*) FROM album_image WHERE album_name LIKE @pattern";
        var parameters = new { pattern = $"'{album.AlbumName}%'" };
        var contentCount = await _db.ExecuteScalarAsync<int>(sql, parameters);
        return contentCount > 0;                 
    }

    public async Task<Album> AddNewAlbumAsync(Album album)
    {
        //Console.WriteLine($"TRY save db: {album}");  
        //insert or update existing album record and use the last image as feature image
        var sql = @"INSERT INTO album (album_name, album_type, last_updated_utc, feature_image_path, parent_album, parent_album_id)
                               VALUES (@album_name, @album_type, @last_updated_utc, @feature_image_path, @parent_album, @parent_album_id)
                    ON CONFLICT (album_name) DO UPDATE
                    SET
                        feature_image_path = EXCLUDED.feature_image_path,
                        last_updated_utc = EXCLUDED.last_updated_utc                        
                    RETURNING id;";        
        album.Id = await _db.ExecuteScalarAsync<long>(sql, album);    
        //Console.WriteLine($"ran album save db: {album.AlbumName}");        
        return album; 
    }

    public async Task<int> DeleteAlbumAsync(string filePath)
    {
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        var sql = "DELETE FROM album WHERE album_name = @album_name";
        var rowsAffected = await _db.ExecuteAsync(sql, album);

        //recursively delete parent album if empty
        if (rowsAffected > 0 && album.HasParentAlbum)
        {
           if (!await AlbumHasContentAsync(album.ParentAlbum))
           {
                rowsAffected += await DeleteAlbumAsync(album.ParentAlbum);
           } 
        }
        return rowsAffected;
    }

    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchicalByName(string albumName)
    {
        var parameters = new { album_name = albumName };
        var sql = "SELECT * FROM get_album_content_hierarchical_by_name(@album_name)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }    

    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchicalById(long albumId)
    {
        var parameters = new { album_id = albumId };
        var sql = "SELECT * FROM get_album_content_hierarchical_by_id(@album_id)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }    

    public async Task<List<AlbumContentFlatten>> GetAlbumContentFlattenByName(string albumName)
    {
        // Escape backslashes for PostgreSQL LIKE pattern
        var albumEscapedLikePattern = albumName.Replace(@"\", @"\\") + "%";
        var parameters = new { pattern = albumEscapedLikePattern };
        var sql = @"SELECT 
                        id,
                        image_name as item_name, 
                        image_type as item_type, 
                        image_path as item_path, 
                        album_name, 
                        last_updated
                    FROM album_image 
                    WHERE album_name LIKE @pattern";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentFlatten.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }  
    public async Task<List<AlbumContentFlatten>> GetAlbumContentFlattenById(long albumId)
    {
        //first get the album name for the album id
        var parameters = new { id = albumId };
        var sql = "SELECT * FROM album WHERE id = @id";
        var albums = await _db.QueryAsync(sql, reader => Album.CreateFromDataReader(reader), parameters);
        if (!albums.Any())
        {
            return new List<AlbumContentFlatten>();
        }
        
        //now run the query to get the images using album_image.album_name.startsWith(album.AlbumName)
        var album = albums.First();   
        // Escape backslashes for PostgreSQL LIKE pattern
        var albumEscapedLikePattern = album.AlbumName.Replace(@"\", @"\\") + "%";
        var parameters2 = new { pattern = albumEscapedLikePattern };
        sql = @"SELECT 
                        id,
                        image_name as item_name, 
                        image_type as item_type, 
                        image_path as item_path, 
                        album_name, 
                        last_updated
                    FROM album_image 
                    WHERE album_name LIKE @pattern";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentFlatten.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }  


    public async Task<List<AlbumContentHierarchical>> GetRootAlbumContentHierarchical()
    {
        var sql = "SELECT * FROM get_root_album_content_hierarchical()";
        var albumContent = await _db.QueryAsync(
            sql, 
            reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return albumContent;                 
    }

}


    