-- Clear all database objects to prepare for recreating schema
-- Run this before running db.sql to ensure a clean slate

-- Drop all functions first
DROP FUNCTION IF EXISTS get_album_content_hierarchical_by_name(VARCHAR) CASCADE;
DROP FUNCTION IF EXISTS get_album_content_hierarchical_by_id(BIGINT) CASCADE;
DROP FUNCTION IF EXISTS get_root_album_content_hierarchical() CASCADE;
DROP FUNCTION IF EXISTS get_recent_images_content_hierarchical(integer) CASCADE;
DROP FUNCTION IF EXISTS get_random_images_content_hierarchical(integer) CASCADE;

-- Drop tables in order respecting foreign key dependencies
-- Tables with foreign keys must be dropped before their referenced tables

-- Drop metadata tables (depend on album_image)
DROP TABLE IF EXISTS public.image_metadata CASCADE;
DROP TABLE IF EXISTS public.video_metadata CASCADE;

-- Drop album_image (depends on album)
DROP TABLE IF EXISTS public.album_image CASCADE;

-- Drop album_settings
DROP TABLE IF EXISTS public.album_settings CASCADE;

-- Drop album tables
DROP TABLE IF EXISTS public.album CASCADE;
DROP TABLE IF EXISTS public.virtual_album CASCADE;

-- Drop auth tables (sessions depends on users)
DROP TABLE IF EXISTS public.sessions CASCADE;
DROP TABLE IF EXISTS public.user_tokens CASCADE;
DROP TABLE IF EXISTS public.users CASCADE;

-- Optionally drop the extension (uncomment if you want to recreate it)
-- DROP EXTENSION IF EXISTS pg_trgm;

-- -- Verify all tables are dropped

-- DO $$
-- DECLARE
--     table_count INTEGER;
-- BEGIN
--         SELECT COUNT(*) INTO table_count
--         FROM information_schema.tables
--         WHERE table_schema = 'public'
--             AND table_type = 'BASE TABLE';

--     IF table_count > 0 THEN
--         RAISE NOTICE 'Warning: % tables still exist in public schema', table_count;
--     ELSE
--         RAISE NOTICE 'All tables cleared successfully';
--     END IF;
-- END;
-- $$ LANGUAGE plpgsql;
