# 🗺️ Treasure Hunt Types

Black Bart's Gold supports **multiple hunt types** with different discovery mechanics. Not all treasure hunts are built the same - variety keeps the game exciting!

---

## 🎯 Discovery Flow Components

Every hunt can mix and match these discovery tools:

| Component | Description |
|-----------|-------------|
| **Map** | Shows general area where coin is located |
| **AR View** | Camera overlay to find exact spot |
| **Radar/Hot-Cold** | Vibration intensity indicates proximity |
| **Compass** | Directional pointer to coin |
| **Distance Meter** | Shows exact meters away |

---

## 📋 Hunt Type Catalog

### Type 1: Direct Navigation (Default)
**Full guidance to the coin**

| Feature | Enabled |
|---------|---------|
| Map location | ✅ Exact marker |
| Distance display | ✅ "X meters away" |
| Compass | ✅ Points to coin |
| AR visibility | ✅ See coin when in range |
| Vibration | ✅ All the way |

**Best for**: New players, casual hunts, quick finds

---

### Type 2: Compass-Only Hunt
**Direction without distance**

| Feature | Enabled |
|---------|---------|
| Map location | ✅ General area (within 1000 yards) |
| Distance display | ❌ Hidden |
| Compass | ✅ Points to coin |
| AR visibility | ✅ See coin when in range |
| Vibration | ✅ Last 100 meters only |

**Experience**: Player sees compass direction, walks toward it. Distance meter disappears within 1000 yards. Must rely on "feel" and vibration to know when getting close.

**Best for**: More challenging hunts, experienced players

---

### Type 3: Pure AR Hunt
**See it to find it**

| Feature | Enabled |
|---------|---------|
| Map location | ❌ No map marker |
| Distance display | ❌ Hidden |
| Compass | ❌ Disabled |
| AR visibility | ✅ See coin when in range |
| Vibration | ✅ All the way |

**Experience**: Player must physically explore the area, scanning with their camera. Coin only appears in AR when within visual range.

**Best for**: Small area hunts, indoor hunts, exploration-focused

---

### Type 4: Radar-Only Hunt
**Vibration guides you**

| Feature | Enabled |
|---------|---------|
| Map location | ✅ General area |
| Distance display | ❌ Hidden |
| Compass | ❌ Disabled |
| AR visibility | ✅ See coin when in range |
| Vibration | ✅ Hot-cold intensity |

**Experience**: "You're getting warmer... warmer... HOT!" - classic treasure hunt feel using vibration patterns.

**Best for**: Nostalgic treasure hunt experience

---

### Type 5: Timed Release Hunt
**Coins appear over time**

| Feature | Enabled |
|---------|---------|
| Map location | ✅ Hunt zone boundary |
| Distance display | ✅ (or configurable) |
| Compass | ✅ (or configurable) |
| AR visibility | ✅ When coin is "live" |
| Vibration | ✅ |

**Mechanic**:
- Hunt zone defined (mall, park, building)
- X coins released over Y minutes
- Example: 15 coins over 15 minutes = 1 coin per minute
- Creates race/competition dynamic

**Best for**: Events, sponsored hunts, indoor venues (malls, buildings)

---

### Type 6: Multi-Find Race
**First come, first served (sort of)**

Uses the **Gold/Silver/Bronze mechanic**:

| Finder | Coin Appearance | Value |
|--------|-----------------|-------|
| 1st | Gold | 100% of face value |
| 2nd | Silver | 50% of face value |
| 3rd | Bronze | 10% of face value |
| 4th+ | Coin disappears | - |

**Example**: $10 coin
- 1st finder: $10 (gold)
- 2nd finder: $5 (silver)
- 3rd finder: $1 (bronze)
- Coin gone after 3rd find

**Experience**: Multiple players can see the same coin. Creates race dynamic. Can see other hunters going after same coin (see [Social Features](./social-features.md)).

---

### Type 7: Single-Find Sequential
**One at a time**

Variant of multi-coin hiding:
- User hides 4 coins at one location
- Each of the first 4 finders gets exactly 1 coin
- Creates fairness - everyone gets equal value

**Example**: User hides 4 BBG at a spot
- Finder 1: 1 BBG
- Finder 2: 1 BBG
- Finder 3: 1 BBG
- Finder 4: 1 BBG
- Location empty after 4 finds

---

## 🏢 Special Venue Hunts

### Indoor Hunts
- Malls, buildings, convention centers
- Uses WiFi/Bluetooth positioning where GPS is weak
- Perfect for Timed Release format
- Weather-independent play

### Event Hunts
- Sponsor-created limited-time hunts
- Custom coin designs with sponsor logos
- Prize pools distributed over event duration
- Leaderboards for the event

---

## ⚙️ Hunt Configuration Options

When creating a hunt (admin or sponsor), these can be toggled:

```
□ Show distance meter
□ Enable compass
□ Show map marker (exact / general area / zone only)
□ Enable vibration (all / last 100m / off)
□ Multi-find enabled (Gold/Silver/Bronze)
□ Timed release (interval: ___ minutes)
□ Max finders: ___
□ Custom coin logo
□ Hunt duration: ___ hours/days
```

---

## 🎮 Hunt Difficulty Spectrum

```
EASY ←──────────────────────────────────────→ HARD

Direct Nav    Compass-Only    Radar    Pure AR
(all guides)  (no distance)   (feel)   (visual only)
```

---

## 📝 Future Hunt Ideas (Brainstorm)

- **Photo Clue Hunt**: Coin location revealed through photo hints
- **Riddle Hunt**: Solve riddles to unlock next waypoint
- **Chain Hunt**: Find coins in sequence, each reveals next location
- **Night Hunt**: Coins only visible after sunset
- **Weather Hunt**: Special coins appear in rain/snow

---

## 🔗 Related Documents

- [Prize Finder Details](./prize-finder-details.md)
- [Coins & Collection](./coins-and-collection.md)
- [Social Features](./social-features.md)
