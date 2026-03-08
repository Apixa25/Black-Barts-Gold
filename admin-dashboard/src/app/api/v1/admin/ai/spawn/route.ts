/**
 * POST /api/v1/admin/ai/spawn
 *
 * Canonical AI spawn route. Supports cell-first spawns using S2 `cell_id`,
 * while still accepting legacy `zone_id` requests during the transition.
 *
 * Request Body: { cell_id?, zone_id?, tier, agent_id, reasoning, value_usd?,
 *                 latitude?, longitude?, metadata?, idempotency_key? }
 *
 * Returns HTTP 200 on success, 429 on spend limit exceeded, 503 if kill switch active.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import {
  AI_AUTONOMOUS_SPEND_LIMIT_USD,
  AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD,
  AI_AGENT_IDS,
  AI_ERROR_CODES,
  type AiAgentId,
} from '@/lib/ai-guardrails'
import { isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import { getPrimaryNamedZone, toNamedZoneOverlay } from '@/lib/geo/named-zone-membership'
import {
  getCellLevel,
  getRandomPointInCell,
  getSpatialCellContext,
  isValidCellToken,
  S2_LEVEL_PRESSURE,
} from '@/lib/geo/s2'
import type { CoinTier, ZoneGeometry, ZoneType } from '@/types/database'

export const dynamic = 'force-dynamic'

interface ActiveZoneRow {
  id: string
  name: string
  zone_type: ZoneType
  geometry: ZoneGeometry
}

interface SpawnedCoinRow {
  id: string
  value: number
  tier: CoinTier
  latitude: number
  longitude: number
  status: string
  created_by: string
  metadata: Record<string, unknown> | null
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
}

interface SpawnExecutionResult {
  coin: SpawnedCoinRow
  resolvedZoneId: string | null
  resolvedZoneName: string | null
  resolvedCellId: string
}

function toSpawnTriggerType(agentId: AiAgentId): string {
  if (agentId === 'ai_spawn_governor' || agentId === 'ai_game_master') return agentId
  return 'manual'
}

function toCoinCreatedBy(agentId: AiAgentId): string {
  const allowed: string[] = ['ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer']
  return allowed.includes(agentId) ? agentId : 'system'
}

function nextHourISO(): string {
  const d = new Date()
  d.setMinutes(0, 0, 0)
  d.setHours(d.getHours() + 1)
  return d.toISOString()
}

function calculateSpawnValue(tier: CoinTier, explicitValue: number | null): number {
  if (explicitValue !== null) {
    return parseFloat(explicitValue.toFixed(2))
  }

  let generatedValue = 0.1
  switch (tier) {
    case 'gold':
      generatedValue = 2.0 + Math.random() * 8.0
      break
    case 'silver':
      generatedValue = 0.5 + Math.random() * 1.5
      break
    case 'bronze':
    default:
      generatedValue = 0.1 + Math.random() * 0.4
      break
  }

  return parseFloat(generatedValue.toFixed(2))
}

async function getActiveNamedZones() {
  const supabase = createServiceRoleClient()
  const { data, error } = await supabase
    .from('zones')
    .select('id, name, zone_type, geometry')
    .eq('status', 'active')

  if (error) throw error
  return (data ?? []) as ActiveZoneRow[]
}

async function runCellFirstSpawn(params: {
  cellId: string
  zoneId: string | null
  tier: CoinTier
  triggerType: string
  createdBy: string
  explicitValue: number | null
  latitude: number | null
  longitude: number | null
  metadata: Record<string, unknown> | null
}): Promise<SpawnExecutionResult> {
  const {
    cellId,
    zoneId,
    tier,
    triggerType,
    createdBy,
    explicitValue,
    latitude,
    longitude,
    metadata,
  } = params

  if (!isValidCellToken(cellId) || getCellLevel(cellId) !== S2_LEVEL_PRESSURE) {
    const error = new Error(`Invalid L${S2_LEVEL_PRESSURE} cell token: ${cellId}`)
    error.name = AI_ERROR_CODES.INVALID_CELL_ID
    throw error
  }

  const spawnLocation = latitude !== null && longitude !== null
    ? { latitude, longitude }
    : getRandomPointInCell(cellId)

  const spatialCellContext = getSpatialCellContext(spawnLocation.latitude, spawnLocation.longitude)
  if (spatialCellContext.s2CellTokenL17 !== cellId) {
    const error = new Error(
      `Spawn coordinates resolve to ${spatialCellContext.s2CellTokenL17}, not requested cell ${cellId}`
    )
    error.name = AI_ERROR_CODES.INVALID_CELL_ID
    throw error
  }

  const activeZones = await getActiveNamedZones()
  const zoneOverlays = activeZones.map((zone) => toNamedZoneOverlay(zone))
  const explicitZone = zoneId ? activeZones.find((zone) => zone.id === zoneId) ?? null : null

  if (zoneId && !explicitZone) {
    const error = new Error(`Zone not found: ${zoneId}`)
    error.name = AI_ERROR_CODES.ZONE_NOT_FOUND
    throw error
  }

  const inferredZone = explicitZone
    ?? getPrimaryNamedZone(spawnLocation.latitude, spawnLocation.longitude, zoneOverlays)

  const supabase = createServiceRoleClient()
  const coinValue = calculateSpawnValue(tier, explicitValue)

  const { data: coin, error: coinInsertError } = await supabase
    .from('coins')
    .insert({
      coin_type: 'fixed',
      value: coinValue,
      tier,
      is_mythical: false,
      latitude: spawnLocation.latitude,
      longitude: spawnLocation.longitude,
      status: 'visible',
      hidden_at: new Date().toISOString(),
      multi_find: false,
      finds_remaining: 1,
      created_by: createdBy,
      metadata,
      s2_cell_token_l17: spatialCellContext.s2CellTokenL17,
      s2_cell_token_l14: spatialCellContext.s2CellTokenL14,
    })
    .select('id, value, tier, latitude, longitude, status, created_by, metadata, s2_cell_token_l17, s2_cell_token_l14')
    .single()

  if (coinInsertError || !coin) {
    throw new Error(coinInsertError?.message ?? 'Coin insert failed')
  }

  const { error: historyInsertError } = await supabase
    .from('spawn_history')
    .insert({
      coin_id: coin.id,
      zone_id: inferredZone?.id ?? null,
      trigger_type: triggerType,
      coin_value: coinValue,
      coin_tier: tier,
      spawn_latitude: spawnLocation.latitude,
      spawn_longitude: spawnLocation.longitude,
      created_by: createdBy,
      s2_cell_token_l17: spatialCellContext.s2CellTokenL17,
      s2_cell_token_l14: spatialCellContext.s2CellTokenL14,
    })

  if (historyInsertError) {
    await supabase.from('coins').delete().eq('id', coin.id)
    throw new Error(historyInsertError.message)
  }

  return {
    coin: coin as SpawnedCoinRow,
    resolvedZoneId: inferredZone?.id ?? null,
    resolvedZoneName: inferredZone?.name ?? null,
    resolvedCellId: spatialCellContext.s2CellTokenL17,
  }
}

async function runLegacyZoneSpawn(params: {
  zoneId: string
  tier: CoinTier
  triggerType: string
  createdBy: string
  explicitValue: number | null
  latitude: number | null
  longitude: number | null
  metadata: Record<string, unknown> | null
}): Promise<SpawnExecutionResult> {
  const {
    zoneId,
    tier,
    triggerType,
    createdBy,
    explicitValue,
    latitude,
    longitude,
    metadata,
  } = params

  const supabase = createServiceRoleClient()
  const { data: coinId, error: spawnError } = await supabase.rpc('spawn_coin', {
    p_zone_id: zoneId,
    p_trigger_type: triggerType,
    p_coin_type: 'fixed',
    p_tier: tier,
    p_value: explicitValue,
    p_latitude: latitude,
    p_longitude: longitude,
  })

  if (spawnError || !coinId) {
    throw new Error(spawnError?.message ?? 'spawn_coin returned null')
  }

  const { data: coin, error: coinFetchError } = await supabase
    .from('coins')
    .select('id, value, tier, latitude, longitude, status, created_by, metadata, s2_cell_token_l17, s2_cell_token_l14')
    .eq('id', coinId)
    .single()

  if (coinFetchError || !coin) {
    throw new Error(coinFetchError?.message ?? 'Spawn succeeded but coin fetch failed')
  }

  const spatialCellContext = getSpatialCellContext(coin.latitude, coin.longitude)

  await supabase
    .from('coins')
    .update({
      created_by: createdBy,
      metadata,
      s2_cell_token_l17: spatialCellContext.s2CellTokenL17,
      s2_cell_token_l14: spatialCellContext.s2CellTokenL14,
    })
    .eq('id', coin.id)

  await supabase
    .from('spawn_history')
    .update({
      created_by: createdBy,
      s2_cell_token_l17: spatialCellContext.s2CellTokenL17,
      s2_cell_token_l14: spatialCellContext.s2CellTokenL14,
    })
    .eq('coin_id', coin.id)

  return {
    coin: {
      ...(coin as SpawnedCoinRow),
      created_by: createdBy,
      metadata,
      s2_cell_token_l17: spatialCellContext.s2CellTokenL17,
      s2_cell_token_l14: spatialCellContext.s2CellTokenL14,
    },
    resolvedZoneId: zoneId,
    resolvedZoneName: null,
    resolvedCellId: spatialCellContext.s2CellTokenL17,
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
    tier,
    agent_id,
    reasoning,
    value_usd,
    latitude,
    longitude,
    metadata,
    idempotency_key,
  } = body

  if ((!zone_id || typeof zone_id !== 'string') && (!cell_id || typeof cell_id !== 'string')) {
    return NextResponse.json(
      { success: false, error: 'Either cell_id or zone_id is required' },
      { status: 400 }
    )
  }
  if (!tier || !['gold', 'silver', 'bronze'].includes(tier as string)) {
    return NextResponse.json({ success: false, error: 'tier must be gold | silver | bronze' }, { status: 400 })
  }
  if (!agent_id || !(AI_AGENT_IDS as readonly string[]).includes(agent_id as string)) {
    return NextResponse.json({ success: false, error: `agent_id must be one of: ${AI_AGENT_IDS.join(', ')}` }, { status: 400 })
  }
  if (!reasoning || typeof reasoning !== 'string' || reasoning.length < 5) {
    return NextResponse.json({ success: false, error: 'reasoning must be at least 5 characters' }, { status: 400 })
  }
  if ((latitude != null && typeof latitude !== 'number') || (longitude != null && typeof longitude !== 'number')) {
    return NextResponse.json(
      { success: false, error: 'latitude and longitude must both be numbers when provided' },
      { status: 400 }
    )
  }
  if ((latitude == null) !== (longitude == null)) {
    return NextResponse.json(
      { success: false, error: 'latitude and longitude must be provided together' },
      { status: 400 }
    )
  }

  const typedAgentId = agent_id as AiAgentId
  const typedTier = tier as CoinTier
  const explicitValue = typeof value_usd === 'number' ? value_usd : null
  const metadataPayload = (metadata as Record<string, unknown>) ?? null
  const targetZoneId = typeof zone_id === 'string' ? zone_id : null
  const targetCellId = typeof cell_id === 'string' ? cell_id : null
  const spawnLatitude = typeof latitude === 'number' ? latitude : null
  const spawnLongitude = typeof longitude === 'number' ? longitude : null

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
        .eq('tool_called', 'spawn_coin')
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

    const { data: spendData } = await supabase.rpc('get_ai_spend_this_hour', { p_agent_id: null })
    const spendThisHour = (spendData as number) ?? 0

    if (spendThisHour >= AI_AUTONOMOUS_SPEND_LIMIT_USD) {
      return NextResponse.json(
        {
          success: false,
          error: 'Autonomous spend limit reached for this hour',
          code: AI_ERROR_CODES.SPEND_LIMIT_EXCEEDED,
          meta: {
            spend_this_hour_usd: spendThisHour,
            limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
            resets_at: nextHourISO(),
          },
        },
        { status: 429 }
      )
    }

    if (explicitValue !== null && explicitValue > AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD) {
      await supabase.from('ai_actions').insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: {
          zone_id: targetZoneId,
          cell_id: targetCellId,
          tier,
          agent_id,
          reasoning,
          value_usd,
          idempotency_key: idempotency_key ?? null,
          metadata: metadataPayload,
        },
        reasoning: reasoning as string,
        result: null,
        success: false,
        error_code: AI_ERROR_CODES.GUARDRAIL_BLOCKED,
        cost_usd: 0,
      })

      return NextResponse.json(
        {
          success: false,
          error: `Single spawn value $${explicitValue} exceeds approval threshold $${AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD}`,
          code: AI_ERROR_CODES.GUARDRAIL_BLOCKED,
          meta: {
            value_requested_usd: explicitValue,
            approval_threshold_usd: AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD,
          },
        },
        { status: 403 }
      )
    }

    const triggerType = toSpawnTriggerType(typedAgentId)
    const createdBy = toCoinCreatedBy(typedAgentId)

    let spawnResult: SpawnExecutionResult
    if (targetCellId) {
      spawnResult = await runCellFirstSpawn({
        cellId: targetCellId,
        zoneId: targetZoneId,
        tier: typedTier,
        triggerType,
        createdBy,
        explicitValue,
        latitude: spawnLatitude,
        longitude: spawnLongitude,
        metadata: metadataPayload,
      })
    } else if (targetZoneId) {
      spawnResult = await runLegacyZoneSpawn({
        zoneId: targetZoneId,
        tier: typedTier,
        triggerType,
        createdBy,
        explicitValue,
        latitude: spawnLatitude,
        longitude: spawnLongitude,
        metadata: metadataPayload,
      })
    } else {
      return NextResponse.json({ success: false, error: 'Missing spawn target' }, { status: 400 })
    }

    const costUsd = spawnResult.coin.value ?? 0
    const responseData = {
      coin_id: spawnResult.coin.id,
      zone_id: spawnResult.resolvedZoneId,
      zone_name: spawnResult.resolvedZoneName,
      cell_id: spawnResult.resolvedCellId,
      tier: spawnResult.coin.tier,
      value_usd: costUsd,
      latitude: spawnResult.coin.latitude,
      longitude: spawnResult.coin.longitude,
      created_by: spawnResult.coin.created_by,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: {
          zone_id: targetZoneId,
          cell_id: targetCellId,
          tier,
          agent_id,
          reasoning,
          idempotency_key: idempotency_key ?? null,
          metadata: metadataPayload,
        },
        reasoning: reasoning as string,
        result: responseData,
        success: true,
        error_code: null,
        cost_usd: costUsd,
      })
      .select('id')
      .single()

    const spendAfter = parseFloat((spendThisHour + costUsd).toFixed(4))
    const spendRemaining = parseFloat((AI_AUTONOMOUS_SPEND_LIMIT_USD - spendAfter).toFixed(4))

    return NextResponse.json({
      success: true,
      data: {
        ...responseData,
        ai_action_id: actionRow?.id ?? null,
      },
      meta: {
        recommended_action: 'observe_hunt_pressure',
        spend_this_hour_usd: spendAfter,
        spend_remaining_usd: spendRemaining,
        autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
        cell_level_used: S2_LEVEL_PRESSURE,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    const errorCode =
      error instanceof Error && error.name in AI_ERROR_CODES
        ? error.name
        : AI_ERROR_CODES.SPAWN_FAILED

    try {
      const supabase = createServiceRoleClient()
      await supabase.from('ai_actions').insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: {
          zone_id: targetZoneId,
          cell_id: targetCellId,
          tier,
          agent_id,
          reasoning,
          value_usd,
          idempotency_key: idempotency_key ?? null,
          metadata: metadataPayload,
        },
        reasoning: reasoning as string,
        result: { error: error instanceof Error ? error.message : String(error) },
        success: false,
        error_code: errorCode,
        cost_usd: 0,
      })
    } catch (auditError) {
      console.error('[ai/spawn] Failed to write failed-action audit row:', auditError)
    }

    if (errorCode === AI_ERROR_CODES.ZONE_NOT_FOUND || errorCode === AI_ERROR_CODES.INVALID_CELL_ID) {
      return NextResponse.json(
        {
          success: false,
          error: error instanceof Error ? error.message : 'Invalid spatial target',
          code: errorCode,
        },
        { status: 400 }
      )
    }

    console.error('[ai/spawn] Error:', error)
    return NextResponse.json(
      {
        success: false,
        error: 'Internal server error',
        code: AI_ERROR_CODES.SPAWN_FAILED,
        details: error instanceof Error ? error.message : String(error),
      },
      { status: 500 }
    )
  }
}
