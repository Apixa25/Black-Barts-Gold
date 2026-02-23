/**
 * POST /api/v1/player/location
 * 
 * Update player's real-time location for the admin dashboard map.
 * Called by the Unity mobile app (Prize-Finder) every 5 seconds while active.
 * 
 * This endpoint:
 * 1. Accepts location data from Unity app
 * 2. Validates the request
 * 3. Upserts to player_locations table (one row per user)
 * 4. Triggers Supabase Realtime for admin dashboard
 * 
 * Request Body (JSON):
 * {
 *   userId: string,           // User's profile ID (required)
 *   latitude: number,         // Current latitude (required)
 *   longitude: number,        // Current longitude (required)
 *   altitude?: number,        // Altitude in meters
 *   accuracyMeters?: number,  // GPS accuracy
 *   heading?: number,         // Direction 0-360
 *   speedMps?: number,        // Speed in meters/second
 *   deviceId?: string,        // Unique device identifier
 *   deviceModel?: string,     // Device model name
 *   appVersion?: string,      // Unity app version
 *   sessionId?: string,       // Current play session ID
 *   isArActive?: boolean,     // Currently in AR mode
 *   isMockLocation?: boolean, // Mock location detected
 *   clientTimestamp?: string, // When device recorded position
 * }
 * 
 * @file admin-dashboard/src/app/api/v1/player/location/route.ts
 * Character count: ~5,500
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'

// Request body type (camelCase from Unity)
interface LocationUpdateRequest {
  userId: string
  latitude: number
  longitude: number
  altitude?: number
  accuracyMeters?: number
  heading?: number
  speedMps?: number
  deviceId?: string
  deviceModel?: string
  appVersion?: string
  sessionId?: string
  isArActive?: boolean
  isMockLocation?: boolean
  clientTimestamp?: string
}

// Speed thresholds for movement type detection (km/h)
const SPEED_THRESHOLDS = {
  walking: 6,
  running: 20,
  driving: 120,
}

/**
 * Determine movement type based on speed
 */
function getMovementType(speedMps: number | undefined, isMockLocation: boolean): string {
  if (isMockLocation) {
    return 'suspicious'
  }
  
  if (speedMps === undefined || speedMps === null) {
    return 'walking'
  }
  
  const speedKmh = speedMps * 3.6
  
  if (speedKmh <= SPEED_THRESHOLDS.walking) return 'walking'
  if (speedKmh <= SPEED_THRESHOLDS.running) return 'running'
  if (speedKmh <= SPEED_THRESHOLDS.driving) return 'driving'
  return 'suspicious'
}

export async function POST(request: NextRequest) {
  try {
    // Parse request body
    const body: LocationUpdateRequest = await request.json()
    
    // Validate required fields
    if (!body.userId) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Missing required field: userId',
          code: 'MISSING_USER_ID'
        },
        { status: 400 }
      )
    }
    
    if (body.latitude === undefined || body.longitude === undefined) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Missing required fields: latitude and longitude',
          code: 'MISSING_COORDS'
        },
        { status: 400 }
      )
    }
    
    // Validate coordinate values
    if (
      isNaN(body.latitude) || 
      isNaN(body.longitude) || 
      body.latitude < -90 || body.latitude > 90 || 
      body.longitude < -180 || body.longitude > 180
    ) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Invalid coordinates',
          code: 'INVALID_COORDS'
        },
        { status: 400 }
      )
    }
    
    // Get service role client (bypasses RLS for this operation)
    const supabase = createServiceRoleClient()
    
    // Determine movement type
    const movementType = getMovementType(body.speedMps, body.isMockLocation || false)
    
    // Build the location data for upsert
    const locationData = {
      user_id: body.userId,
      latitude: body.latitude,
      longitude: body.longitude,
      altitude: body.altitude || null,
      accuracy_meters: body.accuracyMeters || 10,
      heading: body.heading || null,
      speed_mps: body.speedMps || null,
      device_id: body.deviceId || null,
      device_model: body.deviceModel || null,
      app_version: body.appVersion || null,
      session_id: body.sessionId || null,
      is_ar_active: body.isArActive || false,
      is_mock_location: body.isMockLocation || false,
      movement_type: movementType,
      client_timestamp: body.clientTimestamp || new Date().toISOString(),
      server_timestamp: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    }
    
    // Upsert the location (one row per user)
    const { data, error } = await supabase
      .from('player_locations')
      .upsert(locationData, {
        onConflict: 'user_id',
        ignoreDuplicates: false,
      })
      .select('id')
      .single()
    
    if (error) {
      console.error('[API] Error upserting player location:', error)
      
      // Check for specific error types
      if (error.code === '42P01') {
        // Table doesn't exist
        return NextResponse.json(
          { 
            success: false, 
            error: 'player_locations table not found. Please run the M4 migration.',
            code: 'TABLE_NOT_FOUND'
          },
          { status: 500 }
        )
      }
      
      if (error.code === '23503') {
        // Foreign key violation - user doesn't exist
        return NextResponse.json(
          { 
            success: false, 
            error: 'User not found',
            code: 'USER_NOT_FOUND'
          },
          { status: 404 }
        )
      }
      
      return NextResponse.json(
        { 
          success: false, 
          error: 'Database error',
          code: 'DB_ERROR',
          details: error.message
        },
        { status: 500 }
      )
    }
    
    // Also record to history (for trails/anti-cheat) - fire and forget
    supabase
      .from('player_location_history')
      .insert({
        user_id: body.userId,
        latitude: body.latitude,
        longitude: body.longitude,
        accuracy_meters: body.accuracyMeters || 10,
        speed_mps: body.speedMps || null,
        movement_type: movementType,
      })
      .then(({ error: historyError }) => {
        if (historyError) {
          console.warn('[API] Failed to record location history:', historyError.message)
        }
      })
    
    // Log for debugging (can be removed in production)
    console.log(`[API] Player location updated: ${body.userId} at (${body.latitude.toFixed(4)}, ${body.longitude.toFixed(4)}) - ${movementType}`)
    
    return NextResponse.json({
      success: true,
      locationId: data?.id,
      movementType,
      timestamp: new Date().toISOString(),
    })
    
  } catch (error) {
    console.error('[API] Unexpected error in /player/location:', error)
    
    // Check for JSON parse error
    if (error instanceof SyntaxError) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Invalid JSON in request body',
          code: 'INVALID_JSON'
        },
        { status: 400 }
      )
    }
    
    return NextResponse.json(
      { 
        success: false, 
        error: 'Internal server error',
        code: 'INTERNAL_ERROR'
      },
      { status: 500 }
    )
  }
}

/**
 * GET /api/v1/player/location
 *
 * Returns active player rows for the admin dashboard map, enriched with
 * profile details (name/email/avatar) and optional zone names.
 * Uses service role client so admin map cards have complete profile context.
 */
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const includeOffline = searchParams.get('includeOffline') === 'true'
    const zoneId = searchParams.get('zoneId')

    const supabase = createServiceRoleClient()

    let query = supabase
      .from('player_locations')
      .select('*')
      .order('updated_at', { ascending: false })

    if (zoneId) {
      query = query.eq('current_zone_id', zoneId)
    }

    // Keep default behavior aligned with dashboard: active/idle/stale only.
    if (!includeOffline) {
      const cutoffTime = new Date()
      cutoffTime.setMinutes(cutoffTime.getMinutes() - 30)
      query = query.gte('updated_at', cutoffTime.toISOString())
    }

    const { data: locations, error: locationsError } = await query
    if (locationsError) {
      console.error('[API] Error fetching player locations:', locationsError)
      return NextResponse.json(
        { success: false, error: 'Failed to fetch player locations', code: 'DB_ERROR' },
        { status: 500 }
      )
    }

    const rows = locations || []
    if (rows.length === 0) {
      return NextResponse.json({ success: true, players: [] })
    }

    const uniqueUserIds = [...new Set(rows.map((r: any) => r.user_id).filter(Boolean))]
    const uniqueZoneIds = [...new Set(rows.map((r: any) => r.current_zone_id).filter(Boolean))]

    const [{ data: profiles }, { data: zones }] = await Promise.all([
      uniqueUserIds.length > 0
        ? supabase.from('profiles').select('id, full_name, email, avatar_url').in('id', uniqueUserIds)
        : Promise.resolve({ data: [] as any[] }),
      uniqueZoneIds.length > 0
        ? supabase.from('zones').select('id, name').in('id', uniqueZoneIds)
        : Promise.resolve({ data: [] as any[] }),
    ])

    const profilesMap = new Map((profiles || []).map((p: any) => [p.id, p]))
    const zonesMap = new Map((zones || []).map((z: any) => [z.id, z]))

    const players = rows.map((location: any) => {
      const profile = profilesMap.get(location.user_id)
      const zone = zonesMap.get(location.current_zone_id)
      const updatedAtMs = new Date(location.updated_at).getTime()
      const ageMs = Date.now() - updatedAtMs
      const ageSec = Math.floor(ageMs / 1000)

      let activityStatus: 'active' | 'idle' | 'stale' | 'offline' = 'offline'
      if (ageSec <= 30) activityStatus = 'active'
      else if (ageSec <= 120) activityStatus = 'idle'
      else if (ageSec <= 600) activityStatus = 'stale'

      return {
        id: location.id,
        user_id: location.user_id,
        user_name: profile?.full_name || null,
        user_email: profile?.email || null,
        avatar_url: profile?.avatar_url || null,
        latitude: location.latitude,
        longitude: location.longitude,
        accuracy_meters: location.accuracy_meters,
        heading: location.heading,
        activity_status: activityStatus,
        is_ar_active: location.is_ar_active,
        movement_type: location.movement_type,
        current_zone_id: location.current_zone_id,
        current_zone_name: zone?.name || null,
        coins_collected_session: 0,
        time_active_minutes: 0,
        last_updated: location.updated_at,
      }
    })

    return NextResponse.json({ success: true, players })
  } catch (error) {
    console.error('[API] Unexpected error in GET /player/location:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}

/**
 * DELETE /api/v1/player/location
 * 
 * Remove player's location when they go offline/logout.
 * This removes them from the live tracking map.
 */
export async function DELETE(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    const userId = searchParams.get('userId')
    
    if (!userId) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Missing required parameter: userId',
          code: 'MISSING_USER_ID'
        },
        { status: 400 }
      )
    }
    
    const supabase = createServiceRoleClient()
    
    const { error } = await supabase
      .from('player_locations')
      .delete()
      .eq('user_id', userId)
    
    if (error) {
      console.error('[API] Error deleting player location:', error)
      return NextResponse.json(
        { 
          success: false, 
          error: 'Database error',
          code: 'DB_ERROR'
        },
        { status: 500 }
      )
    }
    
    console.log(`[API] Player location removed: ${userId}`)
    
    return NextResponse.json({
      success: true,
      message: 'Player location removed',
    })
    
  } catch (error) {
    console.error('[API] Unexpected error in DELETE /player/location:', error)
    return NextResponse.json(
      { 
        success: false, 
        error: 'Internal server error',
        code: 'INTERNAL_ERROR'
      },
      { status: 500 }
    )
  }
}
