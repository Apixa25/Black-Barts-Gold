/**
 * GET /api/v1/admin/ai/hunt-pressure
 *
 * Primary "eyes" of the Spawn Governor.
 * Returns per-cell analysis of how many active players there are vs how many
 * coins are available. S2 cells are the canonical backend geography.
 *
 * Query Params:
 *   active_window_minutes  number  default 30   — how recent a player update must be
 *   min_pressure_threshold number  default 0    — only return cells at or above this score
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { AI_AUTONOMOUS_SPEND_LIMIT_USD } from '@/lib/ai-guardrails'
import { isAuthorizedRequest, unauthorizedResponse } from '@/lib/ai-auth'
import { getMatchingNamedZones, toNamedZoneOverlay } from '@/lib/geo/named-zone-membership'
import { getCellCenter, S2_LEVEL_PRESSURE, S2_LEVEL_SUMMARY } from '@/lib/geo/s2'
import type { CellHuntPressure, CoinTier, ZoneGeometry, ZoneType } from '@/types/database'

export const dynamic = 'force-dynamic'

interface ActiveZoneRow {
  id: string
  name: string
  zone_type: ZoneType
  geometry: ZoneGeometry
  auto_spawn_config: { min_coins?: number; max_coins?: number } | null
  status: string
}

interface ActivePlayerRow {
  user_id: string
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
  updated_at: string
}

interface ActiveCoinRow {
  id: string
  status: string
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
}

interface HiddenTransactionRow {
  user_id: string
  amount: number
}

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

function buildCellLabel(token: string): string {
  return `L${S2_LEVEL_PRESSURE} ${token}`
}

function getMinCoinsForCell(
  matchingZoneIds: string[],
  zoneMap: Map<string, ActiveZoneRow>,
  defaultMinCoins: number
): number {
  let minCoins = defaultMinCoins

  for (const zoneId of matchingZoneIds) {
    const zone = zoneMap.get(zoneId)
    const zoneMinCoins = zone?.auto_spawn_config?.min_coins
    if (typeof zoneMinCoins === 'number') {
      minCoins = Math.max(minCoins, zoneMinCoins)
    }
  }

  return minCoins
}

export async function GET(request: NextRequest) {
  if (!await isAuthorizedRequest(request)) return unauthorizedResponse()

  try {
    const params = request.nextUrl.searchParams
    const activeWindowMinutes = Math.min(Math.max(parseInt(params.get('active_window_minutes') ?? '30', 10), 5), 120)
    const minPressureThreshold = parseFloat(params.get('min_pressure_threshold') ?? '0')

    const supabase = createServiceRoleClient()

    const [zonesResult, configResult, spendResult] = await Promise.all([
      supabase
        .from('zones')
        .select('id, name, zone_type, geometry, auto_spawn_config, status')
        .eq('status', 'active'),
      supabase
        .from('distribution_config')
        .select('enabled, default_min_coins')
        .eq('id', '00000000-0000-0000-0000-000000000001')
        .single(),
      supabase.rpc('get_ai_spend_this_hour', { p_agent_id: null }),
    ])

    if (zonesResult.error) throw zonesResult.error
    if (configResult.error) throw configResult.error

    const activeZones = (zonesResult.data ?? []) as ActiveZoneRow[]
    const zoneMap = new Map(activeZones.map((zone) => [zone.id, zone]))
    const namedZoneOverlays = activeZones.map((zone) => toNamedZoneOverlay(zone))

    const killSwitchActive = !(configResult.data?.enabled ?? true)
    const defaultMinCoins = configResult.data?.default_min_coins ?? 3
    const spendThisHour = (spendResult.data as number) ?? 0
    const spendRemaining = parseFloat((AI_AUTONOMOUS_SPEND_LIMIT_USD - spendThisHour).toFixed(4))

    const cutoff = new Date(Date.now() - activeWindowMinutes * 60 * 1000).toISOString()

    const [playersResult, coinsResult] = await Promise.all([
      supabase
        .from('player_locations')
        .select('user_id, s2_cell_token_l17, s2_cell_token_l14, updated_at')
        .gte('updated_at', cutoff)
        .not('s2_cell_token_l17', 'is', null),
      supabase
        .from('coins')
        .select('id, status, s2_cell_token_l17, s2_cell_token_l14')
        .in('status', ['hidden', 'visible'])
        .not('s2_cell_token_l17', 'is', null),
    ])

    if (playersResult.error) throw playersResult.error
    if (coinsResult.error) throw coinsResult.error

    const activePlayers = (playersResult.data ?? []) as ActivePlayerRow[]
    const activeCoins = (coinsResult.data ?? []) as ActiveCoinRow[]

    const playerCountByCell = new Map<string, number>()
    const playerIdsByCell = new Map<string, string[]>()
    const parentCellByCell = new Map<string, string>()

    for (const player of activePlayers) {
      if (!player.s2_cell_token_l17) continue

      playerCountByCell.set(
        player.s2_cell_token_l17,
        (playerCountByCell.get(player.s2_cell_token_l17) ?? 0) + 1
      )

      const ids = playerIdsByCell.get(player.s2_cell_token_l17) ?? []
      ids.push(player.user_id)
      playerIdsByCell.set(player.s2_cell_token_l17, ids)

      if (player.s2_cell_token_l14) {
        parentCellByCell.set(player.s2_cell_token_l17, player.s2_cell_token_l14)
      }
    }

    const coinCountByCell = new Map<string, number>()
    for (const coin of activeCoins) {
      if (!coin.s2_cell_token_l17) continue

      coinCountByCell.set(
        coin.s2_cell_token_l17,
        (coinCountByCell.get(coin.s2_cell_token_l17) ?? 0) + 1
      )

      if (coin.s2_cell_token_l14 && !parentCellByCell.has(coin.s2_cell_token_l17)) {
        parentCellByCell.set(coin.s2_cell_token_l17, coin.s2_cell_token_l14)
      }
    }

    const activeCellIds = [...new Set([
      ...playerCountByCell.keys(),
      ...coinCountByCell.keys(),
    ])]

    if (activeCellIds.length === 0) {
      return buildEmptyResponse(killSwitchActive, spendThisHour, spendRemaining)
    }

    const maxHiddenByPlayer = new Map<string, number>()
    const allPlayerIds = [...new Set(activePlayers.map((player) => player.user_id))]

    if (allPlayerIds.length > 0) {
      const { data: txRows, error: txError } = await supabase
        .from('transactions')
        .select('user_id, amount')
        .eq('transaction_type', 'hidden')
        .in('user_id', allPlayerIds)

      if (txError) throw txError

      for (const row of (txRows ?? []) as HiddenTransactionRow[]) {
        const existing = maxHiddenByPlayer.get(row.user_id) ?? 0
        if (row.amount > existing) {
          maxHiddenByPlayer.set(row.user_id, row.amount)
        }
      }
    }

    const cellResults: CellHuntPressure[] = []

    for (const cellId of activeCellIds) {
      const center = getCellCenter(cellId)
      const matchingOverlays = getMatchingNamedZones(center.latitude, center.longitude, namedZoneOverlays)
      const matchingZoneIds = matchingOverlays.map((zone) => zone.id)
      const activePlayerCount = playerCountByCell.get(cellId) ?? 0
      const activeCoinCount = coinCountByCell.get(cellId) ?? 0
      const huntPressure = parseFloat((activePlayerCount / Math.max(activeCoinCount, 1)).toFixed(2))

      if (huntPressure < minPressureThreshold && activePlayerCount === 0) {
        continue
      }

      const minCoins = getMinCoinsForCell(matchingZoneIds, zoneMap, defaultMinCoins)
      const needsSpawn = huntPressure > Math.max(minPressureThreshold, 0) && activeCoinCount < minCoins
      const coinsToSpawn = needsSpawn ? Math.max(0, minCoins - activeCoinCount) : 0

      const zonePlayers = playerIdsByCell.get(cellId) ?? []
      const dist = { cabin_boy: 0, deck_hand: 0, captain: 0, king_of_pirates: 0 }

      for (const userId of zonePlayers) {
        const maxHidden = maxHiddenByPlayer.get(userId) ?? 0
        dist[classifyPlayerTier(maxHidden)]++
      }

      cellResults.push({
        cell_id: cellId,
        cell_label: buildCellLabel(cellId),
        cell_level: S2_LEVEL_PRESSURE,
        parent_cell_id: parentCellByCell.get(cellId) ?? `L${S2_LEVEL_SUMMARY}-unknown`,
        center,
        active_player_count: activePlayerCount,
        active_coin_count: activeCoinCount,
        hunt_pressure: huntPressure,
        needs_spawn: needsSpawn,
        coins_to_spawn: coinsToSpawn,
        player_tier_distribution: dist,
        recommended_spawn_tier: recommendSpawnTier(dist),
        named_zone_overlays: matchingOverlays.map((zone) => ({
          zone_id: zone.id,
          zone_name: zone.name,
          zone_type: zone.zone_type,
        })),
      })
    }

    cellResults.sort((a, b) => {
      if (b.hunt_pressure !== a.hunt_pressure) return b.hunt_pressure - a.hunt_pressure
      return b.active_player_count - a.active_player_count
    })

    const cellsNeedingSpawn = cellResults.filter((cell) => cell.needs_spawn).length
    const totalActivePlayers = activePlayers.length
    const totalActiveCoins = activeCoins.length
    const overallPressure = parseFloat((totalActivePlayers / Math.max(totalActiveCoins, 1)).toFixed(2))
    const highPressureCells = cellResults
      .filter((cell) => cell.hunt_pressure > 5.0)
      .map((cell) => cell.cell_id)

    const recommendedAction: 'spawn_coins' | 'no_action_needed' | 'kill_switch_active' =
      killSwitchActive
        ? 'kill_switch_active'
        : cellsNeedingSpawn > 0
          ? 'spawn_coins'
          : 'no_action_needed'

    return NextResponse.json({
      success: true,
      data: {
        cells: cellResults,
        summary: {
          total_active_cells: cellResults.length,
          cells_needing_spawn: cellsNeedingSpawn,
          total_active_players: totalActivePlayers,
          total_active_coins: totalActiveCoins,
          overall_hunt_pressure: overallPressure,
          // Compatibility aliases for older dashboard cards while naming migrates.
          total_active_zones: cellResults.length,
          zones_needing_spawn: cellsNeedingSpawn,
        },
      },
      meta: {
        recommended_action: recommendedAction,
        high_pressure_cells: highPressureCells,
        // Compatibility alias while callers transition to cells.
        high_pressure_zones: highPressureCells,
        spend_this_hour_usd: spendThisHour,
        autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
        spend_remaining_usd: spendRemaining,
        kill_switch_active: killSwitchActive,
        cell_level_used: S2_LEVEL_PRESSURE,
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
      cells: [],
      summary: {
        total_active_cells: 0,
        cells_needing_spawn: 0,
        total_active_players: 0,
        total_active_coins: 0,
        overall_hunt_pressure: 0,
        total_active_zones: 0,
        zones_needing_spawn: 0,
      },
    },
    meta: {
      recommended_action: killSwitchActive ? 'kill_switch_active' : 'no_action_needed',
      high_pressure_cells: [],
      high_pressure_zones: [],
      spend_this_hour_usd: spendThisHour,
      autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
      spend_remaining_usd: spendRemaining,
      kill_switch_active: killSwitchActive,
      cell_level_used: S2_LEVEL_PRESSURE,
    },
    _links: {
      spawn: '/api/v1/admin/ai/spawn',
      recycle: '/api/v1/admin/ai/recycle-stale',
      economy: '/api/v1/admin/ai/economy-health',
    },
    timestamp: new Date().toISOString(),
  })
}
