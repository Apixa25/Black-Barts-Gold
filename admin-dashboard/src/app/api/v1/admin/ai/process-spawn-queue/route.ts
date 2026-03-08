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

export const dynamic = 'force-dynamic'

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
    const { count: dueBefore, error: dueCountError } = await supabase
      .from('spawn_queue')
      .select('id', { count: 'exact', head: true })
      .eq('status', 'pending')
      .lte('scheduled_time', nowIso)

    if (dueCountError) throw dueCountError

    const { data: processedCount, error: processError } = await supabase.rpc('process_spawn_queue')
    if (processError) throw processError

    const { count: dueAfter } = await supabase
      .from('spawn_queue')
      .select('id', { count: 'exact', head: true })
      .eq('status', 'pending')
      .lte('scheduled_time', nowIso)

    const { data: recentProcessed } = await supabase
      .from('spawn_queue')
      .select('id, zone_id, spawned_coin_id, s2_cell_token_l17, processed_at')
      .not('processed_at', 'is', null)
      .gte('processed_at', new Date(Date.now() - 60 * 1000).toISOString())
      .order('processed_at', { ascending: false })
      .limit(20)

    const resultPayload = {
      queued_due_before: dueBefore ?? 0,
      queued_due_after: dueAfter ?? 0,
      processed_count: (processedCount as number) ?? 0,
      processed_items: recentProcessed ?? [],
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
