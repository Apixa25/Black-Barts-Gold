/**
 * AI Agent Guardrails
 *
 * Central source of truth for all AI agent safety limits.
 * These constants are checked by every AI spawn/mutate route before executing.
 *
 * @file admin-dashboard/src/lib/ai-guardrails.ts
 */

/**
 * Maximum USD value of coins the AI can autonomously spawn per clock hour.
 * If this limit is reached, all AI spawn routes return SPEND_LIMIT_EXCEEDED (HTTP 429)
 * until the next clock hour begins.
 *
 * Change this value to adjust how aggressively the AI can spend per hour.
 * Review weekly once the Spawn Governor is live.
 */
export const AI_AUTONOMOUS_SPEND_LIMIT_USD = 10.00

/**
 * USD value above which a single AI spawn requires human approval.
 * Spawns above this value are queued as PENDING in ai_actions with success=false
 * and error_code='GUARDRAIL_BLOCKED' until a super_admin approves them.
 */
export const AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD = 50.00

/**
 * All valid AI agent IDs — must match the CHECK constraint in the ai_actions table
 * (Migration 014). Used for type-safe agent identification across routes.
 */
export const AI_AGENT_IDS = [
  'ai_spawn_governor',
  'ai_game_master',
  'ai_economy_balancer',
  'ai_churn_agent',
] as const

export type AiAgentId = typeof AI_AGENT_IDS[number]

/**
 * Standard error codes returned by AI routes.
 * Structured so the MCP agent can read the code and decide how to react.
 */
export const AI_ERROR_CODES = {
  SPEND_LIMIT_EXCEEDED:    'SPEND_LIMIT_EXCEEDED',     // hourly cap hit — retry next hour
  DISTRIBUTION_DISABLED:   'DISTRIBUTION_DISABLED',    // kill switch active — wait for admin
  GUARDRAIL_BLOCKED:       'GUARDRAIL_BLOCKED',         // single-spawn too expensive — needs approval
  ZONE_NOT_FOUND:          'ZONE_NOT_FOUND',            // invalid zone_id
  IDEMPOTENCY_CONFLICT:    'IDEMPOTENCY_CONFLICT',      // duplicate request — return cached response
  SPAWN_FAILED:            'SPAWN_FAILED',              // DB error during spawn
} as const

export type AiErrorCode = typeof AI_ERROR_CODES[keyof typeof AI_ERROR_CODES]
