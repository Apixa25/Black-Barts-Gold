# Pokémon GO Design Patterns for Location-Based AR Games

## When to Use This Skill
Use this skill when working on:
- GPS/location features
- AR camera and object placement
- Map integration
- Anti-cheat/spoofing prevention
- Battery optimization
- UX for location-based games
- Any feature where "how did Pokémon GO solve this?" is relevant

## Overview
Pokémon GO (Niantic) has been running since 2016 with hundreds of millions of players. They've solved virtually every problem in location-based AR gaming. When in doubt, research and follow their patterns.

---

## 1. GPS-to-AR Coordinate Conversion

### The Problem
GPS gives lat/lng coordinates. AR gives XYZ positions. How do you place virtual objects at real-world locations?

### Pokémon GO Solution: Compass-Aligned AR
```
1. When AR mode starts, capture the device's compass heading
2. GPS bearing (0° = North) is adjusted by subtracting compass heading
3. This aligns AR's +Z axis with the real-world direction user is facing

Example:
- Coin is at GPS bearing 90° (due east of player)
- User was facing 45° (northeast) when AR started
- Adjusted bearing = 90° - 45° = 45°
- Coin appears 45° to the RIGHT of camera forward

Code pattern:
float adjustedBearing = gpsBearing - initialCompassHeading;
float x = distance * Mathf.Sin(adjustedBearing * Mathf.Deg2Rad);
float z = distance * Mathf.Cos(adjustedBearing * Mathf.Deg2Rad);
```

### Key Insight
AR Foundation's +Z is NOT north - it's wherever the camera was pointing when AR started. You MUST adjust for compass heading.

---

## 2. AR Object Visibility

### The Problem
Virtual objects can be invisible due to shaders, scale, position, or camera issues.

### Pokémon GO Solution: Layered Approach
```
1. SHADERS: Use mobile-optimized unlit shaders (not Standard)
   - Unlit/Color or URP/Unlit work reliably on mobile
   - Standard shader can fail silently on some devices

2. SCALE: Objects should be appropriately sized
   - Pokémon are typically 0.5m - 2m tall
   - Use real-world scale (1 Unity unit = 1 meter)
   
3. HEIGHT: Place objects at comfortable viewing height
   - Ground level to eye level (0m - 1.7m)
   - Not above head or below ground

4. CAMERA: Ensure AR camera is tagged "MainCamera"
   - Use FindFirstObjectByType<Camera>() as fallback
   - AR cameras aren't always tagged properly
```

---

## 3. Location Accuracy & Updates

### The Problem
GPS accuracy varies wildly (2m to 100m+). How do you handle this?

### Pokémon GO Solution: Accuracy Tiers
```
1. HIGH ACCURACY REQUIRED (< 10m): Collecting items, catching Pokémon
2. MEDIUM ACCURACY OK (< 50m): Seeing nearby objects on map
3. LOW ACCURACY OK (< 200m): General gameplay, walking distance

Implementation:
- Show accuracy indicator to user
- Gate high-precision actions on accuracy
- Use horizontal accuracy, not just "has location"
- Average multiple readings for stability
```

### Update Frequency
```
- Active gameplay: Update every 1-2 seconds
- Background: Update every 30-60 seconds
- Stationary: Reduce updates to save battery
- Movement threshold: Only recalculate if moved > 5m
```

---

## 4. Map & Radar Integration

### The Problem
How do players find objects they can't see in AR?

### Pokémon GO Solution: Complementary Views
```
1. MAP VIEW (primary): Top-down map showing all nearby objects
   - Player always at center
   - Objects shown as icons with distance
   - Tap to get directions or enter AR

2. AR VIEW (secondary): Only for close interactions
   - Used for catching/collecting (< 50m typically)
   - Shows objects in camera view
   - Has "compass ring" showing off-screen object directions

3. RADAR/NEARBY: List of objects sorted by distance
   - Shows what's nearby even if not visible
   - Helps players navigate to objects
```

---

## 5. Anti-Cheat & Spoofing Prevention

### The Problem
Players can fake GPS location to cheat.

### Pokémon GO Solution: Multi-Layer Detection
```
1. SPEED CHECKS: Flag impossible travel speeds
   - Walking: < 7 km/h, Running: < 20 km/h, Driving: < 100 km/h
   - Teleporting (instant location change) = definite spoof

2. CONSISTENCY CHECKS:
   - Compare GPS with cell tower triangulation
   - Check if location matches IP geolocation (roughly)
   - Verify accelerometer shows actual movement

3. BEHAVIORAL ANALYSIS:
   - Playing 24/7 without breaks
   - Perfect timing on every action
   - Visiting locations in impossible patterns

4. SOFT BANS vs HARD BANS:
   - First offense: Shadow ban (reduced spawns)
   - Repeat offense: Temporary ban
   - Severe/repeated: Permanent ban
```

---

## 6. Battery & Performance Optimization

### The Problem
GPS + AR + networking = dead battery in 1 hour

### Pokémon GO Solution: Adaptive Resource Usage
```
1. AR MODE: Only run AR when user explicitly opens AR view
   - Map view uses much less power
   - AR camera is expensive!

2. GPS THROTTLING:
   - High frequency only when actively playing
   - Reduce to low-power mode when app backgrounded
   - Use "significant location change" APIs when possible

3. NETWORK BATCHING:
   - Don't make API call for every action
   - Batch nearby object queries (500m radius, cache for 30s)
   - Use delta updates, not full refreshes

4. BATTERY SAVER MODE:
   - Dim/turn off screen when phone upside down
   - Reduce GPS frequency
   - Pause non-essential animations
```

---

## 7. Object Spawning & Distribution

### The Problem
How do you distribute objects so gameplay is fair and fun?

### Pokémon GO Solution: Layered Spawning
```
1. FIXED LOCATIONS: PokéStops/Gyms at real landmarks
   - Use OpenStreetMap or similar for POIs
   - Community submissions for new locations

2. DYNAMIC SPAWNS: Pokémon appear based on:
   - Biome/terrain type (water Pokémon near water)
   - Time of day (nocturnal Pokémon at night)
   - Weather conditions
   - Special events

3. DENSITY RULES:
   - More spawns in populated areas (cell usage data)
   - Minimum spawns even in rural areas
   - Don't spawn in dangerous/private locations
```

---

## 8. UX Patterns

### The Problem
Location-based AR is confusing for new users

### Pokémon GO Solution: Progressive Disclosure
```
1. ONBOARDING:
   - Start with simple catch in AR (no walking required)
   - Introduce map after first success
   - Gradually unlock features

2. ALWAYS SHOW SOMETHING:
   - Never show empty map
   - If no objects nearby, show "walk to find more"
   - Daily bonuses encourage return

3. HAPTIC FEEDBACK:
   - Vibrate when object appears nearby
   - Different patterns for different rarities
   - Vibration intensity increases with proximity

4. AUDIO CUES:
   - Sound when entering object range
   - Ambient sounds based on environment
   - Victory sounds for collection
```

---

## 9. Offline/Poor Connectivity Handling

### The Problem
Mobile networks are unreliable

### Pokémon GO Solution: Graceful Degradation
```
1. CACHE AGGRESSIVELY:
   - Cache map tiles
   - Cache nearby objects for 5+ minutes
   - Cache player inventory/state

2. QUEUE ACTIONS:
   - If offline, queue collection attempts
   - Sync when connection restored
   - Show "pending" state to user

3. RETRY LOGIC:
   - Exponential backoff for failed requests
   - Don't spam server on reconnect
   - Prioritize critical actions (catches > spawns)
```

---

## 10. Common Debugging Checklist

When things don't work, check in this order:

```
□ Is GPS actually working? (Check accuracy, not just "enabled")
□ Is compass calibrated? (Often needs figure-8 motion)
□ Is AR camera finding tracking? (Needs textured surfaces)
□ Is camera tagged "MainCamera"?
□ Is object shader mobile-compatible?
□ Is object within camera frustum? (Check position vs camera)
□ Is object scale appropriate? (Not too small/large)
□ Is API returning data? (Check network tab/logs)
□ Is authentication working? (Token expired?)
□ Is RLS/permissions allowing access? (Supabase policies)
```

---

## Quick Reference: Pokémon GO vs Our Implementation

| Feature | Pokémon GO | Black Bart's Gold | Status |
|---------|-----------|-------------------|--------|
| GPS-to-AR | Compass-aligned | Compass-aligned | ✅ |
| Object visibility | Unlit shaders | Unlit shaders | ✅ |
| Radar/nearby | Yes | RadarUI exists | 🔧 |
| Map view | Primary view | Via GPS Manager | 🔧 |
| Haptic feedback | Proximity-based | Proximity-based | ✅ |
| Anti-cheat | Multi-layer | Basic (needs work) | ⚠️ |
| Offline support | Queue + cache | Basic caching | ⚠️ |
| Battery saver | Multiple modes | Not implemented | ❌ |

---

## Resources
- Niantic Developer Blog: https://nianticlabs.com/blog
- AR Foundation Best Practices: Unity AR documentation
- Location Services: Apple/Google location API docs
