/**
 * Black Bart AI Governor — Command Center page (Server Component)
 *
 * Fetches the initial data from the database directly (no HTTP round-trip)
 * and passes it to the Client Component for real-time updates + interactivity.
 *
 * @file admin-dashboard/src/app/(dashboard)/ai-governor/page.tsx
 */

import { createServiceRoleClient } from '@/lib/supabase/server'
import { AiGovernorClient } from './ai-governor-client'
import type { AiAction } from '@/types/database'

export const dynamic = 'force-dynamic'

export default async function AiGovernorPage() {
  const supabase = createServiceRoleClient()

  const todayStart = new Date()
  todayStart.setHours(0, 0, 0, 0)
  const todayISO = todayStart.toISOString()

  // Fetch all initial data in parallel
  const [configResult, actionsResult, aiSummaryResult, activeCoinsResult] = await Promise.all([
    // Kill switch state
    supabase
      .from('distribution_config')
      .select('enabled, max_spawns_per_cycle, check_interval_seconds')
      .eq('id', '00000000-0000-0000-0000-000000000001')
      .single(),

    // Recent AI actions (last 50 today)
    supabase
      .from('ai_actions')
      .select('*')
      .gte('created_at', todayISO)
      .order('created_at', { ascending: false })
      .limit(50),

    // AI actions aggregate for today
    supabase
      .from('ai_actions')
      .select('agent_id, tool_called, cost_usd, success, created_at')
      .gte('created_at', todayISO),

    // Active coin count
    supabase
      .from('coins')
      .select('id', { count: 'exact', head: true })
      .in('status', ['hidden', 'visible']),
  ])

  const killSwitchEnabled = configResult.data?.enabled ?? true
  const recentActions = (actionsResult.data ?? []) as AiAction[]
  const allTodayActions = aiSummaryResult.data ?? []
  const activeCoinsTotal = activeCoinsResult.count ?? 0

  // Compute today's summary stats
  const coinsSpawnedToday = allTodayActions.filter(a => a.tool_called === 'spawn_coin' && a.success).length
  const aiSpendToday = allTodayActions.reduce((sum, a) => sum + (a.cost_usd ?? 0), 0)
  const actionsToday = allTodayActions.length
  const successRate = actionsToday > 0
    ? Math.round((allTodayActions.filter(a => a.success).length / actionsToday) * 100)
    : 100

  return (
    <AiGovernorClient
      initialKillSwitchEnabled={killSwitchEnabled}
      initialRecentActions={recentActions}
      initialStats={{
        actionsToday,
        coinsSpawnedToday,
        aiSpendToday: parseFloat(aiSpendToday.toFixed(4)),
        successRate,
        activeCoinsTotal,
      }}
    />
  )
}
