# MCP Gap Review — Sprint 1

> Purpose: map the current MCP tool surface against the real AI/admin backend so Sprint 2 can build Black Bart's runtime from concrete gaps instead of guesswork.

---

## Current State

The repo currently has:

- **12 admin AI route files** under:
  - `admin-dashboard/src/app/api/v1/admin/ai/`
- **5 MCP tools** in:
  - `mcp-server/src/game-mcp-server.ts`
- **3 MCP resources** in:
  - `mcp-server/src/game-mcp-server.ts`
- **1 player-facing companion route** in:
  - `admin-dashboard/src/app/api/v1/player/companion/route.ts`

The key architectural truth:

- The **backend AI surface is ahead of the MCP surface**
- The **player companion context exists in route-local helper code**, not reusable MCP or runtime tools
- The **MCP layer is still shaped like Phase 1 Spawn Governor support**, not Phase 2 Black Bart companion/runtime support

---

## Current MCP Coverage

### MCP tools already exposed

From `mcp-server/src/game-mcp-server.ts`:

1. `get_hunt_pressure`
2. `spawn_coin`
3. `recycle_stale_coins`
4. `get_economy_health`
5. `get_ai_actions`

### MCP resources already exposed

1. `game://guardrails`
2. `game://economy/health`
3. `game://hunt/pressure`

These tools are enough for the **Spawn Governor economy loop**, but not enough for a real Black Bart runtime.

---

## Backend AI Surface Already Built

### Economy / governor routes

- `admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/economy-health/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/recycle-stale/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/actions/route.ts`

### Scheduling / orchestration routes

- `admin-dashboard/src/app/api/v1/admin/ai/spawn-queue/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/process-spawn-queue/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/timed-releases/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/process-timed-releases/route.ts`

### Admin-only control routes

- `admin-dashboard/src/app/api/v1/admin/ai/kill-switch/route.ts`
- `admin-dashboard/src/app/api/v1/admin/ai/trigger-governor/route.ts`

### Player-facing companion route

- `admin-dashboard/src/app/api/v1/player/companion/route.ts`

---

## Gap Matrix

## 1. Already exposed in MCP

These are usable today:

- `GET /api/v1/admin/ai/hunt-pressure` → `get_hunt_pressure`
- `POST /api/v1/admin/ai/spawn` → `spawn_coin`
- `POST /api/v1/admin/ai/recycle-stale` → `recycle_stale_coins`
- `GET /api/v1/admin/ai/economy-health` → `get_economy_health`
- `GET /api/v1/admin/ai/actions` → `get_ai_actions`

## 2. Built in backend, not exposed in MCP

These are the most important uncovered AI routes:

- `GET /api/v1/admin/ai/spawn-queue`
- `POST /api/v1/admin/ai/spawn-queue`
- `POST /api/v1/admin/ai/process-spawn-queue`
- `GET /api/v1/admin/ai/timed-releases`
- `POST /api/v1/admin/ai/timed-releases`
- `POST /api/v1/admin/ai/process-timed-releases`

These matter because they already support:

- future scheduling
- deferred action planning
- cell-backed orchestration
- AI-created queue items and release schedules

## 3. Backend exists, but should remain admin-only

These should **not** be MCP tools for autonomous Black Bart:

- `POST /api/v1/admin/ai/kill-switch`
- `POST /api/v1/admin/ai/trigger-governor`

Reason:

- `kill-switch` is explicitly human-admin control
- `trigger-governor` is a ranch-house supervision button, not a runtime tool Black Bart should call on himself

## 4. Needed for Sprint 2 Black Bart runtime, but not built as reusable tools yet

These are the real gaps for the companion/runtime path:

- `get_player_context`
- `get_selected_coin_context`
- `get_hider_context`
- `get_recent_companion_history`
- `get_local_hunt_pressure`
- `build_black_bart_reply`

Important note:

The underlying data access already exists inside:

- `admin-dashboard/src/app/api/v1/player/companion/route.ts`

But it exists as **route-local helper code**, not reusable runtime modules or MCP tools.

---

## Important Mismatches

## 1. MCP `spawn_coin` is still zone-centric

The backend spawn route already supports:

- `cell_id`
- `zone_id`
- optional explicit coordinates

But the MCP tool in `mcp-server/src/game-mcp-server.ts` still only exposes:

- `zone_id`
- `tier`
- optional lat/lng

This means MCP is behind the backend's newer cell-first world model.

## 2. MCP `recycle_stale_coins` is behind the backend too

The backend recycle route supports:

- `cell_id`
- `zone_id`
- metadata

The MCP tool currently exposes:

- `zone_id`
- `max_age_hours`
- no `cell_id`
- no metadata

That is another concrete mismatch.

## 3. No MCP surface for queueing/scheduling

The backend already supports:

- queueing spawn work
- scheduling timed releases
- processing those plans

The MCP server exposes none of that yet.

## 4. No reusable player-context tool layer

For Black Bart Brain v1, the biggest gap is not economy tools.

It is the lack of reusable runtime helpers around:

- player state
- selected coin
- hider profile
- recent companion interactions

Those should be built first as internal runtime modules before deciding whether to promote them into MCP tools.

---

## Recommendation For Sprint 2

## Build internal runtime helpers first

For Sprint 2, do **not** start by expanding MCP aggressively.

Instead:

1. Extract reusable helper modules from:
   - `admin-dashboard/src/app/api/v1/player/companion/route.ts`
2. Build Black Bart runtime internals under:
   - `admin-dashboard/src/lib/black-bart/`
3. Keep the current companion route as the outer API shell
4. Use the existing scripted companion engine as fallback

This is the lowest-risk route to a real AI companion.

## After that, add the next MCP wave

Once Black Bart Brain v1 works server-side, the next MCP tools should be:

1. `queue_spawn`
2. `get_spawn_queue`
3. `schedule_timed_release`
4. `get_timed_releases`

And only after the companion runtime is stable should we consider formal MCP tools like:

1. `get_player_context`
2. `get_selected_coin_context`
3. `get_hider_context`

---

## Concrete Sprint 2 MCP Decision

### Do in Sprint 2

- Extract internal helper functions from the companion route
- Build `admin-dashboard/src/lib/black-bart/context.ts`
- Build `admin-dashboard/src/lib/black-bart/runtime.ts`
- Keep these as **server-internal tools**, not MCP tools

### Defer until after Sprint 2

- expanding MCP for player-companion context
- adding notification/event MCP tools
- exposing admin-only control tools

### Optional small MCP upgrade during Sprint 2

If we want one small MCP improvement while staying low-risk:

- update `spawn_coin` to accept `cell_id`
- update `recycle_stale_coins` to accept `cell_id`

That would align MCP with the backend's cell-first direction without opening a much larger tool surface.

---

## Bottom Line

The MCP server is not "wrong" or broken. It is just **one phase behind** the backend.

Right now it is sufficient for:

- Spawn Governor
- economy observation
- basic spawn/recycle actions

It is not yet sufficient for:

- Black Bart Brain v1
- player-context-driven companion intelligence
- queued/scheduled orchestration

So the smartest next move is:

- **Build Black Bart's internal runtime helper layer first**
- **Use MCP expansion as Phase 2, not Phase 1**

That keeps Sprint 2 focused, additive, and aligned with the AI-first philosophy documented in:

- `Docs/project-vision.md`
- `Docs/AI-integration.md`

