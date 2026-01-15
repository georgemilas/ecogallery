-- Create initial user 
-- user: admin
-- password: admin123 
-- You should change this password after first login!

INSERT INTO public.users (username, email, password_hash, full_name, is_admin, created_utc)
VALUES (
  'admin',
  'gmilas@gmail.com',
  'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',   -- This is Base64(SHA256('admin123'))  
  'Administrator',  
  true,
  NOW()
)
ON CONFLICT (username) DO NOTHING;

-- Use the reset password endpoint to create new passwords properly

