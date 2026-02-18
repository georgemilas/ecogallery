using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Runtime.Serialization;
using ExpParser.BooleanLogic;
using ExpParser.BooleanLogic.SQL;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;
using YamlDotNet.Core.Tokens;

namespace GalleryLib.repository;

public record AlbumRepository: IAlbumRepository, IDisposable, IAsyncDisposable
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


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Actual Album Methods
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Ensure an album record exists for the given file path, creating parent albums as needed recursively
    /// </summary>    
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
    
    public async Task<Album?> GetAlbumByImageAsync(string filePath)
    {
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        var sql = "SELECT * FROM album WHERE album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => Album.CreateFromDataReader(reader), album);
        return albums.FirstOrDefault();                 
    }


    /// <summary>
    /// checks if there are any images in this album or sub-album ("recursive")
    /// </summary>
    public async Task<bool> AlbumHasContentAsync(string filePath)
    {
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        return await AlbumHasContentAsync(album);
    }
    public async Task<bool> AlbumHasContentAsync(Album album)
    {
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
        var sql = $@"INSERT INTO album (album_name, album_description, album_type, last_updated_utc, feature_image_path, parent_album, parent_album_id, role_id, album_timestamp_utc)
                               VALUES (@album_name, @album_description, @album_type, @last_updated_utc, @feature_image_path, @parent_album, @parent_album_id, @role_id, @album_timestamp_utc)
                    ON CONFLICT (album_name) DO UPDATE
                    SET
                        {updateFeature}
                        album_description = EXCLUDED.album_description,
                        last_updated_utc = EXCLUDED.last_updated_utc,
                        album_timestamp_utc = EXCLUDED.album_timestamp_utc,
                        role_id = EXCLUDED.role_id
                    RETURNING id;";        
        album.Id = await _db.ExecuteScalarAsync<long>(sql, album);    
        //Console.WriteLine($"ran album save db: {album.AlbumName}");        
        return album; 
    }

    public async Task<int> DeleteAlbumAsync(string filePath, bool logIfCleaned = false)
    {
        Album album = Album.CreateFromFilePath(filePath, RootFolder);
        return await DeleteAlbumAsync(album, logIfCleaned);
    }
    public async Task<int> DeleteAlbumAsync(Album album, bool logIfCleaned = false)
    {    
        var sql = "DELETE FROM album WHERE album_name = @album_name";
        var rowsAffected = await _db.ExecuteAsync(sql, album);
        
        if (rowsAffected > 0 && logIfCleaned)
        {
            Console.WriteLine($"Deleted album record: {album.AlbumName}");
        }
        //recursively delete parent album if empty
        if (rowsAffected > 0 && album.HasParentAlbum)
        {
           if (!await AlbumHasContentAsync(album.ParentAlbum))
           {
                rowsAffected += await DeleteAlbumAsync(album.ParentAlbum, logIfCleaned);
           } 
        }
        return rowsAffected;
    }


    public async Task<List<AlbumParents>> GetAlbumParentsAsync(long id)
    {
        var sql = @"WITH RECURSIVE ancestors AS (
                        SELECT id, album_name, parent_album::text, regexp_replace(album_name, '.*[\\/]', '')::text AS path, 0 AS depth
                        FROM album
                        WHERE id = @id
                        
                        UNION ALL
                        
                        SELECT t.id, t.album_name, t.parent_album::text, regexp_replace(t.album_name, '.*[\\/]', '')::text as path, a.depth + 1
                        FROM album t
                        JOIN ancestors a ON t.album_name = a.parent_album
                        WHERE a.depth < 100  -- safety limit
                    )
                    SELECT * FROM ancestors ORDER BY depth;";
        var parameters = new { id = id };
        var albumContent = await _db.QueryAsync(sql, reader => AlbumParents.CreateFromDataReader(reader), parameters);
        return albumContent;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// AlbumContentHierarchical Methods
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public async Task<List<AlbumContentHierarchical>> GetRecentImages()
    {
        var sql = "SELECT * FROM get_recent_images_content_hierarchical(200)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return albumContent;                 
    } 

    public async Task<List<AlbumContentHierarchical>> GetRandomImages()
    {
        var sql = "SELECT * FROM get_random_images_content_hierarchical(200)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return albumContent;                 
    }  

    public async Task<(List<AlbumContentHierarchical>, AlbumSearch)> GetAlbumContentHierarchicalByExpression(AlbumSearch albumSearch)
    {
        var expression = System.Text.RegularExpressions.Regex.Replace(albumSearch.Expression, @"\s+", " "); //normalize spaces
        var fields = new SqlFields { 
            DefaultField = "image_path", 
            DateField = "coalesce(exif.date_taken, vm.date_taken, ai.image_timestamp_utc)",
            //EXISTS (SELECT 1 FROM face_embedding fe JOIN face_person fp ON fe.face_person_id = fp.id WHERE fe.album_image_id = ai.id AND fp.name = '{FACE_NAME}')
            //FaceSQL = "EXISTS (SELECT 1 FROM image_people ip WHERE ip.album_image_id = ai.id AND ip.person_names @> Array['{FACE_NAME}'])",
            FaceField = "ip.person_names"
        };        
        var te = new SQLTokenEvaluator(fields, SQLTokenEvaluator.OPERATOR_TYPE.ILIKE_ANY_ARRAY, SQLTokenEvaluator.FIELD_TYPE.STRING);
        var parser =  new KeywordsExpressionParser(expression, new SQLSemantic(te));

        string where = (string)parser.Evaluate(null);        
        //Console.WriteLine($"Debug: AlbumContentByExpression SQL WHERE: {where}");
        
        // Group by SHA-256 (text) with image_path fallback (stable natural key)
        var select = albumSearch.GroupByPHash ? "SELECT DISTINCT ON (COALESCE(ai.image_sha256, ai.image_path))" : "SELECT";
        // Ensure ORDER BY starts with the DISTINCT ON expression; add a deterministic tie-breaker
        var orderby = albumSearch.GroupByPHash ? "ORDER BY COALESCE(ai.image_sha256, ai.image_path), ai.image_timestamp_utc DESC, ai.id DESC" : "ORDER BY ai.image_timestamp_utc DESC, ai.id DESC";
        var limitOffset1 = albumSearch.Limit > 0 ? $"select * from (" : "";
        var limitOffset2 = albumSearch.Limit > 0 ? $") LIMIT {albumSearch.Limit} OFFSET {albumSearch.Offset}" : "";
        var faces = $@"WITH 
                        -- Pre-aggregate which person names appear in each image (computed once)
                        image_people AS (
                            SELECT 
                                fe.album_image_id,
                                array_agg(DISTINCT lower(COALESCE(fp.name, 'box')))::text[] as person_names
                            FROM face_embedding fe
                            JOIN face_person fp ON fe.face_person_id = fp.id
                            --WHERE fp.name IS NOT NULL
                            GROUP BY fe.album_image_id
                        ),
                        -- Face details for final output
                        faces AS (
                            select fe.id as face_id, fe.face_person_id as person_id, fp.name as person_name, fe.album_image_id, fe.bounding_box_x, fe.bounding_box_y, fe.bounding_box_width, fe.bounding_box_height, fe.confidence
                            from face_person fp 
                            join face_embedding fe on fp.id = fe.face_person_id 
                        ),
                        locations as (
                            select lc.id as cluster_id, lc.tier_meters, lc.name, li.album_image_id, ST_Y(lc.centroid) AS centroid_latitude, ST_X(lc.centroid) AS centroid_longitude  
                            from location_cluster lc 
                            join location_cluster_item li on lc.id = li.cluster_id
                        ),
                        target_images as (
                            select ai.id
                            from album_image ai
                            join album a on ai.album_id = a.id
                            left join image_metadata exif on ai.id = exif.album_image_id
                            left join video_metadata vm on ai.id = vm.album_image_id
                            left join image_people ip on ai.id = ip.album_image_id
                            where {where}
                        ),
                        faces_agg as (
                            select album_image_id, json_agg(row_to_json(fe)) as faces
                            from faces fe
                            join target_images ti on ti.id = fe.album_image_id
                            group by album_image_id
                        ),
                        locations_agg as (
                            select album_image_id, json_agg(row_to_json(loc)) as locations
                            from locations loc
                            join target_images ti on ti.id = loc.album_image_id
                            group by album_image_id
                        )"; 
        var sql = $@"{faces} 
                    {limitOffset1}
                    {select}
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
                    ai.image_sha256 AS image_sha256,
                    ai.image_width,
                    ai.image_height,
                    ai.last_updated_utc,
                    coalesce(exif.date_taken, vm.date_taken, ai.image_timestamp_utc) AS item_timestamp_utc,
                    row_to_json(exif) AS image_metadata,
                    row_to_json(vm) AS video_metadata,
                    coalesce(fa.faces, null::json) AS faces,
                    coalesce(la.locations, null::json) AS locations,
                    a.role_id AS role_id
                FROM album_image ai
                JOIN album a ON ai.album_id = a.id
                LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
                LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
                LEFT JOIN faces_agg fa ON ai.id = fa.album_image_id
                LEFT JOIN image_people ip ON ai.id = ip.album_image_id
                LEFT JOIN locations_agg la ON ai.id = la.album_image_id
                JOIN target_images ti ON ai.id = ti.id
                {orderby}
                {limitOffset2};";
        //Console.WriteLine($"Debug: AlbumContentByExpression SQL: {sql}");
        var content = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        if (albumSearch.Limit > 0)
        {
            select = albumSearch.GroupByPHash ? ", COALESCE(ai.image_sha256, ai.image_path)" : "";
            // Ensure ORDER BY starts with the DISTINCT ON expression; add a deterministic tie-breaker
            var groupBy = albumSearch.GroupByPHash ? "GROUP BY COALESCE(ai.image_sha256, ai.image_path)" : "";
            sql = $@"{faces}
                    select count(*) FROM ( 
                        SELECT count(*) as count{select}
                        FROM album_image ai
                        JOIN target_images ti ON ai.id = ti.id
                        {groupBy}
                    )";
            //Console.WriteLine($"Debug: AlbumContentByExpression Count SQL: {sql}");
            albumSearch.Count = await _db.ExecuteScalarAsync<long>(sql);
        }
        return (content, albumSearch);
    }

    /// <summary>
    /// Get album content for a specific list of image IDs (used for face search).
    /// </summary>
    public async Task<List<AlbumContentHierarchical>> GetAlbumContentByImageIdsAsync(List<long> imageIds)
    {
        if (imageIds.Count == 0) return new List<AlbumContentHierarchical>();

        var sql = $@"
            WITH faces as (
                select fe.id as face_id, fe.face_person_id as person_id, fp.name as person_name, fe.album_image_id, fe.bounding_box_x, fe.bounding_box_y, fe.bounding_box_width, fe.bounding_box_height, fe.confidence
                from face_person fp 
                join face_embedding fe on fp.id = fe.face_person_id    
            ),
            locations as (
                select lc.id as cluster_id, lc.tier_meters, lc.name, li.album_image_id, ST_Y(lc.centroid) AS centroid_latitude, ST_X(lc.centroid) AS centroid_longitude  
                from location_cluster lc 
                join location_cluster_item li on lc.id = li.cluster_id
            ),
            target_images as (
                select unnest(@image_ids) as id
            ),
            faces_agg as (
                select album_image_id, json_agg(row_to_json(fe)) as faces
                from faces fe
                join target_images ti on ti.id = fe.album_image_id
                group by album_image_id
            ),
            locations_agg as (
                select album_image_id, json_agg(row_to_json(loc)) as locations
                from locations loc
                join target_images ti on ti.id = loc.album_image_id
                group by album_image_id
            )
            SELECT DISTINCT ON (COALESCE(ai.image_sha256, ai.image_path))
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
                ai.image_sha256 AS image_sha256,
                ai.image_width,
                ai.image_height,
                ai.last_updated_utc,
                ai.image_timestamp_utc AS item_timestamp_utc,
                row_to_json(exif) AS image_metadata,
                row_to_json(vm) AS video_metadata,
                coalesce(fa.faces, null::json) AS faces,
                coalesce(la.locations, null::json) AS locations,
                a.role_id AS role_id
            FROM album_image ai
            JOIN album a ON ai.album_id = a.id
            LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
            LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
            LEFT JOIN faces_agg fa ON ai.id = fa.album_image_id
            LEFT JOIN locations_agg la ON ai.id = la.album_image_id
            JOIN target_images ti ON ai.id = ti.id
            ORDER BY COALESCE(ai.image_sha256, ai.image_path), ai.image_timestamp_utc DESC";

        var content = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), new { image_ids = imageIds.ToArray() });
        return content;
    }


    /// <summary>
    /// Get only album record where album_name = albumName
    /// </summary>    
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
                        COALESCE(ai.image_sha256, cai.image_sha256, '') AS image_sha256,
                        COALESCE(ai.image_width, cai.image_width, 0) AS image_width,
                        COALESCE(ai.image_height, cai.image_height, 0) AS image_height,
                        a.last_updated_utc,
                        a.album_timestamp_utc AS item_timestamp_utc,
                        NULL::json AS image_metadata,
                        NULL::json AS video_metadata,
                        NULL::json AS faces,
                        NULL::json AS locations,
                        a.role_id AS role_id
                    FROM album AS a
                    LEFT JOIN album ca ON a.feature_image_path = ca.album_name              --get the child album
                    LEFT JOIN album_image ai ON a.feature_image_path = ai.image_path        --get the image record of the album feature image
                    LEFT JOIN album_image cai ON ca.feature_image_path = cai.image_path     --get the image record of the child album feature image
                    WHERE a.album_name = @album_name";
        var albums = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), album);
        return albums.FirstOrDefault();                         
    }


    /// <summary>
    /// Get sub-albums and images where album_name = albumName
    /// </summary>    
    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchicalByName(string albumName)
    {
        var parameters = new { album_name = albumName };
        var sql = "SELECT * FROM get_album_content_hierarchical_by_name(@album_name)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }    

    /// <summary>
    /// Get sub-albums and images where album_id = albumId
    /// </summary>    
    public async Task<List<AlbumContentHierarchical>> GetAlbumContentHierarchicalById(long albumId)
    {
        var parameters = new { album_id = albumId };
        var sql = "SELECT * FROM get_album_content_hierarchical_by_id(@album_id)";
        var albumContent = await _db.QueryAsync(sql, reader => AlbumContentHierarchical.CreateFromDataReader(reader), parameters);
        return albumContent;                 
    }    

    /// <summary>
    /// Get sub-albums and images for root
    /// </summary>    
    public async Task<List<AlbumContentHierarchical>> GetRootAlbumContentHierarchical()
    {
        var sql = "SELECT * FROM get_root_album_content_hierarchical()";
        var albumContent = await _db.QueryAsync(
            sql, 
            reader => AlbumContentHierarchical.CreateFromDataReader(reader));
        return albumContent;                 
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Virtual Album Methods
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public async Task<List<VirtualAlbum>> GetAllVirtualAlbumsAsync()
    {
        var sql = "select * from virtual_album";
        var albums = await _db.QueryAsync(sql, reader => VirtualAlbum.CreateFromDataReader(reader));
        return albums;                 
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


    public async Task<VirtualAlbum> UpsertVirtualAlbumAsync(VirtualAlbum album)
    {
        //Console.WriteLine($"TRY save db: {album}");  
        //insert or update existing virtual album record 
        var sql = $@"INSERT INTO virtual_album (album_name, album_description, album_expression, album_folder, album_type, persistent_expression, is_public, feature_image_path, last_updated_utc, created_timestamp_utc, parent_album, parent_album_id, role_id)
                               VALUES (@album_name, @album_description, @album_expression, @album_folder, @album_type, @persistent_expression, @is_public, @feature_image_path, @last_updated_utc, @created_timestamp_utc, @parent_album, @parent_album_id, @role_id)
                    ON CONFLICT (parent_album, album_name) DO UPDATE
                    SET
                        album_description = EXCLUDED.album_description,
                        album_expression = EXCLUDED.album_expression,
                        feature_image_path = EXCLUDED.feature_image_path,
                        last_updated_utc = EXCLUDED.last_updated_utc,
                        is_public = EXCLUDED.is_public,
                        persistent_expression = EXCLUDED.persistent_expression,
                        album_type = EXCLUDED.album_type,
                        album_folder = EXCLUDED.album_folder,
                        role_id = EXCLUDED.role_id                        
                    RETURNING id;";        
        album.Id = await _db.ExecuteScalarAsync<long>(sql, album);    
        //Console.WriteLine($"ran album save db: {album.AlbumName}");        
        return album; 
    }

    public async Task DeleteVirtualAlbumAsync(long id)
    {
        var sql = "DELETE FROM virtual_album WHERE id = @id";
        await _db.ExecuteAsync(sql, new { id });
    }

    public async Task<int> GetVirtualAlbumChildrenCountAsync(long parentId)
    {
        var sql = "SELECT COUNT(*) FROM virtual_album WHERE parent_album_id = @parentId";
        return await _db.ExecuteScalarAsync<int>(sql, new { parentId });
    }

    public async Task<List<AlbumParents>> GetVirtualAlbumParentsAsync(long id)
    {
        var sql = @"WITH RECURSIVE ancestors AS (
                        SELECT id, album_name, parent_album::text, album_name::text AS path, 0 AS depth
                        FROM virtual_album
                        WHERE id = @id
                        
                        UNION ALL
                        
                        SELECT t.id, t.album_name, t.parent_album::text, t.album_name::text AS path, a.depth + 1
                        FROM virtual_album t
                        JOIN ancestors a ON t.album_name = a.parent_album
                        WHERE a.depth < 100  -- safety limit
                    )
                    SELECT * FROM ancestors ORDER BY depth;";
        var parameters = new { id = id };
        var albumContent = await _db.QueryAsync(sql, reader => AlbumParents.CreateFromDataReader(reader), parameters);
        return albumContent;
    }

    public async Task<List<AlbumTree>> GetVirtualAlbumsTreeAsync()
    {
        var sql = @"WITH RECURSIVE ancestors AS (
                        SELECT id, album_name, album_type, album_description, feature_image_path,
                               last_updated_utc, created_timestamp_utc AS album_timestamp_utc,
                               parent_album, parent_album_id, role_id,
                               album_expression, album_folder, 0 as depth
                        FROM virtual_album a
                        WHERE parent_album_id = 0

                        UNION ALL

                        SELECT t.id, t.album_name, t.album_type, t.album_description, t.feature_image_path,
                               t.last_updated_utc, t.created_timestamp_utc AS album_timestamp_utc,
                               t.parent_album, t.parent_album_id, t.role_id,
                               t.album_expression, t.album_folder, a.depth + 1
                        FROM virtual_album t
                        JOIN ancestors a ON a.album_name = t.parent_album
                        WHERE a.depth < 100  -- safety limit
                    )
                    SELECT * FROM ancestors ORDER BY depth, album_timestamp_utc;";
        var albumTree = await _db.QueryAsync(sql, reader => AlbumTree.CreateFromDataReader(reader));
        return albumTree;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// AlbumSettings Methods
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
   
    public async Task<AlbumSettings?> GetAlbumSettingsByUniqueDataIdAsync(string uniqueDataId)
    {
        var sql = "SELECT * FROM album_settings WHERE unique_data_id = @unique_data_id";
        var parameters = new { unique_data_id = uniqueDataId };
        var albumSettings = await _db.QueryAsync(sql, reader => AlbumSettings.CreateFromDataReader(reader), parameters);
        return albumSettings.FirstOrDefault();
    }

    public async Task<AlbumSettings> AddOrUpdateAlbumSettingsAsync(AlbumSettings settings)
    {
        // Check if settings exist, then insert or update
        // Using explicit check because the unique index uses COALESCE expression which can't be used with ON CONFLICT
        AlbumSettings? existing;
        //Console.WriteLine($"Checking existing album settings for UniqueDataId: {settings.UniqueDataId}");
        existing = await GetAlbumSettingsByUniqueDataIdAsync(settings.UniqueDataId);
        
        if (existing != null)
        {
            //Console.WriteLine($"Updating existing album settings for UniqueDataId: {settings.UniqueDataId}");
            // Update existing record
            var updateSql = @"UPDATE album_settings
                              SET banner_position_y = @banner_position_y,
                                  album_sort = @album_sort,
                                  image_sort = @image_sort,
                                  last_updated_utc = @last_updated_utc
                              WHERE id = @id
                              RETURNING id;";
            settings.Id = existing.Id;
            await _db.ExecuteScalarAsync<long>(updateSql, settings);
        }
        else
        {
            //Console.WriteLine($"Inserting new album settings for UniqueDataId: {settings.UniqueDataId}");
            // Insert new record
            var insertSql = @"INSERT INTO album_settings (album_id, unique_data_id, user_id, is_virtual, banner_position_y, album_sort, image_sort, last_updated_utc)
                              VALUES (@album_id, @unique_data_id, @user_id, @is_virtual, @banner_position_y, @album_sort, @image_sort, @last_updated_utc)
                              RETURNING id;";
            settings.Id = await _db.ExecuteScalarAsync<long>(insertSql, settings);
        }
        return settings;
    }

}


    