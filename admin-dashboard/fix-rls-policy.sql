-- Fix RLS policy for player_locations
-- The current policy may have RLS evaluation issues
-- This creates a SECURITY DEFINER function to safely check roles

-- Create a helper function to check admin role (bypasses RLS)
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

-- Drop and recreate the policy
DROP POLICY IF EXISTS "Admins can view all player locations" ON public.player_locations;

CREATE POLICY "Admins can view all player locations" ON public.player_locations
  FOR SELECT USING (
    public.is_super_admin() OR
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'sponsor_admin'
    )
  );

-- Also ensure users can see their own location
CREATE POLICY IF NOT EXISTS "Users can view own location" ON public.player_locations
  FOR SELECT USING (auth.uid() = user_id);
