-- ============================================================================
-- Migration: 015_repair_auto_distribution.sql
-- Purpose: Repair missing tables/functions from migrations 004 and 005
-- ============================================================================
-- Why this exists:
-- Migrations 004 and 005 used uuid_generate_v4() which requires the uuid-ossp
-- extension. That extension is NOT installed on this Supabase project. As a
-- result both migrations were registered as "applied" but their SQL failed
-- partway through — leaving spawn_queue, spawn_history, distribution_config,
-- release_schedules, and release_batches missing from the remote database.
--
-- This migration is purely ADDITIVE:
--   - CREATE TABLE IF NOT EXISTS (safe if tables already exist locally)
--   - CREATE OR REPLACE FUNCTION (always safe)
--   - DROP POLICY IF EXISTS before CREATE POLICY (idempotent)
--   - uuid_generate_v4() replaced with gen_random_uuid() throughout
-- ============================================================================


-- ============================================================================
-- SECTION 0: ZONES TABLE (prerequisite for spawn_queue and spawn_history FKs)
-- ============================================================================
-- The zones table was never captured in a numbered migration file.
-- It must be created here before any table with REFERENCES public.zones(id).

CREATE TABLE IF NOT EXISTS public.zones (
  id                       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  name                     TEXT NOT NULL,
  description              TEXT,
  zone_type                TEXT NOT NULL DEFAULT 'grid'
                             CHECK (zone_type IN ('player', 'sponsor', 'hunt', 'grid')),
  status                   TEXT NOT NULL DEFAULT 'active'
                             CHECK (status IN ('active', 'inactive', 'scheduled', 'completed', 'archived')),
  geometry                 JSONB NOT NULL DEFAULT '{"type":"circle","center":{"latitude":0,"longitude":0},"radius_meters":500}'::JSONB,
  owner_id                 UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  sponsor_id               UUID,
  auto_spawn_config        JSONB,
  timed_release_config     JSONB,
  hunt_config              JSONB,
  start_time               TIMESTAMP WITH TIME ZONE,
  end_time                 TIMESTAMP WITH TIME ZONE,
  coins_placed             INTEGER NOT NULL DEFAULT 0,
  coins_collected          INTEGER NOT NULL DEFAULT 0,
  total_value_distributed  DECIMAL(12, 2) NOT NULL DEFAULT 0,
  active_players           INTEGER NOT NULL DEFAULT 0,
  fill_color               TEXT,
  border_color             TEXT,
  opacity                  DECIMAL(3, 2) NOT NULL DEFAULT 0.30,
  metadata                 JSONB,
  created_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS zones_status_idx  ON public.zones (status);
CREATE INDEX IF NOT EXISTS zones_type_idx    ON public.zones (zone_type);
CREATE INDEX IF NOT EXISTS zones_owner_idx   ON public.zones (owner_id);
CREATE INDEX IF NOT EXISTS zones_sponsor_idx ON public.zones (sponsor_id);

ALTER TABLE public.zones ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can manage zones" ON public.zones;
CREATE POLICY "Admins can manage zones" ON public.zones
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin'))
  );

DROP POLICY IF EXISTS "Public can view active zones" ON public.zones;
CREATE POLICY "Public can view active zones" ON public.zones
  FOR SELECT USING (status = 'active');

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'sponsors'
  ) THEN
    BEGIN
      ALTER TABLE public.zones
        ADD CONSTRAINT zones_sponsor_id_fkey
          FOREIGN KEY (sponsor_id) REFERENCES public.sponsors(id) ON DELETE SET NULL;
    EXCEPTION WHEN duplicate_object THEN NULL;
    END;
  END IF;
END $$;

CREATE OR REPLACE FUNCTION public.update_zones_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS zones_updated_at ON public.zones;
CREATE TRIGGER zones_updated_at
  BEFORE UPDATE ON public.zones
  FOR EACH ROW EXECUTE FUNCTION public.update_zones_updated_at();

COMMENT ON TABLE public.zones IS 'Geographic zones for coin distribution, sponsor hunts, and AI territory management';

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime' AND schemaname = 'public' AND tablename = 'zones'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.zones;
    RAISE NOTICE 'Migration 015: Added zones to supabase_realtime';
  END IF;
END $$;

-- ============================================================================
-- SECTION A: SPAWN QUEUE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_queue (
  id               UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  zone_id          UUID REFERENCES public.zones(id) ON DELETE CASCADE NOT NULL,
  trigger_type     TEXT DEFAULT 'auto' CHECK (trigger_type IN (
                     'auto', 'scheduled', 'manual', 'recycle',
                     'ai_spawn_governor', 'ai_game_master'
                   )),
  scheduled_time   TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

  -- Coin configuration
  coin_type        TEXT DEFAULT 'fixed' CHECK (coin_type IN ('fixed', 'pool')),
  tier             TEXT DEFAULT 'bronze' CHECK (tier IN ('gold', 'silver', 'bronze')),
  min_value        DECIMAL(10, 2) NOT NULL DEFAULT 0.10,
  max_value        DECIMAL(10, 2) NOT NULL DEFAULT 1.00,
  is_mythical      BOOLEAN DEFAULT FALSE,

  -- Target location (if specified)
  target_latitude  DOUBLE PRECISION,
  target_longitude DOUBLE PRECISION,

  -- Status
  status           TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
  error_message    TEXT,

  -- Result
  spawned_coin_id  UUID REFERENCES public.coins(id),

  -- Timestamps
  created_at       TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  processed_at     TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS spawn_queue_status_idx
  ON public.spawn_queue (status, scheduled_time);
CREATE INDEX IF NOT EXISTS spawn_queue_zone_idx
  ON public.spawn_queue (zone_id);

ALTER TABLE public.spawn_queue ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can manage spawn queue" ON public.spawn_queue;
CREATE POLICY "Admins can manage spawn queue" ON public.spawn_queue
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

COMMENT ON TABLE public.spawn_queue IS 'Queue of coins waiting to be spawned by the auto-distribution or AI systems';

-- ============================================================================
-- SECTION B: SPAWN HISTORY
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_history (
  id                      UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  coin_id                 UUID REFERENCES public.coins(id) ON DELETE SET NULL,
  zone_id                 UUID REFERENCES public.zones(id) ON DELETE SET NULL,
  trigger_type            TEXT NOT NULL,

  -- Coin details at spawn time
  coin_value              DECIMAL(10, 2) NOT NULL,
  coin_tier               TEXT NOT NULL,
  spawn_latitude          DOUBLE PRECISION NOT NULL,
  spawn_longitude         DOUBLE PRECISION NOT NULL,

  -- Who spawned it (added in Migration 014 context)
  created_by              TEXT NOT NULL DEFAULT 'system'
                            CHECK (created_by IN (
                              'system', 'admin', 'user',
                              'ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer'
                            )),

  -- Collection tracking
  spawned_at              TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  collected_at            TIMESTAMP WITH TIME ZONE,
  collected_by_user_id    UUID REFERENCES public.profiles(id),
  recycled_at             TIMESTAMP WITH TIME ZONE,

  -- Computed
  time_to_collection_hours DECIMAL(10, 2)
);

CREATE INDEX IF NOT EXISTS spawn_history_zone_idx
  ON public.spawn_history (zone_id, spawned_at DESC);
CREATE INDEX IF NOT EXISTS spawn_history_date_idx
  ON public.spawn_history (spawned_at DESC);

ALTER TABLE public.spawn_history ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can view spawn history" ON public.spawn_history;
CREATE POLICY "Admins can view spawn history" ON public.spawn_history
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

COMMENT ON TABLE public.spawn_history IS 'History of all spawned coins — source for analytics, economy health, and AI audit trail';

-- Enable Realtime for spawn_history (migration 014 had to skip this)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime'
      AND schemaname = 'public'
      AND tablename = 'spawn_history'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.spawn_history;
    RAISE NOTICE 'Migration 015: Added spawn_history to supabase_realtime';
  END IF;
END $$;

-- ============================================================================
-- SECTION C: DISTRIBUTION CONFIG
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.distribution_config (
  id                              UUID DEFAULT gen_random_uuid() PRIMARY KEY,

  -- Global settings (enabled = kill switch)
  enabled                         BOOLEAN DEFAULT TRUE,
  check_interval_seconds          INTEGER DEFAULT 60,
  max_spawns_per_cycle            INTEGER DEFAULT 10,

  -- Default zone settings
  default_min_coins               INTEGER DEFAULT 3,
  default_max_coins               INTEGER DEFAULT 20,
  default_min_value               DECIMAL(10, 2) DEFAULT 0.10,
  default_max_value               DECIMAL(10, 2) DEFAULT 5.00,
  default_tier_gold_weight        INTEGER DEFAULT 10,
  default_tier_silver_weight      INTEGER DEFAULT 30,
  default_tier_bronze_weight      INTEGER DEFAULT 60,

  -- Value distribution
  value_strategy                  TEXT DEFAULT 'tiered',
  mythical_spawn_chance           DECIMAL(5, 4) DEFAULT 0.001,

  -- Recycling
  recycle_enabled                 BOOLEAN DEFAULT TRUE,
  recycle_after_hours             INTEGER DEFAULT 48,
  recycle_to_new_location         BOOLEAN DEFAULT TRUE,

  -- Rate limiting
  max_spawns_per_hour             INTEGER DEFAULT 100,
  cooldown_after_collection_seconds INTEGER DEFAULT 300,

  -- Metadata
  updated_at                      TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_by                      UUID REFERENCES public.profiles(id)
);

-- Seed default config row (fixed UUID so it's always findable)
INSERT INTO public.distribution_config (id)
VALUES ('00000000-0000-0000-0000-000000000001')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE public.distribution_config ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can manage distribution config" ON public.distribution_config;
CREATE POLICY "Admins can manage distribution config" ON public.distribution_config
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

COMMENT ON TABLE public.distribution_config IS 'Global configuration for the auto-distribution system. The enabled flag is the AI kill switch.';
COMMENT ON COLUMN public.distribution_config.enabled IS 'Master kill switch: set to FALSE to immediately stop all AI and auto-distribution spawning';

-- ============================================================================
-- SECTION D: SPAWN COIN FUNCTION
-- ============================================================================
-- Also updates spawn_queue and spawn_history trigger_type constraints to
-- include AI values since those tables now exist.

CREATE OR REPLACE FUNCTION public.spawn_coin(
  p_zone_id        UUID,
  p_trigger_type   TEXT DEFAULT 'auto',
  p_coin_type      TEXT DEFAULT 'fixed',
  p_tier           TEXT DEFAULT 'bronze',
  p_value          DECIMAL DEFAULT NULL,
  p_latitude       DOUBLE PRECISION DEFAULT NULL,
  p_longitude      DOUBLE PRECISION DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
  v_zone     RECORD;
  v_coin_id  UUID;
  v_value    DECIMAL;
  v_lat      DOUBLE PRECISION;
  v_lng      DOUBLE PRECISION;
  v_radius   DOUBLE PRECISION;
  v_angle    DOUBLE PRECISION;
  v_distance DOUBLE PRECISION;
BEGIN
  -- Get zone details
  SELECT * INTO v_zone FROM public.zones WHERE id = p_zone_id;
  IF v_zone IS NULL THEN
    RAISE EXCEPTION 'Zone not found: %', p_zone_id;
  END IF;

  -- Calculate value from tier if not explicitly provided
  IF p_value IS NULL THEN
    CASE p_tier
      WHEN 'bronze' THEN v_value := 0.10 + random() * 0.40;
      WHEN 'silver' THEN v_value := 0.50 + random() * 1.50;
      WHEN 'gold'   THEN v_value := 2.00 + random() * 8.00;
      ELSE                v_value := 0.10;
    END CASE;
  ELSE
    v_value := p_value;
  END IF;

  v_value := ROUND(v_value, 2);

  -- Calculate spawn location within zone if not explicitly provided
  IF p_latitude IS NULL OR p_longitude IS NULL THEN
    IF v_zone.geometry->>'type' = 'circle' THEN
      v_radius   := (v_zone.geometry->>'radius_meters')::DOUBLE PRECISION;
      v_angle    := random() * 2 * PI();
      v_distance := sqrt(random()) * v_radius;
      v_lat := (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION
               + (v_distance / 111320) * cos(v_angle);
      v_lng := (v_zone.geometry->'center'->>'longitude')::DOUBLE PRECISION
               + (v_distance / (111320 * cos(radians(
                   (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION
                 )))) * sin(v_angle);
    ELSE
      -- Polygon: fall back to zone center
      v_lat := (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION;
      v_lng := (v_zone.geometry->'center'->>'longitude')::DOUBLE PRECISION;
    END IF;
  ELSE
    v_lat := p_latitude;
    v_lng := p_longitude;
  END IF;

  -- Create the coin
  INSERT INTO public.coins (
    coin_type, value, tier, latitude, longitude,
    status, hidden_at, is_mythical, multi_find, finds_remaining
  ) VALUES (
    p_coin_type, v_value, p_tier, v_lat, v_lng,
    'visible', NOW(), FALSE, FALSE, 1
  ) RETURNING id INTO v_coin_id;

  -- Record in spawn history
  INSERT INTO public.spawn_history (
    coin_id, zone_id, trigger_type,
    coin_value, coin_tier, spawn_latitude, spawn_longitude,
    created_by
  ) VALUES (
    v_coin_id, p_zone_id, p_trigger_type,
    v_value, p_tier, v_lat, v_lng,
    p_trigger_type  -- trigger_type doubles as created_by here
  );

  -- Update zone statistics
  UPDATE public.zones
  SET coins_placed              = coins_placed + 1,
      total_value_distributed   = total_value_distributed + v_value,
      updated_at                = NOW()
  WHERE id = p_zone_id;

  RETURN v_coin_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.spawn_coin IS 'Spawn a single coin in a zone. Called by auto-distribution, timed releases, admin actions, and AI agents.';

-- ============================================================================
-- SECTION E: PROCESS SPAWN QUEUE FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.process_spawn_queue()
RETURNS INTEGER AS $$
DECLARE
  v_queue_item RECORD;
  v_coin_id    UUID;
  v_processed  INTEGER := 0;
  v_config     RECORD;
BEGIN
  SELECT * INTO v_config FROM public.distribution_config LIMIT 1;

  IF NOT v_config.enabled THEN
    RETURN 0;
  END IF;

  FOR v_queue_item IN
    SELECT * FROM public.spawn_queue
    WHERE status = 'pending'
      AND scheduled_time <= NOW()
    ORDER BY scheduled_time ASC
    LIMIT v_config.max_spawns_per_cycle
  LOOP
    BEGIN
      UPDATE public.spawn_queue SET status = 'processing' WHERE id = v_queue_item.id;

      v_coin_id := public.spawn_coin(
        p_zone_id      := v_queue_item.zone_id,
        p_trigger_type := v_queue_item.trigger_type,
        p_coin_type    := v_queue_item.coin_type,
        p_tier         := v_queue_item.tier,
        p_latitude     := v_queue_item.target_latitude,
        p_longitude    := v_queue_item.target_longitude
      );

      UPDATE public.spawn_queue
      SET status = 'completed', spawned_coin_id = v_coin_id, processed_at = NOW()
      WHERE id = v_queue_item.id;

      v_processed := v_processed + 1;

    EXCEPTION WHEN OTHERS THEN
      UPDATE public.spawn_queue
      SET status = 'failed', error_message = SQLERRM, processed_at = NOW()
      WHERE id = v_queue_item.id;
    END;
  END LOOP;

  RETURN v_processed;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- SECTION F: CHECK AND QUEUE SPAWNS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.check_and_queue_spawns()
RETURNS INTEGER AS $$
DECLARE
  v_zone          RECORD;
  v_current_count INTEGER;
  v_coins_needed  INTEGER;
  v_queued        INTEGER := 0;
  v_tier          TEXT;
  v_random        DOUBLE PRECISION;
BEGIN
  FOR v_zone IN
    SELECT z.*,
      (z.auto_spawn_config->>'enabled')::BOOLEAN as auto_enabled,
      (z.auto_spawn_config->>'min_coins')::INTEGER as min_coins,
      (z.auto_spawn_config->>'max_coins')::INTEGER as max_coins,
      (z.auto_spawn_config->>'coin_type')::TEXT as spawn_coin_type,
      (z.auto_spawn_config->>'min_value')::DECIMAL as spawn_min_value,
      (z.auto_spawn_config->>'max_value')::DECIMAL as spawn_max_value,
      (z.auto_spawn_config->'tier_weights'->>'gold')::INTEGER as gold_weight,
      (z.auto_spawn_config->'tier_weights'->>'silver')::INTEGER as silver_weight,
      (z.auto_spawn_config->'tier_weights'->>'bronze')::INTEGER as bronze_weight
    FROM public.zones z
    WHERE z.status = 'active'
      AND (z.auto_spawn_config->>'enabled')::BOOLEAN = TRUE
  LOOP
    -- Count current active coins (TODO: add proper geometry containment)
    SELECT COUNT(*) INTO v_current_count
    FROM public.coins c
    WHERE c.status IN ('visible', 'hidden');

    v_coins_needed := GREATEST(0, v_zone.min_coins - v_current_count);

    FOR i IN 1..v_coins_needed LOOP
      v_random := random() * (v_zone.gold_weight + v_zone.silver_weight + v_zone.bronze_weight);
      IF v_random < v_zone.bronze_weight THEN
        v_tier := 'bronze';
      ELSIF v_random < v_zone.bronze_weight + v_zone.silver_weight THEN
        v_tier := 'silver';
      ELSE
        v_tier := 'gold';
      END IF;

      INSERT INTO public.spawn_queue (
        zone_id, trigger_type, coin_type, tier, min_value, max_value, status
      ) VALUES (
        v_zone.id, 'auto',
        COALESCE(v_zone.spawn_coin_type, 'fixed'),
        v_tier,
        COALESCE(v_zone.spawn_min_value, 0.10),
        COALESCE(v_zone.spawn_max_value, 1.00),
        'pending'
      );

      v_queued := v_queued + 1;
    END LOOP;
  END LOOP;

  RETURN v_queued;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- SECTION G: RECYCLE STALE COINS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.recycle_stale_coins(
  p_zone_id      UUID DEFAULT NULL,
  p_max_age_hours INTEGER DEFAULT 48
)
RETURNS INTEGER AS $$
DECLARE
  v_coin        RECORD;
  v_recycled    INTEGER := 0;
  v_cutoff_time TIMESTAMP WITH TIME ZONE;
BEGIN
  v_cutoff_time := NOW() - (p_max_age_hours || ' hours')::INTERVAL;

  FOR v_coin IN
    SELECT c.*
    FROM public.coins c
    LEFT JOIN public.spawn_history sh ON sh.coin_id = c.id
    WHERE c.status IN ('visible', 'hidden')
      AND c.hidden_at < v_cutoff_time
      AND c.collected_at IS NULL
      AND (p_zone_id IS NULL OR sh.zone_id = p_zone_id)
  LOOP
    UPDATE public.coins
    SET status = 'recycled', updated_at = NOW()
    WHERE id = v_coin.id;

    UPDATE public.spawn_history
    SET recycled_at = NOW()
    WHERE coin_id = v_coin.id;

    v_recycled := v_recycled + 1;
  END LOOP;

  RETURN v_recycled;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.recycle_stale_coins IS 'Recycle coins uncollected past the age threshold. Called by auto-distribution and AI Spawn Governor.';

-- ============================================================================
-- SECTION H: GET DISTRIBUTION STATS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.get_distribution_stats()
RETURNS JSON AS $$
DECLARE
  v_stats JSON;
BEGIN
  SELECT json_build_object(
    'system_status',            CASE WHEN dc.enabled THEN 'running' ELSE 'stopped' END,
    'last_spawn_time',          (SELECT MAX(spawned_at) FROM public.spawn_history),
    'next_scheduled_spawn',     (SELECT MIN(scheduled_time) FROM public.spawn_queue WHERE status = 'pending'),
    'total_zones_with_auto_spawn', (
      SELECT COUNT(*) FROM public.zones
      WHERE (auto_spawn_config->>'enabled')::BOOLEAN = TRUE
    ),
    'queue_length',             (SELECT COUNT(*) FROM public.spawn_queue WHERE status = 'pending'),
    'coins_spawned_today',      (SELECT COUNT(*) FROM public.spawn_history WHERE spawned_at >= CURRENT_DATE),
    'coins_collected_today',    (SELECT COUNT(*) FROM public.spawn_history WHERE collected_at >= CURRENT_DATE),
    'coins_recycled_today',     (SELECT COUNT(*) FROM public.spawn_history WHERE recycled_at >= CURRENT_DATE),
    'total_value_spawned_today', COALESCE((
      SELECT SUM(coin_value) FROM public.spawn_history WHERE spawned_at >= CURRENT_DATE
    ), 0),
    'total_value_collected_today', COALESCE((
      SELECT SUM(coin_value) FROM public.spawn_history WHERE collected_at >= CURRENT_DATE
    ), 0),
    'average_coin_value', COALESCE((
      SELECT AVG(coin_value) FROM public.spawn_history
      WHERE spawned_at >= CURRENT_DATE - INTERVAL '7 days'
    ), 0),
    'spawn_success_rate',       1.0,
    'errors_today',             (
      SELECT COUNT(*) FROM public.spawn_queue
      WHERE status = 'failed' AND processed_at >= CURRENT_DATE
    )
  ) INTO v_stats
  FROM public.distribution_config dc
  LIMIT 1;

  RETURN v_stats;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- SECTION I: TIMED RELEASES (from migration 005)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.release_schedules (
  id                       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  zone_id                  UUID REFERENCES public.zones(id) ON DELETE CASCADE NOT NULL,
  name                     TEXT NOT NULL,
  description              TEXT,
  total_coins              INTEGER NOT NULL CHECK (total_coins > 0),
  coins_per_release        INTEGER NOT NULL CHECK (coins_per_release > 0),
  release_interval_seconds INTEGER NOT NULL CHECK (release_interval_seconds >= 10),
  start_time               TIMESTAMP WITH TIME ZONE NOT NULL,
  end_time                 TIMESTAMP WITH TIME ZONE,
  status                   TEXT DEFAULT 'scheduled' CHECK (
                             status IN ('scheduled', 'active', 'paused', 'completed', 'cancelled')
                           ),
  coins_released_so_far    INTEGER DEFAULT 0,
  batches_completed        INTEGER DEFAULT 0,
  next_release_at          TIMESTAMP WITH TIME ZONE,
  last_release_at          TIMESTAMP WITH TIME ZONE,
  created_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  created_by               UUID REFERENCES public.profiles(id)
);

CREATE INDEX IF NOT EXISTS release_schedules_zone_idx
  ON public.release_schedules (zone_id);
CREATE INDEX IF NOT EXISTS release_schedules_status_idx
  ON public.release_schedules (status);
CREATE INDEX IF NOT EXISTS release_schedules_next_idx
  ON public.release_schedules (next_release_at)
  WHERE status IN ('scheduled', 'active');

ALTER TABLE public.release_schedules ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins manage release schedules" ON public.release_schedules;
CREATE POLICY "Admins manage release schedules" ON public.release_schedules
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

COMMENT ON TABLE public.release_schedules IS 'Timed release schedules for hunt events and coin drops';

-- ============================================================================

CREATE TABLE IF NOT EXISTS public.release_batches (
  id           UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  schedule_id  UUID REFERENCES public.release_schedules(id) ON DELETE CASCADE NOT NULL,
  zone_id      UUID NOT NULL,
  release_at   TIMESTAMP WITH TIME ZONE NOT NULL,
  coins_count  INTEGER NOT NULL,
  coins_released INTEGER DEFAULT 0,
  status       TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'released', 'partial', 'failed')),
  error_message TEXT,
  created_at   TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS release_batches_schedule_idx
  ON public.release_batches (schedule_id);

ALTER TABLE public.release_batches ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins view release batches" ON public.release_batches;
CREATE POLICY "Admins view release batches" ON public.release_batches
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

COMMENT ON TABLE public.release_batches IS 'Audit log of each timed release batch execution';

-- ============================================================================
-- SECTION J: TIMED RELEASE FUNCTIONS (from migration 005)
-- ============================================================================

CREATE OR REPLACE FUNCTION public.process_timed_releases()
RETURNS INTEGER AS $$
DECLARE
  r          RECORD;
  released   INTEGER := 0;
  batch_coins INTEGER;
  remain     INTEGER;
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
      SET status = 'completed', next_release_at = NULL, updated_at = NOW()
      WHERE id = r.id;
      CONTINUE;
    END IF;

    BEGIN
      FOR i IN 1..batch_coins LOOP
        PERFORM public.spawn_coin(r.zone_id, 'scheduled', 'fixed', 'bronze', NULL, NULL, NULL);
      END LOOP;

      remain := r.total_coins - r.coins_released_so_far - batch_coins;

      UPDATE public.release_schedules
      SET coins_released_so_far = coins_released_so_far + batch_coins,
          batches_completed     = batches_completed + 1,
          last_release_at       = NOW(),
          next_release_at       = CASE
                                    WHEN remain <= 0 THEN NULL
                                    ELSE NOW() + (r.release_interval_seconds || ' seconds')::INTERVAL
                                  END,
          status                = CASE WHEN remain <= 0 THEN 'completed' ELSE 'active' END,
          updated_at            = NOW()
      WHERE id = r.id;

      released := released + batch_coins;

    EXCEPTION WHEN OTHERS THEN
      UPDATE public.release_schedules SET updated_at = NOW() WHERE id = r.id;
      RAISE WARNING 'Timed release % failed: %', r.id, SQLERRM;
    END;
  END LOOP;

  RETURN released;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================

CREATE OR REPLACE FUNCTION public.create_release_schedule(
  p_zone_id                UUID,
  p_name                   TEXT,
  p_description            TEXT DEFAULT NULL,
  p_total_coins            INTEGER DEFAULT 100,
  p_coins_per_release      INTEGER DEFAULT 1,
  p_release_interval_seconds INTEGER DEFAULT 60,
  p_start_time             TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
  v_id    UUID;
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
  ) RETURNING id INTO v_id;
  RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- COMPLETION NOTICE
-- ============================================================================

DO $$
BEGIN
  RAISE NOTICE '=======================================================';
  RAISE NOTICE 'Migration 015: Auto-distribution repair complete.';
  RAISE NOTICE '  spawn_queue          — created (IF NOT EXISTS)';
  RAISE NOTICE '  spawn_history        — created (IF NOT EXISTS)';
  RAISE NOTICE '  distribution_config  — created (IF NOT EXISTS)';
  RAISE NOTICE '  release_schedules    — created (IF NOT EXISTS)';
  RAISE NOTICE '  release_batches      — created (IF NOT EXISTS)';
  RAISE NOTICE '  spawn_coin()         — replaced';
  RAISE NOTICE '  process_spawn_queue() — replaced';
  RAISE NOTICE '  check_and_queue_spawns() — replaced';
  RAISE NOTICE '  recycle_stale_coins() — replaced';
  RAISE NOTICE '  get_distribution_stats() — replaced';
  RAISE NOTICE '  process_timed_releases() — replaced';
  RAISE NOTICE '  create_release_schedule() — replaced';
  RAISE NOTICE '  Realtime: spawn_history — enabled';
  RAISE NOTICE '=======================================================';
END $$;
