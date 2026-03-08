/**
 * PATCH /api/v1/admin/dashboard/zones/[zoneId]/auto-spawn
 *
 * Persists per-zone auto-spawn settings for the dashboard without exposing
 * service-role credentials to the browser.
 *
 * @file admin-dashboard/src/app/api/v1/admin/dashboard/zones/[zoneId]/auto-spawn/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { requireAdminSession } from '@/lib/admin-session'
import { describeError } from '@/lib/ai/error-message'

export const dynamic = 'force-dynamic'

function errorResponse(error: unknown) {
  const message = describeError(error)
  if (message === 'UNAUTHORIZED') {
    return NextResponse.json({ success: false, error: 'Unauthorized' }, { status: 401 })
  }
  if (message === 'FORBIDDEN') {
    return NextResponse.json({ success: false, error: 'Admin role required' }, { status: 403 })
  }

  return NextResponse.json({ success: false, error: message }, { status: 500 })
}

export async function PATCH(
  request: NextRequest,
  context: { params: Promise<{ zoneId: string }> }
) {
  try {
    await requireAdminSession()
    const { zoneId } = await context.params

    let body: { enabled?: boolean; min_coins?: number; max_coins?: number }
    try {
      body = await request.json()
    } catch {
      return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
    }

    const supabase = createServiceRoleClient()
    const { data: zone, error: zoneError } = await supabase
      .from('zones')
      .select('id, auto_spawn_config')
      .eq('id', zoneId)
      .single()

    if (zoneError || !zone) {
      return NextResponse.json({ success: false, error: 'Zone not found' }, { status: 404 })
    }

    const existingConfig = ((zone.auto_spawn_config ?? {}) as Record<string, unknown>)
    const nextConfig = {
      ...existingConfig,
      ...(typeof body.enabled === 'boolean' ? { enabled: body.enabled } : {}),
      ...(typeof body.min_coins === 'number' ? { min_coins: body.min_coins } : {}),
      ...(typeof body.max_coins === 'number' ? { max_coins: body.max_coins } : {}),
    }

    const { error } = await supabase
      .from('zones')
      .update({ auto_spawn_config: nextConfig })
      .eq('id', zoneId)

    if (error) throw error

    return NextResponse.json({
      success: true,
      data: {
        zone_id: zoneId,
        auto_spawn_config: nextConfig,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    return errorResponse(error)
  }
}
