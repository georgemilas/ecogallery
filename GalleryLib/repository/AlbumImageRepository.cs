using System.Data.Common;
using System.IO;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.repository;

public class AlbumImageRepository: IAlbumImageRepository, IDisposable, IAsyncDisposable
{

    public AlbumImageRepository(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
    {
        _configuration = configuration;
        _dbConfig = dbConfig;
        _db = new PostgresDatabaseService(_dbConfig.ToConnectionString());
    }

    private IDatabaseService _db;
    private PicturesDataConfiguration _configuration;
    private DatabaseConfiguration _dbConfig;
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
        var sql = @"INSERT INTO album_image (image_name, image_description, image_path, image_type, last_updated_utc, album_name, album_id, image_timestamp_utc) 
                                     VALUES (@image_name, @image_description, @image_path, @image_type, @last_updated_utc, @album_name, @album_id, @image_timestamp_utc)
                    ON CONFLICT (image_path) DO UPDATE
                            SET
                                last_updated_utc = EXCLUDED.last_updated_utc,
                                image_description = EXCLUDED.image_description                        
                    RETURNING id";        
        image.Id = await _db.ExecuteScalarAsync<long>(sql, image);            
        return image;                        
    }

    public async Task<long> UpdateImageHash(AlbumImage image)
    {
        var sql = @"UPDATE album_image SET last_updated_utc = @last_updated_utc, image_sha256 = @image_sha256
                    WHERE id = @id
                    RETURNING id";
        var id = await _db.ExecuteScalarAsync<long>(sql, image);
        return id;
    }

    public async Task<long> UpdateImageDimensionsAndDateTaken(AlbumImage image)
    {
        var sql = @"UPDATE album_image SET 
                        last_updated_utc = @last_updated_utc, 
                        image_width = @image_width, 
                        image_height = @image_height,
                        image_timestamp_utc = @image_timestamp_utc
                    WHERE id = @id
                    RETURNING id";
        var id = await _db.ExecuteScalarAsync<long>(sql, image);
        return id;
    }

    public async Task<int> DeleteAlbumImageAsync(string filePath)
    {
        AlbumImage image = AlbumImage.CreateFromFilePath(filePath, RootFolder);
        var sql = "DELETE FROM album_image WHERE image_path = @image_path";
        return await _db.ExecuteAsync(sql, image);               
    }

    public async Task<ImageMetadata?> GetImageMetadataAsync(AlbumImage albumImage)
    {
        var sql = "SELECT * FROM image_metadata WHERE album_image_id = @album_image_id";
        var sqlParams = new { album_image_id = albumImage.Id };
        var imageExifs = await _db.QueryAsync(sql, reader => ImageMetadata.CreateFromDataReader(reader), sqlParams);
        return imageExifs.FirstOrDefault();                 
    }

    public async Task<VideoMetadata?> GetVideoMetadataAsync(AlbumImage albumImage)
    {
        var sql = "SELECT * FROM video_metadata WHERE album_image_id = @album_image_id";
        var sqlParams = new { album_image_id = albumImage.Id };
        var videoMetadata = await _db.QueryAsync(sql, reader => VideoMetadata.CreateFromDataReader(reader), sqlParams);
        return videoMetadata.FirstOrDefault();                 
    }

    /// <summary>
    /// Upsert Video Metadata 
    /// </summary>
    public async Task<VideoMetadata> UpsertVideoMetadataAsync(VideoMetadata videoMetadata)
    {
        var sql = @"INSERT INTO public.video_metadata(album_image_id, file_name, file_path, file_size_bytes, date_taken, date_modified, duration, 
                                                    video_width, video_height, video_codec, audio_codec, pixel_format, frame_rate, video_bit_rate, 
                                                    audio_sample_rate, audio_channels, audio_bit_rate, format_name, software, camera, rotation, gps_latitude, gps_longitude, gps_altitude, last_updated_utc)
                                    VALUES (@album_image_id, @file_name, @file_path, @file_size_bytes, @date_taken, @date_modified, @duration, 
                                                    @video_width, @video_height, @video_codec, @audio_codec, @pixel_format, @frame_rate, @video_bit_rate, 
                                                    @audio_sample_rate, @audio_channels, @audio_bit_rate, @format_name, @software, @camera, @rotation, @gps_latitude, @gps_longitude, @gps_altitude, @last_updated_utc)                                     
                    ON CONFLICT (album_image_id) DO UPDATE
                        SET
                            file_size_bytes = EXCLUDED.file_size_bytes, 
                            date_taken = EXCLUDED.date_taken, 
                            date_modified = EXCLUDED.date_modified, 
                            duration = EXCLUDED.duration,                             
                            video_width = EXCLUDED.video_width, 
                            video_height = EXCLUDED.video_height, 
                            video_codec = EXCLUDED.video_codec, 
                            audio_codec = EXCLUDED.audio_codec, 
                            pixel_format = EXCLUDED.pixel_format, 
                            frame_rate = EXCLUDED.frame_rate, 
                            video_bit_rate = EXCLUDED.video_bit_rate, 
                            audio_sample_rate = EXCLUDED.audio_sample_rate, 
                            audio_channels = EXCLUDED.audio_channels, 
                            audio_bit_rate = EXCLUDED.audio_bit_rate, 
                            format_name = EXCLUDED.format_name, 
                            software = EXCLUDED.software, 
                            camera = EXCLUDED.camera, 
                            rotation = EXCLUDED.rotation,
                            gps_latitude = EXCLUDED.gps_latitude,
                            gps_longitude = EXCLUDED.gps_longitude,
                            gps_altitude = EXCLUDED.gps_altitude,
                            last_updated_utc = EXCLUDED.last_updated_utc

                    RETURNING id";        
        videoMetadata.Id = await _db.ExecuteScalarAsync<long>(sql, videoMetadata);            
        return videoMetadata;                        
    }
    public async Task<ImageMetadata> UpsertImageMetadataAsync(ImageMetadata exif)
    {
        var sql = @"INSERT INTO public.image_metadata(album_image_id, camera, lens, focal_length, aperture, exposure_time, iso, date_taken, 
                                                rating, date_modified, flash, metering_mode, exposure_program, exposure_bias, exposure_mode, 
                                                white_balance, color_space, scene_capture_type, circle_of_confusion, field_of_view, depth_of_field,
                                                hyperfocal_distance, normalized_light_value, software, serial_number, lens_serial_number, file_name, 
                                                file_path, file_size_bytes, image_width, image_height, orientation, gps_latitude, gps_longitude, gps_altitude, last_updated_utc)
	                                VALUES (@album_image_id, @camera, @lens, @focal_length, @aperture, @exposure_time, @iso, @date_taken, 
                                                @rating, @date_modified, @flash, @metering_mode, @exposure_program, @exposure_bias, @exposure_mode, 
                                                @white_balance, @color_space, @scene_capture_type, @circle_of_confusion, @field_of_view, @depth_of_field,
                                                @hyperfocal_distance, @normalized_light_value, @software, @serial_number, @lens_serial_number, @file_name, 
                                                @file_path, @file_size_bytes, @image_width, @image_height, @orientation, @gps_latitude, @gps_longitude, @gps_altitude, @last_updated_utc)                                     
                    ON CONFLICT (album_image_id) DO UPDATE
                        SET
                            camera = EXCLUDED.camera, 
                            lens = EXCLUDED.lens, 
                            focal_length = EXCLUDED.focal_length, 
                            aperture = EXCLUDED.aperture, 
                            exposure_time = EXCLUDED.exposure_time, 
                            iso = EXCLUDED.iso, 
                            date_taken = EXCLUDED.date_taken, 
                            rating = EXCLUDED.rating, 
                            date_modified = EXCLUDED.date_modified, 
                            flash = EXCLUDED.flash, 
                            metering_mode = EXCLUDED.metering_mode, 
                            exposure_program = EXCLUDED.exposure_program, 
                            exposure_bias = EXCLUDED.exposure_bias, 
                            exposure_mode = EXCLUDED.exposure_mode, 
                            white_balance = EXCLUDED.white_balance, 
                            color_space = EXCLUDED.color_space, 
                            scene_capture_type = EXCLUDED.scene_capture_type, 
                            circle_of_confusion = EXCLUDED.circle_of_confusion, 
                            field_of_view = EXCLUDED.field_of_view, 
                            depth_of_field = EXCLUDED.depth_of_field,
                            hyperfocal_distance = EXCLUDED.hyperfocal_distance, 
                            normalized_light_value = EXCLUDED.normalized_light_value, 
                            software = EXCLUDED.software, 
                            serial_number = EXCLUDED.serial_number, 
                            lens_serial_number = EXCLUDED.lens_serial_number, 
                            file_name = EXCLUDED.file_name, 
                            file_path = EXCLUDED.file_path, 
                            file_size_bytes = EXCLUDED.file_size_bytes, 
                            image_width = EXCLUDED.image_width, 
                            image_height = EXCLUDED.image_height, 
                            orientation = EXCLUDED.orientation,
                            gps_latitude = EXCLUDED.gps_latitude,
                            gps_longitude = EXCLUDED.gps_longitude,
                            gps_altitude = EXCLUDED.gps_altitude,
                            last_updated_utc = EXCLUDED.last_updated_utc                        
                    RETURNING id";        
        exif.Id = await _db.ExecuteScalarAsync<long>(sql, exif);            
        return exif;                        
    }

    public async Task<List<AlbumImage>> GetAllAlbumImagesAsync()
    {
        var sql = "SELECT * FROM album_image";
        var albumImages = await _db.QueryAsync(sql, reader => AlbumImage.CreateFromDataReader(reader));
        return albumImages;
    }

}


    