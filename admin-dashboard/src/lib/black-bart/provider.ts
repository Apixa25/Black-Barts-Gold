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

const OPENAI_CHAT_COMPLETION_SCHEMA = {
  name: 'black_bart_companion_response',
  strict: true,
  schema: {
    type: 'object',
    additionalProperties: false,
    properties: {
      reply_now: {
        anyOf: [
          {
            type: 'object',
            additionalProperties: false,
            properties: {
              message_type: {
                type: 'string',
                enum: [
                  'greeting',
                  'risk_warning',
                  'hint',
                  'target_context',
                  'encouragement',
                  'collection_reaction',
                ],
              },
              message_text: { type: 'string' },
              priority: { type: 'number' },
            },
            required: ['message_type', 'message_text', 'priority'],
          },
          { type: 'null' },
        ],
      },
      candidate_messages: {
        type: 'array',
        items: {
          type: 'object',
          additionalProperties: false,
          properties: {
            trigger_type: {
              type: 'string',
              enum: [
                'distance_under_meters',
                'coin_collected_success',
                'coin_collection_failed',
              ],
            },
            trigger_value: {
              anyOf: [
                { type: 'number' },
                { type: 'string' },
                { type: 'null' },
              ],
            },
            message_text: { type: 'string' },
            priority: { type: 'number' },
          },
          required: ['trigger_type', 'trigger_value', 'message_text', 'priority'],
        },
      },
      meta: {
        type: 'object',
        additionalProperties: false,
        properties: {
          risk_level: {
            type: 'string',
            enum: ['low', 'medium', 'high'],
          },
          recommended_action: {
            type: 'string',
            enum: [
              'begin_hunt',
              'continue_hunt',
              'continue_with_caution',
              'inspect_before_collecting',
              'check_map_for_better_coin',
              'celebrate_find',
              'recover_and_retry',
              'wait_for_a_target',
            ],
          },
        },
        required: ['risk_level', 'recommended_action'],
      },
    },
    required: ['reply_now', 'candidate_messages', 'meta'],
  },
} as const

function extractChatCompletionText(payload: unknown): string | null {
  if (!payload || typeof payload !== 'object') return null
  const choices = (payload as { choices?: unknown }).choices
  if (!Array.isArray(choices) || choices.length === 0) return null

  const firstChoice = choices[0]
  if (!firstChoice || typeof firstChoice !== 'object') return null

  const message = (firstChoice as { message?: unknown }).message
  if (!message || typeof message !== 'object') return null

  const content = (message as { content?: unknown }).content
  return typeof content === 'string' ? content : null
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
  const model = process.env.BLACK_BART_OPENAI_MODEL?.trim() || 'gpt-4.1-mini'

  try {
    const response = await fetch('https://api.openai.com/v1/chat/completions', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${apiKey}`,
      },
      body: JSON.stringify({
        model,
        temperature: 0.9,
        messages: [
          { role: 'system', content: systemPrompt },
          { role: 'user', content: userPrompt },
        ],
        response_format: {
          type: 'json_schema',
          json_schema: OPENAI_CHAT_COMPLETION_SCHEMA,
        },
      }),
    })

    if (!response.ok) {
      const errorBody = await response.text()
      return {
        providerAttempt: {
          provider: 'openai_chat',
          outcome: 'error',
          reason: `OpenAI chat completion failed with ${response.status}: ${errorBody}`,
        },
        responseText: null,
      }
    }

    const payload = await response.json()
    const responseText = extractChatCompletionText(payload)

    if (!responseText) {
      return {
        providerAttempt: {
          provider: 'openai_chat',
          outcome: 'error',
          reason: 'OpenAI chat completion returned no parseable message content',
        },
        responseText: null,
      }
    }

    return {
      providerAttempt: {
        provider: 'openai_chat',
        outcome: 'success',
        reason: null,
      },
      responseText,
    }
  } catch (error) {
    return {
      providerAttempt: {
        provider: 'openai_chat',
        outcome: 'error',
        reason: `OpenAI request failed: ${error instanceof Error ? error.message : String(error)}`,
      },
      responseText: null,
    }
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
