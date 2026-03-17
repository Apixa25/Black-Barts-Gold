// ============================================================================
// BBGButton.cs
// Black Bart's Gold — Themed Button Component
// Path: Assets/Scripts/UI/Components/BBGButton.cs
// ============================================================================
// Layered button with leather texture, brass border, neon glow, and press
// animations. Stacks: GlowRect → Leather → BrassBorder → Text.
//
// Creation:
//   var btn = BBGButton.Create(parent, "Start Hunting", BBGButtonVariant.Primary);
//   btn.onClick.AddListener(() => StartHunt());
//
// Upgrade existing:
//   var btn = BBGButton.Upgrade(existingButtonGO, BBGButtonVariant.Secondary);
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

namespace BlackBartsGold.UI
{
    public enum BBGButtonVariant
    {
        Primary,
        Secondary,
        Danger,
        Ghost
    }

    [RequireComponent(typeof(RectTransform))]
    public class BBGButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Inspector Fields

        [Header("BBG Button")]
        [SerializeField] private BBGButtonVariant variant = BBGButtonVariant.Primary;
        [SerializeField] private string label = "Button";
        [SerializeField] private int fontSize = 28;
        [SerializeField] private bool showGlow = true;
        [SerializeField] private bool animateBreathing = true;

        #endregion

        #region Runtime State

        private Image _glowImage;
        private Image _bgImage;
        private Image _borderImage;
        private TextMeshProUGUI _labelText;
        private Button _button;
        private RectTransform _rect;

        private Color _glowBaseColor;
        private float _glowMinAlpha;
        private float _glowMaxAlpha;
        private Coroutine _pressCoroutine;
        private bool _built;

        #endregion

        #region Public API

        public Button.ButtonClickedEvent onClick => EnsureButton().onClick;
        public Button UnityButton => EnsureButton();
        public RectTransform RectTransform => _rect != null ? _rect : GetComponent<RectTransform>();
        public BBGButtonVariant Variant => variant;

        public void SetText(string text)
        {
            label = text;
            if (_labelText != null) _labelText.text = text;
        }

        public string GetText() => label;

        public void SetFontSize(int size)
        {
            fontSize = size;
            if (_labelText != null) _labelText.fontSize = size;
        }

        public void SetVariant(BBGButtonVariant v)
        {
            variant = v;
            if (_built) ApplyVariant();
        }

        public void SetInteractable(bool interactable)
        {
            EnsureButton().interactable = interactable;

            if (_bgImage != null)
            {
                Color c = _bgImage.color;
                float targetAlpha = interactable ? 1f : 0.45f;
                _bgImage.color = new Color(c.r, c.g, c.b, targetAlpha);
            }
            if (_borderImage != null)
            {
                Color c = _borderImage.color;
                float targetAlpha = interactable ? 1f : 0.3f;
                _borderImage.color = new Color(c.r, c.g, c.b, targetAlpha);
            }
            if (_labelText != null)
            {
                Color c = _labelText.color;
                float targetAlpha = interactable ? 1f : 0.4f;
                _labelText.color = new Color(c.r, c.g, c.b, targetAlpha);
            }
            if (_glowImage != null)
                _glowImage.enabled = interactable && showGlow;
        }

        public void SetGlowEnabled(bool enabled)
        {
            showGlow = enabled;
            if (_glowImage != null) _glowImage.enabled = enabled;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Create a fully themed button from scratch.
        /// </summary>
        public static BBGButton Create(
            Transform parent,
            string text,
            BBGButtonVariant buttonVariant = BBGButtonVariant.Primary,
            Vector2? size = null)
        {
            string safeName = text.Replace(" ", "");
            var go = new GameObject($"BBG_{safeName}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? new Vector2(400, 70);

            var btn = go.AddComponent<BBGButton>();
            btn.variant = buttonVariant;
            btn.label = text;
            btn.Build();
            return btn;
        }

        /// <summary>
        /// Upgrade an existing Button GameObject with BBG theming.
        /// Hides the old Image and wraps the existing Button.
        /// </summary>
        public static BBGButton Upgrade(
            GameObject existing,
            BBGButtonVariant buttonVariant = BBGButtonVariant.Primary)
        {
            var oldImage = existing.GetComponent<Image>();
            if (oldImage != null)
            {
                oldImage.color = Color.clear;
                oldImage.raycastTarget = false;
            }

            var oldText = existing.GetComponentInChildren<TextMeshProUGUI>();
            string existingLabel = oldText != null ? oldText.text : "Button";
            int existingFontSize = oldText != null ? (int)oldText.fontSize : 28;

            if (oldText != null)
                oldText.gameObject.SetActive(false);

            var btn = existing.GetComponent<BBGButton>();
            if (btn == null)
                btn = existing.AddComponent<BBGButton>();

            btn.variant = buttonVariant;
            btn.label = existingLabel;
            btn.fontSize = existingFontSize;
            btn.Build();
            return btn;
        }

        #endregion

        #region Lifecycle

        private void Start()
        {
            if (!_built) Build();
        }

        private void Update()
        {
            if (!_built || _glowImage == null || !_glowImage.enabled || !animateBreathing)
                return;

            float cycle = BBGThemeProvider.Current.idleBreatheCycle;
            float t = (Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / cycle) + 1f) * 0.5f;
            Color c = _glowBaseColor;
            c.a = Mathf.Lerp(_glowMinAlpha, _glowMaxAlpha, t);
            _glowImage.color = c;
        }

        private void OnDisable()
        {
            if (_pressCoroutine != null)
            {
                StopCoroutine(_pressCoroutine);
                _pressCoroutine = null;
            }
            transform.localScale = Vector3.one;
        }

        #endregion

        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            if (_pressCoroutine != null) StopCoroutine(_pressCoroutine);
            _pressCoroutine = StartCoroutine(AnimatePress(true));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pressCoroutine != null) StopCoroutine(_pressCoroutine);
            _pressCoroutine = StartCoroutine(AnimatePress(false));
        }

        #endregion

        #region Build

        public void Build()
        {
            if (_built)
            {
                ApplyVariant();
                return;
            }

            _rect = GetComponent<RectTransform>();
            var theme = BBGThemeProvider.Current;
            bool isGhost = variant == BBGButtonVariant.Ghost;

            if (showGlow && !isGhost)
            {
                _glowImage = CreateChildImage("_Glow", BBGSprites.GlowRect);
                _glowImage.type = Image.Type.Sliced;
                _glowImage.raycastTarget = false;
                var glowRect = _glowImage.rectTransform;
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                float spread = theme.glowSpread;
                glowRect.offsetMin = new Vector2(-spread, -spread);
                glowRect.offsetMax = new Vector2(spread, spread);
            }

            if (!isGhost)
            {
                _bgImage = CreateChildImage("_Background", BBGSprites.ButtonLeather);
                _bgImage.type = Image.Type.Sliced;
                _bgImage.raycastTarget = true;
                FillParent(_bgImage.rectTransform);
            }
            else
            {
                _bgImage = CreateChildImage("_Background", null);
                _bgImage.color = Color.clear;
                _bgImage.raycastTarget = true;
                FillParent(_bgImage.rectTransform);
            }

            if (!isGhost)
            {
                _borderImage = CreateChildImage("_Border", BBGSprites.ButtonBrassBorder);
                _borderImage.type = Image.Type.Sliced;
                _borderImage.raycastTarget = false;
                FillParent(_borderImage.rectTransform);
            }

            var textGO = new GameObject("_Text");
            textGO.transform.SetParent(transform, false);
            _labelText = textGO.AddComponent<TextMeshProUGUI>();
            _labelText.text = label;
            _labelText.fontSize = fontSize;
            _labelText.fontStyle = FontStyles.Bold;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.raycastTarget = false;
            _labelText.enableWordWrapping = false;
            _labelText.overflowMode = TextOverflowModes.Ellipsis;
            var textRect = _labelText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(theme.spacingMd, theme.spacingSm);
            textRect.offsetMax = new Vector2(-theme.spacingMd, -theme.spacingSm);

            _button = EnsureButton();
            _button.targetGraphic = _bgImage;
            _button.transition = Selectable.Transition.None;

            ApplyVariant();
            _built = true;
        }

        #endregion

        #region Variant Styling

        private void ApplyVariant()
        {
            var t = BBGThemeProvider.Current;

            switch (variant)
            {
                case BBGButtonVariant.Primary:
                    ApplyColors(
                        bgTint: Color.Lerp(Color.white, t.treasureGold, 0.12f),
                        borderTint: t.treasureGold,
                        textColor: t.darkLeather,
                        glowColor: t.neonAmber,
                        glowMin: 0.25f, glowMax: 0.6f);
                    break;

                case BBGButtonVariant.Secondary:
                    ApplyColors(
                        bgTint: Color.white,
                        borderTint: t.brass,
                        textColor: t.parchment,
                        glowColor: t.neonCyan,
                        glowMin: 0.15f, glowMax: 0.4f);
                    break;

                case BBGButtonVariant.Danger:
                    ApplyColors(
                        bgTint: Color.Lerp(Color.white, t.danger, 0.15f),
                        borderTint: Color.Lerp(t.brass, t.danger, 0.3f),
                        textColor: t.fullWhite,
                        glowColor: t.neonMagenta,
                        glowMin: 0.25f, glowMax: 0.6f);
                    break;

                case BBGButtonVariant.Ghost:
                    ApplyColors(
                        bgTint: Color.clear,
                        borderTint: Color.clear,
                        textColor: t.parchment,
                        glowColor: t.neonCyan,
                        glowMin: 0.05f, glowMax: 0.15f);
                    break;
            }
        }

        private void ApplyColors(Color bgTint, Color borderTint, Color textColor,
            Color glowColor, float glowMin, float glowMax)
        {
            if (_bgImage != null) _bgImage.color = bgTint;
            if (_borderImage != null) _borderImage.color = borderTint;
            if (_labelText != null) _labelText.color = textColor;
            if (_glowImage != null)
            {
                _glowBaseColor = glowColor;
                _glowMinAlpha = glowMin;
                _glowMaxAlpha = glowMax;
                _glowImage.color = BBGThemeProvider.WithAlpha(glowColor, glowMin);
            }
        }

        #endregion

        #region Animation

        private IEnumerator AnimatePress(bool down)
        {
            var theme = BBGThemeProvider.Current;
            float duration = theme.buttonPressDuration;
            float pressScale = theme.buttonPressScale;

            Vector3 startScale = transform.localScale;
            Vector3 endScale = down ? Vector3.one * pressScale : Vector3.one;

            if (down && _glowImage != null)
            {
                _glowImage.color = BBGThemeProvider.WithAlpha(_glowBaseColor, 1f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = down ? EaseInCubic(t) : EaseOutBack(t);
                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                yield return null;
            }

            transform.localScale = endScale;
            _pressCoroutine = null;
        }

        private static float EaseInCubic(float t) => t * t * t;

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float v = t - 1f;
            return 1f + c3 * v * v * v + c1 * v * v;
        }

        #endregion

        #region Helpers

        private Button EnsureButton()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) _button = gameObject.AddComponent<Button>();
            return _button;
        }

        private Image CreateChildImage(string childName, Sprite sprite)
        {
            var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        private static void FillParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
