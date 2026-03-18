import type { NextRequest } from 'next/server'
import { createPublicClient, createServiceRoleClient } from '@/lib/supabase/server'
import type {
  CompanionCoinContext,
  CompanionHiderContext,
  CompanionPlayerContext,
} from '@/lib/companion/companion-engine'
import type {
  AuthenticatedPlayer,
  BlackBartLocalHuntPressureSummary,
  BlackBartRecentCompanionAction,
} from '@/lib/black-bart/types'

interface PlayerContextFallback {
  latitude?: number | null
  longitude?: number | null
  currentZoneId?: string | null
  currentCellL17?: string | null
}

export async function getAuthenticatedPlayer(request: NextRequest): Promise<AuthenticatedPlayer | null> {
  const authHeader = request.headers.get('Authorization')
  if (!authHeader || !authHeader.startsWith('Bearer ')) return null

  const token = authHeader.replace('Bearer ', '')
  if (!token) return null

  const publicClient = createPublicClient()
  const serviceClient = createServiceRoleClient()
  const { data: authData, error: authError } = await publicClient.auth.getUser(token)
  if (authError || !authData.user) return null

  const { data: profile } = await serviceClient
    .from('profiles')
    .select('full_name')
    .eq('id', authData.user.id)
    .maybeSingle()

  return {
    id: authData.user.id,
    displayName: profile?.full_name ?? null,
  }
}

export async function getPlayerContext(
  userId: string,
  fallback?: PlayerContextFallback,
): Promise<CompanionPlayerContext> {
  const serviceClient = createServiceRoleClient()

  const { data: location } = await serviceClient
    .from('player_locations')
    .select('latitude, longitude, current_zone_id, s2_cell_token_l17')
    .eq('user_id', userId)
    .maybeSingle()

  return {
    user_id: userId,
    display_name: null,
    latitude: location?.latitude ?? fallback?.latitude ?? null,
    longitude: location?.longitude ?? fallback?.longitude ?? null,
    current_zone_id: location?.current_zone_id ?? fallback?.currentZoneId ?? null,
    current_cell_l17: location?.s2_cell_token_l17 ?? fallback?.currentCellL17 ?? null,
  }
}

export async function getSelectedCoinContext(selectedCoinId?: string | null): Promise<CompanionCoinContext | null> {
  if (!selectedCoinId) return null

  const serviceClient = createServiceRoleClient()
  const { data: coin } = await serviceClient
    .from('coins')
    .select(`
      id,
      coin_type,
      value,
      tier,
      latitude,
      longitude,
      status,
      hider_id,
      location_name,
      description,
      created_by,
      metadata
    `)
    .eq('id', selectedCoinId)
    .maybeSingle()

  if (!coin) return null

  return {
    id: coin.id,
    coin_type: coin.coin_type,
    value: coin.value,
    tier: coin.tier,
    latitude: coin.latitude,
    longitude: coin.longitude,
    status: coin.status,
    hider_id: coin.hider_id,
    location_name: coin.location_name,
    description: coin.description,
    created_by: coin.created_by ?? 'system',
    metadata: coin.metadata,
  }
}

export async function getHiderContext(hiderId: string | null): Promise<CompanionHiderContext | null> {
  if (!hiderId) return null

  const serviceClient = createServiceRoleClient()
  const [{ data: profile }, { count: activeHiddenCount }, { count: hiddenTransactionCount }] = await Promise.all([
    serviceClient
      .from('profiles')
      .select('full_name')
      .eq('id', hiderId)
      .maybeSingle(),
    serviceClient
      .from('coins')
      .select('*', { count: 'exact', head: true })
      .eq('hider_id', hiderId)
      .in('status', ['hidden', 'visible']),
    serviceClient
      .from('transactions')
      .select('*', { count: 'exact', head: true })
      .eq('user_id', hiderId)
      .eq('transaction_type', 'hidden'),
  ])

  return {
    id: hiderId,
    display_name: profile?.full_name ?? null,
    active_hidden_count: activeHiddenCount ?? 0,
    hidden_transaction_count: hiddenTransactionCount ?? 0,
  }
}

export async function getLocalHuntPressureSummary(
  currentCellL17: string | null,
  activeWindowMinutes = 30,
): Promise<BlackBartLocalHuntPressureSummary | null> {
  if (!currentCellL17) return null

  const serviceClient = createServiceRoleClient()
  const cutoff = new Date(Date.now() - activeWindowMinutes * 60 * 1000).toISOString()

  const [{ count: activePlayerCount }, { count: activeCoinCount }] = await Promise.all([
    serviceClient
      .from('player_locations')
      .select('*', { count: 'exact', head: true })
      .eq('s2_cell_token_l17', currentCellL17)
      .gte('updated_at', cutoff),
    serviceClient
      .from('coins')
      .select('*', { count: 'exact', head: true })
      .eq('s2_cell_token_l17', currentCellL17)
      .in('status', ['hidden', 'visible']),
  ])

  const players = activePlayerCount ?? 0
  const coins = activeCoinCount ?? 0

  return {
    cellId: currentCellL17,
    activeWindowMinutes,
    activePlayerCount: players,
    activeCoinCount: coins,
    huntPressure: parseFloat((players / Math.max(coins, 1)).toFixed(2)),
  }
}

export async function getRecentCompanionHistory(
  playerId: string,
  limit = 5,
): Promise<BlackBartRecentCompanionAction[]> {
  const serviceClient = createServiceRoleClient()
  const { data } = await serviceClient
    .from('ai_actions')
    .select('id, tool_called, created_at, parameters, result')
    .eq('agent_id', 'ai_game_master')
    .contains('parameters', { player_id: playerId })
    .like('tool_called', 'player_companion_%')
    .order('created_at', { ascending: false })
    .limit(limit)

  return (data ?? []).map((row) => {
    const parameters = (row.parameters ?? {}) as Record<string, unknown>
    const result = (row.result ?? {}) as Record<string, unknown>

    return {
      id: row.id,
      toolCalled: row.tool_called,
      createdAt: row.created_at,
      intentType: typeof parameters.intent_type === 'string' ? parameters.intent_type : null,
      eventType: typeof parameters.event_type === 'string' ? parameters.event_type : null,
      replyNow: typeof result.reply_now === 'string' ? result.reply_now : null,
    }
  })
}
