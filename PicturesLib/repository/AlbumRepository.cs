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

    public Album CreateAlbumFromPath(string filePath)
    {
        var relativePath = filePath.Replace(RootFolder, string.Empty);
        //exclude the file name or current folder 
        //2025/vacation/florida/image.jpg  =>  albumName = 2025/vacation/florida  
        //2025/vacation/florida  =>  albumName = 2025/vacation  
        var albumName = Path.GetDirectoryName(relativePath) ?? string.Empty;  
        return new Album
        {
            AlbumName = albumName,   //includes the entire relative folder path  ex: 2025/vacation/Florida
            AlbumType  = "folder",   
            FeatureImagePath = relativePath,
            LastUpdated = DateTimeOffset.UtcNow,
            ParentAlbum = Path.GetDirectoryName(albumName) ?? string.Empty
        };
    }

    public Album CreateAlbumFromDataReader(DbDataReader reader)
    {
        return new Album
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            AlbumType = reader.GetString(reader.GetOrdinal("album_type")),
            FeatureImagePath = reader.GetString(reader.GetOrdinal("feature_image_path")),
            LastUpdated = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated")),
            ParentAlbum = reader.GetString(reader.GetOrdinal("parent_album"))
        };
    }


    public async Task EnsureAlbumExistsAsync(string filePath)
    {
        if (!await AlbumExistsAsync(filePath))
        {
            var album = CreateAlbumFromPath(filePath);
            if (album.HasParentAlbum)
            {
                await EnsureAlbumExistsAsync(album.AlbumName);  //esure a parent Album record exists for this current Album
            }
            await AddNewAlbumAsync(filePath);      
        }
    }       

    public async Task<bool> AlbumExistsAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
        var sql = "SELECT * FROM album WHERE album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => CreateAlbumFromDataReader(reader), album);
        return albums.Any();                 
    }

    public async Task<bool> AlbumHasContentAsync(string filePath)
    {
        //this checks if there are any images in this album or any sub-albums
        Album album = CreateAlbumFromPath(filePath);
        var sql = "SELECT count(*) FROM album_image WHERE album_name LIKE @pattern";
        var parameters = new { pattern = $"'{album.AlbumName}%'" };
        var contentCount = await _db.ExecuteScalarAsync<int>(sql, parameters);
        return contentCount > 0;                 
    }

    public async Task<Album> AddNewAlbumAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
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
        Album album = CreateAlbumFromPath(filePath);
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
}


    