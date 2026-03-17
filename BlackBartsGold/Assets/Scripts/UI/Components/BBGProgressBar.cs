// ============================================================================
// BBGProgressBar.cs
// Black Bart's Gold — Themed Progress Bar / Meter
// Path: Assets/Scripts/UI/Components/BBGProgressBar.cs
// ============================================================================
// Brass-framed progress bar with leather track and color-coded glowing fill.
// Supports horizontal and vertical orientations, animated fill, value labels,
// and three color modes: status gradient, fixed color, and theme gold.
//
// Creation:
//   var gas = BBGProgressBar.Create(parent, BBGBarOrientation.Vertical);
//   gas.SetValue(0.75f);
//
//   var xp = BBGProgressBar.Create(parent, BBGBarOrientation.Horizontal,
//       colorMode: BBGBarColorMode.ThemeGold, size: new Vector2(300, 24));
//   xp.SetValue(0.4f, "40%");
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BlackBartsGold.UI
{
    public enum BBGBarOrientation
    {
        Horizontal,
        Vertical
    }

    public enum BBGBarColorMode
    {
        StatusGradient,
        ThemeGold,
        Fixed
    }

    [RequireComponent(typeof(RectTransform))]
    public class BBGProgressBar : MonoBehaviour
    {
        #region Inspector Fields

        [Header("BBG Progress Bar")]
        [SerializeField] private BBGBarOrientation orientation = BBGBarOrientation.Horizontal;
        [SerializeField] private BBGBarColorMode colorMode = BBGBarColorMode.StatusGradient;
        [SerializeField, Range(0f, 1f)] private float value = 1f;
        [SerializeField] private bool animateFill = true;
        [SerializeField] private float fillAnimSpeed = 4f;
        [SerializeField] private bool showLabel = false;
        [SerializeField] private Color fixedFillColor = Color.white;

        #endregion

        #region Runtime State

        private Image _trackImage;
        private Image _fillImage;
        private Image _borderImage;
        private Image _glowImage;
        private TextMeshProUGUI _labelText;
        private RectTransform _rect;
        private float _displayValue;
        private float _targetValue;
        private bool _built;

        #endregion

        #region Public API

        public float Value => value;
        public RectTransform RectTransform => _rect != null ? _rect : GetComponent<RectTransform>();

        /// <summary>Set the bar value (0–1) with optional label text.</summary>
        public void SetValue(float newValue, string labelOverride = null)
        {
            value = Mathf.Clamp01(newValue);
            _targetValue = value;

            if (!animateFill)
            {
                _displayValue = value;
                ApplyFill();
            }

            if (labelOverride != null && _labelText != null)
                _labelText.text = labelOverride;
        }

        public void SetColorMode(BBGBarColorMode mode)
        {
            colorMode = mode;
            ApplyFillColor();
        }

        public void SetFixedColor(Color color)
        {
            fixedFillColor = color;
            if (colorMode == BBGBarColorMode.Fixed) ApplyFillColor();
        }

        public void SetLabel(string text)
        {
            showLabel = true;
            if (_labelText != null)
            {
                _labelText.text = text;
                _labelText.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Factory

        /// <summary>Create a themed progress bar.</summary>
        public static BBGProgressBar Create(
            Transform parent,
            BBGBarOrientation barOrientation = BBGBarOrientation.Horizontal,
            BBGBarColorMode colorMode = BBGBarColorMode.StatusGradient,
            Vector2? size = null,
            bool showLabel = false)
        {
            var go = new GameObject("BBGProgressBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Vector2 defaultSize = barOrientation == BBGBarOrientation.Horizontal
                ? new Vector2(300, 28)
                : new Vector2(50, 120);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? defaultSize;

            var bar = go.AddComponent<BBGProgressBar>();
            bar.orientation = barOrientation;
            bar.colorMode = colorMode;
            bar.showLabel = showLabel;
            bar.Build();
            return bar;
        }

        #endregion

        #region Lifecycle

        private void Start()
        {
            if (!_built) Build();
        }

        private void Update()
        {
            if (!_built || !animateFill) return;

            if (Mathf.Abs(_displayValue - _targetValue) > 0.001f)
            {
                _displayValue = Mathf.Lerp(_displayValue, _targetValue, Time.deltaTime * fillAnimSpeed);
                ApplyFill();
            }
        }

        #endregion

        #region Build

        public void Build()
        {
            if (_built) return;

            _rect = GetComponent<RectTransform>();
            _displayValue = value;
            _targetValue = value;
            var theme = BBGThemeProvider.Current;
            float inset = 4f;

            _trackImage = CreateChildImage("_Track", BBGSprites.ButtonLeather);
            _trackImage.type = Image.Type.Sliced;
            _trackImage.raycastTarget = false;
            _trackImage.color = Color.Lerp(Color.white, Color.black, 0.15f);
            FillParent(_trackImage.rectTransform);

            var fillGO = new GameObject("_Fill", typeof(RectTransform), typeof(CanvasRenderer));
            fillGO.transform.SetParent(transform, false);
            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.raycastTarget = false;

            if (orientation == BBGBarOrientation.Horizontal)
            {
                _fillImage.type = Image.Type.Filled;
                _fillImage.fillMethod = Image.FillMethod.Horizontal;
                _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            else
            {
                _fillImage.type = Image.Type.Filled;
                _fillImage.fillMethod = Image.FillMethod.Vertical;
                _fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            }

            var fillRect = _fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(inset, inset);
            fillRect.offsetMax = new Vector2(-inset, -inset);

            _borderImage = CreateChildImage("_Border", BBGSprites.ButtonBrassBorder);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;
            _borderImage.color = theme.brass;
            FillParent(_borderImage.rectTransform);

            if (showLabel)
            {
                var labelGO = new GameObject("_Label");
                labelGO.transform.SetParent(transform, false);
                _labelText = labelGO.AddComponent<TextMeshProUGUI>();
                _labelText.text = "";
                _labelText.fontSize = 16;
                _labelText.fontStyle = FontStyles.Bold;
                _labelText.alignment = TextAlignmentOptions.Center;
                _labelText.color = theme.parchment;
                _labelText.raycastTarget = false;
                _labelText.enableWordWrapping = false;
                FillParent(_labelText.rectTransform);
            }

            ApplyFill();
            _built = true;
        }

        #endregion

        #region Fill Logic

        private void ApplyFill()
        {
            if (_fillImage == null) return;
            _fillImage.fillAmount = _displayValue;
            ApplyFillColor();
        }

        private void ApplyFillColor()
        {
            if (_fillImage == null) return;
            var theme = BBGThemeProvider.Current;

            switch (colorMode)
            {
                case BBGBarColorMode.StatusGradient:
                    _fillImage.color = GetStatusColor(_displayValue, theme);
                    break;

                case BBGBarColorMode.ThemeGold:
                    _fillImage.color = theme.treasureGold;
                    break;

                case BBGBarColorMode.Fixed:
                    _fillImage.color = fixedFillColor;
                    break;
            }
        }

        /// <summary>
        /// Returns a color that transitions red → amber → green based on value.
        /// 0.0 = danger red, 0.5 = warning amber, 1.0 = success green
        /// </summary>
        private static Color GetStatusColor(float v, BBGTheme theme)
        {
            if (v <= 0.01f) return Color.gray;

            if (v < 0.5f)
                return Color.Lerp(theme.danger, theme.warning, v * 2f);
            else
                return Color.Lerp(theme.warning, theme.success, (v - 0.5f) * 2f);
        }

        #endregion

        #region Helpers

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
