-- ============================================================================
-- Migration: 010_coins_admin_rls.sql
-- Purpose: Allow admin dashboard to manage coins (SELECT all, INSERT, UPDATE, DELETE)
-- ============================================================================
-- The coins table currently has only "Public can view active coins" (SELECT
-- where status IN ('hidden','visible')). Admins need to see all coins and
-- create, edit, and delete coins from the dashboard.
-- ============================================================================

-- Ensure super_admin helper exists (may already exist from fix-rls-policy.sql)
CREATE OR REPLACE FUNCTION public.is_super_admin(check_user_id UUID DEFAULT auth.uid())
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN EXISTS (
    SELECT 1 FROM public.profiles
    WHERE id = check_user_id
    AND role = 'super_admin'
  );
END;
$$;

-- Admins can SELECT all coins (dashboard list includes collected/expired)
DROP POLICY IF EXISTS "Admins can view all coins" ON public.coins;
CREATE POLICY "Admins can view all coins" ON public.coins
  FOR SELECT
  USING (public.is_super_admin());

-- Admins can INSERT coins (create new coin)
DROP POLICY IF EXISTS "Admins can insert coins" ON public.coins;
CREATE POLICY "Admins can insert coins" ON public.coins
  FOR INSERT
  WITH CHECK (public.is_super_admin());

-- Admins can UPDATE coins (edit, move, status change)
DROP POLICY IF EXISTS "Admins can update coins" ON public.coins;
CREATE POLICY "Admins can update coins" ON public.coins
  FOR UPDATE
  USING (public.is_super_admin());

-- Admins can DELETE coins (remove from database)
DROP POLICY IF EXISTS "Admins can delete coins" ON public.coins;
CREATE POLICY "Admins can delete coins" ON public.coins
  FOR DELETE
  USING (public.is_super_admin());

-- Log
DO $$
BEGIN
  RAISE NOTICE 'Migration 010: Coins admin RLS policies created successfully';
END;
$$;
