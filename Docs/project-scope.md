# 🎯 Project Scope - Black Bart's Gold

## Overview

**Black Bart's Gold** is an augmented reality treasure hunting mobile app where players discover virtual coins with real value (backed by Bitcoin) hidden in real-world locations.

---

## 🎮 Core Concept

Players use their mobile device as a "Prize Finder" viewfinder to see hidden virtual spinning coins overlaid on the real world through their camera. Each coin is denominated in **Black Bart Gold (BBG)**, which converts 1:1 to USD and can be exchanged for Bitcoin.

### The Experience
1. Player opens Prize Finder (AR camera view)
2. Sees virtual gold doubloon-style coins in the real world
3. Walks within range (~10-30 feet based on device GPS limits)
4. Centers targeting crosshairs on the coin
5. Presses button to collect
6. Coin flies to screen, spins showing both sides, Black Bart congratulates them
7. Value added to their wallet

---

## 💰 Business Model

### ⛽ The Gas System (Core Revenue)

Playing the game requires **"gas"** - a subscription fee that is consumed daily.

| Component | Details |
|-----------|---------|
| **Price** | $10 USD per month (only option) |
| **Daily Consumption** | ~$0.33/day (1/30th of $10) |
| **Gas Charged** | At midnight daily |
| **No Gas = No Play** | Prize Finder turns off, "Buy More Gas" screen appears |

### 💎 How Money Flows

```
User pays $10
     ↓
$9 worth of coins distributed near them (dynamic cloud system)
$1 is pure gas fee (our revenue)
     ↓
User finds coins OR other players find their hidden coins
     ↓
All coins eventually consumed as gas fees
     ↓
100% of money returns to us over time
```

### 🔑 The Finding Limit System

Players can only find coins up to a certain value based on what they've hidden:

| Player Action | Max Find Limit |
|---------------|----------------|
| Just signed up (no coins hidden) | $1.00 |
| Hidden a $5 coin | $5.00 |
| Hidden a $25 coin | $25.00 |
| Hidden a $100 coin | $100.00 |

- **Limit = Highest single coin ever hidden**
- **Limits never decrease** - once unlocked, permanently unlocked
- Finding a coin above your limit shows: "Sorry, this coin is above your limit" with a hint to hide more

---

## 👥 Target Audience

- **Primary**: Mobile gamers who enjoy location-based games (Pokémon GO players)
- **Secondary**: Treasure hunting enthusiasts, geocaching community
- **Tertiary**: Crypto-curious users interested in earning Bitcoin through gameplay

### Age Considerations
- Initially no age restrictions
- Future: Age verification for sponsor hunts (casino, alcohol sponsors)
- Kid-friendly design: walkable distances, safe locations

---

## 🌍 Geographic Scope

- **International availability from day one**
- **No currency conversion needed** - BBG is the universal in-game currency
- Bitcoin conversion available for cashout (exchange rate at time of conversion)

---

## 📱 Platform Targets

| Platform | Status |
|----------|--------|
| **Android** | Primary (developer uses Android) |
| **iOS** | Day one release (Apple Developer account ready) |

---

## 🚫 What This Is NOT

- ❌ NOT a free-to-play game (pay-to-play only)
- ❌ NOT offline capable (server verification required for all coin finds)
- ❌ NOT a crypto trading platform (simple BBG ↔ Bitcoin conversion only)
- ❌ NO refunds

---

## 📋 Project Phases

### Phase 1 - MVP
- Basic Prize Finder AR view
- Coin hiding and finding
- Gas system
- User accounts (email/password + Gmail)
- Admin dashboard for coin management
- Single hunt type (direct navigation)

### Phase 2 - Enhanced Features
- Multiple hunt types (radar, compass-only, timed release)
- Social features (friends, leaderboards)
- Sponsor hunt system
- Custom logos on coins
- Multiple Black Bart congratulation voice lines

### Phase 3 - Advanced
- Guilds and teams
- Advanced treasure hunt mechanics
- Gambling mechanics (hide for time, get bonus)
- Mythical coin hunts at famous locations

---

## 🔗 Related Documents

- [Prize Finder Details](./prize-finder-details.md)
- [Economy & Currency](./economy-and-currency.md)
- [Treasure Hunt Types](./treasure-hunt-types.md)
- [Coins & Collection](./coins-and-collection.md)
- [User Accounts & Security](./user-accounts-security.md)
- [Social Features](./social-features.md)
- [Admin Dashboard](./admin-dashboard.md)
- [Safety & Legal Research](./safety-and-legal-research.md)
- [Dynamic Coin Distribution](./dynamic-coin-distribution.md)
