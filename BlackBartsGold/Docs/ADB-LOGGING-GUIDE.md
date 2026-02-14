# ADB Logging Guide – Black Bart's Gold

## Quick Reference

All diagnostic logs use the **`[BBG]`** prefix for easy filtering.

### Filter logs (Windows PowerShell)
```powershell
adb logcat -s Unity | Select-String "BBG"
```

### Filter logs (Windows CMD)
```cmd
adb logcat -s Unity | findstr "BBG"
```

### Filter logs (macOS/Linux)
```bash
adb logcat -s Unity | grep "\[BBG\]"
```

### Save logs to file
```powershell
adb logcat -s Unity | Select-String "BBG" | Tee-Object -FilePath bbg_logs.txt
```

### Clear logcat before starting (fresh capture)
```powershell
adb logcat -c
# Then run your app and capture
adb logcat -s Unity | Select-String "BBG"
```

---

## Log Tags

| Tag | Component |
|-----|-----------|
| `[BBG][Setup]` | ARHuntSceneSetup – panel creation, initialization |
| `[BBG][ARHUD]` | ARHUD – events, messages, state changes |
| `[BBG][BackButton]` | Back button tap |
| `[BBG][Radar]` | Radar/mini-map tap |
| `[BBG][DirectTouch]` | DirectTouchHandler – touch detection |
| `[BBG][Collect]` | CollectButtonController – collect flow |
| `[BBG][EmergencyBtn]` | EmergencyMapButton – fallback map button |

---

## What Gets Logged

- **Setup**: AR scene start, each panel created, ARHUD init, references wired
- **ARHUD**: Hunt mode changes, target set/cleared, messages shown, radar tapped
- **DirectTouch**: Touch detection, radar tap, full map open path
- **Collect**: Button shown/hidden, collect clicked, in range, locked, collection attempt

---

## Sharing Logs

To share logs for debugging:

1. Clear logcat: `adb logcat -c`
2. Launch the app and reproduce the issue
3. Capture: `adb logcat -s Unity | Select-String "BBG" | Out-File bb_logs.txt`
4. Share the `bb_logs.txt` file

---

## Verbose Mode

If you need even more detail, the Unity tag `Unity` also shows standard `Debug.Log` output. For full Unity logs:

```powershell
adb logcat -s Unity
```
