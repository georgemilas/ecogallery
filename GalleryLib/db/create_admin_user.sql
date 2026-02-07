-- Create initial user 
-- user: admin
-- password: admin123 
-- You should change this password after first login!

INSERT INTO public.users (username, email, password_hash, full_name, is_admin, created_utc)
VALUES (
  'admin',
  'admin@ecogalery.com',
  'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',   -- This is Base64(SHA256('admin123'))  
  'Administrator',  
  true,
  NOW()
)
ON CONFLICT (username) DO NOTHING;

INSERT INTO public.user_roles (user_id, role_id) VALUES (1, 6)
ON CONFLICT DO NOTHING;

-- Use the reset password endpoint to create new passwords properly

