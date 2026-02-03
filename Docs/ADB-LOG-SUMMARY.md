# ADB Log Summary – Black Bart's Gold APK (Jan 31, 2026)

## How to capture ADB logs

With the phone connected via USB and USB debugging enabled:

```bash
# Last N lines, filtered for Unity and app
adb logcat -d -t 2000 | findstr /i "Unity blackbart com.blackbart Debug UnityEngine"
```

Or for a live stream (clear first, then run app):

```bash
adb logcat -c
adb logcat -s Unity
```

---

## What the logs showed

### 1. App runs correctly on device

- **Package:** `com.blackbart.gold`
- **Device:** Google Pixel 6 (from logs)
- **Location:** GPS updates (e.g. 41.78, -124.19), `PlayerLocationService` and `GPSManager` are active
- **UserId:** `PlayerData.CurrentUser.id='test-user-001'` (fallback when not logged in)

### 2. Auth state

- **AuthService.Exists=False** appears repeatedly.
- So the user is **not logged in** via AuthService; the app correctly falls back to **PlayerData** and uses `test-user-001` for location and API calls.

### 3. Server / location API errors (500)

- **Endpoint:** `POST https://admin.blackbartsgold.com/api/v1/player/location`
- **Result:** **500** with message **"Database error"**
- **Retries:** ApiClient retries (e.g. 1/3, 2/3); after retries the app logs:
  - `[PlayerLocationService] API ERROR (failure #18)` (or similar count)
  - `Message: Database error`
  - `This may be a server-side issue.`
- So the **location update API is failing on the server** (database error). The Unity client and network path look fine; the fix is on the admin/backend side (e.g. Supabase or API route that writes location).

### 4. Flow in logs (summary)

- Location updates → `HandleLocationUpdated` → `GetUserId` (PlayerData.Exists=True, AuthService.Exists=False) → `SendLocationUpdateAsync` → POST /player/location → 500 → retries → API ERROR.

---

## Why you see Login / “Join the Crew” on the phone but not in the Editor

### Build order (first scene = Login)

In **ProjectSettings/EditorBuildSettings.asset** the scene order is:

1. **Login** (first – index 0)
2. Register  
3. MainMenu  
4. ARHunt  
5. Wallet  
6. Settings  
7. ARTest  

So when you **build and run the APK**, the app **starts in the Login scene**. That’s why you see:

- Login screen
- “Join the Crew!” (from `SimpleLoginController` / Login UI)
- Create Account / Back to Login, etc.

### In the Editor

- When you press **Play**, Unity starts from **whatever scene you have open** (often MainMenu or the last one you were editing).
- So you usually **don’t** start in Login in the Editor, and you don’t see those buttons unless you:
  - Open **Login** scene and press Play, or  
  - Change the Editor’s “first scene” / build index 0 to match the build.

So: **same code and same build order** — the difference is “which scene is index 0” and “which scene the Editor starts in when you hit Play.”

---

## What to work on next

1. **Login flow (as you said)**  
   - Wire up the Login scene (and “Join the Crew”) to your auth (e.g. Supabase/AuthService) so that after login, AuthService.Exists is true and you use a real user id instead of `test-user-001`.
   - Ensure after login the app goes to MainMenu (or intended next scene) and that `PlayerLocationService` / API use the logged-in user.

2. **Server 500 “Database error” on `/player/location`**  
   - Fix the backend (admin API + Supabase) so that POST `/api/v1/player/location` no longer returns 500.
   - Check admin-dashboard API route that handles `player/location` and the Supabase table/RLS/migrations for player location.

3. **Optional: align Editor with device**  
   - To always test the same flow as the APK, set the Editor to start from the **Login** scene (e.g. open Login.unity and press Play, or use a bootstrap that loads scene index 0).

---

## Auth / registration deployment checklist (Vercel + Supabase)

If registration returns "Please check your email to confirm" and login returns "Session has expired" (or "Invalid email or password"):

1. **Deploy admin-dashboard to Vercel** – The register route has auto-confirm logic; it must be deployed.
   - `git push` (if Vercel auto-deploys from main)
   - Or: Vercel Dashboard → Deployments → Redeploy latest

2. **Vercel environment variables** – Ensure these are set:
   - `NEXT_PUBLIC_SUPABASE_URL` – Supabase project URL
   - `NEXT_PUBLIC_SUPABASE_ANON_KEY` – Supabase anon key
   - `SUPABASE_SERVICE_ROLE_KEY` – **Required for auto-confirm** (admin API)

3. **Supabase profiles table** – Run migration `001_profiles_and_auth_trigger.sql` so new users get a profile row:
   - Supabase Dashboard → SQL Editor → paste migration SQL → Run
   - Or: `cd admin-dashboard && npm run supabase:db:push`

---

## Quick reference – useful log patterns

| What you care about     | Filter / pattern                          |
|-------------------------|-------------------------------------------|
| Unity app messages      | `adb logcat -s Unity`                     |
| Your app only (by PID)  | `adb logcat --pid=$(adb shell pidof com.blackbart.gold)` |
| Auth / location / API   | Look for `PlayerLocationService`, `ApiClient`, `AuthService` in Unity tag |
| Errors                  | Look for `W Unity` or `E Unity` and `API ERROR`, `Database error` |

---

## Wallet/Settings Debug Log Markers

When debugging the freeze, filter ADB logs for these markers:

| Marker | Meaning |
|--------|---------|
| `[QuickNavigation] 🔘 BUTTON CLICKED!` | User tapped Wallet or Settings - click reached handler |
| `[QuickNavigation] ✅ Scene loaded:` | Scene load completed |
| `[AppBootstrap] 📍 Scene loaded:` | AppBootstrap processing scene |
| `[AppBootstrap] 📱 SceneHasOwnUI=true` | Using scene's EventSystem (not persistent) |
| `[AppBootstrap]   → Using scene ES:` | Scene EventSystem enabled, has InputModule |
| `[AppBootstrap] 📍 EventSystem.current after setup:` | Active EventSystem name |
| `[UIManager] ✅ Scene 'Wallet' has its own UI` | UIManager preserved scene canvas |
| `[WalletUI] 🪙 Start` | WalletUI initialized |
| `[WalletUI] 🔙 BACK BUTTON CLICKED` | **Back button received click** (if missing = freeze) |
| `[SettingsUI] ⚙️ Start` | SettingsUI initialized |
| `[SettingsUI] 🔙 BACK BUTTON CLICKED` | **Back button received click** (if missing = freeze) |
| `[EventSystemFixer] Awake on` | Which EventSystem has fixer |
| `[EventSystemFixer] RefreshEventSystem` | Fixer refresh ran |

**If you see `[WalletUI] Start complete` but never `[WalletUI] 🔙 BACK BUTTON CLICKED` when tapping Back, the EventSystem is not delivering the click.**
