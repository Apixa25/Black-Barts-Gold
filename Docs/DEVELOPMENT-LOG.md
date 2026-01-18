# 🏴‍☠️ Black Bart's Gold - Development Log

> **Purpose**: This log helps AI assistants (and humans!) quickly understand what has been built, key decisions made, and patterns established. Read this at the start of new sessions.

---

## 📋 Quick Reference

| Item | Value |
|------|-------|
| **Project Path** | `C:\Users\Admin\Black-Barts-Gold` |
| **Repository** | https://github.com/Apixa25/Black-Barts-Gold.git |
| **Engine** | Unity 6 (6000.3.4f1) |
| **Current Sprint** | Sprint 0 - Foundation Setup |
| **Current Status** | Unity Installed, Documentation Complete |
| **Last Updated** | January 17, 2026 |

---

## 🎯 Project Overview

**Black Bart's Gold** is an AR treasure hunting mobile app where players discover virtual coins with real Bitcoin value hidden in real-world locations.

### Why Unity?

This project was **migrated from React Native + ViroReact** due to:
1. ViroReact library instability with React Native 0.81+
2. Fabric architecture incompatibility (ClassCastException crashes)
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
2. `BUILD-GUIDE.md` - Step-by-step sprint prompts for Unity
3. This file - Current progress and patterns

---

## 🛠️ Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| **Game Engine** | Unity | 6 (6000.3.4f1 LTS) |
| **AR Framework** | AR Foundation | 5.x (to install) |
| **Android AR** | ARCore XR Plugin | Latest |
| **iOS AR** | ARKit XR Plugin | Latest |
| **Language** | C# | .NET Standard 2.1 |
| **Backend** | TBD | Node.js/Express or Firebase |

---

## 📁 Project Structure (Planned)

```
Assets/
├── Scripts/
│   ├── AR/              # AR-specific scripts
│   │   ├── ARSessionManager.cs
│   │   ├── CoinController.cs
│   │   ├── CoinManager.cs
│   │   ├── CoinSpawner.cs
│   │   └── ARRaycastController.cs
│   ├── Core/            # Core game systems
│   │   ├── GameManager.cs
│   │   ├── SceneLoader.cs
│   │   ├── PlayerData.cs
│   │   ├── SaveSystem.cs
│   │   ├── AuthService.cs
│   │   ├── ApiClient.cs
│   │   └── Models/
│   ├── Economy/         # Wallet, gas, transactions
│   │   ├── WalletService.cs
│   │   ├── GasService.cs
│   │   ├── CollectionService.cs
│   │   └── FindLimitService.cs
│   ├── Location/        # GPS, distance calculations
│   │   ├── LocationService.cs
│   │   ├── GeoUtils.cs
│   │   └── HapticService.cs
│   ├── UI/              # UI controllers
│   │   ├── MainMenuController.cs
│   │   ├── WalletController.cs
│   │   ├── MapController.cs
│   │   ├── ARHUD.cs
│   │   └── [HUD Components]
│   └── Utils/           # Helper utilities
├── Scenes/
│   ├── MainMenu.unity
│   ├── ARHunt.unity
│   ├── Map.unity
│   ├── Wallet.unity
│   └── Settings.unity
├── Prefabs/
│   ├── Coins/           # Coin prefabs
│   ├── UI/              # UI prefabs
│   └── Effects/         # Particle effects
├── Materials/
│   └── Coins/           # Coin materials (Gold, Silver, Bronze, Locked)
├── Models/
│   └── Coins/           # 3D coin models
├── Audio/
│   ├── SFX/             # Sound effects
│   └── Voice/           # Black Bart voice lines
├── Textures/
├── Fonts/
└── Resources/           # Runtime-loaded assets
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

### January 17, 2026 - Project Setup

#### Environment Setup ✅
- [x] Unity Hub installed
- [x] Unity 6 (6000.3.4f1 LTS) installed
- [x] Android Build Support module installed
- [x] Android SDK & NDK configured
- [x] Git repository created and pushed

#### Documentation ✅
- [x] README.md - Project overview
- [x] project-vision.md - Technical vision and decisions
- [x] BUILD-GUIDE.md - Sprint-by-sprint prompts for Unity
- [x] DEVELOPMENT-LOG.md - This file
- [x] PROMPT-GUIDE.md - AI assistant prompting guide
- [x] All business docs migrated from previous project:
  - project-scope.md
  - economy-and-currency.md
  - coins-and-collection.md
  - treasure-hunt-types.md
  - prize-finder-details.md
  - user-accounts-security.md
  - social-features.md
  - admin-dashboard.md
  - dynamic-coin-distribution.md
  - safety-and-legal-research.md

---

## 🚧 Current Sprint: 0 - Foundation Setup

### Goals
- [ ] Create Unity project with AR template
- [ ] Install AR Foundation packages
- [ ] Configure Android build settings
- [ ] Create folder structure
- [ ] Basic AR test scene
- [ ] Build and test on OnePlus 9 Pro

### Next Steps
1. Open Unity Hub
2. Create new project in `C:\Users\Admin\Black-Barts-Gold`
3. Install AR Foundation via Package Manager
4. Configure XR Plug-in Management for ARCore

---

## 🎉 Migration Summary

### What We're Keeping (from React Native project)
- ✅ All business logic documentation (economy, coins, hunts, etc.)
- ✅ Game design decisions
- ✅ UI/UX concepts (HUD layout, pirate theme)
- ✅ Backend API design (will reuse or recreate)

### What's New (Unity-specific)
- 🆕 Unity project structure
- 🆕 C# scripts (replacing TypeScript)
- 🆕 AR Foundation (replacing ViroReact)
- 🆕 Unity UI (replacing React Native)
- 🆕 Native platform builds

### What We're NOT Keeping
- ❌ React Native code
- ❌ ViroReact components
- ❌ Node.js mobile services (will use Unity-native)
- ❌ Zustand stores (will use Unity patterns)

---

## 📌 Key Patterns & Conventions

### File Naming (Unity Standard)
- Scripts: `PascalCase.cs`
- Scenes: `PascalCase.unity`
- Prefabs: `PascalCase.prefab`
- Materials: `PascalCase.mat`

### Code Style
```csharp
// Use regions for organization
#region Public Methods
public void Initialize() { }
#endregion

// Use [SerializeField] for inspector-exposed privates
[SerializeField] private GameObject coinPrefab;

// Events with System.Action
public event Action<Coin> OnCoinCollected;

// Singletons with DontDestroyOnLoad
public static GameManager Instance { get; private set; }
```

### User Preferences (from project-vision.md)
- ✅ Include file paths in code blocks
- ✅ Long, clear explanations
- ✅ Use emojis for engagement 🎯
- ✅ Additive code (don't break existing work)
- ✅ Reference project-vision.md
- ✅ Verify code context before suggesting changes

---

## 🐛 Known Issues / TODOs

### Active Issues
*None yet - fresh project!*

### Future Considerations
1. **AR Foundation version** - May need specific version for Unity 6 compatibility
2. **ARCore minimum version** - Need to verify device compatibility
3. **iOS setup** - Will need Xcode and Apple Developer account

---

## 🎯 Test Device

| Property | Value |
|----------|-------|
| Device | OnePlus 9 Pro |
| OS | Android |
| Architecture | ARM64 |
| ARCore | Supported ✅ |

---

## 🚀 Development Commands

### Build Android APK
```
Unity Menu: File → Build Settings → Build
Or: File → Build and Run (with device connected)
```

### View Android Logs
```powershell
adb logcat -s Unity
```

### Connect Device
```powershell
adb devices
# Should show: dcbf7350    device (or similar)
```

---

## 📝 Important Decisions Made

| Date | Decision | Reason |
|------|----------|--------|
| 2026-01-17 | Migrate to Unity | ViroReact crashes, limited support |
| 2026-01-17 | Unity 6 LTS | Latest stable with best AR support |
| 2026-01-17 | AR Foundation | Cross-platform, production-proven |
| 2026-01-17 | Android first | Primary test device available |

---

## 🔄 How to Use This Log

### Starting a New Session
1. Ask the AI to read `project-vision.md` and this file
2. Mention which sprint you want to work on
3. Reference `BUILD-GUIDE.md` for specific prompts

### After Completing Work
1. Ask the AI to update this log with what was built
2. Commit changes to Git
3. Push to GitHub

---

## 📚 Related Documents

| Document | Description |
|----------|-------------|
| [project-vision.md](./project-vision.md) | Technical vision, architecture |
| [BUILD-GUIDE.md](./BUILD-GUIDE.md) | Sprint prompts for Unity |
| [PROMPT-GUIDE.md](./PROMPT-GUIDE.md) | AI assistant guide |
| [project-scope.md](./project-scope.md) | Business model, phases |
| [economy-and-currency.md](./economy-and-currency.md) | BBG, gas, find limits |
| [coins-and-collection.md](./coins-and-collection.md) | Coin mechanics |
| [prize-finder-details.md](./prize-finder-details.md) | AR HUD design |
| [treasure-hunt-types.md](./treasure-hunt-types.md) | Hunt modes |

---

*Last updated by Claude on January 17, 2026*
