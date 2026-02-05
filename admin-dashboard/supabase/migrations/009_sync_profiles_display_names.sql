-- ============================================================================
-- Sync Profiles Display Names from Auth
-- ============================================================================
-- Backfills profiles.full_name from auth.users for existing users who have
-- a display name in auth but null/empty in profiles. This fixes "Anonymous"
-- showing for players whose name exists in Supabase Auth (raw_user_meta_data).
--
-- Also adds an UPDATE trigger so future auth.users metadata changes sync to profiles.
-- Run in Supabase SQL Editor if applying manually.
-- ============================================================================

-- Backfill: Copy display name from auth.users to profiles where profiles.full_name is null/empty
UPDATE public.profiles p
SET 
  full_name = COALESCE(
    au.raw_user_meta_data->>'full_name',
    au.raw_user_meta_data->>'displayName'
  ),
  updated_at = NOW()
FROM auth.users au
WHERE au.id = p.id
  AND (p.full_name IS NULL OR TRIM(COALESCE(p.full_name, '')) = '')
  AND (au.raw_user_meta_data->>'full_name' IS NOT NULL OR au.raw_user_meta_data->>'displayName' IS NOT NULL);
