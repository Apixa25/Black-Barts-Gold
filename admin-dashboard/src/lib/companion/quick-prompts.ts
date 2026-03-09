export const COMPANION_QUICK_PROMPTS = [
  {
    intent_type: 'ask_risk_about_selected_coin',
    label: 'Is this safe?',
    short_label: 'Safe?',
  },
  {
    intent_type: 'ask_for_hint',
    label: 'Any hint?',
    short_label: 'Hint',
  },
  {
    intent_type: 'ask_who_hid_this',
    label: 'Who hid this?',
    short_label: 'Who hid it?',
  },
  {
    intent_type: 'ask_if_worth_it',
    label: 'Worth the trouble?',
    short_label: 'Worth it?',
  },
  {
    intent_type: 'ask_for_better_target',
    label: 'Anything better nearby?',
    short_label: 'Better target?',
  },
] as const

export type CompanionIntentType = typeof COMPANION_QUICK_PROMPTS[number]['intent_type']

export type CompanionRiskLevel = 'low' | 'medium' | 'high'

export type CompanionRecommendedAction =
  | 'begin_hunt'
  | 'continue_hunt'
  | 'continue_with_caution'
  | 'inspect_before_collecting'
  | 'check_map_for_better_coin'
  | 'celebrate_find'
  | 'recover_and_retry'
  | 'wait_for_a_target'

export type CompanionTriggerType =
  | 'distance_under_meters'
  | 'coin_collected_success'
  | 'coin_collection_failed'

export interface CompanionReplyNow {
  message_id: string
  message_type: 'greeting' | 'risk_warning' | 'hint' | 'target_context' | 'encouragement' | 'collection_reaction'
  message_text: string
  voice_text: string | null
  priority: number
  tap_action: 'none' | 'play_voice'
  expires_at: string | null
}

export interface CompanionCandidateMessage {
  message_id: string
  trigger_type: CompanionTriggerType
  trigger_value: number | string | null
  message_text: string
  voice_text: string | null
  priority: number
}

export interface CompanionResponsePack {
  reply_now: CompanionReplyNow | null
  candidate_messages: CompanionCandidateMessage[]
  meta: {
    risk_level: CompanionRiskLevel
    recommended_action: CompanionRecommendedAction
    selected_coin_id: string | null
  }
}

export function getQuickPromptDefinition(intentType: string) {
  return COMPANION_QUICK_PROMPTS.find(prompt => prompt.intent_type === intentType)
}

export function toIsoExpiry(minutesFromNow: number): string {
  return new Date(Date.now() + minutesFromNow * 60_000).toISOString()
}

export function buildMessageId(prefix: string): string {
  return `${prefix}_${crypto.randomUUID()}`
}
