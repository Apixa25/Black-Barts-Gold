# 🔭 Prize Finder Details

The **Prize Finder** is the core interface of Black Bart's Gold - the AR viewfinder through which players see and collect virtual coins in the real world.

---

## 📱 Main Screen Layout

The Prize Finder is a **sight glass** - a camera view with AR overlays.

```
┌─────────────────────────────────────────────┐
│ [🧭]                           [Find: $5]   │  ← Compass (top-left), Find Limit (top-right)
│                                             │
│                                             │
│                    ⊕                        │  ← Targeting Crosshairs (center)
│                                             │
│                                             │
│                                             │
│  ┌─────┐                            ║█████║ │  ← Mini-map (bottom-left), Gas Meter (right)
│  │ MAP │                            ║█████║ │
│  └─────┘                            ║░░░░░║ │
└─────────────────────────────────────────────┘
```

### UI Elements

| Element | Location | Behavior |
|---------|----------|----------|
| **Compass** | Top-left | Points to selected coin; tap to expand full-screen |
| **Find Limit** | Top-right | Shows max findable value (e.g., "Max: $5") |
| **Crosshairs** | Center | Used to target coins for collection |
| **Mini-map** | Bottom-left | Shows current grid; tap to expand full-screen |
| **Gas Meter** | Right edge | Vertical meter showing remaining playtime |

---

## 🧭 Compass System

### Default View
- Small compass icon in top-left corner
- Arrow points toward currently selected coin
- Updates in real-time as player moves

### Expanded View (tap to expand)
- Full-screen compass mode
- Large directional indicator
- Distance to coin displayed (in some hunt types)

### Phone-Flat Mode
When player holds phone horizontally (parallel to ground):
- **Top edge flashes GREEN** = Moving toward coin
- **Top edge flashes RED** = Moving away from coin
- Provides intuitive directional feedback while walking

---

## ⛽ Gas Meter

### Visual Design
- Vertical meter on right side of screen
- Default scale: 1 month of gameplay
- Fills from bottom to top

### Display Logic
| BBG Balance | Meter Display |
|-------------|---------------|
| 10+ coins | Full (100%) |
| 5 coins | 50% |
| 4 coins | 40% |
| 1 coin | 10% |
| 0 coins | Empty - locked out |

### Low Fuel Warnings
- **15% remaining**: Meter flashes "LOW FUEL"
- **3 days remaining**: Push notification
- **0 remaining**: "Buy More Gas" screen appears, Prize Finder disabled

---

## 🗺️ Map System

The Prize Finder has **two distinct map views**:

### 1. Grid Map (Overview)
- Shows the 3-mile grid system
- Temperature-map style colors indicating activity/density
- Player cannot see individual coins - only grid status
- Tap a grid to enter it (if within range)

### 2. Active Grid Map (Detail)
- Shows the grid the player is currently in
- Displays all visible coins as markers
- Tap a coin marker to select it as your target
- Selected coin gets compass navigation

### Switching Between Views
```
Prize Finder (AR) ←→ Active Grid Map ←→ Grid Overview Map
     [tap mini-map]      [zoom out]        [zoom in]
```

---

## 🎯 Coin Targeting & Collection

### Collection Requirements
1. Be within GPS range (~10-30 feet, device-dependent)
2. Have coin visible in AR view
3. Center crosshairs on coin
4. Press collect button

### Visual Feedback
- Coins appear **small in the distance**
- Coins get **larger as you approach**
- Collection range is as close as device GPS allows

---

## 📳 Vibration Feedback

Vibration is enabled on **all hunt types**:

| Distance | Vibration Pattern |
|----------|-------------------|
| Far | No vibration |
| Getting closer | Gentle pulses |
| Very close | Strong, rapid pulses |
| At collection range | Continuous buzz |

Some hunt types may disable distance display and rely on vibration + compass only (see [Treasure Hunt Types](./treasure-hunt-types.md)).

---

## 📊 Information Display

### Always Visible
- Compass direction to selected coin
- Gas meter
- Find limit
- Mini-map

### Conditionally Visible (based on hunt type)
- Distance in meters ("X meters away")
- Coin value (some hunts hide this)
- Other hunters pursuing same coin

---

## 🔄 Screen States

### Active State
- Full AR view with all overlays
- Coins visible and collectible
- All UI elements functional

### Low Gas State
- Gas meter flashing
- "LOW FUEL" warning overlay
- Still playable until midnight charge

### No Gas State
- Prize Finder disabled
- "Buy More Gas" screen
- Cannot see or collect coins

### Coin Collection State
- Coin flies toward camera
- Coin spins showing both sides (logo + sponsor/default)
- Black Bart congratulation audio plays
- Value added to wallet with animation

---

## 🔗 Related Documents

- [Treasure Hunt Types](./treasure-hunt-types.md)
- [Coins & Collection](./coins-and-collection.md)
- [Economy & Currency](./economy-and-currency.md)
