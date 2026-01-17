# 🤖 Black Bart's Gold - AI Development Prompt Guide

This guide helps structure conversations with AI assistants for consistent, productive development sessions.

---

## 📋 Starting a New Session

Use this template when beginning a development session:

```
I'm working on Black Bart's Gold, a Unity AR treasure hunting game.

Please review:
- Docs/project-vision.md (project overview)
- Docs/DEVELOPMENT-LOG.md (current progress)

Current Sprint: [Sprint Number] - [Sprint Name]
Today's Goal: [What you want to accomplish]

Let's begin!
```

---

## 🛠️ For New Features

```
Sprint [X.X]: [Feature Name]

Requirements:
- [Requirement 1]
- [Requirement 2]
- [Requirement 3]

Acceptance Criteria:
- [How we know it's done]

Please provide:
1. C# scripts needed
2. Unity setup instructions
3. Any prefabs or assets to create
```

---

## 🐛 For Bug Fixes

```
Bug Report:

Location: [Script/Scene/System name]
Expected Behavior: [What should happen]
Actual Behavior: [What's happening instead]
Error Message: [Copy exact error if any]

Steps to Reproduce:
1. [Step 1]
2. [Step 2]
3. [Step 3]

Please investigate and provide a fix.
```

---

## 📝 For Code Review

```
Please review [ScriptName].cs for:

- C# best practices
- Unity-specific optimizations
- Potential performance issues
- Memory management
- Thread safety (if applicable)
- AR Foundation best practices

Provide suggestions with code examples.
```

---

## 🏗️ For Architecture Decisions

```
Architecture Question:

Context: [What you're trying to build]
Options:
1. [Option A]
2. [Option B]
3. [Option C]

Considerations:
- Performance requirements
- Maintainability
- Scalability
- Unity/AR best practices

What approach do you recommend and why?
```

---

## 📱 For Platform-Specific Issues

### Android Issues
```
Android Issue:

Device: [Device name, e.g., OnePlus 9 Pro]
Android Version: [e.g., Android 13]
Unity Version: [e.g., 2022.3.x]
ARCore Version: [If known]

Issue: [Description]
Logcat Output: [Relevant logs]

Please help diagnose and fix.
```

### iOS Issues
```
iOS Issue:

Device: [Device name]
iOS Version: [e.g., iOS 17]
Unity Version: [e.g., 2022.3.x]
Xcode Version: [e.g., 15.x]

Issue: [Description]
Console Output: [Relevant logs]

Please help diagnose and fix.
```

---

## 📁 Project File Reference

When discussing code, reference these locations:

```
📁 Assets/
├── 📁 Scripts/
│   ├── 📁 AR/           # AR-specific scripts
│   ├── 📁 Core/         # Core game systems
│   ├── 📁 Economy/      # Wallet, gas, transactions
│   ├── 📁 UI/           # UI controllers
│   └── 📁 Utils/        # Helper utilities
├── 📁 Scenes/
│   ├── MainMenu.unity
│   ├── ARHunt.unity
│   ├── Map.unity
│   └── Wallet.unity
├── 📁 Prefabs/
│   ├── 📁 Coins/        # Coin prefabs
│   ├── 📁 UI/           # UI prefabs
│   └── 📁 Effects/      # Particle effects
├── 📁 Materials/
├── 📁 Models/
├── 📁 Audio/
└── 📁 Resources/        # Runtime-loaded assets
```

---

## ✅ Session Checklist

Before ending a session, ensure:

- [ ] All new code is explained
- [ ] DEVELOPMENT-LOG.md is updated
- [ ] Any new files are committed
- [ ] Known issues are documented
- [ ] Next steps are clear

---

## 🎯 Unity-Specific Tips

### For Script Creation
- Always specify the namespace: `namespace BlackBartsGold.Scripts.AR`
- Include `using` statements at the top
- Mention if MonoBehaviour or ScriptableObject
- Specify public vs private vs [SerializeField]

### For Scene Setup
- Describe hierarchy structure
- Specify component settings
- Note any required references
- Mention layer/tag requirements

### For AR Features
- Reference AR Foundation components explicitly
- Specify which XR subsystem is needed
- Note any platform-specific code
- Consider AR session lifecycle

---

## 📚 Useful Commands

### Unity Editor (mentioned in prompts)
- "Create a new C# script called..."
- "Add component X to GameObject Y..."
- "Set up a prefab with..."
- "Configure the AR Session Origin..."

### Build & Test
- "Build for Android..."
- "Deploy to connected device..."
- "Check adb logcat for..."
- "Profile performance of..."

---

## 🏴‍☠️ Project-Specific Terminology

| Term | Meaning |
|------|---------|
| **BBG** | Black Bart's Gold - the in-game currency |
| **Gas** | Daily usage cost ($0.33/day equivalent) |
| **Parked Coins** | Coins stored, not in gas tank |
| **Gas Tank** | Active balance used for daily costs |
| **Find Limit** | Daily maximum coins collectible |
| **Hidden Coin** | Player-placed coin for others |
| **Fixed Coin** | Permanent location coin |
| **Random Coin** | Dynamically spawned coin |

---

*"Consistent prompts lead to consistent treasure!" 🗺️*
