---- PostgreSQL database schema for gmpictures --------------------------------


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
    album_type character varying(20) NOT NULL,
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
    album_type character varying(20) NOT NULL,               -- one of expression or folder 
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
----------------- public.album_image -----------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.album_image;

CREATE TABLE
  public.album_image (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    image_name character varying(255) NOT NULL,
    image_path character varying(500) NOT NULL,
    image_type character varying(10) NOT NULL,
    last_updated_utc timestamp with time zone NOT NULL,      --when the record was last updated
    image_timestamp_utc timestamp with time zone NOT NULL,   --when the image file was last modified
    album_name character varying(500) NOT NULL,
    album_id bigint NOT NULL,
    image_sha256 character varying(64) NULL                  --SHA-256 hash of the 400px thumbnail for duplicate detection
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


------------------------------------------------------------------------------
----------------- public.image_exif ------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.image_exif;

CREATE TABLE
  public.image_exif (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    album_image_id bigint NOT NULL,
    camera character varying(100) NULL,
    lens character varying(100) NULL,
    focal_length character varying(150) NULL,
    aperture character varying(20) NULL,
    exposure_time character varying(150) NULL,
    iso integer NULL,
    date_taken timestamp with time zone NULL,
    rating integer NULL,
    date_modified timestamp with time zone NULL,
    flash character varying(150) NULL,
    metering_mode character varying(150) NULL,
    exposure_program character varying(150) NULL,
    exposure_bias character varying(20) NULL,
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
    image_width integer NULL,
    image_height integer NULL,
    last_updated_utc timestamp with time zone NOT NULL      --when the record was last updated
  );

ALTER TABLE
  public.image_exif
ADD
  CONSTRAINT image_exif_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_image_exif_album_image_id
ON public.image_exif (album_image_id);

ALTER TABLE
  public.image_exif
ADD
  CONSTRAINT fk_image_exif_album_image
  FOREIGN KEY (album_image_id)
  REFERENCES public.album_image (id)
  ON DELETE CASCADE;


------------------------------------------------------------------------------
----------------- public.user ------------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.user;

CREATE TABLE
  public.user (
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
  public.user
ADD
  CONSTRAINT user_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_username
ON public.user (username);

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_email
ON public.user (email);


------------------------------------------------------------------------------
----------------- public.session ---------------------------------------------  
------------------------------------------------------------------------------

DROP TABLE IF EXISTS public.session;

CREATE TABLE
  public.session (
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
  public.session
ADD
  CONSTRAINT session_pkey PRIMARY KEY (id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_session_session_token
ON public.session (session_token);

CREATE INDEX IF NOT EXISTS idx_session_user_id
ON public.session (user_id);

CREATE INDEX IF NOT EXISTS idx_session_expires_utc
ON public.session (expires_utc);

ALTER TABLE
  public.session
ADD
  CONSTRAINT fk_session_user
  FOREIGN KEY (user_id)
  REFERENCES public.user (id)
  ON DELETE CASCADE;
