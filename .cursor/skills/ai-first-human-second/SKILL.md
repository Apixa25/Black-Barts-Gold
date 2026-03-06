---
name: ai-first-human-second
description: Applies AI-agent-first design principles when building API endpoints, database schemas, admin dashboard features, or any backend for Black Bart's Gold. The AI agent is the PRIMARY consumer; the human admin dashboard is secondary — but both work. Use when building new API routes, writing migrations, designing admin features, or when the user asks to "build AI-first", "make this agent-friendly", or "design for the MCP server".
---

# AI First, Human Second — BBG Design Principles

The AI agent is the primary consumer of every admin API endpoint and schema. Human dashboard UI is built on top of the same endpoints. Both work — the AI just gets richer data.

For full architecture context, see [Docs/AI-integration.md](../../Docs/AI-integration.md) and [Docs/AI-INTEGRATION-SPEC.md](../../Docs/AI-INTEGRATION-SPEC.md).

---

## 1. Endpoint Naming — Action Verbs, Not Nouns

Every admin route maps directly to an MCP tool name. Use action verbs.

```
✅  POST /api/v1/admin/ai/spawn              → spawn_coin
✅  GET  /api/v1/admin/ai/hunt-pressure      → get_hunt_pressure
✅  POST /api/v1/admin/ai/recycle-stale      → recycle_stale_coins
✅  GET  /api/v1/admin/ai/economy-health     → get_economy_health

❌  GET  /api/v1/admin/coins                 → too noun-y, no clear action
❌  POST /api/v1/admin/management/coin-ops   → vague, not mappable to a tool
```

Game-facing Unity routes (`/coins/nearby`, `/player/location`) are separate and unchanged.

---

## 2. Every Response Gets a `meta` Block

The `meta` block is what separates an AI-friendly API from a human-only one. The agent reads `meta` to decide what to do next without reasoning from scratch.

```typescript
// Standard AI-first response shape
return NextResponse.json({
  data: { /* the actual results */ },
  meta: {
    recommended_action: "spawn_coins",          // pre-computed suggestion
    low_pressure_zones: ["zone-id-1"],          // agent acts on this directly
    spend_this_hour_usd: 4.50,                  // current spend
    autonomous_spend_limit_usd: 10.00,          // hard guardrail, always visible
    spend_remaining_usd: 5.50,                  // agent checks before acting
    zones_needing_spawn: 3,
    total_active_players: 47,
  },
  _links: {
    spawn: "/api/v1/admin/ai/spawn",            // agent knows what to call next
    economy: "/api/v1/admin/ai/economy-health"
  },
  timestamp: new Date().toISOString()
})
```

Human dashboard UI simply ignores `meta` and renders `data`. No conflict.

**`recommended_action` values for coins/zones:**
- `"spawn_coins"` — pressure high, coins needed
- `"recycle_stale"` — old coins sitting in dead zones
- `"no_action_needed"` — system healthy
- `"spend_limit_approaching"` — warn before cutoff

---

## 3. `created_by` on Every Mutation

Every `INSERT` or `UPDATE` triggered by the AI must record who did it. Use these values consistently:

| Value | Meaning |
|-------|---------|
| `"admin"` | Human admin via dashboard UI |
| `"system"` | Auto-distribution background job |
| `"ai_spawn_governor"` | Spawn Governor AI agent |
| `"ai_game_master"` | Black Bart AI character agent |
| `"ai_economy_balancer"` | Economy Balancer AI agent |

Apply to: `coins.created_by`, `spawn_history.created_by`, `ai_actions.agent_id`.

---

## 4. Structured Error Codes (Machine-Readable)

Errors must be machine-readable so the agent can react, not just display.

```typescript
// ✅ AI-first error — agent can check code and retry logic
return NextResponse.json({
  success: false,
  error: "Autonomous spend limit reached for this hour",
  code: "SPEND_LIMIT_EXCEEDED",
  meta: {
    spend_this_hour_usd: 10.00,
    limit_usd: 10.00,
    resets_at: "2026-03-02T15:00:00Z"   // agent knows exactly when to retry
  }
}, { status: 429 })

// ❌ Human-only error — agent can't act on this
return NextResponse.json({ error: "Something went wrong" }, { status: 500 })
```

**Standard error codes for BBG AI routes:**

| Code | Meaning |
|------|---------|
| `SPEND_LIMIT_EXCEEDED` | AI hit hourly autonomous spend cap |
| `ZONE_NOT_FOUND` | Invalid zone_id |
| `IDEMPOTENCY_CONFLICT` | Duplicate request with same key |
| `GUARDRAIL_BLOCKED` | Action requires human approval |
| `DISTRIBUTION_DISABLED` | Kill switch is active |

---

## 5. Idempotency Keys on All Spawn/Mutate Routes

AI agents retry on failure. Without idempotency keys, retries create duplicate coins (real money problem).

```typescript
// Agent sends this header with every mutating request:
// Idempotency-Key: "spawn_governor_2026-03-02T14:35:00Z_zone-abc123"

// Route handler checks:
const idempotencyKey = request.headers.get('Idempotency-Key')
if (idempotencyKey) {
  const existing = await checkIdempotencyCache(idempotencyKey)
  if (existing) return NextResponse.json(existing) // replay cached response
}
```

---

## 6. The `ai_actions` Log Table

Every AI mutation must write a row to `ai_actions`. This is the audit trail, rollback reference, and "What did Black Bart do today?" dashboard source.

```sql
-- Migration: add to 014_ai_schema.sql
CREATE TABLE public.ai_actions (
  id             UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  agent_id       TEXT NOT NULL,       -- 'ai_spawn_governor', 'ai_game_master'
  tool_called    TEXT NOT NULL,       -- 'spawn_coin', 'recycle_stale'
  parameters     JSONB NOT NULL,      -- exactly what was passed
  reasoning      TEXT,                -- AI's stated reason (from prompt)
  result         JSONB,               -- what happened
  cost_usd       DECIMAL(10,4) DEFAULT 0,
  success        BOOLEAN NOT NULL,
  error_code     TEXT,
  created_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

Write to this table BEFORE committing the action. If the action fails, update `success = false`.

---

## 7. Hard Guardrails — Always Enforce, Always Surface

Every agent-facing route must enforce AND surface the spend guardrail in `meta`.

```typescript
// Check at top of every spawn/mutate handler
const spendThisHour = await getAiSpendThisHour()
const SPEND_LIMIT_USD = 10.00  // from distribution_config or env var

if (spendThisHour >= SPEND_LIMIT_USD) {
  return NextResponse.json({
    success: false,
    code: "SPEND_LIMIT_EXCEEDED",
    meta: { spend_this_hour_usd: spendThisHour, limit_usd: SPEND_LIMIT_USD }
  }, { status: 429 })
}
```

Additionally: a kill switch toggle in `distribution_config.enabled` stops ALL AI spawning instantly. Always check this first.

---

## 8. Schema Migrations — Additive Only

When adding AI support to existing tables:

```sql
-- ✅ Additive — never breaks existing code
ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS created_by TEXT DEFAULT 'system',
  ADD COLUMN IF NOT EXISTS metadata   JSONB;

ALTER TABLE public.spawn_history
  ADD COLUMN IF NOT EXISTS created_by TEXT DEFAULT 'system';

-- Add 'ai_spawn_governor' to existing trigger_type CHECK
-- Use a migration comment explaining why
```

Never drop columns. Never change existing CHECK constraints — add new ones or expand them.

---

## Quick Checklist

Before shipping any new admin route or migration, verify:

- [ ] Endpoint name is an action verb that maps to an MCP tool name
- [ ] Response includes a `meta` block with `recommended_action`
- [ ] Response includes `_links` pointing to related agent actions
- [ ] `spend_remaining_usd` and `autonomous_spend_limit_usd` appear in `meta` on any financial route
- [ ] All mutations accept and record `created_by`
- [ ] Error responses use structured `code` fields
- [ ] Mutating routes support `Idempotency-Key` header
- [ ] Mutations write to `ai_actions` log
- [ ] Spend guardrail is checked before any coin spawn
- [ ] Kill switch (`distribution_config.enabled`) is checked first
- [ ] Human dashboard can use this same endpoint — `data` block is clean for UI rendering

---

## The Core Rule

> **Every response must work for both an AI agent reading JSON and a human reading a dashboard. The `data` block serves the UI. The `meta` block serves the agent. Never sacrifice one for the other.**
