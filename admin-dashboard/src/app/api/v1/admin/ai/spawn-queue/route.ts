/**
 * GET/POST /api/v1/admin/ai/spawn-queue
 *
 * AI-facing queue inspection and queueing endpoint. Lets the AI inspect pending
 * queue items and enqueue future spawns with canonical S2 cell metadata.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/spawn-queue/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { isAuthorizedRequest, isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import {
  AI_AGENT_IDS,
  AI_ERROR_CODES,
  type AiAgentId,
} from '@/lib/ai-guardrails'
import { describeError } from '@/lib/ai/error-message'
import { getDefaultValueRange, resolveSpatialTarget, type ActiveZoneOverlayRow } from '@/lib/ai/spatial-targets'
import type { CoinTier } from '@/types/database'

export const dynamic = 'force-dynamic'

interface QueueRow {
  id: string
  zone_id: string
  trigger_type: string
  scheduled_time: string
  coin_type: string
  tier: CoinTier
  min_value: number
  max_value: number
  is_mythical: boolean
  target_latitude: number | null
  target_longitude: number | null
  status: 'pending' | 'processing' | 'completed' | 'failed'
  error_message: string | null
  spawned_coin_id: string | null
  created_at: string
  processed_at: string | null
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
}

function toQueueTriggerType(agentId: AiAgentId): string {
  if (agentId === 'ai_spawn_governor' || agentId === 'ai_game_master') return agentId
  return 'manual'
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
    const dueOnly = params.get('due_only') === 'true'
    const limit = Math.min(Math.max(parseInt(params.get('limit') ?? '50', 10), 1), 200)

    const supabase = createServiceRoleClient()
    let query = supabase
      .from('spawn_queue')
      .select('id, zone_id, trigger_type, scheduled_time, coin_type, tier, min_value, max_value, is_mythical, target_latitude, target_longitude, status, error_message, spawned_coin_id, created_at, processed_at, s2_cell_token_l17, s2_cell_token_l14')
      .order('scheduled_time', { ascending: true })
      .limit(limit)

    if (status && ['pending', 'processing', 'completed', 'failed'].includes(status)) {
      query = query.eq('status', status)
    }
    if (dueOnly) {
      query = query.lte('scheduled_time', new Date().toISOString())
    }

    const { data: rows, error } = await query
    if (error) throw error

    const queueRows = (rows ?? []) as QueueRow[]
    const zoneIds = [...new Set(queueRows.map((row) => row.zone_id))]
    const { data: zoneRows } = zoneIds.length > 0
      ? await supabase.from('zones').select('id, name').in('id', zoneIds)
      : { data: [] as Array<{ id: string; name: string }> }
    const zoneMap = new Map((zoneRows ?? []).map((zone) => [zone.id, zone.name]))

    const now = Date.now()
    const queue = queueRows.map((row) => ({
      id: row.id,
      zone_id: row.zone_id,
      zone_name: zoneMap.get(row.zone_id) ?? 'Unknown zone',
      cell_id: row.s2_cell_token_l17,
      trigger_type: row.trigger_type,
      scheduled_time: row.scheduled_time,
      coin_config: {
        coin_type: row.coin_type,
        tier: row.tier,
        min_value: row.min_value,
        max_value: row.max_value,
        is_mythical: row.is_mythical,
      },
      target_location: row.target_latitude !== null && row.target_longitude !== null
        ? { latitude: row.target_latitude, longitude: row.target_longitude }
        : null,
      status: row.status,
      error_message: row.error_message,
      spawned_coin_id: row.spawned_coin_id,
      created_at: row.created_at,
      processed_at: row.processed_at,
      time_until_seconds: Math.max(0, Math.floor((new Date(row.scheduled_time).getTime() - now) / 1000)),
    }))

    const dueNow = queueRows.filter((row) => row.status === 'pending' && new Date(row.scheduled_time).getTime() <= now).length

    return NextResponse.json({
      success: true,
      data: {
        queue,
        summary: {
          total_items: queueRows.length,
          pending_items: queueRows.filter((row) => row.status === 'pending').length,
          due_now: dueNow,
          queued_cells: [...new Set(queueRows.map((row) => row.s2_cell_token_l17).filter(Boolean))].length,
        },
      },
      meta: {
        recommended_action: dueNow > 0 ? 'process_spawn_queue' : 'no_action_needed',
      },
      _links: {
        enqueue: '/api/v1/admin/ai/spawn-queue',
        process: '/api/v1/admin/ai/process-spawn-queue',
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
    scheduled_time,
    tier,
    min_value,
    max_value,
    is_mythical,
    target_latitude,
    target_longitude,
    agent_id,
    reasoning,
    idempotency_key,
  } = body

  if (!zone_id || typeof zone_id !== 'string') {
    return NextResponse.json({ success: false, error: 'zone_id is required' }, { status: 400 })
  }
  if (!agent_id || !(AI_AGENT_IDS as readonly string[]).includes(agent_id as string)) {
    return NextResponse.json({ success: false, error: `agent_id must be one of: ${AI_AGENT_IDS.join(', ')}` }, { status: 400 })
  }
  if (!reasoning || typeof reasoning !== 'string' || reasoning.length < 5) {
    return NextResponse.json({ success: false, error: 'reasoning must be at least 5 characters' }, { status: 400 })
  }

  const typedTier = (tier && ['gold', 'silver', 'bronze'].includes(tier as string) ? tier : 'bronze') as CoinTier
  const typedAgentId = agent_id as AiAgentId
  const defaultRange = getDefaultValueRange(typedTier)
  const requestedMinValue = typeof min_value === 'number' ? min_value : defaultRange.min
  const requestedMaxValue = typeof max_value === 'number' ? max_value : defaultRange.max

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
        .eq('tool_called', 'queue_spawn')
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

    const queuePayload = {
      zone_id: spatialTarget.zoneId,
      trigger_type: toQueueTriggerType(typedAgentId),
      scheduled_time: typeof scheduled_time === 'string' ? scheduled_time : new Date().toISOString(),
      coin_type: 'fixed',
      tier: typedTier,
      min_value: requestedMinValue,
      max_value: Math.max(requestedMinValue, requestedMaxValue),
      is_mythical: typeof is_mythical === 'boolean' ? is_mythical : false,
      target_latitude: spatialTarget.targetLatitude,
      target_longitude: spatialTarget.targetLongitude,
      status: 'pending' as const,
      s2_cell_token_l17: spatialTarget.cellId,
      s2_cell_token_l14: spatialTarget.parentCellId,
    }

    const { data: queuedItem, error: queueError } = await supabase
      .from('spawn_queue')
      .insert(queuePayload)
      .select('id, scheduled_time, zone_id, s2_cell_token_l17, s2_cell_token_l14, status')
      .single()

    if (queueError || !queuedItem) throw queueError ?? new Error('Queue insert failed')

    const resultPayload = {
      queue_id: queuedItem.id,
      zone_id: queuedItem.zone_id,
      zone_name: spatialTarget.zoneName,
      cell_id: queuedItem.s2_cell_token_l17,
      scheduled_time: queuedItem.scheduled_time,
      status: queuedItem.status,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'queue_spawn',
        parameters: {
          zone_id,
          cell_id: spatialTarget.cellId,
          agent_id,
          reasoning,
          scheduled_time: queuePayload.scheduled_time,
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
        recommended_action: 'process_spawn_queue',
      },
      _links: {
        queue: '/api/v1/admin/ai/spawn-queue',
        process: '/api/v1/admin/ai/process-spawn-queue',
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
