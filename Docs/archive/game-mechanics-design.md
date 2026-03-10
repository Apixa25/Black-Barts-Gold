# 🎮 Black Bart's Gold — Game Mechanics Design

> *"The game doesn't just hand you treasure. It makes you earn it, crave it, and come back for more."* 🤠

**Document Status**: Living Document — expand as features are built  
**Created**: March 4, 2026  
**Origin**: Design session merging Call of Duty franchise mechanics with BBG's AR treasure hunt, filtered through the AI Game Master architecture from `AI-integration.md`  
**Purpose**: Define progression systems, engagement loops, feedback design, and competitive structures that make BBG addictive and fun

---

## 🧭 Design Philosophy

Call of Duty keeps millions playing through **layered progression, meaningful choices, momentum rewards, satisfying feedback, and social status signaling**. Every one of these patterns can be adapted to a location-based AR treasure hunt — especially when an AI Game Master is dynamically running the show.

The AI is what makes BBG's version of these mechanics *superior* to static implementations. CoD perks do the same thing every match. **BBG perks are AI-modulated based on weather, time, player state, and game economy.**

---

## 1. 🧬 Outlaw Perks — AI-Assigned & Adaptive

Players equip **3 Outlaw Perks** from 3 categories. The AI Game Master can temporarily grant or modify perks based on real-world conditions, player behavior, or events.

### 🔴 Prospector Perks (Aggressive / Value-Focused)

| Perk | Effect | CoD Parallel |
|------|--------|-------------|
| **Gold Nose** | Coin detection radius increased by 25% on radar | Double Time |
| **Claim Jumper** | Multi-find coins show a countdown timer of other hunters approaching | High Alert |
| **Assayer's Eye** | Pool coins reveal their value tier (bronze/silver/gold glow) before collection — removes the "?" mystery but lets you prioritize | Engineer |
| **Motherlode** | 10% chance any collected coin triggers a "vein" — 2-3 bonus coins spawn nearby within 60 seconds | Kill Chain |
| **Pocket Scale** | Collecting coins above your current average value gives +5% bonus value | Scavenger |
| **Dynamite** | Once per hunt session, instantly reveal all coins within 100m for 30 seconds (cooldown: 2 hours) | Predator Missile equivalent |

### 🔵 Scout Perks (Stealth / Information)

| Perk | Effect | CoD Parallel |
|------|--------|-------------|
| **Ghost Trail** | Your position is hidden from other hunters on the shared map for 5 minutes after starting a hunt | Ghost |
| **Tracker** | See faint "footprint trails" on the AR view showing paths other hunters recently took — avoid picked-over areas | Tracker |
| **Cartographer** | Mini-map shows coins at 2x the normal range, but without value info | Forward Intel |
| **Sixth Sense** | Phone vibrates in a hot/cold pattern as you get closer to hidden coins, even when AR view is off | Vigilance |
| **Stagecoach Ear** | Receive a Black Bart whisper notification when a high-value coin spawns within 500m | High Alert |
| **Drifter** | Coins you discover in areas you've never hunted before are worth 15% more | Cold-Blooded |

### 🟢 Outlaw Perks (Utility / Social / Defensive)

| Perk | Effect | CoD Parallel |
|------|--------|-------------|
| **Gas Saver** | Daily gas consumption reduced by 15% | Flak Jacket |
| **Quick Draw** | Coin collection tap-to-collect speed is 40% faster (matters in multi-find races) | Fast Hands |
| **Posse Up** | When hunting with guild members nearby, all members get +10% coin value | Guardian |
| **Stash Master** | Coins you hide for others take 25% longer to expire | Quartermaster |
| **Bounty Board** | Access to 1 extra daily contract slot | Gearhead |
| **Iron Horse** | Your find limit is treated as one tier higher for 1 hunt per day | Hardline |

### ⚡ Mono-Category Bonus (From BO6's Combat Specialty)

If all 3 equipped perks are from the **same category**, unlock a powerful 4th bonus:

| Category | Bonus Effect |
|----------|-------------|
| **All Prospector** | "Gold Rush" — Every 5th coin collected in a session is worth double |
| **All Scout** | "Outlaw's Instinct" — AR view highlights the single most valuable coin in your area with a golden beam |
| **All Outlaw** | "Gang Leader" — Your guild members within 200m share your perk effects |

### 🤖 AI-Modulated Perk Behavior

Static perks are CoD. **Living perks are BBG:**

- **Weather-reactive**: "Gold Nose" detection radius doubles during rain → *"Rain's washing the gold to the surface, partner."*
- **Time-of-day**: "Ghost Trail" lasts 10 minutes instead of 5 during nighttime hunts → *"The dark's your friend tonight."*
- **AI-granted temporary perks**: Churn Prevention Agent grants lapsed players a 24-hour "Welcome Back" perk combining Gold Nose + Motherlode
- **Territory-specific**: Entering an Outlaw Territory zone could override one of your perks with a zone-specific one

---

## 2. 📈 Prestige & Mastery — The Long Grind

### Extended Tier System with Prestige

Existing tier system from `FindLimitService`:

```
Cabin Boy ($1) → Deck Hand ($5) → Treasure Hunter ($10) → Captain ($25) → Pirate Legend ($50) → King of Pirates ($100+)
```

**Prestige Layers (Voluntary Reset for Status):**

| Prestige | What Happens | Reward |
|----------|-------------|--------|
| **Prestige 1** | Voluntary reset to Cabin Boy; re-earn all tiers | "Veteran Outlaw" avatar frame + 1 permanent perk slot unlock |
| **Prestige 2** | Reset again | "Seasoned Desperado" frame + access to Legendary hunt types |
| **Prestige 3** | Reset again | "Black Bart's Inner Circle" frame + exclusive territory perks |
| **Prestige 4** | Reset again | "Ghost of the West" animated frame + AI personal treasure maps |
| **Prestige 5 (Master)** | Final prestige — keep all unlocks permanently | "Legend of the West" — BB name-drops you in server-wide notifications |

When a player prestiges, the AI Game Master broadcasts: *"A new outlaw has risen through the ranks and started fresh. Respect the grind, partners. {PlayerName} just hit Prestige 3."*

### Mastery Milestones (CoD Camo Grind → BBG Collection Milestones)

| Mastery Level | Requirement | Reward |
|---------------|------------|--------|
| **Bronze Collector** | Collect 100 coins total | Bronze trail effect on AR avatar |
| **Silver Collector** | Collect 500 coins across 10+ different zones | Silver compass glow |
| **Gold Collector** | Collect 1,000 coins + 50 pool coins + 20 multi-find wins | Gold aura around collected coins |
| **Diamond Collector** | Complete all hunt types at least 10 times each | Diamond sparkle trail + custom coin animation |
| **Black Bart's Mark** | Prestige 3+ AND Diamond Collector AND 100 guild war victories | Animated legendary aura — the ultimate flex |

**Design Principle (from CoD community):** Earned cosmetics > Purchased cosmetics in community respect. Black Bart's Mark should be the hardest thing to earn and visible to everyone on leaderboards.

---

## 3. 🔥 Hunt Streaks — Momentum Compounding

Consecutive coin finds without ending a session trigger escalating rewards (CoD killstreak model):

| Streak | Coins Found | Reward | Black Bart Says... |
|--------|------------|--------|-------------------|
| **3** | 3 in a row | 📡 **Scout Report** — Next coin direction + distance revealed for 2 min | *"Three in a row! Let me point you to the next one..."* |
| **5** | 5 in a row | 💰 **Bonus Dust** — +15% value on next coin collected | *"Five! You're on fire. Next one's a little sweeter."* |
| **7** | 7 in a row | 🗺️ **Treasure Map** — All coins in 200m revealed for 60 seconds | *"Seven! Here's a proper treasure map, partner."* |
| **10** | 10 in a row | ⚡ **Gold Rush** — Next 3 coins are worth double | *"TEN! The whole territory's watching you now."* |
| **15** | 15 in a row | 🏴‍☠️ **Black Bart's Blessing** — Random legendary coin spawns just for you within 50m | *"Fifteen! I don't give this out lightly..."* |
| **25** | 25 in a row | 💀 **OUTLAW LEGEND** — Server-wide announcement + exclusive animated badge + 5x value on a personal legendary spawn | *"TWENTY-FIVE! Every outlaw in the territory just heard your name!"* |

### AI-Powered Streak Intelligence

- **Flow maintenance**: AI watches streaks in real-time and adjusts coin placement to keep the streak going *just barely* — coins spawn slightly closer, slightly more in your path
- **Anti-frustration**: At 9/10 streak, if GPS shows you heading home, AI drops a coin in your path → *"Heading home? There's one more right around that corner..."*
- **Streak-breaking events**: AI can spawn "Rival Gang" interference for players on high streaks — a special coin appears that, if another nearby player collects first, breaks your streak

---

## 4. 📋 Contracts — AI-Generated Micro-Objectives

Daily and weekly micro-objectives that drive return rate (from Warzone's contract system):

### Daily Contract Types

| Contract Type | Task | Reward | CoD Parallel |
|--------------|------|--------|-------------|
| **Bounty Hunt** | Collect the single highest-value coin in your area today | 2x value + XP | Bounty |
| **Scavenger Run** | Collect 5 coins in under 15 minutes | +25% gas refund | Scavenger |
| **Recon Sweep** | Visit 3 different zones in one hunt session | Reveal all coins in next zone entered | Recon |
| **Express Delivery** | Collect a specific coin within 10 minutes of it spawning | 3x value | Supply Run |
| **Most Wanted** | Hunt with your position visible to all nearby players for 20 min | Massive XP + exclusive daily badge | Most Wanted |
| **Prospector's Gamble** | Collect 3 pool coins in a row (unknown values) | If total exceeds $X, triple it | BBG original |

### Contract Multiplier Chain

Each completed contract increases a multiplier for the next:

```
Contract 1: Base reward (1.0x)
Contract 2: 1.25x reward
Contract 3: 1.5x reward
Contract 4: 2.0x reward
Contract 5+: 2.5x reward (capped for economy safety)
```

### AI-Powered Contract Generation

- **Personalized**: Player Classification System (Explorer/Achiever/Competitive/Social from `AI-integration.md`) generates contracts tailored to each archetype
- **Weather-reactive**: Rainy day → *"Sunken Treasure Salvage — collect 5 waterfront coins for triple value"*
- **Dynamic difficulty**: AI adjusts contract difficulty based on recent performance
- **Narrative**: Black Bart writes contract flavor text in character → *"A stagecoach full of gold was ambushed on Main Street. Find the bags before the law does."*

---

## 5. 🎒 Hunt Kits — The Loadout System

Players configure their approach before starting a hunt (CoD Create-a-Class → BBG Hunt Kit):

| Slot | Options | CoD Parallel |
|------|---------|-------------|
| **3 Perk Slots** | Choose from Prospector / Scout / Outlaw | Perk 1/2/3 |
| **Primary Tool** | Radar Boost / Extended Range Compass / Coin Magnet | Primary weapon |
| **Secondary Tool** | Gas Canister (+1 day gas) / Decoy Marker / Streak Shield | Secondary weapon |
| **Field Upgrade** | Treasure Scan (reveal area) / Ghost Mode (hide from others 5 min) / Recall (teleport last coin to you) | Field Upgrade |
| **Wildcard** | Take 2 perks from one category / Extra tool slot / Auto-collect within 5m | Wildcards |

Players save multiple kit presets for different situations:
- **"The Sprinter"** — Speed-focused: Double Time + Quick Draw + fast collection
- **"The Ghost"** — Stealth: Ghost Trail + Tracker + Sixth Sense for competitive multi-find hunts
- **"The Prospector"** — Value: Gold Nose + Assayer's Eye + Motherlode for maximizing coin value

---

## 6. 🎃 Seasonal Events — AI-Generated Living Content

| Event | Timing | Theme | AI Behavior |
|-------|--------|-------|-------------|
| **Gold Rush Week** | Monthly (first week) | Extra coins everywhere | Spawn Governor triples density |
| **The Great Heist** | Quarterly | Guild vs Guild raid event | AI creates territory maps, spawns "vault coins" in territories |
| **Black Bart's Birthday** | Annual | The biggest event of the year | AI spawns legendary coins worldwide; BB sends personal messages |
| **Ghost Gold (Halloween)** | October | Spooky coin variants that disappear faster | AI adjusts coin expiry; "haunted" zones |
| **Outlaw's Last Ride** | Dynamic | Golden hour high-value spawns | Real-World Signal Injection; AI detects sunset |
| **Moonshine Night** | Monthly (full moon) | Ultra-rare coin spawns | Astronomy API trigger; Black Bart's Legendary Cache |
| **Blizzard Bonanza** | December-January | Frozen coins requiring "warming up" (stay near for 30s) | Weather API; AI modifies collection mechanics |

### Mid-Season Surprise Events (AI-Generated, No Human Intervention)

*"A train robbery just happened on the western tracks. Gold scattered from Market Street to the waterfront. Move fast — it expires in 2 hours."*

### Limited-Time Hunt Modes

| Mode | Duration | Mechanic | CoD Parallel |
|------|----------|----------|-------------|
| **Showdown** | 1 hour | Race against 10 nearby players for the same 20 coins | Free-for-All |
| **Posse Raid** | 2 hours | Your guild vs rival guild in a territory | Ground War |
| **Lone Wolf** | 30 min | Hunt solo with no radar, no compass, pure AR | Search & Destroy |
| **Prospector's Gamble** | 1 hour | All coins are pool coins with higher variance | Gun Game |
| **Black Bart's Gauntlet** | 3 hours | Sequential coin chain — each coin reveals the next, getting harder to reach | Campaign missions |

---

## 7. 💰 Seasonal Treasure Map (Battle Pass)

100-tier progression system, split into Free and Premium tracks:

| Tier Range | Free Track | Premium Track |
|-----------|-----------|--------------|
| **1-10** | Gas days, basic avatar items | Exclusive perk "skin" (visual variant), premium gas days |
| **11-25** | XP boosts, radar upgrades | Unique coin trail effects, premium tool |
| **26-50** | Contract rerolls, temporary perks | Exclusive hunt mode access, animated avatar frame |
| **51-75** | Gas days, wildcard tokens | Black Bart voice pack, exclusive territory bonus |
| **76-100** | Prestige token, final free cosmetic | Legendary avatar aura, permanent season emblem on profile |

Premium track costs BBG coins (tied to in-game economy). AI Game Master grants "tier skip" tokens for completing especially difficult contracts.

---

## 8. 🔊 Dopamine Design — Collection Feedback Chain

Every coin collection should feel incredible (CoD hit markers → BBG collection feel):

| Moment | Visual | Audio | Haptic |
|--------|--------|-------|--------|
| **Coin spotted** | Glow appears in AR; radar ping | Distant metallic chime | Subtle pulse |
| **Getting closer** | Glow intensifies; sparkles increase | Heartbeat intensifies | Rhythmic vibration increases |
| **In range** | Crosshairs turn green; coin enlarges | "Lock on" tone | Strong single pulse |
| **Tap to collect** | Coin flies to camera with trail | Satisfying "clink" + value callout | Sharp tap |
| **High value coin** | Golden explosion + particle shower | Treasure chest sound + BB voice line | Extended rumble |
| **Streak milestone** | Full-screen banner + streak count | Announcer callout (BB's voice) | Triple pulse |
| **Level up** | Tier emblem animation + rank display | 3-second Western fanfare + BB congratulations | Celebration pattern |
| **Legendary find** | Screen goes gold; slow-mo coin reveal | Epic orchestral sting + BB monologue | Sustained rumble |

### Black Bart Voice Lines (AI-Generated & Contextual)

- Standard: *"That's another one for the saddlebag."*
- 5-streak: *"Five in a row! You hunt like you were born on the trail."*
- High value: *"Now THAT'S a find. The Wells Fargo company is missing that one."*
- Rival beat: *"Ha! {RivalName} was chasing that one too. Too slow for the likes of you."*
- Weather: *"Finding gold in the rain? You've got the soul of a true outlaw."*
- Time: *"Hunting by moonlight. My kind of desperado."*

---

## 9. 🏆 Competitive Systems — Ranked Hunts

### Ranked Hunt Tiers

| Rank | Tier | Requirement |
|------|------|-------------|
| **Bronze Outlaw** | Entry | Complete 10 ranked hunts |
| **Silver Sheriff** | Intermediate | Top 50% in weekly collections |
| **Gold Marshal** | Advanced | Top 25% weekly + 5 streak of 10+ |
| **Platinum Ranger** | Expert | Top 10% + Prestige 1+ |
| **Diamond Desperado** | Elite | Top 5% + Prestige 2+ + 50 guild war wins |
| **Iridescent Legend** | Pinnacle | Top 1% — Season stat threshold |
| **Top 50** | Ultimate | Top 50 players globally — custom AI-generated hunt events |

### Guild (Posse) System

| Feature | Effect |
|---------|--------|
| **Posse Size** | Up to 50 members |
| **Posse XP Boost** | +10% coin value when hunting near posse members |
| **Posse Tag** | Custom 4-letter tag shown on leaderboards |
| **Posse Territory** | Weekly AI-assigned zones to defend |
| **Posse Challenges** | Weekly cooperative goals (collect 500 coins as a posse) |
| **Posse War** | Automated weekly matchup against rival posse |

---

## 10. 🤖 The AI Advantage — What Makes BBG Different

| Feature | CoD (Static) | BBG (AI-Powered) |
|---------|-------------|------------------|
| **Perk effects** | Same every match | AI modulates based on weather, time, player state |
| **Streaks** | Fixed thresholds | AI adjusts coin placement to maintain flow state |
| **Contracts** | Pre-designed pool | AI generates unique contracts daily per player archetype |
| **Events** | Quarterly, manually designed | Weekly+ AI-generated events with narrative context |
| **Balance** | Patch every 2-4 weeks | Economy Balancer adjusts in real-time |
| **Churn prevention** | Generic "come back" email | AI spawns personal coin at your favorite park with custom BB message |
| **Narrative** | Scripted cutscenes | Black Bart reacts to YOUR hunt in real-time |
| **Seasonal meta** | Designers create content calendar | AI generates meta shifts based on player behavior data |

---

## 📊 Implementation Priority

| Priority | Feature | Why First | Depends On |
|----------|---------|-----------|-----------|
| **1** | Hunt Streaks | Simple counter + reward multiplier; huge engagement impact | CollectionService (exists) |
| **2** | Daily Contracts (AI-generated) | Drives daily return rate; uses existing AI Spawn Governor | MCP tools (Phase 1-2) |
| **3** | Outlaw Perks (6-8 starter perks) | Identity + meaningful choice; transforms hunt feel | PlayerData, GameManager (exist) |
| **4** | Collection Feedback Chain | Dopamine design; makes every coin feel amazing | CoinController, HapticService (exist) |
| **5** | Seasonal Treasure Map (Battle Pass) | Monetization + long-term progression | Economy system (exists) |
| **6** | Ranked Hunts + Posse System | Competitive endgame | Leaderboards + social features (Phase 2-3) |
| **7** | Prestige System | Long-term retention for dedicated players | Find Limit tiers (exist) |
| **8** | Full Kit/Loadout System | Theorycraft depth | All perks + tools implemented |

---

## 🔗 Related Documents

- [AI Integration Plan](../AI-integration.md) — The AI architecture that powers all of these systems
- [PvP Game Design](./pvp-game-design.md) — Player-vs-player interaction, coin types, player-as-coin modes
- [Coins & Collection](../coins-and-collection.md) — Base coin mechanics
- [Treasure Hunt Types](../treasure-hunt-types.md) — Hunt configurations
- [Economy & Currency](../economy-and-currency.md) — BBG, gas, find limits
- [Social Features](../social-features.md) — Friends & leaderboards

---

*"The game doesn't just watch you play. It plays alongside you — rewarding, challenging, and surprising at every turn."* 🤠🗺️💰
