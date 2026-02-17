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

### AI Assistant Guidelines

When working on this project:

1. **Always read first**: Start sessions by reading this file and `DEVELOPMENT-LOG.md`
2. **Use BUILD-GUIDE.md**: Follow sprint prompts for structured development
3. **Change safety policy**:
   - Prefer minimal-risk changes
   - Preserve current behavior unless intentionally changing it
   - Delete/refactor when there's clear benefit + verification
4. **Explain clearly**: Long explanations with file paths
5. **Use emojis**: Keep energy high! 🤠
6. **Test on device**: AR doesn't work in Unity Editor

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
| **BUILD-GUIDE.md** | Unity mobile app - sprint-by-sprint prompts |
| **ADMIN-DASHBOARD-BUILD-GUIDE.md** | 🖥️ Web admin dashboard - build guide |
| **DEVELOPMENT-LOG.md** | Progress tracking |
| **PROMPT-GUIDE.md** | AI assistant templates |
| **project-scope.md** | Business model & phases |
| **economy-and-currency.md** | BBG, gas, find limits |
| **coins-and-collection.md** | Coin mechanics |
| **prize-finder-details.md** | AR HUD design |
| **treasure-hunt-types.md** | Hunt configurations |
| **user-accounts-security.md** | Auth & anti-cheat |
| **social-features.md** | Friends & leaderboards |
| **admin-dashboard.md** | Admin tools |
| **dynamic-coin-distribution.md** | Coin spawning |
| **safety-and-legal-research.md** | Legal considerations |

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
