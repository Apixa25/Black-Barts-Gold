// ============================================================================
// BBGSprites.cs
// Black Bart's Gold — Procedural UI Sprite Library
// Path: Assets/Scripts/UI/Theme/BBGSprites.cs
// ============================================================================
// Generates all base UI textures procedurally using Perlin noise, gradients,
// and the active BBGTheme colors. Every sprite is 9-sliced and ready for use
// with Unity's Image component in Sliced mode.
//
// Textures are generated lazily on first access and cached for the session.
// Call Reload() to regenerate after a theme change.
//
// These procedural sprites are the "good enough for now" visual foundation.
// Any sprite can be replaced with hand-painted art later by dropping a PNG
// into Resources/UI/Sprites/ — BBGSprites checks Resources first and only
// falls back to procedural generation if no asset is found.
//
// Usage:
//   image.sprite = BBGSprites.ButtonLeather;
//   image.type   = Image.Type.Sliced;
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.UI
{
    public static class BBGSprites
    {
        #region Configuration

        private const int BtnSize = 64;
        private const int PanelSize = 128;
        private const int GlowSize = 64;
        private const int GlowRectSize = 80;
        private const int DividerW = 256;
        private const int DividerH = 12;

        private const float BtnBorder = 16f;
        private const float PanelBorder = 24f;
        private const float GlowRectBorder = 20f;

        private const float LeatherSeed = 42.7f;
        private const float WoodSeed = 137.3f;
        private const float BrassSeed = 223.1f;
        private const float ParchmentSeed = 311.9f;

        private const string ResourcePath = "UI/Sprites/";

        #endregion

        #region Cached Sprites

        private static Sprite _btnLeather;
        private static Sprite _btnBrassBorder;
        private static Sprite _panelWood;
        private static Sprite _panelParchment;
        private static Sprite _glowSoft;
        private static Sprite _glowRect;
        private static Sprite _dividerBrass;

        #endregion

        #region Public Accessors

        /// <summary>9-sliced dark leather button background with grain texture and bevel.</summary>
        public static Sprite ButtonLeather =>
            _btnLeather ??= LoadOrGenerate("btn-leather", BtnSize, BtnSize, BtnBorder, GenLeather);

        /// <summary>9-sliced brass border frame for buttons (transparent center).</summary>
        public static Sprite ButtonBrassBorder =>
            _btnBrassBorder ??= LoadOrGenerate("btn-brass-border", BtnSize, BtnSize, BtnBorder, GenBrassBorder);

        /// <summary>9-sliced dark wood plank panel background.</summary>
        public static Sprite PanelWood =>
            _panelWood ??= LoadOrGenerate("panel-wood", PanelSize, PanelSize, PanelBorder, GenWood);

        /// <summary>9-sliced aged parchment card background.</summary>
        public static Sprite PanelParchment =>
            _panelParchment ??= LoadOrGenerate("panel-parchment", PanelSize, PanelSize, PanelBorder, GenParchment);

        /// <summary>Soft radial glow (white, tint via Image.color). Not 9-sliced.</summary>
        public static Sprite GlowSoft =>
            _glowSoft ??= LoadOrGenerate("glow-soft", GlowSize, GlowSize, 0f, GenGlowSoft);

        /// <summary>9-sliced rectangular glow border (white, transparent center). Tint via Image.color.
        /// Place behind a button, slightly larger, to create an outer neon glow.</summary>
        public static Sprite GlowRect =>
            _glowRect ??= LoadOrGenerate("glow-rect", GlowRectSize, GlowRectSize, GlowRectBorder, GenGlowRect);

        /// <summary>Horizontal brass divider bar with 3D bevel. Not 9-sliced.</summary>
        public static Sprite DividerBrass =>
            _dividerBrass ??= LoadOrGenerate("divider-brass", DividerW, DividerH, 0f, GenDivider);

        #endregion

        #region Lifecycle

        /// <summary>Destroy all cached sprites and textures. They regenerate lazily on next access.</summary>
        public static void Reload()
        {
            DestroySprite(ref _btnLeather);
            DestroySprite(ref _btnBrassBorder);
            DestroySprite(ref _panelWood);
            DestroySprite(ref _panelParchment);
            DestroySprite(ref _glowSoft);
            DestroySprite(ref _glowRect);
            DestroySprite(ref _dividerBrass);
            Debug.Log("[BBGSprites] All sprites cleared. Will regenerate on next access.");
        }

        /// <summary>Pre-generate all sprites now (call during a loading screen).</summary>
        public static void WarmUp()
        {
            _ = ButtonLeather;
            _ = ButtonBrassBorder;
            _ = PanelWood;
            _ = PanelParchment;
            _ = GlowSoft;
            _ = GlowRect;
            _ = DividerBrass;
            Debug.Log("[BBGSprites] ✅ All 7 sprites generated and cached.");
        }

        #endregion

        #region Sprite Factory

        private delegate Color[] PixelGenerator(int w, int h);

        private static Sprite LoadOrGenerate(string name, int w, int h, float border, PixelGenerator gen)
        {
            var loaded = Resources.Load<Sprite>(ResourcePath + name);
            if (loaded != null)
            {
                Debug.Log($"[BBGSprites] Loaded '{name}' from Resources.");
                return loaded;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = $"BBG_{name}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(gen(w, h));
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            Sprite sprite;
            if (border > 0f)
            {
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, w, h),
                    new Vector2(0.5f, 0.5f),
                    100f, 0,
                    SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border)
                );
            }
            else
            {
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, w, h),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }

            sprite.name = name;
            return sprite;
        }

        #endregion

        #region Texture Generators

        // ═══════════════════════════════════════════════════════════════
        //  LEATHER — dark brown, multi-octave grain, beveled edges
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenLeather(int w, int h)
        {
            var t = BBGThemeProvider.Current;
            var pixels = new Color[w * h];
            Color baseCol = t.darkLeather;
            Color darkCol = Color.Lerp(baseCol, Color.black, 0.35f);
            Color lightCol = Color.Lerp(baseCol, t.saddleBrown, 0.25f);
            float borderFrac = BtnBorder / w;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);

                    float n1 = Noise(x * 0.08f + LeatherSeed, y * 0.08f + LeatherSeed);
                    float n2 = Noise(x * 0.2f + LeatherSeed + 50f, y * 0.2f + LeatherSeed + 50f);
                    float n3 = Noise(x * 0.5f + LeatherSeed + 100f, y * 0.5f + LeatherSeed + 100f);
                    float grain = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;

                    Color c = Color.Lerp(darkCol, lightCol, grain);

                    float topWarmth = Smoothstep(0.55f, 0.95f, fy) * 0.07f;
                    c.r += topWarmth;
                    c.g += topWarmth * 0.6f;

                    float edge = EdgeFade(fx, fy, borderFrac);
                    c = Color.Lerp(c, darkCol, edge * 0.55f);

                    float inner = InnerHighlight(fx, fy, borderFrac);
                    c = Color.Lerp(c, lightCol, inner * 0.12f);

                    c.a = 1f;
                    pixels[y * w + x] = c;
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  BRASS BORDER — metallic frame, transparent center
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenBrassBorder(int w, int h)
        {
            var t = BBGThemeProvider.Current;
            var pixels = new Color[w * h];
            Color baseCol = t.brass;
            Color darkCol = Color.Lerp(baseCol, Color.black, 0.3f);
            Color brightCol = Color.Lerp(baseCol, Color.white, 0.25f);
            float borderFrac = BtnBorder / w;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);
                    float distFromEdge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));

                    if (distFromEdge > borderFrac)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float borderT = distFromEdge / borderFrac;

                    float brushed = Noise(x * 0.02f + BrassSeed, y * 0.6f + BrassSeed);
                    float fine = Noise(x * 0.4f + BrassSeed + 70f, y * 0.4f + BrassSeed + 70f);
                    float metal = brushed * 0.7f + fine * 0.3f;

                    Color c = Color.Lerp(darkCol, baseCol, metal * 0.6f + 0.4f);

                    float diagonal = (fx + fy) * 0.5f;
                    float reflection = Smoothstep(0.3f, 0.6f, diagonal) * Smoothstep(0.9f, 0.6f, diagonal);
                    c = Color.Lerp(c, brightCol, reflection * 0.35f);

                    float outerBevel = Smoothstep(0.2f, 0f, borderT) * 0.3f;
                    c = Color.Lerp(c, darkCol, outerBevel);

                    float innerBevel = Smoothstep(0.8f, 1f, borderT) * 0.2f;
                    c = Color.Lerp(c, brightCol, innerBevel);

                    float edgeAlpha = Smoothstep(1f, 0.85f, borderT);
                    c.a = Mathf.Lerp(1f, edgeAlpha, 0.3f);

                    pixels[y * w + x] = c;
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  WOOD — horizontal grain, ring patterns, darkened edges
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenWood(int w, int h)
        {
            var t = BBGThemeProvider.Current;
            var pixels = new Color[w * h];
            Color baseCol = t.woodDark;
            Color darkCol = Color.Lerp(baseCol, Color.black, 0.3f);
            Color lightCol = Color.Lerp(baseCol, t.saddleBrown, 0.35f);
            float borderFrac = PanelBorder / w;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);

                    float grain = Noise(x * 0.03f + WoodSeed, y * 0.18f + WoodSeed);
                    float ring = Mathf.Sin(y * 0.35f + Noise(x * 0.06f + WoodSeed + 30f, y * 0.06f + WoodSeed + 30f) * 4f);
                    ring = ring * 0.5f + 0.5f;
                    float knot = Noise(x * 0.15f + WoodSeed + 80f, y * 0.15f + WoodSeed + 80f);
                    knot = Mathf.Max(0f, knot - 0.55f) * 3f;

                    float pattern = grain * 0.5f + ring * 0.35f + knot * 0.15f;
                    Color c = Color.Lerp(darkCol, lightCol, pattern);

                    float vertBand = Noise(x * 0.12f + WoodSeed + 150f, 0.5f);
                    c = Color.Lerp(c, darkCol, vertBand * 0.12f);

                    float plankLine = Mathf.Abs(Mathf.Sin(x * 0.05f * Mathf.PI)) < 0.03f ? 0.15f : 0f;
                    c = Color.Lerp(c, darkCol, plankLine);

                    float edge = EdgeFade(fx, fy, borderFrac);
                    c = Color.Lerp(c, darkCol, edge * 0.45f);

                    c.a = 1f;
                    pixels[y * w + x] = c;
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  PARCHMENT — aged paper, fiber noise, stained edges
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenParchment(int w, int h)
        {
            var t = BBGThemeProvider.Current;
            var pixels = new Color[w * h];
            Color baseCol = t.parchment;
            Color darkCol = Color.Lerp(t.warmTan, t.saddleBrown, 0.3f);
            Color stainCol = Color.Lerp(t.warmTan, t.darkLeather, 0.4f);
            float borderFrac = PanelBorder / w;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);

                    float fiber1 = Noise(x * 0.12f + ParchmentSeed, y * 0.12f + ParchmentSeed);
                    float fiber2 = Noise(x * 0.3f + ParchmentSeed + 60f, y * 0.3f + ParchmentSeed + 60f);
                    float fiber = fiber1 * 0.6f + fiber2 * 0.4f;

                    Color c = Color.Lerp(baseCol, darkCol, fiber * 0.2f);

                    float edgeDist = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
                    float aging = Smoothstep(0.25f, 0.0f, edgeDist) * 0.35f;
                    c = Color.Lerp(c, stainCol, aging);

                    float stain = Mathf.Max(0f, Noise(x * 0.04f + ParchmentSeed + 120f, y * 0.04f + ParchmentSeed + 120f) - 0.58f);
                    c = Color.Lerp(c, stainCol, stain * 0.4f);

                    float cornerDist = Mathf.Min(
                        Mathf.Sqrt(fx * fx + fy * fy),
                        Mathf.Min(
                            Mathf.Sqrt((1f - fx) * (1f - fx) + fy * fy),
                            Mathf.Min(
                                Mathf.Sqrt(fx * fx + (1f - fy) * (1f - fy)),
                                Mathf.Sqrt((1f - fx) * (1f - fx) + (1f - fy) * (1f - fy))
                            )
                        )
                    );
                    float cornerStain = Smoothstep(0.35f, 0.0f, cornerDist) * 0.2f;
                    c = Color.Lerp(c, stainCol, cornerStain);

                    c.a = 1f;
                    pixels[y * w + x] = c;
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  GLOW SOFT — radial gradient, white, tint via Image.color
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenGlowSoft(int w, int h)
        {
            var pixels = new Color[w * h];
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;

                    float alpha = 1f - Smoothstep(0f, 0.85f, dist);
                    alpha *= alpha;

                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  GLOW RECT — 9-sliced rectangular glow border, transparent center
        //  Place behind an element (slightly larger) for neon outer glow.
        //  Tint via Image.color to set glow color.
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenGlowRect(int w, int h)
        {
            var pixels = new Color[w * h];
            float borderFrac = GlowRectBorder / w;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);
                    float distFromEdge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));

                    if (distFromEdge > borderFrac)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float t = distFromEdge / borderFrac;
                    float alpha = (1f - Smoothstep(0f, 0.9f, t));
                    alpha *= alpha;

                    float cornerBoost = 1f - Mathf.Min(
                        Mathf.Min(fx, 1f - fx) / borderFrac,
                        Mathf.Min(fy, 1f - fy) / borderFrac
                    );
                    cornerBoost = Mathf.Clamp01(cornerBoost);
                    alpha *= Mathf.Lerp(1f, 0.7f, cornerBoost * 0.5f);

                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return pixels;
        }

        // ═══════════════════════════════════════════════════════════════
        //  DIVIDER BRASS — horizontal bar with 3D bevel and rounded ends
        // ═══════════════════════════════════════════════════════════════

        private static Color[] GenDivider(int w, int h)
        {
            var t = BBGThemeProvider.Current;
            var pixels = new Color[w * h];
            Color baseCol = t.brass;
            Color darkCol = Color.Lerp(baseCol, Color.black, 0.35f);
            Color brightCol = Color.Lerp(baseCol, Color.white, 0.3f);

            float halfH = h * 0.5f;
            float roundRadius = h * 0.8f;

            for (int y = 0; y < h; y++)
            {
                float fy = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1);

                    float endFade = Smoothstep(0f, 0.04f, fx) * Smoothstep(1f, 0.96f, fx);

                    float metal = Noise(x * 0.03f + BrassSeed, y * 0.5f + BrassSeed);
                    Color c = Color.Lerp(darkCol, baseCol, metal * 0.5f + 0.5f);

                    float bevel = Smoothstep(0f, 0.35f, fy) * Smoothstep(1f, 0.65f, fy);
                    c = Color.Lerp(darkCol, c, bevel);
                    c = Color.Lerp(c, brightCol, Smoothstep(0.4f, 0.7f, fy) * 0.3f);

                    float distFromCenter = Mathf.Abs(fy - 0.5f) * 2f;
                    float barAlpha = Smoothstep(1f, 0.7f, distFromCenter) * endFade;

                    c.a = barAlpha;
                    pixels[y * w + x] = c;
                }
            }
            return pixels;
        }

        #endregion

        #region Math Helpers

        private static float Noise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }

        private static float Smoothstep(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1)) return x >= edge0 ? 1f : 0f;
            float v = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return v * v * (3f - 2f * v);
        }

        /// <summary>
        /// Returns 0 in the center, rising to 1 at the edges.
        /// Used for beveled edge darkening on 9-sliced textures.
        /// </summary>
        private static float EdgeFade(float fx, float fy, float borderFrac)
        {
            float left = Smoothstep(borderFrac, 0f, fx);
            float right = Smoothstep(1f - borderFrac, 1f, fx);
            float bottom = Smoothstep(borderFrac, 0f, fy);
            float top = Smoothstep(1f - borderFrac, 1f, fy);
            return Mathf.Max(Mathf.Max(left, right), Mathf.Max(bottom, top));
        }

        /// <summary>
        /// Returns a thin bright band just inside the border zone.
        /// Creates the "light catching the inner edge" bevel effect.
        /// </summary>
        private static float InnerHighlight(float fx, float fy, float borderFrac)
        {
            float dist = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
            float inner = borderFrac * 0.75f;
            float peak = borderFrac * 0.95f;
            float outer = borderFrac * 1.15f;
            return Smoothstep(inner, peak, dist) * (1f - Smoothstep(peak, outer, dist));
        }

        private static void DestroySprite(ref Sprite sprite)
        {
            if (sprite == null) return;
            if (sprite.texture != null)
                Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
            sprite = null;
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Returns a Texture2D for a named sprite (for non-Image use cases).
        /// The texture is owned by BBGSprites — do not destroy it.
        /// </summary>
        public static Texture2D GetTexture(string spriteName)
        {
            return spriteName switch
            {
                "btn-leather"      => ButtonLeather.texture,
                "btn-brass-border" => ButtonBrassBorder.texture,
                "panel-wood"       => PanelWood.texture,
                "panel-parchment"  => PanelParchment.texture,
                "glow-soft"        => GlowSoft.texture,
                "glow-rect"        => GlowRect.texture,
                "divider-brass"    => DividerBrass.texture,
                _ => null
            };
        }

        /// <summary>
        /// Returns all sprite names available in this library.
        /// Useful for the editor baker and debug tools.
        /// </summary>
        public static string[] AllSpriteNames => new[]
        {
            "btn-leather",
            "btn-brass-border",
            "panel-wood",
            "panel-parchment",
            "glow-soft",
            "glow-rect",
            "divider-brass"
        };

        #endregion
    }
}
