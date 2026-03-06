# ⚔️ Black Bart's Gold — PvP & Player Interaction Design

> *"The most dangerous thing in the West wasn't a gun — it was a coin you didn't look at twice."* 🤠

**Document Status**: Living Document — expand as features are built  
**Created**: March 4, 2026  
**Origin**: Design session focused on player-vs-player mechanics, coin type taxonomy, "player as the treasure" concepts, and player-centric game modes  
**Purpose**: Define how players interact with, compete against, deceive, and surprise each other — all through the game world, never through physical confrontation

---

## 🧭 Core PvP Philosophy

The best PvP in location-based games is **indirect** — players compete through the game world, not through physical confrontation. The Division's Dark Zone, Sea of Thieves' treasure stealing, and Dark Souls' invasions all create heart-pounding tension through *systems*, not combat.

BBG achieves the same adrenaline through **coins, wagers, deception, and the AI Game Master** — while keeping players physically safe from each other.

### Key Design Principles

| Principle | Source | BBG Application |
|-----------|--------|----------------|
| **Vulnerable state mechanic** | Division (contaminated loot), Sea of Thieves (unsold treasure) | Coins in "saddlebag" state before extraction |
| **Escalating risk/reward** | Division (rogue levels), Dark Souls (ember) | Outlaw mode with escalating bounty |
| **Player as content** | Mario Maker, geocaching, Fortnite Creative | Players create treasure, traps, mysteries for each other |
| **Intermittent information** | SpyParty, AC Brotherhood, Gottcha | Periodic pings, not constant tracking |
| **Asymmetric roles** | Dead by Daylight, SpyParty, Among Us | Outlaw vs Deputies, Hider vs Seekers |
| **Meaningful consequences** | EVE Online (permanent loss), Foxhole (logistics dependency) | Real crypto value = genuine stakes |
| **Social trust as currency** | Among Us, Pokémon GO friendship tiers | Guild reputation, outlaw reputation |

---

## PART 1: THE COMPLETE COIN TYPE TAXONOMY 🪙

Different coin types transform every collection from a routine tap into a **decision moment**. The core PvP tension: hostile coins are visually indistinguishable from normal coins at first glance.

> "Is this coin a gift or a trap? Do I tap it or walk away?"

### 💰 Value Coins (Give to Finder)

| Coin Type | Visual | Behavior | Who Places It |
|-----------|--------|----------|--------------|
| **Fixed Value** | Classic gold doubloon, value displayed | Known value, collect to earn | System, Players, Sponsors |
| **Pool / Mystery** | Shimmering "?" coin | Value revealed on collection (slot-machine odds) | System, Players |
| **Bounty Coin** | Skull-stamped gold with red glow | Worth 2-3x normal but harder to find (compass-only, no radar) | AI Game Master, Admins |
| **Gift Coin** | Coin wrapped in a ribbon (AR effect) | Addressed to a specific player — only they can collect it | Players (send to friends) |
| **Alliance Coin** | Coin stamped with guild emblem | Only collectible by members of the hider's guild | Guild members |
| **Chain Coin** | Coin with a small chain link icon | Collecting reveals the location of the NEXT coin in a sequence | Players, AI Game Master |

### 💣 Hostile Coins (Take from Finder)

Every hostile action has a **real cost** to the aggressor — the placer pays upfront. This prevents griefing.

| Coin Type | Visual | Effect on Finder | Cost to Place |
|-----------|--------|-----------------|--------------|
| **Bomb Coin** | Looks identical to a normal coin | Deducts a set amount of XP or BBG from the finder | Placer wagers the "damage" amount (e.g., place a $2 bomb = finder loses $2, placer pays $2 upfront) |
| **Gas Leak Coin** | Looks identical to normal coin | Drains 1 day of gas from the finder's tank | Costs 1 day of the placer's gas to set |
| **Compass Curse** | Looks identical to normal coin | Finder's compass spins wildly for 3 minutes (can't navigate) | Costs XP to place |
| **Time Bomb Coin** | Normal coin with a hidden timer | If not collected within 60 seconds of appearing on radar, it "detonates" and removes nearest uncollected coin from the area | AI Game Master only |
| **Mimic Coin** | Appears as a high-value gold coin | Actual value is 1/10th of displayed amount (shows $50, pays $5) | Players pay the REAL value + a small premium |
| **Tracker Coin** | Looks identical to normal coin | After collecting, the hider can see what ZONE (not exact GPS) the finder hunts in for the next 2 hours | Costs XP to place |

### ⭐ XP & Progression Coins (No Real Money)

| Coin Type | Visual | Behavior | Purpose |
|-----------|--------|----------|---------|
| **XP Coin** | Blue-tinted doubloon with a star | Grants XP toward tier progression, zero monetary value | Advance without spending real money |
| **Streak Coin** | Coin with a flame icon | Counts toward Hunt Streak chain but has no monetary value | Keep streaks alive in low-density areas |
| **Challenge Coin** | Coin with crossed-swords emblem | Requires completing a mini-challenge to claim (e.g., "collect 3 coins in 5 minutes") | Skill-gated progression |
| **Prestige Token** | Platinum coin with a crown | Ultra-rare; counts toward prestige requirements | Long-term progression carrot |

### 💌 Social & Communication Coins

| Coin Type | Visual | Behavior | Use Case |
|-----------|--------|----------|----------|
| **Secret Message Coin** | Coin with a sealed letter icon | Contains a text message from the hider (preset templates to prevent abuse) | Leave notes, clues, lore for strangers |
| **Bottle Coin** | Corked glass bottle in AR | Contains a small BBG gift ($0.10-$1) + a message + sender's name | Asynchronous gifting (Death Stranding-style) |
| **Calling Card Coin** | Coin stamped with the hider's avatar | No value — purely "I was here" marker. Finder sees who left it + their profile | Social discovery, reputation building |
| **Taunt Coin** | Coin with a laughing skull | Contains a voice line from the hider (preset templates) taunting the finder. Worth $0. | Psychological warfare, humor, rivalry fuel |
| **Invitation Coin** | Coin with a handshake icon | Collecting sends both players a "friend request" + small XP bonus | Social connection mechanic |

### 🎲 Wildcard Coins (Random / Special)

| Coin Type | Visual | Behavior | Rarity |
|-----------|--------|----------|--------|
| **Mystery Box** | Chest-shaped coin with "?" | Random effect: could be value, bomb, XP, message, curse, or JACKPOT | Rare — AI-placed during events |
| **Portal Coin** | Swirling dark energy coin | Shifts the player to "Shadow Realm" mode for 5 minutes (different coin layer, higher risk/reward) | Very rare — AI-placed at night/storms |
| **Duel Coin** | Two crossed pistols icon | Collecting triggers a "Showdown" — nearest rival player challenged to a timed collection race | Rare — AI-placed in competitive zones |
| **Black Bart's Personal Coin** | Animated gold coin with BB's face | Ultra-high value + personalized AI-generated Black Bart message. A legendary find. | Extremely rare — 1 per city per week |
| **Copycat Coin** | Coin that mirrors your avatar | Duplicates the last coin you collected (same type, same value). If your last was a bomb... you get bombed again. | Uncommon — AI-placed for chaos |

### 🔍 Trap Detection (Counterplay)

Hostile coins must be beatable through skill and progression:

| Detection Method | What It Reveals | How to Earn |
|-----------------|----------------|-------------|
| **"Assayer's Eye" perk** | Hostile coins have a faint red shimmer in AR | Equip the perk (see `game-mechanics-design.md`) |
| **Long-press inspection** | Hold on a coin for 3 seconds to "inspect" — reveals coin type (but alerts the hider) | Available to all players |
| **Trail Markers** | Other players' warnings ("⚠️ Trap ahead, beware of fakes") | Community knowledge |
| **Hunt Streak bonus at 7** | Treasure Map reveals all coin types in 200m for 60 seconds | Earn through streak |
| **Higher tier = better detection** | Captain+ tier players see a subtle glow difference on traps | Progression reward |

### 🤖 AI Game Master Coin Management

| AI Behavior | Coin Types Used | Logic |
|-------------|----------------|-------|
| **Spawn Governor** | Fixed, Pool, XP, Streak | Matches coin types to player density and tier |
| **Churn Prevention** | Gift, Bounty, BB's Personal | High-value personalized coins for at-risk players |
| **Economy Balancer** | Adjusts ratio of Fixed vs Pool vs XP | If too much real money flowing, increase XP coin ratio |
| **Game Master Events** | Mystery Box, Portal, Duel, Time Bomb | Creates chaos and excitement during events |
| **Real-World Signals** | Portal (storms), Chain (weekends), Bounty (sunset) | Weather/time triggers specific coin types |
| **Territory Wars** | Alliance, Duel, Bomb | Territories get hostile coin types during guild wars |

---

## PART 2: PLAYER BECOMES THE COIN — "Become the Treasure" 🎭

The player IS the treasure. A player wagers that other players won't be able to find them. Draws from Assassin's Creed Brotherhood's "hide in plain sight," SpyParty's behavioral tells, and Dead by Daylight's asymmetric design.

### Core Mechanic

**Setup:**
1. Player activates "Become the Treasure" mode
2. Sets a **wager amount** (e.g., $5 in BBG)
3. Chooses a **hide zone** (public area within 500m of current location)
4. Must stay within that zone for a set duration (15-30 minutes)
5. Their avatar becomes a **virtual treasure coin** placed at their real GPS location
6. The coin updates position as they move — it IS them

**For Seekers:**
- The "Player Coin" appears on nearby hunters' radars like any other high-value coin
- Looks like a normal coin in AR — but it *moves* subtly (because the player is walking)
- Value displayed shows the wager amount (seekers know the stakes)
- Must get within collection range AND tap to "collect"

**Outcomes:**

| Scenario | Hider Gets | Seeker Gets |
|----------|-----------|-------------|
| **Not found** (timer expires) | Keeps wager + earns the same as profit (doubles money) | Nothing |
| **Found by seeker** | Loses wager | Wins the wager amount |
| **Found by multiple seekers** (if enabled) | Loses wager, split among finders | Gold/Silver/Bronze split of wager |

### Tier Variants

| Tier | Name | Wager | Duration | Zone | Seeker Aids |
|------|------|-------|----------|------|------------|
| **Bronze** | "Penny Hide" | $1-$5 | 15 min | 200m | Compass |
| **Silver** | "Dollar Dodge" | $5-$25 | 20 min | 300m | Radar only |
| **Gold** | "Treasure Trail" | $25-$50 | 30 min | 500m | AR only (no radar/compass) |
| **Legendary** | "Black Bart's Shadow" | $50+ | 45 min | 1km | No aids — pure visual AR |

### AI Integration

- AI Game Master places **decoy moving coins** in the same zone to make identification harder
- AI announces hide events: *"Someone's turned themselves into treasure downtown. $10 says you can't find 'em. 🤠"*
- AI adjusts difficulty: if hiders always win, expand seeker tools; if seekers always find, expand zones

### Safety Architecture

| Safety Layer | Implementation |
|-------------|---------------|
| **No exact position** | Seekers see the "Player Coin" on radar as an approximate zone (50m radius), not exact GPS |
| **Public areas only** | Hide zones AI-validated via Google Places API — must be parks, commercial districts, malls |
| **Daylight only** | Available between sunrise and sunset (local time) |
| **Minimum population** | Zone must have minimum active BBG players to activate (prevents 1v1 stalking) |
| **Anonymous until found** | Seekers don't see WHO the Player Coin is — no username, no profile until collection |
| **Abort button** | Hider can cancel at any time and forfeit 50% of wager |
| **Zone boundary alerts** | If hider leaves the zone, 60 seconds to return or forfeit |
| **Buddy system** | Optional: share hide session with a trusted contact who sees your real GPS |
| **Age gate** | 18+ only for wager modes; under-18 can play XP-only versions |
| **Report system** | Both hiders and seekers can report uncomfortable situations |

---

## PART 3: PLAYER-CENTRIC MODES — "You Are the Adventure" 🤠

Beyond coins, modes where the PLAYER is the center of the action.

### Mode 1: "Stagecoach Run" (The Extraction)

Inspired by The Division's Dark Zone extraction mechanic.

**Concept:** Coins collected in a Stagecoach Zone are in **"saddlebag" state** — collected but NOT yet secured. You must physically walk to a Stagecoach extraction point to lock them in.

**Flow:**
1. Enter a "Stagecoach Zone" (~1km radius)
2. Coins collected here go into saddlebag (not wallet)
3. Saddlebag contents visible to nearby players as a bounty indicator
4. 2-3 Stagecoach extraction points in the zone (public, well-lit areas)
5. Walk to extraction point → hold for 30-second "loading" action
6. During countdown, ALL nearby players alerted: *"A stagecoach is loading at the fountain! There's $15 aboard!"*
7. If another player reaches the extraction point during countdown and taps "Rob," they steal 50% of saddlebag
8. If countdown completes without robbery → all coins secured to wallet

**Risk/Reward Tension:**
- Extract with $3 safely now... or keep hunting and extract with $15 at higher risk
- The more you carry, the bigger the target
- Robbery is indirect — happens at the extraction point through the game system, not physical confrontation

### Mode 2: "The Outlaw" (Going Rogue)

Inspired by Dark Souls' invasion system and the Division's Rogue mechanic.

**Concept:** One player "goes outlaw" and gains special abilities to interfere with other hunters — but becomes a high-value target.

**Abilities (15-minute duration):**
- Place Bomb/Curse coins at will (limited supply: 5 per session)
- "Curse" an existing coin — turning the next collect into a trap
- See all hunters' general zones on map (200m accuracy)
- Coins collected are worth 2x value

**Costs:** Gas + XP deposit to activate

**Escalation (Division Rogue System):**

| Outlaw Level | Trigger | Effect |
|-------------|---------|--------|
| **Rogue** | Activate Outlaw mode | Zone-wide announcement, 2x coins, can place traps |
| **Desperado** | Place 3+ traps that hit players | Bounty doubles, your zone accuracy tightens to 100m for hunters |
| **Most Wanted** | Out-collect all hunters at 10-minute mark | Bounty triples, hunters see 50m radius. Survive full 15 min = MASSIVE payout |

**The Friend-or-Foe Twist (from Dark Souls' Mound Makers):** An outlaw can "go straight" at any time — drop a high-value Gift Coin and lose outlaw status. Creates ambiguity: is that outlaw going to bomb you, or gift you?

### Mode 3: "Posse Heist" (Cooperative PvP)

Inspired by Sea of Thieves crew-based treasure and Foxhole logistics.

**Concept:** Two guilds compete to collect coins dropped by a virtual stagecoach moving along a real-world route.

**Flow:**
1. AI creates a "Stagecoach Route" — real-world path along roads (1-2km)
2. Virtual stagecoach moves along route over 30 minutes
3. Drops coins along its path as it moves
4. Two guilds (4-6 players each) compete to collect the most
5. Each guild has a "Strongbox" location — coins must be deposited within 5 minutes or they expire
6. Guilds can place trap coins near the OTHER guild's Strongbox

**Team Strategy:** Split roles — some chase the stagecoach, others guard the Strongbox, others place traps near the enemy's Strongbox. Rewards team coordination, not just individual speed.

### Mode 4: "Phantom" (Decoy Creator)

Inspired by SpyParty's behavioral tells and AC Brotherhood's "hide among NPCs."

**Concept:** A player's real coin collections spawn **Decoy Coins** at nearby locations, flooding the area with fakes.

**Flow:**
1. Activate "Phantom" mode (costs gas)
2. For 10 minutes, every coin collected spawns a Decoy Coin at a nearby random location
3. Decoys look identical to real coins but are worth $0
4. Other hunters suddenly see twice as many coins — but half are fakes
5. Phantom profits from confusion while others waste time on decoys
6. If hunters correctly flag 3 decoy coins, the Phantom is "unmasked" and pays a penalty

**The Tell:** Decoy coins don't have the subtle spinning animation that real coins do — they're slightly static. Observant players can spot the difference.

### Mode 5: "The Sheriff" (Reactive Bounty Hunter)

Inspired by Red Dead Online bounty hunting.

**Concept:** When an Outlaw is active or too many Bomb coins are placed, the AI deputizes a nearby hunter.

**Flow:**
1. BB announces: *"There's an outlaw loose. I need a deputy. Who's in?"*
2. First player to accept becomes "The Sheriff"
3. Sheriff abilities: see trap coins (glow red), disarm traps (turn into XP coins), 3x XP for disarms, compass pointing to trap concentrations
4. Sheriff disarms all traps → Outlaw is "arrested" (loses remaining traps, pays bounty to Sheriff)

### Mode 6: "Dead Man's Hand" (High-Stakes Wager)

Inspired by CoD Black Ops Wager Matches.

**Concept:** Pure wager showdown, 2-6 players hunting identical coin layouts.

**Flow:**
1. Buy into a match (wager: $1-$25)
2. AI spawns IDENTICAL coin set for all participants
3. 10-minute timer — collect as many as possible
4. Payout: 1st = 50%, 2nd = 30%, 3rd = 20% of total pot
5. Bottom half: lose entire wager
6. Black Bart's cut: 10% house fee (revenue model)

**Skill Factor:** Identical layouts mean the winner is determined by navigation skill, route optimization, and speed — not luck. Meets the "65% skill predominance" standard for legal real-money wagering.

### Mode 7: "Haunted Trail" (Asynchronous Ghost Competition)

Inspired by Dark Souls messages and Death Stranding social strand.

**Concept:** Compete against recorded "ghost trails" from previous hunts, not live players.

**Flow:**
1. When you complete a hunt, your path and collection times are recorded as a "Ghost Trail"
2. Other players challenge your ghost: hunt the same zone, try to beat your time/value
3. Challenger sees a translucent "ghost" version of you walking the route — a visible pace car
4. Beat the ghost's total → earn a bonus. Lose → nothing bad happens

**Why This Is Powerful:**
- Completely asynchronous — no need for simultaneous online players
- Works in low-population areas (competing against recordings)
- AI features BEST ghost trails as "Legendary Ghosts" — aspirational competition
- Zero safety concerns — chasing a replay, not a person

---

## PART 4: ASYNCHRONOUS COMMUNICATION 📜

Players communicate indirectly across time (Dark Souls messages, Death Stranding structures).

### Trail Markers (Dark Souls-Style Messages)

After collecting a coin, players can leave a Trail Marker using preset phrase templates:

**Template: [Emoji] + [Phrase 1], [Phrase 2]**

Emojis: 🤠 💀 💰 ⚠️ 🗺️ 🔥 ❄️ 🌙

| Phrase 1 Options | Phrase 2 Options |
|-----------------|-----------------|
| "Rich pickings ahead" | "try going north" |
| "Danger nearby" | "worth the trek" |
| "Nothing here, partner" | "beware of fakes" |
| "The trail runs cold" | "come at night" |
| "Jackpot territory" | "bring extra gas" |
| "Trap coin spotted" | "guild territory" |
| "Outlaw was here" | "trust no one" |
| "Keep walking" | "check the shadows" |

- Appear in AR as small wooden signposts
- Rating system: Tap ✅ (helpful) or ❌ (misleading)
- High-rated markers glow brighter and persist longer
- Author gets +gas bonus for highly-rated markers
- Can be used to **deceive** — community polices through ratings

### Ghost Footprints

In Shadow Realm mode, players see ghostly footprints from other hunters:
- Fade after 1 hour
- Near collected coins: glow green (success trail)
- Near trap coins: glow red (warning trail)
- Creates organic "folk knowledge" layer

### Bottle Coins (Asynchronous Gifts)

Players leave "Bottles" at GPS locations containing:
- Small BBG gift ($0.10-$1)
- A trail marker message
- Their player name / guild tag

Creates the Death Stranding "warm fuzzy" loop — small kindnesses between strangers.

---

## PART 5: THE SHADOW REALM 🌑

Inspired by Zelda's Dark World and Silent Hill's Otherworld.

**Concept:** The same real-world map has TWO layers:
- **Normal World**: Regular coins, standard rules
- **Shadow Realm**: Hidden coins, harder to find, 2-3x value

**How to Enter:**
- Special "Shadow Portals" appear at GPS locations (AI places them at night, during storms, or during events)
- Player must be within 10m and tap the portal
- AR view shifts: color palette goes dark (sepia/red filter per brand-guide.md)
- Regular coins become INVISIBLE; Shadow coins become visible

**Shadow Realm Rules:**
- Timer: 5-10 minutes before pulled back to normal world
- Coins worth MORE but spawn LESS frequently
- Ghost footprints visible (see Asynchronous Communication)
- "Cursed Coins" appear ONLY in shadow realm — chance to REMOVE gas instead of giving value
- Risk/reward tension on every collection

**Visual Design (fits brand-guide.md):**
- Normal: Gold (#FFD700), warm tan backgrounds
- Shadow: Fire Orange (#E25822), dark leather (#3D2914) filter
- Shadow coins glow with eerie brass (#B87333) shimmer
- Black Bart's voice shifts to whisper in shadow realm

---

## PART 6: THE ECONOMY OF AGGRESSION 💰⚔️

### Core Rule: "It Costs to Be an Outlaw"

| Action | Cost to Aggressor | Payoff if Successful |
|--------|-------------------|---------------------|
| Place Bomb Coin | Pays the bomb amount upfront | Satisfaction + XP + Outlaw Reputation points |
| Place Gas Leak Coin | Costs 1 day of aggressor's gas | XP + reputation |
| Place Mimic Coin | Pays the REAL (lower) value upfront | Reputation + target sees taunt message |
| Go Outlaw mode | Gas cost + XP deposit | 2x coin value + bounty if successful |
| Become the Treasure | Wager amount locked | 2x wager returned if not found |
| Stagecoach Robbery | Free | 50% of victim's saddlebag |
| Phantom mode | Gas cost | Collected coins + confusion advantage |

### Outlaw Reputation — Parallel Progression Track

Like Sea of Thieves' Reaper's Bones faction:

| Outlaw Rank | Requirement | Unlocks |
|-------------|-------------|---------|
| **Petty Thief** | Place 10 trap coins | Access to Compass Curse coin type |
| **Stagecoach Robber** | Successfully rob 5 extractions | Access to Tracker Coin type |
| **Desperado** | Go Outlaw 20 times and survive | Outlaw-exclusive cosmetics (black hat, skull bandana) |
| **Infamous** | Win 50 wager matches | Access to Dead Man's Hand high-stakes tier |
| **Legend of the West** | Max Outlaw rank + Max regular rank | Ultimate dual-identity cosmetic — visible to all |

Players can be a sheriff on Monday and an outlaw on Friday. Reputation tracks separately, and other players see your outlaw rank on your profile.

### AI Economy Balancer — Aggression Monitoring

| Metric | Healthy Range | AI Response if Unhealthy |
|--------|--------------|-------------------------|
| Bomb coins / Total coins ratio | < 15% | If too many bombs, increase bomb placement cost |
| Extraction robbery success rate | 20-40% | If too high, increase extraction points; if too low, decrease them |
| Outlaw survival rate | 30-50% | If outlaws always win, increase Sheriff bounties; if always lose, buff outlaw abilities |
| "Become the Treasure" find rate | 40-60% | If too easy to find, expand zones; if too hard, give seekers better tools |
| Player complaints about traps | < 5% of sessions | If spike, temporarily increase trap visibility for all |

---

## PART 7: SAFETY ARCHITECTURE 🛡️

All PvP is designed so players NEVER need to physically find or confront each other.

### Foundational Safety Rules

| Rule | Implementation |
|------|---------------|
| **Never require co-location** | All competitive modes are score-based. Wager hunts: same zone, same timeframe, but no reason to find each other. Bounty hunts: chasing COINS, not PEOPLE. |
| **No lure mechanics in isolated areas** | AI Spawn Governor validates spawn locations in PUBLIC, WELL-TRAFFICKED areas. Use Google Places API walkability/safety scores. Zero coin spawns in isolated areas at night. |
| **Time-of-day safety gates** | After sunset: coin density shifts to well-lit commercial areas only. Night-mode coins spawn CLOSER to roads/buildings. Optional "Daylight Only" mode for parents/minors. |
| **No player location broadcasting** | Players NEVER see real-time GPS positions on the map. Guild members see ZONE (neighborhood-level) not exact location. Ghost footprints are time-delayed 1+ hour. |
| **Anti-luring protections** | Coins uncollected for >2hr in isolated locations auto-expire. AI monitors for "honeypot" patterns and flags/removes them. Player-hidden coins require minimum walkability score. Report button on any coin: "This location feels unsafe" → immediate review. |
| **Speed + awareness warnings** | Speed warning if GPS shows >15mph (driving detection). "Heads up, partner!" reminder every 5 minutes. Buddy system: share GPS with trusted contact (like Uber's share-ride). |

### Real-Money Wager Compliance

For modes involving real-money wagers (Dead Man's Hand, Become the Treasure):

| Requirement | Implementation |
|-------------|---------------|
| Age verification | 18+ with ID check |
| GPS geofencing | Restrict in states where skill-based wagering is prohibited |
| Skill predominance | Identical coin layouts ensure skill > chance (meets 65% threshold) |
| Fair play monitoring | Anti-cheat system |
| Player fund segregation | Escrow for active wagers |
| Responsible gaming | Loss limits, cool-off periods |
| Clear terms of service | Wager rules documented |

---

## PART 8: MYSTERY HUNTS — AI-Generated Investigations 🔍

Inspired by detective/mystery game design patterns and Among Us investigation mechanics.

### "The Stagecoach Mystery" (Investigation Mode)

**Setup (via AI Game Master):**
- BB announces: *"A legendary treasure has been stolen from the stagecoach. I need deputies to track it down. 🤠"*
- A "Case File" appears with: a cryptic riddle, 3 "Suspect" profiles, and a partial map

**Investigation Loop:**
1. **Clue Coins**: Special coins contain evidence fragments → *"The thief was last seen near water"*
2. **Witness Markers**: AR markers play AI-generated voice clips from "witnesses"
3. **Elimination**: Players mark suspects "Innocent" or "Guilty" based on collected evidence
4. **Race**: First to correctly unmask the thief wins the big prize; others earn smaller rewards

**Mystery Types:**

| Type | Duration | Clues Required |
|------|----------|---------------|
| Quick Case | 30 min | 3 clues |
| Full Mystery | 2 hours | 7 clues |
| Legendary Case | Multi-day | 15+ clues |
| Guild Mystery | Week-long | Guild co-op |

---

## PART 9: COIN METADATA SCHEMA EXTENSION

Existing coin fields from `admin-dashboard/src/types/database.ts`:

```
id, coin_type, value, tier, latitude, longitude, status,
hider_id, multi_find, finds_remaining, coin_model
```

**New fields needed for PvP:**

```
interaction_type:  "value" | "bomb" | "xp" | "message" | "gas_leak" |
                   "compass_curse" | "mimic" | "tracker" | "mystery_box" |
                   "portal" | "duel" | "gift" | "alliance" | "chain" |
                   "bounty" | "calling_card" | "taunt" | "invitation" |
                   "decoy" | "player_coin"

hostile:           boolean        -- Quick filter for trap detection perks
message_content:   text           -- For message/taunt coins (preset templates)
target_player_id:  uuid           -- For gift/invitation coins (null = anyone)
chain_next_coin_id: uuid          -- For chain coins (links to next coin)
wager_amount:      decimal        -- For player coins / wager modes
placed_by_outlaw:  boolean        -- Tracks outlaw-placed coins
decoy:             boolean        -- Is this a phantom-placed decoy?
expires_at:        timestamp      -- For time-limited coins (time bombs, saddlebag)
```

### Integration with Existing Services

| Existing Service | PvP Extension |
|-----------------|---------------|
| `CollectionService` | Pre-collection check: route to handler by `interaction_type` (value → wallet, bomb → deduct, message → display) |
| `CoinManager` | New `HuntMode` values: `StagecoachRun`, `OutlawMode`, `BecomeTheTreasure`, `DeadMansHand` |
| `FindLimitService` | Outlaw abilities gated by tier — Cabin Boys can't go Outlaw, Captains+ can |
| `WalletService` | New "saddlebag" state for un-extracted coins (between `collected` and `confirmed`) |
| `GasService` | Gas Leak coins interact with existing gas consumption logic |
| `HapticService` | Different vibration patterns for hostile coin detection vs. normal proximity |
| `CoinApiService` | New endpoints: `/coins/place-trap`, `/coins/inspect`, `/player/go-outlaw`, `/player/become-treasure` |
| AI Spawn Governor | Adjusts hostile/friendly coin ratios per zone |

---

## 📊 Implementation Priority

| Priority | Feature | Why | Phase |
|----------|---------|-----|-------|
| **1** | Bomb + Gas Leak + Mimic coin types | Core PvP tension on every collection | Phase 2 |
| **2** | Trail Markers (async messages) | Community layer, low-risk, high engagement | Phase 2 |
| **3** | Secret Message + Bottle coins | Social connection, asynchronous gifting | Phase 2 |
| **4** | Stagecoach Run (extraction mode) | Highest-excitement PvP mode | Phase 2-3 |
| **5** | Outlaw Mode (going rogue) | Creates dynamic zone-level PvP events | Phase 3 |
| **6** | Dead Man's Hand (wager matches) | Revenue-generating competitive mode | Phase 3 |
| **7** | Become the Treasure (player-as-coin) | Most innovative mode, requires safety infrastructure | Phase 3 |
| **8** | Sheriff (reactive bounty hunting) | Balances Outlaw mode, creates ecosystem of roles | Phase 3 |
| **9** | Posse Heist (guild PvP) | Requires guild system to be built first | Phase 3-4 |
| **10** | Shadow Realm (parallel world layer) | Requires all other systems stable first | Phase 4 |
| **11** | Phantom (decoy creator) | Advanced deception mechanic | Phase 4 |
| **12** | Mystery Hunts (AI investigations) | Requires mature AI Game Master | Phase 4-5 |

---

## 🔗 Related Documents

- [Game Mechanics Design](./game-mechanics-design.md) — Perks, streaks, contracts, battle pass, competitive systems
- [AI Integration Plan](./AI-integration.md) — The AI architecture powering all PvP balancing and events
- [Coins & Collection](./coins-and-collection.md) — Base coin mechanics and visual design
- [Treasure Hunt Types](./treasure-hunt-types.md) — Hunt configurations
- [Safety & Legal Research](./safety-and-legal-research.md) — Legal considerations for wager/PvP modes
- [Economy & Currency](./economy-and-currency.md) — BBG, gas, find limits
- [Social Features](./social-features.md) — Friends, guilds, leaderboards

---

*"Every coin tells a story. Some tell you where the gold is. Some tell you where the trap is. And some... well, some ARE the trap."* 🤠💣💰
