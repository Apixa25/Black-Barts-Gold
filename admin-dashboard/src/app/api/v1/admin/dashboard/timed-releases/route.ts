/**
 * GET/POST/PATCH /api/v1/admin/dashboard/timed-releases
 *
 * Human-admin companion route for the timed-release dashboard. Reads live
 * schedule state and forwards privileged write actions through server-side
 * guardrails instead of exposing the AI API key in the browser.
 *
 * @file admin-dashboard/src/app/api/v1/admin/dashboard/timed-releases/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { requireAdminSession } from '@/lib/admin-session'
import { buildReleaseQueuePreview } from '@/lib/ai/spatial-targets'
import { describeError } from '@/lib/ai/error-message'
import type { CoinTier, ZoneGeometry } from '@/types/database'

export const dynamic = 'force-dynamic'

interface ReleaseScheduleRow {
  id: string
  zone_id: string
  name: string
  description: string | null
  total_coins: number
  coins_per_release: number
  release_interval_seconds: number
  start_time: string
  end_time: string | null
  status: 'scheduled' | 'active' | 'paused' | 'completed' | 'cancelled'
  coins_released_so_far: number
  batches_completed: number
  next_release_at: string | null
  last_release_at: string | null
  created_at: string
  updated_at: string
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
  coin_tier: CoinTier
  min_value: number | null
  max_value: number | null
}

function errorResponse(error: unknown) {
  const message = describeError(error)
  if (message === 'UNAUTHORIZED') {
    return NextResponse.json({ success: false, error: 'Unauthorized' }, { status: 401 })
  }
  if (message === 'FORBIDDEN') {
    return NextResponse.json({ success: false, error: 'Admin role required' }, { status: 403 })
  }

  return NextResponse.json(
    {
      success: false,
      error: message,
    },
    { status: 500 }
  )
}

function getZoneFocusPoint(geometry: ZoneGeometry): { latitude: number; longitude: number } | null {
  if (geometry.type === 'circle' && geometry.center) {
    return geometry.center
  }

  if (geometry.type === 'polygon' && geometry.polygon && geometry.polygon.length > 0) {
    const totals = geometry.polygon.reduce(
      (acc, point) => ({
        latitude: acc.latitude + point.latitude,
        longitude: acc.longitude + point.longitude,
      }),
      { latitude: 0, longitude: 0 }
    )

    return {
      latitude: totals.latitude / geometry.polygon.length,
      longitude: totals.longitude / geometry.polygon.length,
    }
  }

  return null
}

export async function GET(request: NextRequest) {
  try {
    await requireAdminSession()

    const params = request.nextUrl.searchParams
    const status = params.get('status')
    const zoneId = params.get('zone_id')
    const limit = Math.min(Math.max(parseInt(params.get('limit') ?? '100', 10), 1), 200)

    const supabase = createServiceRoleClient()
    let query = supabase
      .from('release_schedules')
      .select('id, zone_id, name, description, total_coins, coins_per_release, release_interval_seconds, start_time, end_time, status, coins_released_so_far, batches_completed, next_release_at, last_release_at, created_at, updated_at, s2_cell_token_l17, s2_cell_token_l14, coin_tier, min_value, max_value')
      .order('start_time', { ascending: true })
      .limit(limit)

    if (status && ['scheduled', 'active', 'paused', 'completed', 'cancelled'].includes(status)) {
      query = query.eq('status', status)
    }
    if (zoneId) {
      query = query.eq('zone_id', zoneId)
    }

    const { data: schedulesData, error } = await query
    if (error) throw error

    const scheduleRows = (schedulesData ?? []) as ReleaseScheduleRow[]
    const zoneIds = [...new Set(scheduleRows.map((row) => row.zone_id))]
    const { data: zoneRows } = zoneIds.length > 0
      ? await supabase.from('zones').select('id, name').in('id', zoneIds)
      : { data: [] as Array<{ id: string; name: string }> }
    const zoneMap = new Map((zoneRows ?? []).map((zone) => [zone.id, zone.name]))

    const schedules = scheduleRows.map((row) => ({
      ...row,
      cell_id: row.s2_cell_token_l17,
      zone_name: zoneMap.get(row.zone_id) ?? 'Unknown zone',
      batches_total: Math.ceil(row.total_coins / row.coins_per_release),
    }))

    const queue = schedules
      .map((schedule) => buildReleaseQueuePreview(schedule))
      .filter((item): item is NonNullable<ReturnType<typeof buildReleaseQueuePreview>> => item !== null)
      .sort((a, b) => a.time_until_seconds - b.time_until_seconds)

    const dueNow = queue.filter((item) => item.time_until_seconds <= 0).length
    const startOfDayIso = new Date(new Date().setHours(0, 0, 0, 0)).toISOString()
    const { data: todayHistory } = await supabase
      .from('spawn_history')
      .select('coin_value, spawned_at')
      .gte('spawned_at', startOfDayIso)

    return NextResponse.json({
      success: true,
      data: {
        schedules,
        queue,
        summary: {
          active_schedules: schedules.filter((schedule) => schedule.status === 'active').length,
          scheduled_schedules: schedules.filter((schedule) => schedule.status === 'scheduled').length,
          completed_today: schedules.filter(
            (schedule) =>
              schedule.status === 'completed' &&
              new Date(schedule.updated_at).getTime() >= new Date(startOfDayIso).getTime()
          ).length,
          total_coins_released_today: todayHistory?.length ?? 0,
          total_value_released_today: Number(
            ((todayHistory ?? []).reduce((sum, row) => sum + Number(row.coin_value ?? 0), 0)).toFixed(2)
          ),
          due_now: dueNow,
        },
      },
      meta: {
        recommended_action: dueNow > 0 ? 'process_timed_releases' : 'no_action_needed',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    return errorResponse(error)
  }
}

export async function POST(request: NextRequest) {
  try {
    await requireAdminSession()

    let body: Record<string, unknown>
    try {
      body = await request.json()
    } catch {
      return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
    }

    const { zoneId, name, totalCoins, coinsPerRelease, releaseIntervalSeconds, startTime, endTime } = body

    if (!zoneId || typeof zoneId !== 'string') {
      return NextResponse.json({ success: false, error: 'zoneId is required' }, { status: 400 })
    }
    if (!name || typeof name !== 'string') {
      return NextResponse.json({ success: false, error: 'name is required' }, { status: 400 })
    }

    const supabase = createServiceRoleClient()
    const { data: zone, error: zoneError } = await supabase
      .from('zones')
      .select('id, name, geometry, zone_type')
      .eq('id', zoneId)
      .single()

    if (zoneError || !zone) {
      return NextResponse.json({ success: false, error: 'Zone not found' }, { status: 404 })
    }

    const focusPoint = getZoneFocusPoint(zone.geometry as ZoneGeometry)
    const aiPayload = {
      zone_id: zoneId,
      name,
      description: `Dashboard-created timed release for ${zone.name}`,
      total_coins: totalCoins,
      coins_per_release: coinsPerRelease,
      release_interval_seconds: releaseIntervalSeconds,
      start_time: startTime,
      end_time: endTime ?? null,
      tier: 'bronze',
      ...(focusPoint
        ? {
            target_latitude: focusPoint.latitude,
            target_longitude: focusPoint.longitude,
          }
        : {}),
      agent_id: 'ai_game_master',
      reasoning: `Human admin created timed release from dashboard for zone ${zone.name}`,
    }

    const headers: HeadersInit = { 'Content-Type': 'application/json' }
    if (process.env.AI_AGENT_API_KEY) {
      headers.Authorization = `Bearer ${process.env.AI_AGENT_API_KEY}`
    }

    const response = await fetch(`${request.nextUrl.origin}/api/v1/admin/ai/timed-releases`, {
      method: 'POST',
      headers,
      body: JSON.stringify(aiPayload),
      cache: 'no-store',
    })

    const payload = await response.json()
    return NextResponse.json(payload, { status: response.status })
  } catch (error) {
    return errorResponse(error)
  }
}

export async function PATCH(request: NextRequest) {
  try {
    await requireAdminSession()

    let body: Record<string, unknown>
    try {
      body = await request.json()
    } catch {
      return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
    }

    const { scheduleId, action } = body
    if (!scheduleId || typeof scheduleId !== 'string') {
      return NextResponse.json({ success: false, error: 'scheduleId is required' }, { status: 400 })
    }
    if (!action || !['pause', 'resume', 'cancel'].includes(action as string)) {
      return NextResponse.json({ success: false, error: 'action must be pause | resume | cancel' }, { status: 400 })
    }

    const supabase = createServiceRoleClient()
    const { data: existing, error: existingError } = await supabase
      .from('release_schedules')
      .select('id, total_coins, coins_released_so_far, status')
      .eq('id', scheduleId)
      .single()

    if (existingError || !existing) {
      return NextResponse.json({ success: false, error: 'Schedule not found' }, { status: 404 })
    }

    const remainingCoins = existing.total_coins - existing.coins_released_so_far
    const updates =
      action === 'pause'
        ? { status: 'paused', updated_at: new Date().toISOString() }
        : action === 'cancel'
          ? { status: 'cancelled', next_release_at: null, updated_at: new Date().toISOString() }
          : {
              status: remainingCoins > 0 ? 'active' : 'completed',
              next_release_at: remainingCoins > 0 ? new Date().toISOString() : null,
              updated_at: new Date().toISOString(),
            }

    const { data, error } = await supabase
      .from('release_schedules')
      .update(updates)
      .eq('id', scheduleId)
      .select('id, status, next_release_at')
      .single()

    if (error || !data) throw error ?? new Error('Schedule update failed')

    return NextResponse.json({
      success: true,
      data,
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    return errorResponse(error)
  }
}
