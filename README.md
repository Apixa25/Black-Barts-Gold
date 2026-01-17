# 🏴‍☠️ Black Bart's Gold

**An AR Treasure Hunting Mobile Game**

Hunt for virtual coins with real cryptocurrency value in the real world using augmented reality!

## 🎮 About

Black Bart's Gold is an immersive AR treasure hunting game where players explore their physical environment to discover and collect virtual gold coins. Built with Unity and AR Foundation for a production-quality experience on both Android and iOS.

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| **Game Engine** | Unity 2022.3 LTS |
| **AR Framework** | AR Foundation 5.x |
| **Android AR** | ARCore XR Plugin |
| **iOS AR** | ARKit XR Plugin |
| **Language** | C# |
| **Backend** | TBD (Firebase / Custom) |

## 🎯 Core Features

- **AR Treasure Hunt** - Find and collect 3D gold coins in augmented reality
- **GPS Integration** - Coins spawn at real-world locations
- **Economy System** - Gas tank mechanics, BBG balance, parked coins
- **Cross-Platform** - Android and iOS from a single codebase

## 🏗️ Project Structure

```
Black-Barts-Gold/
├── Assets/
│   ├── Scripts/        # C# game logic
│   ├── Scenes/         # Unity scenes
│   ├── Prefabs/        # Reusable game objects
│   ├── Materials/      # Visual materials
│   ├── Models/         # 3D models (coins, etc.)
│   ├── Audio/          # Sound effects & music
│   └── UI/             # UI sprites and assets
├── Docs/
│   ├── project-vision.md
│   ├── DEVELOPMENT-LOG.md
│   └── PROMPT-GUIDE.md
├── Packages/           # Unity package manifest
└── ProjectSettings/    # Unity project settings
```

## 🚀 Getting Started

### Prerequisites

1. **Unity Hub** - [Download here](https://unity.com/download)
2. **Unity 2022.3 LTS** - Install via Unity Hub
3. **Android Build Support** - Add via Unity Hub modules
4. **iOS Build Support** - Add via Unity Hub modules (for Mac)

### Setup

1. Clone this repository
2. Open Unity Hub → Add → Select this folder
3. Open the project in Unity
4. Build and run on your device

## 📖 Documentation

- [Project Vision](Docs/project-vision.md) - Full project overview and design
- [Development Log](Docs/DEVELOPMENT-LOG.md) - Sprint progress and history
- [Prompt Guide](Docs/PROMPT-GUIDE.md) - AI development assistance guide

## 🎨 Design Theme

- **Primary Color**: Gold (#FFD700)
- **Secondary Color**: Deep Sea Blue (#1A365D)
- **Accent Color**: Pirate Red (#8B0000)
- **Theme**: Pirate/Nautical treasure hunting

## 📱 Supported Devices

### Android
- ARCore compatible devices
- Android 7.0+ (API 24+)
- Tested on: OnePlus 9 Pro

### iOS
- ARKit compatible devices
- iOS 11.0+
- iPhone 6s and newer

## 📄 License

[TBD]

## 🙏 Acknowledgments

- Migrated from React Native + ViroReact to Unity for production-quality AR
- Inspired by real-world treasure hunting and geocaching
- Built with ❤️ for pirates everywhere

---

*"X marks the spot!" 🗺️*
