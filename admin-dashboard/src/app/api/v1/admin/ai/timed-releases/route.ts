/**
 * GET/POST /api/v1/admin/ai/timed-releases
 *
 * AI-facing timed release management. Lists schedules with derived queue previews
 * and creates new schedules with optional S2 cell targeting metadata.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/timed-releases/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { isAuthorizedRequest, isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import { AI_AGENT_IDS, AI_ERROR_CODES, type AiAgentId } from '@/lib/ai-guardrails'
import { describeError } from '@/lib/ai/error-message'
import { buildReleaseQueuePreview, getDefaultValueRange, resolveSpatialTarget, type ActiveZoneOverlayRow } from '@/lib/ai/spatial-targets'
import type { CoinTier } from '@/types/database'

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

async function getActiveZones() {
  const supabase = createServiceRoleClient()
  const { data, error } = await supabase
    .from('zones')
    .select('id, name, zone_type, geometry')
    .eq('status', 'active')

  if (error) throw error
  return (data ?? []) as ActiveZoneOverlayRow[]
}

export async function GET(request: NextRequest) {
  if (!await isAuthorizedRequest(request)) return unauthorizedResponse()

  try {
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
      zone_name: zoneMap.get(row.zone_id) ?? 'Unknown zone',
      batches_total: Math.ceil(row.total_coins / row.coins_per_release),
    }))

    const queue = schedules
      .map((schedule) => buildReleaseQueuePreview(schedule))
      .filter(Boolean)
      .sort((a, b) => a.time_until_seconds - b.time_until_seconds)

    const dueNow = queue.filter((item) => item.time_until_seconds <= 0).length

    return NextResponse.json({
      success: true,
      data: {
        schedules,
        queue,
        summary: {
          active_schedules: schedules.filter((schedule) => schedule.status === 'active').length,
          scheduled_schedules: schedules.filter((schedule) => schedule.status === 'scheduled').length,
          due_now: dueNow,
          cell_backed_schedules: schedules.filter((schedule) => Boolean(schedule.s2_cell_token_l17)).length,
        },
      },
      meta: {
        recommended_action: dueNow > 0 ? 'process_timed_releases' : 'no_action_needed',
      },
      _links: {
        create: '/api/v1/admin/ai/timed-releases',
        process: '/api/v1/admin/ai/process-timed-releases',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    return NextResponse.json(
      {
        success: false,
        error: 'Internal server error',
        details: describeError(error),
      },
      { status: 500 }
    )
  }
}

export async function POST(request: NextRequest) {
  if (!isValidAiApiKey(request)) return unauthorizedResponse()

  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  const {
    zone_id,
    cell_id,
    name,
    description,
    total_coins,
    coins_per_release,
    release_interval_seconds,
    start_time,
    end_time,
    tier,
    min_value,
    max_value,
    target_latitude,
    target_longitude,
    agent_id,
    reasoning,
    idempotency_key,
  } = body

  if (!zone_id || typeof zone_id !== 'string') {
    return NextResponse.json({ success: false, error: 'zone_id is required' }, { status: 400 })
  }
  if (!name || typeof name !== 'string' || name.length < 3) {
    return NextResponse.json({ success: false, error: 'name must be at least 3 characters' }, { status: 400 })
  }
  if (!agent_id || !(AI_AGENT_IDS as readonly string[]).includes(agent_id as string)) {
    return NextResponse.json({ success: false, error: `agent_id must be one of: ${AI_AGENT_IDS.join(', ')}` }, { status: 400 })
  }
  if (!reasoning || typeof reasoning !== 'string' || reasoning.length < 5) {
    return NextResponse.json({ success: false, error: 'reasoning must be at least 5 characters' }, { status: 400 })
  }

  const totalCoins = typeof total_coins === 'number' ? total_coins : 0
  const coinsPerRelease = typeof coins_per_release === 'number' ? coins_per_release : 0
  const releaseIntervalSeconds = typeof release_interval_seconds === 'number' ? release_interval_seconds : 0

  if (totalCoins <= 0 || coinsPerRelease <= 0 || releaseIntervalSeconds < 10) {
    return NextResponse.json(
      { success: false, error: 'total_coins, coins_per_release, and release_interval_seconds must be valid positive values' },
      { status: 400 }
    )
  }

  const typedTier = (tier && ['gold', 'silver', 'bronze'].includes(tier as string) ? tier : 'bronze') as CoinTier
  const typedAgentId = agent_id as AiAgentId
  const defaults = getDefaultValueRange(typedTier)
  const minValue = typeof min_value === 'number' ? min_value : defaults.min
  const maxValue = typeof max_value === 'number' ? max_value : defaults.max

  try {
    const supabase = createServiceRoleClient()
    const { data: config } = await supabase
      .from('distribution_config')
      .select('enabled')
      .eq('id', '00000000-0000-0000-0000-000000000001')
      .single()

    if (!(config?.enabled ?? true)) {
      return NextResponse.json(
        {
          success: false,
          error: 'Auto-distribution is disabled',
          code: AI_ERROR_CODES.DISTRIBUTION_DISABLED,
          meta: { kill_switch_active: true },
        },
        { status: 503 }
      )
    }

    if (idempotency_key && typeof idempotency_key === 'string') {
      const { data: existing } = await supabase
        .from('ai_actions')
        .select('id, result, success')
        .eq('tool_called', 'schedule_timed_release')
        .contains('parameters', { idempotency_key })
        .limit(1)
        .maybeSingle()

      if (existing) {
        const cachedResult = existing.result as Record<string, unknown> ?? {}
        return NextResponse.json(
          { ...cachedResult, _idempotent: true, ai_action_id: existing.id },
          { status: existing.success ? 200 : 422 }
        )
      }
    }

    const spatialTarget = resolveSpatialTarget({
      zoneId: zone_id,
      cellId: typeof cell_id === 'string' ? cell_id : null,
      latitude: typeof target_latitude === 'number' ? target_latitude : null,
      longitude: typeof target_longitude === 'number' ? target_longitude : null,
      activeZones: await getActiveZones(),
    })

    const startTimeIso = typeof start_time === 'string' ? start_time : new Date().toISOString()
    const { data: scheduleRow, error: scheduleError } = await supabase
      .from('release_schedules')
      .insert({
        zone_id: spatialTarget.zoneId,
        name,
        description: typeof description === 'string' ? description : null,
        total_coins: totalCoins,
        coins_per_release: coinsPerRelease,
        release_interval_seconds: releaseIntervalSeconds,
        start_time: startTimeIso,
        end_time: typeof end_time === 'string' ? end_time : null,
        status: 'scheduled',
        coins_released_so_far: 0,
        batches_completed: 0,
        next_release_at: startTimeIso,
        s2_cell_token_l17: spatialTarget.cellId,
        s2_cell_token_l14: spatialTarget.parentCellId,
        coin_tier: typedTier,
        min_value: minValue,
        max_value: Math.max(minValue, maxValue),
      })
      .select('id, zone_id, name, next_release_at, s2_cell_token_l17, coin_tier')
      .single()

    if (scheduleError || !scheduleRow) throw scheduleError ?? new Error('Schedule insert failed')

    const resultPayload = {
      schedule_id: scheduleRow.id,
      zone_id: scheduleRow.zone_id,
      zone_name: spatialTarget.zoneName,
      cell_id: scheduleRow.s2_cell_token_l17,
      next_release_at: scheduleRow.next_release_at,
      coin_tier: scheduleRow.coin_tier,
      batches_total: Math.ceil(totalCoins / coinsPerRelease),
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'schedule_timed_release',
        parameters: {
          zone_id,
          cell_id: spatialTarget.cellId,
          name,
          total_coins: totalCoins,
          coins_per_release: coinsPerRelease,
          release_interval_seconds: releaseIntervalSeconds,
          start_time: startTimeIso,
          idempotency_key: idempotency_key ?? null,
        },
        reasoning,
        result: resultPayload,
        success: true,
        error_code: null,
        cost_usd: 0,
      })
      .select('id')
      .single()

    return NextResponse.json({
      success: true,
      data: {
        ...resultPayload,
        ai_action_id: actionRow?.id ?? null,
      },
      meta: {
        recommended_action: 'process_timed_releases',
      },
      _links: {
        schedules: '/api/v1/admin/ai/timed-releases',
        process: '/api/v1/admin/ai/process-timed-releases',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    const message = describeError(error)
    const code = message.startsWith('Invalid L17 cell token')
      ? AI_ERROR_CODES.INVALID_CELL_ID
      : message.startsWith('Zone not found:')
        ? AI_ERROR_CODES.ZONE_NOT_FOUND
        : AI_ERROR_CODES.SPAWN_FAILED

    return NextResponse.json(
      {
        success: false,
        error: message,
        code,
      },
      { status: code === AI_ERROR_CODES.SPAWN_FAILED ? 500 : 400 }
    )
  }
}
