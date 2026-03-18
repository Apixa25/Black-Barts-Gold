import { z } from 'zod'
import {
  buildMessageId,
  toIsoExpiry,
  type CompanionCandidateMessage,
  type CompanionReplyNow,
  type CompanionResponsePack,
} from '@/lib/companion/quick-prompts'
import type { BlackBartRuntimeInput } from '@/lib/black-bart/types'

const MAX_REPLY_NOW_LENGTH = 160
const MAX_CANDIDATE_MESSAGE_LENGTH = 120
const MAX_CANDIDATE_MESSAGES = 3

const providerReplySchema = z.object({
  message_type: z.enum([
    'greeting',
    'risk_warning',
    'hint',
    'target_context',
    'encouragement',
    'collection_reaction',
  ]),
  message_text: z.string().min(1),
  priority: z.number().min(1).max(100),
})

const providerCandidateSchema = z.object({
  trigger_type: z.enum([
    'distance_under_meters',
    'coin_collected_success',
    'coin_collection_failed',
  ]),
  trigger_value: z.union([z.number(), z.string(), z.null()]),
  message_text: z.string().min(1),
  priority: z.number().min(1).max(100),
})

const providerResponseSchema = z.object({
  reply_now: providerReplySchema.nullable(),
  candidate_messages: z.array(providerCandidateSchema).max(4),
  meta: z.object({
    risk_level: z.enum(['low', 'medium', 'high']),
    recommended_action: z.enum([
      'begin_hunt',
      'continue_hunt',
      'continue_with_caution',
      'inspect_before_collecting',
      'check_map_for_better_coin',
      'celebrate_find',
      'recover_and_retry',
      'wait_for_a_target',
    ]),
  }),
})

function sanitizeMessageText(text: string, maxLength = 280): string {
  return text.trim().replace(/\s+/g, ' ').slice(0, maxLength)
}

function toReplyNow(reply: z.infer<typeof providerReplySchema>): CompanionReplyNow {
  return {
    message_id: buildMessageId(reply.message_type),
    message_type: reply.message_type,
    message_text: sanitizeMessageText(reply.message_text, MAX_REPLY_NOW_LENGTH),
    voice_text: null,
    priority: reply.priority,
    tap_action: 'none',
    expires_at: toIsoExpiry(15),
  }
}

function toCandidateMessage(candidate: z.infer<typeof providerCandidateSchema>): CompanionCandidateMessage {
  return {
    message_id: buildMessageId(candidate.trigger_type),
    trigger_type: candidate.trigger_type,
    trigger_value: candidate.trigger_value,
    message_text: sanitizeMessageText(candidate.message_text, MAX_CANDIDATE_MESSAGE_LENGTH),
    voice_text: null,
    priority: candidate.priority,
  }
}

function allowedReplyTypesForAction(action: BlackBartRuntimeInput['action']) {
  switch (action) {
    case 'start_session':
      return new Set<CompanionReplyNow['message_type']>(['greeting', 'hint', 'encouragement'])
    case 'submit_intent':
      return new Set<CompanionReplyNow['message_type']>([
        'risk_warning',
        'hint',
        'target_context',
        'encouragement',
      ])
    case 'report_event':
      return new Set<CompanionReplyNow['message_type']>(['collection_reaction', 'encouragement', 'hint'])
  }
}

function sanitizeCandidatesForAction(
  input: BlackBartRuntimeInput,
  candidates: CompanionCandidateMessage[],
): CompanionCandidateMessage[] {
  if (input.action === 'report_event') {
    return []
  }

  const deduped = new Map<CompanionCandidateMessage['trigger_type'], CompanionCandidateMessage>()
  for (const candidate of candidates) {
    if (!deduped.has(candidate.trigger_type)) {
      deduped.set(candidate.trigger_type, candidate)
    }
  }

  const limited = [...deduped.values()].slice(0, MAX_CANDIDATE_MESSAGES)

  return limited.map((candidate) => {
    if (candidate.trigger_type === 'distance_under_meters') {
      const numericValue = typeof candidate.trigger_value === 'number'
        ? candidate.trigger_value
        : Number(candidate.trigger_value)

      return {
        ...candidate,
        trigger_value: Number.isFinite(numericValue)
          ? Math.max(1, Math.min(Math.round(numericValue), 500))
          : 100,
      }
    }

    return {
      ...candidate,
      trigger_value: null,
    }
  })
}

function validateProviderPackForAction(
  input: BlackBartRuntimeInput,
  responsePack: CompanionResponsePack,
): CompanionResponsePack {
  const allowedReplyTypes = allowedReplyTypesForAction(input.action)

  if (responsePack.reply_now && !allowedReplyTypes.has(responsePack.reply_now.message_type)) {
    throw new Error(`Reply type ${responsePack.reply_now.message_type} is not allowed for action ${input.action}`)
  }

  if (!input.selectedCoin && input.action !== 'report_event') {
    return {
      ...responsePack,
      candidate_messages: [],
      meta: {
        ...responsePack.meta,
        selected_coin_id: null,
        recommended_action: 'wait_for_a_target',
      },
    }
  }

  return {
    ...responsePack,
    candidate_messages: sanitizeCandidatesForAction(input, responsePack.candidate_messages),
  }
}

export function normalizeCompanionResponsePack(
  responsePack: CompanionResponsePack | null
): CompanionResponsePack | null {
  if (!responsePack) return null

  return {
    reply_now: responsePack.reply_now,
    candidate_messages: Array.isArray(responsePack.candidate_messages)
      ? responsePack.candidate_messages
      : [],
    meta: {
      risk_level: responsePack.meta.risk_level,
      recommended_action: responsePack.meta.recommended_action,
      selected_coin_id: responsePack.meta.selected_coin_id ?? null,
    },
  }
}

export function parseBlackBartProviderResponse(
  input: BlackBartRuntimeInput,
  responseText: string,
  selectedCoinId: string | null,
): CompanionResponsePack {
  let parsedJson: unknown
  try {
    parsedJson = JSON.parse(responseText)
  } catch (error) {
    throw new Error(`Provider returned invalid JSON: ${error instanceof Error ? error.message : String(error)}`)
  }

  const parsed = providerResponseSchema.parse(parsedJson)

  const providerPack = {
    reply_now: parsed.reply_now ? toReplyNow(parsed.reply_now) : null,
    candidate_messages: parsed.candidate_messages.map(toCandidateMessage),
    meta: {
      risk_level: parsed.meta.risk_level,
      recommended_action: parsed.meta.recommended_action,
      selected_coin_id: selectedCoinId,
    },
  }

  return validateProviderPackForAction(input, providerPack)
}
