# 🔁 Session Handoff — Black Bart's Gold

> **For the AI assistant starting a new conversation:**
> This file is the briefing note that gets you up to speed fast.
> Read `brand-guide.md` first (Black Bart = Wild West stagecoach robber, NOT a pirate).
> Then read this file completely before touching any code.
> Update the "Current State" section at the end of every productive session.

---

## 🤠 Who Steven Is (Working Style)

Steven is the founder and sole developer of Black Bart's Gold. He is **not a trained software engineer** — he is a visionary product builder who collaborates with AI to implement his ideas. This matters enormously for how to work with him:

- **He thinks in systems and analogies**, not in syntax. Always lead with the big picture before the code.
- **He asks "why" before "what"** — always explain the reasoning behind a technical decision, not just the decision itself.
- **He wants long, clear explanations.** Short answers feel dismissive. Rich explanations feel like a real collaborator.
- **He asks protective questions** ("Is this going to cost me money?", "Will this break what we already have?"). Take these seriously and answer them honestly before proceeding.
- **He is energetic and enthusiastic.** Match his energy. Use emojis. Keep momentum high. 🤠
- **He values continuity.** He notices when context gets lost between sessions. The existence of this file is proof of that — he asked for it explicitly.
- **He is building AI-First as a philosophy**, not just as a feature. He wants to eventually apply this pattern to all of his apps.
- **He makes additive progress.** He does not want big rewrites or deletions of working code. Every change should build on what exists.

**Do not:**

- Give short answers to architectural or "explain this to me" questions
- Skip the "why" and jump straight to code
- Suggest deleting or replacing large chunks of working code
- Use pirate terminology (doubloons, "ahoy", nautical themes) — Black Bart is a Wild West outlaw

---

## 🗺️ What Has Been Built (Complete Inventory)

### The Admin Dashboard (`admin-dashboard/`)

A Next.js app deployed to Vercel at `https://admin.blackbartsgold.com`.

**Standard admin pages (all working in production):**

- `/` — Dashboard overview with live stats
- `/players` — Live player map
- `/users` — User management
- `/coins` — Coin management with map view
- `/zones` — Zone management
- `/finances` — Transaction history with charts
- `/sponsors` — Sponsor management
- `/security` — Security logs
- `/settings` — App settings

**AI Governor page (newly built, in production):**

- `/ai-governor` — "Black Bart Command Center" — live AI activity dashboard

### The AI Stack (all code written, partial deployment)

**Database migrations (in `admin-dashboard/supabase/migrations/`):**

- `014_ai_schema.sql` — Adds `created_by`, `metadata` to `coins`; creates `ai_actions` table; adds `get_ai_spend_this_hour()` function; Realtime wiring
- `015_repair_auto_distribution.sql` — Repairs missing tables (`zones`, `spawn_queue`, `spawn_history`, `distribution_config`, etc.) for databases where earlier migrations failed
- `016_spawn_governor_cron.sql` — Sets up `pg_cron` jobs to trigger the Spawn Governor every 5 minutes and at midnight (**requires manual placeholder substitution before applying**)

**Admin AI API routes (all deployed to Vercel, all working):**

- `GET /api/v1/admin/ai/hunt-pressure` — Per-zone player/coin pressure scores
- `GET /api/v1/admin/ai/economy-health` — Full economy financial snapshot
- `GET /api/v1/admin/ai/actions` — AI audit log with filtering and pagination
- `POST /api/v1/admin/ai/spawn` — Spawn a coin (AI agents only, full guardrails)
- `POST /api/v1/admin/ai/recycle-stale` — Recycle old coins (AI agents only)
- `POST /api/v1/admin/ai/kill-switch` — Toggle auto-spawning on/off (admin session only)
- `POST /api/v1/admin/ai/trigger-governor` — Manually fire a governor cycle (admin session only)

**MCP Server (`mcp-server/`):**

- `src/game-mcp-server.ts` — 5 tools + 3 resources registered
- `src/index.ts` — Entry point with stdio transport
- `.cursor/mcp.json` — Cursor IDE integration config
- **Status: Code written, not yet tested end-to-end with a live LLM**

**Spawn Governor Edge Function (`admin-dashboard/supabase/functions/spawn-governor/index.ts`):**

- Full 6-step autonomous decision loop (safety check → economy gate → spawn → recycle → log)
- Realtime subscription to coin collection events
- Handles `cron`, `coin_collected`, and `manual` trigger modes
- **Status: Code written, NOT YET DEPLOYED to Supabase**

**AI Auth (`admin-dashboard/src/lib/ai-auth.ts`):**

- `isValidAiApiKey()` — synchronous, for write routes (AI agents only)
- `isAuthorizedRequest()` — async dual-auth, accepts EITHER API key OR admin session cookie (allows human admin UI to call read routes directly)
- `unauthorizedResponse()` / `forbiddenResponse()` — standard error responses

**AI Guardrails (`admin-dashboard/src/lib/ai-guardrails.ts`):**

- `AI_AUTONOMOUS_SPEND_LIMIT_USD = 10.00` — per hour
- `AI_SINGLE_SPAWN_APPROVAL_THRESHOLD_USD = 50.00` — requires human approval above this
- `AI_AGENT_IDS` — typed list of valid agent identifiers
- `AI_ERROR_CODES` — typed list of structured error codes

---

## 🚦 Deployment Status

| Component | Status | Notes |
| --------- | ------ | ----- |
| Admin dashboard | ✅ Live in production | `https://admin.blackbartsgold.com` |
| AI API routes | ✅ Live in production | All 7 routes deployed to Vercel |
| AI Governor page | ✅ Live in production | `/ai-governor` |
| Database migrations 014/015 | ✅ Applied to remote DB | Verified working |
| Migration 016 (pg_cron) | ✅ Applied to remote DB | Cron jobs created successfully on 2026-03-08 |
| Spawn Governor Edge Function | ❌ Not deployed | Run: `supabase functions deploy spawn-governor --no-verify-jwt` |
| MCP Server | ❌ Not live-tested | Code written, needs `npm install` in `mcp-server/` and end-to-end test |
| Supabase secrets for Edge Function | ❌ Not set | Run: `supabase secrets set ADMIN_API_BASE_URL=... AI_AGENT_API_KEY=...` |

---

## 📋 Tactical Next Steps (In Priority Order)

### Step 1 — Deploy the Spawn Governor Edge Function

```bash
cd admin-dashboard
npx supabase functions deploy spawn-governor --no-verify-jwt
npx supabase secrets set ADMIN_API_BASE_URL=https://admin.blackbartsgold.com
npx supabase secrets set AI_AGENT_API_KEY=<generate a strong random key>
```

Then set `AI_AGENT_API_KEY` in Vercel environment variables to the same value.

### Step 2 — Test the "Summon Black Bart" button

Once Step 1 is done, click "Summon Black Bart" in the `/ai-governor` page. You should see a toast showing coins spawned, coins recycled, and cost.

### Step 3 — Test the MCP Server with Cursor

```bash
cd mcp-server
npm install
```

Then in `.cursor/mcp.json`, fill in:

- `ADMIN_API_BASE_URL`: `https://admin.blackbartsgold.com`
- `AI_AGENT_API_KEY`: the key from Step 1

Restart Cursor. In a new chat, you should be able to say "call get_economy_health" and have it execute against the live API.

### Step 4 — Build the next player-facing AI experience

Good candidates (from `AI-integration.md`):

- **Player Churn Prevention** — detect players who haven't played in 3 days, drop a high-value coin near them
- **AI Game Master messages** — Black Bart sends in-app taunts and hints based on player behavior
- **Outlaw Territory Guild Wars** — zones "claimed" by guilds, AI creates cross-guild tension events

---

## 🧠 Shared Vocabulary (Mental Models We Developed Together)

Use this vocabulary in future sessions — Steven knows these terms and responds well to them.

| Term | Meaning |
| ---- | ------- |
| **The Ranch** | The full system — database is the land, dashboard is the ranch house, AI is the Foreman, you are the Owner, guardrails are the fence |
| **The Foreman** | The Spawn Governor AI agent — runs the ranch day-to-day without needing the Owner present |
| **The Fence** | The guardrails — $10/hr cap, kill switch, idempotency keys. What makes irresponsible AI behavior architecturally impossible |
| **Summon Black Bart** | The manual trigger button on the AI Governor page that fires an immediate governor cycle |
| **The Command Center** | The `/ai-governor` page |
| **Action feed** | The real-time log at the bottom of the Command Center showing every AI decision |
| **The heartbeat** | Steven's word for the 15-second auto-refresh polling on the Command Center page |
| **AI-First** | The design philosophy: build the API for the AI agent first, and the human dashboard becomes better as a result |
| **MCP remote control** | The MCP server — the standardized interface that lets an LLM call game functions as tools |
| **Dual-auth** | The pattern where API routes accept EITHER an AI API key OR an admin session cookie — so both AI agents and human admins can use read routes |
| **`meta` block** | The part of every AI API response that tells the AI what to do next (`recommended_action`, `economy_status`, `alerts`) |

---

## 🔧 Key Files Quick Reference

| Purpose | File |
| ------- | ---- |
| Brand voice, character guide | `Docs/brand-guide.md` |
| Project overview + AI architecture | `Docs/project-vision.md` |
| AI integration concept + behaviors | `Docs/AI-integration.md` |
| AI integration technical spec | `Docs/AI-INTEGRATION-SPEC.md` |
| AI design principles skill | `.cursor/skills/ai-first-human-second/SKILL.md` |
| AI auth helper | `admin-dashboard/src/lib/ai-auth.ts` |
| AI guardrails constants | `admin-dashboard/src/lib/ai-guardrails.ts` |
| Command Center page (server) | `admin-dashboard/src/app/(dashboard)/ai-governor/page.tsx` |
| Command Center page (client UI) | `admin-dashboard/src/app/(dashboard)/ai-governor/ai-governor-client.tsx` |
| Spawn Governor Edge Function | `admin-dashboard/supabase/functions/spawn-governor/index.ts` |
| MCP Server tools | `mcp-server/src/game-mcp-server.ts` |
| Kill switch API route | `admin-dashboard/src/app/api/v1/admin/ai/kill-switch/route.ts` |
| Manual trigger API route | `admin-dashboard/src/app/api/v1/admin/ai/trigger-governor/route.ts` |
| Database types (TypeScript) | `admin-dashboard/src/types/database.ts` |
| Sidebar navigation | `admin-dashboard/src/components/layout/dashboard-sidebar.tsx` |

---

## 💬 Context Notes for the AI Assistant

- Steven often says things like "I trust your recommendation" — he means it. Make a clear recommendation and execute it. Don't ask too many clarifying questions for obvious next steps.
- When Steven asks "explain this to me", give the full, rich explanation. He uses these explanations to teach others and to solidify his own understanding.
- When Steven expresses nervousness about something (cost, breaking existing code, losing context), address the fear directly and honestly before moving forward.
- The AI-First architecture pattern is something Steven wants to apply to ALL of his apps, not just Black Bart's Gold. Keep that bigger picture in mind when making architectural decisions.
- Steven tracks the conversation emotionally as much as technically. Phrases like "this whole conversation is amazing" or "I'm nervous to close this tab" signal he's in a high-trust, high-engagement state. Honor that.

---

## 📅 Session Log

> Update this section at the end of each productive session. Keep the most recent entry at the top.

---

### Session: 2026-03-15 — Sprint 1 Kickoff: Governor Trigger Repair

**What we implemented:**

- Started Sprint 1 with the smallest, safest hardening slice
- Fixed the Spawn Governor coin-collected webhook path in:
  - `admin-dashboard/supabase/functions/spawn-governor/index.ts`
- Replaced the stale call to `checkZonePressureImmediate(coinId)` with the correct cell-first helper:
  - `checkCellPressureImmediate(coinId)`
- Cleaned up the dashboard-triggered auto-distribution audit trail by stamping proxied human actions with explicit metadata in:
  - `admin-dashboard/src/app/api/v1/admin/dashboard/auto-distribution/route.ts`
- Threaded that metadata through the recycle audit path in:
  - `admin-dashboard/src/app/api/v1/admin/ai/recycle-stale/route.ts`
- Updated the Command Center action feed to visually distinguish admin-triggered proxy actions in:
  - `admin-dashboard/src/app/(dashboard)/ai-governor/ai-governor-client.tsx`
- Improved manual admin coin provenance in:
  - `admin-dashboard/src/components/dashboard/coin-dialog.tsx`
  - `admin-dashboard/src/app/(dashboard)/coins/coins-client.tsx`
- Manual create/edit/move paths now stamp `metadata.admin_dashboard.*` fields so admin dashboard mutations are easier to trace later
- New manual coin creation now sets:
  - `created_by = 'admin'`
- Completed a concrete MCP gap review and documented it in:
  - `Docs/archive/mcp-gap-review.md`
- Confirmed the backend AI/admin surface is ahead of the MCP server
- Confirmed Sprint 2 should build reusable internal Black Bart runtime helpers first, then expand MCP second
- Began Sprint 2 scaffolding by adding the new Black Bart runtime foundation under:
  - `admin-dashboard/src/lib/black-bart/types.ts`
  - `admin-dashboard/src/lib/black-bart/prompt.ts`
  - `admin-dashboard/src/lib/black-bart/context.ts`
  - `admin-dashboard/src/lib/black-bart/response-parser.ts`
  - `admin-dashboard/src/lib/black-bart/runtime.ts`
- Rewired the live player companion route to use the new shared context/runtime facade in:
  - `admin-dashboard/src/app/api/v1/player/companion/route.ts`
- Preserved existing behavior by keeping the runtime on a scripted fallback path for now
- Added lightweight runtime metadata to companion audit rows:
  - `runtime_source`
  - `system_prompt_version`
  - `situation_summary`
- Expanded the Black Bart runtime context layer with:
  - local hunt pressure summary from the player's current S2 pressure cell
  - recent companion history from `ai_actions`
- Companion audit rows now also capture:
  - `local_pressure`
  - `recent_companion_history_count`
- Added a provider-attempt abstraction and explicit fallback strategy in:
  - `admin-dashboard/src/lib/black-bart/provider.ts`
  - `admin-dashboard/src/lib/black-bart/runtime.ts`
- The runtime now tries the configured model-provider path first, records the provider attempt, and then falls back safely to the scripted companion engine
- Companion audit rows now also capture:
  - `provider_attempt`
  - `fallback_reason`
- Implemented the first real provider execution path:
  - OpenAI chat completions over direct HTTP
  - strict JSON schema response format
  - structured parsing into `CompanionResponsePack`
- The runtime can now return:
  - `source: 'model_provider'` when provider output parses successfully
  - `source: 'scripted_fallback'` when transport / config / parsing fails
- Upgraded the Command Center companion visibility in:
  - `admin-dashboard/src/app/(dashboard)/ai-governor/ai-governor-client.tsx`
- The companion dashboard now shows:
  - model-backed companion action count
  - scripted fallback companion action count
  - runtime status badges on companion action rows
  - provider / fallback detail text in the action feed

**Why this mattered:**

- The edge function had already been migrated to cell-first immediate replacement logic
- The webhook entrypoint was still calling an old, undefined zone-era helper name
- That meant `trigger=coin_collected` could fail before the immediate replacement logic ever ran

**Verification:**

- Searched the Spawn Governor function for remaining `checkZonePressureImmediate` references
- Confirmed no references remain
- Targeted lint for `admin-dashboard/supabase/functions/spawn-governor/index.ts` passed cleanly
- Targeted lint for the dashboard proxy, recycle route, and Command Center client passed cleanly
- Targeted lint for the manual coin dialog and coin map client passed cleanly
- MCP gap review completed by reading the current MCP server and the uncovered queue / timed release / companion routes
- Targeted lint for the new `black-bart` runtime folder and the player companion route passed cleanly
- Targeted lint passed again after adding local pressure and recent companion history to the Black Bart context/runtime path
- Targeted lint passed again after adding the provider-attempt abstraction and fallback metadata path
- Targeted lint passed again after enabling the first structured provider response path
- Targeted lint passed again after the Command Center visibility upgrade

**What is now true:**

- The coin-collected webhook path now uses the same immediate cell-pressure helper as the Realtime subscription path
- Sprint 1 control-surface hardening has officially begun with a low-risk production-safety fix
- Dashboard-triggered `spawn_now` and `recycle_stale` actions now carry explicit `admin_triggered` provenance metadata
- The Command Center can now label those proxied actions as `admin-triggered` instead of making them read like purely autonomous Black Bart behavior
- Manual admin coin creation now records `created_by = 'admin'`
- Manual admin create/edit/move flows now preserve additive provenance in `metadata.admin_dashboard`
- Manual delete was reviewed, but it still does not write a persisted admin audit row yet because this codebase does not currently expose a standardized helper for writing admin activity records from that route
- The current MCP server still exposes only 5 tools and 3 resources
- The backend already supports queueing and timed-release orchestration that MCP does not yet expose
- The player companion route already contains the core context-fetching logic Sprint 2 needs, but only as route-local helpers, not reusable runtime modules
- The player companion route now depends on reusable `black-bart` context/runtime modules instead of owning all of that logic inline
- The new runtime is currently a safe facade over the existing scripted companion engine, which gives the repo a clean insertion point for a real model-backed Black Bart without changing the live response contract yet
- The Black Bart runtime now has richer situational context even before introducing a real model provider:
  - current-cell hunt pressure
  - short-term companion interaction history
- The Black Bart runtime now has an explicit model-provider abstraction:
  - it reads `BLACK_BART_MODEL_PROVIDER`
  - it records whether the provider path was unavailable / unsupported
  - it falls back deliberately instead of implicitly
- The current OpenAI provider adapter is now actually wired end-to-end:
  - prompt envelope
  - HTTP call
  - strict JSON schema request
  - response parsing
  - fallback on failure
- A configured environment can now start returning real model-backed companion responses without changing the public player route contract
- The ranch house can now visibly distinguish Black Bart's model-backed companion actions from scripted fallback actions

**Best next coding step:**

- Decide whether Sprint 1 should also add a lightweight standardized admin activity logging helper for manual delete/update routes
- Continue Sprint 2 by turning the provider scaffold into a real parsed-response path for one provider
- Next: upgrade the Command Center to show provider-attempt / fallback status for companion actions
- Next: refine prompt packaging and add stronger guardrails around provider-generated candidate messages and reply lengths
- After that: consider adding explicit environment/setup docs for `BLACK_BART_MODEL_PROVIDER` and `BLACK_BART_OPENAI_MODEL`

---

### Session: 2026-03-08 — S2 Coin Stamping + Remote Migration Push

**What we implemented:**

- Continued the S2 rollout by updating the manual/player coin creation path:
  - `admin-dashboard/src/app/api/v1/coins/hide/route.ts`
- Coin creation now computes and stores:
  - `s2_cell_token_l17`
  - `s2_cell_token_l14`
- Added logging that includes the canonical `L17` pressure cell token for hidden coins

**Supabase work completed:**

- Successfully ran remote migration push from `admin-dashboard/`
- `supabase db push` applied:
  - `016_spawn_governor_cron.sql`
  - `017_s2_spatial_context.sql`

**Important outcome:**

- The remote database now has the new S2 spatial columns
- The remote database also now has the Spawn Governor cron migrations applied
- Both player location writes and coin hide writes can stamp canonical S2 cells

**Verification:**

- Targeted lint for `admin-dashboard/src/app/api/v1/coins/hide/route.ts` passed
- The changed S2 files remain lint-clean
- Full repo lint still contains unrelated historical issues outside this slice

**What is now true:**

- New player location writes can store S2 `L17` + `L14`
- New manually hidden/player-hidden coins can store S2 `L17` + `L14`
- The schema needed for cell-based hunt pressure is now present on the remote DB

**Best next coding step:**

- Build the backfill script for legacy rows
- Then migrate `GET /api/v1/admin/ai/hunt-pressure` from zone-based aggregation to cell-based aggregation

---

### Session: 2026-03-08 — S2 Phase 1-3 Implementation

**What we implemented:**

- Began the actual S2 migration work for the admin backend
- Installed the backend S2 dependency in `admin-dashboard/package.json`:
  - `s2js`
- Added shared spatial helper:
  - `admin-dashboard/src/lib/geo/s2.ts`
- Added named-zone overlay helper:
  - `admin-dashboard/src/lib/geo/named-zone-membership.ts`
- Added additive migration:
  - `admin-dashboard/supabase/migrations/017_s2_spatial_context.sql`

**Schema additions introduced by the migration:**

- `player_locations.s2_cell_token_l17`
- `player_locations.s2_cell_token_l14`
- `coins.s2_cell_token_l17`
- `coins.s2_cell_token_l14`
- `spawn_history.s2_cell_token_l17`
- `spawn_history.s2_cell_token_l14`

**Route changes completed:**

- Updated `admin-dashboard/src/app/api/v1/player/location/route.ts`
  - computes canonical S2 spatial context from incoming lat/lng
  - stamps `L17` and `L14` cell tokens on every location upsert
  - returns `spatialCellL17` and `spatialCellL14` in the response

**Type updates completed:**

- Updated `admin-dashboard/src/types/database.ts`
  - `Coin` now includes S2 spatial token fields
  - `PlayerLocation` now includes S2 spatial token fields
  - `current_zone_id` comments now reflect named-zone overlay semantics rather than canonical geography

**Verification:**

- Targeted lints for changed files passed:
  - `src/app/api/v1/player/location/route.ts`
  - `src/lib/geo/s2.ts`
  - `src/lib/geo/named-zone-membership.ts`
  - `src/types/database.ts`
- Full repo lint still has many unrelated pre-existing dashboard/script issues; they were not introduced by this S2 slice

**What is now true:**

- The backend has its first real canonical spatial write path
- New player location writes can carry S2 `L17` + `L14` truth
- The repo now has the helper layer needed for coin stamping, backfills, and cell-based hunt pressure

**Best next coding step:**

- Implement the next S2 slice:
  - stamp coin creation paths (`/api/v1/coins/hide`)
  - add backfill script for legacy rows
  - then migrate hunt pressure to use cell-based aggregation

---

### Session: 2026-03-08 — Zone Implementation Plan

**What we did:**

- Turned the S2 zone architecture decision into a concrete implementation sequence
- Wrote `Docs/archive/zone-implementation-plan.md` as the build guide for the spatial migration
- Chose **S2 cell tokens stored as text** as the canonical backend spatial identifier format
- Recommended `s2js` as the initial backend TypeScript S2 library

**Key implementation decisions:**

- Keep `public.zones` as a **named-zone overlay** system
- Keep Unity `ProximityZone` untouched as local gameplay feedback
- Add S2 fields to:
  - `player_locations`
  - `coins`
  - `spawn_history`
- Make the backend compute canonical spatial context on writes
- Migrate AI hunt pressure from `current_zone_id` / `spawn_history.zone_id` to **cell-based aggregation**

**Most important architectural pivot:**

- The AI Governor should stop depending on `spawn_coin(zone_id, ...)` as its primary world primitive
- Add a new **cell-first** spawn path using explicit lat/lng:
  - recommended DB function: `spawn_coin_at_location(...)`
- Keep legacy zone-based spawning alive for backward compatibility and overlay-driven flows

**Recommended build order:**

1. `017_s2_spatial_context.sql`
2. `admin-dashboard/src/lib/geo/s2.ts`
3. Update `POST /api/v1/player/location`
4. Update `POST /api/v1/coins/hide`
5. Backfill players + coins
6. Add `spawn_coin_at_location(...)`
7. Update AI spawn route
8. Update `GET /api/v1/admin/ai/hunt-pressure`
9. Update Spawn Governor caller and dashboard consumers

**Best next coding step:**

- Implement Phase 1 + Phase 2 + Phase 3 together:
  - create `017_s2_spatial_context.sql`
  - add shared S2 helper module
  - stamp player location writes with `L17` and `L14` tokens

---

### Session: 2026-03-08 — Zone Architecture Proposal

**What we did:**

- Researched how to define world geography for the AI Governor before implementing new backend zone logic
- Confirmed the repo was using "zone" to mean multiple different things:
  - Unity `ProximityZone` = coin-distance feedback bands
  - backend/admin `zones` = geographic spawn and analytics areas
  - design language = "player in a zone" without canonical runtime assignment
- Reviewed Pokémon GO-style spatial partitioning patterns and aligned on **S2 cells** as the correct canonical backend geography
- Wrote `Docs/archive/zone-architecture-proposal.md` as the new source-of-truth design proposal

**Key decision:**

- **S2 cells are the canonical backend world partition**
- The existing `zones` table should be treated as a **named zone overlay system**
- Unity `ProximityZone` remains a **local gameplay concept**, separate from backend geography

**What the proposal defines:**

- Shared vocabulary: `SpatialCell`, `NamedZone`, `ProximityZone`
- Recommended initial S2 hierarchy:
  - `L14` for macro summaries and territory rollups
  - `L17` for neighborhood-scale hunt pressure and spawn balancing
  - optional `L20` later for fine spawn placement rules
- Additive schema path:
  - add S2 cell IDs to `player_locations`
  - add S2 cell IDs to `coins`
  - add S2 cell IDs to `spawn_history`
- AI Governor path:
  - compute hunt pressure per S2 cell instead of generic zone rows
  - use named zones only as overlays, constraints, or modifiers

**Why this matters:**

- Gives the AI Governor a stable map grammar
- Matches the AI-first direction already documented in `Docs/project-vision.md`
- Aligns with the S2 direction already present in `Docs/AI-integration.md`
- Avoids confusing Unity proximity logic with backend world partitioning

**What's next:**

- Write the implementation follow-up for S2 adoption:
  - exact schema additions
  - backend S2 computation strategy
  - hunt-pressure route evolution
  - backward-compatible rollout plan

---

### Session: 2026-03-07 — AI Governor Command Center

**What we did:**

- Built the full "Black Bart Command Center" UI at `/ai-governor`
  - 5 KPI cards (economy, spend, actions, coins, kill switch toggle)
  - Hunt pressure zone grid with hot/warm/cool color coding
  - Economy health panel with supply/demand ratio, margins, coin counts
  - Real-time action feed showing every AI decision
  - "Summon Black Bart" manual trigger button
  - Auto-refreshes every 15 seconds (stops when tab closes)
- Added dual-auth to read routes (accepts API key OR admin session)
- Created `kill-switch` and `trigger-governor` API routes
- Added "Black Bart AI" nav item to sidebar
- Excluded Supabase Edge Functions from Next.js TypeScript check (fixes recurring Vercel build failures)
- Fixed a Vercel production build failure caused by the Deno type error
- Confirmed production deployment at `https://admin.blackbartsgold.com/ai-governor`

**Steven's big insights this session:**

- Understood deeply why AI-First makes any app more alive
- Articulated the vision to apply this pattern across all his apps
- Recognized that the "Command Center" pattern is the human-oversight layer that makes autonomous AI trustworthy

**What's next:**

- Deploy the Spawn Governor Edge Function (Step 1 in tactical next steps above)
- Apply migration 016 (pg_cron jobs)
- Test "Summon Black Bart" with the live Edge Function
- Begin planning next AI behavior (player churn prevention or Game Master taunts)

---

*Last updated: 2026-03-08*
*Update this file at the end of every productive session to keep context fresh.*
