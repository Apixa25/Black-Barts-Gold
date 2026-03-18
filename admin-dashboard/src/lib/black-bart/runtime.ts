import {
  buildEventResponse,
  buildIntentResponse,
  buildStartSessionResponse,
} from '@/lib/companion/companion-engine'
import { buildBlackBartPromptContext, BLACK_BART_SYSTEM_PROMPT_VERSION } from '@/lib/black-bart/prompt'
import { attemptBlackBartProviderResponse } from '@/lib/black-bart/provider'
import {
  normalizeCompanionResponsePack,
  parseBlackBartProviderResponse,
} from '@/lib/black-bart/response-parser'
import type { BlackBartRuntimeInput, BlackBartRuntimeResult } from '@/lib/black-bart/types'

export async function generateBlackBartCompanionResponse(
  input: BlackBartRuntimeInput
): Promise<BlackBartRuntimeResult> {
  const promptContext = buildBlackBartPromptContext(input)
  const providerResult = await attemptBlackBartProviderResponse({
    input,
    promptContext,
  })

  if (providerResult.responseText) {
    try {
      const providerResponsePack = parseBlackBartProviderResponse(
        input,
        providerResult.responseText,
        promptContext.selectedCoinId,
      )

      return {
        responsePack: normalizeCompanionResponsePack(providerResponsePack),
        runtimeMeta: {
          source: 'model_provider',
          systemPromptVersion: BLACK_BART_SYSTEM_PROMPT_VERSION,
          promptContext,
          providerAttempt: providerResult.providerAttempt,
          fallbackReason: null,
        },
      }
    } catch (error) {
      const parseFailureReason = error instanceof Error ? error.message : String(error)

      providerResult.providerAttempt = {
        ...providerResult.providerAttempt,
        outcome: 'error',
        reason: `Provider response parsing failed: ${parseFailureReason}`,
      }
    }
  }

  // Sprint 2 scaffold: the runtime now explicitly tries the model-provider path
  // first and records why it fell back, while still preserving current behavior.
  let scriptedResponsePack = null

  switch (input.action) {
    case 'start_session':
      scriptedResponsePack = buildStartSessionResponse({
        player: input.player,
        selectedCoin: input.selectedCoin,
      })
      break

    case 'submit_intent':
      scriptedResponsePack = buildIntentResponse({
        intentType: input.intentType,
        player: input.player,
        selectedCoin: input.selectedCoin,
        hider: input.hider,
        distanceToCoinMeters: input.distanceToCoinMeters,
      })
      break

    case 'report_event':
      scriptedResponsePack = buildEventResponse({
        eventType: input.eventType,
        selectedCoin: input.selectedCoin,
      })
      break
  }

  return {
    responsePack: normalizeCompanionResponsePack(scriptedResponsePack),
    runtimeMeta: {
      source: 'scripted_fallback',
      systemPromptVersion: BLACK_BART_SYSTEM_PROMPT_VERSION,
      promptContext,
      providerAttempt: providerResult.providerAttempt,
      fallbackReason: providerResult.providerAttempt.reason,
    },
  }
}
