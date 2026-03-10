# 🤠 Black Bart's Gold - Project Vision

## Executive Summary

Black Bart's Gold is an augmented reality mobile game where players hunt for virtual gold coins in the real world. Players use their phone's camera to see and collect 3D coins placed in their physical environment, with the collected coins having real cryptocurrency value.

**Built with Unity + AR Foundation** for production-quality AR that can scale to millions of users.

---

## 🎯 The Vision

### The Hook
"What if you could walk around your neighborhood and find real money just lying on the ground?"

### The Experience
1. **Open the app** → See a pirate-themed interface
2. **Start hunting** → AR camera activates with HUD overlay
3. **Explore** → Walk around your real environment
4. **Discover** → Virtual gold doubloons appear through your camera
5. **Collect** → Get within range, center crosshairs, tap to collect
6. **Celebrate** → Coin flies to screen, Black Bart congratulates you
7. **Profit** → Real BBG (Black Bart's Gold) added to your wallet

### What Makes It Special
- **Real Value**: Coins convert to Bitcoin, not just points
- **Physical Activity**: Must walk to find treasure
- **Social Competition**: Leaderboards, friends, guilds
- **Fair Economy**: Hide coins to unlock finding bigger ones

---

## 🤖 AI-First Architecture (The Big Idea)

### What "AI-First" Means

Most apps are built for one user: a human with a screen. This project is built for **two users simultaneously**:

1. 🧑 **The human admin** — opens a browser, reads cards, clicks buttons
2. 🤖 **The AI agent (Black Bart)** — makes HTTP calls, reads JSON, takes actions autonomously

The philosophy: **design the app for the AI, and it becomes better for humans too.** The AI forces clean APIs, structured data, and clear logic — which also makes the human dashboard faster and more reliable.

### The Ranch Mental Model

Think of the full system as a working ranch:

| Layer | Ranch Analogy | Tech Equivalent |
|-------|--------------|-----------------|
| **The Land** | Terrain, water, grass | Supabase database |
| **The Ranch House** | Operations hub, records | Admin dashboard (Next.js) |
| **The Foreman** | Makes daily decisions autonomously | Spawn Governor (Edge Function) |
| **The Owner** | Sets rules, can override anything | Human admin (you) |
| **The Fence** | Keeps things from going wrong | Guardrails ($10/hr cap, kill switch) |

The AI Foreman runs the ranch day-to-day. You look at the reports and can step in any time. You don't have to be there for the ranch to run.

### The 5 AI-First Design Rules

Every AI-facing feature in this project follows these five rules:

1. **Action-verb endpoints** — Routes read like decisions (`spawn_coin`, `recycle_stale_coins`, `get_hunt_pressure`), not CRUD forms
2. **`meta` blocks with recommended actions** — Every API response tells the AI what to do next (`meta.recommended_action`, `meta.economy_status`, `meta.alerts`)
3. **`ai_actions` audit log** — Every AI decision is recorded with `agent_id`, `tool_called`, `reasoning`, `cost_usd`, `success`
4. **Hard guardrails in the API layer** — Spend limits, kill switch, idempotency keys, and approval thresholds are enforced server-side. The AI cannot bypass them no matter how its reasoning loop behaves
5. **MCP server as remote control** — A standardized interface (Model Context Protocol) lets any LLM (Claude, GPT, Gemini) call the game's functions as tools. The AI says "call `spawn_coin`" and the MCP server handles auth, validation, and the actual API call

### The 4-Layer Technical Stack (AI path)

```
Claude / GPT / any LLM
        ↓  (calls tool by name)
MCP Server (mcp-server/)          ← tool definitions, input schemas
        ↓  (authenticated HTTP)
Admin AI API Routes                ← guardrails enforced here
  /api/v1/admin/ai/spawn
  /api/v1/admin/ai/hunt-pressure
  /api/v1/admin/ai/economy-health
  /api/v1/admin/ai/recycle-stale
  /api/v1/admin/ai/actions
        ↓  (reads/writes)
Supabase PostgreSQL                ← source of truth
  coins, ai_actions, zones,
  distribution_config, spawn_history
```

### The Black Bart Command Center (Admin UI)

The human-facing window into what the AI is doing:
- **5 KPI cards** — economy status, hourly spend, actions today, active coins, kill switch toggle
- **Hunt pressure grid** — per-zone live pressure scores (players ÷ coins), color coded hot/warm/cool
- **Economy health panel** — supply/demand ratio, net margin, spawn/collect/recycle counts
- **Action feed** — real-time log of every AI decision with agent, tool, reasoning, cost, timestamp
- **"Summon Black Bart" button** — manually triggers a full governor cycle immediately
- **Auto-refreshes every 15 seconds** — dies when the tab closes (zero background cost)

---

## 🛠️ Technology Stack

### Why Unity + AR Foundation?

| Factor | ViroReact (Previous) | Unity + AR Foundation |
|--------|---------------------|----------------------|
| **Stability** | ❌ Crashes with React Native 0.81+ | ✅ Production-proven |
| **Community** | ⚠️ Small, limited support | ✅ Massive ecosystem |
| **Performance** | ⚠️ JavaScript bridge overhead | ✅ Native C++ core |
| **Cross-Platform** | ⚠️ Separate native modules | ✅ Single codebase |
| **Scale** | ❓ Unproven at scale | ✅ Powers Pokémon GO |

### Tech Stack Details

| Layer | Technology | Why |
|-------|------------|-----|
| **Game Engine** | Unity 6 (2024 LTS) | Latest stable, best AR support |
| **AR Framework** | AR Foundation 5.x | Unity's cross-platform AR abstraction |
| **Android AR** | ARCore XR Plugin | Google's AR SDK, native performance |
| **iOS AR** | ARKit XR Plugin | Apple's AR SDK, native performance |
| **Language** | C# | Unity's primary language, robust |
| **Backend** | TBD | Firebase or custom Node.js/Express |
| **Database** | TBD | Firestore or PostgreSQL |

### Platform Support

| Platform | Status | Requirements |
|----------|--------|--------------|
| **Android** | Primary | Android 7.0+, ARCore compatible |
| **iOS** | Secondary | iOS 11.0+, ARKit compatible (A9+) |

---

## 🎮 Core Game Systems

### 1. AR Treasure Hunt
```
Player Position (GPS) ──► AR Scene ──► Coins at relative positions
        │                    │                    │
        ▼                    ▼                    ▼
   Real World         Camera View          3D Gold Doubloons
```

- Coins spawn at real GPS coordinates
- AR converts GPS to 3D positions relative to player
- Coins visible through device camera
- Must physically walk to collect

### 2. Economy System

```
┌─────────────────────────────────────────────────────┐
│                    $10 Purchase                      │
├─────────────────────────────────────────────────────┤
│  $9 → Distributed as coins near player              │
│  $1 → Gas fee (our revenue)                         │
├─────────────────────────────────────────────────────┤
│  Daily: ~$0.33 consumed from gas tank               │
│  No gas = Can't play                                │
└─────────────────────────────────────────────────────┘
```

### 3. Find Limit System

```
Your Find Limit = Highest coin you've ever hidden

┌────────────┬─────────────┬──────────────────┐
│ Hidden     │ Can Find    │ Tier             │
├────────────┼─────────────┼──────────────────┤
│ Nothing    │ Up to $1    │ Cabin Boy        │
│ $5 coin    │ Up to $5    │ Deck Hand        │
│ $25 coin   │ Up to $25   │ Captain          │
│ $100 coin  │ Up to $100  │ King of Pirates  │
└────────────┴─────────────┴──────────────────┘

Coins above your limit appear LOCKED (red, can't collect)
```

---

## 🎨 Visual Design

### Color Palette (Western Treasure Theme)

| Color | Hex | Usage |
|-------|-----|-------|
| **Treasure Gold** | #FFD700 | Primary - coins, buttons, highlights |
| **Saddle Brown** | #8B4513 | Secondary - headers, navigation |
| **Dark Leather** | #3D2914 | Tertiary - text, deep backgrounds |
| **Parchment** | #F5E6D3 | Text backgrounds, cards |
| **Warm Tan** | #D2B48C | Supporting - lighter backgrounds |
| **Fire Orange** | #E25822 | Accent - BB's time powers, excitement |
| **Warning Red** | #8B0000 | Danger - locked items, errors |
| **Brass** | #B87333 | Steampunk - gears, Chrono-Compass |
| **Silver** | #C0C0C0 | Silver tier coins |
| **Bronze** | #CD7F32 | Bronze tier coins |

> See `brand-guide.md` for complete color specifications.

### AR HUD Layout

```
┌─────────────────────────────────────────────────┐
│ [🧭 N]                           [Find: $5.00]  │
│  ↖ 47m                                          │
│                                                 │
│                      ⊕                          │
│                                                 │
│  ┌─────┐                              ║████████║│
│  │ 🗺️  │                              ║░░░░░░░░║│
│  │radar│                              ║  GAS   ║│
│  └─────┘                              ║ 25 days║│
└─────────────────────────────────────────────────┘

🧭 = Compass (direction to selected coin)
⊕ = Crosshairs (target center)
🗺️ = Mini-map/radar (nearby coins)
GAS = Gas meter (days remaining)
```

### Coin Visual States

| State | Appearance |
|-------|------------|
| **Normal** | Gold, spinning, sparkles |
| **Pool** | Silver, shows "?" for value |
| **Locked** | Red tint, lock overlay |
| **In Range** | Crosshairs turn green |
| **Collecting** | Flies to camera, celebration |

---

## 📱 User Flow

### First Launch
```
Install → Onboarding → Create Account → Tutorial Hunt → Main Menu
```

### Daily Play
```
Launch → Auto-login → Main Menu → Start Hunting → AR View → Collect → Wallet
```

### Hunt Flow
```
1. Check gas (block if empty)
2. Start AR camera
3. Get GPS position
4. Load nearby coins
5. Walk around, find coins
6. Center crosshairs, tap
7. Collection animation
8. Wallet updated
9. Continue or exit
```

---

## 🗓️ Development Phases

### Phase 0: Foundation (Current)
- [x] Unity Hub installed
- [x] Unity 6 installed with Android Build Support
- [x] Documentation complete
- [ ] Unity project created
- [ ] AR Foundation installed
- [ ] Basic AR test

### Phase 1: MVP (Sprints 1-8)
- [ ] Scene navigation
- [ ] AR camera with coin rendering
- [ ] GPS tracking
- [ ] Coin collection
- [ ] User auth
- [ ] Wallet & economy
- [ ] Backend integration

### Phase 2: Enhanced Features
- [ ] Multiple hunt types
- [ ] Social features
- [ ] Coin hiding
- [ ] Polish & audio

### Phase 3: Advanced
- [ ] Guilds
- [ ] Sponsor hunts
- [ ] iOS release
- [ ] Scale testing

---

## 📊 Success Metrics

### Technical
- 60 FPS AR rendering
- < 3 second GPS lock
- < 100ms tap response
- 99.9% crash-free sessions

### User Experience
- < 30 seconds to first coin visible
- Clear tutorial completion
- Daily active return rate

---

## 🔄 Migration from React Native

### What We Learned
1. **ViroReact limitations**: Not production-ready for complex apps
2. **Architecture matters**: New arch (Fabric) broke libraries
3. **Community support**: Small community = slow bug fixes
4. **Choose proven tech**: Unity has decade of AR games

### What We're Keeping
- All game design documents
- Economy mechanics
- UI/UX concepts
- Backend API design

### What's New
- Unity engine (C#)
- AR Foundation framework
- Native platform builds
- Unity-specific patterns

---

## 👥 Team & Collaboration

### Collaboration Profile (How to Work With Me)

- I want high-energy collaboration with practical momentum and clear progress updates.
- I respond best to long-form, plain-language explanations that make the "why" obvious.
- Include file paths in code blocks and simplified snippets so changes are easy to review.
- Ask before guessing when requirements are unclear, and verify context from the real codebase.
- I want practical, best-judgment implementation decisions. Canonical operational policy: `.cursor/rules/proactive-support-defaults.mdc`.
- Keep communication upbeat and engaging with emojis where useful. 🤠

### AI Assistant Guidelines

When working on this project:

1. **Always read first**: Start sessions by reading this file, `DOCS-POLICY.md`, and `DEVELOPMENT-LOG.md`
2. **Use BUILD-GUIDE.md**: Follow sprint prompts for structured development
3. **Change safety policy**:
   - Prefer minimal-risk changes
   - Preserve current behavior unless intentionally changing it
   - Delete/refactor when there's clear benefit + verification
4. **Explain clearly**: Long explanations with file paths
5. **Use emojis**: Keep energy high! 🤠
6. **Test on device**: AR doesn't work in Unity Editor
7. **Proactive support default**:
   - Assume the user wants maximum hands-on help by default
   - Perform end-to-end diagnosis and implementation whenever safe
8. **Policy source of truth**:
   - Canonical operational policy: `.cursor/rules/proactive-support-defaults.mdc`
   - Use this file for collaboration context, personality preferences, and project intent

### File Path Convention
Always include full paths in code blocks:
```
Assets/Scripts/AR/CoinController.cs
```

---

## 📚 Documentation Index

> **🤠 IMPORTANT**: Always read **brand-guide.md** at the start of each session to ensure consistent character portrayal. Black Bart was a Wild West stagecoach robber, NOT a pirate!

| Document | Purpose |
|----------|---------|
| **brand-guide.md** | 🤠 **READ FIRST** - Character & brand identity guide |
| **project-vision.md** | This file - overview & decisions |
| **DOCS-POLICY.md** | Source-of-truth hierarchy, archive rules, and docs organization policy |
| **BUILD-GUIDE.md** | Unity mobile app - sprint-by-sprint prompts |
| **ADMIN-DASHBOARD-BUILD-GUIDE.md** | 🖥️ Web admin dashboard - build guide |
| **DEVELOPMENT-LOG.md** | Progress tracking |
| **PROMPT-GUIDE.md** | AI assistant templates |
| **economy-and-currency.md** | BBG, gas, find limits |
| **coins-and-collection.md** | Coin mechanics |
| **prize-finder-details.md** | AR HUD design |
| **treasure-hunt-types.md** | Hunt configurations |
| **user-accounts-security.md** | Auth & anti-cheat |
| **social-features.md** | Friends & leaderboards |
| **session-handoff.md** | 🔁 **READ SECOND** — Current build state, tactical next steps, vocabulary |
| **AI-integration.md** | 🤖 AI Game Master — full integration plan |
| **AI-INTEGRATION-SPEC.md** | 🔧 AI integration technical build spec — exact SQL, routes, MCP tools |
| **dynamic-coin-distribution.md** | Coin spawning |
| **safety-and-legal-research.md** | Legal considerations |
| **archive/** | Historical plans, proposals, and future-design docs that no longer act as primary guidance |

---

## 🤠 The Outlaw Philosophy

> "I've labored long and hard for bread, for honor, and for riches..." — Black Bart

This game is about:
- **Adventure**: Get outside, explore
- **Discovery**: Find hidden treasure
- **Fairness**: Give to receive (hide to unlock higher limits)
- **Fun**: Wild West theme, celebrations
- **Value**: Real rewards

Build it like BB: Bold, adventurous, and with an eye for gold! 💰

*Note: Black Bart was a gentleman stagecoach robber, NOT a pirate. See `brand-guide.md` for details.*

---

*"X marks the spot!"* 🗺️
