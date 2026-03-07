/**
 * GET /api/v1/admin/ai/hunt-pressure
 *
 * Primary "eyes" of the Spawn Governor.
 * Returns per-zone analysis of how many active players there are vs how many
 * coins are available. This is the key input the AI reads before deciding
 * whether and where to spawn.
 *
 * Query Params:
 *   active_window_minutes  number  default 30   — how recent a player update must be
 *   min_pressure_threshold number  default 0    — only return zones at or above this score
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { AI_AUTONOMOUS_SPEND_LIMIT_USD } from '@/lib/ai-guardrails'
import type { ZoneType, CoinTier } from '@/types/database'

export const dynamic = 'force-dynamic'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function classifyPlayerTier(maxHiddenUsd: number): 'cabin_boy' | 'deck_hand' | 'captain' | 'king_of_pirates' {
  if (maxHiddenUsd >= 100) return 'king_of_pirates'
  if (maxHiddenUsd >= 25) return 'captain'
  if (maxHiddenUsd >= 5) return 'deck_hand'
  return 'cabin_boy'
}

function recommendSpawnTier(dist: {
  cabin_boy: number
  deck_hand: number
  captain: number
  king_of_pirates: number
}): CoinTier {
  if (dist.king_of_pirates > dist.captain && dist.king_of_pirates > dist.deck_hand) return 'gold'
  if (dist.captain > dist.cabin_boy) return 'silver'
  return 'bronze'
}

function extractZoneCenter(geometry: unknown): { latitude: number; longitude: number } {
  if (!geometry || typeof geometry !== 'object') return { latitude: 0, longitude: 0 }
  const g = geometry as {
    center?: { latitude: number; longitude: number }
    polygon?: Array<{ latitude: number; longitude: number }>
  }
  if (g.center) return g.center
  if (g.polygon?.length) {
    const lats = g.polygon.map(p => p.latitude)
    const lngs = g.polygon.map(p => p.longitude)
    return {
      latitude: lats.reduce((a, b) => a + b, 0) / lats.length,
      longitude: lngs.reduce((a, b) => a + b, 0) / lngs.length,
    }
  }
  return { latitude: 0, longitude: 0 }
}

// ---------------------------------------------------------------------------
// Route Handler
// ---------------------------------------------------------------------------

export async function GET(request: NextRequest) {
  try {
    const params = request.nextUrl.searchParams
    const activeWindowMinutes = Math.min(Math.max(parseInt(params.get('active_window_minutes') ?? '30'), 5), 120)
    const minPressureThreshold = parseFloat(params.get('min_pressure_threshold') ?? '0')

    const supabase = createServiceRoleClient()

    // ── 1. Active zones ──────────────────────────────────────────────────────
    const { data: zones, error: zonesError } = await supabase
      .from('zones')
      .select('id, name, zone_type, geometry, auto_spawn_config, status')
      .eq('status', 'active')

    if (zonesError) throw zonesError

    // ── 2. Kill switch + spend (run in parallel) ─────────────────────────────
    const [configResult, spendResult] = await Promise.all([
      supabase
        .from('distribution_config')
        .select('enabled')
        .eq('id', '00000000-0000-0000-0000-000000000001')
        .single(),
      supabase.rpc('get_ai_spend_this_hour', { p_agent_id: null }),
    ])

    const killSwitchActive = !(configResult.data?.enabled ?? true)
    const spendThisHour = (spendResult.data as number) ?? 0
    const spendRemaining = parseFloat((AI_AUTONOMOUS_SPEND_LIMIT_USD - spendThisHour).toFixed(4))

    if (!zones || zones.length === 0) {
      return buildEmptyResponse(killSwitchActive, spendThisHour, spendRemaining)
    }

    const cutoff = new Date(Date.now() - activeWindowMinutes * 60 * 1000).toISOString()
    const zoneIds = zones.map(z => z.id)

    // ── 3. Active players + coins in zones (run in parallel) ─────────────────
    const [playersResult, spawnHistoryResult] = await Promise.all([
      supabase
        .from('player_locations')
        .select('user_id, current_zone_id, updated_at')
        .gte('updated_at', cutoff),
      supabase
        .from('spawn_history')
        .select('coin_id, zone_id, coins(status)')
        .in('zone_id', zoneIds)
        .is('collected_at', null)
        .is('recycled_at', null),
    ])

    if (playersResult.error) throw playersResult.error
    if (spawnHistoryResult.error) throw spawnHistoryResult.error

    const activePlayers = playersResult.data ?? []

    // Filter to coins that are still live
    // Supabase infers the joined relation as array; normalise to object before checking status
    const activeSpawnRows = (spawnHistoryResult.data ?? []).filter(row => {
      const raw = row.coins as unknown
      const coin = Array.isArray(raw) ? (raw[0] as { status: string } | undefined) : (raw as { status: string } | null)
      return coin && ['hidden', 'visible'].includes(coin.status)
    })

    // ── 4. Build lookup maps ─────────────────────────────────────────────────
    const coinCountByZone = new Map<string, number>()
    for (const row of activeSpawnRows) {
      coinCountByZone.set(row.zone_id, (coinCountByZone.get(row.zone_id) ?? 0) + 1)
    }

    const playerCountByZone = new Map<string, number>()
    const playerIdsByZone = new Map<string, string[]>()
    for (const p of activePlayers) {
      if (p.current_zone_id) {
        playerCountByZone.set(p.current_zone_id, (playerCountByZone.get(p.current_zone_id) ?? 0) + 1)
        const ids = playerIdsByZone.get(p.current_zone_id) ?? []
        ids.push(p.user_id)
        playerIdsByZone.set(p.current_zone_id, ids)
      }
    }

    // ── 5. Player tier distribution via hidden transactions ──────────────────
    const maxHiddenByPlayer = new Map<string, number>()
    const allPlayerIds = activePlayers.map(p => p.user_id)
    if (allPlayerIds.length > 0) {
      const { data: txRows } = await supabase
        .from('transactions')
        .select('user_id, amount')
        .eq('transaction_type', 'hidden')
        .in('user_id', allPlayerIds)

      for (const row of txRows ?? []) {
        const existing = maxHiddenByPlayer.get(row.user_id) ?? 0
        if (row.amount > existing) maxHiddenByPlayer.set(row.user_id, row.amount)
      }
    }

    // ── 6. Per-zone results ──────────────────────────────────────────────────
    const zoneResults = []
    for (const zone of zones) {
      const activeCoinCount = coinCountByZone.get(zone.id) ?? 0
      const activePlayerCount = playerCountByZone.get(zone.id) ?? 0
      const huntPressure = parseFloat((activePlayerCount / Math.max(activeCoinCount, 1)).toFixed(2))

      if (huntPressure < minPressureThreshold && activePlayerCount === 0) continue

      const config = zone.auto_spawn_config as { min_coins?: number; max_coins?: number } | null
      const minCoins = config?.min_coins ?? 3
      const needsSpawn = huntPressure > Math.max(minPressureThreshold, 0) && activeCoinCount < minCoins
      const coinsToSpawn = needsSpawn ? Math.max(0, minCoins - activeCoinCount) : 0

      const zonePlayers = playerIdsByZone.get(zone.id) ?? []
      const dist = { cabin_boy: 0, deck_hand: 0, captain: 0, king_of_pirates: 0 }
      for (const uid of zonePlayers) {
        const maxHidden = maxHiddenByPlayer.get(uid) ?? 0
        dist[classifyPlayerTier(maxHidden)]++
      }

      zoneResults.push({
        zone_id: zone.id,
        zone_name: zone.name,
        zone_type: zone.zone_type as ZoneType,
        center: extractZoneCenter(zone.geometry),
        active_player_count: activePlayerCount,
        active_coin_count: activeCoinCount,
        hunt_pressure: huntPressure,
        needs_spawn: needsSpawn,
        coins_to_spawn: coinsToSpawn,
        player_tier_distribution: dist,
        recommended_spawn_tier: recommendSpawnTier(dist),
      })
    }

    // Sort most urgent first
    zoneResults.sort((a, b) => b.hunt_pressure - a.hunt_pressure)

    const zonesNeedingSpawn = zoneResults.filter(z => z.needs_spawn).length
    const totalActivePlayers = activePlayers.length
    const totalActiveCoins = activeSpawnRows.length
    const overallPressure = parseFloat((totalActivePlayers / Math.max(totalActiveCoins, 1)).toFixed(2))
    const highPressureZones = zoneResults.filter(z => z.hunt_pressure > 5.0).map(z => z.zone_id)

    let recommendedAction: 'spawn_coins' | 'no_action_needed' | 'kill_switch_active'
    if (killSwitchActive) {
      recommendedAction = 'kill_switch_active'
    } else if (zonesNeedingSpawn > 0) {
      recommendedAction = 'spawn_coins'
    } else {
      recommendedAction = 'no_action_needed'
    }

    return NextResponse.json({
      success: true,
      data: {
        zones: zoneResults,
        summary: {
          total_active_zones: zones.length,
          zones_needing_spawn: zonesNeedingSpawn,
          total_active_players: totalActivePlayers,
          total_active_coins: totalActiveCoins,
          overall_hunt_pressure: overallPressure,
        },
      },
      meta: {
        recommended_action: recommendedAction,
        high_pressure_zones: highPressureZones,
        spend_this_hour_usd: spendThisHour,
        autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
        spend_remaining_usd: spendRemaining,
        kill_switch_active: killSwitchActive,
      },
      _links: {
        spawn: '/api/v1/admin/ai/spawn',
        recycle: '/api/v1/admin/ai/recycle-stale',
        economy: '/api/v1/admin/ai/economy-health',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error('[hunt-pressure] Error:', error)
    return NextResponse.json(
      {
        success: false,
        error: 'Internal server error',
        details: error instanceof Error ? error.message : String(error),
      },
      { status: 500 }
    )
  }
}

function buildEmptyResponse(killSwitchActive: boolean, spendThisHour: number, spendRemaining: number) {
  return NextResponse.json({
    success: true,
    data: {
      zones: [],
      summary: {
        total_active_zones: 0,
        zones_needing_spawn: 0,
        total_active_players: 0,
        total_active_coins: 0,
        overall_hunt_pressure: 0,
      },
    },
    meta: {
      recommended_action: killSwitchActive ? 'kill_switch_active' : 'no_action_needed',
      high_pressure_zones: [],
      spend_this_hour_usd: spendThisHour,
      autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
      spend_remaining_usd: spendRemaining,
      kill_switch_active: killSwitchActive,
    },
    _links: {
      spawn: '/api/v1/admin/ai/spawn',
      recycle: '/api/v1/admin/ai/recycle-stale',
      economy: '/api/v1/admin/ai/economy-health',
    },
    timestamp: new Date().toISOString(),
  })
}
