/**
 * POST /api/v1/coins/hide
 * 
 * Hide (create) a new coin at a location.
 * This endpoint is used by both:
 * - Admin dashboard for manual coin placement
 * - Unity app when players hide their own coins
 * 
 * Request body:
 * {
 *   type: 'fixed' | 'pool',
 *   value: number,
 *   latitude: number,
 *   longitude: number,
 *   hiderId?: string,        // User hiding the coin
 *   message?: string,        // Optional message/hint
 *   tier?: 'gold' | 'silver' | 'bronze',
 *   isMythical?: boolean,
 * }
 * 
 * @file admin-dashboard/src/app/api/v1/coins/hide/route.ts
 * Character count: ~3,800
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'
import { keysToCamelCase } from '@/lib/api-utils'
import type { CoinType, CoinTier } from '@/types/database'

interface HideCoinRequest {
  type?: CoinType
  coin_type?: CoinType  // Alternative field name
  value: number
  latitude: number
  longitude: number
  hiderId?: string
  hider_id?: string  // Alternative field name
  message?: string
  description?: string  // Alternative field name
  tier?: CoinTier
  isMythical?: boolean
  is_mythical?: boolean  // Alternative field name
  locationName?: string
  location_name?: string  // Alternative field name
}

/**
 * Determine coin tier based on value
 */
function determineTier(value: number): CoinTier {
  if (value >= 25) return 'gold'
  if (value >= 5) return 'silver'
  return 'bronze'
}

export async function POST(request: NextRequest) {
  try {
    // Parse request body
    let body: HideCoinRequest
    try {
      body = await request.json()
    } catch {
      return NextResponse.json(
        { success: false, error: 'Invalid JSON body', code: 'INVALID_BODY' },
        { status: 400 }
      )
    }
    
    // Validate required fields
    const { value, latitude, longitude } = body
    
    if (value === undefined || value <= 0) {
      return NextResponse.json(
        { success: false, error: 'Value must be greater than 0', code: 'INVALID_VALUE' },
        { status: 400 }
      )
    }
    
    if (latitude === undefined || longitude === undefined) {
      return NextResponse.json(
        { success: false, error: 'Latitude and longitude are required', code: 'MISSING_LOCATION' },
        { status: 400 }
      )
    }
    
    // Validate coordinates
    if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) {
      return NextResponse.json(
        { success: false, error: 'Invalid coordinates', code: 'INVALID_COORDS' },
        { status: 400 }
      )
    }
    
    const supabase = await createClient()
    
    // Prepare coin data (handle both naming conventions)
    const coinType = body.type || body.coin_type || 'fixed'
    const tier = body.tier || determineTier(value)
    const isMythical = body.isMythical || body.is_mythical || false
    const hiderId = body.hiderId || body.hider_id || null
    const description = body.message || body.description || null
    const locationName = body.locationName || body.location_name || null
    
    const coinData = {
      coin_type: coinType,
      value,
      tier,
      is_mythical: isMythical,
      latitude,
      longitude,
      location_name: locationName,
      description,
      status: 'hidden' as const,
      hider_id: hiderId,
      hidden_at: new Date().toISOString(),
      multi_find: false,
      finds_remaining: 1,
    }
    
    // Insert the coin
    const { data: coin, error } = await supabase
      .from('coins')
      .insert(coinData)
      .select()
      .single()
    
    if (error) {
      console.error('[API] Error creating coin:', error)
      return NextResponse.json(
        { success: false, error: 'Failed to create coin', code: 'INSERT_FAILED' },
        { status: 500 }
      )
    }
    
    console.log(`[API] Coin hidden: ${coin.id}, value: $${value.toFixed(2)} at (${latitude}, ${longitude})`)
    
    // Convert to camelCase for Unity compatibility
    const camelCaseCoin = keysToCamelCase(coin)
    
    return NextResponse.json({
      success: true,
      coin: camelCaseCoin,
      message: 'Treasure hidden successfully! 🏴‍☠️',
    })
    
  } catch (error) {
    console.error('[API] Error in POST /coins/hide:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}
