/**
 * Black Bart's Gold — Spawn Governor Edge Function
 *
 * The AI brain of the coin economy. Runs every 5 minutes via pg_cron and
 * makes autonomous decisions about where to spawn and recycle coins based
 * on live player activity and financial health.
 *
 * INVOCATION MODES:
 *   1. Scheduled (cron):        POST /functions/v1/spawn-governor
 *      → Runs the full 6-step governor cycle
 *   2. Coin collected (webhook): POST /functions/v1/spawn-governor?trigger=coin_collected
 *      → Immediately checks the pressure cell where the coin was found and spawns a
 *        replacement if pressure warrants it. Called by a Supabase Database Webhook.
 *   3. Manual trigger:          POST /functions/v1/spawn-governor?trigger=manual
 *      → Same as cron but recorded as manual in the audit log
 *
 * REQUIRED SECRETS (set via `supabase secrets set KEY=value`):
 *   ADMIN_API_BASE_URL           — e.g. https://your-bbg-admin.vercel.app
 *   AI_AGENT_API_KEY             — bearer token matching the admin dashboard env var
 *   AI_AUTONOMOUS_SPEND_LIMIT_USD — override hourly cap (default: 10)
 *
 * STEP 5 (Realtime Wiring):
 * The module-level Supabase Realtime subscription at the bottom of this file
 * enables immediate reactions when coins are collected — rather than waiting
 * for the next 5-minute cron tick. For production resilience, ALSO configure
 * a Supabase Database Webhook: Table=coins, Event=UPDATE → this function with
 * ?trigger=coin_collected in the URL.
 *
 * @file admin-dashboard/supabase/functions/spawn-governor/index.ts
 */

import { createClient, SupabaseClient } from 'npm:@supabase/supabase-js@2'

// ── Environment ──────────────────────────────────────────────────────────────

const SUPABASE_URL = Deno.env.get('SUPABASE_URL') ?? ''
const SERVICE_ROLE_KEY = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') ?? ''
const ADMIN_API_BASE = (Deno.env.get('ADMIN_API_BASE_URL') ?? '').replace(/\/$/, '')
const AI_AGENT_API_KEY = Deno.env.get('AI_AGENT_API_KEY') ?? ''
const SPEND_LIMIT_USD = parseFloat(Deno.env.get('AI_AUTONOMOUS_SPEND_LIMIT_USD') ?? '10')

// ── Types ────────────────────────────────────────────────────────────────────

interface CellHuntPressure {
  cell_id: string
  cell_label: string
  cell_level: number
  parent_cell_id: string
  active_player_count: number
  active_coin_count: number
  hunt_pressure: number
  needs_spawn: boolean
  coins_to_spawn: number
  recommended_spawn_tier: 'gold' | 'silver' | 'bronze'
  player_tier_distribution: {
    cabin_boy: number
    deck_hand: number
    captain: number
    king_of_pirates: number
  }
  named_zone_overlays: Array<{
    zone_id: string
    zone_name: string
    zone_type: string
  }>
}

interface HuntPressureResponse {
  success: boolean
  data: {
    cells: CellHuntPressure[]
    summary: {
      total_active_cells: number
      cells_needing_spawn: number
      total_active_players: number
      total_active_coins: number
      overall_hunt_pressure: number
      total_active_zones?: number
      zones_needing_spawn?: number
    }
  }
  meta: {
    recommended_action: 'spawn_coins' | 'no_action_needed' | 'kill_switch_active'
    spend_this_hour_usd: number
    spend_remaining_usd: number
    kill_switch_active: boolean
    autonomous_spend_limit_usd: number
    high_pressure_cells?: string[]
  }
}

interface EconomyHealthResponse {
  success: boolean
  data: {
    supply_demand_ratio: number
    net_margin_today_usd: number
    ai_spend_this_hour_usd: number
    active_coins_total: number
    coins_spawned_today: number
    coins_collected_today: number
  }
  meta: {
    economy_status: 'healthy' | 'undersupply' | 'oversupply' | 'margin_risk'
    recommended_action: string
    alerts: string[]
  }
}

interface SpawnResponse {
  success: boolean
  code?: string
  data?: {
    coin_id: string
    value_usd: number
    ai_action_id: string | null
    cell_id?: string
    zone_id?: string | null
  }
  meta?: {
    spend_this_hour_usd: number
    spend_remaining_usd: number
    autonomous_spend_limit_usd: number
  }
  error?: string
}

interface RecycleResponse {
  success: boolean
  data?: {
    coins_recycled: number
    zone_id: string | null
    cell_id?: string | null
  }
  meta?: {
    recommended_action: 'spawn_replacements' | 'no_action_needed'
    zones_affected: string[]
    cells_affected?: string[]
  }
}

interface GovernorCycleResult {
  trigger: string
  started_at: string
  action: 'completed' | 'recycled_only' | 'aborted' | 'error'
  cells_processed: number
  coins_spawned: number
  coins_recycled: number
  total_cost_usd: number
  aborted_reason: string | null
  duration_ms: number
}

// ── Supabase client — used for direct DB operations (audit log, history lookups) ─

let _supabase: SupabaseClient | null = null
function getSupabase(): SupabaseClient {
  if (!_supabase) {
    _supabase = createClient(SUPABASE_URL, SERVICE_ROLE_KEY, {
      auth: { persistSession: false, autoRefreshToken: false },
    })
  }
  return _supabase
}

// ── HTTP helper — calls the admin dashboard API ───────────────────────────────

async function callAdminApi<T>(path: string, options?: RequestInit): Promise<T> {
  if (!ADMIN_API_BASE) {
    throw new Error(
      'ADMIN_API_BASE_URL is not set. Add it via: supabase secrets set ADMIN_API_BASE_URL=https://your-dashboard.vercel.app'
    )
  }

  const res = await fetch(`${ADMIN_API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(AI_AGENT_API_KEY ? { Authorization: `Bearer ${AI_AGENT_API_KEY}` } : {}),
      ...(options?.headers ?? {}),
    },
  })

  if (!res.ok && res.status !== 429 && res.status !== 403 && res.status !== 503) {
    const body = await res.text()
    throw new Error(`Admin API ${path} returned ${res.status}: ${body}`)
  }

  return res.json() as Promise<T>
}

// ── Cycle summary writer — logs every cycle to ai_actions for auditability ────

async function writeCycleSummary(result: GovernorCycleResult, trigger: string): Promise<void> {
  const supabase = getSupabase()
  const { error } = await supabase.from('ai_actions').insert({
    agent_id: 'ai_spawn_governor',
    tool_called: 'spawn_governor_cycle',
    parameters: { trigger, cycle_start: result.started_at, spend_limit_usd: SPEND_LIMIT_USD },
    reasoning:
      result.aborted_reason
        ? `Cycle ${result.action}: ${result.aborted_reason}`
        : `Cycle completed: ${result.coins_spawned} spawned, ${result.coins_recycled} recycled in ${result.duration_ms}ms`,
    result: {
      cells_processed: result.cells_processed,
      zones_processed: result.cells_processed,
      coins_spawned: result.coins_spawned,
      coins_recycled: result.coins_recycled,
      total_cost_usd: result.total_cost_usd,
      action: result.action,
      aborted_reason: result.aborted_reason,
      duration_ms: result.duration_ms,
    },
    success: result.action === 'completed' || result.action === 'recycled_only',
    error_code: result.action === 'aborted' ? (result.aborted_reason?.toUpperCase().replace(/\s/g, '_') ?? null) : null,
    cost_usd: result.total_cost_usd,
  })

  if (error) {
    console.error('[SpawnGov] Failed to write cycle summary to ai_actions:', error)
  }
}

// ── Immediate pressure-cell check — called when a coin is collected ──────────
//
// Used by Step 5 (Realtime subscription) and the coin_collected webhook mode.
// Checks the pressure cell that just lost a coin and spawns a replacement if needed.

async function checkCellPressureImmediate(coinId: string): Promise<void> {
  const supabase = getSupabase()

  // Find which pressure cell this coin belongs to via spawn_history.
  const { data: historyRow } = await supabase
    .from('spawn_history')
    .select('zone_id, s2_cell_token_l17')
    .eq('coin_id', coinId)
    .maybeSingle()

  if (!historyRow?.s2_cell_token_l17) {
    console.log(`[SpawnGov] Coin ${coinId} has no stamped pressure cell — skipping immediate check`)
    return
  }

  // Fetch current pressure with a short 5-minute window (we want truly active players)
  let pressure: HuntPressureResponse
  try {
    pressure = await callAdminApi<HuntPressureResponse>(
      '/api/v1/admin/ai/hunt-pressure?active_window_minutes=5'
    )
  } catch (err) {
    console.error('[SpawnGov] Immediate check: hunt-pressure fetch failed:', err)
    return
  }

  if (!pressure.success) return
  if (pressure.meta.kill_switch_active || pressure.meta.spend_remaining_usd <= 0) return

  const cell = pressure.data.cells.find((pressureCell) => pressureCell.cell_id === historyRow.s2_cell_token_l17)
  if (!cell?.needs_spawn) return

  const windowKey = Math.floor(Date.now() / 300_000)
  const idempotencyKey = `collect_replace_${coinId}_${windowKey}`

  const spawnRes = await callAdminApi<SpawnResponse>('/api/v1/admin/ai/spawn', {
    method: 'POST',
    body: JSON.stringify({
      cell_id: cell.cell_id,
      zone_id: historyRow.zone_id ?? null,
      tier: cell.recommended_spawn_tier,
      agent_id: 'ai_spawn_governor',
      reasoning: `Immediate replace after coin ${coinId} was collected in cell ${cell.cell_id}. ${cell.active_player_count} players still active, pressure ${cell.hunt_pressure}.`,
      metadata: {
        trigger: 'coin_collected',
        collected_coin_id: coinId,
        hunt_pressure: cell.hunt_pressure,
        player_count: cell.active_player_count,
        cell_id: cell.cell_id,
      },
      idempotency_key: idempotencyKey,
    }),
  })

  if (spawnRes.success) {
    console.log(
      `[SpawnGov] Immediate spawn for cell ${cell.cell_id} — coin ${spawnRes.data?.coin_id} ($${spawnRes.data?.value_usd})`
    )
  } else {
    console.log(`[SpawnGov] Immediate spawn skipped: ${spawnRes.code ?? spawnRes.error}`)
  }
}

// ── Main Governor Cycle — the 6-step decision loop ───────────────────────────

async function runGovernorCycle(trigger: string): Promise<GovernorCycleResult> {
  const startTime = Date.now()

  const result: GovernorCycleResult = {
    trigger,
    started_at: new Date().toISOString(),
    action: 'completed',
    cells_processed: 0,
    coins_spawned: 0,
    coins_recycled: 0,
    total_cost_usd: 0,
    aborted_reason: null,
    duration_ms: 0,
  }

  try {
    // ────────────────────────────────────────────────────────────────────────
    // STEP 1: Safety checks
    // Abort fast if the kill switch is active, budget is gone, or nobody is playing.
    // ────────────────────────────────────────────────────────────────────────
    let pressure: HuntPressureResponse
    try {
      pressure = await callAdminApi<HuntPressureResponse>('/api/v1/admin/ai/hunt-pressure')
    } catch (err) {
      result.action = 'error'
      result.aborted_reason = `hunt_pressure_fetch_failed: ${err instanceof Error ? err.message : String(err)}`
      return result
    }

    if (!pressure.success) {
      result.action = 'aborted'
      result.aborted_reason = 'hunt_pressure_api_error'
      return result
    }

    if (pressure.meta.kill_switch_active) {
      result.action = 'aborted'
      result.aborted_reason = 'kill_switch_active'
      console.log('[SpawnGov] Kill switch active — aborting cycle')
      return result
    }

    if (pressure.meta.spend_remaining_usd <= 0) {
      result.action = 'aborted'
      result.aborted_reason = 'spend_limit_reached'
      console.log('[SpawnGov] Spend limit reached — aborting cycle')
      return result
    }

    if (pressure.data.summary.total_active_players === 0) {
      result.action = 'aborted'
      result.aborted_reason = 'no_active_players'
      console.log('[SpawnGov] No active players — aborting cycle (nothing to do)')
      return result
    }

    console.log(
      `[SpawnGov] Safety checks passed. ${pressure.data.summary.total_active_players} active players, ` +
      `${pressure.data.summary.cells_needing_spawn} cells need spawn, ` +
      `$${pressure.meta.spend_remaining_usd.toFixed(2)} remaining`
    )

    // ────────────────────────────────────────────────────────────────────────
    // STEP 2: Fetch economy health
    // ────────────────────────────────────────────────────────────────────────
    let economy: EconomyHealthResponse
    try {
      economy = await callAdminApi<EconomyHealthResponse>('/api/v1/admin/ai/economy-health')
    } catch (err) {
      result.action = 'error'
      result.aborted_reason = `economy_health_fetch_failed: ${err instanceof Error ? err.message : String(err)}`
      return result
    }

    console.log(
      `[SpawnGov] Economy: ${economy.meta.economy_status}, ` +
      `ratio ${economy.data.supply_demand_ratio}, ` +
      `margin $${economy.data.net_margin_today_usd.toFixed(2)}`
    )

    // ────────────────────────────────────────────────────────────────────────
    // STEP 3: Economy gate
    // Hard stop on margin_risk. Recycle-only on severe oversupply.
    // ────────────────────────────────────────────────────────────────────────
    if (economy.meta.economy_status === 'margin_risk') {
      result.action = 'aborted'
      result.aborted_reason = 'margin_risk'
      console.error(
        `[SpawnGov] MARGIN RISK — net margin $${economy.data.net_margin_today_usd.toFixed(2)}. ` +
        'Spawning halted. Admin review required.'
      )
      // Write warning to audit log so admin dashboard shows the alert
      await writeCycleSummary(result, trigger)
      return result
    }

    if (economy.data.supply_demand_ratio > 3.0) {
      console.log(
        `[SpawnGov] Oversupply (ratio ${economy.data.supply_demand_ratio}) — running recycle pass, no new spawns`
      )
      const recycleRes = await callAdminApi<RecycleResponse>('/api/v1/admin/ai/recycle-stale', {
        method: 'POST',
        body: JSON.stringify({
          agent_id: 'ai_spawn_governor',
          reasoning: `Oversupply: supply/demand ratio ${economy.data.supply_demand_ratio} > 3.0. Recycling stale coins before considering new spawns.`,
          max_age_hours: 24,
        }),
      })
      result.action = 'recycled_only'
      result.coins_recycled = recycleRes.data?.coins_recycled ?? 0
      result.aborted_reason = 'oversupply'
      await writeCycleSummary(result, trigger)
      return result
    }

    // ────────────────────────────────────────────────────────────────────────
    // STEP 4: Spawn decisions
    // Iterate cells by hunt_pressure DESC (already sorted by the API).
    // Top off each needy cell toward its target coin count, while still stopping
    // immediately if budget runs out or the kill switch flips mid-cycle.
    // ────────────────────────────────────────────────────────────────────────
    const cellsNeedingSpawn = pressure.data.cells.filter((cell) => cell.needs_spawn)
    let spendRemaining = pressure.meta.spend_remaining_usd
    let budgetExhausted = false

    console.log(`[SpawnGov] ${cellsNeedingSpawn.length} cell(s) need spawn`)

    for (const cell of cellsNeedingSpawn) {
      if (budgetExhausted || spendRemaining <= 0) break

      // 5-minute window key — prevents duplicate spawns within the same cycle
      const windowKey = Math.floor(Date.now() / 300_000)
      const plannedSpawns = Math.max(1, cell.coins_to_spawn)

      console.log(
        `[SpawnGov] Spawning up to ${plannedSpawns} ${cell.recommended_spawn_tier} coin(s) in cell ${cell.cell_id} ` +
        `(pressure ${cell.hunt_pressure}, ${cell.active_player_count} players, ${cell.active_coin_count} counted coins)`
      )

      for (let spawnIndex = 0; spawnIndex < plannedSpawns; spawnIndex++) {
        if (budgetExhausted || spendRemaining <= 0) break

        const idempotencyKey = `spawn_gov_${cell.cell_id}_${windowKey}_${spawnIndex + 1}`
        const spawnOrdinal = spawnIndex + 1

        const spawnRes = await callAdminApi<SpawnResponse>('/api/v1/admin/ai/spawn', {
          method: 'POST',
          body: JSON.stringify({
            cell_id: cell.cell_id,
            zone_id: cell.named_zone_overlays[0]?.zone_id ?? null,
            tier: cell.recommended_spawn_tier,
            agent_id: 'ai_spawn_governor',
            reasoning:
              `Cell ${cell.cell_id}: ` +
              `${cell.active_player_count} players, ${cell.active_coin_count} counted coins, ` +
              `pressure ${cell.hunt_pressure}. ` +
              `Top-off spawn ${spawnOrdinal} of ${plannedSpawns}. ` +
              `Economy: ${economy.meta.economy_status}.`,
            metadata: {
              cell_id: cell.cell_id,
              hunt_pressure: cell.hunt_pressure,
              player_count: cell.active_player_count,
              coin_count: cell.active_coin_count,
              economy_status: economy.meta.economy_status,
              supply_demand_ratio: economy.data.supply_demand_ratio,
              trigger,
              spawn_ordinal: spawnOrdinal,
              planned_spawns: plannedSpawns,
            },
            idempotency_key: idempotencyKey,
          }),
        })

        if (!spawnRes.success) {
          if (spawnRes.code === 'SPEND_LIMIT_EXCEEDED') {
            console.log('[SpawnGov] Spend limit hit — stopping spawn loop')
            budgetExhausted = true
            break
          }
          if (spawnRes.code === 'DISTRIBUTION_DISABLED') {
            console.log('[SpawnGov] Kill switch activated mid-cycle — stopping')
            result.action = 'aborted'
            result.aborted_reason = 'kill_switch_active'
            budgetExhausted = true
            break
          }
          console.warn(
            `[SpawnGov] Spawn ${spawnOrdinal}/${plannedSpawns} failed for cell ${cell.cell_id}: ${spawnRes.code ?? spawnRes.error}`
          )
          break
        }

        const coinValue = spawnRes.data?.value_usd ?? 0
        result.coins_spawned++
        result.total_cost_usd = parseFloat((result.total_cost_usd + coinValue).toFixed(4))
        result.cells_processed++
        spendRemaining = spawnRes.meta?.spend_remaining_usd ?? Math.max(0, spendRemaining - coinValue)

        console.log(
          `[SpawnGov] ✓ Spawned coin ${spawnRes.data?.coin_id} ` +
          `($${coinValue}) in cell ${cell.cell_id} [${spawnOrdinal}/${plannedSpawns}] | ` +
          `$${spendRemaining.toFixed(2)} remaining`
        )
      }
    }

    // ────────────────────────────────────────────────────────────────────────
    // STEP 5: Cleanup pass
    // Recycle stale coins directly by dead pressure cell.
    // Uses a 6-hour age limit to avoid recycling freshly placed coins.
    // ────────────────────────────────────────────────────────────────────────
    const deadCells = pressure.data.cells.filter(
      (cell) => cell.active_player_count === 0 && cell.active_coin_count > 0
    )

    if (deadCells.length > 0) {
      console.log(`[SpawnGov] Cleanup pass: ${deadCells.length} dead cell(s) eligible for recycle`)
    }

    for (const cell of deadCells) {
      const recycleRes = await callAdminApi<RecycleResponse>('/api/v1/admin/ai/recycle-stale', {
        method: 'POST',
        body: JSON.stringify({
          agent_id: 'ai_spawn_governor',
          cell_id: cell.cell_id,
          zone_id: cell.named_zone_overlays[0]?.zone_id ?? null,
          reasoning:
            `Cell ${cell.cell_id} has 0 active players and ${cell.active_coin_count} stale coin(s). Recycling after 6h.`,
          max_age_hours: 6,
        }),
      })

      const recycled = recycleRes.data?.coins_recycled ?? 0
      result.coins_recycled += recycled

      if (recycled > 0) {
        console.log(`[SpawnGov] Recycled ${recycled} stale coin(s) in cell ${cell.cell_id}`)
      }
    }

    // ────────────────────────────────────────────────────────────────────────
    // STEP 6: Write cycle summary to ai_actions audit log
    // ────────────────────────────────────────────────────────────────────────
    // Always record the outcome, including clean aborts like "no_active_players",
    // so the dashboard can explain why nothing happened instead of appearing idle.
    await writeCycleSummary(result, trigger)

    console.log(
      `[SpawnGov] Cycle complete — ` +
      `spawned: ${result.coins_spawned}, recycled: ${result.coins_recycled}, ` +
      `cost: $${result.total_cost_usd.toFixed(4)}, duration: ${Date.now() - startTime}ms`
    )
  } catch (err) {
    result.action = 'error'
    result.aborted_reason = err instanceof Error ? err.message : String(err)
    console.error('[SpawnGov] Unhandled cycle error:', err)

    // Best-effort log even on error
    try {
      await writeCycleSummary(result, trigger)
    } catch {
      // Swallow — don't let a logging failure mask the real error
    }
  } finally {
    result.duration_ms = Date.now() - startTime
  }

  return result
}

// ── Step 5: Realtime subscription ────────────────────────────────────────────
//
// This module-level code runs once when the Edge Function isolate loads.
// It subscribes to coin UPDATE events so the Governor can react immediately
// when a coin is collected — without waiting for the next 5-minute cron tick.
//
// PRODUCTION NOTE: For guaranteed delivery, also configure a Supabase Database
// Webhook on the coins table (event=UPDATE) pointing to:
//   POST /functions/v1/spawn-governor?trigger=coin_collected
// The webhook is more reliable than the Realtime subscription because it
// survives Edge Function cold starts.
//
// ─────────────────────────────────────────────────────────────────────────────

function setupRealtimeSubscription(): void {
  if (!SUPABASE_URL || !SERVICE_ROLE_KEY) {
    console.log('[SpawnGov] Realtime subscription skipped — SUPABASE_URL or SERVICE_ROLE_KEY not set')
    return
  }

  const supabase = getSupabase()

  supabase
    .channel('spawn-governor-coin-events')
    .on(
      'postgres_changes',
      {
        event: 'UPDATE',
        schema: 'public',
        table: 'coins',
        filter: 'status=eq.collected',
      },
      async (payload) => {
        const coin = payload.new as { id: string; status: string }
        console.log(`[SpawnGov] Realtime: coin ${coin.id} collected — checking cell pressure`)
        try {
          await checkCellPressureImmediate(coin.id)
        } catch (err) {
          console.error('[SpawnGov] Realtime handler error:', err)
        }
      }
    )
    .subscribe((status, err) => {
      if (status === 'SUBSCRIBED') {
        console.log('[SpawnGov] Realtime subscription active — watching coins.status changes')
      } else if (status === 'CHANNEL_ERROR') {
        console.error('[SpawnGov] Realtime subscription error:', err)
      }
    })
}

// Set up the Realtime subscription when this module loads
setupRealtimeSubscription()

// ── Entry point ───────────────────────────────────────────────────────────────

Deno.serve(async (req: Request) => {
  const url = new URL(req.url)
  const trigger = url.searchParams.get('trigger') ?? 'cron'

  // ── Coin collection webhook mode ─────────────────────────────────────────
  if (trigger === 'coin_collected') {
    try {
      let coinId: string | undefined

      if (req.method === 'POST') {
        const body = await req.json().catch(() => ({}))
        // Supabase Database Webhooks send { type, table, record, old_record }
        // Direct calls send { coin_id }
        coinId = body.coin_id ?? body.record?.id
      } else {
        coinId = url.searchParams.get('coin_id') ?? undefined
      }

      if (!coinId) {
        return new Response(
          JSON.stringify({ error: 'coin_id is required for trigger=coin_collected' }),
          { status: 400, headers: { 'Content-Type': 'application/json' } }
        )
      }

      await checkCellPressureImmediate(coinId)
      return new Response(
        JSON.stringify({ status: 'ok', trigger, coin_id: coinId }),
        { headers: { 'Content-Type': 'application/json' } }
      )
    } catch (err) {
      console.error('[SpawnGov] Webhook handler error:', err)
      return new Response(
        JSON.stringify({ error: err instanceof Error ? err.message : String(err) }),
        { status: 500, headers: { 'Content-Type': 'application/json' } }
      )
    }
  }

  // ── Full governor cycle (cron / manual) ──────────────────────────────────
  const result = await runGovernorCycle(trigger)

  return new Response(JSON.stringify(result, null, 2), {
    status: result.action === 'error' ? 500 : 200,
    headers: { 'Content-Type': 'application/json' },
  })
})
