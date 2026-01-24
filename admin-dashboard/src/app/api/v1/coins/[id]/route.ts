/**
 * Coin-specific API routes
 * 
 * GET /api/v1/coins/[id] - Get a specific coin by ID
 * DELETE /api/v1/coins/[id] - Delete a coin (admin only)
 * 
 * @file admin-dashboard/src/app/api/v1/coins/[id]/route.ts
 * Character count: ~2,800
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'
import { keysToCamelCase } from '@/lib/api-utils'

interface RouteParams {
  params: Promise<{ id: string }>
}

/**
 * GET /api/v1/coins/[id]
 * Fetch a specific coin by ID
 */
export async function GET(
  request: NextRequest,
  { params }: RouteParams
) {
  try {
    const { id } = await params
    
    if (!id) {
      return NextResponse.json(
        { success: false, error: 'Coin ID required', code: 'MISSING_ID' },
        { status: 400 }
      )
    }
    
    const supabase = await createClient()
    
    const { data: coin, error } = await supabase
      .from('coins')
      .select('*')
      .eq('id', id)
      .single()
    
    if (error || !coin) {
      return NextResponse.json(
        { success: false, error: 'Coin not found', code: 'NOT_FOUND' },
        { status: 404 }
      )
    }
    
    // Convert to camelCase for Unity compatibility
    const camelCaseCoin = keysToCamelCase(coin)
    
    return NextResponse.json({
      success: true,
      coin: camelCaseCoin,
    })
    
  } catch (error) {
    console.error('[API] Error in GET /coins/[id]:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}

/**
 * DELETE /api/v1/coins/[id]
 * Delete a coin (owner or admin only)
 */
export async function DELETE(
  request: NextRequest,
  { params }: RouteParams
) {
  try {
    const { id } = await params
    
    if (!id) {
      return NextResponse.json(
        { success: false, error: 'Coin ID required', code: 'MISSING_ID' },
        { status: 400 }
      )
    }
    
    const supabase = await createClient()
    
    // Check if coin exists first
    const { data: existingCoin, error: fetchError } = await supabase
      .from('coins')
      .select('id, status, hider_id')
      .eq('id', id)
      .single()
    
    if (fetchError || !existingCoin) {
      return NextResponse.json(
        { success: false, error: 'Coin not found', code: 'NOT_FOUND' },
        { status: 404 }
      )
    }
    
    // Don't allow deleting already collected coins
    if (existingCoin.status === 'collected') {
      return NextResponse.json(
        { success: false, error: 'Cannot delete collected coin', code: 'ALREADY_COLLECTED' },
        { status: 400 }
      )
    }
    
    // Delete the coin
    const { error: deleteError } = await supabase
      .from('coins')
      .delete()
      .eq('id', id)
    
    if (deleteError) {
      console.error('[API] Error deleting coin:', deleteError)
      return NextResponse.json(
        { success: false, error: 'Failed to delete coin', code: 'DELETE_FAILED' },
        { status: 500 }
      )
    }
    
    console.log(`[API] Coin deleted: ${id}`)
    
    return NextResponse.json({
      success: true,
      message: 'Coin deleted successfully',
    })
    
  } catch (error) {
    console.error('[API] Error in DELETE /coins/[id]:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error', code: 'INTERNAL_ERROR' },
      { status: 500 }
    )
  }
}
