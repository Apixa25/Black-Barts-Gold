# 🔒 User Accounts & Security

Account management and anti-cheat systems for Black Bart's Gold.

---

## 👤 Account Creation

### Registration Options
| Method | Details |
|--------|---------|
| **Email + Password** | Standard registration |
| **Gmail Login** | OAuth integration |

### Required Information
- Email address
- Password (if not OAuth)
- Age (for future sponsor restrictions)

### Age Collection
- Used for segmenting users
- Future: Age verification for casino/alcohol sponsor hunts
- Method TBD (ID verification research needed)

---

## 💳 Subscription

### Pricing
- **$10/month** - only option
- No free tier
- No gas = no play

### Onboarding Flow

#### Ideal Flow (if technically feasible)
1. Download app
2. Create account
3. **Tutorial hunt for demo coin** (no payment required)
4. After tutorial: "Pay $10 to activate Prize Finder"
5. Payment processed
6. Full game unlocked

#### Minimum Viable Flow
1. Download app
2. Create account
3. Immediate payment required ($10)
4. Prize Finder activates
5. Start hunting

---

## 🛡️ Anti-Cheat Measures

### GPS Spoofing Detection
- Cross-reference location with cell towers
- Check for unrealistic movements
- Detect known spoofing apps
- Server-side location validation

### Velocity Checks
- Track movement speed
- Flag impossible speeds (>100mph sustained)
- **No teleporting** - can't jump between distant locations

### Device Integrity
- Check for rooted/jailbroken devices
- Detect emulators
- Hardware ID tracking
- App tampering detection

---

## ⚖️ Cheater Punishment

### Policy: Zero Tolerance
No demerit system. If we believe you're cheating: **immediate suspension**.

### What Gets Banned
| Identifier | Banned |
|------------|--------|
| Device (Hardware ID) | ✅ Yes |
| Email address | ✅ Yes |
| Phone number | ✅ Yes |
| Username | ✅ Yes |

### To Cheat Again, User Would Need
- New device (different hardware ID)
- New email address
- New phone number
- New $10 payment

This makes cheating expensive and tedious.

### Appeal Process
- Account suspended pending review
- User can contact support
- Review is manual
- No automated unbanning

---

## ✅ Pending vs Confirmed Coins

### The 24-Hour Verification Window

| Status | Duration | User Can |
|--------|----------|----------|
| **Pending** | 0-24 hours | See in wallet, cannot use |
| **Confirmed** | After 24 hours | Use, hide, transfer |

### Why Pending?
Prevents coin spoofing attacks:

```
Server knows: Total user deposits = $10,000
Server counts: Total coins in system = $10,500

ERROR: $500 of coins were spoofed somehow!

With 24-hour pending:
- Suspicious coins caught before confirmation
- Investigation can occur
- Fraudulent coins removed
```

### What Triggers Confirmation?
- 24 hours pass without flags
- No system anomalies detected
- Server cross-check passes

---

## 🚫 Dispute Resolution

### Current Policy: None

**Reasoning**:
- Everyone would claim they were cheated
- "I found it but got robbed!" - unverifiable
- Would cost more to investigate than coins are worth
- Creates exploit opportunities

### What We Tell Users
- "First to collect wins"
- Server timestamp is final
- No appeals on coin collection disputes

### Future Consideration
- May implement limited disputes for high-value coins ($50+)
- Would require significant evidence
- Manual review only

---

## 📍 GPS Accuracy

### Device Dependent
- Use whatever accuracy the device provides
- Typical smartphone: 3-10 meter accuracy
- Same standard as Pokémon GO

### Collection Range
- 10-30 feet (3-10 meters)
- As close as device GPS allows
- Cannot be cheated by claiming "I was there"

---

## 🔐 Data Security

### What We Store
- Account credentials (hashed passwords)
- Email, phone, age
- Transaction history
- Location history (for pattern analysis)
- Device identifiers

### Protection Measures
- Encrypted database
- HTTPS everywhere
- No plain-text passwords
- Regular security audits

---

## 📲 Push Notifications

### Types
| Notification | Trigger |
|--------------|---------|
| Coin placed near you | System places coin in your grid |
| Your coin was found | Someone collected your hidden coin |
| Gas running low | 3 days remaining |
| Low fuel alert | 15% gas remaining |

### Spam Prevention
- Limit frequency
- User can customize preferences
- Critical only mode available

---

## 🔗 Related Documents

- [Economy & Currency](./economy-and-currency.md)
- [Safety & Legal Research](./safety-and-legal-research.md)
- [Admin Dashboard](./admin-dashboard.md)
