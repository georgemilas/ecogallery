-- Create initial admin user
-- Password: admin123 (SHA-256 hash)
-- You should change this password after first login!

INSERT INTO public.user (username, email, password_hash, full_name, is_admin, created_utc)
VALUES (
  'admin',
  'gmilas@gmail.com',
  'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',   -- This is base64 of SHA256('admin123')
  'Administrator',  
  true,
  NOW()
)
ON CONFLICT (username) DO NOTHING;

-- Note: The password_hash above is just a placeholder.
-- You need to generate the actual hash by running this C# code:
-- using System.Security.Cryptography;
-- using System.Text;
-- var password = "admin123";
-- var bytes = Encoding.UTF8.GetBytes(password);
-- var hash = SHA256.Create().ComputeHash(bytes);
-- var passwordHash = Convert.ToBase64String(hash);
-- Console.WriteLine(passwordHash);

-- Or use the register endpoint to create users properly
