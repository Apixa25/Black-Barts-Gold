# 🤠 Zone Implementation Plan

> **Status**: Proposed build sequence  
> **Purpose**: Translate `Docs/archive/zone-architecture-proposal.md` into an additive, concrete migration and implementation plan  
> **Related docs**: `Docs/archive/zone-architecture-proposal.md`, `Docs/project-vision.md`, `Docs/AI-integration.md`, `Docs/AI-INTEGRATION-SPEC.md`, `Docs/session-handoff.md`

---

## Executive Summary

This document turns the zone architecture decision into a practical build sequence.

The core implementation rule is:

> **S2 cells become the canonical backend geography first. Then the AI Governor, dashboard, and spawn flows migrate to use them.**

We should do this in **small additive phases**, not a large rewrite.

That means:

- keep the current `zones` table
- keep the current Unity `ProximityZone` behavior
- add S2 spatial context to the existing backend schema
- update location and coin write paths to stamp S2 cell IDs
- migrate hunt pressure from named-zone reasoning to cell reasoning
- introduce a **cell-first spawn path** for the AI Governor

This sequence is designed to fit the additive, low-risk collaboration approach described in `Docs/project-vision.md` and `Docs/session-handoff.md`.

---

## Implementation Principles

### 1. Additive only

Do not delete or break working systems.

- add columns
- add helper modules
- add new route fields
- add new spawn behavior paths
- keep old behavior alive during transition

### 2. Server computes canonical spatial truth

The Unity app should continue sending raw GPS coordinates.

The backend should compute:

- `L17` S2 cell token
- `L14` parent S2 cell token
- optional named-zone overlay membership

### 3. AI-first, human-second

Following `.cursor/skills/ai-first-human-second/SKILL.md`:

- the AI Governor should read `data` + `meta` from routes
- responses should become more spatially explicit over time
- guardrails, auditability, and backward compatibility stay visible

### 4. Minimize scope creep

This plan is for:

- canonical spatial indexing
- hunt pressure
- AI spawn targeting
- overlay compatibility

This plan is **not** trying to fully build:

- territories
- sponsor cell coverage
- safety/banned zone automation
- player personalization logic

Those come later on top of the foundation.

---

## The Key Technical Decision

### Use S2 cell tokens as strings, not numeric IDs

We should store S2 cell identifiers as **text tokens**, not as PostgreSQL `bigint`.

### Why

S2 cell IDs are effectively unsigned 64-bit values. Using numeric DB types across:

- PostgreSQL
- Node.js / TypeScript
- JSON
- Supabase

creates unnecessary type friction and overflow risk.

### Recommendation

Store these as text:

- `s2_cell_token_l17`
- `s2_cell_token_l14`

This keeps:

- SQL simple
- JSON clean
- TypeScript safe
- future migrations low-risk

---

## Recommended Library Choice

### Backend S2 library

Recommended package:

- `s2js`

### Why `s2js`

It appears to be the best fit for this repo because it provides:

- TypeScript support
- S2 cell operations
- parent/child relationships
- neighbors
- polygon/region coverage tools

That makes it a better long-term fit than lighter ports that only cover basic cell math.

### Important boundary

For the first rollout, S2 computation should happen in the **Next.js / Node backend layer**, not inside PostgreSQL.

Why:

- lower implementation risk
- easier testing
- avoids Postgres-specific spatial complexity
- compatible with current route-based architecture

---

## What Changes First

The first milestone is not the AI Governor. The first milestone is **spatial truth on writes**.

That means:

1. when a player location is written, stamp S2 cells
2. when a coin is created, stamp S2 cells
3. when spawn history is written, stamp S2 cells

Once that exists, hunt pressure can become cell-driven.

---

## Build Sequence Overview

| Phase | Goal | Risk | Output |
|-------|------|------|--------|
| 0 | Preflight decisions | Low | locked vocabulary, library choice, sequencing |
| 1 | Add schema columns | Low | migrations for S2 cell tokens |
| 2 | Build spatial helper layer | Low | shared S2 utility module |
| 3 | Stamp player locations with S2 cells | Low | player location writes become cell-aware |
| 4 | Stamp coin writes with S2 cells | Medium | all major coin creation paths become cell-aware |
| 5 | Backfill legacy rows | Medium | existing players/coins/history gain spatial context |
| 6 | Migrate hunt pressure to cells | Medium | AI Governor reads cell-based pressure |
| 7 | Add cell-first spawn path | Medium | AI Governor can spawn by cell, not by zone |
| 8 | Update dashboard + rollout | Medium | UI reads new cell model cleanly |

---

## Phase 0 — Preflight Decisions

### Objective

Lock the minimum implementation decisions before touching schema.

### Decisions to lock

- S2 is the canonical backend geography
- `L17` is the first pressure cell
- `L14` is the first summary/rollup cell
- `zones` becomes a named-zone overlay system
- S2 cell IDs are stored as **text tokens**
- `s2js` is the initial backend S2 library

### Deliverables

- `Docs/archive/zone-architecture-proposal.md`
- `Docs/archive/zone-implementation-plan.md`

### Exit criteria

- team language is aligned around `SpatialCell`, `NamedZone`, `ProximityZone`
- no one is using "zone" ambiguously in the implementation plan

---

## Phase 1 — Migration: Add S2 Spatial Columns

### Objective

Add S2 spatial context fields without breaking any existing behavior.

### Recommended migration file

```sql
admin-dashboard/supabase/migrations/017_s2_spatial_context.sql
```

### Tables to update

#### `public.player_locations`

Add:

- `s2_cell_token_l17 text`
- `s2_cell_token_l14 text`

Keep:

- `current_zone_id`

But redefine it in comments as:

- primary named-zone overlay membership, not canonical geography

#### `public.coins`

Add:

- `s2_cell_token_l17 text`
- `s2_cell_token_l14 text`

#### `public.spawn_history`

Add:

- `s2_cell_token_l17 text`
- `s2_cell_token_l14 text`

### Recommended indexes

Add indexes on:

- `player_locations.s2_cell_token_l17`
- `player_locations.s2_cell_token_l14`
- `coins.s2_cell_token_l17`
- `coins.s2_cell_token_l14`
- `spawn_history.s2_cell_token_l17`
- `spawn_history.s2_cell_token_l14`

### Important comments to add

- S2 tokens are the canonical backend geography
- `current_zone_id` is now overlay membership only
- these fields are additive and may be null during backfill

### Example migration snippet

```sql
admin-dashboard/supabase/migrations/017_s2_spatial_context.sql
ALTER TABLE public.player_locations
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

ALTER TABLE public.coins
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;

ALTER TABLE public.spawn_history
  ADD COLUMN IF NOT EXISTS s2_cell_token_l17 TEXT,
  ADD COLUMN IF NOT EXISTS s2_cell_token_l14 TEXT;
```

### Exit criteria

- migration applies cleanly
- no existing routes break
- all new columns are nullable at this stage

---

## Phase 2 — Build Shared Spatial Helper Layer

### Objective

Create one backend module that all routes use for S2 logic.

### Recommended file

```typescript
admin-dashboard/src/lib/geo/s2.ts
```

### Responsibilities

- convert lat/lng to L17 token
- convert lat/lng to L14 token
- get parent token from child token
- get neighbor tokens for smoothing
- get cell center for debugging/response payloads

### Recommended companion file

```typescript
admin-dashboard/src/lib/geo/named-zone-membership.ts
```

### Responsibilities

- determine whether a point is inside one or more active named zones
- return primary named-zone membership
- preserve compatibility with current `zones` table geometry

### Recommended helper API

```typescript
admin-dashboard/src/lib/geo/s2.ts
export function getSpatialCellContext(latitude: number, longitude: number) {
  return {
    s2CellTokenL17: string,
    s2CellTokenL14: string,
    parentCellToken: string,
  }
}
```

### Important design rule

All routes should use the same helper. Do not duplicate S2 logic inside route files.

### Exit criteria

- one shared spatial helper exists
- one shared named-zone helper exists
- unit test targets are identifiable even if tests are added later

---

## Phase 3 — Stamp Player Location Writes

### Objective

Make `POST /api/v1/player/location` the first canonical spatial write path.

### File to update

```typescript
admin-dashboard/src/app/api/v1/player/location/route.ts
```

### Current behavior

The route currently writes:

- lat/lng
- movement type
- device/session metadata

It does **not** write canonical geography beyond raw coordinates.

### New behavior

On every location update:

1. validate coordinates
2. compute `s2_cell_token_l17`
3. compute `s2_cell_token_l14`
4. compute optional primary named-zone overlay membership
5. upsert all of that into `player_locations`

### Keep compatibility

Keep these response fields unchanged:

- `success`
- `locationId`
- `movementType`
- `timestamp`

Optionally add:

- `spatialCellL17`
- `spatialCellL14`
- `currentNamedZoneId`

### Important semantic change

`current_zone_id` should stop meaning "the canonical zone the player is in."

It should now mean:

- the primary named zone overlay containing the player, or `NULL`

### Recommended precedence for primary named zone

Use this only for compatibility:

1. active sponsor zone
2. active hunt zone
3. active player/personalized zone
4. active legacy grid zone

If multiple zones of the same type match, choose the smallest area or most specific geometry.

### Exit criteria

- new player location writes include S2 cell tokens
- current dashboard map still works
- no consumer relies on `current_zone_id` for core spatial truth

---

## Phase 4 — Stamp Coin Writes

### Objective

Ensure all major coin creation paths attach S2 spatial context.

This phase matters because hunt pressure cannot become cell-first until coins are cell-aware.

### Coin write paths to cover first

#### Path A: Manual / player coin hiding

```typescript
admin-dashboard/src/app/api/v1/coins/hide/route.ts
```

This route inserts directly into `coins`, so it should compute and write:

- `s2_cell_token_l17`
- `s2_cell_token_l14`

at insert time.

#### Path B: AI spawn route

```typescript
admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts
```

This route currently calls `spawn_coin(zone_id, ...)`, then stamps metadata on `coins`.

It should also stamp:

- `coins.s2_cell_token_l17`
- `coins.s2_cell_token_l14`
- matching values on the corresponding `spawn_history` row

### Important challenge

The current SQL function `spawn_coin(zone_id, ...)` is **zone-first**, not cell-first.

That means we need a transition strategy.

### Recommended transition strategy

#### Step 1

Keep `spawn_coin(zone_id, ...)` alive for legacy and overlay-based flows.

#### Step 2

Add a new DB function for cell-first spawning:

```sql
admin-dashboard/supabase/migrations/018_spawn_coin_at_location.sql
```

Recommended function:

- `spawn_coin_at_location(...)`

Inputs:

- `p_trigger_type`
- `p_coin_type`
- `p_tier`
- `p_value`
- `p_latitude`
- `p_longitude`
- optional `p_zone_id` for overlay association

Why this function matters:

- canonical geography is now cell-first
- the AI Governor should not need a named zone in order to spawn
- overlay zone association stays optional

### Why not force SQL to compute S2 cells?

Because the least-risk first implementation is:

- compute S2 in Node/TS
- pass explicit lat/lng to SQL
- stamp S2 tokens after insert

This is easier to test and safer to roll out.

### Exit criteria

- `/coins/hide` writes S2 tokens directly
- AI spawn route can write or enrich coins with S2 tokens
- spawn history rows gain matching S2 tokens

---

## Phase 5 — Backfill Existing Spatial Data

### Objective

Bring existing rows up to the new standard.

### Why this is necessary

Without backfill:

- hunt pressure will see partial data
- AI decisions will be skewed
- dashboard cells will look empty or inconsistent

### Recommended backfill script

```typescript
admin-dashboard/scripts/backfill-s2-spatial-context.ts
```

### Backfill targets

1. `player_locations`
2. `coins`
3. `spawn_history`

### Backfill order

#### First: `player_locations`

Smallest critical live dataset.

#### Second: `coins`

Needed for pressure and spawn balancing.

#### Third: `spawn_history`

Needed for historical analytics and future AI analysis.

### Backfill strategy

- batch reads
- compute tokens in Node
- update rows in chunks
- log counts per table
- support resume/retry

### Operational rule

Backfill should be safe to rerun.

### Exit criteria

- recent active rows are fully stamped
- null S2 token counts are known and trending to zero
- pressure queries can safely move to cell-based aggregation

---

## Phase 6 — Migrate Hunt Pressure To Cells

### Objective

Change the AI Governor's main input from named zones to spatial cells.

### File to update

```typescript
admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts
```

### Current behavior

The route currently:

- reads active named zones
- groups players by `current_zone_id`
- groups active coins by `spawn_history.zone_id`

This is exactly the logic we are replacing.

### New behavior

The route should:

1. read active players from `player_locations`
2. group them by `s2_cell_token_l17`
3. read active coins from `coins`
4. group them by `s2_cell_token_l17`
5. compute hunt pressure per cell
6. roll up summaries to `L14`
7. attach named-zone overlays as modifiers or annotations

### Important source-of-truth change

For active coin counts, use:

- `public.coins`

not:

- `spawn_history`

Why:

- `coins` is the live state
- `spawn_history` is historical context
- pressure should operate on what is active right now

### Recommended response transition

Do not hard-break consumers.

#### Transitional response shape

```typescript
admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts
{
  success: true,
  data: {
    cells: [...],
    summary: {...},
    zones: [...] // temporary compatibility block if still needed by existing UI
  },
  meta: {...}
}
```

### Canonical `cells[]` fields

- `cell_id`
- `cell_level`
- `parent_cell_id`
- `center`
- `active_player_count`
- `active_coin_count`
- `hunt_pressure`
- `player_tier_distribution`
- `recommended_spawn_tier`
- `named_zone_overlays`

### Recommended `meta` additions

- `high_pressure_cells`
- `cells_needing_spawn`
- `total_active_cells`
- `cell_level_used`

### Exit criteria

- AI Governor can reason over `cells[]`
- dashboard still renders during transition
- no critical logic depends on `current_zone_id` for pressure

---

## Phase 7 — Add Cell-First Spawning For The AI Governor

### Objective

Make the AI Governor spawn into spatial cells directly.

This is the most important behavioral change in the whole plan.

### Current problem

The current AI route assumes:

- the target unit is a named zone
- the DB function `spawn_coin(zone_id, ...)` is the correct primitive

That is no longer true for canonical geography.

### New rule

The AI Governor should choose a **pressure cell**, not a named zone.

### Recommended route evolution

Keep the existing route path:

```typescript
admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts
```

But evolve the request shape to support:

- `cell_id` or `cell_token_l17`
- optional `zone_id` as overlay context
- optional explicit `latitude` and `longitude`

### Recommended route behavior

#### If `cell_token_l17` is provided

1. compute a legal spawn point within the cell
2. optionally validate against named zone overlays
3. call `spawn_coin_at_location(...)`
4. stamp S2 tokens and overlay context

#### If only `zone_id` is provided

Use legacy behavior for backward compatibility.

### Recommended spawn point algorithm

Initial version:

1. take target L17 cell
2. choose randomized point inside that cell
3. reject if inside banned overlay
4. reject if too close to existing active coins
5. retry a small fixed number of times
6. fall back to cell center if needed

### Why this is the right cut

It lets:

- AI Governor become cell-first now
- sponsor/event flows remain zone-compatible
- current SQL helper remain useful during transition

### Exit criteria

- AI spawn route accepts a cell-first request
- cell-based hunt pressure can call cell-based spawn
- legacy zone-based spawn still works

---

## Phase 8 — Dashboard And Governor Rollout

### Objective

Make the admin UI show the same spatial truth the AI is using.

### Likely UI areas to update

- AI Governor pressure grid
- map overlays
- player map filters
- zone detail panels

### Recommended behavior

- dashboard should render `cells[]` as the canonical pressure units
- named zones render as overlays on top
- summaries should be able to roll up to `L14`

### Important UX principle

Humans should still see meaningful labels.

That means the raw AI unit can be:

- `cell_id`

but UI should also show:

- approximate center
- matching named overlays
- human-friendly descriptive labels when available

### Exit criteria

- dashboard and AI Governor are reading the same spatial model
- operators understand what the AI is acting on
- "zone" ambiguity is removed from the UI

---

## Concrete Migration Sequence

This is the recommended order of work.

### Step 1

Create:

```sql
admin-dashboard/supabase/migrations/017_s2_spatial_context.sql
```

Adds:

- S2 token columns
- indexes
- comments

### Step 2

Create:

```typescript
admin-dashboard/src/lib/geo/s2.ts
admin-dashboard/src/lib/geo/named-zone-membership.ts
```

Adds:

- shared S2 logic
- shared named-zone membership logic

### Step 3

Update:

```typescript
admin-dashboard/src/app/api/v1/player/location/route.ts
```

Adds:

- S2 stamping on location upsert
- optional overlay membership update

### Step 4

Update:

```typescript
admin-dashboard/src/app/api/v1/coins/hide/route.ts
```

Adds:

- S2 stamping on manual/player-hidden coin creation

### Step 5

Create:

```sql
admin-dashboard/supabase/migrations/018_spawn_coin_at_location.sql
```

Adds:

- cell-first spawn DB function

### Step 6

Update:

```typescript
admin-dashboard/src/app/api/v1/admin/ai/spawn/route.ts
```

Adds:

- cell-first request support
- `spawn_coin_at_location(...)` path
- legacy `zone_id` compatibility path
- S2 stamping on coin + spawn history

### Step 7

Create:

```typescript
admin-dashboard/scripts/backfill-s2-spatial-context.ts
```

Adds:

- batched backfill for players, coins, and spawn history

### Step 8

Update:

```typescript
admin-dashboard/src/app/api/v1/admin/ai/hunt-pressure/route.ts
```

Adds:

- cell-first aggregation
- `cells[]` canonical response
- transitional compatibility fields if needed

### Step 9

Update AI Governor callers

Likely files:

```typescript
admin-dashboard/supabase/functions/spawn-governor/index.ts
admin-dashboard/src/app/api/v1/admin/ai/trigger-governor/route.ts
```

Adds:

- cell-first pressure consumption
- cell-first spawn requests

### Step 10

Update dashboard UI consumers

Likely files:

```typescript
admin-dashboard/src/app/(dashboard)/ai-governor/ai-governor-client.tsx
admin-dashboard/src/app/(dashboard)/zones/zones-client.tsx
```

Adds:

- `cells[]` rendering
- overlays rendered distinctly from canonical spatial units

---

## Verification Checklist By Phase

### After Phase 1

- migrations apply cleanly
- types regenerate or align manually
- no route failures

### After Phase 3

- new player location writes include non-null S2 tokens
- old clients still work without payload changes

### After Phase 4

- hidden/manual coins include S2 tokens on insert

### After Phase 6

- AI spawn route can produce coins with S2 tokens
- spawn history rows also contain matching S2 tokens

### After Phase 7

- backfill reduces null spatial tokens close to zero

### After Phase 8

- hunt pressure returns meaningful `cells[]`
- pressure counts match sampled DB reality

### After Phase 9

- Governor can spawn using cell-first requests
- guardrails and audit logs still work exactly as before

---

## Risks And Mitigations

### Risk 1: Partial spatial coverage during rollout

If some rows have S2 tokens and others do not, analytics become inconsistent.

Mitigation:

- keep columns nullable initially
- backfill quickly after write paths are updated
- expose counts of null spatial tokens during rollout

### Risk 2: SQL-only spawn paths bypass spatial stamping

Some legacy SQL-driven paths may still create coins without Node-side enrichment.

Mitigation:

- add reconciliation/backfill job
- prioritize migrating AI Governor to `spawn_coin_at_location(...)`
- gradually reduce dependence on zone-first SQL spawning

### Risk 3: Dashboard confusion during transition

Operators may still think named zones are the canonical pressure unit.

Mitigation:

- show cells and overlays distinctly
- keep human-readable labels
- update docs and session handoff at the same time

### Risk 4: Boundary noise on small cells

Players near a cell edge can make pressure look artificially split.

Mitigation:

- use L14 rollups for sanity checks
- add neighbor smoothing in pressure logic only where needed

---

## Recommended First Coding Sprint

If we want the smallest high-value implementation slice, it should be:

1. `017_s2_spatial_context.sql`
2. `src/lib/geo/s2.ts`
3. update `POST /api/v1/player/location`
4. update `POST /api/v1/coins/hide`
5. backfill players + coins

Why this sprint first:

- it creates canonical spatial truth
- it does not require dashboard redesign yet
- it gives us real data to inspect before changing Governor logic

Then sprint two should be:

1. `018_spawn_coin_at_location.sql`
2. update AI spawn route
3. update hunt-pressure route
4. update Governor caller

---

## Final Recommendation

Do the implementation in this order:

> **Schema first, shared spatial helper second, write-path stamping third, backfill fourth, hunt pressure fifth, AI spawn sixth, dashboard seventh.**

That order is the safest way to turn the proposal into reality without breaking current behavior.

It preserves:

- the working Unity client
- the current admin dashboard
- the existing `zones` table
- the current AI route surface

while steadily moving the system toward the correct long-term model:

> **S2 cells as canonical geography, named zones as overlays, and proximity zones as client-only feedback.**

---

## Proposed Next Step

The next concrete work item should be to implement **Phase 1 + Phase 2 + Phase 3** together:

- create `017_s2_spatial_context.sql`
- add `admin-dashboard/src/lib/geo/s2.ts`
- update `admin-dashboard/src/app/api/v1/player/location/route.ts`

That gives the repo its first real canonical spatial write path and creates the foundation for every later phase.
