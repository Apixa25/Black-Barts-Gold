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

### 5. Login 401 shows "Session has expired" instead of real error
- **Symptom:** Invalid credentials or unconfirmed email → user saw "Session has expired".
- **Fix:** ApiClient.HandleAuthError now uses backend error message for login failures (invalid credentials, verify email) instead of always returning SessionExpired.
- **File:** `Assets/Scripts/Core/ApiClient.cs`

---

## 🔴 Open bugs (in order)

### 6. Registration returns "check your email" (no email received)
- **Symptom:** Create Account succeeds but shows "Please check your email to confirm"; no email arrives; login fails.
- **Cause:** Either (a) admin-dashboard not deployed to Vercel with auto-confirm, or (b) `SUPABASE_SERVICE_ROLE_KEY` missing in Vercel, or (c) Supabase email confirmation is on and auto-confirm fails.
- **Fix:** Deploy admin-dashboard; ensure `SUPABASE_SERVICE_ROLE_KEY` in Vercel env; run migration `001_profiles_and_auth_trigger.sql` if profiles table missing.
- **See:** `Docs/ADB-LOG-SUMMARY.md` – Auth deployment checklist.

### 7. Settings / My Wallet freeze (fix v2 attempted 2026-02-02)
- **Symptom:** From Main Menu, tap "Settings" or "My Wallet" → scene loads but screen appears frozen (no touch/click response).
- **Cause (v1):** Stale selection, EventSystem refresh – didn't fix it.
- **Cause (v2):** Persistent EventSystem + InputSystemUIInputModule can lose touch after scene load (known Unity issue). Reusing the same EventSystem across scenes causes touch to stop.
- **Fix v2 applied:** For scenes with own UI (MainMenu, Wallet, Settings, Login, Register), **do NOT destroy** the scene's EventSystem. Instead, **disable the persistent one** and let the scene use its own fresh EventSystem. Each scene gets a newly instantiated EventSystem, avoiding the "second scene load" touch bug.
- **Files:** `AppBootstrap.cs`.
- **Test:** Rebuild APK, tap My Wallet (Back should work), tap Settings (Back should work).

### 8. Backend 500 on player location
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
