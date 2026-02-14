# AR & Map UI Assets (Code-Only Setup)

AR and map UI elements are built **entirely from code** at runtime. No Unity Editor wiring required.

## Required Assets in `Assets/Resources/UI/`

| File | Purpose |
|------|---------|
| `crosshairs.jpg` | Crosshairs sprite (center targeting reticle) |
| `gold ring.png` | Collection size ring (shows when targeting a coin) |
| `map-coin-icon.png` | Coin markers on mini-map and full map |
| `player.png` | Player icon on mini-map and full map |

## Easiest Workflow

1. Put your assets in `Assets/` (anywhere)
2. Copy them to `Assets/Resources/UI/` for runtime loading
3. Code loads via `Resources.Load<Sprite>("UI/filename")`

## Fallback

If assets are missing, code uses colored squares/dots. Place assets in Resources/UI for the full experience.
