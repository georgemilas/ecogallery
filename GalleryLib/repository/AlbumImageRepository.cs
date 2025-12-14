using System.Data.Common;
using System.IO;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.repository;

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

    public async Task<AlbumImage?> GetAlbumImageAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromFilePath(filePath, RootFolder);
        var sql = "SELECT * FROM album_image WHERE image_path = @image_path";
        var albumImages = await _db.QueryAsync(sql, reader => AlbumImage.CreateFromDataReader(reader), image);
        return albumImages.FirstOrDefault();                 
    }

    public async Task<AlbumImage> AddNewImageAsync(string filePath, Album? album = null)
    {
        AlbumImage image = AlbumImage.CreateFromFilePath(filePath, RootFolder);
        image.AlbumId = album?.Id ?? 0;
        var sql = @"INSERT INTO album_image (image_name, image_path, image_type, last_updated_utc, album_name, album_id, image_timestamp_utc) 
                                     VALUES (@image_name, @image_path, @image_type, @last_updated_utc, @album_name, @album_id, @image_timestamp_utc)
                    ON CONFLICT (image_path) DO UPDATE
                            SET
                                last_updated_utc = EXCLUDED.last_updated_utc                        
                    RETURNING id";        
        image.Id = await _db.ExecuteScalarAsync<long>(sql, image);            
        return image;                        
    }

    public async Task<int> DeleteAlbumImageAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromFilePath(filePath, RootFolder);
        var sql = "DELETE FROM album_image WHERE image_path = @image_path";
        return await _db.ExecuteAsync(sql, image);               
    }

    public async Task<ImageExif?> GetImageExifAsync(AlbumImage albumImage)
    {
        var sql = "SELECT * FROM image_exif WHERE album_image_id = @album_image_id";
        var sqlParams = new { album_image_id = albumImage.Id };
        var imageExifs = await _db.QueryAsync(sql, reader => ImageExif.CreateFromDataReader(reader), sqlParams);
        return imageExifs.FirstOrDefault();                 
    }

    public async Task<ImageExif> AddNewImageExifAsync(ImageExif exif)
    {
        var sql = @"INSERT INTO public.image_exif(album_image_id, camera, lens, focal_length, aperture, exposure_time, iso, date_taken, 
                                                rating, date_modified, flash, metering_mode, exposure_program, exposure_bias, exposure_mode, 
                                                white_balance, color_space, scene_capture_type, circle_of_confusion, field_of_view, depth_of_field,
                                                hyperfocal_distance, normalized_light_value, software, serial_number, lens_serial_number, file_name, 
                                                file_path, file_size_bytes, image_width, image_height, last_updated_utc)
	                                VALUES (@album_image_id, @camera, @lens, @focal_length, @aperture, @exposure_time, @iso, @date_taken, 
                                                @rating, @date_modified, @flash, @metering_mode, @exposure_program, @exposure_bias, @exposure_mode, 
                                                @white_balance, @color_space, @scene_capture_type, @circle_of_confusion, @field_of_view, @depth_of_field,
                                                @hyperfocal_distance, @normalized_light_value, @software, @serial_number, @lens_serial_number, @file_name, 
                                                @file_path, @file_size_bytes, @image_width, @image_height, @last_updated_utc)                                     
                    ON CONFLICT (album_image_id) DO UPDATE
                        SET
                            last_updated_utc = EXCLUDED.last_updated_utc                        
                    RETURNING id";        
        exif.Id = await _db.ExecuteScalarAsync<long>(sql, exif);            
        return exif;                        
    }

}


    