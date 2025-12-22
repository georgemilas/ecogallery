DROP FUNCTION IF EXISTS get_album_content_hierarchical_by_name(VARCHAR);

CREATE OR REPLACE FUNCTION get_album_content_hierarchical_by_name(p_album_name VARCHAR)
RETURNS TABLE (
    id BIGINT,
    item_name VARCHAR,
    item_description VARCHAR,
    item_type VARCHAR,
    parent_album_id BIGINT,
    parent_album_name VARCHAR,
    feature_item_type VARCHAR,
    feature_item_path VARCHAR,
    inner_feature_item_type VARCHAR,
    inner_feature_item_path VARCHAR,
    last_updated_utc TIMESTAMP WITH TIME ZONE,
    item_timestamp_utc TIMESTAMP WITH TIME ZONE,
    image_exif JSON
) AS $$
SELECT 
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
WHERE a.parent_album = p_album_name

UNION ALL

SELECT 
    ai.id, 
    ai.image_name AS item_name, 
    ai.image_description AS item_description,
    ai.image_type AS item_type, 
    ai.album_id as parent_album_id, 
    ai.album_name AS parent_album_name,
    ai.image_type AS feature_image_type, 
    ai.image_path, 
    ai.image_type, 
    ai.image_path, 
    ai.last_updated_utc,
    ai.image_timestamp_utc AS item_timestamp_utc,
    row_to_json(exif) AS image_exif
FROM album_image ai
LEFT JOIN image_exif exif ON ai.id = exif.album_image_id
WHERE ai.album_name = p_album_name

ORDER BY item_type
$$ LANGUAGE SQL;