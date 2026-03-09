import {
  buildMessageId,
  type CompanionCandidateMessage,
  type CompanionIntentType,
  type CompanionRecommendedAction,
  type CompanionReplyNow,
  type CompanionResponsePack,
  type CompanionRiskLevel,
  toIsoExpiry,
} from '@/lib/companion/quick-prompts'

export interface CompanionCoinContext {
  id: string
  coin_type: 'fixed' | 'pool'
  value: number
  tier: string | null
  latitude: number
  longitude: number
  status: string
  hider_id: string | null
  location_name: string | null
  description: string | null
  created_by: string
  metadata?: Record<string, unknown> | null
}

export interface CompanionPlayerContext {
  user_id: string
  display_name: string | null
  latitude: number | null
  longitude: number | null
  current_zone_id: string | null
  current_cell_l17: string | null
}

export interface CompanionHiderContext {
  id: string
  display_name: string | null
  active_hidden_count: number
  hidden_transaction_count: number
}

export interface BuildStartSessionInput {
  player: CompanionPlayerContext
  selectedCoin: CompanionCoinContext | null
}

export interface BuildIntentReplyInput {
  intentType: CompanionIntentType
  player: CompanionPlayerContext
  selectedCoin: CompanionCoinContext | null
  hider: CompanionHiderContext | null
  distanceToCoinMeters: number | null
}

export interface BuildEventResponseInput {
  eventType: string
  selectedCoin: CompanionCoinContext | null
}

const HOSTILE_KEYWORDS = [
  'bomb',
  'trap',
  'curse',
  'mimic',
  'gas leak',
  'danger',
  'hostile',
] as const

function hasHostileSignal(selectedCoin: CompanionCoinContext | null): boolean {
  if (!selectedCoin) return false

  const description = selectedCoin.description?.toLowerCase() ?? ''
  const metadataBlob = JSON.stringify(selectedCoin.metadata ?? {}).toLowerCase()

  if (selectedCoin.metadata && typeof selectedCoin.metadata.hostile === 'boolean' && selectedCoin.metadata.hostile) {
    return true
  }

  return HOSTILE_KEYWORDS.some(keyword => description.includes(keyword) || metadataBlob.includes(keyword))
}

function deriveRiskLevel(
  selectedCoin: CompanionCoinContext | null,
  hider: CompanionHiderContext | null,
): CompanionRiskLevel {
  if (!selectedCoin) return 'low'

  if (hasHostileSignal(selectedCoin)) return 'high'

  if (selectedCoin.coin_type === 'pool') return 'medium'

  if (hider && (hider.hidden_transaction_count >= 10 || hider.active_hidden_count >= 5)) {
    return 'medium'
  }

  return 'low'
}

function buildReply(
  messageType: CompanionReplyNow['message_type'],
  messageText: string,
  priority: number,
): CompanionReplyNow {
  return {
    message_id: buildMessageId(messageType),
    message_type: messageType,
    message_text: messageText,
    voice_text: null,
    priority,
    tap_action: 'none',
    expires_at: toIsoExpiry(15),
  }
}

function buildRiskCandidates(riskLevel: CompanionRiskLevel): CompanionCandidateMessage[] {
  if (riskLevel === 'high') {
    return [
      {
        message_id: buildMessageId('distance200'),
        trigger_type: 'distance_under_meters',
        trigger_value: 200,
        message_text: 'You are closing in now, partner. Keep those eyes sharp.',
        voice_text: null,
        priority: 65,
      },
      {
        message_id: buildMessageId('distance75'),
        trigger_type: 'distance_under_meters',
        trigger_value: 75,
        message_text: 'That hand feels crooked to me. Inspect before you grab.',
        voice_text: null,
        priority: 80,
      },
      {
        message_id: buildMessageId('collectSuccess'),
        trigger_type: 'coin_collected_success',
        trigger_value: null,
        message_text: 'Well done, partner. You handled that one with sense.',
        voice_text: null,
        priority: 70,
      },
      {
        message_id: buildMessageId('collectFail'),
        trigger_type: 'coin_collection_failed',
        trigger_value: null,
        message_text: 'No shame in backing off a crooked trail. We will find another.',
        voice_text: null,
        priority: 70,
      },
    ]
  }

  if (riskLevel === 'medium') {
    return [
      {
        message_id: buildMessageId('distance200'),
        trigger_type: 'distance_under_meters',
        trigger_value: 200,
        message_text: 'Steady now. This one may take a careful hand.',
        voice_text: null,
        priority: 55,
      },
      {
        message_id: buildMessageId('distance75'),
        trigger_type: 'distance_under_meters',
        trigger_value: 75,
        message_text: 'Close enough now to trust your eyes more than your luck.',
        voice_text: null,
        priority: 65,
      },
      {
        message_id: buildMessageId('collectSuccess'),
        trigger_type: 'coin_collected_success',
        trigger_value: null,
        message_text: 'Good work, friend. That one was worth the walk.',
        voice_text: null,
        priority: 60,
      },
    ]
  }

  return [
    {
      message_id: buildMessageId('distance200'),
      trigger_type: 'distance_under_meters',
      trigger_value: 200,
      message_text: 'Trail looks clean so far. Keep moving, partner.',
      voice_text: null,
      priority: 45,
    },
    {
      message_id: buildMessageId('collectSuccess'),
      trigger_type: 'coin_collected_success',
      trigger_value: null,
      message_text: 'Well done, partner.',
      voice_text: null,
      priority: 55,
    },
  ]
}

function describeCoin(selectedCoin: CompanionCoinContext): string {
  const location = selectedCoin.location_name ? ` near ${selectedCoin.location_name}` : ''
  return `${selectedCoin.coin_type} coin${location}`
}

function describeHider(hider: CompanionHiderContext | null): string {
  if (!hider) return 'an unknown hand'
  if (hider.display_name) return hider.display_name
  return 'a seasoned hand'
}

function buildRecommendedAction(riskLevel: CompanionRiskLevel, hasCoin: boolean): CompanionRecommendedAction {
  if (!hasCoin) return 'wait_for_a_target'
  if (riskLevel === 'high') return 'inspect_before_collecting'
  if (riskLevel === 'medium') return 'continue_with_caution'
  return 'continue_hunt'
}

export function buildStartSessionResponse(input: BuildStartSessionInput): CompanionResponsePack {
  const greeting = input.player.display_name
    ? `Evenin', ${input.player.display_name}. I am ridin' with you now.`
    : 'Evenin\', partner. I am ridin\' with you now.'

  return {
    reply_now: buildReply('greeting', greeting, 35),
    candidate_messages: input.selectedCoin
      ? [
          {
            message_id: buildMessageId('distance200'),
            trigger_type: 'distance_under_meters',
            trigger_value: 200,
            message_text: 'You have a trail ahead of you. Keep your eyes up.',
            voice_text: null,
            priority: 40,
          },
        ]
      : [],
    meta: {
      risk_level: 'low',
      recommended_action: input.selectedCoin ? 'continue_hunt' : 'wait_for_a_target',
      selected_coin_id: input.selectedCoin?.id ?? null,
    },
  }
}

export function buildIntentResponse(input: BuildIntentReplyInput): CompanionResponsePack {
  const riskLevel = deriveRiskLevel(input.selectedCoin, input.hider)
  const recommendedAction = buildRecommendedAction(riskLevel, !!input.selectedCoin)

  if (!input.selectedCoin) {
    return {
      reply_now: buildReply(
        'hint',
        'Point me toward a coin first, partner, and I will read the trail with you.',
        50,
      ),
      candidate_messages: [],
      meta: {
        risk_level: 'low',
        recommended_action: 'wait_for_a_target',
        selected_coin_id: null,
      },
    }
  }

  const coinLabel = describeCoin(input.selectedCoin)
  const hiderLabel = describeHider(input.hider)

  let reply: CompanionReplyNow

  switch (input.intentType) {
    case 'ask_risk_about_selected_coin':
      if (riskLevel === 'high') {
        reply = buildReply(
          'risk_warning',
          `Easy now, partner. That ${coinLabel} bears signs of crooked work. ${hiderLabel} may be trouble.`,
          90,
        )
      } else if (riskLevel === 'medium') {
        reply = buildReply(
          'risk_warning',
          `I would tread careful, friend. ${hiderLabel} has the look of a seasoned hand, and this one may have a sting in it.`,
          80,
        )
      } else {
        reply = buildReply(
          'risk_warning',
          `No strong sign of dirty work yet. Still, keep your wits about you and do not rush the grab.`,
          65,
        )
      }
      break

    case 'ask_for_hint':
      if (input.distanceToCoinMeters !== null && input.distanceToCoinMeters > 150) {
        reply = buildReply(
          'hint',
          `You are still on the outer edge of the trail. Close the distance first, then trust your eyes over your nerves.`,
          65,
        )
      } else if (input.distanceToCoinMeters !== null && input.distanceToCoinMeters > 50) {
        reply = buildReply(
          'hint',
          `You are close enough now to slow down and read the ground, partner. This is where the trail starts talking.`,
          70,
        )
      } else {
        reply = buildReply(
          'hint',
          'You are in the dangerous pocket now. Move steady, inspect before you snatch, and let the trail show its hand.',
          80,
        )
      }
      break

    case 'ask_who_hid_this':
      reply = buildReply(
        'target_context',
        `${hiderLabel} laid this ${coinLabel}. I cannot swear to their intentions yet, but I can tell you they have been active on these trails.`,
        70,
      )
      break

    case 'ask_if_worth_it':
      if (input.selectedCoin.value >= 5) {
        reply = buildReply(
          'encouragement',
          `For that kind of purse, I would keep after it, provided your nerve holds steady.`,
          68,
        )
      } else if (riskLevel === 'high') {
        reply = buildReply(
          'encouragement',
          'For a small prize and a crooked trail, I would not press my luck unless you are hungry for trouble.',
          75,
        )
      } else {
        reply = buildReply(
          'encouragement',
          'Modest take, modest risk. Worth the walk if the trail stays clean.',
          60,
        )
      }
      break

    case 'ask_for_better_target':
      reply = buildReply(
        'target_context',
        'I can help read the trail you have, partner, but this first slice is not scouting a better target yet. For now, I would judge the coin already in your sights.',
        55,
      )
      break

    default:
      reply = buildReply(
        'hint',
        'I heard you, partner. Point me at the trail and I will do my part.',
        50,
      )
      break
  }

  return {
    reply_now: reply,
    candidate_messages: buildRiskCandidates(riskLevel),
    meta: {
      risk_level: riskLevel,
      recommended_action: recommendedAction,
      selected_coin_id: input.selectedCoin.id,
    },
  }
}

export function buildEventResponse(input: BuildEventResponseInput): CompanionResponsePack | null {
  switch (input.eventType) {
    case 'coin_collected_success':
      return {
        reply_now: buildReply(
          'collection_reaction',
          'Well done, partner. That one was worth the walk.',
          75,
        ),
        candidate_messages: [],
        meta: {
          risk_level: 'low',
          recommended_action: 'celebrate_find',
          selected_coin_id: input.selectedCoin?.id ?? null,
        },
      }

    case 'coin_collection_failed':
      return {
        reply_now: buildReply(
          'collection_reaction',
          'No matter, friend. Shake it off and keep to the trail.',
          60,
        ),
        candidate_messages: [],
        meta: {
          risk_level: 'medium',
          recommended_action: 'recover_and_retry',
          selected_coin_id: input.selectedCoin?.id ?? null,
        },
      }

    default:
      return null
  }
}
