# 🤖 Black Bart's Gold — AI Integration Plan

> *"The game doesn't just watch you play. It plays back."* 🤠

**Document Status**: Living Document — keep building on this  
**Created**: March 2, 2026  
**Purpose**: Capture the full AI Game Master vision and integration plan for Black Bart's Gold

---

## 🧭 The Core Vision

Most location-based games (Pokémon GO included) place content **statically on a server**. A spawn point exists at a GPS coordinate, the server picks from a weighted random table, and that's it. The world is not intelligent. It does not watch you. It does not react.

**Black Bart's Gold will be different.**

The goal is to build an AI layer that:
- Watches live player behavior in real time
- Makes autonomous decisions about the game world (spawn coins, fire events, send messages)
- Responds to real-world signals (weather, time of day, local events)
- Manages the coin economy to keep it healthy
- Embodies Black Bart as an active, reactive character

This turns BBG from a **static treasure hunt** into a **living world with a game master running it 24/7** — without any human intervention required.

---

## 🏗️ Architecture Overview

The system is built across four connected layers:

```
┌──────────────────────────────────────────────────────────────┐
│                     AI AGENT LAYER                            │
│   Claude / LLM — runs on schedule + reacts to Realtime events │
│   (Spawn Governor, Churn Agent, Economy Balancer, Game Master) │
└──────────────────────────┬───────────────────────────────────┘
                           │  calls MCP tools
┌──────────────────────────▼───────────────────────────────────┐
│                    MCP SERVER LAYER                           │
│   TypeScript — built with @modelcontextprotocol/sdk           │
│   spawn_coin | get_density | trigger_event | get_player_stats  │
│   expire_coins | get_churn_risk | send_notification | ...      │
└──────────────────────────┬───────────────────────────────────┘
                           │  reads/writes via REST + Realtime
┌──────────────────────────▼───────────────────────────────────┐
│                 SUPABASE / ADMIN API LAYER                    │
│   PostgreSQL + Realtime + Edge Functions                      │
│   • Postgres Changes push events to AI agents in real-time    │
│   • Edge Functions run scheduled AI inference server-side     │
│   • pgvector stores player behavior embeddings                │
└──────────────────────────┬───────────────────────────────────┘
                           │  Realtime subscription
┌──────────────────────────▼───────────────────────────────────┐
│                  UNITY MOBILE GAME (C#)                       │
│   • Receives newly spawned coins via Supabase Realtime        │
│   • Reports collections → triggers AI reactions               │
│   • GPS heatmap data feeds the spawn algorithm                │
└──────────────────────────────────────────────────────────────┘
```

### MCP Server File Structure

```
black-barts-gold-mcp/
├── src/
│   ├── server.ts              # Transport Layer (STDIO / HTTP / SSE)
│   ├── game-mcp-server.ts     # Protocol Layer (MCP tool registration + Zod validation)
│   └── game-service.ts        # Business Layer (Supabase queries, coin logic, events)
├── package.json
└── tsconfig.json
```

> **Why 3 layers?** A failure in the business layer must never silently crash the protocol layer — otherwise the AI agent confidently reports "no coins found" when the server actually crashed. Layering = blast shielding.

---

## 🧰 MCP Tool Registry (What the AI Can Do)

These are the tools the MCP server will expose. Each one maps to an admin API endpoint.

| Tool Name | Action | Priority |
|-----------|--------|----------|
| `spawn_coin` | Spawn a coin at a GPS location with value + tier | Phase 1 |
| `spawn_coin_batch` | Spawn multiple coins at once (for events) | Phase 1 |
| `expire_coin` | Remove a specific coin from the world | Phase 1 |
| `expire_stale_coins` | Bulk-expire coins uncollected past a threshold | Phase 1 |
| `get_coin_density` | Get coin count per S2 cell / region | Phase 1 |
| `get_active_players` | Get players active in the last N minutes | Phase 1 |
| `get_player_stats` | Get a specific player's tier, history, GPS cluster | Phase 2 |
| `get_churn_risk_players` | Return players flagged as churn risk | Phase 2 |
| `trigger_event` | Fire a named game event (flash hunt, raid, etc.) | Phase 2 |
| `send_notification` | Send a push notification with Black Bart's voice | Phase 2 |
| `get_economy_health` | Return supply/demand ratio, margin health metrics | Phase 2 |
| `get_player_density_map` | Return a heatmap of where players are active | Phase 2 |
| `create_territory_zone` | Create a named Outlaw Territory for guild competition | Phase 3 |
| `classify_player` | Return Explorer/Achiever/Competitive/Social classification | Phase 3 |

---

## 🖥️ Building the Admin Dashboard for AI

### Design Principle: Build for Agents, Not Just Humans

The admin dashboard API must be designed so an AI agent can consume and mutate game state reliably. This means:

### 1. Action-Verb Endpoint Naming

Every REST endpoint maps directly to an MCP tool name:

```
GET  /admin/coins/density          →  get_coin_density
POST /admin/coins/spawn            →  spawn_coin
POST /admin/coins/spawn-batch      →  spawn_coin_batch
POST /admin/coins/expire-batch     →  expire_stale_coins
GET  /admin/players/active         →  get_active_players
POST /admin/events/trigger         →  trigger_event
GET  /admin/analytics/churn-risk   →  get_churn_risk_players
GET  /admin/analytics/economy      →  get_economy_health
```

### 2. Self-Describing Responses With Recommended Actions

API responses include a `meta` block the AI can use to make decisions without reasoning from scratch:

```json
{
  "data": {
    "active_coins": 47,
    "active_players": 203
  },
  "meta": {
    "hunt_pressure": 4.3,
    "low_density_zones": ["downtown_east", "waterfront"],
    "recommended_action": "spawn_coins_in_low_density_zones",
    "churn_risk_count": 12
  },
  "_links": {
    "spawn": "/admin/coins/spawn",
    "density_map": "/admin/players/density"
  }
}
```

### 3. OpenAPI Spec as the AI Contract

Every endpoint gets a full OpenAPI spec with `operationId` matching the MCP tool name. This lets tools like Zuplo auto-generate MCP tools from the spec. The OpenAPI spec is the Rosetta Stone between your REST API and the AI agent.

### 4. Supabase Schema Design — AI-First

- Add `metadata jsonb` columns to all major tables (coins, players, events)
- Use `created_by` fields consistently: `"system"` | `"admin"` | `"ai_agent"` — so you can always see what the AI changed
- Use `pgvector` for player behavior embeddings (personalized spawning in Phase 3)
- Enable `postgres_changes` Realtime on `coins`, `players`, and `events` tables

### 5. Supabase Realtime as the AI's Nervous System

Instead of polling, the AI agent reacts to push events:

```typescript
// AI agent wakes up whenever a coin is collected
const subscription = supabase
  .channel('coin-events')
  .on('postgres_changes',
    { event: 'UPDATE', schema: 'public', table: 'coins', filter: 'status=eq.collected' },
    (payload) => {
      aiAgent.onCoinCollected(payload.new); // decide if a replacement should spawn
    }
  )
  .subscribe();
```

---

## 🤖 The Six AI Behaviors

### Behavior 1: Dynamic Spawn Governor ⚡ (Build First)

**What it does**: Runs every 5 minutes. Checks player density vs coin availability across all active zones. Spawns coins in under-hunted areas. Expires stale coins in zones with no players.

**Why it matters**: Solves the #1 retention killer in location-based games — players walking around finding nothing.

**Logic loop**:
```
Every 5 minutes:
1. Query player GPS clusters (active in last 30min)
2. Query uncollected coin density per S2 cell
3. Calculate "hunt pressure" = active players / available coins per zone
4. If pressure > threshold AND coins < minimum: spawn coins
5. If coins uncollected for >6hrs with no players nearby: expire them
6. Match coin tiers to the find-limit tiers of players present in each zone
```

**BBG-specific rule**: Always match coin tiers to the players present. A Cabin Boy area gets bronze coins. A King of Pirates zone gets high-value coins. No one should constantly see locked red coins.

---

### Behavior 2: Real-World Signal Injection 🌤️

**What it does**: Injects live real-world data into what coins spawn, making the game world feel alive and responsive to reality.

| Signal | Source | BBG Behavior |
|--------|--------|--------------|
| **Rainy weather** | OpenWeatherMap API | "Sunken treasure" spawns near rivers, lakes, and waterfront |
| **Sunny/clear** | OpenWeatherMap API | "Desert gold" spawns in open parks and plazas |
| **Sunset / Golden hour** | Time-of-day calculation | "Outlaw's last ride" — high-value coin clusters appear briefly |
| **Friday/Saturday night** | Calendar | "Pirate Raid Night" — rare coin clusters in entertainment districts |
| **Local concerts/events** | Eventbrite API | Stadium area gold rush spawns when large events are nearby |
| **Full moon** | Astronomy API | **Black Bart's Legendary Cache** — ultra-rare, once-a-month spawn |
| **Holidays** | Calendar | Themed events (e.g. Halloween "Ghost Gold", New Year "Treasury Vault") |

**Example player experience**: Players open the app on a rainy Thursday evening and receive: *"The rain's driven honest folk indoors. More for us, partner. The waterfront's rich tonight. 🤠"* — and there ARE actually bonus coins at the waterfront. Not random. Intelligent and themed.

---

### Behavior 3: AI Game Master (Black Bart as a Character) 🏴‍☠️

**What it does**: Claude runs as a scheduled cloud function in character as Black Bart. It reads live game state, makes narrative decisions, and mutates the world — all in Black Bart's voice.

**Prompt structure** (runs via Supabase Edge Function on a schedule):

```
SYSTEM: You are Black Bart, the gentleman outlaw and Game Master of Black Bart's Gold.
You manage a live treasure hunt game. You may spawn coins, fire events, or send
lore notifications. Always stay in character — witty, charming, 19th century outlaw.

Current game state:
- Active players: {active_players}
- Coins collected in last hour: {coins_collected_last_hour}
- Low-density zones: {low_density_zones}
- High-activity zones: {high_activity_zones}
- Weather in top cities: {weather_data}
- Players at churn risk: {churn_risk_count}

You may call: spawn_coin_batch | trigger_event | send_notification | create_territory_zone

Make ONE decision. Explain your reasoning briefly, then issue the tool call.
```

**Example AI response**:
> *"Chicago's waterfront has 3 hunters and no coins — a waste of fine weather. I'll leave them something worth finding. Notifying all Chicago players."*
> → Calls `spawn_coin_batch` with 4 coins on the waterfront
> → Calls `send_notification`: *"The law's hiding from the rain. While they're tucked in, I've left something for you at the river. Move quick, partner. 🤠"*

Players have no idea if a human or an AI did that. **That ambiguity is the magic.**

---

### Behavior 4: Churn Prevention Agent 💔

**What it does**: Monitors behavioral signals and fires personalized retention interventions before players go dark permanently.

**Risk Classification**:

| Days Since Last Login | Risk Level | AI Action |
|-----------------------|------------|-----------|
| 3 days | Low | Send "treasure spotted near you" push notification |
| 7 days | Medium | Spawn a coin near their most-frequented hunt area |
| 14 days | High | "Black Bart's Challenge" — personal coin + lore narrative |
| 30 days | Critical | "You've been forgotten by the gang" final message + bonus gas day |

**Key differentiator**: The AI spawns the coin at a location the player **actually hunts near** (based on their GPS history), not at a random location. A generic push notification is forgettable. A coin that appears at your favorite park with a personalized Black Bart message is a story you tell friends.

---

### Behavior 5: Outlaw Territory — AI-Driven Guild Wars 🗺️

**What it does**: Creates weekly dynamic territorial zones using S2 spatial cells (the same grid system Pokémon GO uses). AI watches competition and dynamically adjusts to prevent any single guild from dominating.

**System flow**:
1. Every Monday, AI identifies 10 "hot zones" based on prior week's player activity
2. Zones get named: *"Black Bart's Stronghold"*, *"Devil's Gulch"*, *"The Deadwood Claim"*
3. Special high-value coins spawn ONLY in these zones for the week
4. Guilds compete to collect the most coins in each zone to "claim" it
5. AI monitors competition in real time — if one guild dominates every zone, it spawns a **"rival gang" counter-event** to force competition back into balance
6. End-of-week: standings published, territories reset, new zones generated

**Why this is powerful**: Weekly, AI-generated competitive content — no human designers required. The game creates its own meta-game.

---

### Behavior 6: Economy Balancer 💰

**What it does**: Continuously monitors the health of the coin economy and makes micro-adjustments to maintain the financial balance. Critical because BBG coins have **real dollar value**.

**Health Metrics Monitored**:

```
├── Coins spawned vs coins collected ratio (supply/demand)
├── Average time-to-collection per zone (demand signal)
├── Gas fee revenue vs coin value distributed (margin health)
├── Player tier distribution (is progression healthy?)
└── Coin value inflation/deflation over time
```

**AI Interventions**:

| Condition | AI Action |
|-----------|-----------|
| Supply >> Demand (coins sitting uncollected) | Reduce spawn rate, increase individual coin values |
| Demand >> Supply (players finding nothing) | Increase spawn frequency, trigger flash hunt event |
| New player area detected | Seed with starter bronze coins to grow the player base |
| Economy inflating | Introduce time-limited "boss coins" as value sinks |
| Specific tier overpowered | Add tier-specific challenges with coin cost |

> ⚠️ **Hard guardrail**: The Economy Balancer AI must have a hard-coded maximum autonomous spend limit (e.g., AI can never spawn coins totaling more than $X/hour without human approval queued in the dashboard). Real money requires human safeguards.

---

## 🛠️ S2 Spatial Grid (The Geospatial Engine)

Black Bart's Gold should use **Google's S2 Geometry library** for spatial indexing — the same system Pokémon GO uses to partition the world.

```
S2 Hierarchy:
L14 Cells → Large city regions → Set rules for overall coin density
    └── L17 Cells → Neighborhood subdivisions → Control actual spawn rate per area
```

- Higher player activity in an L17 cell = AI spawns more content there
- Territories (Behavior 5) are defined as named collections of L17 cells
- Player density heatmap is computed per L17 cell

**JavaScript/TypeScript library**: `s2-geometry` on npm — direct port of Google's S2 library.

---

## 📊 Player Classification System

The AI classifies players into Bartle archetypes to personalize the experience:

| Type | Behavior Signal | AI Response |
|------|-----------------|-------------|
| 🗺️ **Explorer** | Hunts in many different locations | Spawn rare coins in undiscovered areas to reward exploration |
| 🏆 **Achiever** | Grinding for tier upgrades and leaderboard position | Trigger limited-time high-value coin events when they're close to a tier |
| ⚔️ **Competitive** | Leaderboard-focused, active in guild wars | Spawn bounty events + rival gang challenges |
| 👥 **Social** | Guild-active, shares finds, invites friends | Spawn guild-exclusive hunt zones and co-op challenges |

Classification is computed from player behavior data using Supabase + pgvector embeddings in Phase 3.

---

## 🚦 Implementation Phases

### Phase 1 — Foundation (Build With MVP)

- [ ] Design admin API endpoints with AI-friendly naming from day one
- [ ] Write OpenAPI spec for all admin endpoints with `operationId` matching MCP tool names
- [ ] Add `metadata jsonb` and `created_by` columns to Supabase schema
- [ ] Enable Supabase Realtime on `coins`, `players`, `events` tables
- [ ] Build minimal MCP server with 5 core tools: `list_coins`, `spawn_coin`, `expire_coin`, `get_player_stats`, `get_coin_density`
- [ ] Implement S2 cell spatial indexing on coin and player tables

### Phase 2 — First AI Feature: Spawn Governor

- [ ] Build Dynamic Spawn Governor as a Supabase Edge Function (runs every 5 min)
- [ ] Implement hunt pressure calculation (players / coins per S2 cell)
- [ ] Add tier-matching logic (Cabin Boy areas get bronze, etc.)
- [ ] Build coin expiry logic for stale, uncollected coins
- [ ] Dashboard UI: live map showing AI spawn decisions in real time

### Phase 3 — Real-World Signals + Churn Agent

- [ ] Integrate OpenWeatherMap API for weather-based spawn modifiers
- [ ] Integrate time-of-day and calendar signals
- [ ] Build churn risk scoring on player table (last_login, session_count, gas_remaining)
- [ ] Build churn prevention agent — spawns personal coins for at-risk players
- [ ] Extend MCP server with `send_notification`, `get_churn_risk_players` tools

### Phase 4 — AI Game Master (Black Bart)

- [ ] Build scheduled Edge Function that calls Claude API with live game state context
- [ ] Write Black Bart system prompt (in character, with tool access)
- [ ] Add `created_by = "ai_game_master"` tracking to all AI-initiated actions
- [ ] Build admin dashboard view: "What did Black Bart do today?" log
- [ ] Add human approval queue for AI actions above a cost threshold

### Phase 5 — Outlaw Territories + Economy Balancer

- [ ] Build territory zone system on top of S2 cells
- [ ] Implement guild claim/competition mechanics
- [ ] Build Economy Balancer monitor (supply/demand ratios, margin health)
- [ ] Add hard-coded autonomous spend limits with human approval queue
- [ ] Phase 3 player classification: Explorer/Achiever/Competitive/Social using pgvector

---

## 🔐 Guardrails & Safety

Because BBG coins have real dollar value, the AI must operate within firm constraints:

| Guardrail | Rule |
|-----------|------|
| **Spend limit** | AI cannot autonomously spawn coins totaling > $X/hour without queuing human approval |
| **Audit trail** | Every AI action logged with `created_by`, timestamp, reasoning, and tool parameters |
| **Kill switch** | Single admin toggle to pause all AI agent activity instantly |
| **Rollback** | All AI-spawned coins can be bulk-expired by an admin in one click |
| **Rate limiting** | AI agents limited to N tool calls per minute (Redis sliding window) |
| **Human approval queue** | High-cost actions (>$50 in coin value) require admin confirmation before executing |

---

## ❓ Open Questions (To Decide)

1. **How active is Black Bart?** — Is he mostly background infrastructure (invisible), or is he a named character who sends messages players know about? The "AI Game Master who writes in character" concept only works if players are in on it (or delightfully not in on it).

2. **Autonomous financial authority** — What is the maximum dollar value the AI can spawn per hour without human sign-off? This number shapes the whole architecture.

3. **Player privacy + GPS history** — Personalized spawning (churn agent, player classification) requires storing location history. Need to plan a privacy policy and data retention rules early.

4. **Eventbrite / local events API** — Worth integrating for real-world signal injection? Adds complexity but makes the "living world" feel dramatically more real.

5. **How do territories interact with the find-limit system?** — Can a Cabin Boy compete in a King of Pirates territory? Or are territories tier-gated?

---

## 📚 Key Resources & References

| Resource | URL | What It's For |
|----------|-----|---------------|
| MCP TypeScript SDK | `@modelcontextprotocol/sdk` (npm) | Building the MCP server |
| IvanMurzak/Unity-MCP | github.com/IvanMurzak/Unity-MCP | Unity + MCP reference (1,100+ stars) |
| S2 Geometry (JS) | `s2-geometry` (npm) | Pokémon GO-style spatial grid |
| Supabase Realtime Docs | supabase.com/docs/guides/realtime | Push-based AI reactions |
| OpenWeatherMap API | openweathermap.org/api | Weather signal injection (free tier available) |
| Letta + Supabase | docs.letta.com/tutorials/integrations/supabase | Full AI agent + Supabase integration |
| Entropia CelestAI | Reference for economy-managing AI in a live game with real money | Conceptual reference |

---

## 💡 The One-Line Pitch

> **Pokémon GO placed coins statically on a server. Black Bart's Gold has an AI playing the game alongside you — watching, reacting, and making the world feel alive.**

---

*"X marks the spot — and the spot is wherever Black Bart decides it is tonight."* 🗺️🤠
