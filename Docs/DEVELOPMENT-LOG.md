# 🏴‍☠️ Black Bart's Gold - Development Log

> **Purpose**: This log helps AI assistants (and humans!) quickly understand what has been built, key decisions made, and patterns established. Read this at the start of new sessions.

---

## 📋 Quick Reference

| Item | Value |
|------|-------|
| **Project Path** | `C:\Users\Admin\Black-Barts-Gold` |
| **Repository** | https://github.com/Apixa25/Black-Barts-Gold.git |
| **Engine** | Unity 6 (6000.3.4f1 LTS) |
| **Current Sprint** | Sprint 0 Complete ✅ → Ready for Sprint 1 |
| **Current Status** | AR Foundation Working on Device! 🎉 |
| **Test Device** | OnePlus 9 Pro (Android, ARM64, ARCore) |
| **Last Updated** | January 17, 2026 |

---

## 🎯 Project Overview

**Black Bart's Gold** is an AR treasure hunting mobile app where players discover virtual coins with real Bitcoin value hidden in real-world locations.

### Why Unity?

This project was **migrated from React Native + ViroReact** due to:
1. ViroReact library instability with React Native 0.81+
2. Fabric architecture incompatibility (ClassCastException crashes on AR exit)
3. Limited community support for ViroReact
4. Need for production-quality AR at scale (millions of users)

**Unity + AR Foundation** was chosen because:
- Industry standard (Pokémon Go, Harry Potter: Wizards Unite use Unity)
- Native ARCore/ARKit performance
- Cross-platform from single codebase
- Massive community and support
- Asset Store ecosystem

### Core Mechanics
- **Gas System**: $10 = 30 days of play (~$0.33/day consumed)
- **Find Limits**: Can only find coins ≤ your limit; hide bigger coins to unlock bigger finds
- **Default Limit**: $1.00 (hide $5 coin → unlock $5 finds)

### Key Files to Read First
1. `project-vision.md` - Full project philosophy and tech decisions
2. `BUILD-GUIDE.md` - Step-by-step sprint prompts for Unity (8 sprints!)
3. This file - Current progress and patterns

---

## 🛠️ Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| **Game Engine** | Unity | 6 (6000.3.4f1 LTS) |
| **AR Framework** | AR Foundation | 5.x |
| **Android AR** | Google ARCore XR Plugin | Latest |
| **iOS AR** | Apple ARKit XR Plugin | Latest |
| **Language** | C# | .NET Standard 2.1 |
| **Backend** | TBD | Node.js/Express or Firebase |

---

## 📁 Project Structure

```
C:\Users\Admin\Black-Barts-Gold\
├── Docs/                    # Documentation
│   ├── BUILD-GUIDE.md       # Unity sprint prompts (8 sprints)
│   ├── DEVELOPMENT-LOG.md   # This file
│   ├── project-vision.md    # Technical overview
│   ├── PROMPT-GUIDE.md      # AI assistant templates
│   └── [10 business docs]   # Economy, coins, hunts, etc.
│
├── BlackBartsGold/          # Unity Project
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   └── ARTest.unity # Working AR test scene ✅
│   │   └── [Materials, etc.]
│   ├── Packages/
│   │   └── manifest.json    # AR Foundation, ARCore, ARKit
│   └── ProjectSettings/
│
├── README.md
└── .gitignore
```

---

## 🎨 Design System

### Colors (Pirate Theme)
```csharp
public static class Colors
{
    // Primary
    public static Color Gold = new Color(1f, 0.84f, 0f);        // #FFD700
    public static Color DeepSeaBlue = new Color(0.1f, 0.21f, 0.36f); // #1A365D
    public static Color PirateRed = new Color(0.55f, 0f, 0f);   // #8B0000
    
    // Secondary
    public static Color Parchment = new Color(0.96f, 0.9f, 0.83f);  // #F5E6D3
    public static Color DarkBrown = new Color(0.24f, 0.16f, 0.08f); // #3D2914
    
    // Coins
    public static Color Silver = new Color(0.75f, 0.75f, 0.75f); // #C0C0C0
    public static Color Bronze = new Color(0.8f, 0.5f, 0.2f);    // #CD7F32
    
    // Status
    public static Color Success = new Color(0.29f, 0.87f, 0.5f); // #4ADE80
    public static Color Warning = new Color(0.98f, 0.75f, 0.14f); // #FBBF24
    public static Color Error = new Color(0.94f, 0.27f, 0.27f);  // #EF4444
}
```

### Find Limit Tiers
| Tier | Limit | Name |
|------|-------|------|
| 1 | $1.00 | Cabin Boy |
| 2 | $5.00 | Deck Hand |
| 3 | $10.00 | Treasure Hunter |
| 4 | $25.00 | Captain |
| 5 | $50.00 | Pirate Legend |
| 6 | $100.00+ | King of Pirates |

---

## ✅ Completed Work

### 🎉 January 17, 2026 - MAJOR MILESTONE: First AR Test Success!

#### Sprint 0: Foundation Setup - COMPLETE ✅

**Environment Setup:**
- [x] Unity Hub installed
- [x] Unity 6 (6000.3.4f1 LTS) installed
- [x] Android Build Support module installed
- [x] Android SDK & NDK configured (Unity bundled)
- [x] Git repository created and pushed

**AR Foundation Setup:**
- [x] AR Foundation 5.x package installed
- [x] Google ARCore XR Plugin installed
- [x] Apple ARKit XR Plugin installed
- [x] XR Plug-in Management configured (ARCore enabled for Android)

**Android Build Configuration:**
- [x] Platform switched to Android
- [x] Package name: `com.blackbart.gold`
- [x] Minimum API Level: **Android 10.0 (API 29)** ⚠️ Required for ARCore+Vulkan
- [x] Scripting Backend: IL2CPP
- [x] Target Architecture: ARM64

**AR Test Scene:**
- [x] Created `ARTest.unity` scene
- [x] Added AR Session
- [x] Added XR Origin (AR) with camera
- [x] Added test cube (gold colored, position 0,0,3)
- [x] Created gold material

**Build & Deploy:**
- [x] Fixed Gradle build issue (set User Home to `C:\gradle-home`)
- [x] Successfully built APK
- [x] Deployed to OnePlus 9 Pro
- [x] **AR WORKING!** Golden cube visible in real-world AR! 🎉

**Documentation:**
- [x] All 13 documentation files in place
- [x] BUILD-GUIDE.md completely rewritten for Unity (8 sprints)
- [x] DEVELOPMENT-LOG.md updated

---

## 🔧 Issues Encountered & Solutions

### Issue 1: ARCore API Level Requirement
**Error:** `ARCore Required apps using Vulkan require a minimum SDK version of AndroidApiLevel29`

**Solution:** Changed Minimum API Level from 24 to **29** in Player Settings.

**Location:** Edit → Project Settings → Player → Android → Other Settings → Minimum API Level

---

### Issue 2: Gradle Build Failed
**Error:** `CommandInvocationFailure: Gradle build failed`

**Solution:** Set Gradle User Home to a short path to avoid Windows long path issues.

**Location:** Edit → Preferences → External Tools → Gradle → User Home

**Value:** `C:\gradle-home`

**Note:** This is the same fix we used in the React Native project! Windows + Gradle = path problems.

---

## 📌 Key Patterns & Conventions

### Unity Project Location
The Unity project is in a **subfolder**: `BlackBartsGold/` within the repo.
```
C:\Users\Admin\Black-Barts-Gold\BlackBartsGold\  ← Unity project root
```

### File Naming (Unity Standard)
- Scripts: `PascalCase.cs`
- Scenes: `PascalCase.unity`
- Prefabs: `PascalCase.prefab`
- Materials: `PascalCase.mat`

### Build Settings Reminder
- **Minimum API:** 29 (Android 10) - Required for ARCore + Vulkan
- **Gradle User Home:** `C:\gradle-home` - Fixes Windows build issues

### User Preferences (from project-vision.md)
- ✅ Include file paths in code blocks
- ✅ Long, clear explanations
- ✅ Use emojis for engagement 🎯
- ✅ Additive code (don't break existing work)
- ✅ Reference project-vision.md
- ✅ AI handles git commits (user preference!)

---

## 🚀 Next Steps: Sprint 1

### Sprint 1: Unity Project Foundation
Based on `BUILD-GUIDE.md`, next session we will:

1. **Scene Manager & Navigation** (Prompt 1.1)
   - Create GameManager singleton
   - Scene loading system
   - Main scenes: MainMenu, ARHunt, Map, Wallet, Settings

2. **Data Models** (Prompt 1.2)
   - C# classes for: Coin, User, Wallet, Transaction, LocationData
   - Enums for: CoinType, CoinStatus, HuntType, etc.

3. **Player Data & Persistence** (Prompt 1.3)
   - Save/Load system with JSON
   - PlayerPrefs for settings
   - Session persistence

---

## 🎯 Test Device

| Property | Value |
|----------|-------|
| Device | OnePlus 9 Pro |
| OS | Android 11+ |
| Architecture | ARM64 |
| ARCore | Supported & Tested ✅ |

---

## 🚀 Development Commands

### Build Android APK (Unity 6)
```
File → Build and Run
```
Or for just building:
```
File → Build Profiles → Build
```

### View Android Logs
```powershell
adb logcat -s Unity
```

### Connect Device
```powershell
adb devices
```

### Git Commands (handled by AI assistant)
```powershell
cd "C:\Users\Admin\Black-Barts-Gold"
git add -A
git commit -m "Your message"
git push origin main
```

---

## 📝 Important Decisions Made

| Date | Decision | Reason |
|------|----------|--------|
| 2026-01-17 | Migrate to Unity | ViroReact crashes, limited support, not production-ready |
| 2026-01-17 | Unity 6 LTS | Latest stable with best AR Foundation support |
| 2026-01-17 | AR Foundation 5.x | Cross-platform, production-proven |
| 2026-01-17 | Android first | Primary test device (OnePlus 9 Pro) available |
| 2026-01-17 | API Level 29 | Required for ARCore + Vulkan graphics |
| 2026-01-17 | Gradle User Home | `C:\gradle-home` fixes Windows path issues |
| 2026-01-17 | AI handles git | User prefers AI to manage commits |

---

## 🔄 Migration Summary

### From React Native + ViroReact
The previous attempt used:
- React Native 0.81.4
- ViroReact 2.50.1
- TypeScript
- Zustand state management

**Problems encountered:**
- ViroReact crashed on AR exit (`UIManagerModule cannot be cast to Fabric`)
- Fabric architecture incompatibility
- Limited workarounds available
- Small community, slow bug fixes

### To Unity + AR Foundation
New stack provides:
- Production-quality AR (same as Pokémon GO)
- Native performance
- Massive community support
- Cross-platform from single codebase
- No "cheesy workarounds" needed!

---

## 📚 Related Documents

| Document | Description |
|----------|-------------|
| [project-vision.md](./project-vision.md) | Technical vision, architecture |
| [BUILD-GUIDE.md](./BUILD-GUIDE.md) | Sprint prompts for Unity (8 sprints) |
| [PROMPT-GUIDE.md](./PROMPT-GUIDE.md) | AI assistant guide |
| [project-scope.md](./project-scope.md) | Business model, phases |
| [economy-and-currency.md](./economy-and-currency.md) | BBG, gas, find limits |
| [coins-and-collection.md](./coins-and-collection.md) | Coin mechanics |
| [prize-finder-details.md](./prize-finder-details.md) | AR HUD design |
| [treasure-hunt-types.md](./treasure-hunt-types.md) | Hunt modes |
| [user-accounts-security.md](./user-accounts-security.md) | Auth, anti-cheat |
| [social-features.md](./social-features.md) | Friends, leaderboards |
| [admin-dashboard.md](./admin-dashboard.md) | Admin tools |
| [dynamic-coin-distribution.md](./dynamic-coin-distribution.md) | Coin spawning |
| [safety-and-legal-research.md](./safety-and-legal-research.md) | Legal |

---

## 🏆 Milestones

| Date | Milestone | Status |
|------|-----------|--------|
| 2026-01-17 | Unity environment setup | ✅ |
| 2026-01-17 | AR Foundation configured | ✅ |
| 2026-01-17 | First AR build on device | ✅ |
| 2026-01-17 | **FIRST AR OBJECT VISIBLE!** | 🎉✅ |
| TBD | Spinning coin in AR | ⏳ |
| TBD | GPS coin positioning | ⏳ |
| TBD | Coin collection | ⏳ |
| TBD | Full MVP | ⏳ |

---

*Last updated: January 17, 2026 - First successful AR test! 🏴‍☠️*
