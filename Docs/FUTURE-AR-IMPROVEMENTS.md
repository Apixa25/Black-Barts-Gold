# Future AR Improvements - Black Bart's Gold

**Created:** January 25, 2026  
**Purpose:** Track planned AR features and improvements discovered during development

---

## 🔴 Critical Fixes (In Progress)

### 1. Fix GPS-to-AR Positioning (BROKEN)
- **Issue:** CoinSpawner.cs tries to convert GPS coordinates to AR world space
- **Problem:** AR world space and GPS are different coordinate systems that don't align
- **Solution:** Use compass-relative billboards for distance viewing instead

### 2. Implement Compass-Billboard System
- **Purpose:** Show coins at a distance through the camera
- **How:** Position 2D billboards based on compass bearing, not AR tracking
- **Result:** Coins appear in the correct direction as you turn

### 3. Add Compass Smoothing
- **Issue:** Mini-map dots jump around due to compass jitter
- **Solution:** Apply rolling average to compass readings

---

## 🟡 Recommended Additions

### 4. AROcclusionManager
- **What:** Makes virtual objects hide behind real-world objects
- **Why:** Coins would realistically disappear behind walls, furniture
- **Priority:** High - makes AR look professional

### 5. ARPointCloudManager  
- **What:** Shows feature points being tracked (debug visualization)
- **Why:** Helps debug tracking issues
- **Priority:** Medium - useful for development

### 6. Light Estimation
- **What:** Matches virtual object lighting to real environment
- **Why:** Coins would look more realistic (shadows, brightness)
- **Priority:** Medium

### 7. AREnvironmentProbeManager
- **What:** Creates reflections from real environment
- **Why:** Shiny gold coins would reflect surroundings
- **Priority:** Low - nice-to-have

---

## 🟢 Future Features

### 8. Player Location Sharing
- **Vision:** Players can see other players through the Prize-Finder
- **Implementation:** Use same compass-billboard system as coins
- **Far players:** Floating name tags in correct direction
- **Close players:** Avatar anchored to ground

### 9. Sound Effects
- Coin collection sounds
- Proximity beeps (getting warmer/colder)
- Treasure chest opening sounds

### 10. Haptic Feedback Patterns
- Different vibration patterns for different coin values
- Intensity increases as you get closer
- Celebration vibration on collection

### 11. AR+ Mode (Enhanced AR)
- Like Pokémon GO's AR+ mode
- More realistic placement
- Bonus rewards for using AR mode

### 12. Coin Collection Effects
- Particle effects when collecting
- Coin flies to wallet
- XP/score popup

---

## 📊 AR Architecture Reference

### The Proper AR Coin System

```
┌─────────────────────────────────────────────┐
│         MINI-MAP (Top-right)                │
│  • GPS-based radar                          │
│  • Shows all coins within 50m               │
│  • Compass-relative positioning             │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│         DISTANCE VIEWING (>20m)             │
│  • Compass-relative billboards              │
│  • Float in correct direction               │
│  • NOT anchored to AR space                 │
│  • "Look that way" indicators               │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│         REVEAL MODE (<20m)                  │
│  • "REVEAL COIN" button appears             │
│  • Point camera at ground                   │
│  • Coin anchored to AR plane                │
│  • Stable, collectible                      │
└─────────────────────────────────────────────┘
```

### Key Principle
**GPS is for NAVIGATION (where to go)**  
**AR is for PLACEMENT (where to display)**  
**Never mix GPS coordinates with AR world space!**

---

## 🔧 Components Checklist

### Required (Have)
- [x] ARSession
- [x] XROrigin
- [x] ARCameraManager
- [x] ARCameraBackground
- [x] ARPlaneManager (just added)
- [x] ARRaycastManager (just added)
- [x] ARAnchorManager (just added)

### Recommended (Missing)
- [ ] AROcclusionManager
- [ ] ARPointCloudManager
- [ ] Light Estimation enabled
- [ ] AREnvironmentProbeManager

---

## 📝 Notes

- Pokémon GO uses 2D map for distance, AR only for close encounters
- Ingress shows portals at distance using compass-billboards
- AR tracking and GPS are separate systems - don't try to combine them
- Compass readings are noisy - always smooth them
