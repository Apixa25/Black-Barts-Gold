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
|-----------|--------|-------|
| Admin dashboard | ✅ Live in production | `https://admin.blackbartsgold.com` |
| AI API routes | ✅ Live in production | All 7 routes deployed to Vercel |
| AI Governor page | ✅ Live in production | `/ai-governor` |
| Database migrations 014/015 | ✅ Applied to remote DB | Verified working |
| Migration 016 (pg_cron) | ❌ Not applied | Must substitute `YOUR_PROJECT_REF` and `YOUR_SERVICE_ROLE_KEY` placeholders first |
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

### Step 2 — Apply Migration 016 (pg_cron jobs)
Open `admin-dashboard/supabase/migrations/016_spawn_governor_cron.sql`, replace:
- `YOUR_PROJECT_REF` → your Supabase project ref (from the Supabase dashboard URL)
- `YOUR_SERVICE_ROLE_KEY` → from Supabase dashboard → Settings → API

Then run:
```bash
npx supabase db push
```

### Step 3 — Test the "Summon Black Bart" button
Once Steps 1-2 are done, click "Summon Black Bart" in the `/ai-governor` page. You should see a toast showing coins spawned, coins recycled, and cost.

### Step 4 — Test the MCP Server with Cursor
```bash
cd mcp-server
npm install
```
Then in `.cursor/mcp.json`, fill in:
- `ADMIN_API_BASE_URL`: `https://admin.blackbartsgold.com`
- `AI_AGENT_API_KEY`: the key from Step 1

Restart Cursor. In a new chat, you should be able to say "call get_economy_health" and have it execute against the live API.

### Step 5 — Build the next player-facing AI experience
Good candidates (from `AI-integration.md`):
- **Player Churn Prevention** — detect players who haven't played in 3 days, drop a high-value coin near them
- **AI Game Master messages** — Black Bart sends in-app taunts and hints based on player behavior
- **Outlaw Territory Guild Wars** — zones "claimed" by guilds, AI creates cross-guild tension events

---

## 🧠 Shared Vocabulary (Mental Models We Developed Together)

Use this vocabulary in future sessions — Steven knows these terms and responds well to them.

| Term | Meaning |
|------|---------|
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
|---------|------|
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

*Last updated: 2026-03-07*
*Update this file at the end of every productive session to keep context fresh.*
