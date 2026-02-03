# UI Architecture Migration Plan – Wallet/Settings Freeze Fix

**Date:** 2026-02-03  
**Goal:** Resolve persistent touch freeze when opening Wallet/Settings by aligning with market-standard panel-based navigation.

---

## Executive Summary

The Wallet and Settings screens freeze (no touch/click response) because they are **loaded as separate scenes**. This triggers a known Unity bug: **Input System stops working after a scene is loaded** (especially on Android with InputSystemUIInputModule).

**Solution:** Switch to **panel-based navigation** for Wallet and Settings, matching how games like Pokémon GO and Clash of Clans structure their UI. Use show/hide of panels instead of scene loads for these screens.

---

## 1. Research Findings

### 1.1 Market Standard: Single Scene + Panels

| Source | Finding |
|--------|---------|
| Stack Overflow / Unity Discussions | Single scene with activate/deactivate UI panels is **more efficient** than loading scenes. Scene loading causes significant CPU spikes. |
| Unity best practices | Menu systems should occupy a **single scene** to avoid loading overhead and input issues. |
| Pokémon GO / Clash of Clans pattern | Panels are shown/hidden within one scene; visibility controlled by script logic and Sort Order. |

### 1.2 Unity Input System Bug

| Issue | Details |
|-------|---------|
| **Unity Issue Tracker #1388595** | "Input system stops working when a new Scene is loaded" – items become not clickable after scene transition. |
| **Android-specific** | Touch inputs inconsistently trigger click events after scene load; sometimes hover works but click fails. |
| **Root cause** | InputSystemUIInputModule can lose touch/pointer state when scenes load/unload; EventSystem and input routing get confused. |

### 1.3 Project Vision Alignment

From `project-vision.md` and UIManager comments:

> **MARKET STANDARD PATTERN:** Single persistent UI manager that handles all navigation through **showing/hiding panels** rather than loading scenes. This is how Pokémon GO, Clash of Clans, and other top mobile games work.

We currently have **both** architectures: UIManager has panels, but Wallet/Settings are also separate scenes. That conflict causes the freeze.

---

## 2. Current vs Target Architecture

### 2.1 Current Flow (Problematic)

```
Login scene → MainMenu scene → [Tap Wallet] → Load Wallet SCENE → ❌ Touch freezes
                                      → [Tap Settings] → Load Settings SCENE → ❌ Touch freezes
                                      → [Tap Hunt] → Load ARHunt scene → ✅ (different use case)
```

- **MainMenu, Wallet, Settings** = separate Unity scenes, each with its own Canvas + EventSystem.
- QuickNavigation loads scenes via `SceneManager.LoadSceneAsync()`.
- Each scene load can break Input System on Android.

### 2.2 Target Flow (Market Standard)

```
Login scene → MainMenu scene (or “Shell”) → [Tap Wallet] → UIManager.ShowWallet() → ✅ No scene load
                                      → [Tap Settings] → UIManager.ShowSettings() → ✅ No scene load
                                      → [Tap Hunt] → Load ARHunt scene → ✅ (AR needs its own scene)
```

- **Wallet, Settings** = panels on UIManager’s Canvas, shown/hidden with `SetActive()`.
- **No scene load** for Wallet/Settings → no Input System bug.
- **ARHunt** stays as a scene (AR setup, GPS, etc.).

---

## 3. Implementation Plan

### Phase 1: Ensure UIManager Panels Exist and Work

1. **Audit UIManager panels**
   - `CreateWalletPanel()` and `CreateSettingsPanel()` already exist.
   - Ensure they contain all UI from the current Wallet/Settings scenes (balance, buttons, back, etc.).

2. **Wire MainMenu to use UIManager**
   - In MainMenu scene, Wallet and Settings buttons should call `UIManager.Instance.ShowWallet()` and `UIManager.Instance.ShowSettings()` instead of loading scenes via QuickNavigation.

3. **Ensure UIManager Canvas is active on MainMenu**
   - MainMenu currently “has its own UI,” so UIManager canvas is disabled.
   - Decide: either MainMenu uses UIManager’s canvas, or we keep MainMenu scene but have Wallet/Settings as **overlay panels** on top of MainMenu (additive, no scene load).

### Phase 2: Migrate Wallet and Settings to Panels

**Option A – UIManager-driven MainMenu (Recommended)**  
- MainMenu scene only provides background/lightweight shell.
- All main screens (MainMenu, Wallet, Settings) live as UIManager panels.
- One Canvas, one EventSystem for all of them.
- No scene load for Wallet/Settings.

**Option B – Hybrid: MainMenu scene + overlay panels**  
- MainMenu scene keeps its own Canvas for the main menu content.
- Wallet and Settings are UIManager panels shown as overlays (UIManager canvas enabled when showing Wallet/Settings).
- Requires careful EventSystem/Canvas management but avoids loading Wallet/Settings scenes.

**Option C – Single “Home” scene after login**  
- After login, load a “Home” scene that has UIManager with all panels.
- Login/Register remain scenes; ARHunt remains a scene.
- Home = MainMenu + Wallet + Settings as panels.

### Phase 3: Update Navigation

1. **QuickNavigation**
   - For Wallet and Settings buttons: call `UIManager.Instance.ShowWallet()` / `ShowSettings()` instead of `LoadSceneAsync("Wallet")` / `LoadSceneAsync("Settings")`.

2. **Back buttons**
   - In WalletUI and SettingsUI (when used as panels): call `UIManager.Instance.ShowMainMenu()` instead of `SceneLoader.LoadScene(SceneNames.MainMenu)`.

3. **Remove or repurpose Wallet/Settings scenes**
   - Optionally keep them for reference or as prefabs.
   - Or remove from build to avoid accidental use.

---

## 4. Scene vs Panel Decision Matrix

| Screen      | Keep as scene? | Reason |
|------------|----------------|--------|
| Login      | Yes            | Auth flow; can stay isolated. |
| Register   | Yes            | Auth flow. |
| MainMenu   | Yes (or merge) | Entry after login; can be shell or full UI. |
| **Wallet** | **No → panel** | Avoid scene load → avoid Input System freeze. |
| **Settings** | **No → panel** | Same as Wallet. |
| ARHunt     | Yes            | AR + GPS; heavy setup, separate scene makes sense. |

---

## 5. Quick Win (Minimal Change)

If full migration is too large at once:

1. **MainMenu scene** stays as-is.
2. **Add Wallet and Settings as overlay panels** to UIManager.
3. **Change QuickNavigation** for Wallet/Settings buttons:
   - If `sceneTarget == Wallet` → `UIManager.Instance.ShowWallet()` instead of `LoadSceneAsync`.
   - If `sceneTarget == Settings` → `UIManager.Instance.ShowSettings()` instead of `LoadSceneAsync`.
4. **Ensure UIManager canvas is enabled** when showing Wallet/Settings (may require adjusting `SceneHasOwnUI` / `OnSceneLoaded` logic so UIManager canvas can overlay MainMenu).

This keeps MainMenu scene but avoids loading Wallet/Settings scenes, which should eliminate the freeze.

---

## 6. Reference: Current Code Locations

| Component         | File                     | Change |
|-------------------|--------------------------|--------|
| Wallet button     | QuickNavigation.cs       | Call `ShowWallet()` instead of LoadScene |
| Settings button   | QuickNavigation.cs       | Call `ShowSettings()` instead of LoadScene |
| Wallet back       | WalletUI.cs              | Call `ShowMainMenu()` instead of LoadScene |
| Settings back     | SettingsUI.cs            | Call `ShowMainMenu()` instead of LoadScene |
| Panel API         | UIManager.cs             | Use existing `ShowWallet()`, `ShowSettings()`, `ShowMainMenu()` |
| Canvas enablement | UIManager.OnSceneLoaded  | Allow UIManager canvas to show over MainMenu when needed |

---

## 7. Next Steps

1. **Confirm approach** (Option A, B, or C).
2. **Ensure UIManager panels match Wallet/Settings scenes** (layout, data, buttons).
3. **Implement QuickNavigation changes** for Wallet and Settings.
4. **Update back-button handlers** in WalletUI and SettingsUI.
5. **Test on Android device** (Wallet and Settings must respond to touch).
6. **Optionally remove Wallet/Settings from build** once panels are validated.

---

## 8. Summary

| Before                    | After                            |
|---------------------------|-----------------------------------|
| Wallet/Settings = scenes  | Wallet/Settings = UIManager panels |
| Scene load → touch freeze | Show/hide panels → no scene load  |
| Multiple EventSystems     | One EventSystem for all panels    |
| Input System bug exposed  | Input System bug avoided          |

---

*This plan aligns Black Bart’s Gold with market-standard UI patterns and directly addresses the Wallet/Settings touch freeze by removing scene loads for those screens.*
