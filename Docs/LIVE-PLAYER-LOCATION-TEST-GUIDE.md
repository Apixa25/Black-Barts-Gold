# 📍 Live Player Location - Test Guide

> **Purpose**: Step-by-step guide to verify real-time player tracking works end-to-end (Unity App → API → Supabase → Admin Dashboard).

---

## ✅ Prerequisites (You've Done These!)

- [x] Migration `003_player_locations.sql` run in Supabase SQL Editor
- [x] Super admin role confirmed for your user
- [x] Player registration and login working in Unity app

---

## 🧪 Test Plan

### Step 1: Start the Admin Dashboard

```powershell
cd c:\Users\Admin\Black-Barts-Gold\admin-dashboard
npm run dev
```

- Open **http://localhost:3000**
- Log in with your super_admin account
- Go to the **Dashboard** (main page)

### Step 2: Verify Dashboard Loads

On the Dashboard you should see:

- **Live Players** card with map
- Connection badge: **Live** (green) or **Connecting** (yellow) or **Offline** (red)
- Map loads without errors
- If no players yet: Empty map or "No active players" — **this is expected!**

**Browser Console (F12):**
- Look for: `Realtime subscription status: SUBSCRIBED`
- No errors about `player_locations` or permissions

### Step 3: Deploy/Build the Unity APK (If Needed)

Your Pixel 6 will hit the **Production** API by default:
`https://admin.blackbartsgold.com/api/v1`

**Option A – Production (recommended for device):**
- Ensure admin dashboard is deployed to Vercel at `admin.blackbartsgold.com`
- Build APK and install on Pixel 6
- App will use Production mode automatically on device

**Option B – Local Dev (for testing without deployment):**
- Unity app on device cannot use `localhost`
- Set dev server IP: In Unity, use Settings/Debug menu to call `ApiConfig.SetDevServerIP("YOUR_PC_IP", 3000)`
- Your PC and phone must be on the same WiFi
- Find your IP: `ipconfig` (Windows) → IPv4 Address

### Step 4: Run the Unity App on Pixel 6

1. **Launch the app** on your Pixel 6
2. **Log in** with a registered account (required for `userId`)
3. **Start a treasure hunt** (AR Hunt scene) — this starts GPS and `PlayerLocationService`
4. **Allow location permission** when prompted
5. **Move around** (or stand still) — location updates every ~5 seconds

### Step 5: Watch the Admin Dashboard

- Keep the Dashboard open in your browser
- Within **~10–30 seconds** you should see:
  - Your player marker appear on the map
  - Stats: "1 active" (or "1 idle" if no recent update)
  - Connection badge: **Live**
- **Walk around** — marker should update in near real-time (within a few seconds)

---

## 🔍 Troubleshooting

### "No players showing"

| Check | Action |
|-------|--------|
| User logged in? | Must log in before starting AR hunt |
| GPS started? | Enter AR Hunt scene; GPS starts with coin fetch |
| API reachable? | On device, Production URL must be deployed and reachable |
| player_locations table? | Confirm migration ran: Supabase → Table Editor → `player_locations` |

### "Permission denied" or RLS errors

- Verify your admin user has `super_admin` in `profiles.role`
- Log out and log back in to refresh session

### Unity logs to look for

Use **Unity MCP** `read_console` or **logcat** (Android) for:

- `[PlayerLocationService] ✅ Subscribed to PlayerData.OnLocationUpdated`
- `[PlayerLocationService] 🚀 Initiating location send...`
- `[PlayerLocationService] ✅✅✅ SUCCESS! Location #1 sent!`
- `[GPSManager] PlayerLocationService initialized for real-time tracking`

### API errors in Unity

- `TABLE_NOT_FOUND` → Run `003_player_locations.sql` again
- `USER_NOT_FOUND` → Ensure `userId` in request matches a row in `profiles`
- Network error → Check internet; device and PC (if using dev server) must reach the API

---

## 📁 Key Files

| Component | File |
|-----------|------|
| Database | `admin-dashboard/supabase/migrations/003_player_locations.sql` |
| API | `admin-dashboard/src/app/api/v1/player/location/route.ts` |
| Dashboard Hook | `admin-dashboard/src/hooks/use-player-tracking.ts` |
| Unity Service | `BlackBartsGold/Assets/Scripts/Core/PlayerLocationService.cs` |
| GPS/Location | `BlackBartsGold/Assets/Scripts/Location/GPSManager.cs` |

---

## 🎯 Success Criteria

- [ ] Admin Dashboard loads without errors
- [ ] Realtime shows `SUBSCRIBED` in console
- [ ] Unity app logs "SUCCESS! Location sent!"
- [ ] Player marker appears on dashboard map
- [ ] Moving in app updates marker position within seconds

---

*Last updated: Feb 3, 2025*
