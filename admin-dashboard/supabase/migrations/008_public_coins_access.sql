-- ============================================================================
-- Migration: 008_public_coins_access.sql
-- Purpose: Allow public read access to visible/hidden coins for mobile app
-- ============================================================================
-- The Prize-Finder mobile app needs to fetch nearby coins without authentication.
-- This adds an RLS policy allowing anyone to SELECT coins with active status.
-- ============================================================================

-- Enable RLS on coins table if not already enabled
ALTER TABLE public.coins ENABLE ROW LEVEL SECURITY;

-- Drop existing public read policy if it exists (to avoid conflicts)
DROP POLICY IF EXISTS "Public can view active coins" ON public.coins;

-- Create policy allowing public read access to visible/hidden coins
-- This is necessary for the Prize-Finder mobile app to fetch coins
CREATE POLICY "Public can view active coins" ON public.coins
  FOR SELECT
  USING (status IN ('hidden', 'visible'));

-- Log that migration was applied
DO $$
BEGIN
  RAISE NOTICE 'Migration 008: Public coins access policy created successfully';
END
$$;
