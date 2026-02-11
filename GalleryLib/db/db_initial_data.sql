------------------------------------------------------------------------------
----------------- ROLE DATA --------------------------------------------------
------------------------------------------------------------------------------
-- Add base roles and role hierarchy 

INSERT INTO public.roles (name, description) VALUES
('public', 'Public access'),
('private', 'Private album access'),
('client', 'Client access base'),
('user_admin', 'Invite users and manage roles'),
('album_admin', 'Create/modify albums and virtual albums'),
('admin', 'Full access')
-- ('ibm', 'IBM client role'),
-- ('microsoft', 'Microsoft client role')
ON CONFLICT DO NOTHING;

-- admin contains user_admin + album_admin
INSERT INTO public.role_hierarchy (parent_role_id, child_role_id)
SELECT p.id, c.id FROM public.roles p, public.roles c
WHERE c.name = 'admin' AND p.name IN ('user_admin', 'album_admin')
ON CONFLICT DO NOTHING;

-- user_admin, album_admin include public/private/client
INSERT INTO public.role_hierarchy (parent_role_id, child_role_id)
SELECT p.id, c.id FROM public.roles p, public.roles c
WHERE c.name IN ('user_admin','album_admin') AND p.name IN ('public','private','client')
ON CONFLICT DO NOTHING;

-- client includes public
INSERT INTO public.role_hierarchy (parent_role_id, child_role_id)
SELECT p.id, c.id FROM public.roles p, public.roles c
WHERE c.name IN ('client', 'private') AND p.name IN ('public')
ON CONFLICT DO NOTHING;

-- -- client roles include client base
-- INSERT INTO public.role_hierarchy (parent_role_id, child_role_id)
-- SELECT p.id, c.id FROM public.roles p, public.roles c
-- WHERE c.name IN ('ibm','microsoft') AND p.name = 'client'
-- ON CONFLICT DO NOTHING;


------------------------------------------------------------------------------
----------------- ADMIN USER -------------------------------------------------
------------------------------------------------------------------------------
-- Create initial admin user 

INSERT INTO public.users (username, email, password_hash, full_name, is_admin, created_utc)
VALUES (
  'admin',
  'admin@ecogalery.com',
  'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',   -- Dummy password (user should override) Base64(SHA256('admin123'))  
  'Administrator',  
  true,
  NOW()
)
ON CONFLICT (username) DO NOTHING;

INSERT INTO public.user_roles (user_id, role_id) VALUES (1, 6)    -- admin role
ON CONFLICT DO NOTHING;



