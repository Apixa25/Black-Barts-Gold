/**
 * POST /api/v1/admin/ai/process-timed-releases
 *
 * Processes due timed release schedules by expanding them into spawn queue items.
 * This keeps timed releases aligned with the queue toolchain and allows cell-aware
 * coordinates to be generated for each released coin.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/process-timed-releases/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import { AI_AGENT_IDS, AI_ERROR_CODES, type AiAgentId } from '@/lib/ai-guardrails'
import { describeError } from '@/lib/ai/error-message'
import { getDefaultValueRange, resolveSpatialTarget, type ActiveZoneOverlayRow } from '@/lib/ai/spatial-targets'
import type { CoinTier } from '@/types/database'

export const dynamic = 'force-dynamic'

interface DueScheduleRow {
  id: string
  zone_id: string
  name: string
  total_coins: number
  coins_per_release: number
  release_interval_seconds: number
  status: 'scheduled' | 'active' | 'paused' | 'completed' | 'cancelled'
  coins_released_so_far: number
  batches_completed: number
  next_release_at: string | null
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
  coin_tier: CoinTier
  min_value: number | null
  max_value: number | null
}

function toQueueTriggerType(agentId: AiAgentId): string {
  if (agentId === 'ai_spawn_governor' || agentId === 'ai_game_master') return agentId
  return 'scheduled'
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

export async function POST(request: NextRequest) {
  if (!isValidAiApiKey(request)) return unauthorizedResponse()

  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    body = {}
  }

  const { agent_id, reasoning, limit } = body

  if (!agent_id || !(AI_AGENT_IDS as readonly string[]).includes(agent_id as string)) {
    return NextResponse.json(
      { success: false, error: `agent_id must be one of: ${AI_AGENT_IDS.join(', ')}` },
      { status: 400 }
    )
  }
  if (!reasoning || typeof reasoning !== 'string' || reasoning.length < 5) {
    return NextResponse.json(
      { success: false, error: 'reasoning must be at least 5 characters' },
      { status: 400 }
    )
  }

  const typedAgentId = agent_id as AiAgentId
  const maxSchedules = Math.min(Math.max(typeof limit === 'number' ? limit : 20, 1), 100)

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

    const { data: dueSchedules, error: schedulesError } = await supabase
      .from('release_schedules')
      .select('id, zone_id, name, total_coins, coins_per_release, release_interval_seconds, status, coins_released_so_far, batches_completed, next_release_at, s2_cell_token_l17, s2_cell_token_l14, coin_tier, min_value, max_value')
      .in('status', ['scheduled', 'active'])
      .not('next_release_at', 'is', null)
      .lte('next_release_at', new Date().toISOString())
      .order('next_release_at', { ascending: true })
      .limit(maxSchedules)

    if (schedulesError) throw schedulesError

    const schedules = (dueSchedules ?? []) as DueScheduleRow[]
    if (schedules.length === 0) {
      return NextResponse.json({
        success: true,
        data: {
          schedules_processed: 0,
          coins_queued: 0,
          batches_created: 0,
          queue_item_ids: [],
        },
        meta: {
          recommended_action: 'no_action_needed',
        },
        timestamp: new Date().toISOString(),
      })
    }

    const activeZones = await getActiveZones()
    const nowIso = new Date().toISOString()
    const createdQueueIds: string[] = []
    let schedulesProcessed = 0
    let coinsQueued = 0
    let batchesCreated = 0

    for (const schedule of schedules) {
      const batchCoins = Math.min(
        schedule.coins_per_release,
        Math.max(0, schedule.total_coins - schedule.coins_released_so_far)
      )

      if (batchCoins <= 0) {
        await supabase
          .from('release_schedules')
          .update({
            status: 'completed',
            next_release_at: null,
            updated_at: nowIso,
          })
          .eq('id', schedule.id)
        continue
      }

      const queueItems = []
      for (let i = 0; i < batchCoins; i++) {
        const target = resolveSpatialTarget({
          zoneId: schedule.zone_id,
          cellId: schedule.s2_cell_token_l17,
          activeZones,
        })
        const defaults = getDefaultValueRange(schedule.coin_tier ?? 'bronze')

        queueItems.push({
          zone_id: target.zoneId,
          trigger_type: toQueueTriggerType(typedAgentId),
          scheduled_time: nowIso,
          coin_type: 'fixed',
          tier: schedule.coin_tier ?? 'bronze',
          min_value: schedule.min_value ?? defaults.min,
          max_value: schedule.max_value ?? defaults.max,
          is_mythical: false,
          target_latitude: target.targetLatitude,
          target_longitude: target.targetLongitude,
          status: 'pending' as const,
          s2_cell_token_l17: target.cellId,
          s2_cell_token_l14: target.parentCellId,
        })
      }

      const { data: insertedQueueItems, error: insertQueueError } = await supabase
        .from('spawn_queue')
        .insert(queueItems)
        .select('id')

      if (insertQueueError) throw insertQueueError

      createdQueueIds.push(...(insertedQueueItems ?? []).map((item) => item.id))

      const remaining = schedule.total_coins - schedule.coins_released_so_far - batchCoins
      const nextReleaseAt = remaining <= 0
        ? null
        : new Date(Date.now() + schedule.release_interval_seconds * 1000).toISOString()

      const { error: batchError } = await supabase
        .from('release_batches')
        .insert({
          schedule_id: schedule.id,
          zone_id: schedule.zone_id,
          release_at: nowIso,
          coins_count: batchCoins,
          coins_released: 0,
          status: 'pending',
          s2_cell_token_l17: schedule.s2_cell_token_l17,
          s2_cell_token_l14: schedule.s2_cell_token_l14,
          coin_tier: schedule.coin_tier ?? 'bronze',
        })

      if (batchError) throw batchError

      const { error: updateScheduleError } = await supabase
        .from('release_schedules')
        .update({
          coins_released_so_far: schedule.coins_released_so_far + batchCoins,
          batches_completed: schedule.batches_completed + 1,
          last_release_at: nowIso,
          next_release_at: nextReleaseAt,
          status: remaining <= 0 ? 'completed' : 'active',
          updated_at: nowIso,
        })
        .eq('id', schedule.id)

      if (updateScheduleError) throw updateScheduleError

      schedulesProcessed++
      coinsQueued += batchCoins
      batchesCreated++
    }

    const resultPayload = {
      schedules_processed: schedulesProcessed,
      coins_queued: coinsQueued,
      batches_created: batchesCreated,
      queue_item_ids: createdQueueIds,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'process_timed_releases',
        parameters: { agent_id, reasoning, limit: maxSchedules },
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
        recommended_action: coinsQueued > 0 ? 'process_spawn_queue' : 'no_action_needed',
      },
      _links: {
        schedules: '/api/v1/admin/ai/timed-releases',
        queue: '/api/v1/admin/ai/spawn-queue',
        process_queue: '/api/v1/admin/ai/process-spawn-queue',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    const message = describeError(error)
    const code = message.startsWith('Zone not found:')
      ? AI_ERROR_CODES.ZONE_NOT_FOUND
      : message.startsWith('Invalid L17 cell token')
        ? AI_ERROR_CODES.INVALID_CELL_ID
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
