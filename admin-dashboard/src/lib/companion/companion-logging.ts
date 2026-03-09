import { createServiceRoleClient } from '@/lib/supabase/server'

interface LogCompanionActionInput {
  tool_called: string
  player_id: string
  parameters: Record<string, unknown>
  result?: Record<string, unknown> | null
  reasoning?: string | null
  success?: boolean
  error_code?: string | null
}

export async function logCompanionAction({
  tool_called,
  player_id,
  parameters,
  result = null,
  reasoning = null,
  success = true,
  error_code = null,
}: LogCompanionActionInput) {
  const supabase = createServiceRoleClient()

  const payload = {
    agent_id: 'ai_game_master',
    tool_called,
    parameters: {
      player_id,
      ...parameters,
    },
    reasoning,
    result,
    success,
    error_code,
    cost_usd: 0,
  }

  const { error } = await supabase.from('ai_actions').insert(payload)
  if (error) {
    console.error('[companion-logging] Failed to write ai_actions row:', error)
  }
}
