-- ============================================================================
-- Profiles: Add age, phone, avatar preset
-- ============================================================================
-- Adds standardized profile contact fields used by Unity profile editing.
-- phone is stored in E.164 format (+15551234567) when present.
-- ============================================================================

ALTER TABLE public.profiles
  ADD COLUMN IF NOT EXISTS age INTEGER,
  ADD COLUMN IF NOT EXISTS phone TEXT,
  ADD COLUMN IF NOT EXISTS avatar_preset_id TEXT;

-- Constrain phone to E.164 when provided (optional field).
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'profiles_phone_e164_check'
  ) THEN
    ALTER TABLE public.profiles
      ADD CONSTRAINT profiles_phone_e164_check
      CHECK (phone IS NULL OR phone ~ '^\+[1-9][0-9]{7,14}$');
  END IF;
END $$;

COMMENT ON COLUMN public.profiles.age IS 'Player age collected for compliance and sponsor restrictions.';
COMMENT ON COLUMN public.profiles.phone IS 'Optional player phone in E.164 format.';
COMMENT ON COLUMN public.profiles.avatar_preset_id IS 'Optional preset avatar ID fallback for clients.';
