# 🗺️ Mapbox Setup for Black Bart's Gold

Map tiles (radar and full map) use the **Mapbox Static Images API**. You need a valid access token for maps to load.

## Quick Setup

1. **Get a token** from [Mapbox Account](https://account.mapbox.com/access-tokens/) (create a free account if needed).
2. **Copy the example file:**
   ```
   BlackBartsGold/Assets/Resources/MapboxToken.example.txt
   → BlackBartsGold/Assets/Resources/MapboxToken.txt
   ```
3. **Edit `MapboxToken.txt`** and replace `pk.your-mapbox-token-here` with your real token (starts with `pk.`).

`MapboxToken.txt` is in `.gitignore` so your token is never committed.

## Token Priority

MapboxService uses the token in this order:

1. **Inspector** – If you add MapboxService to a scene prefab and set `accessToken` there
2. **Resources/MapboxToken.txt** – Recommended for mobile builds (env vars often unavailable)
3. **Environment** – `MAPBOX_ACCESS_TOKEN` (works in Editor; may not work on device)

## Same Token as Admin Dashboard

If you use Mapbox in the admin dashboard, you can use the same token. It lives in `admin-dashboard/.env.local` as `NEXT_PUBLIC_MAPBOX_TOKEN`. Copy that value into `MapboxToken.txt` for the Unity app.

## Troubleshooting

- **401 Unauthorized** – Token missing or invalid. Ensure `MapboxToken.txt` exists and contains a valid `pk.*` token.
- **No maps showing** – Check ADB logs for `[Mapbox] hasValidToken=...` at startup.
- **"Skipping tile request - no valid token"** – MapboxService now skips requests when token is invalid (avoids 401 spam). Create `MapboxToken.txt` from the example and add your `pk.*` token.
