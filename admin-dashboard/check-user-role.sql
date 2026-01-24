-- Check and fix user role for admin dashboard access
-- Run this via Supabase CLI or SQL Editor

-- Check current role
SELECT id, email, role FROM profiles WHERE email = 'stevensills2@gmail.com';

-- Update role to super_admin if needed
UPDATE profiles 
SET role = 'super_admin' 
WHERE email = 'stevensills2@gmail.com'
RETURNING id, email, role;
