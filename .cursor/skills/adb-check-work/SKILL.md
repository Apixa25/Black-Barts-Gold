---
name: adb-check-work
description: Runs an ADB-first, evidence-driven debugging and verification workflow for Android Unity work. Use when implementing new features, fixing bugs, validating behavior changes, or when the user asks to pull/analyze logs and prove a change works step by step.
---

# ADB Check Work

## Purpose
Use this skill to verify code behavior with runtime evidence, not guesswork.

This workflow is generic and applies to:
- New features
- Bug fixes
- Regressions after refactors
- Intermittent runtime issues

## Core Operating Rules
1. **See first, change second**: gather runtime facts before proposing fixes.
2. **Instrument intentionally**: add scoped logs at decision points (`entry`, `decision`, `result`, `error`).
3. **Prove every step**: each change must be validated in logs.
4. **Stay additive by default**: prefer targeted patches, feature flags, and kill switches over broad deletions.
5. **Use user-flow checkpoints**: validate along app flow boundaries (Main Menu -> Start Hunting -> AR View) to avoid hidden regressions.

## Log Tag Contract
Use consistent tags so searches are deterministic:
- `[BBG][Area][Action]` for app/runtime events
- `[BBG][Verify]` for pass/fail checkpoints
- `[BBG][TapTrace]` for input-hit tracing
- `[BBG][Error]` for exceptional paths

Keep messages short and structured:
- include key state values (`before`, `after`, identifiers, mode/state)
- avoid long prose in high-frequency loops

## Standard Capture Commands (PowerShell)
Run from repo root. Do not clear logs unless user requests a fresh session.

```powershell
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = "C:\Users\Admin\Black-Barts-Gold\logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
adb devices
adb logcat -d -v time > "$logDir\bbg-$ts-all.log"
adb logcat -d -v time Unity:D *:S > "$logDir\bbg-$ts-unity.log"
adb logcat -d -v time | findstr /i "\[BBG\]" > "$logDir\bbg-$ts-bbg-only.log"
Write-Output "TIMESTAMP=$ts"
cmd /c dir "$logDir\bbg-$ts*.log"
```

Optional fresh-session prep (only if user requests):

```powershell
adb logcat -G 32M
adb logcat -c
```

## Required Debug Loop
Follow this loop for each bug/feature task:

1. **Define hypothesis**
   - State what is believed to be wrong/right.
2. **Add instrumentation**
   - Add logs around target functions and branches.
3. **Capture baseline**
   - Pull logs before behavior changes if possible.
4. **Apply minimal patch**
   - Keep change set focused.
5. **Capture verification run**
   - Pull logs immediately after reproducing scenario.
6. **Evaluate evidence**
   - Confirm expected events and state transitions are present.
7. **Decide next step**
   - Done only when logs prove success and no new critical errors appeared.

## Verification Checklist
For each iteration, explicitly verify:
- [ ] Expected function path executed
- [ ] Expected branch decisions made
- [ ] Expected state transitions occurred
- [ ] Expected UI/input events recorded (if relevant)
- [ ] No new `Exception`/`NullReferenceException`/`InvalidOperationException`
- [ ] No regression in adjacent flow steps

## Two-Phase Validation Rule
Validate both:
- **Happy path** (expected successful behavior)
- **Failure/guard path** (invalid state, missing data, or edge path)

Do not mark complete if only happy path is proven.

## Reporting Format
Return results in this order:
1. **Capture info**: device id, timestamp, files/sizes
2. **Critical findings**: crashes/exceptions first
3. **Behavior findings**: what was proven by logs
4. **Not proven yet**: explicit unknowns
5. **Next exact step**: the next capture or patch action

## Cleanup Rule
After issue resolution:
- keep durable low-noise lifecycle logs
- remove or reduce noisy temporary tracing
- retain high-value tags used for future triage

