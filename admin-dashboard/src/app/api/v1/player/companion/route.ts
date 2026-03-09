import { NextRequest, NextResponse } from 'next/server'
import { createPublicClient, createServiceRoleClient } from '@/lib/supabase/server'
import {
  buildEventResponse,
  buildIntentResponse,
  buildStartSessionResponse,
  type CompanionCoinContext,
  type CompanionHiderContext,
  type CompanionPlayerContext,
} from '@/lib/companion/companion-engine'
import {
  COMPANION_QUICK_PROMPTS,
  getQuickPromptDefinition,
  type CompanionIntentType,
} from '@/lib/companion/quick-prompts'
import { logCompanionAction } from '@/lib/companion/companion-logging'

export const dynamic = 'force-dynamic'

type CompanionAction = 'start_session' | 'submit_intent' | 'report_event'

interface BaseCompanionRequest {
  action?: CompanionAction
  companionSessionId?: string
  selectedCoinId?: string | null
  latitude?: number | null
  longitude?: number | null
  currentZoneId?: string | null
  currentCellL17?: string | null
}

interface StartSessionRequest extends BaseCompanionRequest {
  action: 'start_session'
}

interface SubmitIntentRequest extends BaseCompanionRequest {
  action: 'submit_intent'
  intentType?: string
  distanceToCoinMeters?: number | null
}

interface ReportEventRequest extends BaseCompanionRequest {
  action: 'report_event'
  eventType?: string
  messageId?: string | null
  coinId?: string | null
  distanceToCoinMeters?: number | null
  payload?: Record<string, unknown> | null
}

type CompanionRequest = StartSessionRequest | SubmitIntentRequest | ReportEventRequest

interface AuthenticatedPlayer {
  id: string
  displayName: string | null
}

async function getAuthenticatedPlayer(request: NextRequest): Promise<AuthenticatedPlayer | null> {
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

async function getPlayerContext(
  userId: string,
  fallback?: Pick<BaseCompanionRequest, 'latitude' | 'longitude' | 'currentZoneId' | 'currentCellL17'>,
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

async function getSelectedCoinContext(selectedCoinId?: string | null): Promise<CompanionCoinContext | null> {
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

async function getHiderContext(hiderId: string | null): Promise<CompanionHiderContext | null> {
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

function unauthorized(message = 'Missing or invalid authorization token') {
  return NextResponse.json({ success: false, error: message }, { status: 401 })
}

export async function POST(request: NextRequest) {
  const authenticatedPlayer = await getAuthenticatedPlayer(request)
  if (!authenticatedPlayer) return unauthorized()

  let body: CompanionRequest
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  if (!body.action) {
    return NextResponse.json({ success: false, error: 'action is required' }, { status: 400 })
  }

  try {
    const playerContext = await getPlayerContext(authenticatedPlayer.id, body)
    playerContext.display_name = authenticatedPlayer.displayName

    if (body.action === 'start_session') {
      const selectedCoin = await getSelectedCoinContext(body.selectedCoinId)
      const pack = buildStartSessionResponse({
        player: playerContext,
        selectedCoin,
      })
      const companionSessionId = crypto.randomUUID()

      await logCompanionAction({
        tool_called: 'player_companion_session_start',
        player_id: authenticatedPlayer.id,
        parameters: {
          companion_session_id: companionSessionId,
          selected_coin_id: selectedCoin?.id ?? null,
        },
        reasoning: 'Started Black Bart companion session for active hunt.',
        result: {
          reply_now: pack.reply_now?.message_text ?? null,
          candidate_count: pack.candidate_messages.length,
        },
      })

      return NextResponse.json({
        success: true,
        data: {
          companion_session_id: companionSessionId,
          quick_prompts: COMPANION_QUICK_PROMPTS,
          ...pack,
        },
        timestamp: new Date().toISOString(),
      })
    }

    if (body.action === 'submit_intent') {
      if (!body.intentType || !getQuickPromptDefinition(body.intentType)) {
        return NextResponse.json({ success: false, error: 'intentType must be a supported quick prompt' }, { status: 400 })
      }

      const selectedCoin = await getSelectedCoinContext(body.selectedCoinId)
      const hider = await getHiderContext(selectedCoin?.hider_id ?? null)
      const pack = buildIntentResponse({
        intentType: body.intentType as CompanionIntentType,
        player: playerContext,
        selectedCoin,
        hider,
        distanceToCoinMeters: body.distanceToCoinMeters ?? null,
      })

      await logCompanionAction({
        tool_called: 'player_companion_reply',
        player_id: authenticatedPlayer.id,
        parameters: {
          companion_session_id: body.companionSessionId ?? null,
          intent_type: body.intentType,
          selected_coin_id: selectedCoin?.id ?? null,
          distance_to_coin_meters: body.distanceToCoinMeters ?? null,
        },
        reasoning: `Black Bart replied to quick prompt "${body.intentType}".`,
        result: {
          reply_now: pack.reply_now?.message_text ?? null,
          risk_level: pack.meta.risk_level,
          recommended_action: pack.meta.recommended_action,
          candidate_triggers: pack.candidate_messages.map(candidate => ({
            trigger_type: candidate.trigger_type,
            trigger_value: candidate.trigger_value,
          })),
        },
      })

      return NextResponse.json({
        success: true,
        data: {
          companion_session_id: body.companionSessionId ?? null,
          ...pack,
        },
        timestamp: new Date().toISOString(),
      })
    }

    if (body.action === 'report_event') {
      if (!body.eventType || typeof body.eventType !== 'string') {
        return NextResponse.json({ success: false, error: 'eventType is required' }, { status: 400 })
      }

      const selectedCoin = await getSelectedCoinContext(body.coinId ?? body.selectedCoinId)
      const pack = buildEventResponse({
        eventType: body.eventType,
        selectedCoin,
      })

      await logCompanionAction({
        tool_called: 'player_companion_event',
        player_id: authenticatedPlayer.id,
        parameters: {
          companion_session_id: body.companionSessionId ?? null,
          event_type: body.eventType,
          message_id: body.messageId ?? null,
          coin_id: selectedCoin?.id ?? null,
          distance_to_coin_meters: body.distanceToCoinMeters ?? null,
          payload: body.payload ?? null,
        },
        reasoning: `Recorded companion event "${body.eventType}".`,
        result: pack
          ? {
              reply_now: pack.reply_now?.message_text ?? null,
              recommended_action: pack.meta.recommended_action,
            }
          : { acknowledged: true },
      })

      return NextResponse.json({
        success: true,
        data: {
          acknowledged: true,
          companion_session_id: body.companionSessionId ?? null,
          response_pack: pack,
        },
        timestamp: new Date().toISOString(),
      })
    }

    return NextResponse.json({ success: false, error: `Unsupported action: ${body.action}` }, { status: 400 })
  } catch (error) {
    console.error('[player/companion] Error:', error)
    return NextResponse.json(
      {
        success: false,
        error: 'Internal server error',
        details: error instanceof Error ? error.message : String(error),
      },
      { status: 500 },
    )
  }
}
