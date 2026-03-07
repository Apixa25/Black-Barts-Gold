/**
 * GET /api/v1/admin/ai/actions
 *
 * Reads the ai_actions audit log. Powers the "What did Black Bart do today?"
 * admin dashboard view and gives the AI agent a memory of its own recent
 * decisions so it can avoid repeating itself or detect patterns.
 *
 * Query Params:
 *   agent_id?     string   — filter to a specific AI agent
 *   tool_called?  string   — filter to a specific tool (e.g. 'spawn_coin')
 *   date?         string   — ISO date string, defaults to today
 *   limit?        number   — default 50, max 200
 *   offset?       number   — for pagination
 *   success?      boolean  — filter by success (true) or failures (false)
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/actions/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createServiceRoleClient } from '@/lib/supabase/server'
import type { AiAgentId } from '@/types/database'

export const dynamic = 'force-dynamic'

export async function GET(request: NextRequest) {
  try {
    const params = request.nextUrl.searchParams

    const agentId = params.get('agent_id')
    const toolCalled = params.get('tool_called')
    const successFilter = params.get('success')
    const limit = Math.min(Math.max(parseInt(params.get('limit') ?? '50'), 1), 200)
    const offset = Math.max(parseInt(params.get('offset') ?? '0'), 0)

    // Date range: default to today
    let dateStart: string
    let dateEnd: string
    const dateParam = params.get('date')
    if (dateParam) {
      const d = new Date(dateParam)
      d.setHours(0, 0, 0, 0)
      dateStart = d.toISOString()
      d.setHours(23, 59, 59, 999)
      dateEnd = d.toISOString()
    } else {
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      dateStart = today.toISOString()
      today.setHours(23, 59, 59, 999)
      dateEnd = today.toISOString()
    }

    const supabase = createServiceRoleClient()

    // ── Build filtered query ──────────────────────────────────────────────────
    let query = supabase
      .from('ai_actions')
      .select('*', { count: 'exact' })
      .gte('created_at', dateStart)
      .lte('created_at', dateEnd)
      .order('created_at', { ascending: false })
      .range(offset, offset + limit - 1)

    if (agentId) query = query.eq('agent_id', agentId)
    if (toolCalled) query = query.eq('tool_called', toolCalled)
    if (successFilter !== null) query = query.eq('success', successFilter === 'true')

    const { data: actions, error, count } = await query

    if (error) throw error

    const rows = actions ?? []
    const totalCount = count ?? 0
    const hasMore = offset + rows.length < totalCount

    // ── Summary stats ─────────────────────────────────────────────────────────
    const totalCostUsd = parseFloat(
      rows.reduce((sum, r) => sum + (r.cost_usd ?? 0), 0).toFixed(4)
    )
    const successCount = rows.filter(r => r.success).length
    const successRate = rows.length > 0 ? parseFloat(((successCount / rows.length) * 100).toFixed(1)) : 0

    // Most active agent in this result set
    const agentCounts = new Map<string, number>()
    for (const row of rows) {
      agentCounts.set(row.agent_id, (agentCounts.get(row.agent_id) ?? 0) + 1)
    }
    let mostActiveAgent: AiAgentId | null = null
    let maxCount = 0
    for (const [id, cnt] of agentCounts) {
      if (cnt > maxCount) {
        maxCount = cnt
        mostActiveAgent = id as AiAgentId
      }
    }

    // Total actions on the queried day (for "actions today" stat, unfiltered by agent/tool)
    const { count: todayTotal } = await supabase
      .from('ai_actions')
      .select('*', { count: 'exact', head: true })
      .gte('created_at', dateStart)
      .lte('created_at', dateEnd)

    return NextResponse.json({
      success: true,
      data: {
        actions: rows,
        total_count: totalCount,
        has_more: hasMore,
      },
      meta: {
        total_cost_usd: totalCostUsd,
        success_rate: successRate,
        most_active_agent: mostActiveAgent,
        actions_today: todayTotal ?? 0,
      },
      timestamp: new Date().toISOString(),
    })
  } catch (error) {
    console.error('[ai/actions] Error:', error)
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
