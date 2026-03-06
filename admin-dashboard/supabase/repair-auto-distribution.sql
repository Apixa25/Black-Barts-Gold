-- ============================================================================
-- BLACK BART'S GOLD — Auto-Distribution Repair Script
-- ============================================================================
-- WHAT THIS FIXES:
-- Migrations 004 and 005 used uuid_generate_v4() which requires the uuid-ossp
-- extension. That extension is not installed in this project. Both migrations
-- were recorded as "applied" but their SQL failed, so these tables are missing:
--
--   spawn_queue          (queues coins to be spawned)
--   spawn_history        (logs every spawned coin)
--   distribution_config  (kill switch + config for auto-distribution)
--   release_schedules    (timed hunt event schedules)
--   release_batches      (audit log of each timed release)
--
-- And these functions are missing:
--   spawn_coin()
--   process_spawn_queue()
--   check_and_queue_spawns()
--   recycle_stale_coins()
--   get_distribution_stats()
--   process_timed_releases()
--   create_release_schedule()
--
-- HOW TO USE:
-- 1. Open your Supabase project dashboard
-- 2. Go to: Database → SQL Editor
-- 3. Paste this entire script
-- 4. Click "Run"
-- 5. Confirm all NOTICE messages show success (no ERROR lines)
--
-- This script is SAFE TO RUN MORE THAN ONCE:
--   CREATE TABLE IF NOT EXISTS — skips if already exists
--   CREATE OR REPLACE FUNCTION — always updates
--   DROP POLICY IF EXISTS — safe before each policy
-- ============================================================================

-- ============================================================================
-- PART 0: ZONES TABLE (prerequisite — spawn_queue and spawn_history reference it)
-- ============================================================================
-- The zones table was never captured in a migration file. It must be created
-- here first before any table that has a REFERENCES public.zones(id) FK.

CREATE TABLE IF NOT EXISTS public.zones (
  id                       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  name                     TEXT NOT NULL,
  description              TEXT,

  zone_type                TEXT NOT NULL DEFAULT 'grid'
                             CHECK (zone_type IN ('player', 'sponsor', 'hunt', 'grid')),
  status                   TEXT NOT NULL DEFAULT 'active'
                             CHECK (status IN ('active', 'inactive', 'scheduled', 'completed', 'archived')),

  -- Spatial boundary — stored as JSONB: { type: 'circle'|'polygon', center: {lat,lng}, radius_meters, polygon: [{lat,lng}] }
  geometry                 JSONB NOT NULL DEFAULT '{"type":"circle","center":{"latitude":0,"longitude":0},"radius_meters":500}'::JSONB,

  -- Ownership
  owner_id                 UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  sponsor_id               UUID,   -- FK to sponsors added conditionally below

  -- Auto-spawn configuration (JSONB — see ZoneAutoSpawnConfig in database.ts)
  auto_spawn_config        JSONB,

  -- Timed release configuration (JSONB — see ZoneTimedReleaseConfig in database.ts)
  timed_release_config     JSONB,

  -- Hunt configuration (JSONB — see ZoneHuntConfig in database.ts)
  hunt_config              JSONB,

  -- Scheduling
  start_time               TIMESTAMP WITH TIME ZONE,
  end_time                 TIMESTAMP WITH TIME ZONE,

  -- Running statistics (updated by spawn_coin() and collection triggers)
  coins_placed             INTEGER NOT NULL DEFAULT 0,
  coins_collected          INTEGER NOT NULL DEFAULT 0,
  total_value_distributed  DECIMAL(12, 2) NOT NULL DEFAULT 0,
  active_players           INTEGER NOT NULL DEFAULT 0,

  -- Visual customization for the admin map
  fill_color               TEXT,
  border_color             TEXT,
  opacity                  DECIMAL(3, 2) NOT NULL DEFAULT 0.30,

  -- Arbitrary metadata (AI agent context, custom tags, etc.)
  metadata                 JSONB,

  -- Timestamps
  created_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS zones_status_idx    ON public.zones (status);
CREATE INDEX IF NOT EXISTS zones_type_idx      ON public.zones (zone_type);
CREATE INDEX IF NOT EXISTS zones_owner_idx     ON public.zones (owner_id);
CREATE INDEX IF NOT EXISTS zones_sponsor_idx   ON public.zones (sponsor_id);

ALTER TABLE public.zones ENABLE ROW LEVEL SECURITY;

-- Admins can manage all zones
DROP POLICY IF EXISTS "Admins can manage zones" ON public.zones;
CREATE POLICY "Admins can manage zones" ON public.zones
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin'))
  );

-- Public read for the mobile app (needs zone boundaries)
DROP POLICY IF EXISTS "Public can view active zones" ON public.zones;
CREATE POLICY "Public can view active zones" ON public.zones
  FOR SELECT USING (status = 'active');

-- Add sponsor_id FK only if the sponsors table already exists
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'sponsors'
  ) THEN
    ALTER TABLE public.zones
      ADD CONSTRAINT zones_sponsor_id_fkey
        FOREIGN KEY (sponsor_id) REFERENCES public.sponsors(id) ON DELETE SET NULL;
    RAISE NOTICE 'zones: sponsor_id FK added ✓';
  ELSE
    RAISE NOTICE 'zones: sponsors table not found — sponsor_id FK skipped (add later)';
  END IF;
EXCEPTION WHEN duplicate_object THEN
  RAISE NOTICE 'zones: sponsor_id FK already exists — skipped';
END $$;

-- updated_at trigger
CREATE OR REPLACE FUNCTION public.update_zones_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS zones_updated_at ON public.zones;
CREATE TRIGGER zones_updated_at
  BEFORE UPDATE ON public.zones
  FOR EACH ROW EXECUTE FUNCTION public.update_zones_updated_at();

COMMENT ON TABLE public.zones IS 'Geographic zones for coin distribution, sponsor hunts, and AI territory management';
COMMENT ON COLUMN public.zones.geometry IS 'Zone boundary as JSONB: {type, center:{latitude,lng}, radius_meters} for circles or {type, polygon:[{lat,lng}]} for polygons';
COMMENT ON COLUMN public.zones.auto_spawn_config IS 'Auto-distribution settings: enabled, min_coins, max_coins, tier_weights, etc.';
COMMENT ON COLUMN public.zones.metadata IS 'AI agent context: territory name, hunt pressure history, etc.';

-- Enable Realtime so the admin dashboard map updates live
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime' AND schemaname = 'public' AND tablename = 'zones'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.zones;
    RAISE NOTICE 'zones added to Realtime ✓';
  END IF;
END $$;

-- ============================================================================
-- PART 1: SPAWN QUEUE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_queue (
  id               UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  zone_id          UUID REFERENCES public.zones(id) ON DELETE CASCADE NOT NULL,
  trigger_type     TEXT DEFAULT 'auto' CHECK (trigger_type IN (
                     'auto', 'scheduled', 'manual', 'recycle',
                     'ai_spawn_governor', 'ai_game_master'
                   )),
  scheduled_time   TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  coin_type        TEXT DEFAULT 'fixed' CHECK (coin_type IN ('fixed', 'pool')),
  tier             TEXT DEFAULT 'bronze' CHECK (tier IN ('gold', 'silver', 'bronze')),
  min_value        DECIMAL(10, 2) NOT NULL DEFAULT 0.10,
  max_value        DECIMAL(10, 2) NOT NULL DEFAULT 1.00,
  is_mythical      BOOLEAN DEFAULT FALSE,
  target_latitude  DOUBLE PRECISION,
  target_longitude DOUBLE PRECISION,
  status           TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
  error_message    TEXT,
  spawned_coin_id  UUID REFERENCES public.coins(id),
  created_at       TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  processed_at     TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS spawn_queue_status_idx   ON public.spawn_queue (status, scheduled_time);
CREATE INDEX IF NOT EXISTS spawn_queue_zone_idx     ON public.spawn_queue (zone_id);

ALTER TABLE public.spawn_queue ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can manage spawn queue" ON public.spawn_queue;
CREATE POLICY "Admins can manage spawn queue" ON public.spawn_queue
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'super_admin')
  );

COMMENT ON TABLE public.spawn_queue IS 'Queue of coins waiting to be spawned by the auto-distribution or AI systems';

-- ============================================================================
-- PART 2: SPAWN HISTORY
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_history (
  id                       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  coin_id                  UUID REFERENCES public.coins(id) ON DELETE SET NULL,
  zone_id                  UUID REFERENCES public.zones(id) ON DELETE SET NULL,
  trigger_type             TEXT NOT NULL,
  coin_value               DECIMAL(10, 2) NOT NULL,
  coin_tier                TEXT NOT NULL,
  spawn_latitude           DOUBLE PRECISION NOT NULL,
  spawn_longitude          DOUBLE PRECISION NOT NULL,
  created_by               TEXT NOT NULL DEFAULT 'system'
                             CHECK (created_by IN (
                               'system', 'admin', 'user',
                               'ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer'
                             )),
  spawned_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  collected_at             TIMESTAMP WITH TIME ZONE,
  collected_by_user_id     UUID REFERENCES public.profiles(id),
  recycled_at              TIMESTAMP WITH TIME ZONE,
  time_to_collection_hours DECIMAL(10, 2)
);

CREATE INDEX IF NOT EXISTS spawn_history_zone_idx  ON public.spawn_history (zone_id, spawned_at DESC);
CREATE INDEX IF NOT EXISTS spawn_history_date_idx  ON public.spawn_history (spawned_at DESC);

ALTER TABLE public.spawn_history ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can view spawn history" ON public.spawn_history;
CREATE POLICY "Admins can view spawn history" ON public.spawn_history
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin'))
  );

COMMENT ON TABLE public.spawn_history IS 'History of all spawned coins — source for analytics, economy health, and the AI audit trail';

-- Enable Realtime so AI agents react to coin spawns
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime' AND schemaname = 'public' AND tablename = 'spawn_history'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.spawn_history;
    RAISE NOTICE 'spawn_history added to Realtime ✓';
  ELSE
    RAISE NOTICE 'spawn_history already in Realtime — skipped';
  END IF;
END $$;

-- ============================================================================
-- PART 3: DISTRIBUTION CONFIG (the kill switch lives here)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.distribution_config (
  id                                UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  enabled                           BOOLEAN DEFAULT TRUE,
  check_interval_seconds            INTEGER DEFAULT 60,
  max_spawns_per_cycle              INTEGER DEFAULT 10,
  default_min_coins                 INTEGER DEFAULT 3,
  default_max_coins                 INTEGER DEFAULT 20,
  default_min_value                 DECIMAL(10, 2) DEFAULT 0.10,
  default_max_value                 DECIMAL(10, 2) DEFAULT 5.00,
  default_tier_gold_weight          INTEGER DEFAULT 10,
  default_tier_silver_weight        INTEGER DEFAULT 30,
  default_tier_bronze_weight        INTEGER DEFAULT 60,
  value_strategy                    TEXT DEFAULT 'tiered',
  mythical_spawn_chance             DECIMAL(5, 4) DEFAULT 0.001,
  recycle_enabled                   BOOLEAN DEFAULT TRUE,
  recycle_after_hours               INTEGER DEFAULT 48,
  recycle_to_new_location           BOOLEAN DEFAULT TRUE,
  max_spawns_per_hour               INTEGER DEFAULT 100,
  cooldown_after_collection_seconds INTEGER DEFAULT 300,
  updated_at                        TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_by                        UUID REFERENCES public.profiles(id)
);

-- Seed the default config row (fixed UUID so it's always findable)
INSERT INTO public.distribution_config (id)
VALUES ('00000000-0000-0000-0000-000000000001')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE public.distribution_config ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can manage distribution config" ON public.distribution_config;
CREATE POLICY "Admins can manage distribution config" ON public.distribution_config
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'super_admin')
  );

COMMENT ON TABLE public.distribution_config IS 'Global config for auto-distribution. enabled=FALSE is the master kill switch for all AI spawning.';

-- ============================================================================
-- PART 4: SPAWN COIN FUNCTION
-- ============================================================================

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
  SELECT * INTO v_zone FROM public.zones WHERE id = p_zone_id;
  IF v_zone IS NULL THEN
    RAISE EXCEPTION 'Zone not found: %', p_zone_id;
  END IF;

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
      v_lat := (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION;
      v_lng := (v_zone.geometry->'center'->>'longitude')::DOUBLE PRECISION;
    END IF;
  ELSE
    v_lat := p_latitude;
    v_lng := p_longitude;
  END IF;

  INSERT INTO public.coins (
    coin_type, value, tier, latitude, longitude,
    status, hidden_at, is_mythical, multi_find, finds_remaining
  ) VALUES (
    p_coin_type, v_value, p_tier, v_lat, v_lng,
    'visible', NOW(), FALSE, FALSE, 1
  ) RETURNING id INTO v_coin_id;

  INSERT INTO public.spawn_history (
    coin_id, zone_id, trigger_type,
    coin_value, coin_tier, spawn_latitude, spawn_longitude,
    created_by
  ) VALUES (
    v_coin_id, p_zone_id, p_trigger_type,
    v_value, p_tier, v_lat, v_lng,
    p_trigger_type
  );

  UPDATE public.zones
  SET coins_placed            = coins_placed + 1,
      total_value_distributed = total_value_distributed + v_value,
      updated_at              = NOW()
  WHERE id = p_zone_id;

  RETURN v_coin_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.spawn_coin IS 'Spawn a single coin in a zone. Called by auto-distribution, timed releases, admin, and AI agents.';

-- ============================================================================
-- PART 5: PROCESS SPAWN QUEUE FUNCTION
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
  IF NOT v_config.enabled THEN RETURN 0; END IF;

  FOR v_queue_item IN
    SELECT * FROM public.spawn_queue
    WHERE status = 'pending' AND scheduled_time <= NOW()
    ORDER BY scheduled_time ASC
    LIMIT v_config.max_spawns_per_cycle
  LOOP
    BEGIN
      UPDATE public.spawn_queue SET status = 'processing' WHERE id = v_queue_item.id;
      v_coin_id := public.spawn_coin(
        v_queue_item.zone_id, v_queue_item.trigger_type, v_queue_item.coin_type,
        v_queue_item.tier, NULL, v_queue_item.target_latitude, v_queue_item.target_longitude
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
-- PART 6: CHECK AND QUEUE SPAWNS FUNCTION
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
      (z.auto_spawn_config->>'min_coins')::INTEGER as min_coins,
      (z.auto_spawn_config->>'coin_type')::TEXT as spawn_coin_type,
      (z.auto_spawn_config->>'min_value')::DECIMAL as spawn_min_value,
      (z.auto_spawn_config->>'max_value')::DECIMAL as spawn_max_value,
      (z.auto_spawn_config->'tier_weights'->>'gold')::INTEGER as gold_weight,
      (z.auto_spawn_config->'tier_weights'->>'silver')::INTEGER as silver_weight,
      (z.auto_spawn_config->'tier_weights'->>'bronze')::INTEGER as bronze_weight
    FROM public.zones z
    WHERE z.status = 'active' AND (z.auto_spawn_config->>'enabled')::BOOLEAN = TRUE
  LOOP
    SELECT COUNT(*) INTO v_current_count
    FROM public.coins WHERE status IN ('visible', 'hidden');
    v_coins_needed := GREATEST(0, v_zone.min_coins - v_current_count);
    FOR i IN 1..v_coins_needed LOOP
      v_random := random() * (v_zone.gold_weight + v_zone.silver_weight + v_zone.bronze_weight);
      IF    v_random < v_zone.bronze_weight                              THEN v_tier := 'bronze';
      ELSIF v_random < v_zone.bronze_weight + v_zone.silver_weight       THEN v_tier := 'silver';
      ELSE                                                                     v_tier := 'gold';
      END IF;
      INSERT INTO public.spawn_queue (zone_id, trigger_type, coin_type, tier, min_value, max_value, status)
      VALUES (v_zone.id, 'auto', COALESCE(v_zone.spawn_coin_type,'fixed'), v_tier,
              COALESCE(v_zone.spawn_min_value,0.10), COALESCE(v_zone.spawn_max_value,1.00), 'pending');
      v_queued := v_queued + 1;
    END LOOP;
  END LOOP;
  RETURN v_queued;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- PART 7: RECYCLE STALE COINS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.recycle_stale_coins(
  p_zone_id       UUID DEFAULT NULL,
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
    SELECT c.* FROM public.coins c
    LEFT JOIN public.spawn_history sh ON sh.coin_id = c.id
    WHERE c.status IN ('visible', 'hidden')
      AND c.hidden_at < v_cutoff_time
      AND c.collected_at IS NULL
      AND (p_zone_id IS NULL OR sh.zone_id = p_zone_id)
  LOOP
    UPDATE public.coins SET status = 'recycled', updated_at = NOW() WHERE id = v_coin.id;
    UPDATE public.spawn_history SET recycled_at = NOW() WHERE coin_id = v_coin.id;
    v_recycled := v_recycled + 1;
  END LOOP;
  RETURN v_recycled;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.recycle_stale_coins IS 'Recycle uncollected coins past the age threshold. Called by auto-distribution and the AI Spawn Governor.';

-- ============================================================================
-- PART 8: GET DISTRIBUTION STATS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.get_distribution_stats()
RETURNS JSON AS $$
DECLARE v_stats JSON;
BEGIN
  SELECT json_build_object(
    'system_status',               CASE WHEN dc.enabled THEN 'running' ELSE 'stopped' END,
    'last_spawn_time',             (SELECT MAX(spawned_at) FROM public.spawn_history),
    'next_scheduled_spawn',        (SELECT MIN(scheduled_time) FROM public.spawn_queue WHERE status='pending'),
    'queue_length',                (SELECT COUNT(*) FROM public.spawn_queue WHERE status='pending'),
    'coins_spawned_today',         (SELECT COUNT(*) FROM public.spawn_history WHERE spawned_at  >= CURRENT_DATE),
    'coins_collected_today',       (SELECT COUNT(*) FROM public.spawn_history WHERE collected_at >= CURRENT_DATE),
    'coins_recycled_today',        (SELECT COUNT(*) FROM public.spawn_history WHERE recycled_at  >= CURRENT_DATE),
    'total_value_spawned_today',   COALESCE((SELECT SUM(coin_value) FROM public.spawn_history WHERE spawned_at  >= CURRENT_DATE),0),
    'total_value_collected_today', COALESCE((SELECT SUM(coin_value) FROM public.spawn_history WHERE collected_at >= CURRENT_DATE),0),
    'average_coin_value',          COALESCE((SELECT AVG(coin_value) FROM public.spawn_history WHERE spawned_at >= CURRENT_DATE - INTERVAL '7 days'),0),
    'errors_today',                (SELECT COUNT(*) FROM public.spawn_queue WHERE status='failed' AND processed_at >= CURRENT_DATE)
  ) INTO v_stats FROM public.distribution_config dc LIMIT 1;
  RETURN v_stats;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- PART 9: TIMED RELEASES (from migration 005)
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
                             status IN ('scheduled','active','paused','completed','cancelled')),
  coins_released_so_far    INTEGER DEFAULT 0,
  batches_completed        INTEGER DEFAULT 0,
  next_release_at          TIMESTAMP WITH TIME ZONE,
  last_release_at          TIMESTAMP WITH TIME ZONE,
  created_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at               TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  created_by               UUID REFERENCES public.profiles(id)
);

CREATE INDEX IF NOT EXISTS release_schedules_zone_idx   ON public.release_schedules (zone_id);
CREATE INDEX IF NOT EXISTS release_schedules_status_idx ON public.release_schedules (status);
CREATE INDEX IF NOT EXISTS release_schedules_next_idx   ON public.release_schedules (next_release_at)
  WHERE status IN ('scheduled','active');

ALTER TABLE public.release_schedules ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins manage release schedules" ON public.release_schedules;
CREATE POLICY "Admins manage release schedules" ON public.release_schedules
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role IN ('super_admin','sponsor_admin'))
  );

CREATE TABLE IF NOT EXISTS public.release_batches (
  id             UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  schedule_id    UUID REFERENCES public.release_schedules(id) ON DELETE CASCADE NOT NULL,
  zone_id        UUID NOT NULL,
  release_at     TIMESTAMP WITH TIME ZONE NOT NULL,
  coins_count    INTEGER NOT NULL,
  coins_released INTEGER DEFAULT 0,
  status         TEXT DEFAULT 'pending' CHECK (status IN ('pending','released','partial','failed')),
  error_message  TEXT,
  created_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS release_batches_schedule_idx ON public.release_batches (schedule_id);

ALTER TABLE public.release_batches ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins view release batches" ON public.release_batches;
CREATE POLICY "Admins view release batches" ON public.release_batches
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role IN ('super_admin','sponsor_admin'))
  );

CREATE OR REPLACE FUNCTION public.process_timed_releases()
RETURNS INTEGER AS $$
DECLARE
  r           RECORD;
  released    INTEGER := 0;
  batch_coins INTEGER;
  remain      INTEGER;
BEGIN
  FOR r IN
    SELECT * FROM public.release_schedules
    WHERE status IN ('scheduled','active') AND next_release_at IS NOT NULL AND next_release_at <= NOW()
    ORDER BY next_release_at LIMIT 20
  LOOP
    batch_coins := LEAST(r.coins_per_release, r.total_coins - r.coins_released_so_far);
    IF batch_coins <= 0 THEN
      UPDATE public.release_schedules SET status='completed', next_release_at=NULL, updated_at=NOW() WHERE id=r.id;
      CONTINUE;
    END IF;
    BEGIN
      FOR i IN 1..batch_coins LOOP
        PERFORM public.spawn_coin(r.zone_id,'scheduled','fixed','bronze',NULL,NULL,NULL);
      END LOOP;
      remain := r.total_coins - r.coins_released_so_far - batch_coins;
      UPDATE public.release_schedules
      SET coins_released_so_far = coins_released_so_far + batch_coins,
          batches_completed     = batches_completed + 1,
          last_release_at       = NOW(),
          next_release_at       = CASE WHEN remain<=0 THEN NULL ELSE NOW()+(r.release_interval_seconds||' seconds')::INTERVAL END,
          status                = CASE WHEN remain<=0 THEN 'completed' ELSE 'active' END,
          updated_at            = NOW()
      WHERE id = r.id;
      released := released + batch_coins;
    EXCEPTION WHEN OTHERS THEN
      UPDATE public.release_schedules SET updated_at=NOW() WHERE id=r.id;
      RAISE WARNING 'Timed release % failed: %', r.id, SQLERRM;
    END;
  END LOOP;
  RETURN released;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE OR REPLACE FUNCTION public.create_release_schedule(
  p_zone_id UUID, p_name TEXT, p_description TEXT DEFAULT NULL,
  p_total_coins INTEGER DEFAULT 100, p_coins_per_release INTEGER DEFAULT 1,
  p_release_interval_seconds INTEGER DEFAULT 60,
  p_start_time TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE v_id UUID; v_start TIMESTAMP WITH TIME ZONE;
BEGIN
  v_start := COALESCE(p_start_time, NOW());
  INSERT INTO public.release_schedules (
    zone_id, name, description, total_coins, coins_per_release,
    release_interval_seconds, start_time, next_release_at, status
  ) VALUES (
    p_zone_id, p_name, p_description, p_total_coins, p_coins_per_release,
    p_release_interval_seconds, v_start, v_start, 'scheduled'
  ) RETURNING id INTO v_id;
  RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- DONE — verify the output below has no ERROR lines
-- ============================================================================

DO $$
BEGIN
  RAISE NOTICE '========================================================';
  RAISE NOTICE 'BBG Auto-Distribution Repair: COMPLETE ✓';
  RAISE NOTICE '  spawn_queue          — ready';
  RAISE NOTICE '  spawn_history        — ready';
  RAISE NOTICE '  distribution_config  — ready';
  RAISE NOTICE '  release_schedules    — ready';
  RAISE NOTICE '  release_batches      — ready';
  RAISE NOTICE '  spawn_coin()         — ready';
  RAISE NOTICE '  recycle_stale_coins() — ready';
  RAISE NOTICE '  All other functions  — ready';
  RAISE NOTICE '========================================================';
  RAISE NOTICE 'Next step: Run supabase db push to apply migration 015';
  RAISE NOTICE 'and confirm migration list shows 015 on both local+remote.';
END $$;
