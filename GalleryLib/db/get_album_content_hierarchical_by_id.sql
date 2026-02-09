DROP FUNCTION IF EXISTS get_album_content_hierarchical_by_id(BIGINT);

CREATE OR REPLACE FUNCTION get_album_content_hierarchical_by_id(p_album_id BIGINT)
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
    video_metadata JSON,
    faces JSON,
    locations JSON,
    role_id BIGINT
) AS $$
WITH faces as (
  select fe.id as face_id, fe.face_person_id as person_id, fp.name as person_name, fe.album_image_id, fe.bounding_box_x, fe.bounding_box_y, fe.bounding_box_width, fe.bounding_box_height, fe.confidence
  from face_person fp 
  join face_embedding fe on fp.id = fe.face_person_id    
),
locations as (
  select lc.id as cluster_id, lc.tier_meters, lc.name, li.album_image_id, ST_Y(lc.centroid) AS centroid_latitude, ST_X(lc.centroid) AS centroid_longitude  
  from location_cluster lc 
  join location_cluster_item li on lc.id = li.cluster_id
) 
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
WHERE a.parent_album_id = p_album_id

UNION ALL

SELECT
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
    coalesce(json_agg(row_to_json(fe)) FILTER (WHERE fe.face_id is not NULL), null::json) AS faces,
    coalesce(json_agg(row_to_json(loc)) FILTER (WHERE loc.cluster_id is not NULL), null::json) AS locations,
    a.role_id AS role_id
FROM album_image ai
LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
JOIN album AS a ON ai.album_name = a.album_name
LEFT JOIN faces fe ON ai.id = fe.album_image_id
LEFT JOIN locations loc ON ai.id = loc.album_image_id
WHERE a.id = p_album_id
GROUP BY ai.id, exif.id, vm.id, a.id

ORDER BY item_type
$$ LANGUAGE SQL;