"use client"

/**
 * Black Bart AI Governor — Command Center (Client Component)
 *
 * Live dashboard for monitoring and controlling the AI Spawn Governor.
 * Polls the admin AI API routes every 15 seconds for fresh data.
 *
 * Sections:
 *   1. Status bar — 5 KPI cards (economy, spend, actions, coins, kill switch)
 *   2. Hunt pressure grid — per-zone live pressure scores
 *   3. Economy health panel — supply/demand + margin breakdown
 *   4. Action feed — real-time log of every AI decision
 *
 * @file admin-dashboard/src/app/(dashboard)/ai-governor/ai-governor-client.tsx
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { toast } from 'sonner'
import {
  Card, CardContent, CardDescription, CardHeader, CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Separator } from '@/components/ui/separator'
import {
  Bot, Zap, DollarSign, Coins, Activity, TrendingUp, TrendingDown,
  RefreshCw, Play, AlertTriangle, CheckCircle2, XCircle, Clock,
  Users, Flame, Snowflake, BarChart3, Heart,
} from 'lucide-react'
import type { AiAction, ZoneHuntPressure, EconomyStatus } from '@/types/database'

// ── Constants ────────────────────────────────────────────────────────────────

const SPEND_LIMIT_USD = 10.00
const POLL_INTERVAL_MS = 15_000

// ── Types ────────────────────────────────────────────────────────────────────

interface InitialStats {
  actionsToday: number
  coinsSpawnedToday: number
  aiSpendToday: number
  successRate: number
  activeCoinsTotal: number
}

interface EconomyData {
  coins_spawned_today: number
  coins_collected_today: number
  coins_recycled_today: number
  active_coins_total: number
  supply_demand_ratio: number
  value_spawned_today_usd: number
  value_collected_today_usd: number
  gas_revenue_today_usd: number
  net_margin_today_usd: number
  avg_time_to_collection_hours: number
  ai_spend_today_usd: number
  ai_spend_this_hour_usd: number
  ai_actions_today: number
}

interface EconomyResponse {
  success: boolean
  data: EconomyData
  meta: {
    economy_status: EconomyStatus
    recommended_action: string
    alerts: string[]
  }
}

interface PressureResponse {
  success: boolean
  data: {
    zones: ZoneHuntPressure[]
    summary: {
      total_active_zones: number
      zones_needing_spawn: number
      total_active_players: number
      total_active_coins: number
      overall_hunt_pressure: number
    }
  }
  meta: {
    recommended_action: 'spawn_coins' | 'no_action_needed' | 'kill_switch_active'
    spend_this_hour_usd: number
    spend_remaining_usd: number
    kill_switch_active: boolean
  }
}

interface ActionsResponse {
  success: boolean
  data: { actions: AiAction[]; total_count: number; has_more: boolean }
  meta: { total_cost_usd: number; success_rate: number; most_active_agent: string | null; actions_today: number }
}

interface AiGovernorClientProps {
  initialKillSwitchEnabled: boolean
  initialRecentActions: AiAction[]
  initialStats: InitialStats
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function timeAgo(isoString: string): string {
  const diff = Date.now() - new Date(isoString).getTime()
  const mins = Math.floor(diff / 60_000)
  const secs = Math.floor(diff / 1_000)
  if (secs < 60) return `${secs}s ago`
  if (mins < 60) return `${mins}m ago`
  return `${Math.floor(mins / 60)}h ago`
}

function economyStatusConfig(status: EconomyStatus) {
  const map = {
    healthy:      { label: 'Healthy',     color: 'text-green-700',  bg: 'bg-green-50  border-green-200',  icon: CheckCircle2, dot: 'bg-green-500' },
    undersupply:  { label: 'Undersupply', color: 'text-amber-700',  bg: 'bg-amber-50  border-amber-200',  icon: TrendingDown, dot: 'bg-amber-500' },
    oversupply:   { label: 'Oversupply',  color: 'text-blue-700',   bg: 'bg-blue-50   border-blue-200',   icon: TrendingUp,   dot: 'bg-blue-500'  },
    margin_risk:  { label: 'Margin Risk', color: 'text-red-700',    bg: 'bg-red-50    border-red-200',    icon: AlertTriangle, dot: 'bg-red-500'  },
  }
  return map[status] ?? map.healthy
}

function agentConfig(agentId: string) {
  const map: Record<string, { label: string; badge: string }> = {
    ai_spawn_governor:    { label: 'Spawn Gov',  badge: 'bg-gold/20 text-saddle-dark border-gold/30' },
    ai_game_master:       { label: 'Game Master', badge: 'bg-purple-100 text-purple-800 border-purple-200' },
    ai_economy_balancer:  { label: 'Economist',   badge: 'bg-blue-100 text-blue-800 border-blue-200' },
    ai_churn_agent:       { label: 'Churn Agent', badge: 'bg-orange-100 text-orange-800 border-orange-200' },
  }
  return map[agentId] ?? { label: agentId, badge: 'bg-parchment text-leather border-saddle-light/30' }
}

function pressureColor(pressure: number): string {
  if (pressure >= 5) return 'text-red-600'
  if (pressure >= 3) return 'text-amber-600'
  if (pressure >= 1) return 'text-green-600'
  return 'text-leather-light'
}

function pressureBg(pressure: number): string {
  if (pressure >= 5) return 'border-red-200 bg-red-50/50'
  if (pressure >= 3) return 'border-amber-200 bg-amber-50/50'
  if (pressure >= 1) return 'border-green-200 bg-green-50/50'
  return 'border-saddle-light/30 bg-parchment-light/50'
}

function tierBadge(tier: string): string {
  const map: Record<string, string> = {
    gold:   'bg-gold/20 text-saddle-dark border-gold/40',
    silver: 'bg-slate-100 text-slate-700 border-slate-200',
    bronze: 'bg-orange-100 text-orange-700 border-orange-200',
  }
  return map[tier] ?? 'bg-parchment text-leather'
}

// ── Main Component ────────────────────────────────────────────────────────────

export function AiGovernorClient({
  initialKillSwitchEnabled,
  initialRecentActions,
  initialStats,
}: AiGovernorClientProps) {
  // ── State ────────────────────────────────────────────────────────────────
  const [killSwitchEnabled, setKillSwitchEnabled] = useState(initialKillSwitchEnabled)
  const [recentActions, setRecentActions] = useState<AiAction[]>(initialRecentActions)
  const [economy, setEconomy] = useState<EconomyResponse | null>(null)
  const [pressure, setPressure] = useState<PressureResponse | null>(null)
  const [stats, setStats] = useState(initialStats)
  const [lastRefreshed, setLastRefreshed] = useState(new Date())
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isTriggering, setIsTriggering] = useState(false)
  const [isTogglingKillSwitch, setIsTogglingKillSwitch] = useState(false)
  const [secondsSinceRefresh, setSecondsSinceRefresh] = useState(0)
  const pollTimerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // ── Data fetching ─────────────────────────────────────────────────────────
  const fetchLiveData = useCallback(async (showToast = false) => {
    setIsRefreshing(true)
    try {
      const [economyRes, pressureRes, actionsRes] = await Promise.all([
        fetch('/api/v1/admin/ai/economy-health'),
        fetch('/api/v1/admin/ai/hunt-pressure'),
        fetch('/api/v1/admin/ai/actions?limit=30'),
      ])

      if (economyRes.ok) setEconomy(await economyRes.json())
      if (pressureRes.ok) setPressure(await pressureRes.json())
      if (actionsRes.ok) {
        const actionsData: ActionsResponse = await actionsRes.json()
        if (actionsData.success) {
          setRecentActions(actionsData.data.actions)
          setStats(prev => ({
            ...prev,
            actionsToday: actionsData.meta.actions_today,
          }))
        }
      }

      setLastRefreshed(new Date())
      setSecondsSinceRefresh(0)
      if (showToast) toast.success('Data refreshed')
    } catch (err) {
      if (showToast) toast.error('Refresh failed — check console')
      console.error('[AiGovernor] Fetch error:', err)
    } finally {
      setIsRefreshing(false)
    }
  }, [])

  // Initial fetch + polling
  useEffect(() => {
    fetchLiveData()
    pollTimerRef.current = setInterval(() => fetchLiveData(), POLL_INTERVAL_MS)
    return () => { if (pollTimerRef.current) clearInterval(pollTimerRef.current) }
  }, [fetchLiveData])

  // "X seconds ago" counter
  useEffect(() => {
    const t = setInterval(() => setSecondsSinceRefresh(s => s + 1), 1000)
    return () => clearInterval(t)
  }, [lastRefreshed])

  // ── Kill switch toggle ────────────────────────────────────────────────────
  const handleKillSwitch = async () => {
    const newValue = !killSwitchEnabled
    setIsTogglingKillSwitch(true)
    try {
      const res = await fetch('/api/v1/admin/ai/kill-switch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled: newValue }),
      })
      const data = await res.json()
      if (data.success) {
        setKillSwitchEnabled(newValue)
        toast.success(data.data.message)
      } else {
        toast.error(data.error ?? 'Failed to toggle kill switch')
      }
    } catch {
      toast.error('Network error — kill switch unchanged')
    } finally {
      setIsTogglingKillSwitch(false)
    }
  }

  // ── Manual governor trigger ───────────────────────────────────────────────
  const handleManualTrigger = async () => {
    setIsTriggering(true)
    toast.info('🤠 Summoning Black Bart…')
    try {
      const res = await fetch('/api/v1/admin/ai/trigger-governor', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      })
      const data = await res.json()
      if (data.success) {
        const result = data.data
        const msg = result?.coins_spawned != null
          ? `Cycle complete: ${result.coins_spawned} spawned, ${result.coins_recycled} recycled ($${(result.total_cost_usd ?? 0).toFixed(2)})`
          : 'Governor cycle completed'
        toast.success(`🤠 ${msg}`)
        await fetchLiveData()
      } else if (data.code === 'EDGE_FUNCTION_NOT_CONFIGURED') {
        toast.error('Edge Function not deployed yet', {
          description: 'Run: supabase functions deploy spawn-governor --no-verify-jwt',
          duration: 8000,
        })
      } else {
        toast.error(data.error ?? 'Governor cycle failed')
      }
    } catch {
      toast.error('Network error — could not reach the Edge Function')
    } finally {
      setIsTriggering(false)
    }
  }

  // ── Derived values ────────────────────────────────────────────────────────
  const economyStatus = economy?.meta.economy_status ?? 'healthy'
  const statusCfg = economyStatusConfig(economyStatus)
  const StatusIcon = statusCfg.icon

  const spendThisHour = economy?.data.ai_spend_this_hour_usd ?? pressure?.meta.spend_this_hour_usd ?? 0
  const spendPct = Math.min((spendThisHour / SPEND_LIMIT_USD) * 100, 100)
  const totalActivePlayers = pressure?.data.summary.total_active_players ?? 0
  const zonesNeedingSpawn = pressure?.data.summary.zones_needing_spawn ?? 0

  const alerts = economy?.meta.alerts ?? []

  return (
    <>
    {/* ── Heartbeat keyframe animation ─────────────────────────────────────── */}
    <style>{`
      @keyframes bb-heartbeat {
        0%, 100% { transform: scale(1);    opacity: 1;    }
        10%       { transform: scale(1.35); opacity: 1;    }
        20%       { transform: scale(1);    opacity: 1;    }
        32%       { transform: scale(1.2);  opacity: 1;    }
        50%       { transform: scale(1);    opacity: 0.85; }
      }
      @keyframes bb-heartbeat-fast {
        0%, 100% { transform: scale(1);    opacity: 1;    }
        10%       { transform: scale(1.4);  opacity: 1;    }
        20%       { transform: scale(1);    opacity: 1;    }
        32%       { transform: scale(1.25); opacity: 1;    }
        50%       { transform: scale(1);    opacity: 0.8;  }
      }
      .bb-heart          { animation: bb-heartbeat      1.6s ease-in-out infinite; display: inline-block; }
      .bb-heart-stressed { animation: bb-heartbeat-fast 0.9s ease-in-out infinite; display: inline-block; }
      .bb-heart-idle     { display: inline-block; opacity: 0.3; }
    `}</style>

    <div className="space-y-6">
      {/* ── Page header ──────────────────────────────────────────────────── */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark flex items-center gap-2">
            <Bot className="h-6 w-6 text-gold" />
            Black Bart Command Center
          </h2>
          <p className="text-leather-light text-sm mt-0.5">
            AI Game Master live activity — auto-refreshes every {POLL_INTERVAL_MS / 1000}s
          </p>
        </div>
        <div className="flex items-center gap-2">
          {/* ── Heartbeat indicator ──────────────────────────────────────── */}
          <div className="flex items-center gap-1.5 select-none">
            <Heart
              className={
                !killSwitchEnabled
                  ? 'bb-heart-idle h-12 w-12 text-slate-400'
                  : economyStatus === 'margin_risk'
                    ? 'bb-heart-stressed h-12 w-12 text-red-500 fill-red-500'
                    : secondsSinceRefresh < 5
                      ? 'bb-heart h-12 w-12 text-red-400 fill-red-400'
                      : 'bb-heart h-12 w-12 text-red-300 fill-red-300'
              }
            />
            <span className="text-xs text-leather-light">
              {!killSwitchEnabled
                ? 'AI sleeping'
                : secondsSinceRefresh < 5
                  ? 'Just updated'
                  : `Updated ${secondsSinceRefresh}s ago`}
            </span>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchLiveData(true)}
            disabled={isRefreshing}
            className="border-saddle-light/50 text-leather"
          >
            <RefreshCw className={`h-4 w-4 mr-1.5 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button
            size="sm"
            onClick={handleManualTrigger}
            disabled={isTriggering || !killSwitchEnabled}
            className="bg-gold hover:bg-gold-dark text-leather font-semibold"
          >
            <Zap className={`h-4 w-4 mr-1.5 ${isTriggering ? 'animate-pulse' : ''}`} />
            {isTriggering ? 'Running…' : 'Summon Black Bart'}
          </Button>
        </div>
      </div>

      {/* ── Alerts banner ────────────────────────────────────────────────── */}
      {alerts.length > 0 && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 flex gap-3">
          <AlertTriangle className="h-5 w-5 text-red-600 shrink-0 mt-0.5" />
          <div className="space-y-1">
            {alerts.map((alert, i) => (
              <p key={i} className="text-sm text-red-700 font-medium">{alert}</p>
            ))}
          </div>
        </div>
      )}

      {/* ── Top KPI row ───────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4">
        {/* Economy status */}
        <Card className={`border ${statusCfg.bg} col-span-1`}>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-leather-light uppercase tracking-wide">Economy</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <div className={`flex items-center gap-1.5 font-bold text-lg ${statusCfg.color}`}>
              <StatusIcon className="h-5 w-5" />
              {statusCfg.label}
            </div>
            <p className="text-xs text-leather-light mt-1">
              Ratio: {(economy?.data.supply_demand_ratio ?? 0).toFixed(1)}
            </p>
          </CardContent>
        </Card>

        {/* Hourly spend */}
        <Card className="border-saddle-light/30 col-span-1">
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-leather-light uppercase tracking-wide">Hourly Spend</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4 space-y-2">
            <div className="flex items-end justify-between">
              <span className="font-bold text-saddle-dark text-lg">${spendThisHour.toFixed(2)}</span>
              <span className="text-xs text-leather-light">/ ${SPEND_LIMIT_USD.toFixed(0)}</span>
            </div>
            <Progress
              value={spendPct}
              className="h-2"
            />
            <p className="text-xs text-leather-light">
              {spendPct >= 100 ? '🛑 Limit reached' : `$${(SPEND_LIMIT_USD - spendThisHour).toFixed(2)} remaining`}
            </p>
          </CardContent>
        </Card>

        {/* Actions today */}
        <Card className="border-saddle-light/30 col-span-1">
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-leather-light uppercase tracking-wide">Actions Today</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <div className="text-2xl font-bold text-saddle-dark">
              {economy?.data.ai_actions_today ?? stats.actionsToday}
            </div>
            <p className="text-xs text-green-600 mt-1">
              {stats.successRate}% success rate
            </p>
          </CardContent>
        </Card>

        {/* Coins spawned / active */}
        <Card className="border-saddle-light/30 col-span-1">
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-leather-light uppercase tracking-wide">Coins</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <div className="text-2xl font-bold text-saddle-dark">
              {economy?.data.active_coins_total ?? stats.activeCoinsTotal}
            </div>
            <p className="text-xs text-leather-light mt-1">
              {economy?.data.coins_spawned_today ?? stats.coinsSpawnedToday} spawned today
            </p>
          </CardContent>
        </Card>

        {/* Kill switch */}
        <Card className={`col-span-2 lg:col-span-1 border-2 ${killSwitchEnabled ? 'border-green-300 bg-green-50' : 'border-red-300 bg-red-50'}`}>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-leather-light uppercase tracking-wide">Auto-Spawn</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4 space-y-3">
            <div className={`flex items-center gap-2 font-bold ${killSwitchEnabled ? 'text-green-700' : 'text-red-700'}`}>
              <div className={`h-2.5 w-2.5 rounded-full ${killSwitchEnabled ? 'bg-green-500 animate-pulse' : 'bg-red-500'}`} />
              {killSwitchEnabled ? 'ENABLED' : 'DISABLED'}
            </div>
            <Button
              size="sm"
              variant="outline"
              className={`w-full text-xs font-semibold ${killSwitchEnabled
                ? 'border-red-300 text-red-700 hover:bg-red-100'
                : 'border-green-300 text-green-700 hover:bg-green-100'}`}
              onClick={handleKillSwitch}
              disabled={isTogglingKillSwitch}
            >
              {isTogglingKillSwitch ? '…' : killSwitchEnabled ? '🛑 Disable' : '▶ Enable'}
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* ── Hunt pressure grid + Economy details ─────────────────────────── */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Hunt pressure zones (2/3 width) */}
        <div className="lg:col-span-2 space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold text-saddle-dark flex items-center gap-2">
              <Flame className="h-4 w-4 text-gold" />
              Hunt Pressure by Zone
            </h3>
            <div className="flex items-center gap-3 text-xs text-leather-light">
              <span className="flex items-center gap-1"><span className="h-2 w-2 rounded-full bg-red-500 inline-block" /> Hot ≥5</span>
              <span className="flex items-center gap-1"><span className="h-2 w-2 rounded-full bg-amber-500 inline-block" /> Warm ≥3</span>
              <span className="flex items-center gap-1"><span className="h-2 w-2 rounded-full bg-green-500 inline-block" /> Cool ≥1</span>
            </div>
          </div>

          {pressure?.data.zones && pressure.data.zones.length > 0 ? (
            <div className="grid gap-3 sm:grid-cols-2">
              {pressure.data.zones.map(zone => (
                <Card key={zone.zone_id} className={`border ${pressureBg(zone.hunt_pressure)}`}>
                  <CardContent className="p-4">
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1 min-w-0">
                        <p className="font-semibold text-saddle-dark text-sm truncate">{zone.zone_name}</p>
                        <p className="text-xs text-leather-light capitalize">{zone.zone_type} zone</p>
                      </div>
                      <div className={`text-2xl font-black tabular-nums ${pressureColor(zone.hunt_pressure)}`}>
                        {zone.hunt_pressure.toFixed(1)}
                      </div>
                    </div>

                    <div className="mt-3 grid grid-cols-3 gap-2 text-center">
                      <div className="rounded bg-white/60 px-2 py-1">
                        <p className="text-xs text-leather-light">Players</p>
                        <p className="font-bold text-saddle-dark text-sm">{zone.active_player_count}</p>
                      </div>
                      <div className="rounded bg-white/60 px-2 py-1">
                        <p className="text-xs text-leather-light">Coins</p>
                        <p className="font-bold text-saddle-dark text-sm">{zone.active_coin_count}</p>
                      </div>
                      <div className="rounded bg-white/60 px-2 py-1">
                        <p className="text-xs text-leather-light">Spawn</p>
                        <p className={`font-bold text-sm capitalize ${tierBadge(zone.recommended_spawn_tier).includes('gold') ? 'text-saddle-dark' : ''}`}>
                          {zone.recommended_spawn_tier}
                        </p>
                      </div>
                    </div>

                    {zone.needs_spawn && (
                      <div className="mt-2 rounded bg-amber-100 border border-amber-200 px-2 py-1 flex items-center gap-1.5">
                        <Flame className="h-3 w-3 text-amber-600" />
                        <span className="text-xs font-medium text-amber-700">
                          Needs {zone.coins_to_spawn} coin{zone.coins_to_spawn !== 1 ? 's' : ''}
                        </span>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <Card className="border-saddle-light/30">
              <CardContent className="py-12 text-center text-leather-light">
                <Snowflake className="h-8 w-8 mx-auto mb-3 opacity-40" />
                <p className="font-medium">No active zones with players</p>
                <p className="text-sm mt-1">Zones appear here when players are online</p>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Economy health panel (1/3 width) */}
        <div className="space-y-4">
          <h3 className="font-semibold text-saddle-dark flex items-center gap-2">
            <BarChart3 className="h-4 w-4 text-gold" />
            Economy Health
          </h3>

          <Card className="border-saddle-light/30">
            <CardContent className="p-4 space-y-4">
              {economy ? (
                <>
                  {/* Supply/Demand */}
                  <div>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-leather-light">Supply / Demand</span>
                      <span className={`font-bold ${economyStatus === 'healthy' ? 'text-green-600' : economyStatus === 'margin_risk' ? 'text-red-600' : 'text-amber-600'}`}>
                        {economy.data.supply_demand_ratio.toFixed(2)}
                      </span>
                    </div>
                    <Progress value={Math.min((economy.data.supply_demand_ratio / 3) * 100, 100)} className="h-2" />
                    <p className="text-xs text-leather-light mt-1">Healthy range: 0.8 – 2.5</p>
                  </div>

                  <Separator className="bg-saddle-light/20" />

                  {/* Today's numbers */}
                  <div className="space-y-2 text-sm">
                    {[
                      { label: 'Spawned today', value: economy.data.coins_spawned_today, unit: 'coins' },
                      { label: 'Collected today', value: economy.data.coins_collected_today, unit: 'coins' },
                      { label: 'Recycled today', value: economy.data.coins_recycled_today, unit: 'coins' },
                    ].map(row => (
                      <div key={row.label} className="flex justify-between">
                        <span className="text-leather-light">{row.label}</span>
                        <span className="font-medium text-saddle-dark">{row.value} {row.unit}</span>
                      </div>
                    ))}
                  </div>

                  <Separator className="bg-saddle-light/20" />

                  {/* Financial */}
                  <div className="space-y-2 text-sm">
                    {[
                      { label: 'Value spawned', value: `$${economy.data.value_spawned_today_usd.toFixed(2)}`, positive: null },
                      { label: 'Gas revenue', value: `$${economy.data.gas_revenue_today_usd.toFixed(2)}`, positive: true },
                      { label: 'Net margin', value: `$${economy.data.net_margin_today_usd.toFixed(2)}`, positive: economy.data.net_margin_today_usd >= 0 },
                    ].map(row => (
                      <div key={row.label} className="flex justify-between">
                        <span className="text-leather-light">{row.label}</span>
                        <span className={`font-medium ${row.positive === null ? 'text-saddle-dark' : row.positive ? 'text-green-600' : 'text-red-600'}`}>
                          {row.value}
                        </span>
                      </div>
                    ))}
                  </div>

                  <Separator className="bg-saddle-light/20" />

                  {/* Recommended action */}
                  <div className={`rounded-md px-3 py-2 text-xs font-medium ${statusCfg.bg} ${statusCfg.color} border`}>
                    {economy.meta.recommended_action}
                  </div>
                </>
              ) : (
                <div className="py-8 text-center text-leather-light">
                  <Activity className="h-6 w-6 mx-auto mb-2 opacity-40 animate-pulse" />
                  <p className="text-sm">Loading economy data…</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Active players summary */}
          <Card className="border-saddle-light/30">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-gold/10 flex items-center justify-center">
                  <Users className="h-5 w-5 text-gold" />
                </div>
                <div>
                  <p className="text-2xl font-bold text-saddle-dark">{totalActivePlayers}</p>
                  <p className="text-xs text-leather-light">Active hunters right now</p>
                </div>
              </div>
              {zonesNeedingSpawn > 0 && (
                <div className="mt-3 text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded px-2 py-1.5">
                  🔥 {zonesNeedingSpawn} zone{zonesNeedingSpawn !== 1 ? 's' : ''} need{zonesNeedingSpawn === 1 ? 's' : ''} a coin drop
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* ── Action feed ───────────────────────────────────────────────────── */}
      <Card className="border-saddle-light/30">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <Activity className="h-5 w-5 text-gold" />
              What Black Bart Did Today
            </CardTitle>
            <CardDescription className="text-xs">
              {recentActions.length} actions shown · auto-updates every {POLL_INTERVAL_MS / 1000}s
            </CardDescription>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {recentActions.length === 0 ? (
            <div className="py-12 text-center text-leather-light">
              <Bot className="h-8 w-8 mx-auto mb-3 opacity-30" />
              <p className="font-medium">No AI actions yet today</p>
              <p className="text-sm mt-1">Black Bart&apos;s decisions will appear here in real time</p>
            </div>
          ) : (
            <div className="divide-y divide-saddle-light/15">
              {recentActions.map((action, idx) => {
                const agent = agentConfig(action.agent_id)
                const isCycleSummary = action.tool_called === 'spawn_governor_cycle'

                return (
                  <div
                    key={action.id}
                    className={`px-5 py-3.5 flex items-start gap-3 hover:bg-parchment/40 transition-colors ${idx === 0 ? 'bg-gold/5' : ''}`}
                  >
                    {/* Success/fail icon */}
                    <div className="mt-0.5 shrink-0">
                      {action.success
                        ? <CheckCircle2 className="h-4 w-4 text-green-500" />
                        : <XCircle className="h-4 w-4 text-red-400" />
                      }
                    </div>

                    {/* Content */}
                    <div className="flex-1 min-w-0 space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        {/* Agent badge */}
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border ${agent.badge}`}>
                          {agent.label}
                        </span>

                        {/* Tool name */}
                        <code className="text-xs bg-parchment px-1.5 py-0.5 rounded text-leather font-mono">
                          {action.tool_called}
                        </code>

                        {/* Cycle summary badge */}
                        {isCycleSummary && (
                          <Badge variant="outline" className="text-xs border-gold/30 text-saddle-dark">
                            cycle
                          </Badge>
                        )}

                        {/* Error code */}
                        {action.error_code && (
                          <Badge variant="outline" className="text-xs border-red-200 text-red-600">
                            {action.error_code}
                          </Badge>
                        )}
                      </div>

                      {/* Reasoning */}
                      {action.reasoning && (
                        <p className="text-sm text-leather truncate" title={action.reasoning}>
                          {action.reasoning}
                        </p>
                      )}

                      {/* Cycle result summary */}
                      {isCycleSummary && action.result && (
                        <p className="text-xs text-leather-light">
                          {(action.result as Record<string, unknown>).coins_spawned as number ?? 0} spawned ·{' '}
                          {(action.result as Record<string, unknown>).coins_recycled as number ?? 0} recycled ·{' '}
                          {((action.result as Record<string, unknown>).duration_ms as number ?? 0)}ms
                        </p>
                      )}
                    </div>

                    {/* Right side: cost + time */}
                    <div className="shrink-0 text-right space-y-1">
                      {action.cost_usd > 0 && (
                        <p className="text-xs font-semibold text-saddle-dark">
                          ${action.cost_usd.toFixed(2)}
                        </p>
                      )}
                      <p className="text-xs text-leather-light flex items-center gap-1 justify-end">
                        <Clock className="h-3 w-3" />
                        {timeAgo(action.created_at)}
                      </p>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* ── Footer hint ───────────────────────────────────────────────────── */}
      <div className="text-xs text-leather-light text-center pb-2">
        <DollarSign className="h-3 w-3 inline mr-1" />
        Autonomous spend limit: ${SPEND_LIMIT_USD}/hr ·
        <Coins className="h-3 w-3 inline mx-1" />
        Single-spawn approval threshold: $50 ·
        <Activity className="h-3 w-3 inline mx-1" />
        Governor cycle: every 5 minutes
      </div>
    </div>
    </>
  )
}
