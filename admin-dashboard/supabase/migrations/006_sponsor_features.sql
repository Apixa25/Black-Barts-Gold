-- ============================================================================
-- Sponsor Features Migration (M7)
-- ============================================================================
-- Phase M7: Sponsor Features
-- 
-- This migration adds tables and functions for:
-- - Sponsor zone analytics tracking
-- - Bulk coin placement tracking
-- - Sponsor performance metrics
-- ============================================================================

-- ============================================================================
-- SPONSOR ZONE ANALYTICS TABLE
-- ============================================================================
-- Tracks performance metrics for each sponsor zone

CREATE TABLE IF NOT EXISTS public.sponsor_zone_analytics (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- References
  zone_id UUID NOT NULL REFERENCES public.zones(id) ON DELETE CASCADE,
  sponsor_id UUID NOT NULL REFERENCES public.sponsors(id) ON DELETE CASCADE,
  
  -- Coin metrics (snapshot)
  total_coins_placed INTEGER DEFAULT 0,
  coins_collected INTEGER DEFAULT 0,
  coins_active INTEGER DEFAULT 0,
  coins_expired INTEGER DEFAULT 0,
  
  -- Value metrics (snapshot)
  total_value_placed DECIMAL(10, 2) DEFAULT 0.00,
  total_value_collected DECIMAL(10, 2) DEFAULT 0.00,
  average_coin_value DECIMAL(10, 2) DEFAULT 0.00,
  
  -- Engagement metrics
  unique_collectors INTEGER DEFAULT 0,
  total_collections INTEGER DEFAULT 0,
  average_collection_time_minutes INTEGER DEFAULT 0,
  
  -- Time-based metrics
  first_coin_placed_at TIMESTAMP WITH TIME ZONE,
  last_coin_collected_at TIMESTAMP WITH TIME ZONE,
  peak_collection_hour INTEGER, -- 0-23
  
  -- Performance score (0-100)
  performance_score INTEGER DEFAULT 0,
  
  -- Period
  period_start TIMESTAMP WITH TIME ZONE NOT NULL,
  period_end TIMESTAMP WITH TIME ZONE NOT NULL,
  
  -- Metadata
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  
  -- Unique constraint: one analytics record per zone per period
  UNIQUE(zone_id, period_start, period_end)
);

-- Indexes
CREATE INDEX idx_sponsor_zone_analytics_zone ON public.sponsor_zone_analytics(zone_id);
CREATE INDEX idx_sponsor_zone_analytics_sponsor ON public.sponsor_zone_analytics(sponsor_id);
CREATE INDEX idx_sponsor_zone_analytics_period ON public.sponsor_zone_analytics(period_start, period_end);

-- ============================================================================
-- BULK PLACEMENT HISTORY TABLE
-- ============================================================================
-- Tracks bulk coin placement operations for audit and analytics

CREATE TABLE IF NOT EXISTS public.bulk_coin_placements (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- References
  sponsor_id UUID NOT NULL REFERENCES public.sponsors(id) ON DELETE CASCADE,
  zone_id UUID REFERENCES public.zones(id) ON DELETE SET NULL,
  
  -- Placement configuration (stored as JSONB for flexibility)
  config JSONB NOT NULL,
  
  -- Results
  coins_placed INTEGER DEFAULT 0,
  coins_failed INTEGER DEFAULT 0,
  total_cost DECIMAL(10, 2) DEFAULT 0.00,
  
  -- Status
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
  error_message TEXT,
  
  -- Timestamps
  started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  completed_at TIMESTAMP WITH TIME ZONE,
  
  -- Metadata
  created_by UUID REFERENCES public.profiles(id),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_bulk_placements_sponsor ON public.bulk_coin_placements(sponsor_id);
CREATE INDEX idx_bulk_placements_zone ON public.bulk_coin_placements(zone_id);
CREATE INDEX idx_bulk_placements_status ON public.bulk_coin_placements(status);
CREATE INDEX idx_bulk_placements_created ON public.bulk_coin_placements(created_at DESC);

-- ============================================================================
-- FUNCTIONS
-- ============================================================================

-- Function to calculate and update sponsor zone analytics
CREATE OR REPLACE FUNCTION public.calculate_sponsor_zone_analytics(
  p_zone_id UUID,
  p_period_start TIMESTAMP WITH TIME ZONE,
  p_period_end TIMESTAMP WITH TIME ZONE
)
RETURNS public.sponsor_zone_analytics
LANGUAGE plpgsql
AS $$
DECLARE
  v_analytics public.sponsor_zone_analytics;
  v_zone public.zones;
  v_coins_stats RECORD;
  v_value_stats RECORD;
  v_collector_stats RECORD;
  v_performance_score INTEGER;
BEGIN
  -- Get zone info
  SELECT * INTO v_zone FROM public.zones WHERE id = p_zone_id;
  
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Zone not found: %', p_zone_id;
  END IF;
  
  -- Calculate coin metrics
  SELECT 
    COUNT(*) FILTER (WHERE created_at >= p_period_start AND created_at <= p_period_end) as total_placed,
    COUNT(*) FILTER (WHERE status = 'collected' AND collected_at >= p_period_start AND collected_at <= p_period_end) as collected,
    COUNT(*) FILTER (WHERE status = 'visible') as active,
    COUNT(*) FILTER (WHERE status = 'expired') as expired
  INTO v_coins_stats
  FROM public.coins
  WHERE zone_id = p_zone_id;
  
  -- Calculate value metrics
  SELECT 
    COALESCE(SUM(value) FILTER (WHERE created_at >= p_period_start AND created_at <= p_period_end), 0) as total_placed,
    COALESCE(SUM(value) FILTER (WHERE status = 'collected' AND collected_at >= p_period_start AND collected_at <= p_period_end), 0) as total_collected,
    COALESCE(AVG(value) FILTER (WHERE created_at >= p_period_start AND created_at <= p_period_end), 0) as avg_value
  INTO v_value_stats
  FROM public.coins
  WHERE zone_id = p_zone_id;
  
  -- Calculate collector metrics
  SELECT 
    COUNT(DISTINCT collected_by) FILTER (WHERE collected_at >= p_period_start AND collected_at <= p_period_end) as unique_collectors,
    COUNT(*) FILTER (WHERE collected_at >= p_period_start AND collected_at <= p_period_end) as total_collections,
    COALESCE(AVG(EXTRACT(EPOCH FROM (collected_at - created_at)) / 60) FILTER (WHERE collected_at >= p_period_start AND collected_at <= p_period_end), 0)::INTEGER as avg_time_minutes
  INTO v_collector_stats
  FROM public.coins
  WHERE zone_id = p_zone_id;
  
  -- Calculate performance score (simplified version)
  -- Collection rate (0-40), Value efficiency (0-30), Engagement (0-20), Activity (0-10)
  v_performance_score := LEAST(
    (v_coins_stats.collected::DECIMAL / NULLIF(v_coins_stats.total_placed, 0) * 40)::INTEGER +
    (v_value_stats.total_collected::DECIMAL / NULLIF(v_value_stats.total_placed, 1) * 30)::INTEGER +
    (v_collector_stats.unique_collectors::DECIMAL / NULLIF(v_coins_stats.total_placed, 1) * 20)::INTEGER +
    10, -- Activity bonus (simplified)
    100
  );
  
  -- Upsert analytics record
  INSERT INTO public.sponsor_zone_analytics (
    zone_id,
    sponsor_id,
    total_coins_placed,
    coins_collected,
    coins_active,
    coins_expired,
    total_value_placed,
    total_value_collected,
    average_coin_value,
    unique_collectors,
    total_collections,
    average_collection_time_minutes,
    first_coin_placed_at,
    last_coin_collected_at,
    performance_score,
    period_start,
    period_end
  )
  VALUES (
    p_zone_id,
    v_zone.sponsor_id,
    COALESCE(v_coins_stats.total_placed, 0),
    COALESCE(v_coins_stats.collected, 0),
    COALESCE(v_coins_stats.active, 0),
    COALESCE(v_coins_stats.expired, 0),
    COALESCE(v_value_stats.total_placed, 0),
    COALESCE(v_value_stats.total_collected, 0),
    COALESCE(v_value_stats.avg_value, 0),
    COALESCE(v_collector_stats.unique_collectors, 0),
    COALESCE(v_collector_stats.total_collections, 0),
    COALESCE(v_collector_stats.avg_time_minutes, 0),
    (SELECT MIN(created_at) FROM public.coins WHERE zone_id = p_zone_id),
    (SELECT MAX(collected_at) FROM public.coins WHERE zone_id = p_zone_id AND collected_at IS NOT NULL),
    v_performance_score,
    p_period_start,
    p_period_end
  )
  ON CONFLICT (zone_id, period_start, period_end)
  DO UPDATE SET
    total_coins_placed = EXCLUDED.total_coins_placed,
    coins_collected = EXCLUDED.coins_collected,
    coins_active = EXCLUDED.coins_active,
    coins_expired = EXCLUDED.coins_expired,
    total_value_placed = EXCLUDED.total_value_placed,
    total_value_collected = EXCLUDED.total_value_collected,
    average_coin_value = EXCLUDED.average_coin_value,
    unique_collectors = EXCLUDED.unique_collectors,
    total_collections = EXCLUDED.total_collections,
    average_collection_time_minutes = EXCLUDED.average_collection_time_minutes,
    first_coin_placed_at = EXCLUDED.first_coin_placed_at,
    last_coin_collected_at = EXCLUDED.last_coin_collected_at,
    performance_score = EXCLUDED.performance_score,
    updated_at = NOW()
  RETURNING * INTO v_analytics;
  
  RETURN v_analytics;
END;
$$;

-- Function to get sponsor analytics summary
CREATE OR REPLACE FUNCTION public.get_sponsor_analytics(
  p_sponsor_id UUID,
  p_period_start TIMESTAMP WITH TIME ZONE DEFAULT NOW() - INTERVAL '30 days',
  p_period_end TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
  sponsor_id UUID,
  sponsor_name TEXT,
  total_zones INTEGER,
  active_zones INTEGER,
  total_zone_area_km2 DECIMAL,
  total_coins_placed INTEGER,
  total_coins_collected INTEGER,
  total_coins_active INTEGER,
  collection_rate DECIMAL,
  total_value_placed DECIMAL,
  total_value_collected DECIMAL,
  average_coin_value DECIMAL,
  roi_percentage DECIMAL,
  total_unique_collectors INTEGER,
  total_collections INTEGER,
  average_collections_per_zone DECIMAL,
  total_spent DECIMAL,
  cost_per_collection DECIMAL,
  cost_per_unique_collector DECIMAL
)
LANGUAGE plpgsql
AS $$
BEGIN
  RETURN QUERY
  SELECT 
    s.id as sponsor_id,
    s.company_name as sponsor_name,
    COUNT(DISTINCT z.id)::INTEGER as total_zones,
    COUNT(DISTINCT z.id) FILTER (WHERE z.status = 'active')::INTEGER as active_zones,
    COALESCE(SUM(
      CASE 
        WHEN z.geometry->>'type' = 'circle' THEN 
          PI() * POWER((z.geometry->'radius_meters')::DECIMAL / 1000, 2)
        ELSE 0.1 -- Approximate for polygons
      END
    ), 0) as total_zone_area_km2,
    COALESCE(SUM(aza.total_coins_placed), 0)::INTEGER as total_coins_placed,
    COALESCE(SUM(aza.coins_collected), 0)::INTEGER as total_coins_collected,
    COALESCE(SUM(aza.coins_active), 0)::INTEGER as total_coins_active,
    CASE 
      WHEN SUM(aza.total_coins_placed) > 0 THEN 
        (SUM(aza.coins_collected)::DECIMAL / SUM(aza.total_coins_placed) * 100)
      ELSE 0
    END as collection_rate,
    COALESCE(SUM(aza.total_value_placed), 0) as total_value_placed,
    COALESCE(SUM(aza.total_value_collected), 0) as total_value_collected,
    CASE 
      WHEN SUM(aza.total_coins_placed) > 0 THEN 
        (SUM(aza.total_value_placed) / SUM(aza.total_coins_placed))
      ELSE 0
    END as average_coin_value,
    CASE 
      WHEN SUM(aza.total_value_placed) > 0 THEN 
        ((SUM(aza.total_value_collected) - SUM(aza.total_value_placed)) / SUM(aza.total_value_placed) * 100)
      ELSE 0
    END as roi_percentage,
    COALESCE(MAX(aza.unique_collectors), 0)::INTEGER as total_unique_collectors,
    COALESCE(SUM(aza.total_collections), 0)::INTEGER as total_collections,
    CASE 
      WHEN COUNT(DISTINCT z.id) > 0 THEN 
        (SUM(aza.coins_collected)::DECIMAL / COUNT(DISTINCT z.id))
      ELSE 0
    END as average_collections_per_zone,
    s.total_spent as total_spent,
    CASE 
      WHEN SUM(aza.coins_collected) > 0 THEN 
        (s.total_spent / SUM(aza.coins_collected))
      ELSE s.total_spent
    END as cost_per_collection,
    CASE 
      WHEN MAX(aza.unique_collectors) > 0 THEN 
        (s.total_spent / MAX(aza.unique_collectors))
      ELSE s.total_spent
    END as cost_per_unique_collector
  FROM public.sponsors s
  LEFT JOIN public.zones z ON z.sponsor_id = s.id AND z.zone_type = 'sponsor'
  LEFT JOIN public.sponsor_zone_analytics aza ON aza.zone_id = z.id 
    AND aza.period_start >= p_period_start 
    AND aza.period_end <= p_period_end
  WHERE s.id = p_sponsor_id
  GROUP BY s.id, s.company_name, s.total_spent;
END;
$$;

-- ============================================================================
-- RLS POLICIES
-- ============================================================================

ALTER TABLE public.sponsor_zone_analytics ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.bulk_coin_placements ENABLE ROW LEVEL SECURITY;

-- Super admins can do everything
CREATE POLICY "Admins can manage sponsor analytics" ON public.sponsor_zone_analytics
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

CREATE POLICY "Admins can manage bulk placements" ON public.bulk_coin_placements
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Sponsor admins can view their own analytics
CREATE POLICY "Sponsor admins can view own analytics" ON public.sponsor_zone_analytics
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.sponsors
      WHERE id = sponsor_id AND admin_user_id = auth.uid()
    )
  );

-- ============================================================================
-- TRIGGERS
-- ============================================================================

-- Update timestamp trigger
CREATE TRIGGER sponsor_zone_analytics_updated_at
  BEFORE UPDATE ON public.sponsor_zone_analytics
  FOR EACH ROW
  EXECUTE FUNCTION public.update_updated_at();

CREATE TRIGGER bulk_coin_placements_updated_at
  BEFORE UPDATE ON public.bulk_coin_placements
  FOR EACH ROW
  EXECUTE FUNCTION public.update_updated_at();
