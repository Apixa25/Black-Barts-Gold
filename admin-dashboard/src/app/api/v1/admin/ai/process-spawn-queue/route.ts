/**
 * POST /api/v1/admin/ai/process-spawn-queue
 *
 * Processes due spawn queue items using the existing SQL processor. Queue items
 * may now carry explicit target coordinates, which makes the legacy processor
 * compatible with cell-first queue entries.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/process-spawn-queue/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import { AI_AGENT_IDS, AI_ERROR_CODES, type AiAgentId } from '@/lib/ai-guardrails'
import { describeError } from '@/lib/ai/error-message'
import { getSpatialCellContext } from '@/lib/geo/s2'

export const dynamic = 'force-dynamic'

interface QueueRow {
  id: string
  zone_id: string
  trigger_type: string
  coin_type: string
  tier: 'gold' | 'silver' | 'bronze'
  min_value: number
  max_value: number
  target_latitude: number | null
  target_longitude: number | null
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
}

function calculateQueuedValue(minValue: number, maxValue: number): number {
  if (maxValue <= minValue) return Number(minValue.toFixed(2))
  return Number((minValue + Math.random() * (maxValue - minValue)).toFixed(2))
}

export async function POST(request: NextRequest) {
  if (!isValidAiApiKey(request)) return unauthorizedResponse()

  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    body = {}
  }

  const { agent_id, reasoning } = body

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

  try {
    const supabase = createServiceRoleClient()
    const nowIso = new Date().toISOString()
    const { data: config, error: configError } = await supabase
      .from('distribution_config')
      .select('enabled, max_spawns_per_cycle')
      .eq('id', '00000000-0000-0000-0000-000000000001')
      .single()

    if (configError) throw configError
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

    const maxPerCycle = Math.max(1, config?.max_spawns_per_cycle ?? 10)
    const { count: dueBefore, error: dueCountError } = await supabase
      .from('spawn_queue')
      .select('id', { count: 'exact', head: true })
      .eq('status', 'pending')
      .lte('scheduled_time', nowIso)

    if (dueCountError) throw dueCountError

    const { data: dueRows, error: dueRowsError } = await supabase
      .from('spawn_queue')
      .select('id, zone_id, trigger_type, coin_type, tier, min_value, max_value, target_latitude, target_longitude, s2_cell_token_l17, s2_cell_token_l14')
      .eq('status', 'pending')
      .lte('scheduled_time', nowIso)
      .order('scheduled_time', { ascending: true })
      .limit(maxPerCycle)

    if (dueRowsError) throw dueRowsError

    const processedItems: Array<{
      id: string
      zone_id: string
      spawned_coin_id: string | null
      s2_cell_token_l17: string | null
      processed_at: string
      status: 'completed' | 'failed'
      error_message?: string
    }> = []

    for (const row of (dueRows ?? []) as QueueRow[]) {
      const { data: claimedRows, error: claimError } = await supabase
        .from('spawn_queue')
        .update({ status: 'processing' })
        .eq('id', row.id)
        .eq('status', 'pending')
        .select('id')

      if (claimError) throw claimError
      if (!claimedRows || claimedRows.length === 0) continue

      try {
        const queuedValue = calculateQueuedValue(row.min_value, row.max_value)
        const { data: coinId, error: spawnError } = await supabase.rpc('spawn_coin', {
          p_zone_id: row.zone_id,
          p_trigger_type: row.trigger_type,
          p_coin_type: row.coin_type,
          p_tier: row.tier,
          p_value: queuedValue,
          p_latitude: row.target_latitude,
          p_longitude: row.target_longitude,
        })

        if (spawnError || !coinId) {
          throw new Error(spawnError?.message ?? 'spawn_coin returned null')
        }

        const { data: coinRow, error: coinFetchError } = await supabase
          .from('coins')
          .select('id, latitude, longitude')
          .eq('id', coinId)
          .single()

        if (coinFetchError || !coinRow) {
          throw new Error(coinFetchError?.message ?? 'Spawn succeeded but coin fetch failed')
        }

        const spatialContext = getSpatialCellContext(coinRow.latitude, coinRow.longitude)

        const finalL17 = row.s2_cell_token_l17 ?? spatialContext.s2CellTokenL17
        const finalL14 = row.s2_cell_token_l14 ?? spatialContext.s2CellTokenL14

        const { error: coinUpdateError } = await supabase
          .from('coins')
          .update({
            s2_cell_token_l17: finalL17,
            s2_cell_token_l14: finalL14,
            created_by: row.trigger_type,
          })
          .eq('id', coinId)

        if (coinUpdateError) throw coinUpdateError

        const { error: historyUpdateError } = await supabase
          .from('spawn_history')
          .update({
            s2_cell_token_l17: finalL17,
            s2_cell_token_l14: finalL14,
            created_by: row.trigger_type,
          })
          .eq('coin_id', coinId)

        if (historyUpdateError) throw historyUpdateError

        const processedAt = new Date().toISOString()
        const { error: completeError } = await supabase
          .from('spawn_queue')
          .update({
            status: 'completed',
            spawned_coin_id: coinId,
            processed_at: processedAt,
            error_message: null,
          })
          .eq('id', row.id)

        if (completeError) throw completeError

        processedItems.push({
          id: row.id,
          zone_id: row.zone_id,
          spawned_coin_id: coinId,
          s2_cell_token_l17: finalL17,
          processed_at: processedAt,
          status: 'completed',
        })
      } catch (itemError) {
        const failedAt = new Date().toISOString()
        const message = describeError(itemError)

        await supabase
          .from('spawn_queue')
          .update({
            status: 'failed',
            error_message: message,
            processed_at: failedAt,
          })
          .eq('id', row.id)

        processedItems.push({
          id: row.id,
          zone_id: row.zone_id,
          spawned_coin_id: null,
          s2_cell_token_l17: row.s2_cell_token_l17,
          processed_at: failedAt,
          status: 'failed',
          error_message: message,
        })
      }
    }

    const { count: dueAfter } = await supabase
      .from('spawn_queue')
      .select('id', { count: 'exact', head: true })
      .eq('status', 'pending')
      .lte('scheduled_time', nowIso)

    const resultPayload = {
      queued_due_before: dueBefore ?? 0,
      queued_due_after: dueAfter ?? 0,
      processed_count: processedItems.filter((item) => item.status === 'completed').length,
      failed_count: processedItems.filter((item) => item.status === 'failed').length,
      processed_items: processedItems,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'process_spawn_queue',
        parameters: { agent_id, reasoning },
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
        recommended_action: (dueAfter ?? 0) > 0 ? 'process_spawn_queue' : 'no_action_needed',
      },
      _links: {
        queue: '/api/v1/admin/ai/spawn-queue',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    const message = describeError(error)
    return NextResponse.json(
      {
        success: false,
        error: message,
        code: AI_ERROR_CODES.SPAWN_FAILED,
      },
      { status: 500 }
    )
  }
}
