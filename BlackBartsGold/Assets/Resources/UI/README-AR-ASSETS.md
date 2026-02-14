# AR UI Assets (Code-Only Setup)

The AR crosshairs and collection size ring are built **entirely from code** at runtime. No Unity Editor wiring required.

## Required Assets

Place these in `Assets/Resources/UI/`:

| File | Purpose |
|------|---------|
| `crosshairs.jpg` | Crosshairs sprite (center targeting reticle) |
| `gold ring.png` | Collection size ring (shows when targeting a coin) |

## Loading Paths

- Crosshairs: `Resources.Load<Sprite>("UI/crosshairs")`
- Gold ring: `Resources.Load<Sprite>("UI/gold ring")`

## Fallback

If assets are missing, the crosshairs show as a gold square. Move your assets here for the full experience.
