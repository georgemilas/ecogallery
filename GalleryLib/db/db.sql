---- PostgreSQL database schema for ecogallery --------------------------------

-- Enable once per database the pg_trgm extension for trigram indexing and searching
-- basically enables col ILIKE ANY(ARRAY[...]) to be fast by working against an index on col rather than table scan
CREATE EXTENSION pg_trgm;

------------------------------------------------------------------------------
----------------- public.album -----------------------------------------------
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.album;

CREATE TABLE
  public.album (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_name character varying(500) NOT NULL,
    album_type character varying(50) NOT NULL,
    album_description character varying(1500) NULL,
    last_updated_utc timestamp with time zone NULL,          --when the record was last updated
    album_timestamp_utc timestamp with time zone NOT NULL,   --when the image file that caused the album to be created was last updated
    feature_image_path character varying(500) NULL,
    parent_album character varying(500) NULL,
    parent_album_id bigint NULL
  );

ALTER TABLE
  public.album
ADD
  CONSTRAINT album_pkey PRIMARY KEY (id);


CREATE UNIQUE INDEX IF NOT EXISTS ux_album_album_name
ON public.album (album_name);

------------------------------------------------------------------------------
----------------- public.virtual_album -----------------------------------------------
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.virtual_album;

CREATE TABLE
  public.virtual_album (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_name character varying(500) NOT NULL,
    album_description character varying(1500) NULL,
    album_expression character varying(2500) NULL,
    album_folder character varying(500) NULL,
    album_type character varying(50) NOT NULL,               -- one of expression or folder 
    persistent_expression boolean NOT NULL DEFAULT false,    -- whether the expression is saved across sessions 
    is_public boolean NOT NULL DEFAULT true,                 -- whether the virtual album is public or private
    feature_image_path character varying(500) NULL,
    last_updated_utc timestamp with time zone NULL,          --when the record was last updated
    created_timestamp_utc timestamp with time zone NOT NULL,   --when the image file that caused the album to be created was last updated
    parent_album character varying(500) NULL,
    parent_album_id bigint NULL
  );    

ALTER TABLE
  public.virtual_album
ADD
  CONSTRAINT virtual_album_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_virtual_album_parent_album_album_name
ON public.virtual_album (parent_album, album_name);


------------------------------------------------------------------------------
----------------- public.album_settings --------------------------------------  
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.album_settings;

CREATE TABLE public.album_settings (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_id BIGINT NOT NULL DEFAULT 0,
    search_id character varying(100) NULL,       -- hash of search expression (for search result preferences)
    is_virtual boolean NOT NULL DEFAULT false,
    user_id BIGINT NOT NULL,
    banner_position_y INT NOT NULL DEFAULT 38,
    album_sort character varying(50) NOT NULL DEFAULT 'name-asc',
    image_sort character varying(50) NOT NULL DEFAULT 'timestamp-desc',
    last_updated_utc timestamp with time zone NULL          --when the record was last updated
);


ALTER TABLE
  public.album_settings
ADD
  CONSTRAINT album_settings_pkey PRIMARY KEY (id);

-- Unique constraint: either album_id (when search_id is null) or search_id (when album_id is 0)
CREATE UNIQUE INDEX IF NOT EXISTS ux_album_settings_album_id_user_id
ON public.album_settings (album_id, user_id, is_virtual, COALESCE(search_id, ''));



------------------------------------------------------------------------------
----------------- public.album_image -----------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.album_image;

CREATE TABLE
  public.album_image (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    image_name character varying(255) NOT NULL,
    image_path character varying(500) NOT NULL,
    image_description character varying(1500) NULL,
    image_type character varying(10) NOT NULL,
    last_updated_utc timestamp with time zone NOT NULL,      --when the record was last updated
    image_timestamp_utc timestamp with time zone NOT NULL,   --when the image file was last modified
    album_name character varying(500) NOT NULL,
    album_id bigint NOT NULL,
    image_sha256 character varying(64) NULL,                 --SHA-256 hash of the 400px thumbnail for duplicate detection
    image_width integer NOT NULL DEFAULT 0,                  --display width in pixels (rotation-corrected)
    image_height integer NOT NULL DEFAULT 0                  --display height in pixels (rotation-corrected)
  );

ALTER TABLE
  public.album_image
ADD
  CONSTRAINT "AlbumImage_pkey" PRIMARY KEY (id);


CREATE UNIQUE INDEX IF NOT EXISTS ux_album_image_image_path
ON public.album_image (image_path);


CREATE INDEX IF NOT EXISTS ux_album_image_album_name
ON public.album_image (album_name);

-- Create GIN index with trigram ops on image_path for faster ILIKE ANY(ARRAY[...]) searches
CREATE INDEX IF NOT EXISTS idx_album_image_image_path_trgm 
ON public.album_image USING GIN (image_path gin_trgm_ops);


-- Create an index on image_sha256 for grouping/filtering
CREATE INDEX IF NOT EXISTS idx_album_image_image_sha256 ON public.album_image (image_sha256);

-- Index for sorting by image_timestamp_utc (for recent images)
CREATE INDEX IF NOT EXISTS idx_album_image_timestamp_utc 
ON public.album_image (image_timestamp_utc DESC);


------------------------------------------------------------------------------
----------------- public.image_metadata ------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.image_metadata;

CREATE TABLE
  public.image_metadata (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_image_id bigint NOT NULL,
    camera character varying(100) NULL,
    lens character varying(100) NULL,
    focal_length character varying(150) NULL,
    aperture character varying(50) NULL,
    exposure_time character varying(150) NULL,
    iso integer NULL,
    date_taken timestamp with time zone NULL,
    rating integer NULL,
    date_modified timestamp with time zone NULL,
    flash character varying(150) NULL,
    metering_mode character varying(150) NULL,
    exposure_program character varying(150) NULL,
    exposure_bias character varying(50) NULL,
    exposure_mode character varying(150) NULL,
    white_balance character varying(150) NULL,
    color_space character varying(150) NULL,
    scene_capture_type character varying(150) NULL,
    circle_of_confusion numeric NULL,
    field_of_view numeric NULL,
    depth_of_field numeric NULL,         
    hyperfocal_distance numeric NULL,    --may be bigger than 9999.99 so we may need (12,2)
    normalized_light_value numeric NULL, 
    software character varying(100) NULL,
    serial_number character varying(100) NULL,
    lens_serial_number character varying(100) NULL,
    file_name character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    file_size_bytes bigint NULL,
    image_width integer NOT NULL DEFAULT 0,
    image_height integer NOT NULL DEFAULT 0,
    orientation integer NULL,
    last_updated_utc timestamp with time zone NOT NULL      --when the record was last updated
  );

ALTER TABLE
  public.image_metadata
ADD
  CONSTRAINT image_metadata_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_image_metadata_album_image_id
ON public.image_metadata (album_image_id);

-- Index for sorting by date_taken
CREATE INDEX IF NOT EXISTS idx_image_metadata_date_taken 
ON public.image_metadata (date_taken DESC) WHERE date_taken IS NOT NULL;

ALTER TABLE
  public.image_metadata
ADD
  CONSTRAINT fk_image_metadata_album_image
  FOREIGN KEY (album_image_id)
  REFERENCES public.album_image (id)
  ON DELETE CASCADE;


------------------------------------------------------------------------------
----------------- public.video_metadata --------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.video_metadata;

CREATE TABLE
  public.video_metadata (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_image_id bigint NOT NULL,
    file_name character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    file_size_bytes bigint NULL,
    date_taken timestamp with time zone NULL,
    date_modified timestamp with time zone NULL,
    duration interval NULL,                      -- Video duration as PostgreSQL interval
    video_width integer NOT NULL DEFAULT 0,
    video_height integer NOT NULL DEFAULT 0,
    video_codec character varying(100) NULL,
    audio_codec character varying(100) NULL,
    pixel_format character varying(100) NULL,
    frame_rate numeric NULL,               -- e.g., 29.970, 59.940
    video_bit_rate bigint NULL,
    audio_sample_rate integer NULL,              -- e.g., 44100, 48000
    audio_channels integer NULL,                 -- e.g., 2 for stereo
    audio_bit_rate bigint NULL,
    format_name character varying(150) NULL,     -- e.g., "mov,mp4,m4a,3gp,3g2,mj2"
    software character varying(255) NULL,        -- Format long name or encoder
    camera character varying(150) NULL,          -- Camera make and model
    rotation integer NULL,                       -- Rotation in degrees (0, 90, 180, 270)  
    last_updated_utc timestamp with time zone NOT NULL  -- when the record was last updated
  );

ALTER TABLE
  public.video_metadata
ADD
  CONSTRAINT video_metadata_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_video_metadata_album_image_id
ON public.video_metadata (album_image_id);

-- Index for sorting by date_taken
CREATE INDEX IF NOT EXISTS idx_video_metadata_date_taken 
ON public.video_metadata (date_taken DESC) WHERE date_taken IS NOT NULL;

ALTER TABLE
  public.video_metadata
ADD
  CONSTRAINT fk_video_metadata_album_image
  FOREIGN KEY (album_image_id)
  REFERENCES public.album_image (id)
  ON DELETE CASCADE;




------------------------------------------------------------------------------
----------------- public.users------------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.users;

CREATE TABLE
  public.users (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    username character varying(100) NOT NULL,
    email character varying(255) NOT NULL,
    password_hash character varying(255) NOT NULL,
    full_name character varying(255) NULL,
    is_active boolean NOT NULL DEFAULT true,
    is_admin boolean NOT NULL DEFAULT false,
    created_utc timestamp with time zone NOT NULL DEFAULT NOW(),
    last_login_utc timestamp with time zone NULL
  );

ALTER TABLE
  public.users
ADD
  CONSTRAINT users_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username
ON public.users (username);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email
ON public.users (email);

------------------------------------------------------------------------------
----------------- public.sessions ---------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.sessions;

CREATE TABLE
  public.sessions (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    session_token character varying(255) NOT NULL,
    user_id bigint NOT NULL,
    created_utc timestamp with time zone NOT NULL DEFAULT NOW(),
    expires_utc timestamp with time zone NOT NULL,
    last_activity_utc timestamp with time zone NOT NULL DEFAULT NOW(),
    ip_address character varying(45) NULL,
    user_agent character varying(500) NULL
  );

ALTER TABLE
  public.sessions
ADD
  CONSTRAINT sessions_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_session_session_token
ON public.sessions (session_token);

CREATE INDEX IF NOT EXISTS idx_session_user_id
ON public.sessions (user_id);

CREATE INDEX IF NOT EXISTS idx_session_expires_utc
ON public.sessions (expires_utc);

ALTER TABLE
  public.sessions 
ADD
  CONSTRAINT fk_session_user
  FOREIGN KEY (user_id)
  REFERENCES public.users (id)
  ON DELETE CASCADE;



------------------------------------------------------------------------------
----------------- public.user_tokens -----------------------------------------
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.user_tokens;

CREATE TABLE public.user_tokens (
    id SERIAL PRIMARY KEY,
    user_id BIGINT NULL REFERENCES public.users(id),
    token_type VARCHAR(50) NOT NULL DEFAULT 'password_reset',  -- e.g., 'password_reset', 'user_registration'
    token VARCHAR(128) NOT NULL UNIQUE,
    created_utc TIMESTAMP with time zone NOT NULL DEFAULT NOW(),
    expires_utc TIMESTAMP with time zone NOT NULL DEFAULT NOW() + INTERVAL '1 hour',
    used BOOLEAN NOT NULL DEFAULT FALSE
);
CREATE INDEX idx_user_tokens_token ON public.user_tokens(token);


------------------------------------------------------------------------------
----------------- public.face_person -----------------------------------------
-- Represents a named person (cluster of faces identified as the same person)
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.face_person;

CREATE TABLE public.face_person (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    name character varying(255) NULL,              -- User-assigned name (NULL if not yet labeled)
    representative_embedding real[] NULL,          -- Average embedding for this person (for quick comparison)
    face_count integer NOT NULL DEFAULT 0,         -- Number of faces in this cluster
    created_utc timestamp with time zone NOT NULL DEFAULT NOW(),
    last_updated_utc timestamp with time zone NOT NULL DEFAULT NOW()
);

ALTER TABLE public.face_person
ADD CONSTRAINT face_person_pkey PRIMARY KEY (id);

CREATE INDEX idx_face_person_name ON public.face_person (name) WHERE name IS NOT NULL;


------------------------------------------------------------------------------
----------------- public.face_embedding --------------------------------------
-- Stores individual face detections and their embeddings
------------------------------------------------------------------------------
DROP TABLE IF EXISTS public.face_embedding;

CREATE TABLE public.face_embedding (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_image_id bigint NOT NULL,
    face_person_id bigint NULL,                    -- NULL until clustered/labeled
    embedding real[] NOT NULL,                     -- 512-dimensional ArcFace embedding vector
    bounding_box_x integer NOT NULL,               -- Face bounding box in original image
    bounding_box_y integer NOT NULL,
    bounding_box_width integer NOT NULL,
    bounding_box_height integer NOT NULL,
    confidence real NOT NULL,                      -- Detection confidence score
    is_confirmed boolean NOT NULL DEFAULT false,   -- User confirmed this face belongs to person
    created_utc timestamp with time zone NOT NULL DEFAULT NOW(),
    last_updated_utc timestamp with time zone NOT NULL DEFAULT NOW()
);

ALTER TABLE public.face_embedding
ADD CONSTRAINT face_embedding_pkey PRIMARY KEY (id);

CREATE INDEX idx_face_embedding_album_image_id ON public.face_embedding (album_image_id);
CREATE INDEX idx_face_embedding_face_person_id ON public.face_embedding (face_person_id) WHERE face_person_id IS NOT NULL;

ALTER TABLE public.face_embedding
ADD CONSTRAINT fk_face_embedding_album_image
FOREIGN KEY (album_image_id)
REFERENCES public.album_image (id)
ON DELETE CASCADE;

ALTER TABLE public.face_embedding
ADD CONSTRAINT fk_face_embedding_face_person
FOREIGN KEY (face_person_id)
REFERENCES public.face_person (id)
ON DELETE SET NULL;
