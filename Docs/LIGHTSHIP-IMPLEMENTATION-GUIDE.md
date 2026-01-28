# Niantic Lightship Implementation Guide for Black Bart's Gold

## Executive Summary

This guide outlines how to integrate Niantic's Lightship technology to make Black Bart's Gold look and feel like Pokémon GO.

### Key Discovery: Two-Part Strategy

| Component | Technology | Status |
|-----------|------------|--------|
| **Map Display** | Mapbox Unity SDK | ✅ You already use Mapbox for admin! |
| **AR Positioning** | Lightship ARDK + VPS | ✅ Active, supported, free tier |
| ~~Lightship Maps~~ | ~~Niantic Maps SDK~~ | ❌ **Sunset October 2025 - Don't use** |

**Bottom Line:** Use Mapbox for maps + Lightship ARDK for AR = Best of both worlds!

---

## Part 1: Lightship ARDK Setup

### Prerequisites

- **Unity Version:** 6000.0.58f2 OR 2022.3.62f2 (LTS only)
- **Current Project:** Unity 6 ✅ (compatible)
- **Account:** Free Niantic developer account

### Installation Steps

#### Step 1: Create Niantic Account
1. Go to https://lightship.dev
2. Create free developer account
3. Create a new project called "Black Barts Gold"
4. Copy your **API Key**

#### Step 2: Install ARDK Package
```
Window > Package Manager > + > Add package from git URL
URL: https://github.com/niantic-lightship/ardk-upm.git
```

#### Step 3: Configure Project
1. Accept Input System activation prompt
2. Go to **Edit > Project Settings > XR Plug-in Management**
3. Enable "Niantic Lightship SDK" for Android
4. Go to **Lightship > Project Validation**
5. Fix any issues shown
6. Add your API Key via **Lightship > Settings**

#### Step 4: Install SharedAR (Optional - for multiplayer)
```
URL: https://github.com/niantic-lightship/sharedar-upm.git
```

---

## Part 2: What Lightship ARDK Gives Us

### VPS (Visual Positioning System)
- **Centimeter-accurate positioning** using computer vision
- Content stays anchored to real-world locations
- Works at 100,000+ public VPS locations worldwide

### Key Features for Black Bart's Gold

| Feature | Benefit |
|---------|---------|
| **Location Drift Mitigation** | Coins stay put when you look away and back |
| **Persistent AR Anchors** | Coins remain in exact spot across sessions |
| **Depth/Occlusion** | Coins hide behind real objects (trees, walls) |
| **Meshing** | Coins can sit ON real surfaces |
| **Semantic Segmentation** | Know if coin is near grass, water, building |

### Sample Code: VPS-Anchored Coin

```csharp
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;

public class VPSCoinPlacer : MonoBehaviour
{
    [SerializeField] private ARLocationManager locationManager;
    [SerializeField] private GameObject coinPrefab;
    
    public void PlaceCoinAtVPSLocation(ARLocation location, Vector3 offset)
    {
        // Create anchor at real-world VPS location
        var anchor = locationManager.CreateAnchor(location, offset);
        
        // Spawn coin at anchor - it will stay there!
        Instantiate(coinPrefab, anchor.transform);
    }
}
```

---

## Part 3: Mapbox for Maps (You Already Have This!)

Since you're using Mapbox for admin, you can use the **same account** for the Unity app.

### Mapbox Unity SDK Installation

```
Window > Package Manager > + > Add package from git URL
URL: https://github.com/mapbox/mapbox-unity-sdk.git
```

### What Mapbox Provides

- Real street maps with buildings
- Satellite imagery
- 3D terrain
- Custom styling (pirate theme! 🏴‍☠️)
- GPS location tracking
- Directions/routing

---

## Part 4: Implementation Phases

### Phase 1: Foundation (1-2 days)
- [ ] Create Niantic developer account
- [ ] Install Lightship ARDK package
- [ ] Verify project validation passes
- [ ] Test basic AR session works

### Phase 2: Replace AR Positioning (2-3 days)
- [ ] Replace ARCoinPositioner with Lightship anchors
- [ ] Implement drift mitigation
- [ ] Test coin stability (no more drifting!)

### Phase 3: Add Mapbox Map (2-3 days)  
- [ ] Install Mapbox Unity SDK
- [ ] Create full-screen map view
- [ ] Show coins as markers on real map
- [ ] Implement tap-to-select coin

### Phase 4: Visual Polish (3-5 days)
- [ ] Enable depth occlusion (coins behind objects)
- [ ] Add surface meshing (coins on ground)
- [ ] Custom coin shaders/effects
- [ ] Pirate-themed map styling

### Phase 5: Advanced Features (Future)
- [ ] VPS locations for special coins
- [ ] Shared AR (see other players)
- [ ] AR photo mode

---

## Part 5: Costs & Limits

### Lightship ARDK
- **Price:** FREE (currently)
- **Limits:** 
  - 50MB per SharedAR session
  - Rate limits on API calls
  - Future pricing TBD (will include free tier)

### Mapbox
- **Free Tier:** 50,000 map loads/month
- **Paid:** Scales with usage
- **Note:** Check if your admin account covers mobile

---

## Part 6: Quick Wins (Do These First!)

### Immediate Improvements Without Full Integration

1. **Enable Lightship's Depth Occlusion**
   - Coins hide behind real objects
   - Makes AR feel more real
   - ~30 minutes to implement

2. **Use Lightship's Improved Tracking**
   - Better than stock ARFoundation
   - Less drift automatically
   - Just install ARDK

3. **Add Meshing for Ground Detection**
   - Coins sit ON surfaces
   - Not floating in air
   - ~1 hour to implement

---

## Resources

### Official Documentation
- Lightship ARDK: https://lightship.dev/docs/ardk/
- Lightship VPS: https://lightship.dev/docs/ardk/features/lightship_vps/
- Mapbox Unity: https://docs.mapbox.com/unity/maps/guides/

### Sample Projects
- Lightship Samples: https://lightship.dev/docs/ardk/sample_projects/
- VPS Location AR: https://github.com/niantic-lightship/ardk-samples

### Tools
- Geospatial Browser: https://lightship.dev/account/geospatial-browser
- Scaniverse App: For creating custom VPS locations

---

## Decision Point

**Option A: Quick Integration (Recommended for MVP)**
- Install Lightship ARDK
- Use improved tracking + depth occlusion
- Keep current simple radar UI
- Timeline: 1-2 days

**Option B: Full Pokémon GO Style**
- Install Lightship ARDK + Mapbox
- Real map with streets/buildings
- VPS anchoring at public locations
- Timeline: 1-2 weeks

---

## Next Steps

1. **Create Niantic account** at https://lightship.dev
2. **Get API key** for Black Bart's Gold project
3. **Tell me which option** (A or B) you want to pursue
4. **I'll implement it** step by step!

🏴‍☠️ Ready to make this app beautiful!
