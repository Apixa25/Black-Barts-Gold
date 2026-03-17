// ============================================================================
// BBGPanel.cs
// Black Bart's Gold — Themed Panel / Container Component
// Path: Assets/Scripts/UI/Components/BBGPanel.cs
// ============================================================================
// Container with textured background (wood, parchment, or dark) and optional
// brass frame. Provides a ContentArea RectTransform for child elements.
//
// Creation:
//   var panel = BBGPanel.Create(parent, BBGPanelStyle.Wood);
//   myChild.transform.SetParent(panel.ContentArea, false);
//
// Card shortcut (parchment with header):
//   var card = BBGPanel.CreateCard(parent, "Player Stats", new Vector2(400, 250));
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    public enum BBGPanelStyle
    {
        Wood,
        Parchment,
        Dark
    }

    [RequireComponent(typeof(RectTransform))]
    public class BBGPanel : MonoBehaviour
    {
        #region Inspector Fields

        [Header("BBG Panel")]
        [SerializeField] private BBGPanelStyle style = BBGPanelStyle.Wood;
        [SerializeField] private bool showBorder = true;
        [SerializeField] private bool showGlow = false;
        [SerializeField] private string headerText = "";

        #endregion

        #region Runtime State

        private Image _bgImage;
        private Image _borderImage;
        private Image _glowImage;
        private TextMeshProUGUI _headerLabel;
        private RectTransform _contentArea;
        private RectTransform _rect;
        private bool _built;

        #endregion

        #region Public API

        public RectTransform ContentArea
        {
            get
            {
                if (!_built) Build();
                return _contentArea;
            }
        }

        public RectTransform RectTransform => _rect != null ? _rect : GetComponent<RectTransform>();

        public void SetStyle(BBGPanelStyle newStyle)
        {
            style = newStyle;
            if (_built) ApplyStyle();
        }

        public void SetHeader(string text)
        {
            headerText = text;
            if (_headerLabel != null)
            {
                _headerLabel.text = text;
                _headerLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
                UpdateContentInsets();
            }
        }

        public void SetGlowEnabled(bool enabled)
        {
            showGlow = enabled;
            if (_glowImage != null) _glowImage.enabled = enabled;
        }

        public void SetGlowColor(Color color)
        {
            if (_glowImage != null) _glowImage.color = color;
        }

        #endregion

        #region Factory Methods

        /// <summary>Create a themed panel.</summary>
        public static BBGPanel Create(
            Transform parent,
            BBGPanelStyle panelStyle = BBGPanelStyle.Wood,
            Vector2? size = null)
        {
            var go = new GameObject($"BBGPanel_{panelStyle}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size ?? new Vector2(500, 400);

            var panel = go.AddComponent<BBGPanel>();
            panel.style = panelStyle;
            panel.Build();
            return panel;
        }

        /// <summary>Create a parchment card with optional header.</summary>
        public static BBGPanel CreateCard(
            Transform parent,
            string header = null,
            Vector2? size = null)
        {
            var panel = Create(parent, BBGPanelStyle.Parchment, size ?? new Vector2(420, 200));
            if (!string.IsNullOrEmpty(header))
                panel.SetHeader(header);
            return panel;
        }

        /// <summary>Create a full-screen dark overlay panel.</summary>
        public static BBGPanel CreateOverlay(Transform parent)
        {
            var go = new GameObject("BBGOverlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var panel = go.AddComponent<BBGPanel>();
            panel.style = BBGPanelStyle.Dark;
            panel.showBorder = false;
            panel.showGlow = false;
            panel.Build();
            return panel;
        }

        #endregion

        #region Lifecycle

        private void Start()
        {
            if (!_built) Build();
        }

        #endregion

        #region Build

        public void Build()
        {
            if (_built)
            {
                ApplyStyle();
                return;
            }

            _rect = GetComponent<RectTransform>();
            var theme = BBGThemeProvider.Current;

            if (showGlow)
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

            Sprite bgSprite = style switch
            {
                BBGPanelStyle.Wood => BBGSprites.PanelWood,
                BBGPanelStyle.Parchment => BBGSprites.PanelParchment,
                _ => null
            };

            _bgImage = CreateChildImage("_Background", bgSprite);
            _bgImage.type = bgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _bgImage.raycastTarget = true;
            FillParent(_bgImage.rectTransform);

            if (showBorder && style != BBGPanelStyle.Dark)
            {
                _borderImage = CreateChildImage("_Border", BBGSprites.ButtonBrassBorder);
                _borderImage.type = Image.Type.Sliced;
                _borderImage.raycastTarget = false;
                FillParent(_borderImage.rectTransform);
            }

            bool hasHeader = !string.IsNullOrEmpty(headerText);
            if (hasHeader)
            {
                var headerGO = new GameObject("_Header");
                headerGO.transform.SetParent(transform, false);
                _headerLabel = headerGO.AddComponent<TextMeshProUGUI>();
                _headerLabel.text = headerText;
                _headerLabel.fontSize = 22;
                _headerLabel.fontStyle = FontStyles.Bold;
                _headerLabel.alignment = TextAlignmentOptions.Center;
                _headerLabel.raycastTarget = false;
                _headerLabel.enableWordWrapping = false;
                _headerLabel.overflowMode = TextOverflowModes.Ellipsis;

                var headerRect = _headerLabel.rectTransform;
                headerRect.anchorMin = new Vector2(0, 1);
                headerRect.anchorMax = new Vector2(1, 1);
                headerRect.pivot = new Vector2(0.5f, 1);
                headerRect.offsetMin = new Vector2(theme.spacingMd, 0);
                headerRect.offsetMax = new Vector2(-theme.spacingMd, -theme.spacingSm);
                headerRect.sizeDelta = new Vector2(headerRect.sizeDelta.x, 36);
            }

            var contentGO = new GameObject("_Content");
            contentGO.transform.SetParent(transform, false);
            _contentArea = contentGO.AddComponent<RectTransform>();
            UpdateContentInsets();

            ApplyStyle();
            _built = true;
        }

        #endregion

        #region Styling

        private void ApplyStyle()
        {
            var t = BBGThemeProvider.Current;

            switch (style)
            {
                case BBGPanelStyle.Wood:
                    if (_bgImage != null) _bgImage.color = Color.white;
                    if (_borderImage != null) _borderImage.color = t.brass;
                    if (_headerLabel != null) _headerLabel.color = t.treasureGold;
                    if (_glowImage != null) _glowImage.color = BBGThemeProvider.WithAlpha(t.neonCyan, 0.3f);
                    break;

                case BBGPanelStyle.Parchment:
                    if (_bgImage != null) _bgImage.color = Color.white;
                    if (_borderImage != null) _borderImage.color = t.saddleBrown;
                    if (_headerLabel != null) _headerLabel.color = t.darkLeather;
                    if (_glowImage != null) _glowImage.color = BBGThemeProvider.WithAlpha(t.neonAmber, 0.25f);
                    break;

                case BBGPanelStyle.Dark:
                    if (_bgImage != null) _bgImage.color = t.opaqueBlack;
                    if (_borderImage != null) _borderImage.color = t.warmGray;
                    if (_headerLabel != null) _headerLabel.color = t.parchment;
                    if (_glowImage != null) _glowImage.color = BBGThemeProvider.WithAlpha(t.neonCyan, 0.2f);
                    break;
            }
        }

        private void UpdateContentInsets()
        {
            if (_contentArea == null) return;
            var theme = BBGThemeProvider.Current;

            bool hasHeader = _headerLabel != null && _headerLabel.gameObject.activeSelf;
            float topInset = hasHeader
                ? theme.spacingSm + 36 + theme.spacingSm
                : theme.spacingMd;

            _contentArea.anchorMin = Vector2.zero;
            _contentArea.anchorMax = Vector2.one;
            _contentArea.offsetMin = new Vector2(theme.spacingMd, theme.spacingMd);
            _contentArea.offsetMax = new Vector2(-theme.spacingMd, -topInset);
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
