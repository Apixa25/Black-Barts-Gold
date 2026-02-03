# Current Bugs and Current Fix Ideas

Track bugs and planned fixes so we can work through them in order. Update this file as we fix or re-prioritize.

**Last updated:** 2026-02-02

---

## ✅ Fixed

### 1. Debug info panel stuck on "Loading..."
- **Symptom:** In AR view, the "DEBUG INFO" window never showed live data; it stayed on "Loading...".
- **Cause:** `UIManager.isInARMode` was only set in `StartARHunt()`, but "Start Hunting" uses `SceneLoader.LoadScene(ARHunt)` so `isInARMode` stayed false. `Update()` only calls `UpdateDiagnosticsPanel()` when `isInARMode` is true.
- **Fix:** In `UIManager.OnSceneLoaded`, when `scene.name == "ARHunt"`, set `isInARMode = true` so the debug panel updates every 0.5s.
- **File:** `Assets/Scripts/Core/UIManager.cs`

### 2. My Wallet freezes after Back from AR
- **Symptom:** From AR view → Back → Main Menu → tap "My Wallet" → app freezes/hangs.
- **Cause:** When loading Wallet (or Settings), UIManager disables its own canvas and preserves the scene’s Canvas, but the **persistent EventSystem** (from AppBootstrap) was not re-targeting the new scene’s Canvas. Input continued to target the old/disabled UI, so the Wallet screen appeared frozen (no touch/click response).
- **Fix:** In `UIManager.OnSceneLoaded`, when `SceneHasOwnUI(scene.name)` is true (Wallet, Settings, etc.), start a one-frame coroutine that then calls `EventSystemFixer.RefreshEventSystem()`. This forces the EventSystem to refresh so it retargets the new scene’s Canvas.
- **File:** `Assets/Scripts/Core/UIManager.cs` (added `RefreshEventSystemNextFrame()` and call when scene-with-own-UI loads).

### 3. Login bypasses real auth
- **Symptom:** First screen shows "Login" and "Join the Crew"; tapping Login went straight to Main Menu with no email/password.
- **Fix:** AuthService init in AppBootstrap; SimpleLoginController shows LoginControllerUIToolkit (email/password form) when Login clicked; form calls AuthService + dashboard API.
- **Files:** `AppBootstrap.cs`, `SimpleLoginController.cs`, `LoginControllerUIToolkit.cs`.

### 4. "Join the Crew" shows "Registration coming soon!"
- **Symptom:** Tapping "Join the Crew" showed placeholder instead of real registration.
- **Fix:** RegisterButtonHandler now creates full registration form and calls AuthService.Register + dashboard API.
- **Files:** `RegisterButtonHandler.cs`.

---

## 🔴 Open bugs (in order)

### 5. Settings freezes / does nothing
- **Symptom:** From Main Menu (or after Back from AR), tap "Settings" → app hangs or nothing happens.
- **Fix ideas:**
  - Same as Wallet: capture ADB logs during freeze; look for null refs or blocking calls in SettingsUI.
  - Check Settings scene: SerializeField refs, Canvas, EventSystem.
  - Check SettingsUI.Start/OnEnable for blocking or missing refs.
- **Files to check:** `Assets/Scripts/UI/SettingsUI.cs`, Settings scene setup.

### 6. Backend 500 on player location
- **Symptom:** POST `/api/v1/player/location` returns 500 "Database error"; app retries and logs API errors (see ADB-LOG-SUMMARY.md).
- **Fix:** Backend/admin-dashboard + Supabase: fix the player location API route and/or DB (table, RLS, migrations). Not an APK code change.
- **References:** `Docs/ADB-LOG-SUMMARY.md`, admin-dashboard API and Supabase migrations.

---

## 📋 Debug workflow

1. **Reproduce** the bug (device or editor).
2. **Capture ADB logs** when relevant: `adb logcat -c` then `adb logcat -s Unity`; trigger the bug; save output.
3. **Fix** one bug at a time; retest.
4. **Update this file:** move fixed items to "Fixed" and add new bugs/fix ideas as we find them.

---

## Quick reference – ADB

```bash
# Clear then stream Unity logs
adb logcat -c
adb logcat -s Unity

# Or filter for app + keywords
adb logcat -d -t 2000 | findstr /i "Unity blackbart com.blackbart Debug UnityEngine"
```
