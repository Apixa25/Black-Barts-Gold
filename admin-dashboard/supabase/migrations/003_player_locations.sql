-- ============================================================================
-- Player Locations Table - M4: Player Tracking
-- ============================================================================
-- This table stores real-time player locations for the admin dashboard map.
-- The Unity app sends location updates every 5-10 seconds.
-- 
-- Run this in Supabase SQL Editor to create the player tracking system.
-- ============================================================================

-- Create player_locations table
CREATE TABLE IF NOT EXISTS public.player_locations (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
  
  -- Current position
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  altitude DOUBLE PRECISION,
  
  -- Accuracy & quality
  accuracy_meters DOUBLE PRECISION NOT NULL DEFAULT 10,
  heading DOUBLE PRECISION,          -- Direction in degrees (0-360, 0=North)
  speed_mps DOUBLE PRECISION,        -- Speed in meters per second
  
  -- Device info
  device_id TEXT,                    -- Unique device identifier
  device_model TEXT,                 -- e.g., "OnePlus 9 Pro"
  app_version TEXT,                  -- Unity app version
  
  -- Session info
  session_id UUID,                   -- Current play session ID
  is_ar_active BOOLEAN DEFAULT FALSE,
  
  -- Anti-cheat metadata
  is_mock_location BOOLEAN DEFAULT FALSE,
  movement_type TEXT DEFAULT 'walking' CHECK (movement_type IN ('walking', 'running', 'driving', 'suspicious')),
  distance_traveled_session DOUBLE PRECISION DEFAULT 0,
  
  -- Zone context (FK added later if zones table exists)
  current_zone_id UUID,
  
  -- Timestamps
  client_timestamp TIMESTAMP WITH TIME ZONE,  -- When device recorded position
  server_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create unique constraint on user_id (one location per user)
CREATE UNIQUE INDEX IF NOT EXISTS player_locations_user_id_unique ON public.player_locations (user_id);

-- Create spatial index for geo queries
CREATE INDEX IF NOT EXISTS player_locations_lat_lng_idx ON public.player_locations (latitude, longitude);

-- Create index for recent locations
CREATE INDEX IF NOT EXISTS player_locations_updated_at_idx ON public.player_locations (updated_at DESC);

-- Create index for zone filtering
CREATE INDEX IF NOT EXISTS player_locations_zone_idx ON public.player_locations (current_zone_id);

-- Create index for suspicious activity
CREATE INDEX IF NOT EXISTS player_locations_suspicious_idx ON public.player_locations (movement_type) WHERE movement_type = 'suspicious';

-- Enable Row Level Security
ALTER TABLE public.player_locations ENABLE ROW LEVEL SECURITY;

-- ============================================================================
-- RLS Policies for player_locations
-- Using DROP IF EXISTS + CREATE pattern for idempotent migrations
-- (PostgreSQL does not support CREATE POLICY IF NOT EXISTS)
-- ============================================================================

DROP POLICY IF EXISTS "Admins can view all player locations" ON public.player_locations;
CREATE POLICY "Admins can view all player locations" ON public.player_locations
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

DROP POLICY IF EXISTS "Users can update own location" ON public.player_locations;
CREATE POLICY "Users can update own location" ON public.player_locations
  FOR ALL USING (auth.uid() = user_id);

DROP POLICY IF EXISTS "Service role can manage locations" ON public.player_locations;
CREATE POLICY "Service role can manage locations" ON public.player_locations
  FOR ALL USING (true);

-- Create updated_at trigger (drop first for idempotency)
DROP TRIGGER IF EXISTS player_locations_updated_at ON public.player_locations;
CREATE TRIGGER player_locations_updated_at
  BEFORE UPDATE ON public.player_locations
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();

-- ============================================================================
-- Player Location History Table (for trails and anti-cheat)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.player_location_history (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  accuracy_meters DOUBLE PRECISION NOT NULL,
  speed_mps DOUBLE PRECISION,
  movement_type TEXT DEFAULT 'walking' CHECK (movement_type IN ('walking', 'running', 'driving', 'suspicious')),
  recorded_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for history queries
CREATE INDEX IF NOT EXISTS player_location_history_user_idx ON public.player_location_history (user_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS player_location_history_time_idx ON public.player_location_history (recorded_at DESC);

-- Enable RLS
ALTER TABLE public.player_location_history ENABLE ROW LEVEL SECURITY;

-- ============================================================================
-- RLS Policies for player_location_history
-- Using DROP IF EXISTS + CREATE pattern for idempotent migrations
-- ============================================================================

DROP POLICY IF EXISTS "Admins can view player history" ON public.player_location_history;
CREATE POLICY "Admins can view player history" ON public.player_location_history
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin')
    )
  );

DROP POLICY IF EXISTS "Service role can insert history" ON public.player_location_history;
CREATE POLICY "Service role can insert history" ON public.player_location_history
  FOR INSERT WITH CHECK (true);

-- ============================================================================
-- Function to upsert player location (called by Unity app)
-- ============================================================================

CREATE OR REPLACE FUNCTION public.upsert_player_location(
  p_latitude DOUBLE PRECISION,
  p_longitude DOUBLE PRECISION,
  p_altitude DOUBLE PRECISION DEFAULT NULL,
  p_accuracy_meters DOUBLE PRECISION DEFAULT 10,
  p_heading DOUBLE PRECISION DEFAULT NULL,
  p_speed_mps DOUBLE PRECISION DEFAULT NULL,
  p_device_id TEXT DEFAULT NULL,
  p_device_model TEXT DEFAULT NULL,
  p_app_version TEXT DEFAULT NULL,
  p_session_id UUID DEFAULT NULL,
  p_is_ar_active BOOLEAN DEFAULT FALSE,
  p_is_mock_location BOOLEAN DEFAULT FALSE,
  p_client_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS UUID AS $$
DECLARE
  v_user_id UUID;
  v_location_id UUID;
  v_movement_type TEXT;
  v_speed_kmh DOUBLE PRECISION;
  v_previous_location RECORD;
  v_distance_delta DOUBLE PRECISION;
BEGIN
  -- Get current user
  v_user_id := auth.uid();
  IF v_user_id IS NULL THEN
    RAISE EXCEPTION 'Not authenticated';
  END IF;
  
  -- Calculate speed in km/h
  v_speed_kmh := COALESCE(p_speed_mps * 3.6, 0);
  
  -- Determine movement type based on speed
  IF v_speed_kmh <= 6 THEN
    v_movement_type := 'walking';
  ELSIF v_speed_kmh <= 20 THEN
    v_movement_type := 'running';
  ELSIF v_speed_kmh <= 120 THEN
    v_movement_type := 'driving';
  ELSE
    v_movement_type := 'suspicious';
  END IF;
  
  -- Flag mock location as suspicious
  IF p_is_mock_location THEN
    v_movement_type := 'suspicious';
  END IF;
  
  -- Get previous location for distance calculation
  SELECT latitude, longitude, distance_traveled_session
  INTO v_previous_location
  FROM public.player_locations
  WHERE user_id = v_user_id;
  
  -- Calculate distance from previous location (Haversine approximation)
  IF v_previous_location IS NOT NULL THEN
    v_distance_delta := 111000 * SQRT(
      POWER(p_latitude - v_previous_location.latitude, 2) +
      POWER((p_longitude - v_previous_location.longitude) * COS(RADIANS(p_latitude)), 2)
    );
  ELSE
    v_distance_delta := 0;
  END IF;
  
  -- Upsert the location
  INSERT INTO public.player_locations (
    user_id,
    latitude,
    longitude,
    altitude,
    accuracy_meters,
    heading,
    speed_mps,
    device_id,
    device_model,
    app_version,
    session_id,
    is_ar_active,
    is_mock_location,
    movement_type,
    distance_traveled_session,
    client_timestamp,
    server_timestamp
  ) VALUES (
    v_user_id,
    p_latitude,
    p_longitude,
    p_altitude,
    p_accuracy_meters,
    p_heading,
    p_speed_mps,
    p_device_id,
    p_device_model,
    p_app_version,
    p_session_id,
    p_is_ar_active,
    p_is_mock_location,
    v_movement_type,
    COALESCE(v_previous_location.distance_traveled_session, 0) + v_distance_delta,
    p_client_timestamp,
    NOW()
  )
  ON CONFLICT (user_id) DO UPDATE SET
    latitude = EXCLUDED.latitude,
    longitude = EXCLUDED.longitude,
    altitude = EXCLUDED.altitude,
    accuracy_meters = EXCLUDED.accuracy_meters,
    heading = EXCLUDED.heading,
    speed_mps = EXCLUDED.speed_mps,
    device_id = EXCLUDED.device_id,
    device_model = EXCLUDED.device_model,
    app_version = EXCLUDED.app_version,
    session_id = EXCLUDED.session_id,
    is_ar_active = EXCLUDED.is_ar_active,
    is_mock_location = EXCLUDED.is_mock_location,
    movement_type = EXCLUDED.movement_type,
    distance_traveled_session = EXCLUDED.distance_traveled_session,
    client_timestamp = EXCLUDED.client_timestamp,
    server_timestamp = NOW(),
    updated_at = NOW()
  RETURNING id INTO v_location_id;
  
  -- Record in history (for trails, sample every 10 seconds or significant movement)
  INSERT INTO public.player_location_history (
    user_id,
    latitude,
    longitude,
    accuracy_meters,
    speed_mps,
    movement_type
  ) VALUES (
    v_user_id,
    p_latitude,
    p_longitude,
    p_accuracy_meters,
    p_speed_mps,
    v_movement_type
  );
  
  RETURN v_location_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Enable Realtime for player_locations
-- ============================================================================

-- Safely add to Realtime publication (idempotent)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables 
    WHERE pubname = 'supabase_realtime' 
    AND tablename = 'player_locations'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.player_locations;
    RAISE NOTICE 'Added player_locations to supabase_realtime publication';
  ELSE
    RAISE NOTICE 'player_locations already in supabase_realtime publication';
  END IF;
END $$;

-- ============================================================================
-- Clean up old history (run periodically via cron/scheduled function)
-- ============================================================================

CREATE OR REPLACE FUNCTION public.cleanup_old_player_history()
RETURNS INTEGER AS $$
DECLARE
  deleted_count INTEGER;
BEGIN
  -- Delete history older than 24 hours
  DELETE FROM public.player_location_history
  WHERE recorded_at < NOW() - INTERVAL '24 hours';
  
  GET DIAGNOSTICS deleted_count = ROW_COUNT;
  RETURN deleted_count;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Comments
-- ============================================================================

COMMENT ON TABLE public.player_locations IS 'Real-time player locations for admin dashboard tracking';
COMMENT ON TABLE public.player_location_history IS 'Historical player positions for trails and anti-cheat analysis';
COMMENT ON FUNCTION public.upsert_player_location IS 'Upserts player location from Unity app, calculates movement type and distance';
