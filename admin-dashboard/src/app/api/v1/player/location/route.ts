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
import { keysToSnakeCase } from '@/lib/api-utils'

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
