
DROP FUNCTION IF EXISTS get_recent_images_content_hierarchical(integer);

CREATE OR REPLACE FUNCTION get_recent_images_content_hierarchical(p_count integer DEFAULT 100)
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
WITH recent_with_dates AS (
    -- Pre-select recent images using correct date (EXIF for images, date_taken for videos)
    SELECT ai.id
    FROM album_image ai
    LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
    LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
    ORDER BY COALESCE(exif.date_taken, vm.date_taken, ai.image_timestamp_utc) DESC
    LIMIT p_count
), 
faces as (
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
  select id
  from recent_with_dates
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
    coalesce(fa.faces, null::json) AS faces,
    coalesce(la.locations, null::json) AS locations,
    a.role_id AS role_id
FROM recent_with_dates rwd
INNER JOIN album_image ai ON ai.id = rwd.id
JOIN album AS a ON ai.album_name = a.album_name
LEFT JOIN image_metadata exif ON ai.id = exif.album_image_id
LEFT JOIN video_metadata vm ON ai.id = vm.album_image_id
  LEFT JOIN faces_agg fa ON ai.id = fa.album_image_id
  LEFT JOIN locations_agg la ON ai.id = la.album_image_id

$$ LANGUAGE SQL;