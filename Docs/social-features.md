# 👥 Social Features

Community and multiplayer aspects of Black Bart's Gold.

---

## 📊 Leaderboards

### Types
| Leaderboard | Metric |
|-------------|--------|
| Most Coins Found | Total coins collected |
| Most Value Found | Total BBG collected |
| Most Coins Hidden | Total coins placed |
| Most Value Hidden | Total BBG placed |
| Weekly Champions | Weekly resets |
| All-Time Legends | Lifetime stats |

### Scope
- Global leaderboards
- Regional leaderboards (by country/state)
- Grid-based leaderboards (local competition)

---

## 👁️ Seeing Other Hunters

### Same-Coin Tracking
When a player selects a coin to chase:
- Other players who selected the SAME coin are visible
- See their location on mini-map
- Creates race dynamic

### How It Works
```
User A selects Coin #1234 → Visible to others chasing #1234
User B selects Coin #1234 → Sees User A on map
User C selects Coin #5678 → Cannot see A or B (different coin)
```

### Privacy Balance
- Only visible when chasing same coin
- General location only (not exact)
- Anonymous (no usernames shown while hunting)

---

## 👫 Friends List (Phase 2)

### Features
- Add friends by username
- See friends' activity (if they allow)
- Compare stats
- Challenge friends to races

### Friend Activity
- "[Friend] just found a $5 coin!"
- "[Friend] hid a coin near you"
- "[Friend] is hunting in your area"

---

## 🏰 Guilds & Teams (Phase 3)

### Concept
- Form groups with other players
- Team leaderboards
- Guild-based challenges
- Shared goals

### Features
- Guild chat
- Guild coin pool (optional contribution)
- Team hunts
- Territory control (advanced)

---

## 🗺️ Nearby Players

### Map View
- See anonymous player markers in your grid
- Know how crowded an area is
- Adds social proof / activity indication

### Privacy Options
- Can hide yourself from map
- Ghost mode (premium feature?)

---

## 📣 Notifications (Social)

| Notification | Description |
|--------------|-------------|
| Friend found coin | Your friend collected a coin |
| Friend hiding nearby | Friend placed a coin in your grid |
| Guild challenge | Team event starting |
| Leaderboard update | You moved up/down |

---

## 🎮 Pokémon GO Research Questions

### What Does Pokémon GO Do?
Need to research:
- [ ] How do they show nearby players?
- [ ] Friend system implementation
- [ ] Team/faction mechanics
- [ ] Social privacy controls
- [ ] Group raid system (similar to our team hunts?)

### Apply Learnings
- Don't reinvent the wheel
- Use proven social mechanics
- Adapt for treasure hunting context

---

## 💬 Communication (Future)

### Options to Consider
- In-app messaging (friends only)
- Quick emotes/reactions
- Voice chat for team hunts
- Push-to-talk nearby

### Moderation Concerns
- Report system needed
- Automated filtering
- Block/mute capabilities

---

## 🏆 Achievements & Badges

### Examples
| Badge | Requirement |
|-------|-------------|
| First Find | Collect your first coin |
| Treasure Hunter | Find 100 coins |
| High Roller | Find a $50+ coin |
| Generous Spirit | Hide 50 coins |
| Early Bird | Find a coin before 6 AM |
| Night Owl | Find a coin after midnight |
| Explorer | Visit 50 different grids |
| Mountain Goat | Find a coin at 5000+ ft elevation |

---

## 🔗 Related Documents

- [Prize Finder Details](./prize-finder-details.md)
- [Treasure Hunt Types](./treasure-hunt-types.md)
- [Project Scope](./project-scope.md)
