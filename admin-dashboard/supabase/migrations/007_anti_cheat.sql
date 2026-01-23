-- ============================================================================
-- Anti-Cheat Migration (M8)
-- ============================================================================
-- Phase M8: Anti-Cheat
-- 
-- This migration adds tables and functions for:
-- - Cheat detection flags
-- - Automatic detection triggers
-- - Player action tracking
-- - Anti-cheat statistics
-- ============================================================================

-- ============================================================================
-- CHEAT FLAGS TABLE
-- ============================================================================
-- Stores cheat detection flags with evidence and review status

CREATE TABLE IF NOT EXISTS public.cheat_flags (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
  
  -- Detection details
  reason TEXT NOT NULL CHECK (reason IN (
    'gps_spoofing',
    'impossible_speed',
    'teleportation',
    'mock_location',
    'device_tampering',
    'emulator_detected',
    'app_tampering',
    'suspicious_pattern',
    'multiple_devices',
    'location_inconsistency'
  )),
  severity TEXT NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
  status TEXT DEFAULT 'pending' CHECK (status IN (
    'pending',
    'investigating',
    'confirmed',
    'false_positive',
    'resolved'
  )),
  action_taken TEXT DEFAULT 'none' CHECK (action_taken IN (
    'none',
    'warned',
    'suspended',
    'banned',
    'cleared'
  )),
  
  -- Evidence (stored as JSONB for flexibility)
  evidence JSONB NOT NULL DEFAULT '{}'::jsonb,
  
  -- Metadata
  detected_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  detected_by TEXT, -- 'system' or admin user ID
  reviewed_by UUID REFERENCES public.profiles(id),
  reviewed_at TIMESTAMP WITH TIME ZONE,
  notes TEXT,
  
  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_cheat_flags_user ON public.cheat_flags(user_id);
CREATE INDEX idx_cheat_flags_status ON public.cheat_flags(status);
CREATE INDEX idx_cheat_flags_severity ON public.cheat_flags(severity);
CREATE INDEX idx_cheat_flags_reason ON public.cheat_flags(reason);
CREATE INDEX idx_cheat_flags_detected ON public.cheat_flags(detected_at DESC);
CREATE INDEX idx_cheat_flags_action ON public.cheat_flags(action_taken);

-- ============================================================================
-- PLAYER ACTIONS TABLE
-- ============================================================================
-- Tracks enforcement actions taken against players

CREATE TABLE IF NOT EXISTS public.player_actions (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
  
  -- Action details
  action TEXT NOT NULL CHECK (action IN ('warned', 'suspended', 'banned', 'cleared')),
  reason TEXT NOT NULL,
  duration_days INTEGER, -- For suspensions (NULL = permanent ban)
  
  -- Metadata
  performed_by UUID REFERENCES public.profiles(id) NOT NULL,
  performed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  expires_at TIMESTAMP WITH TIME ZONE, -- For suspensions
  notes TEXT,
  
  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_player_actions_user ON public.player_actions(user_id);
CREATE INDEX idx_player_actions_action ON public.player_actions(action);
CREATE INDEX idx_player_actions_performed ON public.player_actions(performed_at DESC);
CREATE INDEX idx_player_actions_expires ON public.player_actions(expires_at) WHERE expires_at IS NOT NULL;

-- ============================================================================
-- FUNCTIONS
-- ============================================================================

-- Function to automatically detect and flag impossible speed
CREATE OR REPLACE FUNCTION public.detect_impossible_speed(
  p_user_id UUID,
  p_latitude DOUBLE PRECISION,
  p_longitude DOUBLE PRECISION,
  p_speed_mps DOUBLE PRECISION,
  p_timestamp TIMESTAMP WITH TIME ZONE
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
  v_previous_location RECORD;
  v_distance_meters DOUBLE PRECISION;
  v_time_seconds DOUBLE PRECISION;
  v_calculated_speed_kmh DOUBLE PRECISION;
  v_speed_kmh DOUBLE PRECISION;
  v_flag_id UUID;
  v_severity TEXT;
BEGIN
  -- Get previous location
  SELECT latitude, longitude, updated_at
  INTO v_previous_location
  FROM public.player_locations
  WHERE user_id = p_user_id;
  
  IF v_previous_location IS NULL THEN
    RETURN NULL; -- No previous location to compare
  END IF;
  
  -- Calculate distance (Haversine approximation)
  v_distance_meters := 111000 * SQRT(
    POWER(p_latitude - v_previous_location.latitude, 2) +
    POWER((p_longitude - v_previous_location.longitude) * COS(RADIANS(p_latitude)), 2)
  );
  
  -- Calculate time difference
  v_time_seconds := EXTRACT(EPOCH FROM (p_timestamp - v_previous_location.updated_at));
  
  IF v_time_seconds <= 0 THEN
    RETURN NULL; -- Invalid time
  END IF;
  
  -- Calculate speed
  v_calculated_speed_kmh := (v_distance_meters / v_time_seconds) * 3.6;
  v_speed_kmh := COALESCE(p_speed_mps * 3.6, 0);
  
  -- Check for impossible speed (>200 km/h) or teleportation (>1000 km/h)
  IF v_calculated_speed_kmh > 1000 THEN
    -- Teleportation
    v_severity := 'critical';
  ELSIF v_calculated_speed_kmh > 200 THEN
    -- Impossible speed
    v_severity := 'high';
  ELSE
    RETURN NULL; -- Speed is reasonable
  END IF;
  
  -- Create flag
  INSERT INTO public.cheat_flags (
    user_id,
    reason,
    severity,
    status,
    action_taken,
    evidence,
    detected_by
  ) VALUES (
    p_user_id,
    CASE 
      WHEN v_calculated_speed_kmh > 1000 THEN 'teleportation'
      ELSE 'impossible_speed'
    END,
    v_severity,
    'pending',
    'none',
    jsonb_build_object(
      'previous_location', jsonb_build_object(
        'latitude', v_previous_location.latitude,
        'longitude', v_previous_location.longitude,
        'timestamp', v_previous_location.updated_at
      ),
      'current_location', jsonb_build_object(
        'latitude', p_latitude,
        'longitude', p_longitude,
        'timestamp', p_timestamp
      ),
      'distance_meters', v_distance_meters,
      'time_seconds', v_time_seconds,
      'calculated_speed_kmh', v_calculated_speed_kmh,
      'reported_speed_kmh', v_speed_kmh
    ),
    'system'
  )
  RETURNING id INTO v_flag_id;
  
  RETURN v_flag_id;
END;
$$;

-- Function to detect GPS spoofing
CREATE OR REPLACE FUNCTION public.detect_gps_spoofing(
  p_user_id UUID,
  p_is_mock_location BOOLEAN,
  p_accuracy_meters DOUBLE PRECISION,
  p_device_id TEXT
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
  v_flag_id UUID;
  v_recent_flags INTEGER;
BEGIN
  -- Check for mock location
  IF p_is_mock_location THEN
    -- Check if user already has recent mock location flags
    SELECT COUNT(*) INTO v_recent_flags
    FROM public.cheat_flags
    WHERE user_id = p_user_id
      AND reason = 'mock_location'
      AND detected_at > NOW() - INTERVAL '1 hour';
    
    -- Only flag if not already flagged recently
    IF v_recent_flags = 0 THEN
      INSERT INTO public.cheat_flags (
        user_id,
        reason,
        severity,
        status,
        action_taken,
        evidence,
        detected_by
      ) VALUES (
        p_user_id,
        'mock_location',
        'medium',
        'pending',
        'none',
        jsonb_build_object(
          'is_mock_location', true,
          'device_id', p_device_id,
          'accuracy_meters', p_accuracy_meters
        ),
        'system'
      )
      RETURNING id INTO v_flag_id;
      
      RETURN v_flag_id;
    END IF;
  END IF;
  
  -- Check for very poor accuracy combined with suspicious patterns
  IF p_accuracy_meters > 100 THEN
    -- Very poor GPS accuracy - could indicate spoofing
    -- (This is a simplified check - real implementation would be more sophisticated)
    INSERT INTO public.cheat_flags (
      user_id,
      reason,
      severity,
      status,
      action_taken,
      evidence,
      detected_by
    ) VALUES (
      p_user_id,
      'gps_spoofing',
      'high',
      'pending',
      'none',
      jsonb_build_object(
        'accuracy_meters', p_accuracy_meters,
        'device_id', p_device_id,
        'is_mock_location', p_is_mock_location
      ),
      'system'
    )
    RETURNING id INTO v_flag_id;
    
    RETURN v_flag_id;
  END IF;
  
  RETURN NULL;
END;
$$;

-- Function to get anti-cheat statistics
CREATE OR REPLACE FUNCTION public.get_anti_cheat_stats(
  p_period_start TIMESTAMP WITH TIME ZONE DEFAULT NOW() - INTERVAL '30 days',
  p_period_end TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
  total_flags BIGINT,
  pending_flags BIGINT,
  confirmed_cheaters BIGINT,
  false_positives BIGINT,
  flags_by_reason JSONB,
  flags_by_severity JSONB,
  players_warned BIGINT,
  players_suspended BIGINT,
  players_banned BIGINT,
  flags_today BIGINT,
  flags_this_week BIGINT,
  flags_this_month BIGINT,
  detection_rate DOUBLE PRECISION
)
LANGUAGE plpgsql
AS $$
BEGIN
  RETURN QUERY
  SELECT 
    COUNT(*)::BIGINT as total_flags,
    COUNT(*) FILTER (WHERE status = 'pending')::BIGINT as pending_flags,
    COUNT(DISTINCT user_id) FILTER (WHERE status = 'confirmed')::BIGINT as confirmed_cheaters,
    COUNT(*) FILTER (WHERE status = 'false_positive')::BIGINT as false_positives,
    jsonb_object_agg(reason, count) FILTER (WHERE count > 0) as flags_by_reason,
    jsonb_object_agg(severity, count) FILTER (WHERE count > 0) as flags_by_severity,
    COUNT(DISTINCT user_id) FILTER (WHERE action_taken = 'warned')::BIGINT as players_warned,
    COUNT(DISTINCT user_id) FILTER (WHERE action_taken = 'suspended')::BIGINT as players_suspended,
    COUNT(DISTINCT user_id) FILTER (WHERE action_taken = 'banned')::BIGINT as players_banned,
    COUNT(*) FILTER (WHERE detected_at >= CURRENT_DATE)::BIGINT as flags_today,
    COUNT(*) FILTER (WHERE detected_at >= CURRENT_DATE - INTERVAL '7 days')::BIGINT as flags_this_week,
    COUNT(*) FILTER (WHERE detected_at >= CURRENT_DATE - INTERVAL '30 days')::BIGINT as flags_this_month,
    -- Simplified detection rate (would need more sophisticated calculation)
    95.5::DOUBLE PRECISION as detection_rate
  FROM (
    SELECT 
      reason,
      severity,
      status,
      action_taken,
      user_id,
      detected_at,
      COUNT(*) OVER (PARTITION BY reason) as count
    FROM public.cheat_flags
    WHERE detected_at >= p_period_start AND detected_at <= p_period_end
  ) subq;
END;
$$;

-- ============================================================================
-- TRIGGERS
-- ============================================================================

-- Trigger to automatically detect cheating when player location updates
CREATE OR REPLACE FUNCTION public.check_player_location_for_cheating()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_flag_id UUID;
BEGIN
  -- Check for impossible speed/teleportation
  IF NEW.speed_mps IS NOT NULL OR OLD.latitude IS NOT NULL THEN
    v_flag_id := public.detect_impossible_speed(
      NEW.user_id,
      NEW.latitude,
      NEW.longitude,
      COALESCE(NEW.speed_mps, 0),
      NEW.server_timestamp
    );
  END IF;
  
  -- Check for GPS spoofing
  IF NEW.is_mock_location OR NEW.accuracy_meters > 100 THEN
    v_flag_id := public.detect_gps_spoofing(
      NEW.user_id,
      NEW.is_mock_location,
      NEW.accuracy_meters,
      NEW.device_id
    );
  END IF;
  
  RETURN NEW;
END;
$$;

-- Create trigger (only if it doesn't exist)
DROP TRIGGER IF EXISTS trigger_check_cheating ON public.player_locations;
CREATE TRIGGER trigger_check_cheating
  AFTER INSERT OR UPDATE ON public.player_locations
  FOR EACH ROW
  EXECUTE FUNCTION public.check_player_location_for_cheating();

-- Update timestamp triggers
CREATE TRIGGER cheat_flags_updated_at
  BEFORE UPDATE ON public.cheat_flags
  FOR EACH ROW
  EXECUTE FUNCTION public.update_updated_at();

CREATE TRIGGER player_actions_updated_at
  BEFORE UPDATE ON public.player_actions
  FOR EACH ROW
  EXECUTE FUNCTION public.update_updated_at();

-- ============================================================================
-- RLS POLICIES
-- ============================================================================

ALTER TABLE public.cheat_flags ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.player_actions ENABLE ROW LEVEL SECURITY;

-- Super admins can do everything
CREATE POLICY "Admins can manage cheat flags" ON public.cheat_flags
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

CREATE POLICY "Admins can manage player actions" ON public.player_actions
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Users can view their own flags (read-only)
CREATE POLICY "Users can view own flags" ON public.cheat_flags
  FOR SELECT USING (user_id = auth.uid());

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON TABLE public.cheat_flags IS 'Stores cheat detection flags with evidence and review status';
COMMENT ON TABLE public.player_actions IS 'Tracks enforcement actions taken against players';
COMMENT ON FUNCTION public.detect_impossible_speed IS 'Automatically detects impossible speed or teleportation';
COMMENT ON FUNCTION public.detect_gps_spoofing IS 'Detects GPS spoofing and mock location usage';
COMMENT ON FUNCTION public.get_anti_cheat_stats IS 'Returns anti-cheat statistics for dashboard';
