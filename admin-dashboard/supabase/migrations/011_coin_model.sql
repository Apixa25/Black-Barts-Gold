-- ============================================================================
-- Migration: 011_coin_model.sql
-- Purpose: Add coin_model to coins for selecting 3D graphic (BB Gold, Prize Race, etc.)
-- ============================================================================

ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS coin_model TEXT NOT NULL DEFAULT 'bb_gold'
  CHECK (coin_model IN ('bb_gold', 'prize_race'));

COMMENT ON COLUMN public.coins.coin_model IS 'Which 3D coin graphic to show in AR: bb_gold (Black Bart), prize_race (Prize Race)';
