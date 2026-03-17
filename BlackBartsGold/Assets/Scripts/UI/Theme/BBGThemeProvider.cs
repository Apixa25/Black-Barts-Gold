// ============================================================================
// BBGThemeProvider.cs
// Black Bart's Gold — Global Theme Access
// Path: Assets/Scripts/UI/Theme/BBGThemeProvider.cs
// ============================================================================
// Static access point for the BBGTheme ScriptableObject.
// Loads from Resources/BBGTheme automatically. Falls back to runtime defaults
// if no asset exists yet, so the app always works.
//
// Usage examples:
//   Color gold  = BBGThemeProvider.Gold;
//   Color bg    = BBGThemeProvider.DarkLeather;
//   float pad   = BBGThemeProvider.Current.spacingMd;
//   Color tier  = BBGThemeProvider.Current.GetTierColor("Trail Boss");
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.UI
{
    public static class BBGThemeProvider
    {
        private static BBGTheme _current;

        /// <summary>
        /// The active theme instance. Auto-loads from Resources/BBGTheme
        /// on first access. Creates runtime defaults as fallback.
        /// </summary>
        public static BBGTheme Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        /// <summary>
        /// Force-reload the theme from Resources.
        /// Useful after hot-reload in the Unity Editor.
        /// </summary>
        public static void Reload()
        {
            _current = null;
            Load();
        }

        private static void Load()
        {
            _current = Resources.Load<BBGTheme>("BBGTheme");

            if (_current != null)
            {
                Debug.Log("[BBGTheme] ✅ Loaded theme asset from Resources/BBGTheme");
            }
            else
            {
                _current = ScriptableObject.CreateInstance<BBGTheme>();
                Debug.LogWarning(
                    "[BBGTheme] ⚠️ No BBGTheme asset found in Resources/. Using runtime defaults.\n" +
                    "Create one via: Right-click in Project → Create → Black Bart's Gold → UI Theme\n" +
                    "Then move it to Assets/Resources/ and rename to BBGTheme.");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  PRIMARY — quick access to the most-used brand colors
        // ─────────────────────────────────────────────────────────────

        public static Color Gold        => Current.treasureGold;
        public static Color Brass       => Current.brass;
        public static Color FireOrange  => Current.fireOrange;
        public static Color SaddleBrown => Current.saddleBrown;

        // ─────────────────────────────────────────────────────────────
        //  SURFACE — backgrounds and containers
        // ─────────────────────────────────────────────────────────────

        public static Color DarkLeather  => Current.darkLeather;
        public static Color WoodDark     => Current.woodDark;
        public static Color Parchment    => Current.parchment;
        public static Color WarmTan      => Current.warmTan;
        public static Color DarkCharcoal => Current.darkCharcoal;
        public static Color WarmGray     => Current.warmGray;

        // ─────────────────────────────────────────────────────────────
        //  NEON — the temporal energy that bleeds through BB's world
        // ─────────────────────────────────────────────────────────────

        public static Color NeonCyan    => Current.neonCyan;
        public static Color NeonAmber   => Current.neonAmber;
        public static Color NeonMagenta => Current.neonMagenta;

        // ─────────────────────────────────────────────────────────────
        //  SEMANTIC — state communication
        // ─────────────────────────────────────────────────────────────

        public static Color Success => Current.success;
        public static Color Danger  => Current.danger;
        public static Color Warning => Current.warning;

        // ─────────────────────────────────────────────────────────────
        //  UTILITY
        // ─────────────────────────────────────────────────────────────

        public static Color SemiTransparent => Current.semiTransparentBlack;
        public static Color Transparent     => Current.transparentBlack;
        public static Color OpaqueBlack     => Current.opaqueBlack;

        // ─────────────────────────────────────────────────────────────
        //  LEGACY COMPATIBILITY
        //  Maps old color names used across existing scripts to the
        //  new theme tokens. Lets existing code migrate incrementally.
        // ─────────────────────────────────────────────────────────────

        /// <summary>Old name from UIManager/LoginSetup. Use Gold instead.</summary>
        public static Color GoldColor => Current.treasureGold;

        /// <summary>Old name from UIManager/LoginSetup. Use DarkLeather instead.</summary>
        public static Color DarkBrown => Current.darkLeather;

        /// <summary>Old name from UIManager. Kept for backward compatibility.</summary>
        public static Color DeepSeaBlue => new Color(0.102f, 0.212f, 0.365f);

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a copy of the color with a different alpha value.
        /// </summary>
        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        /// <summary>
        /// Lerp between two theme colors. Useful for animated transitions.
        /// </summary>
        public static Color Lerp(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }
    }
}
