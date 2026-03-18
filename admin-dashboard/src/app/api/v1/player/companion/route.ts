import { NextRequest, NextResponse } from 'next/server'
import {
  COMPANION_QUICK_PROMPTS,
  getQuickPromptDefinition,
  type CompanionIntentType,
} from '@/lib/companion/quick-prompts'
import {
  getAuthenticatedPlayer,
  getHiderContext,
  getLocalHuntPressureSummary,
  getPlayerContext,
  getRecentCompanionHistory,
  getSelectedCoinContext,
} from '@/lib/black-bart/context'
import { generateBlackBartCompanionResponse } from '@/lib/black-bart/runtime'
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

  const requestedAction = body.action

  try {
    const playerContext = await getPlayerContext(authenticatedPlayer.id, body)
    playerContext.display_name = authenticatedPlayer.displayName
    const [localHuntPressure, recentCompanionHistory] = await Promise.all([
      getLocalHuntPressureSummary(playerContext.current_cell_l17),
      getRecentCompanionHistory(authenticatedPlayer.id),
    ])

    if (requestedAction === 'start_session') {
      const selectedCoin = await getSelectedCoinContext(body.selectedCoinId)
      const runtimeResult = await generateBlackBartCompanionResponse({
        action: 'start_session',
        player: playerContext,
        selectedCoin,
        localHuntPressure,
        recentCompanionHistory,
      })
      const pack = runtimeResult.responsePack
      if (!pack) {
        throw new Error('Black Bart runtime returned no response for start_session')
      }
      const companionSessionId = crypto.randomUUID()

      await logCompanionAction({
        tool_called: 'player_companion_session_start',
        player_id: authenticatedPlayer.id,
        parameters: {
          companion_session_id: companionSessionId,
          selected_coin_id: selectedCoin?.id ?? null,
          runtime_source: runtimeResult.runtimeMeta.source,
          system_prompt_version: runtimeResult.runtimeMeta.systemPromptVersion,
          local_pressure: localHuntPressure,
          recent_companion_history_count: recentCompanionHistory.length,
        },
        reasoning: 'Started Black Bart companion session for active hunt.',
        result: {
          reply_now: pack.reply_now?.message_text ?? null,
          candidate_count: pack.candidate_messages.length,
          runtime_source: runtimeResult.runtimeMeta.source,
          local_pressure: localHuntPressure,
          recent_companion_history_count: recentCompanionHistory.length,
          situation_summary: runtimeResult.runtimeMeta.promptContext.situationSummary,
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

    if (requestedAction === 'submit_intent') {
      if (!body.intentType || !getQuickPromptDefinition(body.intentType)) {
        return NextResponse.json({ success: false, error: 'intentType must be a supported quick prompt' }, { status: 400 })
      }

      const selectedCoin = await getSelectedCoinContext(body.selectedCoinId)
      const hider = await getHiderContext(selectedCoin?.hider_id ?? null)
      const runtimeResult = await generateBlackBartCompanionResponse({
        action: 'submit_intent',
        player: playerContext,
        selectedCoin,
        hider,
        intentType: body.intentType as CompanionIntentType,
        distanceToCoinMeters: body.distanceToCoinMeters ?? null,
        localHuntPressure,
        recentCompanionHistory,
      })
      const pack = runtimeResult.responsePack
      if (!pack) {
        throw new Error('Black Bart runtime returned no response for submit_intent')
      }

      await logCompanionAction({
        tool_called: 'player_companion_reply',
        player_id: authenticatedPlayer.id,
        parameters: {
          companion_session_id: body.companionSessionId ?? null,
          intent_type: body.intentType,
          selected_coin_id: selectedCoin?.id ?? null,
          distance_to_coin_meters: body.distanceToCoinMeters ?? null,
          runtime_source: runtimeResult.runtimeMeta.source,
          system_prompt_version: runtimeResult.runtimeMeta.systemPromptVersion,
          local_pressure: localHuntPressure,
          recent_companion_history_count: recentCompanionHistory.length,
        },
        reasoning: `Black Bart replied to quick prompt "${body.intentType}".`,
        result: {
          reply_now: pack.reply_now?.message_text ?? null,
          risk_level: pack.meta.risk_level,
          recommended_action: pack.meta.recommended_action,
          runtime_source: runtimeResult.runtimeMeta.source,
          local_pressure: localHuntPressure,
          recent_companion_history_count: recentCompanionHistory.length,
          situation_summary: runtimeResult.runtimeMeta.promptContext.situationSummary,
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

    if (requestedAction === 'report_event') {
      if (!body.eventType || typeof body.eventType !== 'string') {
        return NextResponse.json({ success: false, error: 'eventType is required' }, { status: 400 })
      }

      const selectedCoin = await getSelectedCoinContext(body.coinId ?? body.selectedCoinId)
      const runtimeResult = await generateBlackBartCompanionResponse({
        action: 'report_event',
        selectedCoin,
        eventType: body.eventType,
        localHuntPressure,
        recentCompanionHistory,
      })
      const pack = runtimeResult.responsePack

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
          runtime_source: runtimeResult.runtimeMeta.source,
          system_prompt_version: runtimeResult.runtimeMeta.systemPromptVersion,
          local_pressure: localHuntPressure,
          recent_companion_history_count: recentCompanionHistory.length,
        },
        reasoning: `Recorded companion event "${body.eventType}".`,
        result: pack
          ? {
              reply_now: pack.reply_now?.message_text ?? null,
              recommended_action: pack.meta.recommended_action,
              runtime_source: runtimeResult.runtimeMeta.source,
              local_pressure: localHuntPressure,
              recent_companion_history_count: recentCompanionHistory.length,
              situation_summary: runtimeResult.runtimeMeta.promptContext.situationSummary,
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

    return NextResponse.json({ success: false, error: `Unsupported action: ${requestedAction}` }, { status: 400 })
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
