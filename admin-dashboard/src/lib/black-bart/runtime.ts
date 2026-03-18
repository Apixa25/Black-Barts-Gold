import {
  buildEventResponse,
  buildIntentResponse,
  buildStartSessionResponse,
} from '@/lib/companion/companion-engine'
import { buildBlackBartPromptContext, BLACK_BART_SYSTEM_PROMPT_VERSION } from '@/lib/black-bart/prompt'
import { normalizeCompanionResponsePack } from '@/lib/black-bart/response-parser'
import type { BlackBartRuntimeInput, BlackBartRuntimeResult } from '@/lib/black-bart/types'

export async function generateBlackBartCompanionResponse(
  input: BlackBartRuntimeInput
): Promise<BlackBartRuntimeResult> {
  // Sprint 2 scaffold: this facade gives us one safe insertion point for a real
  // model-backed runtime later while preserving current behavior today.
  let responsePack = null

  switch (input.action) {
    case 'start_session':
      responsePack = buildStartSessionResponse({
        player: input.player,
        selectedCoin: input.selectedCoin,
      })
      break

    case 'submit_intent':
      responsePack = buildIntentResponse({
        intentType: input.intentType,
        player: input.player,
        selectedCoin: input.selectedCoin,
        hider: input.hider,
        distanceToCoinMeters: input.distanceToCoinMeters,
      })
      break

    case 'report_event':
      responsePack = buildEventResponse({
        eventType: input.eventType,
        selectedCoin: input.selectedCoin,
      })
      break
  }

  return {
    responsePack: normalizeCompanionResponsePack(responsePack),
    runtimeMeta: {
      source: 'scripted_fallback',
      systemPromptVersion: BLACK_BART_SYSTEM_PROMPT_VERSION,
      promptContext: buildBlackBartPromptContext(input),
    },
  }
}
