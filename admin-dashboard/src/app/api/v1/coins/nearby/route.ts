/**
 * GET /api/v1/coins/nearby
 * 
 * Fetch coins near a GPS location for the Unity mobile app.
 * This is the primary endpoint for the AR treasure hunting experience.
 * 
 * Query Parameters:
 * - lat: Latitude (required)
 * - lng: Longitude (required)  
 * - radius: Search radius in meters (optional, default 500)
 * 
 * Returns coins that are:
 * - Within the specified radius
 * - Status is 'hidden' or 'visible' (not collected/expired)
 * - Not already collected by this user
 * 
 * @file admin-dashboard/src/app/api/v1/coins/nearby/route.ts
 * Character count: ~4,500
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'
import { keysToCamelCase } from '@/lib/api-utils'

// Default search radius in meters
const DEFAULT_RADIUS = 500
const MAX_RADIUS = 5000 // Maximum allowed radius to prevent abuse

// Earth's radius in meters for Haversine calculation
const EARTH_RADIUS_METERS = 6371000

/**
 * Calculate approximate bounding box for a point and radius
 * This is used to efficiently filter coins before doing precise distance calc
 */
function getBoundingBox(lat: number, lng: number, radiusMeters: number) {
  // Rough conversion: 1 degree latitude ≈ 111,320 meters
  const latDelta = radiusMeters / 111320
  // Longitude degrees vary by latitude
  const lngDelta = radiusMeters / (111320 * Math.cos(lat * Math.PI / 180))
  
  return {
    minLat: lat - latDelta,
    maxLat: lat + latDelta,
    minLng: lng - lngDelta,
    maxLng: lng + lngDelta,
  }
}

/**
 * Calculate Haversine distance between two points
 * Returns distance in meters
 */
function haversineDistance(
  lat1: number, lng1: number,
  lat2: number, lng2: number
): number {
  const dLat = (lat2 - lat1) * Math.PI / 180
  const dLng = (lng2 - lng1) * Math.PI / 180
  
  const a = 
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
    Math.sin(dLng / 2) * Math.sin(dLng / 2)
  
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
  
  return EARTH_RADIUS_METERS * c
}

/**
 * Calculate bearing from point 1 to point 2
 * Returns bearing in degrees (0-360, 0 = North)
 */
function calculateBearing(
  lat1: number, lng1: number,
  lat2: number, lng2: number
): number {
  const dLng = (lng2 - lng1) * Math.PI / 180
  const lat1Rad = lat1 * Math.PI / 180
  const lat2Rad = lat2 * Math.PI / 180
  
  const y = Math.sin(dLng) * Math.cos(lat2Rad)
  const x = Math.cos(lat1Rad) * Math.sin(lat2Rad) -
            Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(dLng)
  
  let bearing = Math.atan2(y, x) * 180 / Math.PI
  bearing = (bearing + 360) % 360 // Normalize to 0-360
  
  return bearing
}

export async function GET(request: NextRequest) {
  try {
    // Parse query parameters
    const { searchParams } = new URL(request.url)
    const latStr = searchParams.get('lat')
    const lngStr = searchParams.get('lng')
    const radiusStr = searchParams.get('radius')
    
    // Validate required parameters
    if (!latStr || !lngStr) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Missing required parameters: lat and lng',
          code: 'MISSING_PARAMS'
        },
        { status: 400 }
      )
    }
    
    const lat = parseFloat(latStr)
    const lng = parseFloat(lngStr)
    const radius = Math.min(
      parseFloat(radiusStr || String(DEFAULT_RADIUS)),
      MAX_RADIUS
    )
    
    // Validate coordinate values
    if (isNaN(lat) || isNaN(lng) || lat < -90 || lat > 90 || lng < -180 || lng > 180) {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Invalid coordinates',
          code: 'INVALID_COORDS'
        },
        { status: 400 }
      )
    }
    
    // Get Supabase client
    const supabase = await createClient()
    
    // Calculate bounding box for efficient query
    const bbox = getBoundingBox(lat, lng, radius)
    
    // Query coins within bounding box
    // We filter by bounding box first (fast), then calculate precise distance
    const { data: coins, error } = await supabase
      .from('coins')
      .select(`
        id,
        coin_type,
        value,
        tier,
        is_mythical,
        latitude,
        longitude,
        location_name,
        status,
        hider_id,
        hidden_at,
        sponsor_id,
        logo_url,
        multi_find,
        finds_remaining,
        description
      `)
      .gte('latitude', bbox.minLat)
      .lte('latitude', bbox.maxLat)
      .gte('longitude', bbox.minLng)
      .lte('longitude', bbox.maxLng)
      .in('status', ['hidden', 'visible']) // Only active coins
      .order('value', { ascending: false }) // Higher value first
    
    if (error) {
      console.error('[API] Error fetching coins:', error)
      return NextResponse.json(
        { 
          success: false, 
          error: 'Database error',
          code: 'DB_ERROR'
        },
        { status: 500 }
      )
    }
    
    // Calculate precise distance and filter to actual radius
    // Also add distance and bearing for each coin
    const nearbyCoins = (coins || [])
      .map(coin => {
        const distance = haversineDistance(lat, lng, coin.latitude, coin.longitude)
        const bearing = calculateBearing(lat, lng, coin.latitude, coin.longitude)
        
        return {
          ...coin,
          // Add computed fields for the Unity app
          distanceMeters: Math.round(distance * 10) / 10, // 1 decimal place
          bearingDegrees: Math.round(bearing * 10) / 10,
          // Determine if collectible (within 5m range as per game design)
          isInRange: distance <= 5,
        }
      })
      .filter(coin => coin.distanceMeters <= radius)
      .sort((a, b) => a.distanceMeters - b.distanceMeters) // Closest first
    
    // Log for debugging (remove in production)
    console.log(`[API] /coins/nearby: Found ${nearbyCoins.length} coins within ${radius}m of (${lat}, ${lng})`)
    
    // Convert to camelCase for Unity compatibility
    const camelCaseCoins = keysToCamelCase(nearbyCoins)
    
    return NextResponse.json({
      success: true,
      coins: camelCaseCoins,
      totalCount: nearbyCoins.length,
      searchCenter: { lat, lng },
      searchRadius: radius,
      timestamp: new Date().toISOString(),
    })
    
  } catch (error) {
    console.error('[API] Unexpected error in /coins/nearby:', error)
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
