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
import { createServiceRoleClient } from '@/lib/supabase/server'
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
    
    // Use service role so we can update/delete coins when mobile app collects (no cookie auth)
    const supabase = createServiceRoleClient()
    
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
    
    // Update coin status
    const now = new Date().toISOString()
    
    // Calculate new finds_remaining for multi-find coins
    const newFindsRemaining = coin.multi_find 
      ? Math.max(0, (coin.finds_remaining || 1) - 1)
      : 0
    
    // Only set status to 'collected' if:
    // - It's not a multi-find coin, OR
    // - It IS a multi-find coin but no finds remaining
    const fullyConsumed = !(coin.multi_find && newFindsRemaining > 0)
    const newStatus = fullyConsumed ? 'collected' : 'visible'
    
    if (fullyConsumed) {
      // Remove coin from database when fully consumed (one-time find or last find of multi-find)
      const { error: deleteError } = await supabase
        .from('coins')
        .delete()
        .eq('id', coinId)
      
      if (deleteError) {
        console.error('[API] Error deleting coin after collection:', deleteError)
        // Fallback: mark as collected instead of deleting
        const { error: updateError } = await supabase
          .from('coins')
          .update({
            status: 'collected',
            collected_at: now,
            collected_by: body.userId || null,
            finds_remaining: 0,
          })
          .eq('id', coinId)
        if (updateError) {
          return NextResponse.json(
            { success: false, error: 'Failed to collect coin', code: 'UPDATE_FAILED' },
            { status: 500 }
          )
        }
      } else {
        console.log(`[API] Coin removed after collection: ${coinId}, value: $${finalValue.toFixed(2)}, multiFind: ${coin.multi_find}`)
      }
    } else {
      // Multi-find coin with finds still remaining - just update
      const { error: updateError } = await supabase
        .from('coins')
        .update({
          status: newStatus,
          collected_at: now,
          collected_by: body.userId || null,
          finds_remaining: newFindsRemaining,
        })
        .eq('id', coinId)
      
      if (updateError) {
        console.error('[API] Error updating coin:', updateError)
        return NextResponse.json(
          { success: false, error: 'Failed to collect coin', code: 'UPDATE_FAILED' },
          { status: 500 }
        )
      }
      console.log(`[API] Coin collected (multi-find): ${coinId}, value: $${finalValue.toFixed(2)}, remaining: ${newFindsRemaining}`)
    }
    
    // Create a transaction record for tracking (if user provided)
    if (body.userId) {
      try {
        const { error: txError } = await supabase
          .from('transactions')
          .insert({
            user_id: body.userId,
            transaction_type: 'found',
            amount: finalValue,
            balance_after: 0, // Will be updated by trigger or app
            coin_id: coinId,
            description: `Found ${coin.multi_find ? 'multi-find ' : ''}coin: $${finalValue.toFixed(2)}`,
            status: 'confirmed',
            metadata: {
              coin_type: coin.coin_type,
              original_value: coin.value,
              tier: coin.tier,
              multi_find: coin.multi_find,
              finds_remaining: newFindsRemaining,
              location: { lat: coin.latitude, lng: coin.longitude },
            },
          })
        
        if (txError) {
          console.error('[API] Failed to create transaction record:', txError)
          // Don't fail the collection if transaction logging fails
        } else {
          console.log(`[API] Transaction recorded for user ${body.userId}`)
        }
      } catch (txEx) {
        console.error('[API] Exception creating transaction:', txEx)
      }
    }
    
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
      status: newStatus,
      collected_at: now,
      collected_by: body.userId || null,
      finds_remaining: newFindsRemaining,
    })
    
    return NextResponse.json({
      success: true,
      coin: updatedCoin,
      value: finalValue,
      originalValue: coin.value,
      wasPoolCoin: coin.coin_type === 'pool',
      message,
      // Multi-find coin info
      isMultiFind: coin.multi_find || false,
      findsRemaining: newFindsRemaining,
      fullyCollected: newStatus === 'collected',
    })
    
  } catch (error) {
    console.error('[API] Error in POST /coins/[id]/collect:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}
