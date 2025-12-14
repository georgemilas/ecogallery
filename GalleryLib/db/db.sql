---- PostgreSQL database schema for gmpictures --------------------------------


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
    album_id bigint NOT NULL
  );

ALTER TABLE
  public.album_image
ADD
  CONSTRAINT "AlbumImage_pkey" PRIMARY KEY (id);


CREATE UNIQUE INDEX IF NOT EXISTS ux_album_image_image_path
ON public.album_image (image_path);


CREATE INDEX IF NOT EXISTS ux_album_image_album_name
ON public.album_image (album_name);


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


