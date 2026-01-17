# 🏴‍☠️ Black Bart's Gold - Project Vision

## Executive Summary

Black Bart's Gold is an augmented reality mobile game where players hunt for virtual gold coins in the real world. Players use their phone's camera to see and collect 3D coins placed in their physical environment, with the collected coins having real cryptocurrency value.

---

## 🎯 Core Concept

### The Hook
"What if you could walk around your neighborhood and find real money just lying on the ground?"

### Gameplay Loop
1. **Explore** - Walk around the real world with your phone
2. **Discover** - AR coins appear in your camera view when you're close
3. **Collect** - Tap coins to add them to your wallet
4. **Earn** - Collected coins have real BBG (Black Bart's Gold) value

---

## 🛠️ Technology Stack

| Layer | Technology | Why |
|-------|------------|-----|
| **Game Engine** | Unity 2022.3 LTS | Industry standard, proven at scale (Pokémon Go uses Unity) |
| **AR Framework** | AR Foundation 5.x | Cross-platform AR (wraps ARCore + ARKit) |
| **Android AR** | ARCore XR Plugin | Google's AR SDK, native performance |
| **iOS AR** | ARKit XR Plugin | Apple's AR SDK, native performance |
| **Language** | C# | Unity's primary language, robust and performant |
| **Backend** | TBD | Firebase or custom Node.js/Express |
| **Database** | TBD | Firestore or PostgreSQL |

### Why Unity + AR Foundation?

1. **Production Proven** - Pokémon Go, Harry Potter: Wizards Unite use Unity
2. **Cross-Platform** - Single codebase for Android + iOS
3. **Native Performance** - Direct ARCore/ARKit integration
4. **Massive Community** - Extensive documentation, tutorials, support
5. **Asset Ecosystem** - Unity Asset Store for 3D models, effects, etc.

---

## 🎮 Core Features

### Phase 1: AR Treasure Hunt (MVP)
- [ ] AR camera view with coin rendering
- [ ] GPS-based coin spawning
- [ ] Tap-to-collect mechanic
- [ ] Basic coin counter HUD
- [ ] Distance-based coin visibility

### Phase 2: User System
- [ ] User registration/login
- [ ] Profile management
- [ ] Session persistence
- [ ] Cloud save

### Phase 3: Economy System
- [ ] BBG wallet balance
- [ ] Gas tank mechanics ($0.33/day)
- [ ] Parked coins system
- [ ] Transaction history
- [ ] Find limits (hide coins to unlock)

### Phase 4: Map & Navigation
- [ ] 2D map view with coin markers
- [ ] Distance/direction indicators
- [ ] Radar proximity system
- [ ] Coin clustering for dense areas

### Phase 5: Social & Polish
- [ ] Leaderboards
- [ ] Achievements
- [ ] Sound effects & haptics
- [ ] Beautiful 3D coin models
- [ ] Particle effects

---

## 💰 Economy Design

### Gas System
- **Daily Cost**: $0.33 worth of BBG
- **Purpose**: Sustainable ecosystem, prevents exploitation
- **Low Gas Warning**: Alert when < 7 days remaining
- **No Gas**: Cannot hunt until refueled

### Find Limits
| Level | Daily Finds | Requirement |
|-------|-------------|-------------|
| 1 | 10 coins | Default |
| 2 | 25 coins | Hide 5 coins |
| 3 | 50 coins | Hide 15 coins |
| 4 | 100 coins | Hide 30 coins |
| 5 | Unlimited | Hide 50 coins |

### Coin Types
- **Fixed Coins**: Always in same location, respawn after cooldown
- **Random Coins**: Spawn randomly in valid areas
- **Hidden Coins**: Player-placed coins for others to find
- **Event Coins**: Special time-limited coins

---

## 🎨 Visual Design

### Color Palette
| Color | Hex | Usage |
|-------|-----|-------|
| **Gold** | #FFD700 | Primary - coins, highlights, CTAs |
| **Deep Sea Blue** | #1A365D | Secondary - backgrounds, headers |
| **Pirate Red** | #8B0000 | Accent - warnings, important actions |
| **Parchment** | #F5E6D3 | Text backgrounds, cards |
| **Ocean Teal** | #0D7377 | Success states, positive feedback |

### Typography
- **Headers**: Bold, nautical feel
- **Body**: Clean, readable
- **Numbers**: Monospace for values

### UI Theme
- Pirate/nautical aesthetic
- Treasure map textures
- Rope borders
- Compass elements
- Anchor iconography

---

## 📱 Technical Requirements

### Android
- **Minimum**: Android 7.0 (API 24)
- **AR Support**: ARCore compatible device
- **Permissions**: Camera, Location (fine), Internet
- **Storage**: ~100MB

### iOS
- **Minimum**: iOS 11.0
- **AR Support**: ARKit compatible (iPhone 6s+)
- **Permissions**: Camera, Location, Internet
- **Storage**: ~100MB

---

## 🗓️ Development Phases

### Phase 0: Foundation (Week 1)
- Unity project setup
- AR Foundation configuration
- Android build pipeline
- Basic AR test scene

### Phase 1: Core AR (Weeks 2-3)
- GPS integration
- Coin spawning system
- Collection mechanics
- Basic HUD

### Phase 2: User System (Week 4)
- Authentication
- Profile management
- Cloud persistence

### Phase 3: Economy (Week 5)
- Wallet implementation
- Gas mechanics
- Transactions

### Phase 4: Map & Nav (Week 6)
- Map view
- Navigation helpers
- Radar system

### Phase 5: Polish (Weeks 7-8)
- iOS build
- Sound & haptics
- Visual polish
- Testing

---

## 📊 Success Metrics

### Technical
- 60 FPS AR rendering
- < 3 second GPS lock
- < 100ms tap-to-collect response
- 99.9% crash-free sessions

### User Experience
- < 30 seconds from launch to first coin seen
- Clear understanding of mechanics within first session
- Compelling reason to return daily

---

## 🔄 Migration Notes

This project was migrated from React Native + ViroReact due to:
1. ViroReact library instability with React Native 0.81+
2. Fabric architecture incompatibility
3. Limited community support for ViroReact
4. Need for production-quality AR at scale

Unity + AR Foundation was chosen because:
1. Industry standard (Pokémon Go, etc.)
2. Native ARCore/ARKit performance
3. Cross-platform from single codebase
4. Massive community and support

---

## 👥 Team

- **Developer**: Building with AI assistance
- **Platform**: Initially Android (OnePlus 9 Pro), then iOS

---

*"Dead men tell no tales, but their gold still glitters!" 🏴‍☠️*
