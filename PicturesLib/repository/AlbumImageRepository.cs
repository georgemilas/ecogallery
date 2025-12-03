using System.Data.Common;
using System.IO;
using PicturesLib.model.album;
using PicturesLib.model.configuration;
using PicturesLib.service.database;

namespace PicturesLib.repository;

public class AlbumImageRepository: IDisposable, IAsyncDisposable
{

    public AlbumImageRepository(PicturesDataConfiguration configuration)
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

    public AlbumImage CreateAlbumImageFromPath(string filePath)
    {

        var path = filePath.Replace(RootFolder, string.Empty);
        return new AlbumImage
        {
            AlbumName = Path.GetDirectoryName(path) ?? string.Empty,   //includes the entire folder path  ex: 2025/vacation/Florida
            ImageName = Path.GetFileName(path),
            ImagePath = path,
            ImageType  = Path.GetExtension(path),   //includes the dot, e.g. ".jpg"             
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public AlbumImage CreateAlbumImageFromDataReader(DbDataReader reader)
    {
        return new AlbumImage
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AlbumName = reader.GetString(reader.GetOrdinal("album_name")),
            ImageName = reader.GetString(reader.GetOrdinal("image_name")),
            ImagePath = reader.GetString(reader.GetOrdinal("image_path")),
            ImageType = reader.GetString(reader.GetOrdinal("image_type")),
            LastUpdated = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_updated"))
        };
    }

    public async Task<bool> AlbumImageExistsAsync(string filePath)
    {
        AlbumImage image = CreateAlbumImageFromPath(filePath);
        var sql = "SELECT * FROM album_image WHERE image_path = @ImagePath";
        var albumImages = await _db.QueryAsync(sql, reader => AlbumImage.CreateImageFromDataReader(reader), image);
        return albumImages.Any();                 
    }

    public async Task<AlbumImage> AddNewImageAsync(string filePath)
    {
        AlbumImage image = CreateAlbumImageFromPath(filePath);
        var sql = @"INSERT INTO album_image (image_name, image_path, album_name, image_type, last_updated) 
            VALUES (@ImageName, @ImagePath, @AlbumName, @ImageType, @LastUpdated)
            RETURNING id";        
        image.Id = await _db.ExecuteScalarAsync<int>(sql, image);            
        return image;                        
    }

    public async Task<int> DeleteAlbumImageAsync(string filePath)
    {
        AlbumImage image = CreateAlbumImageFromPath(filePath);
        var sql = "DELETE FROM album_image WHERE image_path = @ImagePath";
        return await _db.ExecuteAsync(sql, image);               
    }


}


    