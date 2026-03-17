// ============================================================================
// BBGShaderLib.cs
// Black Bart's Gold — Shader Material Factory & Cache
// Path: Assets/Scripts/UI/Theme/BBGShaderLib.cs
// ============================================================================
// Manages shared and per-instance materials for BBG custom UI shaders.
// Provides graceful fallback — if shaders aren't found (build stripped, etc.)
// callers get null and fall back to sprite-based visuals.
//
// Usage:
//   Material mat = BBGShaderLib.GlowMaterial;
//   if (mat != null) myImage.material = mat;  // shader path
//   else myImage.sprite = BBGSprites.GlowRect; // sprite fallback
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.UI
{
    public static class BBGShaderLib
    {
        private const string GlowShaderName = "BBG/UI/Glow";
        private const string NeonOutlineShaderName = "BBG/UI/NeonOutline";

        private static Material _glowMaterial;
        private static Material _neonOutlineMaterial;
        private static bool _glowChecked;
        private static bool _neonChecked;

        #region Shared Materials

        /// <summary>
        /// Shared glow material with default theme settings.
        /// All buttons share this material — individual intensity is controlled
        /// via Image.color (vertex color) from C# animation code.
        /// Returns null if the shader isn't available.
        /// </summary>
        public static Material GlowMaterial
        {
            get
            {
                if (!_glowChecked)
                {
                    _glowChecked = true;
                    _glowMaterial = CreateSharedGlowMaterial();
                }
                return _glowMaterial;
            }
        }

        /// <summary>
        /// Shared neon outline material with default theme settings.
        /// Returns null if the shader isn't available.
        /// </summary>
        public static Material NeonOutlineMaterial
        {
            get
            {
                if (!_neonChecked)
                {
                    _neonChecked = true;
                    _neonOutlineMaterial = CreateSharedNeonOutlineMaterial();
                }
                return _neonOutlineMaterial;
            }
        }

        #endregion

        #region Per-Instance Materials

        /// <summary>
        /// Create a unique glow material for a specific element.
        /// Use when you need to animate shader properties independently.
        /// </summary>
        public static Material CreateGlowInstance(Color glowColor, float intensity = 1.5f, float falloff = 8f)
        {
            var shader = Shader.Find(GlowShaderName);
            if (shader == null) return null;

            var mat = new Material(shader)
            {
                name = "BBGGlow_Instance",
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetColor(PropGlowColor, glowColor);
            mat.SetFloat(PropGlowIntensity, intensity);
            mat.SetFloat(PropGlowFalloff, falloff);
            return mat;
        }

        /// <summary>
        /// Create a unique neon outline material for a specific element.
        /// </summary>
        public static Material CreateNeonOutlineInstance(Color neonColor, float coreBrightness = 2.5f)
        {
            var shader = Shader.Find(NeonOutlineShaderName);
            if (shader == null) return null;

            var mat = new Material(shader)
            {
                name = "BBGNeonOutline_Instance",
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetColor(PropNeonColor, neonColor);
            mat.SetFloat(PropCoreBrightness, coreBrightness);
            return mat;
        }

        #endregion

        #region Property Setters

        public static void SetGlowColor(Material mat, Color color)
        {
            if (mat != null) mat.SetColor(PropGlowColor, color);
        }

        public static void SetGlowIntensity(Material mat, float intensity)
        {
            if (mat != null) mat.SetFloat(PropGlowIntensity, intensity);
        }

        public static void SetNeonColor(Material mat, Color color)
        {
            if (mat != null) mat.SetColor(PropNeonColor, color);
        }

        public static void SetCoreBrightness(Material mat, float brightness)
        {
            if (mat != null) mat.SetFloat(PropCoreBrightness, brightness);
        }

        public static void SetPulseSpeed(Material mat, float speed)
        {
            if (mat != null) mat.SetFloat(PropPulseSpeed, speed);
        }

        #endregion

        #region Diagnostics

        /// <summary>True if the glow shader compiled and is available.</summary>
        public static bool IsGlowAvailable => GlowMaterial != null;

        /// <summary>True if the neon outline shader compiled and is available.</summary>
        public static bool IsNeonOutlineAvailable => NeonOutlineMaterial != null;

        /// <summary>Force re-check shader availability (e.g. after hot-reload).</summary>
        public static void Invalidate()
        {
            if (_glowMaterial != null) Object.DestroyImmediate(_glowMaterial);
            if (_neonOutlineMaterial != null) Object.DestroyImmediate(_neonOutlineMaterial);
            _glowMaterial = null;
            _neonOutlineMaterial = null;
            _glowChecked = false;
            _neonChecked = false;
        }

        #endregion

        #region Internal

        private static readonly int PropGlowColor = Shader.PropertyToID("_GlowColor");
        private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PropGlowFalloff = Shader.PropertyToID("_GlowFalloff");
        private static readonly int PropCornerRadius = Shader.PropertyToID("_CornerRadius");
        private static readonly int PropInnerPad = Shader.PropertyToID("_InnerPad");
        private static readonly int PropNeonColor = Shader.PropertyToID("_NeonColor");
        private static readonly int PropCoreBrightness = Shader.PropertyToID("_CoreBrightness");
        private static readonly int PropCoreSharpness = Shader.PropertyToID("_CoreSharpness");
        private static readonly int PropPulseSpeed = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PropPulseAmount = Shader.PropertyToID("_PulseAmount");

        private static Material CreateSharedGlowMaterial()
        {
            var shader = Shader.Find(GlowShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[BBGShaderLib] Glow shader not found — falling back to sprite glow.");
                return null;
            }

            var theme = BBGThemeProvider.Current;
            var mat = new Material(shader)
            {
                name = "BBGGlow_Shared",
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetColor(PropGlowColor, new Color(1.5f, 1.3f, 1.0f, 1f));
            mat.SetFloat(PropGlowIntensity, 1.5f);
            mat.SetFloat(PropGlowFalloff, 8f);
            mat.SetFloat(PropCornerRadius, 0.06f);
            mat.SetFloat(PropInnerPad, 0.04f);
            mat.SetFloat(PropPulseSpeed, 3f);
            mat.SetFloat(PropPulseAmount, 0.12f);

            Debug.Log("[BBGShaderLib] Glow shader material created.");
            return mat;
        }

        private static Material CreateSharedNeonOutlineMaterial()
        {
            var shader = Shader.Find(NeonOutlineShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[BBGShaderLib] NeonOutline shader not found — falling back to sprite glow.");
                return null;
            }

            var theme = BBGThemeProvider.Current;
            var mat = new Material(shader)
            {
                name = "BBGNeonOutline_Shared",
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetColor(PropNeonColor, theme.neonCyan);
            mat.SetFloat(PropCoreBrightness, 2.5f);
            mat.SetFloat(PropCoreSharpness, 60f);
            mat.SetFloat(PropGlowIntensity, 0.8f);
            mat.SetFloat(PropGlowFalloff, 12f);
            mat.SetFloat(PropCornerRadius, 0.04f);
            mat.SetFloat(PropPulseSpeed, 2f);
            mat.SetFloat(PropPulseAmount, 0.1f);

            Debug.Log("[BBGShaderLib] NeonOutline shader material created.");
            return mat;
        }

        #endregion
    }
}
