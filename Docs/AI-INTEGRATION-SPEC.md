# 🤖 Black Bart's Gold — AI Integration Technical Spec

> **This is the build blueprint.** `AI-integration.md` is the vision. This file is the wiring diagram — exact SQL, exact endpoint contracts, exact tool schemas. Open this when it's time to build.

**Document Status**: Phase 1 & 2 Complete Spec  
**Last Updated**: March 2026  
**Prerequisites**: Migrations 001–013 applied, admin-dashboard running on Next.js 16 + Supabase

---

## 📦 What Already Exists (Do Not Rebuild)

Before writing a single line of new code, know what's already done:

| Asset | Location | Status |
|-------|----------|--------|
| `spawn_coin()` PostgreSQL function | Migration 004 | ✅ Works end-to-end |
| `recycle_stale_coins()` function | Migration 004 | ✅ Works |
| `process_spawn_queue()` function | Migration 004 | ✅ Works |
| `check_and_queue_spawns()` function | Migration 004 | ⚠️ Has TODO: geometry containment |
| `spawn_queue` table | Migration 004 | ✅ Ready |
| `spawn_history` table | Migration 004 | ✅ Ready |
| `distribution_config` table | Migration 004 | ✅ Kill switch (`enabled` bool) exists |
| `player_locations` table | Migration 003 | ✅ Realtime already enabled |
| `player_location_history` table | Migration 003 | ✅ |
| `zones` table with `metadata jsonb` | Migration (zones) | ✅ |
| All existing `/api/v1/` game routes | Unity-facing | ✅ Unchanged |

---

## 🔢 Build Order

```
Step 1  →  Migration 014 (schema additions)           [~30 min]
Step 2  →  5 new admin AI API routes                  [~3 hrs]
Step 3  →  MCP server (5 tools wrapping those routes) [~4 hrs]
Step 4  →  Spawn Governor (Supabase Edge Function)    [~3 hrs]
Step 5  →  Realtime wiring (react on coin collection) [~1 hr]
```

Each step is independently testable and additive. Nothing in steps 2–5 modifies existing code.

---

## STEP 1: Migration 014 — AI Schema Additions

**File**: `admin-dashboard/supabase/migrations/014_ai_schema.sql`

### 1a. Add `created_by` and `metadata` to `coins`

```sql
-- ============================================================================
-- Migration: 014_ai_schema.sql
-- Purpose: Add AI agent audit trail and action logging infrastructure
-- ============================================================================

-- Add created_by to coins so we know who/what spawned each coin
ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS created_by TEXT NOT NULL DEFAULT 'system'
    CHECK (created_by IN ('system', 'admin', 'user', 'ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer'));

-- Add metadata to coins for AI context (reasoning, signals used, etc.)
ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS metadata JSONB;

COMMENT ON COLUMN public.coins.created_by IS 'Who/what created this coin: system, admin, user, or an AI agent';
COMMENT ON COLUMN public.coins.metadata  IS 'AI agent context: reasoning, weather signal, hunt pressure score, etc.';
```

### 1b. Add `created_by` to `spawn_history`

```sql
ALTER TABLE public.spawn_history
  ADD COLUMN IF NOT EXISTS created_by TEXT NOT NULL DEFAULT 'system'
    CHECK (created_by IN ('system', 'admin', 'user', 'ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer'));

COMMENT ON COLUMN public.spawn_history.created_by IS 'Who/what triggered this spawn';
```

### 1c. Expand `spawn_queue.trigger_type` to include AI values

```sql
-- Drop and recreate the CHECK constraint with expanded values
ALTER TABLE public.spawn_queue
  DROP CONSTRAINT IF EXISTS spawn_queue_trigger_type_check;

ALTER TABLE public.spawn_queue
  ADD CONSTRAINT spawn_queue_trigger_type_check
    CHECK (trigger_type IN ('auto', 'scheduled', 'manual', 'recycle', 'ai_spawn_governor', 'ai_game_master'));
```

### 1d. Create `ai_actions` audit log table

```sql
CREATE TABLE IF NOT EXISTS public.ai_actions (
  id            UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- Agent identity
  agent_id      TEXT NOT NULL
    CHECK (agent_id IN ('ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer', 'ai_churn_agent')),
  
  -- What it did
  tool_called   TEXT NOT NULL,        -- 'spawn_coin', 'recycle_stale_coins', 'send_notification'
  parameters    JSONB NOT NULL,       -- exact parameters passed to the tool
  reasoning     TEXT,                 -- AI's stated reason (from prompt response)
  
  -- What happened
  result        JSONB,                -- tool return value or error details
  success       BOOLEAN NOT NULL DEFAULT FALSE,
  error_code    TEXT,                 -- matches API error code strings
  
  -- Financial impact (critical for audit)
  cost_usd      DECIMAL(10, 4) NOT NULL DEFAULT 0,
  
  -- Timestamps
  created_at    TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Index for dashboard "What did the AI do today?" view
CREATE INDEX IF NOT EXISTS ai_actions_agent_time_idx
  ON public.ai_actions (agent_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ai_actions_date_idx
  ON public.ai_actions (created_at DESC);

-- RLS: Only super admins can read; service role writes
ALTER TABLE public.ai_actions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Admins can view AI actions" ON public.ai_actions;
CREATE POLICY "Admins can view AI actions" ON public.ai_actions
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

COMMENT ON TABLE public.ai_actions IS 'Audit log of every action taken by AI agents. Source of truth for the AI activity dashboard.';
```

### 1e. Enable Realtime on `coins` and `spawn_history`

```sql
-- coins table — so AI agent reacts when coins are collected
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime' AND tablename = 'coins'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.coins;
    RAISE NOTICE 'Added coins to supabase_realtime';
  END IF;
END $$;

-- spawn_history — so admin dashboard updates live when AI spawns
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime' AND tablename = 'spawn_history'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.spawn_history;
    RAISE NOTICE 'Added spawn_history to supabase_realtime';
  END IF;
END $$;
```

### 1f. Add AI spend tracking function

```sql
-- Returns total USD value of AI-spawned coins in the current hour
-- Used by all spawn routes to enforce the autonomous spend limit
CREATE OR REPLACE FUNCTION public.get_ai_spend_this_hour(
  p_agent_id TEXT DEFAULT NULL
)
RETURNS DECIMAL AS $$
DECLARE
  v_spend DECIMAL;
BEGIN
  SELECT COALESCE(SUM(cost_usd), 0)
  INTO v_spend
  FROM public.ai_actions
  WHERE success = TRUE
    AND tool_called = 'spawn_coin'
    AND created_at >= DATE_TRUNC('hour', NOW())
    AND (p_agent_id IS NULL OR agent_id = p_agent_id);
  
  RETURN v_spend;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.get_ai_spend_this_hour IS 'Returns USD spent by AI agents spawning coins in the current clock hour';
```

---

## STEP 2: Five New Admin AI API Routes

All routes live under `admin-dashboard/src/app/api/v1/admin/ai/`.  
All routes use `createServiceRoleClient()` (same pattern as existing routes).  
All routes follow the AI-first response shape: `{ data, meta, _links, timestamp }`.

### Autonomous Spend Limit

The spend limit is the **single most important guardrail**. Every spawn route checks it first.

```typescript
// admin-dashboard/src/lib/ai-guardrails.ts
// Shared constants imported by all AI routes

export const AI_AUTONOMOUS_SPEND_LIMIT_USD = 10.00  // max $/hour autonomous spending
export const AI_AGENT_IDS = [
  'ai_spawn_governor',
  'ai_game_master', 
  'ai_economy_balancer',
  'ai_churn_agent'
] as const
export type AiAgentId = typeof AI_AGENT_IDS[number]
```

---

### Route 1: `GET /api/v1/admin/ai/hunt-pressure`

**Purpose**: Primary "eyes" of the Spawn Governor. Returns per-zone analysis of how many players are present vs how many coins are available. This is the data the agent reads before deciding where to spawn.

**File**: `admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts`

#### Request
```
GET /api/v1/admin/ai/hunt-pressure
Query params (all optional):
  - active_window_minutes: number (default: 30) — how recent a player must be to count as "active"
  - min_pressure_threshold: number (default: 3.0) — only return zones above this pressure
```

#### Response Shape
```typescript
{
  success: true,
  data: {
    zones: [
      {
        zone_id: string,
        zone_name: string,
        zone_type: ZoneType,
        center: { latitude: number, longitude: number },
        
        // Current state
        active_player_count: number,    // players updated within active_window_minutes
        active_coin_count: number,      // coins with status 'hidden' or 'visible'
        
        // Computed score — the key AI input
        hunt_pressure: number,          // active_player_count / max(active_coin_count, 1)
        needs_spawn: boolean,           // hunt_pressure > threshold AND coins < min_coins config
        coins_to_spawn: number,         // how many needed to reach zone's min_coins config
        
        // Tier breakdown (AI matches spawn tier to players present)
        player_tier_distribution: {
          cabin_boy: number,            // players whose highest_hidden = null or < $5
          deck_hand: number,            // players who've hidden $5+
          captain: number,              // players who've hidden $25+
          king_of_pirates: number       // players who've hidden $100+
        },
        recommended_spawn_tier: CoinTier  // tier matching majority of players
      }
    ],
    summary: {
      total_active_zones: number,
      zones_needing_spawn: number,
      total_active_players: number,
      total_active_coins: number,
      overall_hunt_pressure: number
    }
  },
  meta: {
    recommended_action: 'spawn_coins' | 'no_action_needed' | 'kill_switch_active',
    high_pressure_zones: string[],          // zone_ids with pressure > 5.0
    spend_this_hour_usd: number,
    autonomous_spend_limit_usd: number,     // AI_AUTONOMOUS_SPEND_LIMIT_USD
    spend_remaining_usd: number,
    kill_switch_active: boolean             // distribution_config.enabled
  },
  _links: {
    spawn: '/api/v1/admin/ai/spawn',
    recycle: '/api/v1/admin/ai/recycle-stale',
    economy: '/api/v1/admin/ai/economy-health'
  },
  timestamp: string
}
```

#### Key Implementation Note
Player "active" status uses `player_locations.updated_at` — already being written by the Unity app every 5 seconds. No new data collection needed.

---

### Route 2: `POST /api/v1/admin/ai/spawn`

**Purpose**: The Spawn Governor's primary action. Wraps the existing `spawn_coin()` PostgreSQL function, adds AI audit logging, and enforces the spend guardrail.

**File**: `admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts`

#### Request Body
```typescript
{
  // Required
  zone_id: string,          // UUID of target zone
  tier: 'gold' | 'silver' | 'bronze',
  agent_id: AiAgentId,      // which AI agent is calling this
  reasoning: string,        // AI's stated reason — stored in ai_actions.reasoning
  
  // Optional
  value_usd?: number,       // explicit value; if omitted, spawn_coin() calculates from tier
  latitude?: number,        // explicit location; if omitted, spawn_coin() picks random in zone
  longitude?: number,
  metadata?: Record<string, unknown>,  // e.g. { weather: 'rain', hunt_pressure: 4.2 }
  
  // Idempotency — prevents duplicate spawns on retry
  idempotency_key?: string  // recommended: `${agent_id}_${zone_id}_${ISO_timestamp}`
}
```

#### Response Shape
```typescript
// Success
{
  success: true,
  data: {
    coin_id: string,
    zone_id: string,
    tier: CoinTier,
    value_usd: number,
    latitude: number,
    longitude: number,
    created_by: string,     // the agent_id passed in
    ai_action_id: string    // UUID of the row written to ai_actions
  },
  meta: {
    spend_this_hour_usd: number,        // AFTER this spawn
    spend_remaining_usd: number,
    autonomous_spend_limit_usd: number
  },
  timestamp: string
}

// Spend limit exceeded (HTTP 429)
{
  success: false,
  error: 'Autonomous spend limit reached for this hour',
  code: 'SPEND_LIMIT_EXCEEDED',
  meta: {
    spend_this_hour_usd: number,
    limit_usd: number,
    resets_at: string   // start of next hour ISO timestamp
  }
}

// Kill switch active (HTTP 503)
{
  success: false,
  error: 'Auto-distribution is disabled',
  code: 'DISTRIBUTION_DISABLED',
  meta: { kill_switch_active: true }
}
```

#### Implementation Sequence (inside the route handler)
```
1. Check distribution_config.enabled → if false, return DISTRIBUTION_DISABLED
2. Check idempotency_key cache (Redis or DB) → if hit, return cached response
3. Check get_ai_spend_this_hour() → if >= limit, return SPEND_LIMIT_EXCEEDED
4. Call spawn_coin(zone_id, trigger_type=agent_id, tier, value, lat, lng) → get coin_id
5. Update coins SET created_by=agent_id, metadata=metadata WHERE id=coin_id
6. INSERT into ai_actions (agent_id, tool_called='spawn_coin', parameters, reasoning, result, cost_usd=value_usd, success=true)
7. Store idempotency_key → coin_id mapping (TTL: 24h)
8. Return success response
```

---

### Route 3: `POST /api/v1/admin/ai/recycle-stale`

**Purpose**: Wraps the existing `recycle_stale_coins()` PostgreSQL function. Cleans up coins in dead zones (no active players nearby). Also logs the action.

**File**: `admin-dashboard/src/app/api/v1/admin/ai/recycle-stale/route.ts`

#### Request Body
```typescript
{
  agent_id: AiAgentId,
  reasoning: string,
  max_age_hours?: number,   // default: 48 (matches recycle_stale_coins default)
  zone_id?: string          // if omitted, recycles across all zones
}
```

#### Response Shape
```typescript
{
  success: true,
  data: {
    coins_recycled: number,
    zone_id: string | null,
    ai_action_id: string
  },
  meta: {
    recommended_action: 'no_action_needed' | 'spawn_replacements',
    zones_affected: string[]
  },
  timestamp: string
}
```

---

### Route 4: `GET /api/v1/admin/ai/economy-health`

**Purpose**: Gives the AI (and human admins) a snapshot of the coin economy's financial health. Monitors supply/demand balance and margin safety.

**File**: `admin-dashboard/src/app/api/v1/admin/ai/economy-health/route.ts`

#### Response Shape
```typescript
{
  success: true,
  data: {
    // Supply/demand
    coins_spawned_today: number,
    coins_collected_today: number,
    coins_recycled_today: number,
    active_coins_total: number,
    supply_demand_ratio: number,    // spawned / max(collected, 1) — healthy = 1.0–2.0

    // Financial
    value_spawned_today_usd: number,
    value_collected_today_usd: number,
    gas_revenue_today_usd: number,
    net_margin_today_usd: number,   // gas_revenue - value_collected

    // Average performance
    avg_time_to_collection_hours: number,   // from spawn_history
    avg_coin_value_usd: number,

    // AI spend
    ai_spend_today_usd: number,
    ai_spend_this_hour_usd: number,
    ai_actions_today: number
  },
  meta: {
    economy_status: 'healthy' | 'oversupply' | 'undersupply' | 'margin_risk',
    recommended_action: string,
    alerts: string[]    // e.g. ["Supply/demand ratio 4.2 — too many uncollected coins"]
  },
  _links: {
    hunt_pressure: '/api/v1/admin/ai/hunt-pressure',
    spawn: '/api/v1/admin/ai/spawn',
    recycle: '/api/v1/admin/ai/recycle-stale'
  },
  timestamp: string
}
```

#### Economy Status Logic
```
supply_demand_ratio < 0.8  → 'undersupply'   (players finding nothing)
supply_demand_ratio 0.8–2.5 → 'healthy'
supply_demand_ratio > 2.5  → 'oversupply'    (coins sitting uncollected)
net_margin_today_usd < 0   → 'margin_risk'   (paying out more than earning)
```

---

### Route 5: `GET /api/v1/admin/ai/actions`

**Purpose**: Read the `ai_actions` log. Powers the "What did Black Bart do today?" dashboard view and provides the AI with memory of its own recent decisions.

**File**: `admin-dashboard/src/app/api/v1/admin/ai/actions/route.ts`

#### Request Query Params
```
agent_id?: string         — filter by agent
tool_called?: string      — filter by tool
date?: string             — ISO date string, defaults to today
limit?: number            — default 50, max 200
offset?: number           — for pagination
success?: boolean         — filter by success/failure
```

#### Response Shape
```typescript
{
  success: true,
  data: {
    actions: AiAction[],   // rows from ai_actions table
    total_count: number,
    has_more: boolean
  },
  meta: {
    total_cost_usd: number,         // sum of cost_usd for returned period
    success_rate: number,           // percentage
    most_active_agent: string,
    actions_today: number
  },
  timestamp: string
}
```

---

## STEP 3: MCP Server — 5 Tool Definitions

**Package**: `@modelcontextprotocol/sdk` (add to admin-dashboard or a new `mcp-server/` package)  
**File**: `mcp-server/src/game-mcp-server.ts`

Each tool wraps one of the 5 routes above. The Zod schemas here define exactly what the AI agent is allowed to pass.

```typescript
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js'
import { z } from 'zod'

const ADMIN_API_BASE = process.env.ADMIN_API_BASE_URL  // e.g. https://bbg-admin.vercel.app

export function registerTools(server: McpServer) {

  // Tool 1: get_hunt_pressure
  server.registerTool('get_hunt_pressure', {
    active_window_minutes: z.number().min(5).max(120).default(30)
      .describe('How recent (minutes) a player update must be to count as active'),
    min_pressure_threshold: z.number().min(0).max(20).default(3.0)
      .describe('Only return zones with hunt pressure above this value')
  }, async (args) => {
    const params = new URLSearchParams({
      active_window_minutes: String(args.active_window_minutes),
      min_pressure_threshold: String(args.min_pressure_threshold)
    })
    const res = await fetch(`${ADMIN_API_BASE}/api/v1/admin/ai/hunt-pressure?${params}`, {
      headers: { Authorization: `Bearer ${process.env.AI_AGENT_API_KEY}` }
    })
    return { content: [{ type: 'text', text: await res.text() }] }
  })

  // Tool 2: spawn_coin
  server.registerTool('spawn_coin', {
    zone_id: z.string().uuid().describe('UUID of the zone to spawn in'),
    tier: z.enum(['gold', 'silver', 'bronze']).describe('Coin tier'),
    agent_id: z.enum(['ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer', 'ai_churn_agent'])
      .describe('Which AI agent is spawning'),
    reasoning: z.string().min(10).max(500)
      .describe('Why this coin is being spawned — stored in audit log'),
    value_usd: z.number().min(0.01).max(100).optional()
      .describe('Explicit value; omit to use tier defaults'),
    latitude: z.number().min(-90).max(90).optional(),
    longitude: z.number().min(-180).max(180).optional(),
    metadata: z.record(z.unknown()).optional()
      .describe('AI context to attach to coin: weather signal, hunt pressure, etc.'),
    idempotency_key: z.string().optional()
      .describe('Unique key to prevent duplicate spawns on retry')
  }, async (args) => {
    const res = await fetch(`${ADMIN_API_BASE}/api/v1/admin/ai/spawn`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${process.env.AI_AGENT_API_KEY}`,
        ...(args.idempotency_key ? { 'Idempotency-Key': args.idempotency_key } : {})
      },
      body: JSON.stringify(args)
    })
    return { content: [{ type: 'text', text: await res.text() }] }
  })

  // Tool 3: recycle_stale_coins
  server.registerTool('recycle_stale_coins', {
    agent_id: z.enum(['ai_spawn_governor', 'ai_game_master', 'ai_economy_balancer', 'ai_churn_agent']),
    reasoning: z.string().min(10).max(500),
    max_age_hours: z.number().min(1).max(168).default(48)
      .describe('Recycle coins uncollected for longer than this many hours'),
    zone_id: z.string().uuid().optional()
      .describe('Limit to specific zone; omit for all zones')
  }, async (args) => {
    const res = await fetch(`${ADMIN_API_BASE}/api/v1/admin/ai/recycle-stale`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${process.env.AI_AGENT_API_KEY}` },
      body: JSON.stringify(args)
    })
    return { content: [{ type: 'text', text: await res.text() }] }
  })

  // Tool 4: get_economy_health
  server.registerTool('get_economy_health', {}, async () => {
    const res = await fetch(`${ADMIN_API_BASE}/api/v1/admin/ai/economy-health`, {
      headers: { Authorization: `Bearer ${process.env.AI_AGENT_API_KEY}` }
    })
    return { content: [{ type: 'text', text: await res.text() }] }
  })

  // Tool 5: get_ai_actions
  server.registerTool('get_ai_actions', {
    agent_id: z.string().optional(),
    limit: z.number().min(1).max(200).default(20)
      .describe('Number of recent actions to return'),
    success: z.boolean().optional()
  }, async (args) => {
    const params = new URLSearchParams()
    if (args.agent_id) params.set('agent_id', args.agent_id)
    if (args.limit) params.set('limit', String(args.limit))
    if (args.success !== undefined) params.set('success', String(args.success))
    const res = await fetch(`${ADMIN_API_BASE}/api/v1/admin/ai/actions?${params}`, {
      headers: { Authorization: `Bearer ${process.env.AI_AGENT_API_KEY}` }
    })
    return { content: [{ type: 'text', text: await res.text() }] }
  })
}
```

---

## STEP 4: Spawn Governor — Logic Spec

**Deployed as**: Supabase Edge Function, scheduled every 5 minutes via `pg_cron` or Supabase Cron.  
**File**: `admin-dashboard/supabase/functions/spawn-governor/index.ts`

### Decision Pseudocode

```
SPAWN GOVERNOR LOOP (every 5 minutes):

1. SAFETY CHECKS (abort if any fail)
   a. Is distribution_config.enabled = true?  → if no, EXIT "kill switch active"
   b. Is get_ai_spend_this_hour() < limit?    → if no, EXIT "spend limit reached"
   c. Are there any active players?            → if no, EXIT "no players online"

2. FETCH GAME STATE
   a. Call get_hunt_pressure(active_window_minutes=30)
   b. Call get_economy_health()

3. ECONOMY GATE
   If economy_health.economy_status = 'margin_risk':
     → Log warning to ai_actions, EXIT without spawning
   If economy_health.supply_demand_ratio > 3.0:
     → Run recycle_stale_coins first, EXIT (don't add more)

4. SPAWN DECISIONS (per zone, ordered by hunt_pressure DESC)
   For each zone where needs_spawn = true:
     a. Check spend_remaining_usd > zone's min_coin_value → skip if not enough budget
     b. Determine spawn tier = zone.recommended_spawn_tier
     c. Generate idempotency_key = `spawn_gov_${zone_id}_${floor(Date.now() / 300000)}`
        (5-minute window key — prevents duplicates within same cycle)
     d. Call spawn_coin({
          zone_id, tier,
          agent_id: 'ai_spawn_governor',
          reasoning: `Zone ${zone_name}: ${active_player_count} players, ${active_coin_count} coins, pressure ${hunt_pressure}`,
          metadata: { hunt_pressure, player_count: active_player_count, economy_status },
          idempotency_key
        })
     e. If SPEND_LIMIT_EXCEEDED returned → stop all further spawns this cycle
     f. If success → continue to next zone
   
5. CLEANUP PASS
   For zones with active_player_count = 0 AND active_coin_count > zone.max_coins:
     Call recycle_stale_coins({ zone_id, max_age_hours: 6, reasoning: 'No active players, zone over capacity' })

6. LOG SUMMARY
   Single final ai_actions row summarizing the cycle:
   { tool: 'spawn_governor_cycle', result: { zones_processed, coins_spawned, coins_recycled, total_cost_usd } }
```

### Spawn Tier Rules

| Zone Player Breakdown | Spawn Tier |
|-----------------------|------------|
| Majority `cabin_boy` | `bronze` |
| Majority `deck_hand` | `silver` |
| Any `captain` or `king_of_pirates` present | `gold` (one gold max per cycle per zone) |
| Mixed (no majority) | `bronze` |

> **Why**: A Cabin Boy seeing locked red $25 coins everywhere is the fastest way to lose them. Match the world to the players in it.

---

## STEP 5: Realtime Wiring — React to Coin Collections

When a coin is collected, the Spawn Governor should be aware so it can react immediately (not wait 5 minutes). This is wired in the Spawn Governor Edge Function startup:

```typescript
// Inside spawn-governor Edge Function initialization
const supabase = createClient(SUPABASE_URL, SERVICE_ROLE_KEY)

supabase
  .channel('coin-collected-events')
  .on('postgres_changes', {
    event: 'UPDATE',
    schema: 'public',
    table: 'coins',
    filter: 'status=eq.collected'
  }, async (payload) => {
    const coin = payload.new
    // Trigger immediate zone pressure check for the zone this coin was in
    // (find zone via spawn_history where coin_id = coin.id)
    await checkZonePressureImmediate(coin.id)
  })
  .subscribe()
```

> **Note**: `coins` Realtime is enabled in Migration 014 (Step 1e above). `player_locations` Realtime is already enabled from Migration 003.

---

## TypeScript Type Updates Required

After running Migration 014, update `admin-dashboard/src/types/database.ts`:

### `Coin` interface — add 2 fields
```typescript
export interface Coin {
  // ... existing fields unchanged ...
  
  /** Who/what created this coin */
  created_by: 'system' | 'admin' | 'user' | 'ai_spawn_governor' | 'ai_game_master' | 'ai_economy_balancer'
  
  /** AI agent context attached at spawn time */
  metadata?: Record<string, unknown>
}
```

### `SpawnTriggerType` — expand enum
```typescript
export type SpawnTriggerType =
  | 'auto'
  | 'scheduled'
  | 'manual'
  | 'recycle'
  | 'ai_spawn_governor'    // ← new
  | 'ai_game_master'       // ← new
```

### New `AiAction` interface — add to bottom of file
```typescript
// ============================================================================
// AI AGENT TYPES — Phase AI-1: AI Integration
// ============================================================================

export type AiAgentId =
  | 'ai_spawn_governor'
  | 'ai_game_master'
  | 'ai_economy_balancer'
  | 'ai_churn_agent'

export interface AiAction {
  id: string
  agent_id: AiAgentId
  tool_called: string
  parameters: Record<string, unknown>
  reasoning: string | null
  result: Record<string, unknown> | null
  success: boolean
  error_code: string | null
  cost_usd: number
  created_at: string
}

export interface AiActionsSummary {
  total_actions: number
  total_cost_usd: number
  success_rate: number
  coins_spawned: number
  coins_recycled: number
  most_active_agent: AiAgentId | null
}
```

---

## 🔐 Guardrails Reference

These are non-negotiable. Every spawn route enforces all of them.

| Guardrail | Value | Where Enforced |
|-----------|-------|----------------|
| Hourly autonomous spend cap | `$10.00 USD` | `AI_AUTONOMOUS_SPEND_LIMIT_USD` constant + `get_ai_spend_this_hour()` |
| Kill switch | `distribution_config.enabled` boolean | Checked first in every spawn route |
| Max coins per cycle | `distribution_config.max_spawns_per_cycle` | Already exists in config |
| Idempotency window | 5-minute key window | `Idempotency-Key` header + cache |
| AI action audit | Every action logged to `ai_actions` | Route handlers + Spawn Governor |
| Human approval queue | Actions > `$50 USD` single spawn | Raise `GUARDRAIL_BLOCKED`, queue for admin review |
| Full rollback | All AI coins have `created_by = 'ai_*'` | Admin can bulk-expire by `created_by` in one SQL statement |

---

## ✅ Pre-Build Checklist

Before writing any code in a build session, confirm:

- [ ] Migrations 001–013 are applied to the target Supabase project
- [ ] `supabase db push` will be used to apply Migration 014
- [ ] `createServiceRoleClient()` is available in `src/lib/supabase/server.ts` ✅ (already exists)
- [ ] `AI_AGENT_API_KEY` env var is added to `.env.local` and Vercel
- [ ] `ADMIN_API_BASE_URL` env var is set for the MCP server
- [ ] `AI_AUTONOMOUS_SPEND_LIMIT_USD` is reviewed and agreed on before deploy

---

## 📎 Related Documents

| Document | Purpose |
|----------|---------|
| `Docs/AI-integration.md` | Vision document — the "why" and full feature list |
| `.cursor/skills/ai-first-human-second/SKILL.md` | Design principles — reference when building any new route |
| `admin-dashboard/src/types/database.ts` | TypeScript types to update after Migration 014 |
| `admin-dashboard/supabase/migrations/004_auto_distribution.sql` | Existing `spawn_coin()` function reference |
| `admin-dashboard/supabase/migrations/003_player_locations.sql` | Player location schema reference |

---

*"Know your tools before you draw them."* 🤠
