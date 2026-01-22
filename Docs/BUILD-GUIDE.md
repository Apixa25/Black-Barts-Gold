# 🤠 Black Bart's Gold - Unity Build Guide

This document provides a **step-by-step prompt guide** for building the entire Black Bart's Gold application using **Unity + AR Foundation**. Follow these prompts sequentially to build out each feature.

---

## 📋 Table of Contents

1. [How to Use This Guide](#how-to-use-this-guide)
2. [Pre-Development Setup](#pre-development-setup)
3. [Phase 1: MVP](#phase-1-mvp)
   - [Sprint 1: Unity Project Foundation](#sprint-1-unity-project-foundation)
   - [Sprint 2: AR Foundation Setup](#sprint-2-ar-foundation-setup)
   - [Sprint 3: AR Coin System](#sprint-3-ar-coin-system)
   - [Sprint 4: GPS & Location](#sprint-4-gps--location)
   - [Sprint 5: User Interface](#sprint-5-user-interface)
   - [Sprint 6: User Authentication](#sprint-6-user-authentication)
   - [Sprint 7: Wallet & Economy](#sprint-7-wallet--economy)
   - [Sprint 8: Backend Integration](#sprint-8-backend-integration)
4. [Phase 2: Enhanced Features](#phase-2-enhanced-features)
5. [Phase 3: Advanced Features](#phase-3-advanced-features)
6. [Reference Documents](#reference-documents)

---

## 🎯 How to Use This Guide

### For Each Sprint:

1. **Read the overview** to understand what we're building
2. **Copy the prompts** to the AI assistant (one at a time)
3. **Follow the sequence** - prompts build on each other
4. **Test after each prompt** before moving on
5. **Commit code** after each working feature

### Prompt Format:

Each prompt includes:
- 🎯 **Goal**: What we're trying to accomplish
- 📁 **Files Involved**: What files will be created/modified
- 📋 **Acceptance Criteria**: How to know it's done
- 💬 **The Prompt**: What to paste to the AI assistant

---

## 🛠️ Pre-Development Setup

### Before Starting ANY Code

**Prompt 0.1: Unity Hub & Editor Verification**
```
Before we start coding Black Bart's Gold in Unity, let's verify my development environment is ready.

Please help me check:
1. Unity Hub is installed
2. Unity 2022.3 LTS or Unity 6 is installed
3. Android Build Support module is installed
4. Android SDK & NDK are configured
5. My Android device is connected and recognized

Run the necessary checks and tell me what I need to fix.
```

**Prompt 0.2: Create Unity Project**
```
Let's create the Black Bart's Gold Unity project.

Please guide me through:
1. Creating a new 3D project in Unity Hub
2. Project name: "BlackBartsGold"
3. Location: C:\Users\Admin\Black-Barts-Gold
4. Template: 3D (Built-in Render Pipeline) or 3D Mobile

After creation, help me verify the project opens correctly.
```

**Prompt 0.3: Install AR Foundation Packages**
```
Install AR Foundation and platform packages for Black Bart's Gold.

In Unity Package Manager, install:
1. AR Foundation (latest 5.x)
2. ARCore XR Plugin (for Android)
3. ARKit XR Plugin (for iOS - optional for now)
4. XR Plugin Management

Guide me through:
- Opening Package Manager (Window → Package Manager)
- Finding and installing each package
- Verifying installation in Project Settings → XR Plug-in Management
```

**Prompt 0.4: Project Folder Structure**
```
Set up the project folder structure for Black Bart's Gold in Unity.

Create this structure in Assets/:
Assets/
├── Scripts/
│   ├── AR/              # AR-specific scripts
│   ├── Core/            # Core game systems (GameManager, etc.)
│   ├── Economy/         # Wallet, gas, transactions
│   ├── Location/        # GPS, distance calculations
│   ├── UI/              # UI controllers
│   └── Utils/           # Helper utilities
├── Scenes/
│   ├── MainMenu.unity
│   ├── ARHunt.unity
│   ├── Map.unity
│   └── Wallet.unity
├── Prefabs/
│   ├── Coins/           # Coin prefabs
│   ├── UI/              # UI prefabs
│   └── Effects/         # Particle effects
├── Materials/
│   └── Coins/           # Coin materials
├── Models/
│   └── Coins/           # 3D coin models
├── Audio/
│   ├── SFX/             # Sound effects
│   └── Voice/           # Black Bart voice lines
├── Textures/
│   └── UI/              # UI textures
├── Fonts/
└── Resources/           # Runtime-loaded assets

Create placeholder folders and add a README.txt in each explaining its purpose.
```

**Prompt 0.5: Configure Android Build Settings**
```
Configure Unity for Android builds.

Please help me set up:

1. Switch Platform to Android:
   - File → Build Settings → Android → Switch Platform

2. Player Settings (Edit → Project Settings → Player):
   - Company Name: [Your company]
   - Product Name: Black Bart's Gold
   - Package Name: com.yourcompany.blackbartsgold
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Latest
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 (uncheck ARMv7 for modern devices)

3. XR Plug-in Management:
   - Enable ARCore for Android
   - Enable ARKit for iOS (if building for iOS later)

4. Other Settings:
   - Color Space: Linear
   - Graphics API: OpenGLES3, Vulkan

Verify all settings are correct for AR development.
```

---

## 🚀 Phase 1: MVP

### Sprint 1: Unity Project Foundation
**Goal**: Basic project structure with scene navigation

---

**Prompt 1.1: Scene Manager & Navigation**

🎯 **Goal**: Set up scene management and navigation between screens

📁 **Files**: `Assets/Scripts/Core/SceneManager.cs`, `Assets/Scripts/Core/GameManager.cs`

📋 **Acceptance Criteria**:
- Can load different scenes
- GameManager persists between scenes
- Basic scene flow works

💬 **Prompt**:
```
Create the core scene management system for Black Bart's Gold.

Create Assets/Scripts/Core/GameManager.cs:
- Singleton pattern (DontDestroyOnLoad)
- Manages game state
- Handles scene transitions
- Stores player data reference

Create Assets/Scripts/Core/SceneLoader.cs:
- Static methods for loading scenes
- LoadScene(string sceneName)
- LoadSceneAsync with loading screen
- Scene names enum: MainMenu, ARHunt, Map, Wallet, Settings

Create these empty scenes:
1. MainMenu - Home screen
2. ARHunt - AR treasure hunting
3. Map - 2D map view
4. Wallet - Balance and transactions
5. Settings - User settings

Set up a simple Main Menu with buttons to navigate to each scene.
```

---

**Prompt 1.2: Data Models**

🎯 **Goal**: Define all data structures for the game

📁 **Files**: `Assets/Scripts/Core/Models/*.cs`

📋 **Acceptance Criteria**:
- All core data types defined
- Serializable for saving
- Clean structure

💬 **Prompt**:
```
Create data models for Black Bart's Gold.

Based on our documentation, create these C# classes in Assets/Scripts/Core/Models/:

Coin.cs:
- string id
- CoinType coinType (Fixed, Pool)
- float value
- float contribution
- double latitude, longitude
- string hiderId
- DateTime hiddenAt
- string logoFrontUrl, logoBackUrl
- HuntType huntType
- bool multiFind
- int findsRemaining
- CoinTier currentTier (Gold, Silver, Bronze, None)
- CoinStatus status (Hidden, Visible, Collected, Confirmed)

User.cs:
- string id
- string email
- float bbgBalance
- float gasRemaining
- float findLimit
- DateTime createdAt
- UserStats stats

UserStats.cs:
- int totalFound
- int totalHidden
- float totalValueFound
- float totalValueHidden
- float highestHiddenValue

Wallet.cs:
- float total
- float gasTank
- float parked
- float pending
- List<Transaction> recentTransactions

Transaction.cs:
- string id
- TransactionType type (Found, Hidden, GasConsumed, Purchased, Transfer)
- float amount
- DateTime timestamp
- string coinId
- TransactionStatus status (Pending, Confirmed)

LocationData.cs:
- double latitude
- double longitude
- float accuracy
- DateTime timestamp

Create necessary enums in Enums.cs:
- CoinType, CoinStatus, CoinTier
- HuntType (from treasure-hunt-types.md)
- TransactionType, TransactionStatus

Make all classes [Serializable] for easy saving/loading.
```

---

**Prompt 1.3: Player Data & Persistence**

🎯 **Goal**: Save and load player data

📁 **Files**: `Assets/Scripts/Core/PlayerData.cs`, `Assets/Scripts/Core/SaveSystem.cs`

📋 **Acceptance Criteria**:
- Player data saves to device
- Data loads on startup
- Works across sessions

💬 **Prompt**:
```
Create the player data and save system for Black Bart's Gold.

Create Assets/Scripts/Core/PlayerData.cs:
- Singleton, persists across scenes
- Stores current User data
- Stores Wallet data
- Stores LocationData
- Events: OnBalanceChanged, OnGasChanged, OnFindLimitChanged

Create Assets/Scripts/Core/SaveSystem.cs:
- SavePlayerData() - Serialize to JSON, save to Application.persistentDataPath
- LoadPlayerData() - Load and deserialize
- DeletePlayerData() - Clear saved data (for logout)
- Use JsonUtility or Newtonsoft.Json

Data to persist:
- User profile
- Auth token
- Wallet balance
- Settings preferences
- Last known location

Auto-save on:
- Balance changes
- Settings changes
- App pause/quit

Auto-load on:
- Game start
```

---

### Sprint 2: AR Foundation Setup
**Goal**: Working AR camera with plane detection

---

**Prompt 2.1: AR Session Setup**

🎯 **Goal**: Configure AR Foundation for basic AR

📁 **Files**: `Assets/Scenes/ARHunt.unity`, AR setup

📋 **Acceptance Criteria**:
- AR camera view works
- Tracking state shown
- No crashes

💬 **Prompt**:
```
Set up AR Foundation in the ARHunt scene.

In ARHunt scene, create:

1. AR Session Origin (or XR Origin in newer versions):
   - Add AR Session Origin component
   - Add AR Camera as child
   - Add AR Raycast Manager
   - Add AR Plane Manager (optional, for debugging)

2. AR Session:
   - Create GameObject with AR Session component
   - Configure for optimal mobile performance

3. Create Assets/Scripts/AR/ARSessionManager.cs:
   - Reference to ARSession
   - Track ARSession.state
   - Handle tracking states: None, Limited, Tracking
   - Events: OnTrackingStateChanged
   - Methods: ResetSession(), PauseSession(), ResumeSession()

4. Create simple UI to show:
   - Current tracking state
   - "Looking for surfaces..." when Limited
   - "Ready!" when Tracking

Test on Android device to verify AR camera works.
```

---

**Prompt 2.2: AR Plane Detection (Debug)**

🎯 **Goal**: Visualize detected planes for debugging

📁 **Files**: `Assets/Scripts/AR/PlaneVisualizer.cs`

📋 **Acceptance Criteria**:
- Detected planes show visualization
- Can toggle visualization on/off
- Helps verify AR is working

💬 **Prompt**:
```
Add plane detection visualization for debugging AR.

Create Assets/Scripts/AR/PlaneVisualizer.cs:
- Reference to ARPlaneManager
- Toggle visualization on/off (debug only)
- Different colors for horizontal vs vertical planes
- Show plane boundaries

Create a simple plane prefab:
- Quad mesh with semi-transparent material
- Grid pattern texture
- Scales to fit detected plane

This is for development only - we won't show planes in the final game,
but it helps verify AR is working correctly.

Add a debug toggle button in the AR scene to show/hide planes.
```

---

**Prompt 2.3: AR Raycast System**

🎯 **Goal**: Detect where player is looking in AR

📁 **Files**: `Assets/Scripts/AR/ARRaycastController.cs`

📋 **Acceptance Criteria**:
- Can raycast from screen center
- Detects hits on AR planes
- Detects hits on placed objects

💬 **Prompt**:
```
Create the AR raycast system for detecting what the player is looking at.

Create Assets/Scripts/AR/ARRaycastController.cs:
- Reference to ARRaycastManager
- Raycast from screen center (crosshairs position)
- Return hit info: position, normal, distance
- Separate methods for:
  - RaycastPlanes() - Hit AR planes
  - RaycastCoins() - Hit coin objects (using Physics.Raycast + layer mask)
  
- Events:
  - OnCoinHovered(Coin coin)
  - OnCoinUnhovered()
  - OnCoinSelected(Coin coin)

- Properties:
  - CurrentHoveredCoin
  - IsHoveringCoin

Update every frame to track what crosshairs are pointing at.
This will be used for the targeting system.
```

---

### Sprint 3: AR Coin System
**Goal**: 3D coins that appear and can be collected in AR

---

**Prompt 3.1: Coin Prefab & Materials**

🎯 **Goal**: Create the 3D coin visual

📁 **Files**: Coin prefab, materials

📋 **Acceptance Criteria**:
- Coin looks like gold doubloon
- Has front and back faces
- Materials for gold, silver, bronze, locked

💬 **Prompt**:
```
Create the 3D coin prefab for Black Bart's Gold.

Create coin prefab in Assets/Prefabs/Coins/CoinPrefab.prefab:

Structure:
- CoinPrefab (empty parent)
  - CoinModel (the 3D mesh)
  - ValueLabel (TextMeshPro - 3D text above coin)
  - SparkleEffect (Particle System)
  - CollectionEffect (Particle System - plays on collect)
  - AudioSource (for coin sounds)

For the coin model:
- Use a cylinder or find/create a coin mesh
- Scale: approximately 0.3 units diameter
- Add slight bevel/thickness for 3D look

Create materials in Assets/Materials/Coins/:
- GoldCoin.mat - Metallic gold (#FFD700)
- SilverCoin.mat - Metallic silver (#C0C0C0)
- BronzeCoin.mat - Metallic bronze (#CD7F32)
- LockedCoin.mat - Darker, red tint for above-limit coins

The coin should:
- Rotate slowly (spin animation)
- Bob up and down gently
- Have sparkle particles around it
- Show value label that always faces camera (billboard)
```

---

**Prompt 3.2: Coin Controller Script**

🎯 **Goal**: Coin behavior and interaction

📁 **Files**: `Assets/Scripts/AR/CoinController.cs`

📋 **Acceptance Criteria**:
- Coin spawns at GPS position
- Animations work (idle, collect)
- Can be tapped to collect
- Shows correct value/state

💬 **Prompt**:
```
Create the coin controller script.

Create Assets/Scripts/AR/CoinController.cs:

Properties:
- Coin data (the model class)
- bool isLocked (above player's find limit)
- bool isInRange (close enough to collect)
- bool isCollecting (animation playing)

Setup:
- Initialize(Coin coinData, bool locked, bool inRange)
- Set material based on tier and locked state
- Set value label text (show "?" for pool coins)
- Start idle animation

Animations (use Animator or DOTween):
- IdleAnimation() - Spin + bob, loops forever
- LockedPulse() - Slower, red pulse for locked coins
- CollectAnimation() - Fly toward camera, spin fast, shrink, fade
- AppearAnimation() - Scale up from 0 when spawning

Interaction:
- OnPointerClick() or custom tap detection
- If locked: Show "above limit" popup
- If not in range: Show "get closer" message
- If collectible: Play collect animation, trigger collection

Events:
- OnCollected(Coin coin)
- OnTapped(Coin coin)

The coin should always face the camera (billboard the value label).
Use layers for raycasting (e.g., "Coin" layer).
```

---

**Prompt 3.3: Coin Spawner & Manager**

🎯 **Goal**: Spawn coins at GPS locations in AR

📁 **Files**: `Assets/Scripts/AR/CoinSpawner.cs`, `Assets/Scripts/AR/CoinManager.cs`

📋 **Acceptance Criteria**:
- Coins spawn at correct real-world positions
- Coins update as player moves
- Only nearby coins are rendered

💬 **Prompt**:
```
Create the coin spawning system.

Create Assets/Scripts/AR/CoinManager.cs:
- Singleton, manages all active coins
- List<CoinController> activeCoins
- Dictionary for quick lookup by ID
- 
Methods:
- SetNearbyCoins(List<Coin> coins) - Update from server data
- SpawnCoin(Coin coin) - Instantiate at AR position
- DespawnCoin(string coinId) - Remove from scene
- UpdateCoinPositions() - Recalculate AR positions as player moves
- GetCoinById(string id)

Create Assets/Scripts/AR/CoinSpawner.cs:
- Converts GPS coordinates to AR world position
- Uses player's current GPS as origin (0,0,0)
- Calculates relative position for each coin

GPS to AR conversion:
- Get player's current lat/lng
- For each coin, calculate:
  - Distance in meters (Haversine formula)
  - Bearing (compass direction)
  - Convert to X/Z position in AR space
  - Y = fixed height above ground (e.g., 1.5 meters)

Properties:
- maxRenderDistance (e.g., 100 meters)
- coinPrefab reference

Only spawn coins within render distance.
Update positions every few seconds as GPS updates.
```

---

**Prompt 3.4: Test Coins in AR**

🎯 **Goal**: Display test coins to verify system works

📁 **Files**: Test setup

📋 **Acceptance Criteria**:
- Test coins appear in AR
- Can see coins when looking around
- Tapping coins triggers response

💬 **Prompt**:
```
Add test coins to verify the AR coin system works.

Create Assets/Scripts/AR/TestCoinSpawner.cs:
- Spawns test coins at fixed AR positions (not GPS)
- For development/testing only

Spawn 5 test coins:
1. Position (0, 1.5, -3) - directly in front, $1.00 gold
2. Position (-2, 1.5, -4) - left side, $5.00 gold
3. Position (2, 1.5, -4) - right side, pool coin (show "?")
4. Position (0, 1.5, -6) - further away, $10.00 silver
5. Position (-1, 2, -3) - higher up, $25.00 LOCKED (above limit)

Test the following:
1. Open AR scene
2. Look around - coins should be visible
3. Crosshairs should detect when hovering over coin
4. Tap a coin - should show appropriate response
5. Locked coin should show popup
6. Regular coin should play collection animation

Add a debug panel showing:
- Number of active coins
- Currently hovered coin
- Player find limit
```

---

### Sprint 4: GPS & Location
**Goal**: Real GPS tracking with distance calculations

---

**Prompt 4.1: Location Service**

🎯 **Goal**: Get and track device GPS location

📁 **Files**: `Assets/Scripts/Location/LocationService.cs`

📋 **Acceptance Criteria**:
- Gets current GPS position
- Tracks position changes
- Handles permissions

💬 **Prompt**:
```
Create the GPS location service for Black Bart's Gold.

Create Assets/Scripts/Location/LocationService.cs:

Singleton pattern.

Methods:
- StartLocationService() - Request permission, start tracking
- StopLocationService() - Stop tracking
- GetCurrentLocation() - Returns LocationData
- IsLocationEnabled() - Check if GPS is on

Use Unity's Input.location API:
- Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters)
- Input.location.lastData for current position
- Input.location.status for service state

Handle states:
- Disabled - GPS off, prompt user
- Initializing - Show loading
- Running - Normal operation
- Failed - Show error

Events:
- OnLocationUpdated(LocationData location)
- OnLocationError(string error)
- OnPermissionGranted()
- OnPermissionDenied()

Update location every 5 meters of movement.
Store last known good location for when GPS is unavailable.

Note: On Android, also need to request location permission in AndroidManifest.xml
```

---

**Prompt 4.2: Distance & Bearing Calculations**

🎯 **Goal**: Calculate distance and direction to coins

📁 **Files**: `Assets/Scripts/Location/GeoUtils.cs`

📋 **Acceptance Criteria**:
- Accurate distance calculation
- Correct bearing/compass direction
- GPS to AR position conversion

💬 **Prompt**:
```
Create geospatial utility functions.

Create Assets/Scripts/Location/GeoUtils.cs (static class):

Distance:
- float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
- Uses Haversine formula
- Returns distance in meters

Bearing:
- float CalculateBearing(double fromLat, double fromLon, double toLat, double toLon)
- Returns bearing in degrees (0-360, 0=North)

Direction helpers:
- string GetCardinalDirection(float bearing) - Returns "N", "NE", "E", etc.
- bool IsWithinRadius(LocationData point1, LocationData point2, float radiusMeters)

GPS to AR conversion:
- Vector3 GpsToArPosition(LocationData playerPos, LocationData targetPos, float heightAboveGround)
- Calculates relative X/Z from bearing and distance
- X = east/west offset
- Z = north/south offset (Unity forward is typically Z)
- Y = heightAboveGround parameter

AR to GPS conversion (reverse):
- LocationData ArPositionToGps(LocationData playerPos, Vector3 arPosition)

Batch helpers:
- List<Coin> FilterCoinsByDistance(List<Coin> coins, LocationData playerPos, float maxDistance)
- List<Coin> SortCoinsByDistance(List<Coin> coins, LocationData playerPos)
```

---

**Prompt 4.3: Haptic Feedback**

🎯 **Goal**: Vibration based on proximity to coins

📁 **Files**: `Assets/Scripts/Location/HapticService.cs`

📋 **Acceptance Criteria**:
- Vibrates when near coins
- Intensity increases as you get closer
- Different pattern for collection

💬 **Prompt**:
```
Create haptic feedback for proximity to coins.

Create Assets/Scripts/Location/HapticService.cs:

Reference the proximity zones from our docs:
| Distance | Vibration Pattern |
|----------|-------------------|
| > 50m    | No vibration      |
| 30-50m   | Light pulse every 2 seconds |
| 15-30m   | Medium pulse every 1 second |
| 5-15m    | Heavy pulse every 0.5 seconds |
| < 5m     | Continuous buzz (collectible range) |

Methods:
- StartProximityFeedback(float distanceMeters) - Start vibration pattern
- StopProximityFeedback() - Stop all vibration
- TriggerCollectionFeedback() - Special success pattern
- TriggerLockedFeedback() - Error/denied pattern
- SetEnabled(bool enabled) - Respect user settings

Use:
- Handheld.Vibrate() for basic vibration
- Or Android native plugin for more control

Coroutine-based timing for pulse patterns.
Only one active feedback at a time.
Check settings before vibrating (user may have disabled).
```

---

**Prompt 4.4: Connect GPS to AR Coins**

🎯 **Goal**: Position coins based on real GPS

📁 **Files**: Update CoinManager, CoinSpawner

📋 **Acceptance Criteria**:
- Coins appear at real-world GPS positions
- Positions update as player moves
- Distance/direction accurate

💬 **Prompt**:
```
Connect real GPS location to AR coin positioning.

Update Assets/Scripts/AR/CoinManager.cs:
1. Subscribe to LocationService.OnLocationUpdated
2. When location updates:
   - Recalculate AR positions for all coins
   - Update distance to each coin
   - Update in-range status for each coin
   - Trigger haptic feedback for nearest coin

Update Assets/Scripts/AR/CoinSpawner.cs:
1. Use GeoUtils.GpsToArPosition() for conversions
2. Only spawn coins within maxRenderDistance
3. Despawn coins that move out of range
4. Handle the AR "drift" - coins may shift as AR tracking adjusts

Create Assets/Scripts/AR/AROriginSync.cs:
- Syncs AR world origin with GPS
- Called when location significantly changes
- Handles the offset between AR and real world

The coins should appear in the correct real-world direction:
- A coin to the north should appear when facing north
- Distance shown should match real walking distance
- Compass should point accurately to coins

Test by:
1. Placing a test coin at your actual GPS location + 20 meters north
2. Walk toward it
3. Verify distance decreases
4. Verify coin appears in correct direction
```

---

### Sprint 5: User Interface
**Goal**: HUD overlay and UI screens

---

**Prompt 5.1: AR HUD Overlay**

🎯 **Goal**: Heads-up display over AR camera

📁 **Files**: `Assets/Scripts/UI/ARHUD.cs`, UI prefabs

📋 **Acceptance Criteria**:
- HUD displays over AR view
- Shows compass, gas, find limit
- Crosshairs in center

💬 **Prompt**:
```
Create the AR HUD overlay for the treasure hunting screen.

Reference the layout from docs/prize-finder-details.md:

┌─────────────────────────────────────────────┐
│ [🧭]                           [Find: $5]   │  ← Compass, Find Limit
│                                             │
│                    ⊕                        │  ← Crosshairs
│                                             │
│  ┌─────┐                            ║█████║ │  ← Mini-map, Gas Meter
│  │ MAP │                            ║░░░░░║ │
│  └─────┘                                    │
└─────────────────────────────────────────────┘

Create UI Canvas (Screen Space - Overlay) with:

1. Compass (Top-Left):
   - Direction arrow pointing to selected/nearest coin
   - Cardinal direction text (N, NE, E, etc.)
   - Distance to coin
   - Create: Assets/Scripts/UI/CompassUI.cs

2. Find Limit Display (Top-Right):
   - Shows "Find: $X.XX"
   - Color coded by tier
   - Create: Assets/Scripts/UI/FindLimitUI.cs

3. Crosshairs (Center):
   - Custom crosshair graphic
   - Changes color: White (normal), Green (coin in range), Red (locked)
   - Pulse animation when hovering coin
   - Create: Assets/Scripts/UI/CrosshairsUI.cs

4. Mini Map (Bottom-Left):
   - Radar-style display
   - Player dot in center
   - Coin dots around edge
   - Tap to open full map
   - Create: Assets/Scripts/UI/MiniMapUI.cs

5. Gas Meter (Right Edge):
   - Vertical gauge
   - Fill level = days remaining / 30
   - Color: Green (>50%), Yellow (15-50%), Red (<15%)
   - Flashes when low
   - Create: Assets/Scripts/UI/GasMeterUI.cs

Create Assets/Scripts/UI/ARHUD.cs:
- References to all HUD elements
- UpdateHUD() called each frame
- Show/Hide methods
- Connect to PlayerData for values
```

---

**Prompt 5.2: Main Menu Screen**

🎯 **Goal**: Home screen with navigation

📁 **Files**: MainMenu scene, UI scripts

📋 **Acceptance Criteria**:
- Shows player balance
- Navigation to all screens
- Pirate theme styling

💬 **Prompt**:
```
Create the Main Menu (Home) screen.

In MainMenu scene, create UI with:

Header Section:
- "Black Bart's Gold" title with pirate styling
- Player balance display (large, gold text): "$XX.XX BBG"
- Gas status: "⛽ XX days remaining"

Main Actions (Large buttons):
- "🤠 Start Hunting" → Load ARHunt scene
- "🗺️ Treasure Map" → Load Map scene
- "💰 Wallet" → Load Wallet scene
- "⚙️ Settings" → Load Settings scene

Quick Stats:
- "Coins Found: XXX"
- "Find Limit: $XX.XX"
- "Hidden: XXX coins"

Create Assets/Scripts/UI/MainMenuController.cs:
- Load player data on start
- Update UI with current values
- Handle button clicks
- Check gas before allowing hunting

Wild West Theme Styling (see brand-guide.md for details):
- Primary: Gold (#FFD700) - treasure, success
- Secondary: Deep Sea Blue (#1A365D) - mystery, depth
- Accent: Western Red (#8B0000) - warnings, locked items
- Tertiary: Parchment (#F5E6D3), Dark Brown (#3D2914)
- Font: Bold Western/slab serif for headers (or use TextMeshPro default for now)
- Buttons: Gold borders, brass/leather accents
- Background: Weathered wood, parchment, or old map texture
- NOTE: Black Bart was a stagecoach robber, NOT a pirate!

If gas is 0, disable "Start Hunting" and show "Buy More Gas" instead.
```

---

**Prompt 5.3: Wallet Screen**

🎯 **Goal**: Balance display and transaction history

📁 **Files**: Wallet scene, UI scripts

📋 **Acceptance Criteria**:
- Shows balance breakdown
- Transaction history list
- Park/Unpark functionality

💬 **Prompt**:
```
Create the Wallet screen.

Reference docs/economy-and-currency.md for wallet system.

In Wallet scene, create UI:

Balance Header:
- Total Balance (large): "$XX.XX BBG"

Balance Breakdown Cards:
- Gas Tank: $XX.XX (with progress bar, "XX days remaining")
- Parked: $XX.XX (protected from gas consumption)
- Pending: $XX.XX (with countdown to confirmation)

Action Buttons:
- "Park Coins" → Modal to move coins to parked
- "Unpark Coins" → Modal to move to gas tank
- "Add Gas" → Purchase flow (stub for now)

Transaction History (ScrollView):
- List of recent transactions
- Each shows: Icon, Description, Amount (+green/-red), Time
- Transaction types: Found, Hidden, Gas Consumed, Purchased
- Status badge for pending

Create Assets/Scripts/UI/WalletController.cs:
- Load wallet data
- UpdateUI()
- ShowParkModal(), ShowUnparkModal()
- RefreshTransactions()

Create Assets/Scripts/UI/TransactionItem.cs:
- Prefab for transaction list item
- SetData(Transaction tx)
- Show appropriate icon per type
- Format time as relative ("2 hours ago")

Back button to return to Main Menu.
```

---

**Prompt 5.4: Map Screen**

🎯 **Goal**: 2D map showing coin locations

📁 **Files**: Map scene, UI scripts

📋 **Acceptance Criteria**:
- Shows player position
- Shows nearby coins
- Can tap coin to navigate

💬 **Prompt**:
```
Create the Map screen showing coin locations.

In Map scene, create:

Map Display:
- For MVP, use a simple radar-style view (not a real map)
- Player in center
- Coins as dots at relative positions
- Scale: show coins within ~500 meter radius

Create Assets/Scripts/UI/MapController.cs:
- Get nearby coins from CoinManager or API
- Convert GPS to 2D map positions
- Update in real-time as player moves

Coin Markers:
- Gold dot for fixed value coins
- Silver dot for pool coins
- Red dot with lock for above-limit coins
- Size based on value (bigger = more valuable)

Selected Coin Panel (when tapping a coin):
- Shows coin value (or "?" for pool)
- Shows distance and direction
- "Navigate" button → Opens ARHunt with this coin selected
- "Cancel" button

Create Assets/Scripts/UI/CoinMarker.cs:
- Prefab for map marker
- SetCoin(Coin coin, float distance)
- OnClick → Select coin

Player marker:
- Show current position (center)
- Show current heading (arrow direction)

List View (bottom of screen):
- Scrollable list of nearby coins
- Sorted by distance
- Shows: Value, Distance, Direction
- Tap to select

Back button to Main Menu.
```

---

### Sprint 6: User Authentication
**Goal**: Login, registration, session management

---

**Prompt 6.1: Auth Service**

🎯 **Goal**: Handle authentication

📁 **Files**: `Assets/Scripts/Core/AuthService.cs`

📋 **Acceptance Criteria**:
- Can register new account
- Can login with email/password
- Session persists

💬 **Prompt**:
```
Create the authentication service.

Create Assets/Scripts/Core/AuthService.cs:

Singleton pattern.

Methods:
- async Task<User> Register(string email, string password, int age)
  - Validate email format
  - Validate password (min 8 chars)
  - Call API (stub for now, return mock user)
  
- async Task<User> Login(string email, string password)
  - Authenticate credentials
  - Store auth token
  - Load user data

- async Task<User> LoginWithGoogle()
  - Trigger Google OAuth (stub for now)
  
- async Task Logout()
  - Clear stored credentials
  - Clear player data
  
- async Task<User> GetCurrentUser()
  - Check for stored session
  - Validate token
  - Return user if valid

- void SaveSession(string token)
- void ClearSession()
- bool HasValidSession()

Events:
- OnLoginSuccess(User user)
- OnLogoutSuccess()
- OnAuthError(string error)

For MVP, use mock responses:
- Any email/password logs in
- Return a test user with starter values
- Store "fake" token in PlayerPrefs

Later we'll connect to real backend.
```

---

**Prompt 6.2: Auth Screens**

🎯 **Goal**: Login and registration UI

📁 **Files**: Auth scene/screens, UI scripts

📋 **Acceptance Criteria**:
- Login form works
- Registration form works
- Validation shows errors

💬 **Prompt**:
```
Create authentication screens.

Create Login Screen UI:
- Email input field
- Password input field
- "Login" button
- "Login with Google" button
- "Create Account" link
- Error message display area

Create Registration Screen UI:
- Email input field
- Password input field
- Confirm Password input field
- Age dropdown (13-99)
- Terms of Service checkbox
- "Create Account" button
- "Already have account?" link

Create Onboarding Screen (first launch):
- Welcome message with Black Bart branding
- Brief game explanation
- "Login" button
- "Create Account" button
- "How It Works" expandable section

Create Assets/Scripts/UI/AuthController.cs:
- Manages auth UI flow
- ShowLogin(), ShowRegister(), ShowOnboarding()
- ValidateLoginForm(), ValidateRegistrationForm()
- HandleLogin(), HandleRegister()
- Show/hide loading indicator
- Display error messages

Validation:
- Email: Valid format check
- Password: Min 8 characters
- Confirm Password: Must match
- Age: Must be 13+
- Terms: Must be checked

On successful auth:
- Save session
- Load player data
- Navigate to Main Menu
```

---

**Prompt 6.3: Protected Scenes & Auto-Login**

🎯 **Goal**: Require auth to access game

📁 **Files**: Update GameManager, scene loading

📋 **Acceptance Criteria**:
- Non-auth users see login
- Auth users see main menu
- Session persists across restarts

💬 **Prompt**:
```
Implement protected scenes and auto-login.

Update Assets/Scripts/Core/GameManager.cs:

On game start:
1. Check for saved session
2. If valid session:
   - Load player data
   - Go to Main Menu
3. If no session:
   - Go to Onboarding/Login

Create Assets/Scripts/Core/SessionManager.cs:
- CheckSession() - Verify stored token
- IsLoggedIn property
- AutoLogin() - Try to restore session

Protected scenes check:
- Main Menu, AR Hunt, Map, Wallet, Settings all require auth
- If accessed without auth, redirect to Login

Update scene loading:
- SceneLoader.LoadProtectedScene(string name) 
  - Check auth first
  - Redirect to login if not authenticated

Handle token expiration:
- If API returns 401, clear session and redirect to login
- Show "Session expired, please log in again"

Add logout option in Settings:
- Confirm dialog
- Clear all data
- Return to Onboarding
```

---

### Sprint 7: Wallet & Economy
**Goal**: Gas system, balance management

---

**Prompt 7.1: Wallet Service**

🎯 **Goal**: Manage BBG balance

📁 **Files**: `Assets/Scripts/Economy/WalletService.cs`

📋 **Acceptance Criteria**:
- Track balance breakdown
- Handle park/unpark
- Transaction history

💬 **Prompt**:
```
Create the wallet service.

Create Assets/Scripts/Economy/WalletService.cs:

Reference docs/economy-and-currency.md:
- Purchased coins: Must use as gas, can't park
- Found coins: Can park OR use as gas
- Pending: 0-24 hours, can see but not use
- Confirmed: After 24 hours, fully usable

Methods:
- async Task<Wallet> GetBalance()
  - Return: total, gasTank, parked, pending
  
- async Task<List<Transaction>> GetTransactions(int limit, int offset)
  - Return paginated history
  
- async Task ParkCoins(float amount)
  - Move found coins from gas to parked
  - Validate: only found coins can be parked
  
- async Task UnparkCoins(float amount)
  - Move parked to gas tank
  - Charge one day's gas fee ($0.33)
  
- async Task<float> ConsumeGas()
  - Daily consumption (~$0.33)
  - Return remaining
  
- bool CanPlay()
  - Return gasRemaining > 0

Transaction recording:
- AddTransaction(type, amount, coinId)
- Auto-create transaction entries for all balance changes

For MVP, use local storage for all wallet data.
Later will sync with backend.
```

---

**Prompt 7.2: Gas System**

🎯 **Goal**: Implement gas consumption and warnings

📁 **Files**: `Assets/Scripts/Economy/GasService.cs`

📋 **Acceptance Criteria**:
- Gas decrements daily
- Low gas warning shows
- No gas blocks hunting

💬 **Prompt**:
```
Implement the gas system.

Create Assets/Scripts/Economy/GasService.cs:

Reference docs/prize-finder-details.md gas section:
- $10 = 30 days of play
- ~$0.33 consumed per day at midnight
- 15% remaining: Warning
- 0 remaining: Can't play

Methods:
- GetGasStatus() → GasStatus object:
  - float remaining (dollar value)
  - int daysLeft
  - bool isLow (<15%)
  - bool isEmpty (0)
  - string statusMessage
  
- CheckAndConsumeGas()
  - Called on app launch
  - If new day since last charge, consume $0.33
  - Store last charge date
  
- GetGasMeterColor() → Color
  - Green if >50%
  - Yellow if 15-50%
  - Red if <15%

Create Assets/Scripts/UI/NoGasOverlay.cs:
- Full screen overlay shown when gas = 0
- "Looks Like You've Hit a Dry Well, Partner!"
- Stagecoach/desert illustration (or text for now)
- "Buy More Gas" button
- "Unpark Coins" button (if has parked balance)

Create Assets/Scripts/UI/LowGasWarning.cs:
- Banner at top of AR screen
- "⚠️ LOW FUEL - X days remaining"
- Flashing animation
- Dismissible (once per session)

Update AR scene:
- Check gas on load
- Show NoGasOverlay if empty (disable AR)
- Show LowGasWarning if low
```

---

**Prompt 7.3: Coin Collection Flow**

🎯 **Goal**: Complete collection with wallet updates

📁 **Files**: `Assets/Scripts/Economy/CollectionService.cs`

📋 **Acceptance Criteria**:
- Collection updates wallet
- Find limit enforced
- Animation and feedback

💬 **Prompt**:
```
Create the complete coin collection flow.

Create Assets/Scripts/Economy/CollectionService.cs:

Pre-collection checks:
- CanCollect(Coin coin) → CollectionCheck result:
  - bool canCollect
  - string reason (if can't)
  - Reasons: "TOO_FAR", "OVER_LIMIT", "NO_GAS", "ALREADY_COLLECTED"

Collection process:
- async Task<CollectionResult> CollectCoin(Coin coin):
  1. Validate can collect
  2. Determine value (for pool coins, calculate now)
  3. Play collection animation
  4. Add value to wallet (pending status)
  5. Create transaction record
  6. Remove coin from scene
  7. Trigger haptic feedback
  8. Return result with value

Pool coin value calculation (from docs/dynamic-coin-distribution.md):
- Slot machine algorithm
- Based on player's recent find history
- Returns calculated value

Over-limit handling:
- IsOverLimit(Coin coin) → bool
- GetOverLimitMessage(Coin coin) → string
- Show hint: "Hide $X to unlock!"

Create Assets/Scripts/UI/CollectionPopup.cs:
- Shows after successful collection
- "+$X.XX" with gold styling
- Black Bart congratulation (text for now, audio later)
- Auto-dismiss after 2 seconds

Update CoinController:
- OnCollect() calls CollectionService.CollectCoin()
- Plays animation
- Shows popup
- Removes self
```

---

**Prompt 7.4: Find Limit System**

🎯 **Goal**: Enforce find limits

📁 **Files**: `Assets/Scripts/Economy/FindLimitService.cs`

📋 **Acceptance Criteria**:
- Limits enforced correctly
- Over-limit coins show locked
- Hiding updates limit

💬 **Prompt**:
```
Implement the find limit system.

Reference docs/economy-and-currency.md:
- Default limit: $1.00 (no coins hidden)
- Limit = highest single coin ever hidden
- Limits never decrease

Create Assets/Scripts/Economy/FindLimitService.cs:

Methods:
- float GetCurrentLimit()
- bool IsOverLimit(Coin coin)
- UpdateLimitAfterHide(float hiddenValue)
- GetTierInfo() → tier name, color, next unlock

Tiers (Western themed - see brand-guide.md):
- $1.00: "Greenhorn"
- $5.00: "Prospector"  
- $10.00: "Treasure Hunter"
- $25.00: "Trail Boss"
- $50.00: "Frontier Legend"
- $100.00+: "Gold Rush King"

Create Assets/Scripts/UI/FindLimitPopup.cs:
- Modal shown when tapping locked coin
- Shows: "This treasure's above yer limit, partner!"
- Current limit: "$X.XX"
- Coin value: "$Y.YY"
- "Hide $Z.ZZ to unlock!"
- "Hide a Coin" button → Navigate to Hide screen
- "Cancel" button

Visual indicators:
- Locked coins have red tint material
- Locked coins have different particle effect
- Lock icon overlay

Update CoinController:
- Check limit on spawn
- Apply locked visual if over limit
- Show popup instead of collecting if locked
```

---

### Sprint 8: Backend Integration
**Goal**: Connect to real server

---

**Prompt 8.1: API Client**

🎯 **Goal**: HTTP client for backend calls

📁 **Files**: `Assets/Scripts/Core/ApiClient.cs`

📋 **Acceptance Criteria**:
- Can make GET/POST requests
- Handles auth headers
- Error handling

💬 **Prompt**:
```
Create the API client for backend communication.

Create Assets/Scripts/Core/ApiClient.cs:

Singleton.

Configuration:
- baseUrl (dev: "http://localhost:3000/api/v1", prod: TBD)
- timeout (30 seconds)

Methods:
- async Task<T> Get<T>(string endpoint)
- async Task<T> Post<T>(string endpoint, object body)
- async Task<T> Put<T>(string endpoint, object body)
- async Task Delete(string endpoint)

Features:
- Add auth token to headers automatically
- JSON serialization/deserialization
- Error handling with ApiException
- Retry logic for network failures
- Timeout handling

Use UnityWebRequest for HTTP calls.

Error handling:
- Network error: Throw NetworkException
- 401 Unauthorized: Clear session, throw AuthException
- 400 Bad Request: Parse error message, throw ApiException
- 500 Server Error: Throw ServerException

Create Assets/Scripts/Core/ApiException.cs:
- int statusCode
- string message
- string errorCode

Logging:
- Log requests in debug mode
- Log response times
- Log errors
```

---

**Prompt 8.2: Connect Services to API**

🎯 **Goal**: Wire up all services to backend

📁 **Files**: Update all service files

📋 **Acceptance Criteria**:
- Auth uses real API
- Coins come from server
- Wallet syncs with server

💬 **Prompt**:
```
Connect all services to use the real backend API.

Update AuthService.cs:
- POST /auth/register
- POST /auth/login  
- POST /auth/google
- POST /auth/logout
- GET /auth/me

Update WalletService.cs:
- GET /wallet
- GET /wallet/transactions
- POST /wallet/park
- POST /wallet/unpark
- POST /wallet/consume-gas

Create CoinApiService.cs:
- GET /coins/nearby?lat=X&lng=Y&radius=Z
- POST /coins/hide
- POST /coins/:id/collect
- DELETE /coins/:id

Update AuthService to use ApiClient.
Update WalletService to use ApiClient.
Update CollectionService to use CoinApiService.

Handle offline mode:
- Cache last known data
- Queue actions for sync when online
- Show "offline" indicator

Error handling:
- Show user-friendly error messages
- Retry option for failed requests
- Don't lose user progress on errors

Add API configuration:
- Toggle between mock and real API
- Environment-based URL selection
```

---

**Prompt 8.3: End-to-End Testing**

🎯 **Goal**: Verify complete flow works

📋 **Acceptance Criteria**:
- Register → Login → Hunt → Collect → Wallet updated
- All screens work
- No crashes

💬 **Prompt**:
```
Test the complete game flow end-to-end.

Test Scenario 1: New User
1. Launch app → Onboarding screen
2. Tap "Create Account"
3. Fill registration form
4. Login → Main Menu
5. Check balance shows starter value

Test Scenario 2: Treasure Hunt
1. Tap "Start Hunting"
2. AR camera opens
3. Look around for coins
4. Walk toward a coin
5. Crosshairs turn green when in range
6. Tap to collect
7. Animation plays
8. Value popup shows
9. Return to menu
10. Balance increased

Test Scenario 3: Locked Coin
1. Find a coin above your limit
2. Verify it shows locked (red)
3. Tap it
4. Popup shows "above limit"
5. Shows hint to hide more

Test Scenario 4: Gas System
1. Set gas to low (<15%)
2. Open AR hunt
3. Low gas warning shows
4. Set gas to 0
5. AR hunt blocked
6. No gas screen shows

Test Scenario 5: Wallet
1. Open Wallet screen
2. Verify balance breakdown
3. View transaction history
4. Test park/unpark

Document all bugs found.
Fix critical issues.
```

---

## 🚀 Phase 2: Enhanced Features

*(To be detailed after Phase 1 completion)*

### Sprints Overview

**Sprint 9: Multiple Hunt Types**
- Compass-only hunt mode
- Radar-only hunt mode
- Timed release hunts

**Sprint 10: Social Features**
- Friends list
- Leaderboards
- Activity feed

**Sprint 11: Coin Hiding**
- Full hide coin wizard
- Custom coin placement
- Map selection

**Sprint 12: Polish & Audio**
- Black Bart voice lines
- Sound effects
- Visual polish

---

## 🎮 Phase 3: Advanced Features

*(To be detailed after Phase 2 completion)*

- Guilds and teams
- Advanced treasure hunts
- Sponsor system
- Mythical coin hunts

---

## 📚 Reference Documents

> **🤠 IMPORTANT**: Read **brand-guide.md** before starting any UI/UX work. Black Bart was a Wild West stagecoach robber, NOT a pirate!

| Document | Use For |
|----------|---------|
| [brand-guide.md](./brand-guide.md) | 🤠 **READ FIRST** - Character identity, visual style, voice |
| [project-vision.md](./project-vision.md) | Overall concept, tech decisions |
| [project-scope.md](./project-scope.md) | Features, phases, business model |
| [prize-finder-details.md](./prize-finder-details.md) | AR UI layout, HUD design |
| [coins-and-collection.md](./coins-and-collection.md) | Coin mechanics, collection |
| [economy-and-currency.md](./economy-and-currency.md) | BBG, gas, find limits |
| [treasure-hunt-types.md](./treasure-hunt-types.md) | Hunt configurations |
| [user-accounts-security.md](./user-accounts-security.md) | Auth, anti-cheat |
| [social-features.md](./social-features.md) | Friends, leaderboards |
| [admin-dashboard.md](./admin-dashboard.md) | Admin tools |
| [dynamic-coin-distribution.md](./dynamic-coin-distribution.md) | Coin distribution |
| [safety-and-legal-research.md](./safety-and-legal-research.md) | Legal |

---

## 🤠 Unity-Specific Tips

### Testing AR
- AR doesn't work in Unity Editor - must test on device
- Use Unity Remote for quick preview (limited AR)
- Build and deploy for full AR testing

### Performance
- Profile with Unity Profiler
- Watch draw calls in AR scenes
- Limit active coin count

### Debugging
- Use Debug.Log for tracing
- Unity Console for errors
- ADB logcat for Android logs

---

## 📝 Version History

| Date | Changes |
|------|---------|
| Jan 17, 2026 | Initial Unity build guide created |
| - | Migrated from React Native + ViroReact |

---

**Ready to build? Start with Prompt 0.1!** 🤠
