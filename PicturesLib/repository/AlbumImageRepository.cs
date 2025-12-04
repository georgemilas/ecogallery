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

    public async Task<bool> AlbumImageExistsAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromPath(filePath, RootFolder);
        var sql = "SELECT * FROM album_image WHERE image_path = @image_path";
        var albumImages = await _db.QueryAsync(sql, reader => AlbumImage.CreateFromDataReader(reader), image);
        return albumImages.Any();                 
    }

    public async Task<AlbumImage> AddNewImageAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromPath(filePath, RootFolder);
        var sql = @"INSERT INTO album_image (image_name, image_path, album_name, image_type, last_updated) 
                                     VALUES (@image_name, @image_path, @album_name, @image_type, @last_updated)
            ON CONFLICT (image_path) DO UPDATE
                    SET
                        last_updated = EXCLUDED.last_updated                        
            RETURNING id";        
        image.Id = await _db.ExecuteScalarAsync<long>(sql, image);            
        return image;                        
    }

    public async Task<int> DeleteAlbumImageAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromPath(filePath, RootFolder);
        var sql = "DELETE FROM album_image WHERE image_path = @image_path";
        return await _db.ExecuteAsync(sql, image);               
    }


}


    