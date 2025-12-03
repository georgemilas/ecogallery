using System.Data.Common;
using System.IO;
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
        var path = filePath.Replace(RootFolder, string.Empty);
        return new Album
        {
            AlbumName = Path.GetDirectoryName(path) ?? string.Empty,   //includes the entire folder path  ex: 2025/vacation/Florida
            AlbumType  = "folder",   
            FeatureImagePath = path,
            LastUpdated = DateTimeOffset.UtcNow
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
            LastUpdated = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated"))
        };
    }

    public async Task EnsureAlbumExistsAsync(string filePath)
    {
        if (!await AlbumExistsAsync(filePath))
        {
            await AddNewAlbumAsync(filePath);                        
        }
    }       

    public async Task<bool> AlbumExistsAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
        var sql = "SELECT * FROM album WHERE album_name = @AlbumName";
        var albums = await _db.QueryAsync(sql, reader => CreateAlbumFromDataReader(reader), album);
        return albums.Any();                 
    }

    public async Task<bool> AlbumHasContentAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
        var sql = "SELECT count(*) FROM album_image WHERE album_name like '@Pattern'";
        var parameters = new { Pattern = $"{album.AlbumName}%" };
        var albums = await _db.ExecuteScalarAsync<int>(sql, parameters);
        return albums > 0;                 
    }

    public async Task<Album> AddNewAlbumAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
        var sql = @"INSERT INTO album (album_name, album_type, feature_image_path, last_updated) 
            VALUES (@AlbumName, @AlbumType, @FeatureImagePath, @LastUpdated)
            RETURNING id";        
        album.Id = await _db.ExecuteScalarAsync<int>(sql, album);            
        return album;                        
    }

    public async Task<int> DeleteAlbumAsync(string filePath)
    {
        Album album = CreateAlbumFromPath(filePath);
        var sql = "DELETE FROM album WHERE album_name = @AlbumName";
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


    