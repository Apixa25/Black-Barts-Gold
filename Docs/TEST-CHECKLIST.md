# 🧪 Black Bart's Gold - Device Test Checklist

## 📱 Test Device
- **Device**: OnePlus 9 Pro
- **OS**: Android
- **Build Date**: January 18, 2026
- **Phase**: MVP (Phase 1 Complete)

---

## 🔐 Test Scenario 1: New User Flow

### Onboarding
- [ ] App launches without crash
- [ ] Onboarding screen appears (first launch)
- [ ] "How It Works" button opens panel
- [ ] "Create Account" button navigates to Register
- [ ] "Login" button navigates to Login

### Registration
- [ ] Email field accepts input
- [ ] Password field accepts input (toggle visibility works)
- [ ] Confirm password field works
- [ ] Age dropdown works
- [ ] Terms checkbox works
- [ ] Validation shows errors for:
  - [ ] Invalid email format
  - [ ] Password too short (<8 chars)
  - [ ] Passwords don't match
  - [ ] Age not selected
  - [ ] Terms not accepted
- [ ] Successful registration → Main Menu

### Login
- [ ] Email field accepts input
- [ ] Password field accepts input
- [ ] Validation shows errors for invalid input
- [ ] "Create Account" link goes to Register
- [ ] Successful login → Main Menu

---

## 🏠 Test Scenario 2: Main Menu

### Display
- [ ] Player balance shows (test wallet)
- [ ] Gas status shows
- [ ] Username displays

### Navigation
- [ ] "Start Hunting" button works
- [ ] "Map" button works
- [ ] "Wallet" button works
- [ ] "Settings" button works

---

## 🎯 Test Scenario 3: AR Treasure Hunt

### AR Initialization
- [ ] Camera permission requested
- [ ] Camera activates
- [ ] "Looking for surfaces..." message appears
- [ ] AR tracking establishes

### AR HUD
- [ ] Crosshairs visible in center
- [ ] Gas meter visible
- [ ] Find limit display visible
- [ ] Compass shows (if coins nearby)

### Coin Spawning
- [ ] Test coins spawn (using TestCoinSpawner)
- [ ] Coins are visible in AR
- [ ] Coins animate (spin/bob)

### Coin Interaction
- [ ] Crosshairs change color when hovering coin
- [ ] Crosshairs turn green when in range
- [ ] Tapping collects coin (if in range)
- [ ] Collection popup shows with value
- [ ] Haptic feedback on collection

---

## 🔒 Test Scenario 4: Locked Coin

- [ ] Find a coin above limit (if available)
- [ ] Locked coins show red tint
- [ ] Tapping locked coin shows popup
- [ ] Popup shows "above your limit" message
- [ ] "Hide a Coin" button visible
- [ ] Cancel button closes popup

---

## ⛽ Test Scenario 5: Gas System

### Low Gas Warning
- [ ] Warning banner shows when gas < 15%
- [ ] Flashing animation works
- [ ] Dismiss button works
- [ ] Add Gas button navigates to Wallet

### No Gas Overlay
- [ ] When gas = 0, overlay shows
- [ ] "Ye've Run Aground!" message visible
- [ ] Buy Gas button works
- [ ] Unpark button shows (if has parked coins)
- [ ] Main Menu button works

---

## 💰 Test Scenario 6: Wallet

### Display
- [ ] Total balance shows
- [ ] Gas tank balance shows
- [ ] Parked balance shows
- [ ] Pending balance shows
- [ ] Gas days remaining shows

### Transaction History
- [ ] Transaction list visible
- [ ] Transactions show type icons
- [ ] Amounts formatted correctly

### Actions
- [ ] Park coins button works (if has found coins)
- [ ] Unpark coins button works (if has parked)
- [ ] Add gas button works

---

## 🗺️ Test Scenario 7: Map

- [ ] Map screen opens
- [ ] Player location shown (if GPS enabled)
- [ ] Coin markers show (if coins nearby)
- [ ] Back button returns to menu

---

## ⚙️ Test Scenario 8: Settings

- [ ] Settings screen opens
- [ ] Audio toggle works
- [ ] Haptics toggle works
- [ ] Logout button works
- [ ] Logout returns to Login screen

---

## 📶 Test Scenario 9: Network Status

- [ ] Network indicator shows
- [ ] Shows WiFi/Mobile/Offline correctly
- [ ] Sync button works (if pending actions)
- [ ] Auto-syncs when coming back online

---

## 🐛 Bugs Found

| # | Description | Severity | Screen |
|---|-------------|----------|--------|
| 1 | | | |
| 2 | | | |
| 3 | | | |
| 4 | | | |
| 5 | | | |

---

## ✅ Test Summary

| Category | Pass | Fail | Notes |
|----------|------|------|-------|
| Onboarding | | | |
| Registration | | | |
| Login | | | |
| Main Menu | | | |
| AR Hunt | | | |
| Locked Coins | | | |
| Gas System | | | |
| Wallet | | | |
| Map | | | |
| Settings | | | |
| Network | | | |

---

## 📝 Notes

*Add any additional observations here:*




---

*Test conducted on: ________________*
*Tester: ________________*
*Build version: MVP Phase 1*
