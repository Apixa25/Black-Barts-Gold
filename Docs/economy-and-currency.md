# 💰 Economy & Currency

The economic system of Black Bart's Gold is designed as a **closed-loop system** where all money eventually returns to the company as gas fees.

---

## 🪙 Black Bart Gold (BBG)

### Conversion Rates
| From | To | Rate |
|------|-----|------|
| 1 BBG | 1 USD | Fixed 1:1 |
| 1 USD | X Satoshi | Daily exchange rate |

**Note**: For MVP purposes, focus on BBG as abstract game currency. Bitcoin integration is a later concern.

### Denominations

#### Coin Denominations
| Value | Visual |
|-------|--------|
| 5¢ | Small copper-tinted coin |
| 10¢ | Small copper-tinted coin |
| 25¢ | Medium silver-tinted coin |
| 50¢ | Medium silver-tinted coin |
| $1 | Gold doubloon |
| $5 | Gold doubloon (larger) |
| $10 | Gold doubloon (larger, ornate) |
| $50 | Gold doubloon (very ornate) |
| $100 | Gold doubloon (legendary design) |

Smaller denominations (cents) allow for many more coins to be found, increasing engagement.

---

## ⛽ The Gas System

### How It Works
1. **User pays $10** → Gets 1 month of playtime
2. **Daily consumption** → ~$0.33 deducted at midnight
3. **Gas depletes** → Must pay again or use found coins
4. **Zero gas** → Prize Finder disabled

### Gas Source Priority
```
Purchased BBG (must be used as gas)
         ↓
    [CONSUMED DAILY]
         ↓
Found BBG can be:
  → Used as gas (extend playtime)
  → Parked in wallet (protected from gas consumption)
  → Hidden (raise find limit)
```

### Critical Rule: Purchased vs Found Coins

| Coin Type | Can Park? | Must Use as Gas? |
|-----------|-----------|------------------|
| **Purchased** ($10 buy-in) | ❌ No | ✅ Yes - consumed over month |
| **Found** (collected in game) | ✅ Yes | ❌ Optional - player's choice |

**Example Scenario**:
- User pays $10 to start
- Over the month, finds $3 in coins
- They can park that $3 in their wallet
- At month end: $10 consumed as gas, $3 sitting in wallet
- Prize Finder turns off (no gas)
- They still have $3 but can't play
- To play again: move $3 to gas OR buy more

### Parking Coins
- Only **found coins** can be parked
- Parked coins are protected from gas consumption
- Moving coins from parked → gas immediately charges 1 day's fee
- Useful for players taking a break

---

## 💼 Wallet System

### Wallet Balance Screen
Every player has a wallet showing:
- Total BBG balance
- Breakdown: Gas tank vs Parked coins
- Pending vs Confirmed coins
- Transaction history

### Pending vs Confirmed

| Status | Meaning | Duration |
|--------|---------|----------|
| **Pending** | Recently found, under verification | 24 hours |
| **Confirmed** | Verified, fully usable | Permanent |

**Why Pending?**
- Prevents GPS spoofing attacks
- Allows server cross-checks
- If total coins in system > total money received, we know someone cheated
- Suspicious pending coins can be investigated before confirmation

---

## 🔓 Finding Limits

### The Core Mechanic
Your **finding limit = your highest single CONTRIBUTION**

This applies to BOTH coin types:
- Fixed value coins: contribution = the coin value
- Pool/slot coins: contribution = what you put in the pool

| Action | Max Find |
|--------|----------|
| Nothing hidden | $1.00 |
| Hid $5 fixed coin | $5.00 |
| Contributed $5 to pool | $5.00 |
| Hid $25 fixed coin | $25.00 |
| Contributed $100 to pool | $100.00 |

### Key Rules
- **Based on YOUR contribution** (not what finder receives)
- **Based on single highest** (not cumulative)
- **Never decreases** - once unlocked, permanent
- **Displayed on Prize Finder** - always visible
- **Both coin types count equally** toward limit

### Example: Pool Contribution and Limits
```
User A contributes $10 to pool (slot machine coin)
  → User A's finding limit: $10 ✓
  → Doesn't matter what the finder gets paid

User B finds that coin, algorithm pays $15
  → User B's limit: UNCHANGED (they didn't hide/contribute)
  → User B just got lucky on the slot machine
```

### Finding Over-Limit Coins
When a player finds a coin above their limit:
1. Coin appears with a **glow** (not showing value)
2. Message: "Sorry, this coin is above your limit"
3. Hint shown: "Hide $X more to unlock" (hint = current coin value + $5)
4. Player cannot collect the coin

**Example**: Player with $5 limit finds a $20 coin
- Sees glowing coin (value hidden)
- Gets message about limit
- Hint: "Hide $25 to unlock higher finds"

---

## 💸 User-to-User Transfers

### Phase 1 (MVP)
- In-app transfers only
- Simple: User A sends X BBG to User B
- No external wallets
- Security monitoring for fraud

### Future Phases
- Potential BBG cryptocurrency token
- External wallet support
- More complex transfer mechanisms

---

## 🎰 Two Ways to Hide Coins

### Option 1: Fixed Value
- "I'm hiding exactly $10"
- Finder gets $10
- Creates races for known value
- Good for raising finding limit with certainty

### Option 2: Pool Contribution (Slot Machine)
- "I'm contributing $10 to the pool"
- Finder gets algorithm-determined amount
- Could be $2... or $50!
- Your $10 still counts toward YOUR finding limit
- Adds mystery and excitement

See [Dynamic Coin Distribution](./dynamic-coin-distribution.md) for full details.

---

## 🎲 Gambling Mechanics (Future)

### Hide-for-Bonus
- User hides a coin (e.g., $10)
- Sets duration (e.g., 1 hour)
- Sets proximity requirement (e.g., within 100 yards of themselves)
- If NO ONE finds it in that time: User gets $11 back
- If someone finds it: Finder gets it, hider gets nothing

**Risk/Reward**: Higher reward for longer durations or closer proximity

**Funding**: Bonus comes from company funds (our promotional cost)

---

## 📊 Economic Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    USER PAYS $10                            │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  $9 distributed as coins near user (dynamic cloud system)  │
│  $1 immediate gas fee (our revenue)                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  User finds coins → Can hide → Others find → They hide     │
│  Coins circulate through the player economy                │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Eventually ALL coins consumed as gas fees                  │
│  100% of money returns to company over time                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 💳 No Refunds Policy

- All purchases are final
- BBG has no guaranteed real-world value
- Bitcoin conversion is a service we provide, not a guarantee
- Terms of service will make this clear

---

## 🔗 Related Documents

- [Project Scope](./archive/project-scope.md)
- [Dynamic Coin Distribution](./dynamic-coin-distribution.md)
- [Coins & Collection](./coins-and-collection.md)
- [User Accounts & Security](./user-accounts-security.md)
