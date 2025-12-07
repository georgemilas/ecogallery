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

