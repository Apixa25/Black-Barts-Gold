/**
 * POST /api/v1/coins/[id]/collect
 * 
 * Collect a coin - the main treasure hunting action!
 * This endpoint handles:
 * - Validating the coin can be collected
 * - Calculating final value (for pool coins)
 * - Updating coin status to 'collected'
 * - Creating a transaction record
 * 
 * Request body:
 * {
 *   userId: string,       // The collecting user's ID
 *   latitude: number,     // User's current position (for validation)
 *   longitude: number,
 * }
 * 
 * @file admin-dashboard/src/app/api/v1/coins/[id]/collect/route.ts
 * Character count: ~5,200
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'
import { keysToCamelCase } from '@/lib/api-utils'

interface RouteParams {
  params: Promise<{ id: string }>
}

interface CollectRequest {
  userId?: string
  latitude?: number
  longitude?: number
}

// Collection range in meters (must be within 5m to collect)
const COLLECTION_RANGE_METERS = 5

// Earth's radius for Haversine
const EARTH_RADIUS_METERS = 6371000

/**
 * Calculate Haversine distance between two points
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
 * Calculate pool coin value using weighted random (slot machine)
 * Based on the game's pool coin algorithm
 */
function calculatePoolValue(baseValue: number): number {
  const roll = Math.random()
  let multiplier: number
  
  // Weighted distribution:
  // 50% chance: 0.2x - 0.8x (below base)
  // 35% chance: 0.8x - 1.5x (around base)
  // 13% chance: 1.5x - 3.0x (bonus)
  // 2% chance:  3.0x - 5.0x (jackpot!)
  
  if (roll < 0.50) {
    multiplier = 0.2 + Math.random() * 0.6 // 0.2 - 0.8
  } else if (roll < 0.85) {
    multiplier = 0.8 + Math.random() * 0.7 // 0.8 - 1.5
  } else if (roll < 0.98) {
    multiplier = 1.5 + Math.random() * 1.5 // 1.5 - 3.0
  } else {
    multiplier = 3.0 + Math.random() * 2.0 // 3.0 - 5.0
  }
  
  // Round to cents
  return Math.round(baseValue * multiplier * 100) / 100
}

export async function POST(
  request: NextRequest,
  { params }: RouteParams
) {
  try {
    const { id: coinId } = await params
    
    if (!coinId) {
      return NextResponse.json(
        { success: false, error: 'Coin ID required', code: 'MISSING_ID' },
        { status: 400 }
      )
    }
    
    // Parse request body
    let body: CollectRequest = {}
    try {
      body = await request.json()
    } catch {
      // Body is optional for simple collection
    }
    
    const supabase = await createClient()
    
    // Fetch the coin
    const { data: coin, error: fetchError } = await supabase
      .from('coins')
      .select('*')
      .eq('id', coinId)
      .single()
    
    if (fetchError || !coin) {
      return NextResponse.json(
        { success: false, error: 'Coin not found', code: 'NOT_FOUND' },
        { status: 404 }
      )
    }
    
    // Check if already collected
    if (coin.status === 'collected') {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Coin has already been collected',
          code: 'ALREADY_COLLECTED'
        },
        { status: 400 }
      )
    }
    
    // Check if expired
    if (coin.status === 'expired' || coin.status === 'recycled') {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Coin is no longer available',
          code: 'COIN_EXPIRED'
        },
        { status: 400 }
      )
    }
    
    // Validate distance if location provided
    if (body.latitude !== undefined && body.longitude !== undefined) {
      const distance = haversineDistance(
        body.latitude, body.longitude,
        coin.latitude, coin.longitude
      )
      
      if (distance > COLLECTION_RANGE_METERS) {
        return NextResponse.json(
          { 
            success: false, 
            error: `Too far from coin. You are ${Math.round(distance)}m away, need to be within ${COLLECTION_RANGE_METERS}m`,
            code: 'TOO_FAR',
            distance: Math.round(distance),
            required: COLLECTION_RANGE_METERS
          },
          { status: 400 }
        )
      }
    }
    
    // Calculate final value
    let finalValue: number
    if (coin.coin_type === 'pool') {
      // Pool coins use slot machine algorithm
      finalValue = calculatePoolValue(coin.value)
    } else {
      // Fixed coins have exact value
      finalValue = coin.value
    }
    
    // Update coin status to collected
    const now = new Date().toISOString()
    const { error: updateError } = await supabase
      .from('coins')
      .update({
        status: 'collected',
        collected_at: now,
        collected_by: body.userId || null,
        // For multi-find coins, decrement finds_remaining
        finds_remaining: coin.multi_find 
          ? Math.max(0, (coin.finds_remaining || 1) - 1)
          : 0,
      })
      .eq('id', coinId)
    
    if (updateError) {
      console.error('[API] Error updating coin:', updateError)
      return NextResponse.json(
        { success: false, error: 'Failed to collect coin', code: 'UPDATE_FAILED' },
        { status: 500 }
      )
    }
    
    // Log the collection
    console.log(`[API] Coin collected: ${coinId}, value: $${finalValue.toFixed(2)}`)
    
    // Generate congratulation message
    const messages = [
      "Nice find, partner! 🤠",
      "Ye struck gold! 💰",
      "The treasure's yours! 🎉",
      "Black Bart would be proud! 🏆",
      "Another coin for the collection! 🪙",
    ]
    const message = messages[Math.floor(Math.random() * messages.length)]
    
    // Convert to camelCase for Unity compatibility
    const updatedCoin = keysToCamelCase({
      ...coin,
      status: 'collected',
      collected_at: now,
      collected_by: body.userId || null,
    })
    
    return NextResponse.json({
      success: true,
      coin: updatedCoin,
      value: finalValue,
      originalValue: coin.value,
      wasPoolCoin: coin.coin_type === 'pool',
      message,
    })
    
  } catch (error) {
    console.error('[API] Error in POST /coins/[id]/collect:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}
