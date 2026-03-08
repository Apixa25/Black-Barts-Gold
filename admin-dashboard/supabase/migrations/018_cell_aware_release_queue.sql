-- ============================================================================
-- Migration 018: Cell-aware spawn queue and timed releases
-- ============================================================================
-- Adds canonical S2 context and basic coin configuration fields so queued spawns
-- and timed releases can target the same cell-first geography as the Governor.

ALTER TABLE public.spawn_queue
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

CREATE INDEX IF NOT EXISTS spawn_queue_s2_l17_idx
  ON public.spawn_queue (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS spawn_queue_s2_l14_idx
  ON public.spawn_queue (s2_cell_token_l14);

COMMENT ON COLUMN public.spawn_queue.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure/spawn cell for queued spawns. Added in Migration 018.';

COMMENT ON COLUMN public.spawn_queue.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell for queued spawns. Added in Migration 018.';

ALTER TABLE public.release_schedules
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT,
  ADD COLUMN IF NOT EXISTS coin_tier TEXT NOT NULL DEFAULT 'bronze'
    CHECK (coin_tier IN ('gold', 'silver', 'bronze')),
  ADD COLUMN IF NOT EXISTS min_value DECIMAL(10, 2),
  ADD COLUMN IF NOT EXISTS max_value DECIMAL(10, 2);

CREATE INDEX IF NOT EXISTS release_schedules_s2_l17_idx
  ON public.release_schedules (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS release_schedules_s2_l14_idx
  ON public.release_schedules (s2_cell_token_l14);

COMMENT ON COLUMN public.release_schedules.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure/spawn cell for timed releases. Added in Migration 018.';

COMMENT ON COLUMN public.release_schedules.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell for timed releases. Added in Migration 018.';

COMMENT ON COLUMN public.release_schedules.coin_tier IS
  'Coin tier to enqueue/spawn for the release schedule. Added in Migration 018.';

ALTER TABLE public.release_batches
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT,
  ADD COLUMN IF NOT EXISTS coin_tier TEXT NOT NULL DEFAULT 'bronze'
    CHECK (coin_tier IN ('gold', 'silver', 'bronze'));

CREATE INDEX IF NOT EXISTS release_batches_s2_l17_idx
  ON public.release_batches (s2_cell_token_l17);

CREATE INDEX IF NOT EXISTS release_batches_s2_l14_idx
  ON public.release_batches (s2_cell_token_l14);

COMMENT ON COLUMN public.release_batches.s2_cell_token_l17 IS
  'Canonical S2 Level 17 pressure/spawn cell for the executed release batch. Added in Migration 018.';

COMMENT ON COLUMN public.release_batches.s2_cell_token_l14 IS
  'Canonical S2 Level 14 parent summary cell for the executed release batch. Added in Migration 018.';
