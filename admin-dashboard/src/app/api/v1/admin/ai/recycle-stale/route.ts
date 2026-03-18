/**
 * POST /api/v1/admin/ai/recycle-stale
 *
 * Canonical stale-coin recycler. Recycles directly from live coin + spawn history
 * rows and supports cell-first cleanup with optional legacy zone filtering.
 *
 * Request Body: { agent_id, reasoning, max_age_hours?, cell_id?, zone_id?, metadata? }
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/recycle-stale/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import {
  AI_AGENT_IDS,
  AI_ERROR_CODES,
  type AiAgentId,
} from '@/lib/ai-guardrails'
import { isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import { getCellLevel, isValidCellToken, S2_LEVEL_PRESSURE } from '@/lib/geo/s2'

export const dynamic = 'force-dynamic'

interface StaleCoinRow {
  id: string
  hidden_at: string
  s2_cell_token_l17: string | null
}

interface SpawnHistoryRow {
  coin_id: string
  zone_id: string | null
  s2_cell_token_l17: string | null
  recycled_at: string | null
}

async function writeFailedAudit(params: {
  agentId: AiAgentId
  reasoning: string
  maxAgeHours: number
  targetZoneId: string | null
  targetCellId: string | null
  metadata: Record<string, unknown> | null
  errorCode: string
  errorMessage: string
}) {
  const {
    agentId,
    reasoning,
    maxAgeHours,
    targetZoneId,
    targetCellId,
    metadata,
    errorCode,
    errorMessage,
  } = params
  const supabase = createServiceRoleClient()
  await supabase.from('ai_actions').insert({
    agent_id: agentId,
    tool_called: 'recycle_stale_coins',
    parameters: {
      agent_id: agentId,
      reasoning,
      max_age_hours: maxAgeHours,
      zone_id: targetZoneId,
      cell_id: targetCellId,
      metadata,
    },
    reasoning,
    result: { error: errorMessage },
    success: false,
    error_code: errorCode,
    cost_usd: 0,
  })
}

export async function POST(request: NextRequest) {
  if (!isValidAiApiKey(request)) return unauthorizedResponse()

  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  const { agent_id, reasoning, max_age_hours, zone_id, cell_id, metadata } = body

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
  const maxAgeHours = typeof max_age_hours === 'number' ? Math.max(1, Math.min(max_age_hours, 168)) : 48
  const targetZoneId = typeof zone_id === 'string' ? zone_id : null
  const targetCellId = typeof cell_id === 'string' ? cell_id : null
  const metadataPayload = (metadata as Record<string, unknown>) ?? null

  if (targetCellId && (!isValidCellToken(targetCellId) || getCellLevel(targetCellId) !== S2_LEVEL_PRESSURE)) {
    await writeFailedAudit({
      agentId: typedAgentId,
      reasoning,
      maxAgeHours,
      targetZoneId,
      targetCellId,
      metadata: metadataPayload,
      errorCode: AI_ERROR_CODES.INVALID_CELL_ID,
      errorMessage: `Invalid L${S2_LEVEL_PRESSURE} cell token: ${targetCellId}`,
    })

    return NextResponse.json(
      {
        success: false,
        error: `Invalid L${S2_LEVEL_PRESSURE} cell token: ${targetCellId}`,
        code: AI_ERROR_CODES.INVALID_CELL_ID,
      },
      { status: 400 }
    )
  }

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

    const cutoffIso = new Date(Date.now() - maxAgeHours * 60 * 60 * 1000).toISOString()
    const { data: staleCoins, error: staleCoinsError } = await supabase
      .from('coins')
      .select('id, hidden_at, s2_cell_token_l17')
      .in('status', ['visible', 'hidden'])
      .is('collected_at', null)
      .lt('hidden_at', cutoffIso)

    if (staleCoinsError) throw staleCoinsError

    const staleCoinRows = (staleCoins ?? []) as StaleCoinRow[]
    const staleCoinIds = staleCoinRows.map((coin) => coin.id)

    let historyByCoinId = new Map<string, SpawnHistoryRow>()
    if (staleCoinIds.length > 0) {
      const { data: spawnHistoryRows, error: historyError } = await supabase
        .from('spawn_history')
        .select('coin_id, zone_id, s2_cell_token_l17, recycled_at')
        .in('coin_id', staleCoinIds)
        .is('recycled_at', null)

      if (historyError) throw historyError

      historyByCoinId = new Map(
        ((spawnHistoryRows ?? []) as SpawnHistoryRow[]).map((row) => [row.coin_id, row])
      )
    }

    const coinsToRecycle = staleCoinRows.filter((coin) => {
      const history = historyByCoinId.get(coin.id)
      const cellToken = coin.s2_cell_token_l17 ?? history?.s2_cell_token_l17 ?? null
      const zoneToken = history?.zone_id ?? null

      if (targetCellId && cellToken !== targetCellId) return false
      if (targetZoneId && zoneToken !== targetZoneId) return false
      return true
    })

    const coinIdsToRecycle = coinsToRecycle.map((coin) => coin.id)
    const recycledAt = new Date().toISOString()

    if (coinIdsToRecycle.length > 0) {
      const { error: updateCoinsError } = await supabase
        .from('coins')
        .update({ status: 'recycled', updated_at: recycledAt })
        .in('id', coinIdsToRecycle)

      if (updateCoinsError) throw updateCoinsError

      const { error: updateHistoryError } = await supabase
        .from('spawn_history')
        .update({ recycled_at: recycledAt })
        .in('coin_id', coinIdsToRecycle)
        .is('recycled_at', null)

      if (updateHistoryError) throw updateHistoryError
    }

    const cellsAffected = [...new Set(
      coinsToRecycle
        .map((coin) => coin.s2_cell_token_l17 ?? historyByCoinId.get(coin.id)?.s2_cell_token_l17 ?? null)
        .filter((cell): cell is string => Boolean(cell))
    )]
    const zonesAffected = [...new Set(
      coinsToRecycle
        .map((coin) => historyByCoinId.get(coin.id)?.zone_id ?? null)
        .filter((zone): zone is string => Boolean(zone))
    )]

    const coinsRecycled = coinIdsToRecycle.length
    const resultPayload = {
      coins_recycled: coinsRecycled,
      zone_id: targetZoneId,
      cell_id: targetCellId,
      zones_affected: zonesAffected,
      cells_affected: cellsAffected,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'recycle_stale_coins',
        parameters: {
          agent_id,
          reasoning,
          max_age_hours: maxAgeHours,
          zone_id: targetZoneId,
          cell_id: targetCellId,
          metadata: metadataPayload,
        },
        reasoning: reasoning as string,
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
        coins_recycled: coinsRecycled,
        zone_id: targetZoneId,
        cell_id: targetCellId,
        ai_action_id: actionRow?.id ?? null,
      },
      meta: {
        recommended_action: coinsRecycled > 0 ? 'spawn_replacements' : 'no_action_needed',
        zones_affected: zonesAffected,
        cells_affected: cellsAffected,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error)
    await writeFailedAudit({
      agentId: typedAgentId,
      reasoning,
      maxAgeHours,
      targetZoneId,
      targetCellId,
      metadata: metadataPayload,
      errorCode: AI_ERROR_CODES.SPAWN_FAILED,
      errorMessage,
    })

    console.error('[ai/recycle-stale] Error:', error)
    return NextResponse.json(
      {
        success: false,
        error: 'Internal server error',
        code: AI_ERROR_CODES.SPAWN_FAILED,
        details: errorMessage,
      },
      { status: 500 }
    )
  }
}
