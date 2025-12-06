DROP FUNCTION IF EXISTS get_album_content_hierarchical_by_name(VARCHAR);

CREATE OR REPLACE FUNCTION get_album_content_hierarchical_by_name(p_album_name VARCHAR)
RETURNS TABLE (
    id BIGINT,
    item_name VARCHAR,
    item_type VARCHAR,
    feature_item_type VARCHAR,
    feature_item_path VARCHAR,
    inner_feature_item_type VARCHAR,
    inner_feature_item_path VARCHAR,
    last_updated_utc TIMESTAMP WITH TIME ZONE
) AS $$
SELECT 
    pa.id, 
    pa.album_name AS item_name, 
    pa.album_type AS item_type, 
    ai.image_type AS feature_item_type, 
    pa.feature_image_path AS feature_item_path, 
    iai.image_type AS inner_feature_item_type, 
    a.feature_image_path AS inner_feature_item_path, 
    pa.last_updated_utc  
FROM album AS pa
LEFT JOIN album_image ai ON pa.feature_image_path = ai.image_path
LEFT JOIN album a ON pa.feature_image_path = a.album_name
LEFT JOIN album_image iai ON a.feature_image_path = iai.image_path
WHERE pa.parent_album = p_album_name

UNION 

SELECT 
    ai.id, 
    ai.image_name AS item_name, 
    ai.image_type AS item_type, 
    ai.image_type AS feature_image_type, 
    ai.image_path, 
    ai.image_type, 
    ai.image_path, 
    ai.last_updated_utc   
FROM album_image ai
WHERE ai.album_name = p_album_name

ORDER BY item_type
$$ LANGUAGE SQL;