import { buildBlackBartPromptEnvelope } from '@/lib/black-bart/prompt'
import type {
  BlackBartPromptContext,
  BlackBartProviderAttempt,
  BlackBartRuntimeInput,
} from '@/lib/black-bart/types'

interface BlackBartProviderResult {
  providerAttempt: BlackBartProviderAttempt
  responseText: string | null
}

function getConfiguredProvider(): string | null {
  const configured = process.env.BLACK_BART_MODEL_PROVIDER?.trim().toLowerCase()
  return configured ? configured : null
}

async function tryOpenAiResponsesProvider(params: {
  input: BlackBartRuntimeInput
  promptContext: BlackBartPromptContext
}): Promise<BlackBartProviderResult> {
  const apiKey = process.env.OPENAI_API_KEY?.trim()
  if (!apiKey) {
    return {
      providerAttempt: {
        provider: 'openai_responses',
        outcome: 'unavailable',
        reason: 'OPENAI_API_KEY is not configured',
      },
      responseText: null,
    }
  }

  const { systemPrompt, userPrompt } = buildBlackBartPromptEnvelope(params.input, params.promptContext)

  // Sprint 2 safety slice: the provider adapter exists and is prompt-ready,
  // but we intentionally keep the live runtime on scripted fallback until the
  // response schema + validation contract are implemented end-to-end.
  void systemPrompt
  void userPrompt

  return {
    providerAttempt: {
      provider: 'openai_responses',
      outcome: 'unavailable',
      reason: 'OpenAI provider adapter scaffolded but response parsing is not enabled yet',
    },
    responseText: null,
  }
}

export async function attemptBlackBartProviderResponse(params: {
  input: BlackBartRuntimeInput
  promptContext: BlackBartPromptContext
}): Promise<BlackBartProviderResult> {
  const configuredProvider = getConfiguredProvider()

  if (!configuredProvider) {
    return {
      providerAttempt: {
        provider: 'unconfigured',
        outcome: 'unavailable',
        reason: 'BLACK_BART_MODEL_PROVIDER is not configured',
      },
      responseText: null,
    }
  }

  if (configuredProvider === 'openai') {
    return tryOpenAiResponsesProvider(params)
  }

  return {
    providerAttempt: {
      provider: 'unsupported',
      outcome: 'unavailable',
      reason: `Unsupported BLACK_BART_MODEL_PROVIDER value: ${configuredProvider}`,
    },
    responseText: null,
  }
}
