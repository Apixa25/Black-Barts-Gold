# 🤠 Zone Architecture Proposal

> **Status**: Proposed  
> **Purpose**: Define what a "zone" means in Black Bart's Gold so the AI Governor, admin dashboard, and Unity client all speak the same geospatial language.  
> **Related docs**: `Docs/project-vision.md`, `Docs/AI-integration.md`, `Docs/AI-INTEGRATION-SPEC.md`, `Docs/session-handoff.md`

---

## Executive Summary

Black Bart's Gold should adopt **Google's S2 cell system** as the canonical way to partition the world for backend gameplay logic.

That means:

- The **backend world model** should be based on **S2 spatial cells**
- The existing `zones` table should become a **named zone overlay system**, not the base map partition
- The Unity client's current `ProximityZone` concept should remain a **local coin-distance state**, completely separate from world geography

This proposal keeps the architecture aligned with the AI-first direction in `Docs/project-vision.md`, matches the S2 direction already called out in `Docs/AI-integration.md`, and gives the AI Governor a stable, shared, queryable map grammar.

---

## Why This Proposal Exists

Right now, the repo uses the word **zone** to mean multiple different things:

1. **Unity gameplay zone**
   The player's distance band from the nearest coin: `OutOfRange`, `Far`, `Medium`, `Near`, `Collectible`

2. **Backend/admin zone**
   A geographic area used for spawning, analytics, sponsor features, and hunt pressure

3. **Design-language zone**
   A general idea like "the player spawns into a zone" that is not yet fully implemented in runtime code

That ambiguity is already causing architectural confusion.

The AI Governor needs to know:

- where players are
- where coins are
- which areas are over-supplied
- which areas are under-supplied
- which regions are sponsor/event/safety overlays
- where to spawn with low risk and high confidence

To do that well, the system needs a **stable base geography**. A player-radius model is too temporary. A hand-made polygon-only model is too manual. A simple quadrant model is too crude.

The correct foundation is a **hierarchical spatial grid**.

---

## Design Goals

This proposal optimizes for the goals already established in `Docs/project-vision.md` and `Docs/AI-integration.md`.

### Product goals

- Make the world feel alive and responsive
- Let the AI Governor react to real player density
- Support sponsor zones, event zones, and territory systems later
- Avoid flooding new players with locked high-tier content

### Technical goals

- Use a shared world model for all players
- Make geospatial aggregation cheap and deterministic
- Support multiple scales of reasoning
- Keep the rollout additive and low-risk
- Avoid rewriting working Unity proximity behavior

### AI-first goals

- Give the AI Governor clean, stable spatial units
- Make pressure, density, and balancing machine-readable
- Keep the world partition understandable in API responses and audit logs

---

## Core Decision

### Canonical decision

**Black Bart's Gold should use S2 cells as the canonical backend spatial unit.**

### What that means

- Every player location maps to one or more **S2 cell IDs**
- Every coin location maps to one or more **S2 cell IDs**
- Hunt pressure is calculated per **S2 cell**
- Spawn decisions are made per **S2 cell**
- Named zones like sponsor areas and event regions are **overlays** on top of cells

### What that does not mean

- A player's personal radius is **not** the canonical world zone
- The existing `zones` table is **not** the base partition of the world
- Unity's `ProximityZone` should **not** be reused for backend geography

---

## Vocabulary: The New Shared Language

This section should become the source of truth for the team and the codebase.

### 1. `ProximityZone`

**Definition**: A local gameplay state in the Unity client that describes how close the player is to the nearest coin.

Examples:

- `OutOfRange`
- `Far`
- `Medium`
- `Near`
- `Collectible`

**Used for**:

- haptics
- compass/radar UI
- collection readiness
- local AR feedback

**Not used for**:

- AI Governor geography
- global spawn balancing
- dashboard heatmaps
- sponsor/event boundaries

### 2. `SpatialCell`

**Definition**: The canonical backend map unit. A deterministic S2 cell that contains a player or coin location.

**Used for**:

- hunt pressure
- density calculations
- AI spawn decisions
- stale coin recycling
- territory rollups
- retention targeting

### 3. `NamedZone`

**Definition**: A human-meaningful gameplay or business region layered on top of the S2 grid.

Examples:

- sponsor zone
- timed hunt zone
- outlaw territory
- banned safety zone
- city launch zone

**Used for**:

- admin workflows
- sponsor placement
- event logic
- safety rules
- narrative territories

### 4. `ZoneMembership`

**Definition**: The relationship between an entity and a geographic unit.

Examples:

- a player is in `SpatialCell L17 abc123`
- a player is also inside `NamedZone "Downtown Launch District"`
- a coin belongs to `SpatialCell L17 xyz456`
- a territory is a collection of S2 cells

---

## Recommended Spatial Model

Black Bart's Gold should use a **multi-layer geospatial model**:

### Layer 1: S2 cells are the base world partition

This is the canonical backend layer.

- Every player location is indexed into S2 cells
- Every coin location is indexed into S2 cells
- AI Governor logic operates here first

### Layer 2: Named zones are overlays

This is the admin/game-design layer.

- Sponsor campaigns
- Event hunts
- Safety exclusions
- Territory systems

These overlays do not replace cells. They constrain or bias cell-based behavior.

### Layer 3: Proximity zones remain local client logic

This is the Unity feedback layer.

- Distance to nearest coin
- Collection range
- Haptic feedback bands

This layer should remain independent.

---

## Recommended Initial S2 Hierarchy

This proposal recommends starting with **two active levels** and reserving a third for future exact-placement logic.

### Level A: Macro region cell

**Recommended starting point**: **S2 Level 14**

Why:

- Good for district-scale summaries
- Useful for dashboard rollups
- Good for territory grouping and operational visibility
- Already referenced in `Docs/AI-integration.md`

### Level B: Spawn pressure cell

**Recommended starting point**: **S2 Level 17**

Why:

- Good for neighborhood-scale pressure
- Fine enough to avoid treating an entire district as one blob
- Coarse enough to aggregate real player/coin activity meaningfully
- Already aligned with the S2 direction described in `Docs/AI-integration.md`

### Level C: Optional fine placement cell

**Candidate future level**: **S2 Level 20**

Why:

- Good for fine-grained spawn deduplication
- Good for avoiding multiple nearly identical spawn points
- Useful later if the spawn engine needs tighter placement rules

### Important note

These starting levels should be treated as **initial defaults**, not sacred law.

Before locking them permanently, we should validate them against:

- one dense urban play area
- one suburban area
- one lower-density area

The architecture should support level changes without changing the mental model.

---

## Why S2 Over The Other Options

### Why not player-radius zones?

Because they move constantly and do not form a shared world model.

Good for:

- "spawn a coin near this player"

Bad for:

- global balancing
- territory logic
- heatmaps
- durable analytics

### Why not fixed quadrants?

Because they are arbitrary and too coarse.

Good for:

- quick prototypes

Bad for:

- production geospatial logic
- city-to-city scaling
- clean AI reasoning

### Why not polygons-only?

Because polygons are great overlays but a poor universal partition.

Good for:

- sponsor footprints
- events
- safety rules

Bad for:

- covering the full playable world automatically

### Why not H3?

H3 is a strong spatial system, especially for heatmaps and neighbor logic, but S2 is the better choice for this repo because:

- the project already points toward S2 in `Docs/AI-integration.md`
- Pokémon GO-style architecture is a direct inspiration
- S2 parent-child containment is a very clean fit for multi-scale game logic

---

## What A "Zone" Means After This Proposal

After adopting this proposal, the word **zone** should be used carefully.

### Canonical language

- **SpatialCell** = the backend's base map unit
- **NamedZone** = a human/gameplay overlay area
- **ProximityZone** = a Unity coin-distance band

### Practical wording

Instead of saying:

- "the player spawned into a zone"

Say:

- "the player entered S2 cell `X`"
- "the player is inside named zone `Y`"
- "the player is in `Near` proximity to the nearest coin"

This may sound more technical at first, but it removes ambiguity and makes AI behavior easier to reason about and audit.

---

## Data Model Proposal

The key principle here is **least churn**.

We should not tear out the existing `zones` table. We should redefine its role and add the minimum new fields needed to support S2-based logic.

### Keep: `zones` table

Keep the current `public.zones` table, but treat it as a **named zone overlay table**.

That means:

- sponsor zones stay here
- hunt zones stay here
- future territory zones can live here
- safety/banned zones can live here

This table is no longer the canonical partition of the world.

### Additive fields on `player_locations`

Add fields like:

- `s2_cell_id_l17`
- `s2_cell_id_l14`
- `current_named_zone_id` or keep `current_zone_id` and redefine it as named-zone membership only

Recommended meaning:

- `s2_cell_id_l17` = player's current pressure cell
- `s2_cell_id_l14` = player's current parent summary cell
- `current_zone_id` = optional named zone overlay the player is inside, if any

### Additive fields on `coins`

Add:

- `s2_cell_id_l17`
- `s2_cell_id_l14`

This allows:

- direct cell aggregation without recomputing every query
- easier stale-cell cleanup
- pressure calculations without expensive geometry logic

### Additive fields on `spawn_history`

Add:

- `s2_cell_id_l17`
- `s2_cell_id_l14`

This preserves historical analytics even if:

- a coin moves
- overlay logic changes
- named zone definitions evolve

### Optional future table: `zone_cell_memberships`

If we later want territories or event zones defined as sets of cells, add:

- `zone_id`
- `s2_cell_id`
- `s2_level`

This is useful for:

- territory ownership
- named regions built from cell unions
- precomputed zone coverage

This is **not required** for phase one.

---

## Mapping Rules

### Rule 1: The server computes canonical spatial cells

Do **not** trust the client to define the authoritative cell.

The Unity app should keep sending raw GPS data. The backend should compute:

- `s2_cell_id_l17`
- `s2_cell_id_l14`

This prevents drift and keeps all analytics consistent.

### Rule 2: Every player location write updates spatial cell membership

When a location update arrives:

1. validate coordinates
2. compute S2 Level 17 cell
3. compute its Level 14 parent
4. upsert both cell IDs onto `player_locations`
5. optionally compute named-zone membership

### Rule 3: Every coin write stores spatial cell membership

When a coin is spawned:

1. choose target S2 cell
2. choose a legal spawn point inside or near that cell
3. write the coin location
4. compute and store its S2 cell IDs
5. optionally associate it with a named zone

### Rule 4: Named zones bias or constrain cell logic

Examples:

- sponsor zone: only spawn branded coins inside matching cells
- hunt zone: raise minimum density inside covered cells
- banned zone: suppress spawn entirely
- territory zone: group cells into a weekly competitive region

The base analytics still operate on cells.

---

## AI Governor Behavior Under This Model

This is the key reason for adopting S2.

### The AI Governor should reason about cells, not generic zones

For each active **L17 pressure cell**, the system should compute:

- active player count
- active coin count
- hunt pressure
- tier distribution of players in the cell
- recent spawn count
- recent collection rate
- stale coin count
- overlay modifiers from named zones

### Proposed hunt pressure formula

Initial formula:

`hunt_pressure = active_players / max(active_coins, 1)`

This is simple, explainable, and already aligned with the AI routes in `Docs/AI-INTEGRATION-SPEC.md`.

### Recommended smoothing rule

Because real-world player density is uneven, use **neighbor-aware smoothing** when needed.

Examples:

- if a single L17 cell has very low activity, include immediate neighboring L17 cells when making spawn decisions
- use the parent L14 cell as a sanity-check rollup

This prevents overreacting to tiny cell-boundary effects.

### Spawn decision flow

1. Look at active L17 cells
2. Rank by hunt pressure
3. Filter out banned or disabled overlays
4. Check spend guardrails
5. Match spawn tier to player tier distribution
6. Spawn into the target cell
7. Record cell IDs in coin and spawn history

### Cleanup flow

1. Look for cells with zero active players
2. Detect excess active coins
3. Recycle stale coins
4. Re-seed nearby pressured cells if budget allows

---

## Named Zone Overlay Model

Named zones still matter. They just no longer define the whole world.

### Types of named zones

The existing `zone_type` model is still useful:

- `sponsor`
- `hunt`
- `player`
- `grid`

But the meaning should tighten.

### Recommended semantic shift

- `sponsor` = business or advertiser-defined region
- `hunt` = event region with special rules
- `player` = optional personalized or local-interest overlay, not canonical geography
- `grid` = legacy category that should gradually fade from the language once S2 cells become the true partition

### Future better taxonomy

Long term, it may be cleaner to evolve `zone_type` toward categories like:

- `sponsor`
- `event`
- `territory`
- `safety`
- `personalized`

That change is not required immediately.

---

## Unity Client Implications

This proposal intentionally keeps Unity changes minimal at first.

### What should not change immediately

- `ProximityZone`
- collection range logic
- compass/radar behavior
- nearby coin fetching based on radius

Those systems are working and should stay stable.

### What can be added later

- include the player's current S2 cell in debug tools
- show named zone labels on the map if useful
- optionally request "cell context" from backend for UI

### Important separation

The Unity client should continue to think:

- "Which coins are near me?"
- "Can I collect this coin?"

The backend should think:

- "Which cells are healthy or starved?"
- "Which cells need seeding?"
- "Which overlays modify those cells?"

---

## API Evolution Proposal

The API surface should gradually reflect the new model without breaking working features.

### Short-term rule

Keep existing routes alive, but clarify their semantics.

### Recommended path

#### `GET /api/v1/admin/ai/hunt-pressure`

This route should evolve from **per-zone** pressure to **per-spatial-cell** pressure.

Recommended response shape in the next version:

- `cell_id`
- `cell_level`
- `parent_cell_id`
- `active_player_count`
- `active_coin_count`
- `hunt_pressure`
- `recommended_spawn_tier`
- `named_zone_overlays`

### Backward compatibility

During transition:

- keep old response fields for current dashboard consumers
- add new spatial fields in parallel
- migrate dashboard components after data is stable

### Why this matters

The route name `hunt-pressure` still makes sense. The thing that must change is the meaning of the unit being measured.

---

## Privacy And Safety Considerations

This proposal is powerful because it supports more adaptive spawning, but it also increases geospatial sophistication, so we should stay aligned with the caution already reflected in `Docs/project-vision.md`, `Docs/AI-integration.md`, and `Docs/session-handoff.md`.

### Principle 1: Cells are safer than raw path reasoning

The AI should prefer operating on aggregated cell data instead of raw player trails whenever possible.

### Principle 2: Personalized spawns should use patterns, not exact stalking behavior

For churn prevention or player-tailored hunts:

- use frequent-hunt cells
- avoid precise home/work inference
- prefer public-area overlays where possible

### Principle 3: Named safety zones should override all spawn logic

If a cell intersects a banned or unsafe overlay:

- do not spawn there
- do not route the AI there

---

## Additive Rollout Plan

This is the lowest-risk implementation path.

### Phase 1: Terminology and documentation cleanup

- Adopt the vocabulary in this document
- Treat `ProximityZone`, `SpatialCell`, and `NamedZone` as distinct concepts
- Update future docs and route specs to use this language

### Phase 2: Add S2 cell IDs to location-bearing tables

- add S2 fields to `player_locations`
- add S2 fields to `coins`
- add S2 fields to `spawn_history`
- compute them on server-side writes

### Phase 3: Update hunt pressure logic

- compute pressure per L17 cell
- roll up summaries per L14 cell
- keep response shape backward compatible while dashboard transitions

### Phase 4: Reframe `zones` as overlays

- use `zones` for sponsor, event, safety, and territory logic
- stop treating `zones` as the canonical base geography

### Phase 5: Build named territories on top of S2

- define territories as collections of cells
- add weekly ownership and scoring
- let AI operate at both cell and territory scales

---

## Questions This Proposal Answers

### What is an S2-backed spatial cell in Black Bart's Gold?

It is the canonical backend geographic unit used for player density, coin density, spawn pressure, balancing, and AI decisions.

### What is a named zone?

It is a human-meaningful overlay region such as a sponsor zone, timed hunt, territory, or safety area.

### How do players and coins map into cells?

The backend computes S2 cell IDs from raw lat/lng on write and stores them on the relevant rows.

### How does the AI Governor calculate hunt pressure?

By comparing active player count to active coin count per L17 cell, with optional smoothing from neighboring cells and rollups to parent L14 cells.

### What schema changes do we need with the least churn?

Add S2 cell ID fields to `player_locations`, `coins`, and `spawn_history`, keep the current `zones` table, and redefine it as a named-zone overlay system.

---

## Final Recommendation

Black Bart's Gold should formally adopt this rule:

> **S2 cells are the world's canonical backend geography. Named zones are overlays. Proximity zones are local client feedback.**

That gives us:

- a clean world model for the AI Governor
- a path aligned with Pokémon GO-style architecture
- compatibility with the direction already written in `Docs/AI-integration.md`
- minimal disruption to working Unity gameplay systems
- a strong foundation for sponsor features, outlaw territories, and retention logic

---

## Proposed Next Step

The next implementation document should translate this proposal into a build plan covering:

- exact schema additions
- API contract changes
- how to compute S2 cell IDs in the backend
- how to migrate the AI Governor from zone-based pressure to cell-based pressure
- how to keep the rollout backward compatible

That follow-up should be implementation-focused and additive, using this proposal as the source of truth.
