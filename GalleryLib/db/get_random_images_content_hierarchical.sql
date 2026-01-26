
DROP FUNCTION IF EXISTS get_random_images_content_hierarchical(integer);

CREATE OR REPLACE FUNCTION get_random_images_content_hierarchical(p_count integer DEFAULT 100)
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
    image_sha256 VARCHAR,
    image_width INTEGER,
    image_height INTEGER,
    last_updated_utc TIMESTAMP WITH TIME ZONE,
    item_timestamp_utc TIMESTAMP WITH TIME ZONE,
    image_metadata JSON,
    video_metadata JSON
) AS $$
SELECT DISTINCT ON (random(), COALESCE(ai.image_sha256, ai.image_path))
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
    row_to_json(vm) AS video_metadata
FROM album_image ai
LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
ORDER BY random(), COALESCE(ai.image_sha256, ai.image_path) asc
LIMIT p_count

$$ LANGUAGE SQL;