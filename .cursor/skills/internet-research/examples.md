# Internet Research Examples

Real-world examples of research scenarios and how to handle them.

---

## Example 1: Researching Pokémon GO Architecture

### User Request
"Research how Pokémon GO is built"

### Search Execution
```
Parallel searches run:
1. "Pokémon GO technical architecture how it was built Unity Niantic 2024"
2. "Niantic Lightship AR SDK Unity tutorial location-based game"
3. "GitHub location-based AR game Unity open source Pokemon GO clone"
4. "GDC talk Niantic Pokemon GO backend infrastructure"
```

### Key Findings Discovered

**Technology Stack:**
| Layer | Technology |
|-------|------------|
| Game Engine | Unity |
| Cloud | Google Cloud Platform |
| Database | Cloud Spanner (~5,000 nodes) |
| Containers | Google Kubernetes Engine |
| AR SDK | Niantic Lightship ARDK |

**Open Source Examples Found:**
- `MapzenGo` - Pokemon GO clone with OpenStreetMap
- `niantic-lightship/ardk-samples` - Official Niantic samples
- `BinGo` - Location-based AR with ARCore + Mapbox

**Resources Located:**
- Official SDK: https://lightship.dev/docs/ardk/
- GDC Talk: "Creation of Planet-Scale Shared Augmented Realities"
- Google Cloud Case Study: How Pokémon GO scales to millions

---

## Example 2: Finding AR Frameworks

### User Request
"What AR SDK should I use for a location-based Unity game?"

### Search Execution
```
Parallel searches:
1. "AR SDK Unity location-based comparison 2024"
2. "Niantic Lightship vs ARCore vs ARKit Unity"
3. "Unity AR Foundation location GPS integration"
4. "best AR framework mobile game 2024"
```

### Findings Format

**Framework Comparison:**
| SDK | Pros | Cons | Best For |
|-----|------|------|----------|
| Niantic Lightship | Built for location games, VPS | Requires API key | Pokemon GO-style games |
| AR Foundation | Unity native, cross-platform | No location features built-in | General AR |
| Mapbox | Great maps, Unity SDK | Separate from AR | Map-heavy apps |

**Recommendation:** Niantic Lightship for location-based AR games specifically, as it's purpose-built for this use case.

---

## Example 3: Backend Architecture Research

### User Request
"How do multiplayer mobile games handle real-time sync?"

### Search Execution
```
Parallel searches:
1. "multiplayer mobile game backend architecture real-time"
2. "mobile game server sync netcode patterns"
3. "GitHub multiplayer game server Unity"
4. "GDC multiplayer networking mobile games"
```

### Pattern Discovered

**Common Architecture:**
```
Client (Unity) 
    ↓ WebSocket/UDP
Game Server (stateful, handles game logic)
    ↓ 
Database (player state, persistent data)
    ↓
Match Service (lobbies, matchmaking)
```

**Key Technologies:**
- **Photon** - Popular Unity multiplayer
- **Mirror** - Open source Unity networking
- **PlayFab** - Microsoft's game backend
- **Nakama** - Open source game server

---

## Example 4: UI/UX Pattern Research

### User Request
"How do treasure hunt apps show nearby items?"

### Search Execution
```
Parallel searches:
1. "treasure hunt app UI nearby items radar"
2. "location-based game UI patterns"
3. "Pokemon GO nearby tracker UI design"
4. "mobile game proximity indicator UX"
```

### UI Patterns Found

**Common Approaches:**
1. **Radar View** - Circular display with dots for nearby items
2. **List View** - Sorted by distance with icons
3. **AR Overlay** - Directional indicators on camera view
4. **Map Markers** - Traditional map pins

**Best Practice:** Combine radar (quick glance) with list (detailed info) and let users switch between views.

---

## Example 5: Handling Niche Topics

### User Request
"Research anti-GPS-spoofing techniques for mobile games"

### Initial Search (Limited Results)
```
"anti GPS spoofing mobile game detection" → Few useful results
```

### Expanded Search Strategy
```
1. "Pokemon GO ban spoof detection how" → Player discussions reveal methods
2. "mobile game location verification techniques" → Technical approaches
3. "GPS spoofing detection Android iOS" → Platform-specific APIs
4. "site:stackoverflow.com detect mock location android" → Implementation details
```

### Synthesized Findings

**Detection Layers:**
1. **API Level**: Check for mock location providers (Android)
2. **Behavioral**: Track impossible movement speeds
3. **Cross-reference**: Compare GPS with cell tower/WiFi location
4. **Server-side**: Analyze patterns across player base

---

## Search Query Evolution

Sometimes initial queries need refinement:

### Too Broad
❌ "how to make AR game" → Too many generic results

### Better
✅ "Unity AR Foundation location-based game tutorial GPS" → Specific tech stack

### Too Narrow  
❌ "ARCoinPositioner Unity 6 GPS bearing calculation" → Too specific, no results

### Better
✅ "Unity AR object placement GPS coordinates bearing" → Searchable concepts

---

## When to Stop Researching

✅ **Stop when you have:**
- Clear understanding of the architecture/pattern
- 2-3 code examples to reference
- Official documentation located
- Confidence to make a recommendation

❌ **Keep going if:**
- Only found outdated information (pre-2022)
- No code examples found
- Conflicting approaches with no clear winner
- User asked for comprehensive comparison
