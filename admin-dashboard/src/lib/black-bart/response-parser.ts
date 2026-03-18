import type { CompanionResponsePack } from '@/lib/companion/quick-prompts'

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
