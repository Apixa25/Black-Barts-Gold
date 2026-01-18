# 🪙 Coins & Collection

Everything about how coins look, behave, and get collected in Black Bart's Gold.

---

## 🎨 Visual Design

### Style
- **Era**: 1875 gold doubloon / pirate treasure aesthetic
- **Material**: Gold-stamped appearance
- **Shape**: Classic round coin with raised edge
- **Condition**: Weathered but valuable looking

### Coin Faces

| Side | Default | With Sponsor |
|------|---------|--------------|
| **Front** | Black Bart logo | Black Bart logo |
| **Back** | Black Bart logo | Sponsor logo |

- Every coin shows Black Bart on at least one side
- Users hiding coins can add their **own logo** to the back
- Sponsors get their logo on the back for branded hunts
- Advertising value: Logo displayed prominently during collection animation

### Size Scaling in AR

| Distance | Appearance |
|----------|------------|
| Far (100+ ft) | Tiny glint/sparkle |
| Medium (50-100 ft) | Small visible coin |
| Near (10-50 ft) | Medium coin, details visible |
| Collection range | Large, fully detailed |

**Key UX**: Coins grow larger as you approach, creating anticipation.

---

## 🎯 Two Coin Type Visuals

Coins MUST look visually distinct based on their type:

### Fixed Value Coins
- Value is known and displayed
- Creates races - players compete for known prizes
- **Visual style**: [TBD - perhaps solid/stable appearance]
- Shows denomination clearly

### Pool/Slot Machine Coins (Mystery)
- Value unknown until collected
- Could pay big or small
- **Visual style**: [TBD - perhaps shimmering/mysterious glow]
- Shows "?" or mystery indicator instead of value

### Why Visual Distinction Matters
- Players can choose their strategy
- Fixed coins = guaranteed race for known value
- Slot coins = gamble for potential big win
- Adds depth to gameplay decisions

---

## 🌈 Gold/Silver/Bronze System

When **multi-find is enabled**, the first 3 players to find a coin get different rewards:

### Visual Changes
| Finder | Coin Color | Value |
|--------|------------|-------|
| 1st | 🥇 Gold | 100% of face value |
| 2nd | 🥈 Silver | 50% of face value |
| 3rd | 🥉 Bronze | 10% of face value |
| 4th+ | Coin disappears | N/A |

### Example: $10 Coin
| Finder | Gets |
|--------|------|
| 1st | $10.00 (gold) |
| 2nd | $5.00 (silver) |
| 3rd | $1.00 (bronze) |
| Total distributed | $16.00 |

**Note**: This means multi-find coins are worth 160% of face value total. The extra 60% comes from the hider's contribution (they pay $10, system distributes $16 worth of value).

### Alternative: Value Stays Same, Prestige Different
If we don't want extra value:
| Finder | Gets | Visual |
|--------|------|--------|
| 1st | $10 | Gold |
| 2nd | $10 | Silver |
| 3rd | $10 | Bronze |

*(Needs decision on which model to use)*

---

## 📍 Coin Placement

### Who Can Hide Coins
| Actor | Method | Logo |
|-------|--------|------|
| **System** | Automatic distribution | Black Bart |
| **Users** | Manual placement on map | Custom or Black Bart |
| **Sponsors** | Admin interface (Phase 1) / Self-service (Phase 2) | Sponsor logo |

### Hiding Rules
- **No minimum distance** between coins (blanket an area with 100 coins if you want!)
- **No placement on water** (ocean, lakes, rivers)
- **Banned zones**: TBD - see [Safety & Legal Research](./safety-and-legal-research.md)
- **Private property**: Currently allowed (enables backyard Easter egg hunts)

### Coin Density
- Users can see other hunters going after the same coin
- High-density areas create exciting competition
- No artificial limits on how many coins per location

---

## 🎯 Collection Mechanics

### Requirements to Collect
1. ✅ Be within GPS range (~10-30 feet, device-dependent)
2. ✅ Have coin visible in Prize Finder AR view
3. ✅ Center targeting crosshairs on coin
4. ✅ Press collect button
5. ✅ Be below your find limit OR coin is within your limit

### Collection Range
- As close as device GPS allows
- Estimated: 10-30 feet depending on hardware
- Same standard as Pokémon GO uses
- Cannot collect from outside this range even if visible

---

## ✨ Collection Animation

When a coin is successfully collected:

### Visual Sequence
1. Coin **flies toward the camera** from its AR position
2. Coin **spins** showing both sides (front and back logos)
3. Coin **grows large** filling significant screen space
4. Value display appears
5. Coin **shrinks into wallet** animation

### Audio
- **Black Bart voice line** plays congratulating the hunter
- Different voice lines based on:
  - Coin denomination
  - Coin difficulty
  - Hunt type
  - Random variety

**Phase 1**: Single standard congratulation
**Phase 2**: ~10 different voice lines with variation

### Sample Voice Lines (Future)
- "Arrr! Fine treasure ye've found!"
- "Black Bart's gold finds a worthy hunter!"
- "That be a hefty doubloon, matey!"
- "Ye've got the eyes of a true treasure hunter!"

---

## ⏳ Coin Lifespan

### Standard Coins
- Exist at location **until found**
- No automatic expiration
- Can sit for months/years

### Recycled Coins
- If no players nearby for ~2 hours, coin returns to communal pool
- Re-distributed to areas with active players
- See [Dynamic Coin Distribution](./dynamic-coin-distribution.md)

### Mythical Coins
- High-value coins in difficult locations
- **Never auto-expire**
- Admin can manually retrieve if needed
- Create legendary challenges (e.g., $2500 on Great Wall of China)

---

## 🔄 Coin Retrieval (Unfound Coins)

When a user hides a coin and wants it back:

### Rules
- **Must physically go to the coin location**
- Cannot remotely retrieve
- Same collection mechanics as finding
- No penalty for retrieving your own coin

### Why This Design?
- Prevents "hide, change mind, retrieve" spam
- Encourages thoughtful placement
- Creates real cost to hiding (your time to retrieve)

---

## 📊 Coin States

```
┌─────────────┐
│   HIDDEN    │ ← Placed by user/system/sponsor
└─────────────┘
       ↓
┌─────────────┐
│   VISIBLE   │ ← In a player's grid, can be seen in AR
└─────────────┘
       ↓
┌─────────────┐
│  COLLECTED  │ ← Player pressed collect, in pending status
└─────────────┘
       ↓
┌─────────────┐
│  CONFIRMED  │ ← 24 hours passed, verified, in wallet
└─────────────┘
```

---

## 🏷️ Coin Metadata

Each coin stores:
```
{
  id: unique identifier
  coin_type: "fixed" or "pool"           // NEW: distinguishes the two types
  value: amount in BBG or null           // null for pool coins (determined at collection)
  contribution: amount hider put in      // for pool coins, tracks what was contributed
  location: GPS coordinates
  hider: user ID or "system" or sponsor ID
  hidden_at: timestamp
  logo_front: image URL (always Black Bart)
  logo_back: image URL (custom or Black Bart)
  hunt_type: reference to hunt configuration
  multi_find: boolean
  finds_remaining: 3, 2, 1, or null
  current_tier: gold, silver, bronze, or null
}
```

### Key Difference
- **Fixed coins**: `value` is set, `contribution` equals `value`
- **Pool coins**: `value` is null until collected, `contribution` tracks hider's input

---

## 🔗 Related Documents

- [Prize Finder Details](./prize-finder-details.md)
- [Economy & Currency](./economy-and-currency.md)
- [Treasure Hunt Types](./treasure-hunt-types.md)
- [Dynamic Coin Distribution](./dynamic-coin-distribution.md)
