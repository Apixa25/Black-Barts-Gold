-- ============================================================================
-- Migration: 012_coin_model_color_bb.sql
-- Purpose: Add 'color_bb' to coin_model options (Color BB 3D coin as default option)
-- ============================================================================

ALTER TABLE public.coins
  DROP CONSTRAINT IF EXISTS coins_coin_model_check;

ALTER TABLE public.coins
  ADD CONSTRAINT coins_coin_model_check
  CHECK (coin_model IN ('bb_gold', 'prize_race', 'color_bb'));

COMMENT ON COLUMN public.coins.coin_model IS 'Which 3D coin graphic to show in AR: bb_gold, prize_race, color_bb';
