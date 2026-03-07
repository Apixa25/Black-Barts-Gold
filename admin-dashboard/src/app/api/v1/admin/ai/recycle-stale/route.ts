/**
 * POST /api/v1/admin/ai/recycle-stale
 *
 * Wraps the existing recycle_stale_coins() PostgreSQL function.
 * Cleans up coins that have been sitting uncollected for too long in zones
 * where there are no active players. Logs every call to ai_actions.
 *
 * Request Body: { agent_id, reasoning, max_age_hours?, zone_id? }
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

export const dynamic = 'force-dynamic'

export async function POST(request: NextRequest) {
  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  const { agent_id, reasoning, max_age_hours, zone_id } = body

  // ── Validate ───────────────────────────────────────────────────────────────
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

  try {
    const supabase = createServiceRoleClient()

    // ── Kill switch check ────────────────────────────────────────────────────
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

    // ── Call recycle_stale_coins() RPC ───────────────────────────────────────
    const { data: recycledCount, error: recycleError } = await supabase.rpc('recycle_stale_coins', {
      p_zone_id: targetZoneId,
      p_max_age_hours: maxAgeHours,
    })

    if (recycleError) {
      await supabase.from('ai_actions').insert({
        agent_id: typedAgentId,
        tool_called: 'recycle_stale_coins',
        parameters: { agent_id, reasoning, max_age_hours: maxAgeHours, zone_id: targetZoneId },
        reasoning: reasoning as string,
        result: { error: recycleError.message },
        success: false,
        error_code: AI_ERROR_CODES.SPAWN_FAILED,
        cost_usd: 0,
      })
      return NextResponse.json(
        { success: false, error: 'Recycle failed', details: recycleError.message },
        { status: 500 }
      )
    }

    const coinsRecycled = (recycledCount as number) ?? 0

    // ── Determine zones actually affected ────────────────────────────────────
    // If a specific zone was targeted, that's our answer. Otherwise, query which
    // zones had stale coins recycled in the last minute (best-effort).
    let zonesAffected: string[] = []
    if (targetZoneId) {
      zonesAffected = coinsRecycled > 0 ? [targetZoneId] : []
    } else if (coinsRecycled > 0) {
      const cutoff = new Date(Date.now() - 60 * 1000).toISOString()
      const { data: recentRecycles } = await supabase
        .from('spawn_history')
        .select('zone_id')
        .not('recycled_at', 'is', null)
        .gte('recycled_at', cutoff)
      const uniqueZones = [...new Set((recentRecycles ?? []).map(r => r.zone_id))]
      zonesAffected = uniqueZones
    }

    // ── Log to ai_actions ────────────────────────────────────────────────────
    const resultPayload = {
      coins_recycled: coinsRecycled,
      zone_id: targetZoneId,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'recycle_stale_coins',
        parameters: { agent_id, reasoning, max_age_hours: maxAgeHours, zone_id: targetZoneId },
        reasoning: reasoning as string,
        result: { ...resultPayload, zones_affected: zonesAffected },
        success: true,
        error_code: null,
        cost_usd: 0,
      })
      .select('id')
      .single()

    const recommendedAction = coinsRecycled > 0 ? 'spawn_replacements' : 'no_action_needed'

    return NextResponse.json({
      success: true,
      data: {
        coins_recycled: coinsRecycled,
        zone_id: targetZoneId,
        ai_action_id: actionRow?.id ?? null,
      },
      meta: {
        recommended_action: recommendedAction,
        zones_affected: zonesAffected,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error('[ai/recycle-stale] Error:', error)
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
