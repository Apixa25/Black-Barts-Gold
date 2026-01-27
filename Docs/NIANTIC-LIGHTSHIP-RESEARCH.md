# Niantic Lightship ARDK Research
## Black Bart's Gold - Future AR Enhancement Possibilities

**Research Date**: January 2026  
**Status**: Research complete, implementation pending  
**Priority**: Future enhancement (after core Prize Finder is stable)

---

## Executive Summary

Niantic (creators of Pokémon GO) have released their AR development toolkit called **Lightship ARDK** (now called **Niantic Spatial Platform**). This is the same technology that powers Pokémon GO's AR features. Our research reveals that their Visual Positioning System (VPS) could enable premium "verified location" treasure hunts with centimeter-level accuracy.

**Key Insight**: GPS alone cannot provide convincing AR placement. Niantic explicitly states:
> *"While we're able to use signals like GPS and compass data to place Pokémon on the 2D map, there is far too much error in those signals to place AR content convincingly in the 3D physical world."*

---

## How Pokémon GO Actually Works

### The Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Game Engine | **Unity** | Core game development |
| Cloud Infrastructure | **Google Cloud Platform** | Backend services |
| Database | **Cloud Spanner** (~5,000 nodes) | Game state storage |
| Container Orchestration | **Google Kubernetes Engine** | Microservices |
| AR Positioning | **Visual Positioning System (VPS)** | Centimeter-accurate placement |

### Visual Positioning System (VPS)

VPS is the secret sauce that makes Pokémon appear "fixed in space" rather than floating around:

1. **Pre-scanned Locations**: Players and Niantic scan real-world locations using cameras
2. **3D Maps Created**: Scans are processed into 3D point cloud maps stored in the cloud
3. **Localization**: When a user opens AR, their camera frames are matched against these maps
4. **Anchors**: Once matched, virtual content is anchored to specific 3D points
5. **6DOF Tracking**: Device motion tracking keeps content stable as user moves

### The "Pokémon Playgrounds" Feature (November 2024)

Niantic's latest feature demonstrates the full power of VPS:
- Players can **place Pokémon at real-world locations**
- Other players **see the same Pokémon in the same spot**
- Pokémon **persist across sessions** - come back tomorrow and it's still there
- Achieved through **anchors** that define relationships between physical features and digital content

---

## Niantic Lightship ARDK Features

### Core Capabilities

| Feature | Description | Relevance to Black Bart's Gold |
|---------|-------------|--------------------------------|
| **VPS (Visual Positioning)** | Centimeter-accurate positioning at scanned locations | Premium treasure locations |
| **Depth & Occlusion** | Virtual objects hide behind real objects | Coins hidden behind trees/posts |
| **Meshing** | Real-time 3D mesh of environment | Coins sitting ON surfaces |
| **Semantic Segmentation** | Identifies sky, ground, buildings, etc. (20+ channels) | Context-aware coin placement |
| **Object Detection** | Recognizes 200+ object classes | "Find the coin near the bench" |
| **Shared AR** | Multiplayer AR (up to 10 players) | Cooperative treasure hunts |
| **Navigation Mesh** | AI pathfinding in AR | Animated creatures leading to treasure |

### VPS-Specific Features

- **Public Locations**: Thousands of pre-scanned landmarks worldwide
- **Private Locations**: Scan your own locations for development/testing
- **Geospatial Browser**: Web tool to discover and download location data
- **Coverage API**: Query nearby VPS-activated locations at runtime
- **Mesh Download**: Get 3D mesh of locations for development
- **Remote Authoring**: Place content at locations from your desk

### Unity SDK Requirements

- **Unity 6000.0.58f2** (LTS) ✅ We're on Unity 6!
- **Unity 2022.3.62f2** (LTS alternative)
- Free API key from Niantic developer portal
- Works WITH AR Foundation (extends, doesn't replace)

---

## Sample Projects Available

Niantic provides official samples at: `https://github.com/niantic-lightship/ardk-samples`

### Samples We Should Explore

| Sample | What It Shows | Application for BBG |
|--------|---------------|---------------------|
| **VPS Localization** | Finding and tracking at real locations | Verified treasure spots |
| **Shared AR VPS** | Multiplayer at same location | Group treasure hunts |
| **Depth Display** | Depth buffer visualization | Understanding environment |
| **Occlusion** | Objects behind real surfaces | Hidden coins |
| **Meshing** | Real-time environment mesh | Coins on ground/surfaces |
| **Navigation Mesh** | AI pathfinding | Animated guide creatures |
| **Object Detection** | Recognizing real objects | Contextual placement |
| **Remote Authoring** | Place content from desk | Level design workflow |
| **Cloud Persistence** | Save/load AR content | Persistent treasure locations |

---

## How We Could Use This for Black Bart's Gold

### Tier 1: Premium "Verified Locations" (VPS)

Create special treasure hunts at famous landmarks:

```
┌─────────────────────────────────────────────────┐
│         🏴‍☠️ VERIFIED TREASURE HUNT 🏴‍☠️           │
│                                                 │
│  Location: Golden Gate Bridge Overlook          │
│  Treasure: Legendary Gold Doubloon ($50 value)  │
│  Accuracy: Centimeter-precise                   │
│                                                 │
│  Features:                                      │
│  ✓ Coin hidden behind specific lamp post        │
│  ✓ Visible to all players in same spot          │
│  ✓ Persists until collected                     │
│  ✓ AR effects: shadows, occlusion, particles    │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Tier 2: Enhanced AR Features

Even without VPS, Lightship offers:

- **Better Occlusion**: Coins that hide behind real objects
- **Surface Placement**: Coins that sit ON detected surfaces
- **Semantic Awareness**: Don't place coins in the sky
- **Depth Effects**: Realistic shadows and lighting

### Tier 3: Social Features

- **Shared AR**: Multiple players see same coin
- **Co-op Hunts**: Work together to find treasure
- **AR Photos**: Take photos with found treasure

---

## Implementation Roadmap

### Phase 1: Current (GPS + AR Foundation)
- ✅ Basic GPS-to-AR positioning
- ✅ World-space anchoring fix (using initial compass)
- 🔧 Position locking when close
- 🔧 Billboard/WorldLocked modes

### Phase 2: Enhanced AR (Lightship without VPS)
- Add Niantic Lightship SDK
- Enable depth/occlusion
- Enable meshing for surface placement
- Better visual effects

### Phase 3: VPS Integration
- Register for Niantic developer account
- Explore Geospatial Browser for locations
- Create test scans with Scaniverse app
- Implement VPS-based treasure hunts
- Design "Verified Location" treasure tier

### Phase 4: Social Features
- Shared AR implementation
- Multiplayer treasure hunts
- Persistent world content

---

## Visual Effects Possibilities

### What Lightship Enables

Based on the sample projects, we could achieve:

**Depth & Occlusion**
```
Before: Coin floats in front of everything
After:  Coin hidden behind lamp post, revealed as you walk around
```

**Meshing**
```
Before: Coin at fixed height
After:  Coin resting ON park bench, ON ground, ON table
```

**Semantic Segmentation**
```
Before: Coin might appear in sky
After:  System knows sky vs ground vs building, places appropriately
```

**Object Detection**
```
Possibility: "Find the treasure near the red fire hydrant"
System can identify real-world objects for contextual hints
```

---

## Development Resources

### Official Documentation
- **Main Docs**: https://lightship.dev/docs/ardk/
- **VPS Overview**: https://lightship.dev/docs/ardk/features/lightship_vps/
- **Sample Projects**: https://lightship.dev/docs/ardk/sample_projects/
- **Setup Guide**: https://lightship.dev/docs/ardk/setup/

### Key Tutorials
- **Location AR with Code**: https://lightship.dev/docs/ardk/how-to/vps/location_ar_code/
- **Real World Location AR**: https://lightship.dev/docs/ardk/how-to/vps/real_world_location_ar/
- **VPS Coverage Query**: https://lightship.dev/docs/ardk/features/location_ar_vps_coverage/

### GitHub Repository
```
git clone https://github.com/niantic-lightship/ardk-samples.git
```

### Tools
- **Geospatial Browser**: https://lightship.dev/account/geospatial-browser/
- **Scaniverse App**: For creating private VPS locations
- **Remote Authoring**: Place content without visiting location

---

## Technical Notes

### How VPS Localization Works

```
1. User opens AR at VPS-activated location
   └─> Camera captures frames

2. Frames sent to Niantic cloud
   └─> Matched against pre-built 3D map

3. Match found = localization successful
   └─> Returns 6DOF pose (position + orientation)

4. Anchor created at matched position
   └─> Virtual content attached to anchor

5. Device motion tracking maintains alignment
   └─> Content stays stable as user moves
```

### VPS vs GPS Comparison

| Aspect | GPS Only | GPS + VPS |
|--------|----------|-----------|
| Accuracy | 5-50 meters | 1-10 centimeters |
| Works indoors | No | Sometimes (if scanned) |
| Requires scanning | No | Yes |
| Works everywhere | Yes | Only at scanned locations |
| Multiplayer sync | Approximate | Exact same position |
| Persistence | Session only | Across sessions |

### Integration with AR Foundation

Lightship extends AR Foundation rather than replacing it:

```csharp
// Existing AR Foundation code continues to work
// Lightship adds managers that enhance functionality

// ARLocationManager - for VPS locations
// ARPersistentAnchorManager - for persistent anchors
// ARSemanticSegmentationManager - for scene understanding
// AROcclusionManager enhanced - better occlusion
```

---

## Cost Considerations

### Niantic Lightship Pricing (as of research date)

- **Free Tier**: Available for development and small-scale use
- **API calls**: Check current limits on Niantic developer portal
- **VPS queries**: May have rate limits
- **Commercial use**: Review Niantic's terms of service

### Our Recommendation

Start with free tier for development:
1. Get API key
2. Test with sample projects
3. Scan test locations with Scaniverse
4. Evaluate quality and feasibility
5. Plan production rollout

---

## Questions to Explore

1. **Coverage in our target areas?** - Use Geospatial Browser to check
2. **Private location creation?** - How many can we create?
3. **Offline capability?** - What happens without internet?
4. **Battery impact?** - VPS uses more processing
5. **User experience** - How long does localization take?

---

## Conclusion

Niantic Lightship represents a significant opportunity to elevate Black Bart's Gold from a basic GPS treasure hunt to a truly immersive AR experience. The technology that powers Pokémon GO is now available to us.

**Recommended Next Steps**:
1. ✅ Document research (this file)
2. Create Niantic developer account
3. Download and run sample projects
4. Explore Geospatial Browser for locations in target areas
5. Create test VPS location with Scaniverse
6. Prototype VPS-based treasure placement

---

## References

- Niantic Engineering Blog: "Engineering Pokémon Playgrounds" (November 2024)
- Google Cloud Case Study: "How Pokémon GO scales to millions of requests"
- GDC 2017: "Creation of Planet-Scale Shared Augmented Realities"
- Unity Unite 2016: "Unity Architecture in Pokémon Go"

---

*Document created from research session, January 2026*
*Black Bart's Gold - AR Treasure Hunting Game*
