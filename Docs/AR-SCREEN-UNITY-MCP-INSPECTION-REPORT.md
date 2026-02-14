# AR Screen – Unity MCP Inspection Report

**Date:** 2026-02-10  
**Scene:** ARHunt (active)  
**Method:** Live Unity MCP inspection (get_hierarchy, find_gameobjects)

---

## Summary

The Unity MCP was used to inspect the ARHunt scene. Several assets and references are missing or misconfigured, which explains the missing mini-map, crosshairs, and white square.

---

## 1. Crosshairs (Instance ID: 39856)

**Path:** `HUDCanvas/Crosshairs`

| Finding | Status |
|---------|--------|
| **Transform type** | Uses `Transform` instead of `RectTransform` |
| **Components** | Transform, CrosshairsController |
| **Child** | CollectionSizeCircle (RectTransform, Image) |
| **CrosshairsController.crosshairsImage** | Likely null – Crosshairs has no Image on itself |

**Issue:**  
- Crosshairs uses a 3D `Transform`, not a UI `RectTransform`, so it may not behave correctly under the Canvas.  
- `CrosshairsController` expects `crosshairsImage` (SerializeField). The only Image is on the child `CollectionSizeCircle`, which is for the collection-size circle, not the main crosshairs.  
- `ARHuntSceneSetup.SetupCrosshairs()` adds an Image at runtime, but `CrosshairsController` does not use it because its `crosshairsImage` is not assigned.

**Fix in Unity:**
1. Select `HUDCanvas/Crosshairs`.
2. Replace `Transform` with `RectTransform` (or recreate as a UI element).
3. Add an Image for the main crosshairs (or use the one added by `SetupCrosshairs`).
4. Assign that Image to `CrosshairsController.crosshairsImage` in the Inspector.

---

## 2. RadarPanel / Mini-Map (Instance ID: 40026)

**Path:** `HUDCanvas/RadarPanel`

| Finding | Status |
|---------|--------|
| **Components** | RectTransform, RadarUI, Image, Button |
| **Children** | 0 |
| **radarContainer** | Auto-assigned to self (RadarPanel RectTransform) |
| **coinDotSprite** | Assigned in scene (from scene file) |

**Issue:**  
- RadarPanel has no child structure (radarContainer, playerDot, etc.), but `RadarUI` can use itself as the container and creates coin dots at runtime.  
- The “no mini-map” problem may be due to:
  - Position/size (anchors, sizeDelta) placing it off-screen or too small.
  - Canvas or parent layout hiding it.
  - `RadarUI` references not wired in the Inspector.

**Fix in Unity:**
1. Select `HUDCanvas/RadarPanel`.
2. In the Inspector, check RectTransform (anchors, position, size).
3. Ensure it is visible (e.g. bottom-right, ~180×180).
4. Confirm `RadarUI` references (especially `coinDotSprite`) are assigned.

---

## 3. ARHUD (HUDManager, Instance ID: 40006)

**Path:** `HUDCanvas/HUDManager`

| Finding | Status |
|---------|--------|
| **Components** | Transform, ARHUD |
| **SerializeField refs** | compass, radar, crosshairs, gasMeter, findLimit, directionIndicator, etc. |

**Issue:**  
- `ARHUD` uses SerializeFields for compass, radar, crosshairs, etc.  
- If these are not assigned in the Inspector, ARHUD will not update them (it checks `if (crosshairs != null)` etc.).  
- `CompassUI` has 0 instances in the scene, so `compass` cannot be assigned.

**Fix in Unity:**
1. Select `HUDCanvas/HUDManager`.
2. In the ARHUD component, assign:
   - **radar** → `RadarPanel` (RadarUI)
   - **crosshairs** → `Crosshairs` (CrosshairsController)
   - **directionIndicator** → `DirectionIndicatorPanel` (CoinDirectionIndicator)
   - **gasMeter** → GasMeterUI (if present)
   - **findLimit** → FindLimitUI (if present)
3. Add a `CompassUI` component to the appropriate panel (e.g. CompassPanel or CompassArrowPanel) if compass behavior is needed, then assign it to `compass`.

---

## 4. FullMapPanel (Instance ID: 39986)

**Path:** `HUDCanvas/FullMapPanel`

| Finding | Status |
|---------|--------|
| **Active** | Inactive (expected when map is closed) |
| **Components** | RectTransform, Image, FullMapUI |
| **Children** | Header, MapContainer, SelectionPanel |

FullMapPanel and FullMapUI are present and structured correctly.

---

## 5. Scene Hierarchy (Roots)

| Root | Instance ID | Children |
|------|-------------|----------|
| AR Session | 40062 | 0 |
| XR Origin | 39952 | 1+ |
| EventSystem | 39932 | 0 |
| GameManager | 39828 | 2 |
| **HUDCanvas** | 40050 | 13 |
| ARCoinPlacer | 39730 | 0 |
| CoinManager | 40070 | 0 |

---

## 6. HUDCanvas Children

| Name | Instance ID | Components | Notes |
|------|-------------|------------|-------|
| Crosshairs | 39856 | Transform, CrosshairsController | Needs RectTransform, crosshairsImage |
| CompassPanel | 39824 | Transform only | No CompassUI |
| GasMeterPanel | 40038 | Transform only | Possibly no GasMeterUI |
| FindLimitPanel | 39820 | Transform only | Possibly no FindLimitUI |
| RadarPanel | 40026 | RectTransform, RadarUI, Image, Button | OK; check layout |
| BackButton | 39744 | RectTransform, Image, Button | OK |
| HUDManager | 40006 | Transform, ARHUD | Needs refs assigned |
| FullMapPanel | 39986 | RectTransform, Image, FullMapUI | OK (inactive) |
| DirectionIndicatorPanel | 39702 | RectTransform, CoinDirectionIndicator, etc. | OK |
| CollectButton | 39870 | RectTransform, Button, CollectButtonController | OK |
| CompassArrowPanel | 39862 | RectTransform, Image | No CompassUI |
| CoinInfoPanel | 39756 | RectTransform, Image, CanvasGroup | OK |
| TestDummy | 39718 | Transform | - |

---

## 7. Recommended Actions

1. **Crosshairs**
   - Convert Crosshairs to use RectTransform.
   - Add or assign an Image for the main crosshairs.
   - Assign that Image to `CrosshairsController.crosshairsImage`.

2. **ARHUD references**
   - Assign radar, crosshairs, directionIndicator (and gasMeter/findLimit if used) in the ARHUD Inspector.

3. **RadarPanel**
   - Verify RectTransform layout (anchors, position, size) so the mini-map is visible.

4. **ARHuntSceneSetup**
   - Add a null check for `rect` in `SetupCrosshairs()` to avoid NullReferenceException if RectTransform is missing.

---

## 8. About Ask Mode vs Agent Mode

In Ask mode, Unity MCP tools were blocked with:  
“You are in ask mode and cannot run non read-only tools.”

In Agent mode, the Unity MCP tools work, and this inspection was done with:
- `manage_scene` (get_hierarchy)
- `find_gameobjects` (by_name, by_component)

So Unity MCP is usable in Agent mode; in Ask mode it is restricted.
