using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Runtime.Serialization;
using ExpParser.BooleanLogic;
using ExpParser.BooleanLogic.SQL;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.repository;

public record AlbumRepository: IDisposable, IAsyncDisposable
{
    public AlbumRepository(PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig)
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

    


    public async Task<Album> EnsureAlbumExistsAsync(string filePath)
    {
        var album = Album.CreateFromFilePath(filePath, RootFolder);
        var dbalbum = await GetAlbumByImageAsync(filePath);
        //Console.WriteLine($"ran album get db: {filePath}");
        if (dbalbum == null || dbalbum.FeatureImagePath != album.FeatureImagePath)
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

    public async Task<Album?> GetAlbumByNameAsync(string albumName)
    {
        Album album = Album.CreateFromAlbumPath(albumName, RootFolder);
        var sql = "SELECT * FROM album WHERE album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => Album.CreateFromDataReader(reader), album);
        return albums.FirstOrDefault();                         
    }
    public async Task<AlbumContentHierarchical?> GetAlbumHierarchicalByNameAsync(string albumName)
    {
        Album album = Album.CreateFromAlbumPath(albumName, RootFolder);
        var sql = @"SELECT 
                        a.id, 
                        a.album_name AS item_name, 
                        a.album_description AS item_description,
                        a.album_type AS item_type, 
                        a.parent_album_id as parent_album_id, 
                        a.parent_album AS parent_album_name,
                        ai.image_type AS feature_item_type, 
                        a.feature_image_path AS feature_item_path, 
                        cai.image_type AS inner_feature_item_type, 
                        ca.feature_image_path AS inner_feature_item_path, 
                        a.last_updated_utc,
                        a.album_timestamp_utc AS item_timestamp_utc,
                        NULL::json AS image_exif  
                    FROM album AS a
                    LEFT JOIN album ca ON a.feature_image_path = ca.album_name              --get the child album
                    LEFT JOIN album_image ai ON a.feature_image_path = ai.image_path        --get the image record of the album feature image
                    LEFT JOIN album_image cai ON ca.feature_image_path = cai.image_path     --get the image record of the child album feature image
                    WHERE a.album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), album);
        return albums.FirstOrDefault();                         
    }

    public async Task<Album?> GetAlbumByImageAsync(string filePath)
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
        // Escape backslashes for PostgreSQL LIKE pattern
        var albumEscapedLikePattern = album.AlbumName.Replace(@"\", @"\\") + "%";
        var sql = "SELECT count(*) FROM album_image WHERE album_name LIKE @pattern";
        var parameters = new { pattern = albumEscapedLikePattern };
        var contentCount = await _db.ExecuteScalarAsync<long>(sql, parameters);
        return contentCount > 0;
    }

    public async Task<Album> AddNewAlbumAsync(Album album)
    {
        var updateFeature = _configuration.IsFeatureFile(album.FeatureImagePath ?? string.Empty)
            ? "feature_image_path = EXCLUDED.feature_image_path,"
            : "";
        //album.FeatureImagePath ??= string.Empty;
        //Console.WriteLine($"TRY save db: {album}");  
        //insert or update existing album record and use the last image as feature image
        var sql = $@"INSERT INTO album (album_name, album_description, album_type, last_updated_utc, feature_image_path, parent_album, parent_album_id, album_timestamp_utc)
                               VALUES (@album_name, @album_description, @album_type, @last_updated_utc, @feature_image_path, @parent_album, @parent_album_id, @album_timestamp_utc)
                    ON CONFLICT (album_name) DO UPDATE
                    SET
                        {updateFeature}
                        album_description = EXCLUDED.album_description,
                        last_updated_utc = EXCLUDED.last_updated_utc,
                        album_timestamp_utc = EXCLUDED.album_timestamp_utc
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
        
        if (rowsAffected > 0)
        {
            Console.WriteLine($"Deleted album record: {album.AlbumName}");
        }
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


    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchicalByExpression(string expression, bool groupByPHash = true)
    {
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"\s+", " "); //normalize spaces
        var te = new SQLTokenEvaluator("image_path", SQLTokenEvaluator.OPERATOR_TYPE.ILIKE_ANY_ARRAY, SQLTokenEvaluator.FIELD_TYPE.STRING);
        var parser = new BooleanLogicExpressionParser(expression, new SQLSemantic(te));
        string where = (string)parser.Evaluate(null);        
        Console.WriteLine($"Debug: AlbumContentByExpression SQL WHERE: {where}");
        
        // Group by SHA-256 (text) with image_path fallback (stable natural key)
        var select = groupByPHash ? "SELECT DISTINCT ON (COALESCE(ai.image_sha256, ai.image_path))" : "SELECT";
        // Ensure ORDER BY starts with the DISTINCT ON expression; add a deterministic tie-breaker
        var orderby = groupByPHash ? "ORDER BY COALESCE(ai.image_sha256, ai.image_path), ai.image_timestamp_utc DESC, ai.id DESC" : "ORDER BY ai.image_timestamp_utc DESC, ai.id DESC";
                    
        var sql = $@"{select}
                    ai.id, 
                    ai.image_name AS item_name, 
                    ai.image_description AS item_description,
                    ai.image_type AS item_type, 
                    ai.album_id as parent_album_id, 
                    ai.album_name AS parent_album_name,
                    ai.image_type AS feature_item_type, 
                    ai.image_path AS feature_item_path, 
                    ai.image_type AS inner_feature_item_type, 
                    ai.image_path AS inner_feature_item_path,   
                    ai.last_updated_utc,
                    ai.image_timestamp_utc AS item_timestamp_utc,
                    row_to_json(exif) AS image_exif
                FROM album_image ai
                LEFT JOIN image_exif exif ON ai.id = exif.album_image_id
                WHERE {where}
                {orderby}";
        var content = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return content;                
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

    
    public async Task<List<AlbumContentHierarchical>> GetRootAlbumContentHierarchical()
    {
        var sql = "SELECT * FROM get_root_album_content_hierarchical()";
        var albumContent = await _db.QueryAsync(
            sql, 
            reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return albumContent;                 
    }

    public async Task<VirtualAlbum?> GetRootVirtualAlbumsAsync()
    {
        var sql = "select * from virtual_album where parent_album = ''";
        var albumContent = await _db.QueryAsync(sql, reader => VirtualAlbum.CreateFromDataReader(reader));
        return albumContent.FirstOrDefault();                 
    }
    public async Task<VirtualAlbum?> GetVirtualAlbumByIdAsync(long id)
    {
        var sql = "select * from virtual_album where id = @id";
        var parameters = new { id };
        var albumContent = await _db.QueryAsync(sql, reader => VirtualAlbum.CreateFromDataReader(reader),parameters);
        return albumContent.FirstOrDefault();                 
    }
    public async Task<List<VirtualAlbum>> GetVirtualAlbumContentByIdAsync(long id)
    {
        var sql = "select * from virtual_album where parent_album_id = @parent_album_id";
        var parameters = new { parent_album_id = id };
        var albumContent = await _db.QueryAsync(sql, reader => VirtualAlbum.CreateFromDataReader(reader),parameters);
        return albumContent;                 
    }
    public async Task<VirtualAlbum?> GetVirtualAlbumByNameAsync(string name)
    {
        var sql = "select * from virtual_album where album_name = @album_name";
        var parameters = new { album_name = name };
        var albumContent = await _db.QueryAsync(sql, reader => VirtualAlbum.CreateFromDataReader(reader),parameters);
        return albumContent.FirstOrDefault();                 
    }


    public async Task<VirtualAlbum> AddNewVirtualAlbumAsync(VirtualAlbum album)
    {
        //Console.WriteLine($"TRY save db: {album}");  
        //insert or update existing virtual album record 
        var sql = $@"INSERT INTO virtual_album (album_name, album_description, album_expression, album_folder, album_type, persistent_expression, is_public, feature_image_path, last_updated_utc, created_timestamp_utc, parent_album, parent_album_id)
                               VALUES (@album_name, @album_description, @album_expression, @album_folder, @album_type, @persistent_expression, @is_public, @feature_image_path, @last_updated_utc, @created_timestamp_utc, @parent_album, @parent_album_id)
                    ON CONFLICT (parent_album, album_name) DO UPDATE
                    SET
                        album_description = EXCLUDED.album_description,
                        album_expression = EXCLUDED.album_expression,
                        feature_image_path = EXCLUDED.feature_image_path,
                        last_updated_utc = EXCLUDED.last_updated_utc,
                        is_public = EXCLUDED.is_public,
                        persistent_expression = EXCLUDED.persistent_expression,
                        album_type = EXCLUDED.album_type,
                        album_folder = EXCLUDED.album_folder                        
                    RETURNING id;";        
        album.Id = await _db.ExecuteScalarAsync<long>(sql, album);    
        //Console.WriteLine($"ran album save db: {album.AlbumName}");        
        return album; 
    }



}


    