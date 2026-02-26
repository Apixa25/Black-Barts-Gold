---
name: adb-offline-capture
description: Capture and analyze Android ADB logs for Black Bart's Gold offline coin-hunt sessions. Use when the user says to run an ADB capture workflow, pull logs after reconnecting a phone, or analyze hunt telemetry/errors from logcat.
---

# ADB Offline Capture Skill

## When to Use This Skill
Use this skill when the user asks to:
- Capture device logs before/after an outdoor hunt
- Run a disconnected data collection session and pull logs on reconnect
- Analyze Unity/AR/GPS behavior from Android logcat
- Check for crashes, exceptions, GPS staleness, AR state transitions, and backend location uploads

---

## Project Context Guardrails (Black Bart's Gold)
- Canonical operational policy: `.cursor/rules/proactive-support-defaults.mdc`.
- Do not clear logs unless the user wants a fresh run.
- Prioritize AR hunt signals tied to product vision:
  - AR HUD/hunt flow stability
  - GPS reliability while moving
  - Coin search/selection loops
  - Crash-free runtime and successful location sync

---

## Standard Workflow

### 1) Verify device and create log output folder
Run in PowerShell:

```powershell
# Path: C:\Users\Admin\Black-Barts-Gold\logs\
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = "C:\Users\Admin\Black-Barts-Gold\logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
adb devices
```

### 2) Optional fresh-session prep (only when user asks)
```powershell
# Path: C:\Users\Admin\Black-Barts-Gold\logs\
adb logcat -G 32M
adb logcat -c
```

### 3) Pull three log views after reconnect
```powershell
# Path: C:\Users\Admin\Black-Barts-Gold\logs\
adb logcat -d -v time > "$logDir\bbg-$ts-all.log"
adb logcat -d -v time Unity:D *:S > "$logDir\bbg-$ts-unity.log"
adb logcat -d -v time | findstr /i "\[BBG\]" > "$logDir\bbg-$ts-bbg-only.log"
Get-ChildItem "$logDir\bbg-$ts*.log"
```

---

## Analysis Checklist

### A) Crash and fatal scan (highest priority)
Look for:
- `FATAL EXCEPTION`
- `ANR`
- `Fatal signal`
- `E/Unity`
- `Exception`, `NullReferenceException`, `MissingReferenceException`

### B) AR and hunt state scan
Look for:
- `SessionTracking`, `AR tracking working`
- `SWITCHING TO GYRO`
- `HuntMode`
- `SpawnTargetCoin`, target selection events
- `ARRaycastController`, `RadarUI`, map/open/close transitions

### C) GPS quality scan
Look for:
- `STALE_DEVICE_FIX`
- `MICRO_SKIP`
- movement deltas and stale count trends

### D) Backend sync scan
Look for:
- `SendLocationUpdateAsync START/END`
- `POST ... /player/location`
- `200` responses and response payloads
- excessive throttling loops

### E) UI artifact scan
Look for:
- missing glyph warnings (e.g., `Unicode value \u2715`, replacement with `\u25A1`)
- TextMeshPro font fallback messages

---

## Reporting Format
Return a short, structured summary:

1. **Capture info**: device id, timestamp, output files and sizes.
2. **Critical findings**: crashes/fatals first.
3. **Behavioral findings**: AR mode transitions, GPS staleness, API upload health.
4. **What this run does not show**: explicitly call out missing expected signals.
5. **Recommended next capture**: exact command plan for the next run.

---

## Fast Re-run Command Block
If the user asks for "run the skill now", execute:

```powershell
# Path: C:\Users\Admin\Black-Barts-Gold\logs\
$ts = Get-Date -Format "yyyyMMdd-HHmmss"; $logDir = "C:\Users\Admin\Black-Barts-Gold\logs"; New-Item -ItemType Directory -Path $logDir -Force | Out-Null; adb devices; adb logcat -d -v time > "$logDir\bbg-$ts-all.log"; adb logcat -d -v time Unity:D *:S > "$logDir\bbg-$ts-unity.log"; adb logcat -d -v time | findstr /i "\[BBG\]" > "$logDir\bbg-$ts-bbg-only.log"; Write-Output "TIMESTAMP=$ts"; cmd /c dir "$logDir\bbg-$ts*.log"
```

Then analyze the Unity log first, followed by all-log crash signals.
