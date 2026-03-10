# 🖥️ Admin Dashboard

Tools for managing Black Bart's Gold as administrators.

---

## 👤 User Types

| Role | Capabilities |
|------|--------------|
| **Super Admin** (Us) | Full system access, hide coins, manage sponsors |
| **Sponsor Admin** | Hide coins with their logo, view their coin stats |
| **Regular User** | Play game, hide personal coins |

---

## 🪙 Coin Management

### Viewing Coins
- Filter by age (coins hidden 30+ days, 90+ days, 1+ year)
- Filter by value (all $50+ coins, all $100+ coins)
- Filter by status (found, unfound, pending)
- Filter by hider (system, user, sponsor)
- Map visualization of all coins

### Coin Actions
| Action | Use Case |
|--------|----------|
| **Retrieve** | Take back system coins that are stale |
| **Relocate** | Move a coin to different location |
| **Adjust Value** | Change system coin value (before found) |
| **Delete** | Remove coin entirely |
| **Flag as Mythical** | Mark for special promotion |

### Stale Coin Detection
```
Alert: 147 coins older than 90 days
  - 142 are 10¢-50¢ (likely uninteresting)
  - 5 are $5+ (possibly in hard locations)

Action options:
  [Retrieve All Under $1] [Review High Value] [Ignore]
```

---

## 🏔️ Mythical Coins

### What Are They?
High-value coins in challenging/famous locations:
- $2500 on Great Wall of China
- $1000 at Machu Picchu
- $500 on a remote mountain peak

### Admin Features
- Create mythical coin with custom description
- Set "protected" status (won't auto-recycle)
- Track attempts/nearby activity
- Promote in-app as legendary challenges

### Notifications
```
⚠️ High-Value Coin Alert
$400 coin on Pike's Peak has been unfound for 180 days
No players within 5 miles in last 30 days

[Create Treasure Hunt Event] [Feature in App] [Leave It]
```

---

## 📊 System Metrics

### Financial Dashboard
| Metric | Description |
|--------|-------------|
| Total Deposits | All user payments |
| Total Coins in System | Sum of all BBG in circulation |
| Total Payouts | All coins found |
| Profit Margin | (Deposits - Coins in Circulation) |
| Daily Gas Revenue | Consumed gas fees |

### Integrity Checks
```
✅ Deposits: $45,230
✅ Coins in System: $40,707 (90% as expected)
✅ No anomalies detected

OR

⚠️ ALERT: Coin count exceeds deposits!
   Expected max: $40,707
   Actual count: $41,200
   Discrepancy: $493

   → Review pending coins for fraud
```

### Activity Metrics
- Active users (daily/weekly/monthly)
- Coins found per day
- Average find value
- Grid activity heatmap
- New user retention

---

## 🎨 Coin Design Tools

### Creating Coins
- Set denomination
- Upload custom logo (for sponsor/user coins)
- Set hunt type
- Choose multi-find options
- Set location on map

### Bulk Creation
For events/sponsor hunts:
- Upload CSV of locations
- Set parameters for all
- Schedule release time
- Configure timed release (1 coin per X minutes)

---

## 👔 Sponsor Management

### Phase 1: We Create for Sponsors
1. Sponsor contacts us
2. We receive payment
3. We create coins with their logo
4. We place coins per their specs
5. We provide analytics

### Phase 2: Sponsor Self-Service
1. Sponsor creates account
2. Sponsor uploads logo
3. Sponsor pays for coins
4. Sponsor places coins via admin portal
5. Sponsor views own analytics

### Sponsor Dashboard (Their View)
- Their coins: placed, found, remaining
- Find statistics
- Engagement metrics
- ROI calculations

---

## 🚨 Security Monitoring

### Cheater Detection
- Velocity anomaly alerts
- GPS spoofing patterns
- Device integrity failures
- Unusual find patterns

### User Management
| Action | Use Case |
|--------|----------|
| **Suspend** | Pending investigation |
| **Ban** | Confirmed cheater |
| **Unban** | False positive |
| **Review History** | See all user activity |

### Fraud Dashboard
```
🚨 Potential Fraud Detected

User: suspicious_player_42
- Traveled 200 miles in 5 minutes
- 47 coins found in 1 hour
- Device shows emulator signatures

[Suspend Account] [Flag for Review] [Dismiss]
```

---

## 📍 Location Management

### Banned Zones
- Add/remove banned areas
- Draw polygons on map
- Import from external source
- Automatic water body detection

### Zone Types
| Zone | Status | Notes |
|------|--------|-------|
| Oceans/Lakes | Auto-banned | GPS + map data |
| Private Property | Allowed | User's choice |
| Schools | TBD | Research needed |
| Government Buildings | TBD | Research needed |

---

## 📈 Reports

### Scheduled Reports
- Daily: New users, coins found, revenue
- Weekly: Active user trends, popular areas
- Monthly: Full financial summary, growth metrics

### Custom Reports
- Date range selection
- Filter by region/grid
- Export to CSV/PDF
- User segment analysis

---

## 🔗 Related Documents

- [Safety & Legal Research](../safety-and-legal-research.md)
- [Economy & Currency](../economy-and-currency.md)
- [User Accounts & Security](../user-accounts-security.md)
