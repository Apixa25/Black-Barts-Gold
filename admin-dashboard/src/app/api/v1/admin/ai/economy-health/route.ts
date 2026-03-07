/**
 * GET /api/v1/admin/ai/economy-health
 *
 * Gives the AI (and human admins) a snapshot of the coin economy's financial
 * health. Covers supply/demand balance, USD margins, AI spend tracking, and
 * average collection performance. Used by the Spawn Governor as an economy
 * gate — it will not spawn if economy_status is 'margin_risk'.
 *
 * Economy status logic:
 *   supply_demand_ratio < 0.8  → 'undersupply'   (players finding nothing)
 *   supply_demand_ratio 0.8–2.5 → 'healthy'
 *   supply_demand_ratio > 2.5  → 'oversupply'    (coins sitting uncollected)
 *   net_margin_today_usd < 0   → 'margin_risk'   (paying out more than earning)
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/economy-health/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import { AI_AUTONOMOUS_SPEND_LIMIT_USD } from '@/lib/ai-guardrails'
import { isValidAiApiKey, unauthorizedResponse } from '@/lib/ai-auth'
import type { EconomyStatus } from '@/types/database'

export const dynamic = 'force-dynamic'

export async function GET(request: NextRequest) {
  if (!isValidAiApiKey(request)) return unauthorizedResponse()
  try {
    const supabase = createServiceRoleClient()
    const todayStart = new Date()
    todayStart.setHours(0, 0, 0, 0)
    const todayISO = todayStart.toISOString()

    // ── Run all queries in parallel ──────────────────────────────────────────
    const [spawnHistoryResult, activeCoinsResult, aiActionsResult, spendHourResult, gasResult] =
      await Promise.all([
        // Today's spawn/collect/recycle activity
        supabase
          .from('spawn_history')
          .select('coin_id, coin_value, spawned_at, collected_at, recycled_at, time_to_collection_hours')
          .gte('spawned_at', todayISO),

        // Total live coins right now
        supabase
          .from('coins')
          .select('id, value')
          .in('status', ['hidden', 'visible']),

        // Today's AI actions
        supabase
          .from('ai_actions')
          .select('cost_usd, success, agent_id, created_at')
          .gte('created_at', todayISO),

        // AI spend this clock hour
        supabase.rpc('get_ai_spend_this_hour', { p_agent_id: null }),

        // Gas revenue today from transactions
        supabase
          .from('transactions')
          .select('amount')
          .eq('transaction_type', 'gas_consumed')
          .gte('created_at', todayISO),
      ])

    // ── Spawn history aggregation ────────────────────────────────────────────
    const spawnRows = spawnHistoryResult.data ?? []
    const coinsSpawnedToday = spawnRows.length
    const coinsCollectedToday = spawnRows.filter(r => r.collected_at).length
    const coinsRecycledToday = spawnRows.filter(r => r.recycled_at).length

    const valueSpawnedToday = spawnRows.reduce((sum, r) => sum + (r.coin_value ?? 0), 0)
    const valueCollectedToday = spawnRows
      .filter(r => r.collected_at)
      .reduce((sum, r) => sum + (r.coin_value ?? 0), 0)

    const avgCoinValueUsd =
      coinsSpawnedToday > 0
        ? parseFloat((valueSpawnedToday / coinsSpawnedToday).toFixed(4))
        : 0

    const collectionTimesWithData = spawnRows.filter(
      r => r.collected_at && typeof r.time_to_collection_hours === 'number'
    )
    const avgTimeToCollectionHours =
      collectionTimesWithData.length > 0
        ? parseFloat(
            (
              collectionTimesWithData.reduce((sum, r) => sum + (r.time_to_collection_hours ?? 0), 0) /
              collectionTimesWithData.length
            ).toFixed(2)
          )
        : 0

    // ── Active coins ─────────────────────────────────────────────────────────
    const activeCoinsTotal = activeCoinsResult.data?.length ?? 0

    // ── Supply/demand ratio ───────────────────────────────────────────────────
    const supplyDemandRatio = parseFloat(
      (coinsSpawnedToday / Math.max(coinsCollectedToday, 1)).toFixed(2)
    )

    // ── Gas revenue + margin ─────────────────────────────────────────────────
    const gasRevenueToday = (gasResult.data ?? []).reduce((sum, r) => sum + (r.amount ?? 0), 0)
    const netMarginToday = parseFloat((gasRevenueToday - valueCollectedToday).toFixed(4))

    // ── AI spend ──────────────────────────────────────────────────────────────
    const aiActions = aiActionsResult.data ?? []
    const aiSpendToday = parseFloat(
      aiActions.reduce((sum, r) => sum + (r.cost_usd ?? 0), 0).toFixed(4)
    )
    const aiSpendThisHour = (spendHourResult.data as number) ?? 0
    const aiActionsToday = aiActions.length

    // ── Economy status ────────────────────────────────────────────────────────
    let economyStatus: EconomyStatus = 'healthy'
    if (netMarginToday < 0 && coinsCollectedToday > 0) {
      economyStatus = 'margin_risk'
    } else if (supplyDemandRatio < 0.8) {
      economyStatus = 'undersupply'
    } else if (supplyDemandRatio > 2.5) {
      economyStatus = 'oversupply'
    }

    // ── Build recommended action + alerts ────────────────────────────────────
    const alerts: string[] = []
    if (economyStatus === 'margin_risk') {
      alerts.push(`Net margin is $${netMarginToday.toFixed(2)} — payouts exceed gas revenue today`)
    }
    if (supplyDemandRatio > 3.0) {
      alerts.push(`Supply/demand ratio ${supplyDemandRatio} — too many uncollected coins`)
    }
    if (aiSpendThisHour >= AI_AUTONOMOUS_SPEND_LIMIT_USD * 0.8) {
      alerts.push(
        `AI spend at $${aiSpendThisHour.toFixed(2)} this hour — approaching $${AI_AUTONOMOUS_SPEND_LIMIT_USD} limit`
      )
    }

    const recommendedActionMap: Record<EconomyStatus, string> = {
      healthy: 'Monitor and continue normal spawn operations',
      undersupply: 'Spawn more coins — players are active but finding few rewards',
      oversupply: 'Run recycle_stale_coins and pause new spawns until ratio drops below 2.0',
      margin_risk: 'Stop AI spawning immediately — review gas pricing and coin values',
    }

    return NextResponse.json({
      success: true,
      data: {
        coins_spawned_today: coinsSpawnedToday,
        coins_collected_today: coinsCollectedToday,
        coins_recycled_today: coinsRecycledToday,
        active_coins_total: activeCoinsTotal,
        supply_demand_ratio: supplyDemandRatio,
        value_spawned_today_usd: parseFloat(valueSpawnedToday.toFixed(4)),
        value_collected_today_usd: parseFloat(valueCollectedToday.toFixed(4)),
        gas_revenue_today_usd: parseFloat(gasRevenueToday.toFixed(4)),
        net_margin_today_usd: netMarginToday,
        avg_time_to_collection_hours: avgTimeToCollectionHours,
        avg_coin_value_usd: avgCoinValueUsd,
        ai_spend_today_usd: aiSpendToday,
        ai_spend_this_hour_usd: aiSpendThisHour,
        ai_actions_today: aiActionsToday,
      },
      meta: {
        economy_status: economyStatus,
        recommended_action: recommendedActionMap[economyStatus],
        alerts,
      },
      _links: {
        hunt_pressure: '/api/v1/admin/ai/hunt-pressure',
        spawn: '/api/v1/admin/ai/spawn',
        recycle: '/api/v1/admin/ai/recycle-stale',
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error('[ai/economy-health] Error:', error)
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
