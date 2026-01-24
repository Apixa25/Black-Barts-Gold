# 🧪 Realtime Testing Checklist

## Quick Test Guide

### 1. Open Dashboard
- URL: `http://localhost:3000`
- Login: `stevensills2@gmail.com` / `123456`

### 2. Test Player Tracking (Realtime)
**Location**: Dashboard → Live Players Map widget

**What to Check:**
- [ ] Map loads without errors
- [ ] Connection status shows "connected" (if players exist)
- [ ] Console shows: "Realtime subscription status: SUBSCRIBED"
- [ ] If no players: Shows "No active players" message
- [ ] If players exist: They appear on map with markers

**Console Messages to Look For:**
```
Player location change: { eventType: 'INSERT' or 'UPDATE', ... }
Realtime subscription status: SUBSCRIBED
```

**Note**: Since we just switched to real data, you might see "No active players" if the `player_locations` table is empty. This is expected - the Realtime subscription is working, just waiting for data.

### 3. Test Anti-Cheat Panel (Realtime)
**Location**: Security → Anti-Cheat tab

**What to Check:**
- [ ] Panel loads without errors
- [ ] Stats cards display (may show 0s if no flags exist)
- [ ] Console shows: "Anti-cheat Realtime subscription status: SUBSCRIBED"
- [ ] Flagged Players table displays (may be empty)
- [ ] Recent Flags table displays (may be empty)

**Console Messages to Look For:**
```
Cheat flag change: { eventType: 'INSERT' or 'UPDATE', ... }
Anti-cheat Realtime subscription status: SUBSCRIBED
```

**Note**: If tables are empty, that's fine - Realtime is listening. When the SQL trigger detects cheating, new flags will appear automatically.

### 4. Test Realtime Connection Status

**Player Tracking:**
- Check browser console for subscription status
- Should see "SUBSCRIBED" after a few seconds
- If you see "CLOSED" or "CHANNEL_ERROR", check:
  - Realtime is enabled in Supabase Dashboard
  - Network tab shows WebSocket connection

**Anti-Cheat:**
- Same checks as above
- Both subscriptions should connect independently

### 5. Expected Behavior

**With Real Data (Empty Tables):**
- ✅ No errors in console
- ✅ "No active players" / "No flagged players" messages
- ✅ Realtime subscriptions show "SUBSCRIBED"
- ✅ Stats show 0s (correct for empty data)

**With Real Data (Populated Tables):**
- ✅ Players appear on map
- ✅ Flags appear in Anti-Cheat panel
- ✅ Updates happen automatically when data changes
- ✅ No page refresh needed

### 6. Troubleshooting

**If Realtime doesn't connect:**
1. Check Supabase Dashboard → Database → Replication
2. Verify `player_locations` and `cheat_flags` are enabled
3. Check browser console for WebSocket errors
4. Verify `.env.local` has correct Supabase URL and keys

**If you see errors:**
- Check browser console (F12)
- Check Network tab for failed requests
- Verify tables exist: `player_locations`, `cheat_flags`

### 7. Test Auto-Detection (Future)

When Unity app sends location updates:
1. SQL trigger runs automatically
2. If cheating detected → new flag created
3. Realtime subscription receives INSERT event
4. Anti-Cheat panel updates automatically
5. No page refresh needed!

---

## Success Criteria ✅

- [ ] Dashboard loads without errors
- [ ] Security/Anti-Cheat page loads
- [ ] Realtime subscriptions connect (check console)
- [ ] No console errors related to Realtime
- [ ] UI shows appropriate empty states (if no data)
- [ ] Stats calculate correctly (even if 0s)
