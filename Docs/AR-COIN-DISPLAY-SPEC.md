# AR Coin Display System - Implementation Specification
# Black Bart's Gold - Prize Finder

## Document Info
- **Version**: 1.0
- **Created**: January 2026
- **Purpose**: Technical specification for distance-adaptive AR coin display
- **Reference**: Inspired by Minecraft Earth, ViroReact billboard patterns, and Pokémon GO

---

## 1. Overview

### 1.1 The Problem
Current implementation places coins at fixed world positions that:
- Are invisible until manually "revealed"
- Don't scale based on distance
- Get stuck to screen center instead of world-locked positions

### 1.2 The Solution
A hybrid display system that:
- Shows coins as **billboards** (always-facing indicators) when far away
- Transitions to **world-locked objects** when close
- Maintains **minimum screen size** at any distance
- Scales naturally with **perspective** when near

### 1.3 Reference Implementations
| Game | Far Display | Near Display | Transition |
|------|------------|--------------|------------|
| Minecraft Earth | Scaled buildplates | World-locked | Distance-based |
| Pokémon GO | 2D map icons | AR encounter | Tap to engage |
| ViroReact | Billboard transform | Fixed to world | Property-based |
| **Black Bart's Gold** | Billboard (3D) | World-locked | Automatic by distance |

---

## 2. Display Mode Architecture

### 2.1 Display Modes

```csharp
public enum CoinDisplayMode
{
    Hidden,      // Out of range (>100m) - not rendered
    Billboard,   // Far range (15-100m) - always faces camera, min screen size
    WorldLocked, // Near range (<15m) - fixed in AR space, natural perspective
    Collected    // Post-collection - cleanup state
}
```

### 2.2 Distance Thresholds

```csharp
[System.Serializable]
public class CoinDisplaySettings
{
    [Header("Distance Thresholds (meters)")]
    public float hideDistance = 100f;        // Beyond this = hidden
    public float billboardDistance = 15f;    // Beyond this = billboard mode
    public float collectionDistance = 5f;    // Within this = can collect
    
    [Header("Screen Size Settings")]
    public float minScreenSizePixels = 60f;  // Same as crosshairs
    public float maxScreenSizePixels = 200f; // Cap for very close
    
    [Header("World Scale Settings")]
    public float baseWorldScale = 0.3f;      // Real-world coin size (30cm)
    public float maxWorldScale = 0.5f;       // Maximum when very close
    
    [Header("Transition Settings")]
    public float transitionSmoothTime = 0.3f; // Seconds to blend modes
}
```

### 2.3 Visual Behavior by Mode

| Mode | Position | Rotation | Scale | Rendering |
|------|----------|----------|-------|-----------|
| **Hidden** | Not updated | N/A | N/A | Disabled |
| **Billboard** | GPS-relative, follows camera distance | Always faces camera | Constant screen size | Enabled, unlit |
| **WorldLocked** | Fixed world position | Y-axis spin only | Perspective-based | Enabled, lit |

---

## 3. Core Components

### 3.1 ARCoinRenderer (New Component)

Replaces visual rendering logic in CoinController.

```csharp
/// <summary>
/// Handles distance-adaptive rendering for AR coins.
/// Implements ViroReact-style billboard behavior with world-locking transition.
/// </summary>
public class ARCoinRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform coinVisual;      // The 3D coin mesh
    [SerializeField] private Transform billboardQuad;   // Optional 2D indicator
    
    [Header("Settings")]
    [SerializeField] private CoinDisplaySettings settings;
    
    // Current state
    public CoinDisplayMode CurrentMode { get; private set; }
    public float DistanceToCamera { get; private set; }
    
    // Camera reference (cached)
    private Camera arCamera;
    private Transform cameraTransform;
    
    // Smoothing
    private float currentScale;
    private float scaleVelocity;
    private CoinDisplayMode targetMode;
    private float modeTransitionProgress;
}
```

**Key Methods:**

```csharp
void Update()
{
    UpdateCameraReference();
    UpdateDistance();
    UpdateDisplayMode();
    ApplyModeRendering();
}

void UpdateDistance()
{
    if (cameraTransform == null) return;
    DistanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);
}

void UpdateDisplayMode()
{
    CoinDisplayMode newMode;
    
    if (DistanceToCamera > settings.hideDistance)
        newMode = CoinDisplayMode.Hidden;
    else if (DistanceToCamera > settings.billboardDistance)
        newMode = CoinDisplayMode.Billboard;
    else
        newMode = CoinDisplayMode.WorldLocked;
    
    if (newMode != targetMode)
    {
        targetMode = newMode;
        OnModeTransitionStart(CurrentMode, targetMode);
    }
    
    // Smooth transition
    if (CurrentMode != targetMode)
    {
        modeTransitionProgress += Time.deltaTime / settings.transitionSmoothTime;
        if (modeTransitionProgress >= 1f)
        {
            CurrentMode = targetMode;
            modeTransitionProgress = 0f;
            OnModeTransitionComplete();
        }
    }
}

void ApplyModeRendering()
{
    switch (CurrentMode)
    {
        case CoinDisplayMode.Hidden:
            ApplyHiddenMode();
            break;
        case CoinDisplayMode.Billboard:
            ApplyBillboardMode();
            break;
        case CoinDisplayMode.WorldLocked:
            ApplyWorldLockedMode();
            break;
    }
}
```

### 3.2 Billboard Mode Implementation

```csharp
/// <summary>
/// Billboard mode: Coin always faces camera with constant screen size.
/// Based on ViroReact's transformBehaviors: ["billboard"] pattern.
/// </summary>
void ApplyBillboardMode()
{
    if (cameraTransform == null || coinVisual == null) return;
    
    // ============================================================
    // STEP 1: BILLBOARD ROTATION (ViroReact pattern)
    // Make coin always face the camera
    // ============================================================
    Vector3 lookDirection = cameraTransform.position - transform.position;
    lookDirection.y = 0; // Keep upright (billboardY style)
    
    if (lookDirection.sqrMagnitude > 0.001f)
    {
        transform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
    }
    
    // ============================================================
    // STEP 2: CONSTANT SCREEN SIZE SCALING
    // Maintain minimum pixel size regardless of distance
    // ============================================================
    float targetScale = CalculateScaleForScreenSize(settings.minScreenSizePixels);
    
    // Smooth scale transition
    currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref scaleVelocity, 0.1f);
    transform.localScale = Vector3.one * currentScale;
    
    // ============================================================
    // STEP 3: SPIN ANIMATION (optional, for visual appeal)
    // ============================================================
    if (coinVisual != null)
    {
        coinVisual.Rotate(0, 90f * Time.deltaTime, 0, Space.Self);
    }
}

/// <summary>
/// Calculate the world scale needed to achieve a specific screen size in pixels.
/// This is the key formula for distance-independent visibility.
/// </summary>
float CalculateScaleForScreenSize(float targetScreenPixels)
{
    if (arCamera == null) return settings.baseWorldScale;
    
    // Camera field of view and screen height
    float fov = arCamera.fieldOfView;
    float screenHeight = Screen.height;
    
    // Calculate world size that produces targetScreenPixels at current distance
    // Formula: screenSize = (worldSize / distance) * (screenHeight / (2 * tan(fov/2)))
    // Solved for worldSize: worldSize = (targetScreenPixels * distance * 2 * tan(fov/2)) / screenHeight
    
    float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
    float tanHalfFov = Mathf.Tan(halfFovRad);
    
    float worldScale = (targetScreenPixels * DistanceToCamera * 2f * tanHalfFov) / screenHeight;
    
    // Clamp to reasonable bounds
    return Mathf.Clamp(worldScale, settings.baseWorldScale, settings.baseWorldScale * 10f);
}
```

### 3.3 World-Locked Mode Implementation

```csharp
/// <summary>
/// World-locked mode: Coin stays fixed in AR space with natural perspective.
/// Based on ViroReact's dragType: "FixedToWorld" pattern.
/// </summary>
void ApplyWorldLockedMode()
{
    if (coinVisual == null) return;
    
    // ============================================================
    // STEP 1: FIXED POSITION (no camera-relative movement)
    // Position is set once when entering this mode, then stays fixed
    // ============================================================
    // Position is managed by ARCoinPositioner, not updated here
    
    // ============================================================
    // STEP 2: PERSPECTIVE SCALING (natural, based on distance)
    // Closer = bigger (but don't exceed maxWorldScale)
    // ============================================================
    float normalizedDistance = Mathf.InverseLerp(
        settings.collectionDistance, 
        settings.billboardDistance, 
        DistanceToCamera
    );
    
    // Inverse lerp: closer (0) = bigger, further (1) = smaller
    float targetScale = Mathf.Lerp(
        settings.maxWorldScale,   // Close: max size
        settings.baseWorldScale,  // Far: base size
        normalizedDistance
    );
    
    currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref scaleVelocity, 0.1f);
    transform.localScale = Vector3.one * currentScale;
    
    // ============================================================
    // STEP 3: Y-AXIS SPIN ONLY (world-locked, not billboard)
    // Coin spins but doesn't face camera
    // ============================================================
    if (coinVisual != null)
    {
        coinVisual.Rotate(0, 45f * Time.deltaTime, 0, Space.Self);
    }
    
    // ============================================================
    // STEP 4: OPTIONAL BOB ANIMATION
    // Gentle float to make coin feel alive
    // ============================================================
    float bobY = Mathf.Sin(Time.time * 2f) * 0.05f;
    coinVisual.localPosition = new Vector3(0, bobY, 0);
}
```

### 3.4 ARCoinPositioner (Updated Component)

Handles GPS-to-AR position conversion with continuous updates.

```csharp
/// <summary>
/// Converts GPS coordinates to AR world positions.
/// Continuously updates position based on player movement and compass.
/// </summary>
public class ARCoinPositioner : MonoBehaviour
{
    [Header("GPS Data")]
    public double latitude;
    public double longitude;
    
    [Header("Settings")]
    [SerializeField] private float heightAboveGround = 1.0f;
    [SerializeField] private float positionSmoothTime = 0.5f;
    
    // Cached references
    private ARCoinRenderer coinRenderer;
    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    
    // Initial compass heading (captured at AR session start)
    private static float initialCompassHeading = 0f;
    private static bool hasInitialHeading = false;
    
    void Update()
    {
        // Only update position for non-world-locked coins
        // World-locked coins stay fixed once placed
        if (coinRenderer != null && 
            coinRenderer.CurrentMode != CoinDisplayMode.WorldLocked)
        {
            UpdatePositionFromGPS();
        }
        
        // Smooth position update
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref positionVelocity, 
            positionSmoothTime
        );
    }
    
    /// <summary>
    /// Calculate AR position from GPS coordinates.
    /// Uses compass-aligned positioning (Pokémon GO pattern).
    /// </summary>
    void UpdatePositionFromGPS()
    {
        var playerLocation = GPSManager.Instance?.CurrentLocation;
        if (playerLocation == null) return;
        
        // Calculate GPS distance and bearing
        var coinLocation = new LocationData(latitude, longitude);
        float distance = (float)playerLocation.DistanceTo(coinLocation);
        float gpsBearing = (float)playerLocation.BearingTo(coinLocation);
        
        // Compass alignment (from SKILL.md Pokemon GO patterns)
        float adjustedBearing = gpsBearing;
        if (hasInitialHeading)
        {
            adjustedBearing = gpsBearing - initialCompassHeading;
        }
        
        // Convert to AR coordinates
        float bearingRad = adjustedBearing * Mathf.Deg2Rad;
        float x = distance * Mathf.Sin(bearingRad);
        float z = distance * Mathf.Cos(bearingRad);
        float y = heightAboveGround;
        
        targetPosition = new Vector3(x, y, z);
    }
    
    /// <summary>
    /// Lock coin to current world position (for world-locked mode).
    /// Called when transitioning from Billboard to WorldLocked.
    /// </summary>
    public void LockToWorldPosition()
    {
        // Stop smooth movement - coin stays exactly where it is
        targetPosition = transform.position;
        positionVelocity = Vector3.zero;
    }
    
    /// <summary>
    /// Capture initial compass heading at AR session start.
    /// Must be called once when AR session begins.
    /// </summary>
    public static void CaptureInitialCompassHeading()
    {
        if (Input.compass.enabled)
        {
            initialCompassHeading = Input.compass.trueHeading;
            hasInitialHeading = true;
            Debug.Log($"[ARCoinPositioner] Captured compass heading: {initialCompassHeading}°");
        }
    }
}
```

---

## 4. Mode Transitions

### 4.1 Transition Flow

```
HIDDEN <-----------------------------------------------> BILLBOARD
         @ 100m (hideDistance)                    @ 100m

BILLBOARD <---------------------------------------------> WORLDLOCKED  
            @ 15m (billboardDistance)             @ 15m

                        | (within 5m + tap)
                    
                    COLLECTED
```

### 4.2 Transition Events

```csharp
public class ARCoinRenderer : MonoBehaviour
{
    // Events for UI/Audio feedback
    public event Action<CoinDisplayMode, CoinDisplayMode> OnModeChanged;
    public event Action OnEnteredCollectionRange;
    public event Action OnExitedCollectionRange;
    
    void OnModeTransitionStart(CoinDisplayMode from, CoinDisplayMode to)
    {
        Debug.Log($"[ARCoinRenderer] Transitioning: {from} -> {to}");
        
        // Special handling for entering world-locked mode
        if (to == CoinDisplayMode.WorldLocked)
        {
            // Lock position - coin stops following GPS updates
            GetComponent<ARCoinPositioner>()?.LockToWorldPosition();
            
            // Optional: Try to place on detected AR plane
            TryAnchorToPlane();
        }
        
        OnModeChanged?.Invoke(from, to);
    }
    
    void OnModeTransitionComplete()
    {
        // Check collection range
        if (CurrentMode == CoinDisplayMode.WorldLocked && 
            DistanceToCamera <= settings.collectionDistance)
        {
            OnEnteredCollectionRange?.Invoke();
        }
    }
    
    void TryAnchorToPlane()
    {
        // Attempt to raycast down and find AR plane
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, 
            out RaycastHit hit, 3f, planeLayerMask))
        {
            transform.position = hit.point + Vector3.up * 0.1f;
            Debug.Log("[ARCoinRenderer] Anchored to detected plane");
        }
    }
}
```

---

## 5. Visual Design

### 5.1 Billboard Mode Visuals

```
+---------------------------------------------+
|                                             |
|         FAR COIN (Billboard Mode)           |
|                                             |
|              +------------+                 |
|              |    COIN    | <- Gold icon    |
|              |   $5.00    | <- Value label  |
|              |    47m     | <- Distance     |
|              +------------+                 |
|                                             |
|         - Always faces camera               |
|         - Constant 60px minimum size        |
|         - Gentle spin animation             |
|         - Slight glow/pulse effect          |
|                                             |
+---------------------------------------------+
```

### 5.2 World-Locked Mode Visuals

```
+---------------------------------------------+
|                                             |
|        NEAR COIN (World-Locked Mode)        |
|                                             |
|                  $5.00                       |
|              +------------+                 |
|             /              \                |
|            |   3D COIN     | <- 3D gold     |
|             \              /                |
|              +------------+                 |
|                  ===                        |
|              (ground shadow)                |
|                                             |
|         - Fixed in world space              |
|         - Natural perspective scaling       |
|         - Y-axis spin only                  |
|         - Sparkle particle effects          |
|         - Ground shadow for depth cue       |
|                                             |
+---------------------------------------------+
```

### 5.3 Transition Animation

```csharp
/// <summary>
/// Smooth visual transition between billboard and world-locked modes.
/// </summary>
void AnimateTransition(float progress)
{
    // Billboard -> WorldLocked transition
    if (targetMode == CoinDisplayMode.WorldLocked)
    {
        // Gradually reduce billboard rotation
        float billboardInfluence = 1f - progress;
        
        // Blend between facing camera and world-aligned
        Quaternion billboardRot = Quaternion.LookRotation(-cameraDirection);
        Quaternion worldRot = Quaternion.identity;
        transform.rotation = Quaternion.Slerp(worldRot, billboardRot, billboardInfluence);
        
        // Fade in ground shadow
        if (groundShadow != null)
        {
            groundShadow.alpha = progress;
        }
        
        // Scale transition
        float billboardScale = CalculateScaleForScreenSize(settings.minScreenSizePixels);
        float worldScale = CalculateWorldLockedScale();
        currentScale = Mathf.Lerp(billboardScale, worldScale, progress);
    }
}
```

---

## 6. Integration with Existing Systems

### 6.1 CoinManager Updates

```csharp
// In CoinManager.cs - Update SpawnCoin method
public CoinController SpawnCoin(Coin coinData)
{
    // Create coin GameObject
    GameObject coinObj = Instantiate(coinPrefab);
    
    // Add new components
    var renderer = coinObj.AddComponent<ARCoinRenderer>();
    var positioner = coinObj.AddComponent<ARCoinPositioner>();
    
    // Configure positioner with GPS data
    positioner.latitude = coinData.latitude;
    positioner.longitude = coinData.longitude;
    
    // Subscribe to events
    renderer.OnEnteredCollectionRange += () => OnCoinInRange(coinData);
    renderer.OnModeChanged += (from, to) => OnCoinModeChanged(coinData, from, to);
    
    // Start in appropriate mode based on current distance
    // (renderer will auto-detect in first Update)
    
    return coinObj.GetComponent<CoinController>();
}
```

### 6.2 UIManager Updates

```csharp
// In UIManager.cs - Update mini-map to show mode
void UpdateMiniMapDot(CoinController coin)
{
    var renderer = coin.GetComponent<ARCoinRenderer>();
    var dot = GetOrCreateDot(coin.CoinId);
    
    // Color based on display mode
    switch (renderer.CurrentMode)
    {
        case CoinDisplayMode.Hidden:
            dot.gameObject.SetActive(false);
            break;
        case CoinDisplayMode.Billboard:
            dot.color = Color.yellow; // Far - yellow dot
            break;
        case CoinDisplayMode.WorldLocked:
            dot.color = Color.green;  // Near - green dot
            break;
    }
}
```

### 6.3 Remove/Update Old Components

Files to modify:
- `CoinController.cs` - Remove old display mode enum, delegate to ARCoinRenderer
- `CoinSpawner.cs` - Remove one-time positioning, use ARCoinPositioner
- `ARCoinPlacer.cs` - Simplify to only handle manual "reveal" override

---

## 7. Performance Considerations

### 7.1 LOD (Level of Detail)

```csharp
[System.Serializable]
public class CoinLODSettings
{
    [Header("Billboard Mode (Far)")]
    public Mesh simpleMesh;           // Low-poly or quad
    public Material unlitMaterial;    // Unlit, no shadows
    public bool enableParticles = false;
    
    [Header("World-Locked Mode (Near)")]
    public Mesh detailedMesh;         // High-poly coin
    public Material litMaterial;      // PBR with shadows
    public bool enableParticles = true;
}
```

### 7.2 Update Frequency

```csharp
[Header("Performance")]
[SerializeField] private float billboardUpdateRate = 0.1f;   // 10 Hz for far coins
[SerializeField] private float worldLockedUpdateRate = 0.033f; // 30 Hz for near coins

float lastUpdateTime;

void Update()
{
    float updateRate = (CurrentMode == CoinDisplayMode.Billboard) 
        ? billboardUpdateRate 
        : worldLockedUpdateRate;
    
    if (Time.time - lastUpdateTime < updateRate) return;
    lastUpdateTime = Time.time;
    
    // ... rest of update logic
}
```

### 7.3 Shader Recommendations

| Mode | Shader | Reason |
|------|--------|--------|
| Billboard | `Unlit/Color` or `URP/Unlit` | No lighting calc, max compatibility |
| WorldLocked | `URP/Lit` | Realistic appearance when close |

---

## 8. Testing Checklist

### 8.1 Distance Tests
- [ ] Coin hidden at >100m
- [ ] Coin shows as billboard at 50m
- [ ] Coin shows as billboard at 20m
- [ ] Coin transitions to world-locked at ~15m
- [ ] Coin stays world-locked when backing up to 20m (hysteresis)
- [ ] Coin collectible at <5m

### 8.2 Billboard Mode Tests
- [ ] Coin always faces camera when rotating phone
- [ ] Coin maintains ~60px screen size at all distances
- [ ] Coin visible against bright sky
- [ ] Coin visible against dark shadows

### 8.3 World-Locked Mode Tests
- [ ] Coin stays in place when walking around it
- [ ] Coin stays in place when tilting phone up/down
- [ ] Coin scales naturally (bigger when closer)
- [ ] Coin can be collected

### 8.4 Transition Tests
- [ ] Smooth transition from billboard to world-locked
- [ ] No visual popping or jumping
- [ ] Position doesn't suddenly change during transition

---

## 9. Future Enhancements

### 9.1 Phase 2: AR Plane Integration
- Detect ground plane when entering world-locked mode
- Anchor coin to detected plane for better stability
- Show ground shadow only when plane detected

### 9.2 Phase 3: Occlusion
- Use Niantic's occlusion or AR Foundation's environment occlusion
- Coins partially hidden behind real objects

### 9.3 Phase 4: Multi-Coin Optimization
- Instanced rendering for multiple billboard coins
- Spatial partitioning for efficient distance checks

---

## 10. References

### 10.1 External Documentation
- [Unity AR Scale Blog Post](https://blog.unity.com/engine-platform/dealing-with-scale-in-ar)
- [ViroReact ViroNode Documentation](https://viro-community.readme.io/docs/vironode)
- [AR+GPS Location Package](https://docs.unity-ar-gps-location.com/guide/)

### 10.2 Project Files
- `pokemon-go-patterns/SKILL.md` - Compass alignment patterns
- `CoinController.cs` - Current coin implementation
- `CoinSpawner.cs` - Current GPS-to-AR conversion
- `ARCoinPlacer.cs` - Current anchor system

---

## Appendix A: Quick Reference

### Distance Thresholds
| Distance | Mode | Behavior |
|----------|------|----------|
| >100m | Hidden | Not rendered |
| 15-100m | Billboard | Faces camera, constant screen size |
| 5-15m | WorldLocked | Fixed position, perspective scale |
| <5m | WorldLocked + Collectible | Can tap to collect |

### Key Formulas

**Constant Screen Size Scale:**
```
worldScale = (targetPixels * distance * 2 * tan(fov/2)) / screenHeight
```

**GPS to AR Position (Compass-Aligned):**
```
adjustedBearing = gpsBearing - initialCompassHeading
x = distance * sin(adjustedBearing)
z = distance * cos(adjustedBearing)
```

---

*Document created for Black Bart's Gold AR Prize Finder development.*
*Based on research from Minecraft Earth, ViroReact, Pokémon GO, and Unity AR best practices.*
