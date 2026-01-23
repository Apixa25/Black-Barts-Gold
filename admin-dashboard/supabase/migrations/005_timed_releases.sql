-- ============================================================================
-- Timed Releases - M6: Timed Releases
-- ============================================================================
-- Schedule coin drops, batch releases, and hunt event scheduling.
-- Run after 004_auto_distribution.sql.
-- ============================================================================

-- ============================================================================
-- RELEASE SCHEDULES TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.release_schedules (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  zone_id UUID REFERENCES public.zones(id) ON DELETE CASCADE NOT NULL,
  name TEXT NOT NULL,
  description TEXT,

  total_coins INTEGER NOT NULL CHECK (total_coins > 0),
  coins_per_release INTEGER NOT NULL CHECK (coins_per_release > 0),
  release_interval_seconds INTEGER NOT NULL CHECK (release_interval_seconds >= 10),
  start_time TIMESTAMP WITH TIME ZONE NOT NULL,
  end_time TIMESTAMP WITH TIME ZONE,

  status TEXT DEFAULT 'scheduled' CHECK (
    status IN ('scheduled', 'active', 'paused', 'completed', 'cancelled')
  ),
  coins_released_so_far INTEGER DEFAULT 0,
  batches_completed INTEGER DEFAULT 0,
  next_release_at TIMESTAMP WITH TIME ZONE,
  last_release_at TIMESTAMP WITH TIME ZONE,

  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  created_by UUID REFERENCES public.profiles(id)
);

CREATE INDEX IF NOT EXISTS release_schedules_zone_idx ON public.release_schedules (zone_id);
CREATE INDEX IF NOT EXISTS release_schedules_status_idx ON public.release_schedules (status);
CREATE INDEX IF NOT EXISTS release_schedules_next_idx ON public.release_schedules (next_release_at)
  WHERE status IN ('scheduled', 'active');

ALTER TABLE public.release_schedules ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Admins manage release schedules" ON public.release_schedules
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

-- ============================================================================
-- RELEASE BATCHES TABLE (optional audit of each batch)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.release_batches (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  schedule_id UUID REFERENCES public.release_schedules(id) ON DELETE CASCADE NOT NULL,
  zone_id UUID NOT NULL,
  release_at TIMESTAMP WITH TIME ZONE NOT NULL,
  coins_count INTEGER NOT NULL,
  coins_released INTEGER DEFAULT 0,
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'released', 'partial', 'failed')),
  error_message TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS release_batches_schedule_idx ON public.release_batches (schedule_id);

ALTER TABLE public.release_batches ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Admins view release batches" ON public.release_batches
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

-- ============================================================================
-- PROCESS TIMED RELEASES FUNCTION
-- ============================================================================
-- Call periodically (e.g. every 30s) to release coins for schedules whose
-- next_release_at <= NOW().

CREATE OR REPLACE FUNCTION public.process_timed_releases()
RETURNS INTEGER AS $$
DECLARE
  r RECORD;
  released INTEGER := 0;
  batch_coins INTEGER;
  remain INTEGER;
BEGIN
  FOR r IN
    SELECT * FROM public.release_schedules
    WHERE status IN ('scheduled', 'active')
      AND next_release_at IS NOT NULL
      AND next_release_at <= NOW()
    ORDER BY next_release_at
    LIMIT 20
  LOOP
    batch_coins := LEAST(r.coins_per_release, r.total_coins - r.coins_released_so_far);
    IF batch_coins <= 0 THEN
      UPDATE public.release_schedules
      SET status = 'completed',
          next_release_at = NULL,
          updated_at = NOW()
      WHERE id = r.id;
      CONTINUE;
    END IF;

    BEGIN
      FOR i IN 1..batch_coins LOOP
        PERFORM public.spawn_coin(
          r.zone_id,
          'scheduled',
          'fixed',
          'bronze',
          NULL,
          NULL,
          NULL
        );
      END LOOP;

      remain := r.total_coins - r.coins_released_so_far - batch_coins;
      UPDATE public.release_schedules
      SET coins_released_so_far = coins_released_so_far + batch_coins,
          batches_completed = batches_completed + 1,
          last_release_at = NOW(),
          next_release_at = CASE
            WHEN remain <= 0 THEN NULL
            ELSE NOW() + (r.release_interval_seconds || ' seconds')::INTERVAL
          END,
          status = CASE WHEN remain <= 0 THEN 'completed' ELSE 'active' END,
          updated_at = NOW()
      WHERE id = r.id;

      released := released + batch_coins;
    EXCEPTION WHEN OTHERS THEN
      UPDATE public.release_schedules
      SET updated_at = NOW()
      WHERE id = r.id;
      RAISE WARNING 'Timed release % failed: %', r.id, SQLERRM;
    END;
  END LOOP;

  RETURN released;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- HELPER: Create schedule and enqueue first batch
-- ============================================================================

CREATE OR REPLACE FUNCTION public.create_release_schedule(
  p_zone_id UUID,
  p_name TEXT,
  p_description TEXT DEFAULT NULL,
  p_total_coins INTEGER DEFAULT 100,
  p_coins_per_release INTEGER DEFAULT 1,
  p_release_interval_seconds INTEGER DEFAULT 60,
  p_start_time TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
  v_id UUID;
  v_start TIMESTAMP WITH TIME ZONE;
BEGIN
  v_start := COALESCE(p_start_time, NOW());
  INSERT INTO public.release_schedules (
    zone_id, name, description,
    total_coins, coins_per_release, release_interval_seconds,
    start_time, next_release_at, status
  ) VALUES (
    p_zone_id, p_name, p_description,
    p_total_coins, p_coins_per_release, p_release_interval_seconds,
    v_start, v_start, 'scheduled'
  )
  RETURNING id INTO v_id;
  RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON TABLE public.release_schedules IS 'M6: Timed release schedules for hunt events';
COMMENT ON TABLE public.release_batches IS 'M6: Audit of each release batch';
COMMENT ON FUNCTION public.process_timed_releases IS 'Process due timed releases';
COMMENT ON FUNCTION public.create_release_schedule IS 'Create a new timed release schedule';
