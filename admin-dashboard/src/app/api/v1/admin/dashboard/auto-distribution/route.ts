/**
 * GET/POST /api/v1/admin/dashboard/auto-distribution
 *
 * Human-admin live data route for the auto-distribution dashboard. Aggregates
 * queue/config/zone state for browser hooks and proxies privileged actions
 * server-side so the AI API key never reaches the client.
 *
 * @file admin-dashboard/src/app/api/v1/admin/dashboard/auto-distribution/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { requireAdminSession } from '@/lib/admin-session'
import { describeError } from '@/lib/ai/error-message'
import type { DistributionAction, ZoneGeometry, ZoneType } from '@/types/database'

export const dynamic = 'force-dynamic'

interface ZoneRow {
  id: string
  name: string
  zone_type: ZoneType
  auto_spawn_config: {
    enabled?: boolean
    min_coins?: number
    max_coins?: number
  } | null
  geometry: ZoneGeometry
  coins_placed: number
  coins_collected: number
}

interface QueueRow {
  id: string
  zone_id: string
  trigger_type: string
  scheduled_time: string
  coin_type: string
  tier: 'gold' | 'silver' | 'bronze'
  min_value: number
  max_value: number
  is_mythical: boolean
  status: 'pending' | 'processing' | 'completed' | 'failed'
  error_message: string | null
  spawned_coin_id: string | null
  created_at: string
  processed_at: string | null
  s2_cell_token_l17: string | null
  s2_cell_token_l14: string | null
}

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

async function proxyAiWriteRoute(
  request: NextRequest,
  path: string,
  body: Record<string, unknown>
) {
  const headers: HeadersInit = { 'Content-Type': 'application/json' }
  if (process.env.AI_AGENT_API_KEY) {
    headers.Authorization = `Bearer ${process.env.AI_AGENT_API_KEY}`
  }

  const response = await fetch(`${request.nextUrl.origin}${path}`, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
    cache: 'no-store',
  })

  const payload = await response.json()
  return {
    status: response.status,
    payload,
  }
}

export async function GET() {
  try {
    await requireAdminSession()

    const supabase = createServiceRoleClient()
    const startOfDay = new Date()
    startOfDay.setHours(0, 0, 0, 0)
    const startOfDayIso = startOfDay.toISOString()

    const [
      { data: configRow, error: configError },
      { data: zonesData, error: zonesError },
      { data: queueData, error: queueError },
      { data: todayHistory, error: historyError },
      { data: latestHistory, error: latestHistoryError },
    ] = await Promise.all([
      supabase
        .from('distribution_config')
        .select('*')
        .eq('id', '00000000-0000-0000-0000-000000000001')
        .single(),
      supabase
        .from('zones')
        .select('id, name, zone_type, auto_spawn_config, geometry, coins_placed, coins_collected')
        .eq('status', 'active')
        .order('created_at', { ascending: true }),
      supabase
        .from('spawn_queue')
        .select('id, zone_id, trigger_type, scheduled_time, coin_type, tier, min_value, max_value, is_mythical, status, error_message, spawned_coin_id, created_at, processed_at, s2_cell_token_l17, s2_cell_token_l14')
        .order('scheduled_time', { ascending: true })
        .limit(100),
      supabase
        .from('spawn_history')
        .select('zone_id, coin_value, spawned_at, collected_at, recycled_at')
        .gte('spawned_at', startOfDayIso),
      supabase
        .from('spawn_history')
        .select('spawned_at')
        .order('spawned_at', { ascending: false })
        .limit(1)
        .maybeSingle(),
    ])

    if (configError) throw configError
    if (zonesError) throw zonesError
    if (queueError) throw queueError
    if (historyError) throw historyError
    if (latestHistoryError) throw latestHistoryError

    const config = configRow
      ? {
          enabled: configRow.enabled,
          check_interval_seconds: configRow.check_interval_seconds,
          max_spawns_per_cycle: configRow.max_spawns_per_cycle,
          default_min_coins: configRow.default_min_coins,
          default_max_coins: configRow.default_max_coins,
          default_value_range: {
            min: configRow.default_min_value,
            max: configRow.default_max_value,
          },
          default_tier_weights: {
            gold: configRow.default_tier_gold_weight,
            silver: configRow.default_tier_silver_weight,
            bronze: configRow.default_tier_bronze_weight,
          },
          value_strategy: configRow.value_strategy,
          mythical_spawn_chance: configRow.mythical_spawn_chance,
          recycle_enabled: configRow.recycle_enabled,
          recycle_after_hours: configRow.recycle_after_hours,
          recycle_to_new_location: configRow.recycle_to_new_location,
          max_spawns_per_hour: configRow.max_spawns_per_hour,
          cooldown_after_collection_seconds: configRow.cooldown_after_collection_seconds,
        }
      : null

    const zoneRows = (zonesData ?? []) as ZoneRow[]
    const queueRows = (queueData ?? []) as QueueRow[]
    const zoneIdToName = new Map(zoneRows.map((zone) => [zone.id, zone.name]))

    const historyByZone = new Map<string, { spawnedToday: number; collectedToday: number }>()
    for (const row of todayHistory ?? []) {
      const key = row.zone_id ?? '__none__'
      const current = historyByZone.get(key) ?? { spawnedToday: 0, collectedToday: 0 }
      current.spawnedToday += 1
      if (row.collected_at) current.collectedToday += 1
      historyByZone.set(key, current)
    }

    const zoneStatuses = zoneRows.map((zone) => {
      const autoSpawn = zone.auto_spawn_config ?? {}
      const autoSpawnEnabled = autoSpawn.enabled ?? zone.zone_type !== 'hunt'
      const minCoins = autoSpawn.min_coins ?? config?.default_min_coins ?? 3
      const maxCoins = autoSpawn.max_coins ?? config?.default_max_coins ?? 20
      const currentCoinCount = Math.max(0, zone.coins_placed - zone.coins_collected)
      const counts = historyByZone.get(zone.id) ?? { spawnedToday: 0, collectedToday: 0 }
      const pendingForZone = queueRows.filter(
        (row) => row.zone_id === zone.id && (row.status === 'pending' || row.status === 'processing')
      ).length
      const coinsToSpawn = Math.max(0, minCoins - currentCoinCount)

      return {
        zone_id: zone.id,
        zone_name: zone.name,
        zone_type: zone.zone_type,
        auto_spawn_enabled: autoSpawnEnabled,
        min_coins: minCoins,
        max_coins: maxCoins,
        current_coin_count: currentCoinCount,
        active_coins: currentCoinCount,
        collected_today: counts.collectedToday,
        needs_spawn: autoSpawnEnabled && coinsToSpawn > 0,
        coins_to_spawn: coinsToSpawn,
        next_spawn_time:
          queueRows.find((row) => row.zone_id === zone.id && row.status === 'pending')?.scheduled_time ?? undefined,
        average_collection_time_hours: 0,
        spawn_rate_per_hour: counts.spawnedToday / Math.max(1, new Date().getHours() + 1),
        collection_rate_per_hour: counts.collectedToday / Math.max(1, new Date().getHours() + 1),
        pending_queue_count: pendingForZone,
      }
    })

    const spawnQueue = queueRows.map((row) => ({
      id: row.id,
      zone_id: row.zone_id,
      zone_name: zoneIdToName.get(row.zone_id) ?? 'Unknown zone',
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
      status: row.status,
      error_message: row.error_message ?? undefined,
      created_at: row.created_at,
    }))

    const pendingQueue = queueRows.filter((row) => row.status === 'pending')
    const completedToday = queueRows.filter(
      (row) =>
        row.status === 'completed' &&
        row.processed_at &&
        new Date(row.processed_at).getTime() >= startOfDay.getTime()
    )
    const failedToday = queueRows.filter(
      (row) =>
        row.status === 'failed' &&
        row.processed_at &&
        new Date(row.processed_at).getTime() >= startOfDay.getTime()
    )
    const totalValueSpawned = Number(
      ((todayHistory ?? []).reduce((sum, row) => sum + Number(row.coin_value ?? 0), 0)).toFixed(2)
    )
    const collectedHistory = (todayHistory ?? []).filter((row) => row.collected_at)
    const totalValueCollected = Number(
      (collectedHistory.reduce((sum, row) => sum + Number(row.coin_value ?? 0), 0)).toFixed(2)
    )

    const stats = {
      system_status: config?.enabled ? 'running' : 'paused',
      last_spawn_time: latestHistory?.spawned_at ?? null,
      next_scheduled_spawn: pendingQueue[0]?.scheduled_time ?? null,
      total_zones_with_auto_spawn: zoneStatuses.filter((zone) => zone.auto_spawn_enabled).length,
      zones_needing_spawn: zoneStatuses.filter((zone) => zone.needs_spawn).length,
      queue_length: pendingQueue.length,
      coins_spawned_today: todayHistory?.length ?? 0,
      coins_collected_today: collectedHistory.length,
      coins_recycled_today: (todayHistory ?? []).filter((row) => row.recycled_at).length,
      total_value_spawned_today: totalValueSpawned,
      total_value_collected_today: totalValueCollected,
      average_coin_value: todayHistory && todayHistory.length > 0 ? totalValueSpawned / todayHistory.length : 0,
      average_spawn_time_ms: 0,
      spawn_success_rate:
        completedToday.length + failedToday.length > 0
          ? completedToday.length / (completedToday.length + failedToday.length)
          : 1,
      errors_today: failedToday.length,
    }

    return NextResponse.json({
      success: true,
      data: {
        stats,
        zoneStatuses,
        spawnQueue,
        config,
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

    let body: DistributionAction
    try {
      body = await request.json()
    } catch {
      return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
    }

    const supabase = createServiceRoleClient()

    switch (body.type) {
      case 'start':
        await supabase
          .from('distribution_config')
          .update({ enabled: true })
          .eq('id', '00000000-0000-0000-0000-000000000001')
        return NextResponse.json({ success: true, data: { system_status: 'running' } })

      case 'pause':
      case 'stop':
        await supabase
          .from('distribution_config')
          .update({ enabled: false })
          .eq('id', '00000000-0000-0000-0000-000000000001')
        return NextResponse.json({ success: true, data: { system_status: body.type === 'stop' ? 'stopped' : 'paused' } })

      case 'clear_queue': {
        const { error } = await supabase.from('spawn_queue').delete().eq('status', 'pending')
        if (error) throw error
        return NextResponse.json({ success: true, data: { cleared: true } })
      }

      case 'spawn_now': {
        const results = []
        for (let i = 0; i < body.count; i++) {
          const response = await proxyAiWriteRoute(request, '/api/v1/admin/ai/spawn', {
            zone_id: body.zone_id,
            cell_id: body.cell_id ?? null,
            tier: 'bronze',
            agent_id: 'ai_game_master',
            reasoning: `Human admin triggered dashboard spawn_now for zone ${body.zone_id}`,
          })
          results.push(response.payload)
        }
        return NextResponse.json({ success: true, data: { results } })
      }

      case 'recycle_stale':
        {
          const response = await proxyAiWriteRoute(request, '/api/v1/admin/ai/recycle-stale', {
            zone_id: body.zone_id ?? null,
            cell_id: body.cell_id ?? null,
            agent_id: 'ai_game_master',
            reasoning: `Human admin triggered dashboard recycle_stale for ${body.cell_id ?? body.zone_id ?? 'system'}`,
          })

          return NextResponse.json(response.payload, { status: response.status })
        }

      case 'update_config': {
        const configUpdate = body.config
        if (!configUpdate) {
          return NextResponse.json({ success: false, error: 'config is required' }, { status: 400 })
        }

        const updatePayload: Record<string, unknown> = {}
        if (typeof configUpdate.enabled === 'boolean') updatePayload.enabled = configUpdate.enabled
        if (typeof configUpdate.check_interval_seconds === 'number') {
          updatePayload.check_interval_seconds = configUpdate.check_interval_seconds
        }
        if (typeof configUpdate.max_spawns_per_cycle === 'number') {
          updatePayload.max_spawns_per_cycle = configUpdate.max_spawns_per_cycle
        }

        const { error } = await supabase
          .from('distribution_config')
          .update(updatePayload)
          .eq('id', '00000000-0000-0000-0000-000000000001')

        if (error) throw error
        return NextResponse.json({ success: true, data: { updated: true } })
      }

      default:
        return NextResponse.json({ success: false, error: 'Unsupported action' }, { status: 400 })
    }
  } catch (error) {
    return errorResponse(error)
  }
}
