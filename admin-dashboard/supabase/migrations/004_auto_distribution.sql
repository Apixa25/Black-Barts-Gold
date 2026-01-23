-- ============================================================================
-- Auto-Distribution System - M5: Auto-Distribution
-- ============================================================================
-- This migration adds functions and tables for automatic coin distribution.
-- 
-- Run this in Supabase SQL Editor to enable auto-distribution.
-- ============================================================================

-- ============================================================================
-- SPAWN QUEUE TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_queue (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  zone_id UUID REFERENCES public.zones(id) ON DELETE CASCADE NOT NULL,
  trigger_type TEXT DEFAULT 'auto' CHECK (trigger_type IN ('auto', 'scheduled', 'manual', 'recycle')),
  scheduled_time TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  
  -- Coin configuration
  coin_type TEXT DEFAULT 'fixed' CHECK (coin_type IN ('fixed', 'pool')),
  tier TEXT DEFAULT 'bronze' CHECK (tier IN ('gold', 'silver', 'bronze')),
  min_value DECIMAL(10, 2) NOT NULL DEFAULT 0.10,
  max_value DECIMAL(10, 2) NOT NULL DEFAULT 1.00,
  is_mythical BOOLEAN DEFAULT FALSE,
  
  -- Target location (if specified)
  target_latitude DOUBLE PRECISION,
  target_longitude DOUBLE PRECISION,
  
  -- Status
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
  error_message TEXT,
  
  -- Result
  spawned_coin_id UUID REFERENCES public.coins(id),
  
  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  processed_at TIMESTAMP WITH TIME ZONE
);

-- Indexes
CREATE INDEX IF NOT EXISTS spawn_queue_status_idx ON public.spawn_queue (status, scheduled_time);
CREATE INDEX IF NOT EXISTS spawn_queue_zone_idx ON public.spawn_queue (zone_id);

-- Enable RLS
ALTER TABLE public.spawn_queue ENABLE ROW LEVEL SECURITY;

-- Policy: Admins can manage spawn queue
CREATE POLICY "Admins can manage spawn queue" ON public.spawn_queue
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin')
    )
  );

-- ============================================================================
-- SPAWN HISTORY TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.spawn_history (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  coin_id UUID REFERENCES public.coins(id) ON DELETE SET NULL,
  zone_id UUID REFERENCES public.zones(id) ON DELETE SET NULL,
  trigger_type TEXT NOT NULL,
  
  -- Coin details at spawn time
  coin_value DECIMAL(10, 2) NOT NULL,
  coin_tier TEXT NOT NULL,
  spawn_latitude DOUBLE PRECISION NOT NULL,
  spawn_longitude DOUBLE PRECISION NOT NULL,
  
  -- Collection tracking
  spawned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  collected_at TIMESTAMP WITH TIME ZONE,
  collected_by_user_id UUID REFERENCES public.profiles(id),
  recycled_at TIMESTAMP WITH TIME ZONE,
  
  -- Computed
  time_to_collection_hours DECIMAL(10, 2)
);

-- Indexes
CREATE INDEX IF NOT EXISTS spawn_history_zone_idx ON public.spawn_history (zone_id, spawned_at DESC);
CREATE INDEX IF NOT EXISTS spawn_history_date_idx ON public.spawn_history (spawned_at DESC);

-- Enable RLS
ALTER TABLE public.spawn_history ENABLE ROW LEVEL SECURITY;

-- Policy: Admins can view spawn history
CREATE POLICY "Admins can view spawn history" ON public.spawn_history
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

-- ============================================================================
-- DISTRIBUTION CONFIG TABLE
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.distribution_config (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- Global settings
  enabled BOOLEAN DEFAULT TRUE,
  check_interval_seconds INTEGER DEFAULT 60,
  max_spawns_per_cycle INTEGER DEFAULT 10,
  
  -- Default zone settings
  default_min_coins INTEGER DEFAULT 3,
  default_max_coins INTEGER DEFAULT 20,
  default_min_value DECIMAL(10, 2) DEFAULT 0.10,
  default_max_value DECIMAL(10, 2) DEFAULT 5.00,
  default_tier_gold_weight INTEGER DEFAULT 10,
  default_tier_silver_weight INTEGER DEFAULT 30,
  default_tier_bronze_weight INTEGER DEFAULT 60,
  
  -- Value distribution
  value_strategy TEXT DEFAULT 'tiered',
  mythical_spawn_chance DECIMAL(5, 4) DEFAULT 0.001,
  
  -- Recycling
  recycle_enabled BOOLEAN DEFAULT TRUE,
  recycle_after_hours INTEGER DEFAULT 48,
  recycle_to_new_location BOOLEAN DEFAULT TRUE,
  
  -- Rate limiting
  max_spawns_per_hour INTEGER DEFAULT 100,
  cooldown_after_collection_seconds INTEGER DEFAULT 300,
  
  -- Metadata
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_by UUID REFERENCES public.profiles(id)
);

-- Insert default config if not exists
INSERT INTO public.distribution_config (id)
VALUES ('00000000-0000-0000-0000-000000000001')
ON CONFLICT (id) DO NOTHING;

-- Enable RLS
ALTER TABLE public.distribution_config ENABLE ROW LEVEL SECURITY;

-- Policy: Admins can manage config
CREATE POLICY "Admins can manage distribution config" ON public.distribution_config
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin')
    )
  );

-- ============================================================================
-- SPAWN COIN FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.spawn_coin(
  p_zone_id UUID,
  p_trigger_type TEXT DEFAULT 'auto',
  p_coin_type TEXT DEFAULT 'fixed',
  p_tier TEXT DEFAULT 'bronze',
  p_value DECIMAL DEFAULT NULL,
  p_latitude DOUBLE PRECISION DEFAULT NULL,
  p_longitude DOUBLE PRECISION DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
  v_zone RECORD;
  v_coin_id UUID;
  v_value DECIMAL;
  v_lat DOUBLE PRECISION;
  v_lng DOUBLE PRECISION;
  v_radius DOUBLE PRECISION;
  v_angle DOUBLE PRECISION;
  v_distance DOUBLE PRECISION;
BEGIN
  -- Get zone details
  SELECT * INTO v_zone FROM public.zones WHERE id = p_zone_id;
  IF v_zone IS NULL THEN
    RAISE EXCEPTION 'Zone not found: %', p_zone_id;
  END IF;
  
  -- Calculate value if not provided
  IF p_value IS NULL THEN
    CASE p_tier
      WHEN 'bronze' THEN v_value := 0.10 + random() * 0.40;
      WHEN 'silver' THEN v_value := 0.50 + random() * 1.50;
      WHEN 'gold' THEN v_value := 2.00 + random() * 8.00;
      ELSE v_value := 0.10;
    END CASE;
  ELSE
    v_value := p_value;
  END IF;
  
  -- Round to 2 decimal places
  v_value := ROUND(v_value, 2);
  
  -- Calculate spawn location if not provided
  IF p_latitude IS NULL OR p_longitude IS NULL THEN
    IF v_zone.geometry->>'type' = 'circle' THEN
      -- Random point in circle
      v_radius := (v_zone.geometry->>'radius_meters')::DOUBLE PRECISION;
      v_angle := random() * 2 * PI();
      v_distance := sqrt(random()) * v_radius;
      
      v_lat := (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION 
        + (v_distance / 111320) * cos(v_angle);
      v_lng := (v_zone.geometry->'center'->>'longitude')::DOUBLE PRECISION 
        + (v_distance / (111320 * cos(radians((v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION)))) * sin(v_angle);
    ELSE
      -- For polygon, use zone center as fallback
      v_lat := (v_zone.geometry->'center'->>'latitude')::DOUBLE PRECISION;
      v_lng := (v_zone.geometry->'center'->>'longitude')::DOUBLE PRECISION;
    END IF;
  ELSE
    v_lat := p_latitude;
    v_lng := p_longitude;
  END IF;
  
  -- Create the coin
  INSERT INTO public.coins (
    coin_type,
    value,
    tier,
    latitude,
    longitude,
    status,
    hidden_at,
    is_mythical,
    multi_find,
    finds_remaining
  ) VALUES (
    p_coin_type,
    v_value,
    p_tier,
    v_lat,
    v_lng,
    'visible',
    NOW(),
    FALSE,
    FALSE,
    1
  ) RETURNING id INTO v_coin_id;
  
  -- Record in spawn history
  INSERT INTO public.spawn_history (
    coin_id,
    zone_id,
    trigger_type,
    coin_value,
    coin_tier,
    spawn_latitude,
    spawn_longitude
  ) VALUES (
    v_coin_id,
    p_zone_id,
    p_trigger_type,
    v_value,
    p_tier,
    v_lat,
    v_lng
  );
  
  -- Update zone statistics
  UPDATE public.zones
  SET 
    coins_placed = coins_placed + 1,
    total_value_distributed = total_value_distributed + v_value,
    updated_at = NOW()
  WHERE id = p_zone_id;
  
  RETURN v_coin_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- PROCESS SPAWN QUEUE FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.process_spawn_queue()
RETURNS INTEGER AS $$
DECLARE
  v_queue_item RECORD;
  v_coin_id UUID;
  v_processed INTEGER := 0;
  v_config RECORD;
BEGIN
  -- Get distribution config
  SELECT * INTO v_config FROM public.distribution_config LIMIT 1;
  
  -- Check if distribution is enabled
  IF NOT v_config.enabled THEN
    RETURN 0;
  END IF;
  
  -- Process pending items up to max_spawns_per_cycle
  FOR v_queue_item IN 
    SELECT * FROM public.spawn_queue 
    WHERE status = 'pending' 
      AND scheduled_time <= NOW()
    ORDER BY scheduled_time ASC
    LIMIT v_config.max_spawns_per_cycle
  LOOP
    BEGIN
      -- Update status to processing
      UPDATE public.spawn_queue SET status = 'processing' WHERE id = v_queue_item.id;
      
      -- Spawn the coin
      v_coin_id := public.spawn_coin(
        p_zone_id := v_queue_item.zone_id,
        p_trigger_type := v_queue_item.trigger_type,
        p_coin_type := v_queue_item.coin_type,
        p_tier := v_queue_item.tier,
        p_latitude := v_queue_item.target_latitude,
        p_longitude := v_queue_item.target_longitude
      );
      
      -- Mark as completed
      UPDATE public.spawn_queue 
      SET 
        status = 'completed',
        spawned_coin_id = v_coin_id,
        processed_at = NOW()
      WHERE id = v_queue_item.id;
      
      v_processed := v_processed + 1;
      
    EXCEPTION WHEN OTHERS THEN
      -- Mark as failed
      UPDATE public.spawn_queue 
      SET 
        status = 'failed',
        error_message = SQLERRM,
        processed_at = NOW()
      WHERE id = v_queue_item.id;
    END;
  END LOOP;
  
  RETURN v_processed;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- CHECK AND QUEUE SPAWNS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.check_and_queue_spawns()
RETURNS INTEGER AS $$
DECLARE
  v_zone RECORD;
  v_current_count INTEGER;
  v_coins_needed INTEGER;
  v_queued INTEGER := 0;
  v_tier TEXT;
  v_random DOUBLE PRECISION;
  v_tier_weights RECORD;
BEGIN
  -- Loop through zones with auto-spawn enabled
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
    -- Count current coins in zone (simplified - doesn't check geometry containment)
    SELECT COUNT(*) INTO v_current_count
    FROM public.coins c
    WHERE c.status IN ('visible', 'hidden')
      -- TODO: Add proper geometry containment check
      ;
    
    -- Calculate how many coins needed
    v_coins_needed := GREATEST(0, v_zone.min_coins - v_current_count);
    
    -- Queue spawns
    FOR i IN 1..v_coins_needed LOOP
      -- Select tier based on weights
      v_random := random() * (v_zone.gold_weight + v_zone.silver_weight + v_zone.bronze_weight);
      IF v_random < v_zone.bronze_weight THEN
        v_tier := 'bronze';
      ELSIF v_random < v_zone.bronze_weight + v_zone.silver_weight THEN
        v_tier := 'silver';
      ELSE
        v_tier := 'gold';
      END IF;
      
      -- Insert into spawn queue
      INSERT INTO public.spawn_queue (
        zone_id,
        trigger_type,
        coin_type,
        tier,
        min_value,
        max_value,
        status
      ) VALUES (
        v_zone.id,
        'auto',
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
-- RECYCLE STALE COINS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.recycle_stale_coins(
  p_zone_id UUID DEFAULT NULL,
  p_max_age_hours INTEGER DEFAULT 48
)
RETURNS INTEGER AS $$
DECLARE
  v_coin RECORD;
  v_recycled INTEGER := 0;
  v_cutoff_time TIMESTAMP WITH TIME ZONE;
BEGIN
  v_cutoff_time := NOW() - (p_max_age_hours || ' hours')::INTERVAL;
  
  -- Find and recycle stale coins
  FOR v_coin IN 
    SELECT c.* 
    FROM public.coins c
    LEFT JOIN public.spawn_history sh ON sh.coin_id = c.id
    WHERE c.status IN ('visible', 'hidden')
      AND c.hidden_at < v_cutoff_time
      AND c.collected_at IS NULL
      AND (p_zone_id IS NULL OR sh.zone_id = p_zone_id)
  LOOP
    -- Mark coin as recycled
    UPDATE public.coins 
    SET status = 'recycled', updated_at = NOW()
    WHERE id = v_coin.id;
    
    -- Update spawn history
    UPDATE public.spawn_history
    SET recycled_at = NOW()
    WHERE coin_id = v_coin.id;
    
    v_recycled := v_recycled + 1;
  END LOOP;
  
  RETURN v_recycled;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- GET DISTRIBUTION STATS FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION public.get_distribution_stats()
RETURNS JSON AS $$
DECLARE
  v_stats JSON;
BEGIN
  SELECT json_build_object(
    'system_status', CASE WHEN dc.enabled THEN 'running' ELSE 'stopped' END,
    'last_spawn_time', (SELECT MAX(spawned_at) FROM public.spawn_history),
    'next_scheduled_spawn', (SELECT MIN(scheduled_time) FROM public.spawn_queue WHERE status = 'pending'),
    'total_zones_with_auto_spawn', (
      SELECT COUNT(*) FROM public.zones 
      WHERE (auto_spawn_config->>'enabled')::BOOLEAN = TRUE
    ),
    'zones_needing_spawn', 0, -- TODO: Calculate properly
    'queue_length', (SELECT COUNT(*) FROM public.spawn_queue WHERE status = 'pending'),
    'coins_spawned_today', (
      SELECT COUNT(*) FROM public.spawn_history 
      WHERE spawned_at >= CURRENT_DATE
    ),
    'coins_collected_today', (
      SELECT COUNT(*) FROM public.spawn_history 
      WHERE collected_at >= CURRENT_DATE
    ),
    'coins_recycled_today', (
      SELECT COUNT(*) FROM public.spawn_history 
      WHERE recycled_at >= CURRENT_DATE
    ),
    'total_value_spawned_today', COALESCE((
      SELECT SUM(coin_value) FROM public.spawn_history 
      WHERE spawned_at >= CURRENT_DATE
    ), 0),
    'total_value_collected_today', COALESCE((
      SELECT SUM(coin_value) FROM public.spawn_history 
      WHERE collected_at >= CURRENT_DATE
    ), 0),
    'average_coin_value', COALESCE((
      SELECT AVG(coin_value) FROM public.spawn_history 
      WHERE spawned_at >= CURRENT_DATE - INTERVAL '7 days'
    ), 0),
    'spawn_success_rate', 1.0,
    'errors_today', (
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
-- COMMENTS
-- ============================================================================

COMMENT ON TABLE public.spawn_queue IS 'Queue of coins waiting to be spawned';
COMMENT ON TABLE public.spawn_history IS 'History of all spawned coins for analytics';
COMMENT ON TABLE public.distribution_config IS 'Global configuration for auto-distribution';
COMMENT ON FUNCTION public.spawn_coin IS 'Spawn a single coin in a zone';
COMMENT ON FUNCTION public.process_spawn_queue IS 'Process pending items in spawn queue';
COMMENT ON FUNCTION public.check_and_queue_spawns IS 'Check zones and queue spawns for those below minimum';
COMMENT ON FUNCTION public.recycle_stale_coins IS 'Recycle coins that have been uncollected too long';
COMMENT ON FUNCTION public.get_distribution_stats IS 'Get statistics about the distribution system';
