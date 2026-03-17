// ============================================================================
// BBGTheme.cs
// Black Bart's Gold — Central UI Theme (ScriptableObject)
// Path: Assets/Scripts/UI/Theme/BBGTheme.cs
// ============================================================================
// Single source of truth for every color, spacing, and style token in the app.
// Wild West + Steampunk base palette infused with neon temporal energy.
//
// Create asset: Right-click in Project → Create → Black Bart's Gold → UI Theme
// Place in Resources/ folder named "BBGTheme" for auto-loading.
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.UI
{
    [CreateAssetMenu(fileName = "BBGTheme", menuName = "Black Bart's Gold/UI Theme")]
    public class BBGTheme : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        //  PRIMARY COLORS — the core brand identity
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ PRIMARY COLORS ═══")]

        [Tooltip("#FFD700 — Main accent: coins, CTAs, highlights")]
        public Color treasureGold = new Color(1f, 0.843f, 0f);

        [Tooltip("#B87333 — Steampunk metal: borders, frames, Chrono-Compass")]
        public Color brass = new Color(0.722f, 0.451f, 0.2f);

        [Tooltip("#E25822 — Time powers, energy effects, urgency")]
        public Color fireOrange = new Color(0.886f, 0.345f, 0.133f);

        [Tooltip("#8B4513 — Headers, navigation, accents")]
        public Color saddleBrown = new Color(0.545f, 0.271f, 0.075f);

        // ═══════════════════════════════════════════════════════════════
        //  SURFACE COLORS — backgrounds and containers
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ SURFACE COLORS ═══")]

        [Tooltip("#3D2914 — Primary backgrounds, deep and rich")]
        public Color darkLeather = new Color(0.239f, 0.161f, 0.078f);

        [Tooltip("#2A1A0E — Deepest backgrounds, charred saloon walls")]
        public Color woodDark = new Color(0.165f, 0.102f, 0.055f);

        [Tooltip("#F5E6D3 — Cards, text areas, old paper feel")]
        public Color parchment = new Color(0.961f, 0.902f, 0.827f);

        [Tooltip("#D2B48C — Lighter backgrounds, hover states")]
        public Color warmTan = new Color(0.824f, 0.706f, 0.549f);

        [Tooltip("#2D2D2D — Dark-mode backgrounds")]
        public Color darkCharcoal = new Color(0.176f, 0.176f, 0.176f);

        [Tooltip("#4A4A4A — Secondary dark surfaces")]
        public Color warmGray = new Color(0.29f, 0.29f, 0.29f);

        // ═══════════════════════════════════════════════════════════════
        //  NEON / TEMPORAL ENERGY — the Chrono-Compass bleed-through
        //  HDR-enabled so glow shaders can push intensity beyond 1.0
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ NEON / TEMPORAL ENERGY ═══")]

        [ColorUsage(true, true)]
        [Tooltip("#00FFE5 — Tesla energy, tech highlights, time-displacement crackle")]
        public Color neonCyan = new Color(0f, 1f, 0.898f);

        [ColorUsage(true, true)]
        [Tooltip("#FFBF00 — Warm energy, proximity signals, Chrono-Compass dial")]
        public Color neonAmber = new Color(1f, 0.749f, 0f);

        [ColorUsage(true, true)]
        [Tooltip("#FF00AA — Rare/epic events, temporal anomaly warnings")]
        public Color neonMagenta = new Color(1f, 0f, 0.667f);

        // ═══════════════════════════════════════════════════════════════
        //  SEMANTIC COLORS — state communication
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ SEMANTIC COLORS ═══")]

        [Tooltip("#39FF14 — Collection confirmed, valid states (neon green)")]
        public Color success = new Color(0.224f, 1f, 0.078f);

        [Tooltip("#FF1744 — Errors, locked, low gas (hot neon red)")]
        public Color danger = new Color(1f, 0.09f, 0.267f);

        [Tooltip("#FFD740 — Caution states (amber flash)")]
        public Color warning = new Color(1f, 0.843f, 0.251f);

        // ═══════════════════════════════════════════════════════════════
        //  COIN TIER COLORS
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ COIN TIERS ═══")]

        [Tooltip("#FFD700")]
        public Color coinGold = new Color(1f, 0.843f, 0f);

        [Tooltip("#C0C0C0")]
        public Color coinSilver = new Color(0.753f, 0.753f, 0.753f);

        [Tooltip("#CD7F32")]
        public Color coinBronze = new Color(0.804f, 0.498f, 0.196f);

        [Tooltip("#E5E4E2")]
        public Color coinPlatinum = new Color(0.898f, 0.894f, 0.886f);

        [Tooltip("#B9F2FF")]
        public Color coinDiamond = new Color(0.725f, 0.949f, 1f);

        // ═══════════════════════════════════════════════════════════════
        //  UTILITY COLORS
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ UTILITY ═══")]

        public Color semiTransparentBlack = new Color(0f, 0f, 0f, 0.7f);
        public Color transparentBlack = new Color(0f, 0f, 0f, 0.5f);
        public Color opaqueBlack = new Color(0f, 0f, 0f, 0.9f);
        public Color fullWhite = Color.white;

        // ═══════════════════════════════════════════════════════════════
        //  SPACING TOKENS (dp / pixels at 1x scale)
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ SPACING ═══")]

        public float spacingXs = 4f;
        public float spacingSm = 8f;
        public float spacingMd = 16f;
        public float spacingLg = 24f;
        public float spacingXl = 32f;

        // ═══════════════════════════════════════════════════════════════
        //  CORNER RADIUS
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ CORNER RADIUS ═══")]

        [Tooltip("Input fields, small cards")]
        public float cornerRadiusSm = 6f;

        [Tooltip("Buttons, panels")]
        public float cornerRadiusMd = 12f;

        [Tooltip("Modal popups, overlays")]
        public float cornerRadiusLg = 20f;

        // ═══════════════════════════════════════════════════════════════
        //  BORDER WIDTH
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ BORDER WIDTH ═══")]

        [Tooltip("Subtle borders")]
        public float borderThin = 1.5f;

        [Tooltip("Standard button/card borders")]
        public float borderNormal = 3f;

        [Tooltip("Primary CTA, focus states")]
        public float borderThick = 5f;

        // ═══════════════════════════════════════════════════════════════
        //  BUTTON ANIMATION
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ BUTTON ANIMATION ═══")]

        [Tooltip("Scale when pressed (< 1 = shrink)")]
        [Range(0.85f, 1f)]
        public float buttonPressScale = 0.95f;

        [Tooltip("Scale on hover/focus (> 1 = grow)")]
        [Range(1f, 1.15f)]
        public float buttonHoverScale = 1.05f;

        [Tooltip("Press animation duration (seconds)")]
        [Range(0.05f, 0.3f)]
        public float buttonPressDuration = 0.15f;

        [Tooltip("Glow intensity multiplier on hover")]
        [Range(1f, 3f)]
        public float buttonHoverGlowIntensity = 1.5f;

        [Tooltip("Idle breathing glow cycle (seconds)")]
        [Range(1f, 6f)]
        public float idleBreatheCycle = 3f;

        // ═══════════════════════════════════════════════════════════════
        //  GLOW SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ GLOW ═══")]

        [ColorUsage(true, true)]
        [Tooltip("Default glow color for interactive elements")]
        public Color defaultGlowColor = new Color(0f, 1f, 0.898f, 0.6f);

        [Tooltip("Glow spread in pixels")]
        [Range(2f, 20f)]
        public float glowSpread = 8f;

        [Tooltip("Default glow intensity")]
        [Range(0.5f, 3f)]
        public float glowIntensity = 1.2f;

        // ═══════════════════════════════════════════════════════════════
        //  TRANSITION SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("═══ TRANSITIONS ═══")]

        [Tooltip("Panel open/close animation duration (seconds)")]
        [Range(0.1f, 0.5f)]
        public float panelTransitionDuration = 0.3f;

        [Tooltip("Screen transition flash duration (seconds)")]
        [Range(0.1f, 0.4f)]
        public float screenTransitionDuration = 0.2f;

        [Tooltip("Error shake cycles")]
        [Range(1, 5)]
        public int errorShakeCycles = 3;

        [Tooltip("Error shake duration per cycle (seconds)")]
        [Range(0.02f, 0.1f)]
        public float errorShakeDuration = 0.05f;

        // ═══════════════════════════════════════════════════════════════
        //  HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a copy of the color with a different alpha.
        /// </summary>
        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Returns the color associated with a player tier name.
        /// Supports both old tier names and new Western-themed names.
        /// </summary>
        public Color GetTierColor(string tierName)
        {
            if (string.IsNullOrEmpty(tierName)) return coinBronze;

            switch (tierName.ToLowerInvariant().Trim())
            {
                case "cabin boy":
                case "greenhorn":
                case "tenderfoot":
                    return coinBronze;

                case "deck hand":
                case "prospector":
                case "cowhand":
                    return coinSilver;

                case "treasure hunter":
                case "wrangler":
                    return coinGold;

                case "captain":
                case "trail boss":
                case "outlaw":
                    return coinPlatinum;

                case "king of pirates":
                case "frontier legend":
                case "gold rush king":
                case "marshal":
                    return coinDiamond;

                default:
                    return coinBronze;
            }
        }

        /// <summary>
        /// Converts a hex string (#RRGGBB or #RRGGBBAA) to a Unity Color.
        /// </summary>
        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            Debug.LogWarning($"[BBGTheme] Failed to parse hex color: {hex}");
            return Color.magenta;
        }
    }
}
