// ============================================================================
// EmojiHelper.cs
// Black Bart's Gold - Emoji-to-Text Sanitizer for TextMeshPro
// Path: Assets/Scripts/Utils/EmojiHelper.cs
// Created: 2026-02-09 - Fix emoji squares in LiberationSans SDF
// ============================================================================
// LiberationSans SDF doesn't include emoji glyphs, so they render as squares.
// This helper replaces common emoji with ASCII-safe text equivalents so the
// pirate personality is preserved without needing a custom font atlas.
// ============================================================================

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlackBartsGold.Utils
{
    /// <summary>
    /// Replaces emoji characters with ASCII-safe text equivalents for TextMeshPro.
    /// Call <see cref="Sanitize"/> before assigning text to any TMP_Text component.
    /// </summary>
    public static class EmojiHelper
    {
        // ── Known emoji → text replacements (most common in our UI) ──
        private static readonly Dictionary<string, string> EmojiMap = new Dictionary<string, string>
        {
            // Pirate theme
            { "\U0001F3F4\u200D\u2620\uFE0F", "[PIRATE]" },   // 🏴‍☠️  (flag + ZWJ + skull)
            { "\U0001F3F4",                    "[FLAG]" },      // 🏴   (black flag alone)
            
            // Status / warnings
            { "\u26A0\uFE0F",   "[!]" },        // ⚠️
            { "\u26A0",         "[!]" },        // ⚠  (without variation selector)
            { "\u2705",         "[OK]" },       // ✅
            { "\u274C",         "[X]" },        // ❌
            { "\u2713",         "[OK]" },       // ✓
            
            // Economy / coins
            { "\U0001FA99",     "[COIN]" },     // 🪙
            { "\U0001F4B0",     "$" },          // 💰
            { "\U0001F4B3",     "[CARD]" },     // 💳
            { "\U0001F4A1",     "*" },          // 💡
            
            // Navigation / map
            { "\U0001F4CD",     "[>]" },        // 📍
            { "\U0001F5FA\uFE0F", "[MAP]" },    // 🗺️
            { "\U0001F5FA",     "[MAP]" },      // 🗺
            { "\U0001F50D",     "[?]" },        // 🔍
            
            // Vehicles / gas
            { "\u26FD",         "[GAS]" },      // ⛽
            { "\U0001F697",     "[CAR]" },      // 🚗
            
            // Lock / unlock
            { "\U0001F512",     "[LOCK]" },     // 🔒
            { "\U0001F513",     "[OPEN]" },     // 🔓
            
            // Misc
            { "\U0001F4F1",     "[PHONE]" },    // 📱
            { "\U0001F3E6",     "[BANK]" },     // 🏦
            { "\U0001F381",     "[GIFT]" },     // 🎁
            { "\U0001F4E4",     "[SEND]" },     // 📤
            { "\U0001F4DD",     "[NOTE]" },     // 📝
            { "\u21A9\uFE0F",  "[RET]" },      // ↩️
            { "\u21A9",        "[RET]" },       // ↩
            { "\u2194\uFE0F",  "[<->]" },      // ↔️
            { "\u2194",        "[<->]" },       // ↔
            { "\u2693",        "[ANCHOR]" },    // ⚓
            { "\u26F5",        "[SHIP]" },      // ⛵
            { "\U0001F480",    "[SKULL]" },     // 💀
            { "\U0001F396\uFE0F", "[MEDAL]" },  // 🎖️
            { "\U0001F396",    "[MEDAL]" },     // 🎖
            { "\U0001F451",    "[CROWN]" },     // 👑
            { "\U0001F3AF",    "[TARGET]" },    // 🎯
            { "\U0001F527",    "[WRENCH]" },    // 🔧
            { "\U0001F6AA",    "[DOOR]" },      // 🚪
            { "\u2699\uFE0F",  "[GEAR]" },      // ⚙️
            { "\u2699",        "[GEAR]" },      // ⚙
            { "\u2715",        "X" },           // ✕
            { "\U0001F17F\uFE0F", "[P]" },      // 🅿️
            { "\U0001F17F",    "[P]" },         // 🅿
            { "\U0001F45B",    "[WALLET]" },    // 👟 (sneaker - "MY WALLET" in MainMenu)
        };

        // Regex to catch any remaining emoji/surrogates we didn't map
        private static readonly Regex SurrogateRegex = new Regex(
            @"[\uD800-\uDBFF][\uDC00-\uDFFF]" +   // surrogate pairs
            @"|[\u2600-\u27BF]" +                    // misc symbols & dingbats
            @"|[\uFE00-\uFE0F]" +                    // variation selectors
            @"|[\u200D]",                             // zero-width joiner
            RegexOptions.Compiled);

        /// <summary>
        /// Replace emoji with ASCII text equivalents.
        /// Safe to call on any string — returns original if no emoji found.
        /// </summary>
        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Phase 1: Replace known emoji with text equivalents
            foreach (var kvp in EmojiMap)
            {
                if (text.Contains(kvp.Key))
                {
                    text = text.Replace(kvp.Key, kvp.Value);
                }
            }

            // Phase 2: Strip any remaining unmapped emoji / surrogates
            text = SurrogateRegex.Replace(text, "");

            // Phase 3: Clean up double spaces left by removals
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text.Trim();
        }
    }
}
