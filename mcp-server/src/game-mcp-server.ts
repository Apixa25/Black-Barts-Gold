/**
 * Black Bart's Gold — MCP Game Server
 *
 * Registers the 5 tools and 3 resources that give an AI agent live,
 * structured access to the Black Bart's Gold game economy.
 *
 * Tools   → actions  (the AI calls these to change game state)
 * Resources → read-only live data the AI can reference at any time
 *
 * Architecture: every tool wraps one of the 5 admin AI routes built in Step 2.
 * The routes live at /api/v1/admin/ai/* on the admin dashboard.
 *
 * @file mcp-server/src/game-mcp-server.ts
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js'
import { z } from 'zod'

// ---------------------------------------------------------------------------
// Shared HTTP helper — all tools use this to call the admin API
// ---------------------------------------------------------------------------

function getAdminApiBase(): string {
  const base = process.env.ADMIN_API_BASE_URL
  if (!base) throw new Error('ADMIN_API_BASE_URL environment variable is not set')
  return base.replace(/\/$/, '') // strip trailing slash
}

async function callAdminApi(path: string, options?: RequestInit): Promise<Response> {
  const base = getAdminApiBase()
  const apiKey = process.env.AI_AGENT_API_KEY

  const res = await fetch(`${base}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(apiKey ? { Authorization: `Bearer ${apiKey}` } : {}),
      ...(options?.headers ?? {}),
    },
  })
  return res
}

/** Converts an HTTP response into MCP content, marking non-2xx as errors */
async function toMcpContent(res: Response): Promise<{ content: [{ type: 'text'; text: string }]; isError?: boolean }> {
  const text = await res.text()
  return {
    content: [{ type: 'text' as const, text }],
    ...(res.ok ? {} : { isError: true }),
  }
}

// ---------------------------------------------------------------------------
// Agent ID constants (must match ai_actions CHECK constraint)
// ---------------------------------------------------------------------------

const AGENT_IDS = ['ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer', 'ai_churn_agent'] as const
type AgentId = typeof AGENT_IDS[number]

// ---------------------------------------------------------------------------
// Guardrail constants (mirrored from admin-dashboard for reference in descriptions)
// ---------------------------------------------------------------------------

const SPEND_LIMIT_USD = Number(process.env.AI_AUTONOMOUS_SPEND_LIMIT_USD ?? 10)
const APPROVAL_THRESHOLD_USD = Number(process.env.AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD ?? 50)

// ---------------------------------------------------------------------------
// createGameServer — builds and returns the configured McpServer
// ---------------------------------------------------------------------------

export function createGameServer(): McpServer {
  const server = new McpServer({
    name: "Black Bart's Gold",
    version: '0.1.0',
  })

  // ══════════════════════════════════════════════════════════════════════════
  // TOOL 1 — get_hunt_pressure
  //
  // The Spawn Governor reads this first every cycle.
  // Returns per-zone analysis: active players vs active coins → hunt_pressure score.
  // The meta.recommended_action field tells you directly whether to spawn.
  // ══════════════════════════════════════════════════════════════════════════
  server.tool(
    'get_hunt_pressure',
    `Returns live hunt pressure for every active game zone.
Hunt pressure = active_player_count / max(active_coin_count, 1).
A score above 3.0 means players are competing for too few coins.
The response includes meta.recommended_action ('spawn_coins' | 'no_action_needed' | 'kill_switch_active')
and meta.spend_remaining_usd so you can gate your next action.
ALWAYS call this before spawn_coin to avoid wasting the hourly spend budget.`,
    {
      active_window_minutes: z
        .number()
        .min(5)
        .max(120)
        .default(30)
        .describe(
          'How many minutes back to look for active player locations. ' +
          'Use 30 for normal operations. Use 5 for high-frequency monitoring.'
        ),
      min_pressure_threshold: z
        .number()
        .min(0)
        .max(20)
        .default(0)
        .describe(
          'Only return zones with hunt_pressure at or above this value. ' +
          'Use 3.0 to filter to zones that actually need attention. ' +
          'Use 0 (default) to see all zones.'
        ),
    },
    async (args) => {
      try {
        const params = new URLSearchParams({
          active_window_minutes: String(args.active_window_minutes ?? 30),
          min_pressure_threshold: String(args.min_pressure_threshold ?? 0),
        })
        const res = await callAdminApi(`/api/v1/admin/ai/hunt-pressure?${params}`)
        return toMcpContent(res)
      } catch (err) {
        return {
          content: [{ type: 'text' as const, text: `get_hunt_pressure failed: ${err instanceof Error ? err.message : String(err)}` }],
          isError: true,
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // TOOL 2 — spawn_coin
  //
  // The Spawn Governor's primary action. Creates one coin in a zone.
  // Built-in guardrails (enforced server-side, not just here):
  //   • Kill switch: returns 503 if distribution is disabled
  //   • Hourly spend cap: $${SPEND_LIMIT_USD}/hr — returns 429 when hit
  //   • Single-spawn gate: coins > $${APPROVAL_THRESHOLD_USD} need human approval
  //   • Idempotency: use idempotency_key to safely retry on network failure
  // ══════════════════════════════════════════════════════════════════════════
  server.tool(
    'spawn_coin',
    `Spawns a single coin in the specified zone.
GUARDRAILS (all enforced server-side):
  - Hourly autonomous spend cap: $${SPEND_LIMIT_USD}/hr. When hit, HTTP 429 is returned.
  - Single-spawn approval gate: coins worth > $${APPROVAL_THRESHOLD_USD} require human approval (HTTP 403).
  - Kill switch: if distribution is disabled, HTTP 503 is returned.
  - Idempotency: supply idempotency_key to safely retry after a network failure without double-spawning.
WORKFLOW: Call get_hunt_pressure first → pick the zone with the highest hunt_pressure 
and needs_spawn=true → use the recommended_spawn_tier as the tier.
Always include a meaningful reasoning string — it's stored in the audit log and helps admins review AI decisions.`,
    {
      zone_id: z
        .string()
        .uuid()
        .describe('UUID of the zone to spawn in. Get this from get_hunt_pressure → data.zones[n].zone_id'),
      tier: z
        .enum(['gold', 'silver', 'bronze'])
        .describe(
          'Coin tier. Use recommended_spawn_tier from get_hunt_pressure for the best player match. ' +
          'gold = highest value, bronze = lowest value.'
        ),
      agent_id: z
        .enum(AGENT_IDS)
        .describe('Your agent identifier. Use the one that matches your role.'),
      reasoning: z
        .string()
        .min(10)
        .max(500)
        .describe(
          'Why you are spawning this coin. Be specific — this is stored in the audit log. ' +
          'Example: "Zone downtown: 8 active players, only 1 coin available, pressure 8.0"'
        ),
      value_usd: z
        .number()
        .min(0.01)
        .max(100)
        .optional()
        .describe(
          'Explicit USD value for the coin. Omit to let the server calculate from tier defaults. ' +
          `Coins above $${APPROVAL_THRESHOLD_USD} require human approval.`
        ),
      latitude: z
        .number()
        .min(-90)
        .max(90)
        .optional()
        .describe('Explicit spawn latitude. Omit to let the server pick a random point within the zone.'),
      longitude: z
        .number()
        .min(-180)
        .max(180)
        .optional()
        .describe('Explicit spawn longitude. Omit to let the server pick a random point within the zone.'),
      metadata: z
        .record(z.unknown())
        .optional()
        .describe(
          'Freeform context to attach to the coin for later analysis. ' +
          'Recommended keys: hunt_pressure, active_player_count, weather_signal, economy_status.'
        ),
      idempotency_key: z
        .string()
        .optional()
        .describe(
          'Unique key to prevent duplicate spawns if this tool call is retried. ' +
          'Recommended format: `${agent_id}_${zone_id}_${Math.floor(Date.now() / 300000)}` ' +
          '(5-minute window prevents duplicates within the same governor cycle).'
        ),
    },
    async (args) => {
      try {
        const res = await callAdminApi('/api/v1/admin/ai/spawn', {
          method: 'POST',
          body: JSON.stringify(args),
          headers: args.idempotency_key
            ? { 'Idempotency-Key': args.idempotency_key }
            : {},
        })
        return toMcpContent(res)
      } catch (err) {
        return {
          content: [{ type: 'text' as const, text: `spawn_coin failed: ${err instanceof Error ? err.message : String(err)}` }],
          isError: true,
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // TOOL 3 — recycle_stale_coins
  //
  // Cleans up coins that have been sitting uncollected for too long.
  // Call this when economy_health shows oversupply OR when a zone has
  // zero active players and more coins than its max_coins config.
  // The response meta.recommended_action tells you if you should follow
  // up with spawn_coin to replace the recycled coins.
  // ══════════════════════════════════════════════════════════════════════════
  server.tool(
    'recycle_stale_coins',
    `Recycles coins that have been uncollected past the max_age_hours threshold.
Use this when:
  - get_economy_health returns economy_status = 'oversupply' (supply_demand_ratio > 2.5)
  - A zone has active_player_count = 0 and too many coins sitting uncollected
  - Before spawning fresh coins in a zone that already has stale ones
The response includes data.coins_recycled and meta.recommended_action 
('spawn_replacements' | 'no_action_needed').
If coins_recycled > 0 and recommended_action = 'spawn_replacements', 
follow up with spawn_coin calls in the affected zones.`,
    {
      agent_id: z
        .enum(AGENT_IDS)
        .describe('Your agent identifier.'),
      reasoning: z
        .string()
        .min(10)
        .max(500)
        .describe(
          'Why you are recycling. Be specific. ' +
          'Example: "Zone park: 0 active players, 7 coins over max_coins limit, supply_demand_ratio 3.8"'
        ),
      max_age_hours: z
        .number()
        .min(1)
        .max(168)
        .default(48)
        .describe(
          'Recycle coins that have been uncollected for longer than this many hours. ' +
          'Default 48h (2 days). Use 6h for zones with no active players.'
        ),
      zone_id: z
        .string()
        .uuid()
        .optional()
        .describe(
          'Limit recycling to a single zone UUID. ' +
          'Omit to recycle stale coins across all zones (use with care).'
        ),
    },
    async (args) => {
      try {
        const res = await callAdminApi('/api/v1/admin/ai/recycle-stale', {
          method: 'POST',
          body: JSON.stringify(args),
        })
        return toMcpContent(res)
      } catch (err) {
        return {
          content: [{ type: 'text' as const, text: `recycle_stale_coins failed: ${err instanceof Error ? err.message : String(err)}` }],
          isError: true,
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // TOOL 4 — get_economy_health
  //
  // The economy gate. Call this to check if the game is financially healthy
  // before taking any spawning action. The Spawn Governor MUST abort its cycle
  // if economy_status = 'margin_risk' — we are paying out more than we earn.
  // ══════════════════════════════════════════════════════════════════════════
  server.tool(
    'get_economy_health',
    `Returns a financial health snapshot of the entire coin economy for today.
Key fields to check before spawning:
  - meta.economy_status: 'healthy' | 'undersupply' | 'oversupply' | 'margin_risk'
  - data.supply_demand_ratio: coins_spawned / coins_collected (healthy = 0.8–2.5)
  - data.net_margin_today_usd: gas_revenue - value_collected (must stay positive)
  - data.ai_spend_this_hour_usd: compared to the $${SPEND_LIMIT_USD}/hr limit
  - meta.alerts: array of specific warnings that need attention
DECISION RULES:
  - economy_status = 'margin_risk' → STOP all spawning immediately, alert admin
  - economy_status = 'oversupply'  → recycle_stale_coins, then pause spawning
  - economy_status = 'undersupply' → increase spawn rate
  - economy_status = 'healthy'     → continue normal operations`,
    {},
    async () => {
      try {
        const res = await callAdminApi('/api/v1/admin/ai/economy-health')
        return toMcpContent(res)
      } catch (err) {
        return {
          content: [{ type: 'text' as const, text: `get_economy_health failed: ${err instanceof Error ? err.message : String(err)}` }],
          isError: true,
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // TOOL 5 — get_ai_actions
  //
  // Your memory. Returns the ai_actions audit log so you can see what you
  // (and other agents) have done recently. Use this to:
  //   - Avoid spawning in a zone you already spawned in this cycle
  //   - Detect if a previous spawn failed and needs retry
  //   - Review the cost of recent actions before spending more
  // ══════════════════════════════════════════════════════════════════════════
  server.tool(
    'get_ai_actions',
    `Returns the AI actions audit log — a record of every tool call made by all AI agents.
Use this as your short-term memory to:
  - Check what you already did in the current cycle (filter by your agent_id)
  - Look for failed spawns that need retry (filter by success=false)
  - Audit total spend before making more spawn decisions
The response meta.total_cost_usd shows total USD spent in the queried period.
Default date is today.`,
    {
      agent_id: z
        .string()
        .optional()
        .describe(
          'Filter to a specific agent. Use your own agent_id to check your recent actions. ' +
          `Valid values: ${AGENT_IDS.join(', ')}`
        ),
      tool_called: z
        .string()
        .optional()
        .describe("Filter by tool name. Example: 'spawn_coin' to see only spawn actions."),
      date: z
        .string()
        .optional()
        .describe('ISO date string (YYYY-MM-DD). Defaults to today.'),
      limit: z
        .number()
        .min(1)
        .max(200)
        .default(20)
        .describe('Number of most recent actions to return. Default 20.'),
      offset: z
        .number()
        .min(0)
        .default(0)
        .describe('Pagination offset. Use with limit to page through results.'),
      success: z
        .boolean()
        .optional()
        .describe('Filter by outcome. true = successful actions only. false = failures only. Omit for all.'),
    },
    async (args) => {
      try {
        const params = new URLSearchParams()
        if (args.agent_id) params.set('agent_id', args.agent_id)
        if (args.tool_called) params.set('tool_called', args.tool_called)
        if (args.date) params.set('date', args.date)
        if (args.limit) params.set('limit', String(args.limit))
        if (args.offset) params.set('offset', String(args.offset))
        if (args.success !== undefined) params.set('success', String(args.success))
        const res = await callAdminApi(`/api/v1/admin/ai/actions?${params}`)
        return toMcpContent(res)
      } catch (err) {
        return {
          content: [{ type: 'text' as const, text: `get_ai_actions failed: ${err instanceof Error ? err.message : String(err)}` }],
          isError: true,
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // RESOURCE 1 — game://guardrails
  //
  // The AI's safety rulebook. Always available, no network call required.
  // Lists all spend limits, blocked actions, and how to read error codes.
  // ══════════════════════════════════════════════════════════════════════════
  server.resource(
    'guardrails',
    'game://guardrails',
    { description: "The Black Bart's Gold AI safety guardrails. Read this to understand spend limits and blocked actions.", mimeType: 'application/json' },
    async () => ({
      contents: [
        {
          uri: 'game://guardrails',
          mimeType: 'application/json',
          text: JSON.stringify(
            {
              autonomous_spend_limit_usd_per_hour: SPEND_LIMIT_USD,
              single_spawn_approval_threshold_usd: APPROVAL_THRESHOLD_USD,
              error_codes: {
                SPEND_LIMIT_EXCEEDED: 'Hourly cap hit. HTTP 429. Wait until the next clock hour.',
                DISTRIBUTION_DISABLED: 'Kill switch active. HTTP 503. Wait for admin to re-enable.',
                GUARDRAIL_BLOCKED: `Single spawn > $${APPROVAL_THRESHOLD_USD}. HTTP 403. Needs human approval.`,
                ZONE_NOT_FOUND: 'Invalid zone_id. HTTP 404. Call get_hunt_pressure to get valid zone UUIDs.',
                IDEMPOTENCY_CONFLICT: 'Duplicate idempotency_key. Returns the cached result, not an error.',
                SPAWN_FAILED: 'Database error during spawn_coin. HTTP 500. Safe to retry with a new idempotency_key.',
              },
              spawn_governor_abort_conditions: [
                'economy_status = margin_risk',
                'kill_switch_active = true',
                'spend_remaining_usd <= 0',
                'no active players in any zone',
                'supply_demand_ratio > 3.0 (run recycle_stale_coins instead)',
              ],
              coin_created_by_values: [
                'ai_spawn_governor',
                'ai_game_master',
                'ai_economy_balancer',
                'system',
                'admin',
                'user',
              ],
            },
            null,
            2
          ),
        },
      ],
    })
  )

  // ══════════════════════════════════════════════════════════════════════════
  // RESOURCE 2 — game://economy/health
  //
  // Live economy snapshot. Refreshes on every read (HTTP call to the API).
  // Subscribe to this at the start of each governor cycle.
  // ══════════════════════════════════════════════════════════════════════════
  server.resource(
    'economy_health',
    'game://economy/health',
    { description: 'Live economy health snapshot — supply/demand ratio, margins, AI spend. Refreshes on every read.', mimeType: 'application/json' },
    async () => {
      try {
        const res = await callAdminApi('/api/v1/admin/ai/economy-health')
        const text = await res.text()
        return {
          contents: [
            {
              uri: 'game://economy/health',
              mimeType: 'application/json',
              text,
            },
          ],
        }
      } catch (err) {
        return {
          contents: [
            {
              uri: 'game://economy/health',
              mimeType: 'application/json',
              text: JSON.stringify({ error: `Failed to fetch economy health: ${err instanceof Error ? err.message : String(err)}` }),
            },
          ],
        }
      }
    }
  )

  // ══════════════════════════════════════════════════════════════════════════
  // RESOURCE 3 — game://hunt/pressure
  //
  // Live hunt pressure across all zones. Refreshes on every read.
  // Use this as a quick-read alternative to calling get_hunt_pressure()
  // when you just want the current state without filtering.
  // ══════════════════════════════════════════════════════════════════════════
  server.resource(
    'hunt_pressure',
    'game://hunt/pressure',
    { description: 'Live hunt pressure for all active zones — player counts, coin counts, and pressure scores. Refreshes on every read.', mimeType: 'application/json' },
    async () => {
      try {
        const res = await callAdminApi('/api/v1/admin/ai/hunt-pressure')
        const text = await res.text()
        return {
          contents: [
            {
              uri: 'game://hunt/pressure',
              mimeType: 'application/json',
              text,
            },
          ],
        }
      } catch (err) {
        return {
          contents: [
            {
              uri: 'game://hunt/pressure',
              mimeType: 'application/json',
              text: JSON.stringify({ error: `Failed to fetch hunt pressure: ${err instanceof Error ? err.message : String(err)}` }),
            },
          ],
        }
      }
    }
  )

  return server
}

export type { AgentId }
