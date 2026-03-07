/**
 * POST /api/v1/admin/ai/spawn
 *
 * The Spawn Governor's primary action. Wraps the existing spawn_coin() PostgreSQL
 * function, enforces the hourly spend guardrail, logs every action to ai_actions,
 * and supports idempotency keys to prevent duplicate spawns on agent retries.
 *
 * Request Body: { zone_id, tier, agent_id, reasoning, value_usd?, latitude?,
 *                 longitude?, metadata?, idempotency_key? }
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

export const dynamic = 'force-dynamic'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Map AI agent IDs to the trigger_type values the spawn_history CHECK allows */
function toSpawnTriggerType(agentId: AiAgentId): string {
  if (agentId === 'ai_spawn_governor' || agentId === 'ai_game_master') return agentId
  return 'manual'
}

/** Map AI agent IDs to the created_by values the coins CHECK allows */
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

// ---------------------------------------------------------------------------
// Route Handler
// ---------------------------------------------------------------------------

export async function POST(request: NextRequest) {
  let body: Record<string, unknown>
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  // ── Validate required fields ───────────────────────────────────────────────
  const { zone_id, tier, agent_id, reasoning, value_usd, latitude, longitude, metadata, idempotency_key } = body

  if (!zone_id || typeof zone_id !== 'string') {
    return NextResponse.json({ success: false, error: 'zone_id is required' }, { status: 400 })
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

  const typedAgentId = agent_id as AiAgentId

  try {
    const supabase = createServiceRoleClient()

    // ── 1. Kill switch check ──────────────────────────────────────────────────
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

    // ── 2. Idempotency check ──────────────────────────────────────────────────
    if (idempotency_key && typeof idempotency_key === 'string') {
      const { data: existing } = await supabase
        .from('ai_actions')
        .select('id, result, success, cost_usd')
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

    // ── 3. Spend limit check ─────────────────────────────────────────────────
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

    // ── 4. Single-spawn approval threshold ───────────────────────────────────
    const explicitValue = typeof value_usd === 'number' ? value_usd : null
    if (explicitValue !== null && explicitValue > AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD) {
      // Log to ai_actions as PENDING/blocked and return guardrail response
      await supabase.from('ai_actions').insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: { zone_id, tier, agent_id, reasoning, value_usd, idempotency_key: idempotency_key ?? null, metadata: metadata ?? null },
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

    // ── 5. Call spawn_coin() RPC ─────────────────────────────────────────────
    const triggerType = toSpawnTriggerType(typedAgentId)
    const { data: coinId, error: spawnError } = await supabase.rpc('spawn_coin', {
      p_zone_id: zone_id,
      p_trigger_type: triggerType,
      p_coin_type: 'fixed',
      p_tier: tier,
      p_value: explicitValue,
      p_latitude: typeof latitude === 'number' ? latitude : null,
      p_longitude: typeof longitude === 'number' ? longitude : null,
    })

    if (spawnError || !coinId) {
      await supabase.from('ai_actions').insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: { zone_id, tier, agent_id, reasoning, idempotency_key: idempotency_key ?? null },
        reasoning: reasoning as string,
        result: { error: spawnError?.message ?? 'spawn_coin returned null' },
        success: false,
        error_code: AI_ERROR_CODES.SPAWN_FAILED,
        cost_usd: 0,
      })
      return NextResponse.json(
        {
          success: false,
          error: 'Spawn failed',
          code: AI_ERROR_CODES.SPAWN_FAILED,
          details: spawnError?.message,
        },
        { status: 500 }
      )
    }

    // ── 6. Stamp created_by + metadata onto the coin ─────────────────────────
    await supabase
      .from('coins')
      .update({
        created_by: toCoinCreatedBy(typedAgentId),
        metadata: (metadata as Record<string, unknown>) ?? null,
      })
      .eq('id', coinId)

    // ── 7. Fetch coin details for the response ────────────────────────────────
    const { data: coin, error: coinFetchError } = await supabase
      .from('coins')
      .select('id, value, tier, latitude, longitude, status, created_by, metadata')
      .eq('id', coinId)
      .single()

    if (coinFetchError || !coin) {
      return NextResponse.json(
        { success: false, error: 'Spawn succeeded but could not fetch coin details', coin_id: coinId },
        { status: 207 }
      )
    }

    // ── 8. Write ai_actions audit row ─────────────────────────────────────────
    const costUsd = (coin.value as number) ?? 0
    const responseData = {
      coin_id: coin.id,
      zone_id: zone_id as string,
      tier: coin.tier,
      value_usd: costUsd,
      latitude: coin.latitude,
      longitude: coin.longitude,
      created_by: coin.created_by,
    }

    const { data: actionRow } = await supabase
      .from('ai_actions')
      .insert({
        agent_id: typedAgentId,
        tool_called: 'spawn_coin',
        parameters: {
          zone_id,
          tier,
          agent_id,
          reasoning,
          idempotency_key: idempotency_key ?? null,
          metadata: metadata ?? null,
        },
        reasoning: reasoning as string,
        result: responseData,
        success: true,
        error_code: null,
        cost_usd: costUsd,
      })
      .select('id')
      .single()

    // ── 9. Updated spend after this spawn ─────────────────────────────────────
    const spendAfter = parseFloat((spendThisHour + costUsd).toFixed(4))
    const spendRemaining = parseFloat((AI_AUTONOMOUS_SPEND_LIMIT_USD - spendAfter).toFixed(4))

    return NextResponse.json({
      success: true,
      data: {
        ...responseData,
        ai_action_id: actionRow?.id ?? null,
      },
      meta: {
        spend_this_hour_usd: spendAfter,
        spend_remaining_usd: spendRemaining,
        autonomous_spend_limit_usd: AI_AUTONOMOUS_SPEND_LIMIT_USD,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error('[ai/spawn] Error:', error)
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
