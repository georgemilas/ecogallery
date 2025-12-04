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

    


    public async Task EnsureAlbumExistsAsync(string filePath)
    {
        if (!await AlbumExistsAsync(filePath))
        {
            var album = Album.CreateFromPath(filePath, RootFolder);
            if (album.HasParentAlbum)
            {
                await EnsureAlbumExistsAsync(album.AlbumName);  //esure a parent Album record exists for this current Album
            }
            await AddNewAlbumAsync(filePath);      
        }
    }       

    public async Task<bool> AlbumExistsAsync(string filePath)
    {
        Album album = Album.CreateFromPath(filePath, RootFolder);
        var sql = "SELECT * FROM album WHERE album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => Album.CreateFromDataReader(reader), album);
        return albums.Any();                 
    }

    public async Task<bool> AlbumHasContentAsync(string filePath)
    {
        //this checks if there are any images in this album or any sub-albums
        Album album = Album.CreateFromPath(filePath, RootFolder);
        var sql = "SELECT count(*) FROM album_image WHERE album_name LIKE @pattern";
        var parameters = new { pattern = $"'{album.AlbumName}%'" };
        var contentCount = await _db.ExecuteScalarAsync<int>(sql, parameters);
        return contentCount > 0;                 
    }

    public async Task<Album> AddNewAlbumAsync(string filePath)
    {
        Album album = Album.CreateFromPath(filePath, RootFolder);
        //insert or update existing album record and use the last image as feature image
        var sql = @"INSERT INTO album (album_name, album_type, feature_image_path, last_updated, parent_album)
                               VALUES (@album_name, @album_type, @feature_image_path, @last_updated, @parent_album)
                    ON CONFLICT (album_name) DO UPDATE
                    SET
                        feature_image_path = EXCLUDED.feature_image_path,
                        last_updated = EXCLUDED.last_updated                        
                    RETURNING id;";        
        album.Id = await _db.ExecuteScalarAsync<long>(sql, album);            
        return album; 
    }

    public async Task<int> DeleteAlbumAsync(string filePath)
    {
        Album album = Album.CreateFromPath(filePath, RootFolder);
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

    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchical(string albumName)
    {
        var parameters = new { album_name = $"'{albumName}'" };
        var sql = @"SELECT pa.album_name as item_name, pa.album_type as item_type, ai.image_type as feature_item_type, pa.feature_image_path as feature_item_path, iai.image_type as inner_feature_item_type, a.feature_image_path as inner_feature_item_path, pa.last_updated  
                        FROM album as pa 
                        LEFT JOIN album_image ai on pa.feature_image_path = ai.image_path
                        LEFT JOIN album a on pa.feature_image_path = a.album_name
                        LEFT JOIN album_image iai on a.feature_image_path = iai.image_path
                        WHERE pa.parent_album = @album_name
                    UNION 
                    SELECT image_name as item_name, image_type as item_type, image_type as feature_image_type, image_path, image_type, image_path, last_updated
                        FROM album_image 
                        WHERE album_name = @album_name  
                    ORDER BY item_type";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }    

    public async Task<List<AlbumContentFlatten>> GetAlbumContentFlatten(string albumName)
    {
        var parameters = new { pattern = $"'{albumName}%'" };
        var sql = @"SELECT image_name as item_name, image_type as item_type, image_path as item_path, album_name, last_updated
                    FROM album_image 
                    WHERE album_name LIKE @pattern";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentFlatten.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }  
}


    