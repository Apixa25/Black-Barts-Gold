-- ============================================================================
-- Migration: 017_s2_spatial_context.sql
-- Purpose: Add canonical S2 spatial context to location-bearing tables
-- ============================================================================
-- Why this exists:
-- Black Bart's Gold now treats S2 cells as the canonical backend geography.
-- These columns are additive and nullable so we can:
--   1. stamp new writes immediately
--   2. backfill legacy rows safely
--   3. migrate hunt pressure and spawning in phases
-- ============================================================================

-- ============================================================================
-- SECTION A: PLAYER LOCATIONS
-- ============================================================================

ALTER TABLE public.player_locations
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

CREATE INDEX IF NOT EXISTS player_locations_s2_l17_idx
  ON public.player_locations (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS player_locations_s2_l14_idx
  ON public.player_locations (s2_cell_token_l14);

COMMENT ON COLUMN public.player_locations.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure cell token for the player''s current location';

COMMENT ON COLUMN public.player_locations.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell token for the player''s current location';

COMMENT ON COLUMN public.player_locations.current_zone_id IS
  'Optional named-zone overlay membership. Not canonical geography after Migration 017.';

-- ============================================================================
-- SECTION B: COINS
-- ============================================================================

ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

CREATE INDEX IF NOT EXISTS coins_s2_l17_idx
  ON public.coins (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS coins_s2_l14_idx
  ON public.coins (s2_cell_token_l14);

COMMENT ON COLUMN public.coins.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure cell token for the coin location';

COMMENT ON COLUMN public.coins.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell token for the coin location';

-- ============================================================================
-- SECTION C: SPAWN HISTORY
-- ============================================================================

ALTER TABLE public.spawn_history
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

CREATE INDEX IF NOT EXISTS spawn_history_s2_l17_idx
  ON public.spawn_history (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS spawn_history_s2_l14_idx
  ON public.spawn_history (s2_cell_token_l14);

COMMENT ON COLUMN public.spawn_history.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure cell token recorded at spawn time';

COMMENT ON COLUMN public.spawn_history.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell token recorded at spawn time';
