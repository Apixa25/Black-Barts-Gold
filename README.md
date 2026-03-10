# 🤠 Black Bart's Gold

## An AR Treasure Hunting Mobile Game

Hunt for virtual coins with real cryptocurrency value in the real world using augmented reality!

## 🎮 About

Black Bart's Gold is an immersive AR treasure hunting game where players explore their physical environment to discover and collect virtual gold coins. Built with Unity and AR Foundation for a production-quality experience on both Android and iOS.

### Who is Black Bart?

Black Bart (Charles E. Boles, 1829-1888) was a legendary **Wild West stagecoach robber** - a gentleman bandit known for his politeness and the poems he left at crime scenes. Our mascot reimagines him as a time-traveling treasure hider who spreads wealth through hidden BBG (Black Bart's Gold) coins.

> **Note**: Black Bart was a Wild West outlaw, NOT a pirate! See `Docs/brand-guide.md` for details.

## 🛠️ Technology Stack

### Mobile App (Unity)

| Component | Technology |
| --------- | ---------- |
| **Game Engine** | Unity 6 (6000.x LTS) |
| **AR Framework** | AR Foundation 6.x |
| **Android AR** | ARCore XR Plugin |
| **iOS AR** | ARKit XR Plugin |
| **Language** | C# |

### Admin Dashboard (Web)

| Component | Technology |
| --------- | ---------- |
| **Framework** | Next.js 14+ (App Router) |
| **Language** | TypeScript |
| **Styling** | Tailwind CSS + shadcn/ui |
| **Database** | Supabase (PostgreSQL) |
| **Auth** | Supabase Auth |
| **Hosting** | Vercel |

## 🎯 Core Features

- **AR Treasure Hunt** - Find and collect 3D gold coins in augmented reality
- **GPS Integration** - Coins spawn at real-world locations
- **Economy System** - Gas tank mechanics, BBG balance, parked coins
- **Find Limits** - Hide coins to unlock finding bigger ones
- **Cross-Platform** - Android and iOS from a single codebase
- **Admin Dashboard** - Web-based management tools

## 🏗️ Project Structure (Monorepo)

```text
Black-Barts-Gold/
├── BlackBartsGold/         # Unity mobile app
│   ├── Assets/
│   │   ├── Scripts/        # C# game logic
│   │   ├── Scenes/         # Unity scenes
│   │   ├── Prefabs/        # Reusable game objects
│   │   └── ...
│   └── ProjectSettings/
├── admin-dashboard/        # Next.js web admin
│   ├── src/
│   │   ├── app/           # App Router pages
│   │   ├── components/    # React components
│   │   └── lib/           # Utilities
│   └── package.json
├── Assets/Brand/           # Brand assets (logos, mascot images)
└── Docs/                   # Documentation
    ├── brand-guide.md      # 🤠 Character & brand identity
    ├── project-vision.md   # Project overview
    ├── BUILD-GUIDE.md      # Unity app build guide
    ├── DOCS-POLICY.md      # Docs authority and archive rules
    └── ...
```

## 🚀 Getting Started

### Run Mobile App (Unity)

See [`Docs/BUILD-GUIDE.md`](Docs/BUILD-GUIDE.md) for the complete Unity build guide.

### Run Admin Dashboard (Web)

See [`admin-dashboard/README.md`](admin-dashboard/README.md) for admin dashboard setup and local development.

```bash
cd admin-dashboard
npm install
npm run dev
```

## 📖 Documentation

Start with the [Docs Index](Docs/README.md) for the fastest overview of canonical docs, active references, and archived material.

| Document | Purpose |
| -------- | ------- |
| [Brand Guide](Docs/brand-guide.md) | 🤠 **READ FIRST** - Character & visual identity |
| [Project Vision](Docs/project-vision.md) | Full project overview and design |
| [Build Guide (Unity)](Docs/BUILD-GUIDE.md) | Mobile app development guide |
| [Admin Dashboard README](admin-dashboard/README.md) | Web admin setup and local development |
| [Development Log](Docs/DEVELOPMENT-LOG.md) | Sprint progress and history |

## 🎨 Design Theme

**Wild West + Steampunk** (NOT pirate!)

| Color | Hex | Usage |
| ----- | --- | ----- |
| **Treasure Gold** | #FFD700 | Primary - coins, buttons |
| **Saddle Brown** | #8B4513 | Secondary - headers |
| **Dark Leather** | #3D2914 | Text, backgrounds |
| **Parchment** | #F5E6D3 | Cards, text areas |
| **Fire Orange** | #E25822 | Accents, excitement |
| **Brass** | #B87333 | Steampunk elements |

## 📱 Supported Devices

### Android

- ARCore compatible devices
- Android 7.0+ (API 24+)

### iOS

- ARKit compatible devices
- iOS 11.0+
- iPhone 6s and newer

## 📄 License

[TBD]

## 🙏 Acknowledgments

- Based on the historical Charles E. Boles "Black Bart" (1829-1888)
- Migrated from React Native + ViroReact to Unity for production-quality AR
- Inspired by real-world treasure hunting and geocaching

---

*"X marks the spot, partner!"* 🤠
