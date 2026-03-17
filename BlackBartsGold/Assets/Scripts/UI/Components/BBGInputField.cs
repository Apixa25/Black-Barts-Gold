// ============================================================================
// BBGInputField.cs
// Black Bart's Gold — Themed Text Input Component
// Path: Assets/Scripts/UI/Components/BBGInputField.cs
// ============================================================================
// Layered input field with leather background, brass border, gold caret,
// and themed placeholder/text colors. Wraps TMP_InputField.
//
// Creation:
//   var input = BBGInputField.Create(parent, "Email address");
//   input.onValueChanged.AddListener(v => Validate(v));
//
//   var pwInput = BBGInputField.Create(parent, "Password",
//       contentType: TMP_InputField.ContentType.Password);
//   string password = pwInput.Text;
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class BBGInputField : MonoBehaviour
    {
        #region Inspector Fields

        [Header("BBG Input Field")]
        [SerializeField] private string placeholder = "Enter text...";
        [SerializeField] private int fontSize = 28;
        [SerializeField] private int characterLimit = 0;
        [SerializeField] private TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;

        #endregion

        #region Runtime State

        private Image _bgImage;
        private Image _borderImage;
        private TMP_InputField _inputField;
        private TextMeshProUGUI _placeholderText;
        private TextMeshProUGUI _inputText;
        private RectTransform _rect;
        private bool _built;

        #endregion

        #region Public API

        public TMP_InputField InputField
        {
            get
            {
                if (!_built) Build();
                return _inputField;
            }
        }

        public string Text
        {
            get => InputField.text;
            set => InputField.text = value;
        }

        public TMP_InputField.OnChangeEvent onValueChanged => InputField.onValueChanged;
        public TMP_InputField.SubmitEvent onEndEdit => InputField.onEndEdit;

        public RectTransform RectTransform => _rect != null ? _rect : GetComponent<RectTransform>();

        public void SetPlaceholder(string text)
        {
            placeholder = text;
            if (_placeholderText != null) _placeholderText.text = text;
        }

        public void SetContentType(TMP_InputField.ContentType type)
        {
            contentType = type;
            if (_inputField != null) _inputField.contentType = type;
        }

        public void SetCharacterLimit(int limit)
        {
            characterLimit = limit;
            if (_inputField != null) _inputField.characterLimit = limit;
        }

        public void SetInteractable(bool interactable)
        {
            if (_inputField != null) _inputField.interactable = interactable;
            if (_bgImage != null)
            {
                Color c = _bgImage.color;
                _bgImage.color = new Color(c.r, c.g, c.b, interactable ? 1f : 0.5f);
            }
        }

        /// <summary>Flash the border red briefly for validation errors.</summary>
        public void FlashError()
        {
            if (_borderImage != null)
                StartCoroutine(ErrorFlashRoutine());
        }

        #endregion

        #region Factory

        /// <summary>Create a fully themed input field.</summary>
        public static BBGInputField Create(
            Transform parent,
            string placeholderText,
            TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard,
            int characterLimit = 0,
            Vector2? size = null)
        {
            string safeName = placeholderText.Replace(" ", "");
            var go = new GameObject($"BBGInput_{safeName}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? new Vector2(500, 70);

            var input = go.AddComponent<BBGInputField>();
            input.placeholder = placeholderText;
            input.contentType = contentType;
            input.characterLimit = characterLimit;
            input.Build();
            return input;
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
            if (_built) return;

            _rect = GetComponent<RectTransform>();
            var theme = BBGThemeProvider.Current;
            float pad = theme.spacingMd;
            float padV = theme.spacingSm;

            _bgImage = CreateChildImage("_Background", BBGSprites.ButtonLeather);
            _bgImage.type = Image.Type.Sliced;
            _bgImage.raycastTarget = true;
            FillParent(_bgImage.rectTransform);

            _borderImage = CreateChildImage("_Border", BBGSprites.ButtonBrassBorder);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;
            _borderImage.color = theme.brass;
            FillParent(_borderImage.rectTransform);

            var textAreaGO = new GameObject("_TextArea", typeof(RectTransform));
            textAreaGO.transform.SetParent(transform, false);
            textAreaGO.AddComponent<RectMask2D>();
            var textAreaRect = textAreaGO.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(pad, padV);
            textAreaRect.offsetMax = new Vector2(-pad, -padV);

            var placeholderGO = new GameObject("_Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            _placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            _placeholderText.text = placeholder;
            _placeholderText.fontSize = fontSize;
            _placeholderText.fontStyle = FontStyles.Italic;
            _placeholderText.alignment = TextAlignmentOptions.Left;
            _placeholderText.color = BBGThemeProvider.WithAlpha(theme.warmTan, 0.6f);
            _placeholderText.raycastTarget = false;
            _placeholderText.enableWordWrapping = false;
            _placeholderText.overflowMode = TextOverflowModes.Ellipsis;
            FillParent(_placeholderText.rectTransform);

            var textGO = new GameObject("_Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            _inputText = textGO.AddComponent<TextMeshProUGUI>();
            _inputText.text = "";
            _inputText.fontSize = fontSize;
            _inputText.alignment = TextAlignmentOptions.Left;
            _inputText.color = theme.parchment;
            _inputText.raycastTarget = false;
            _inputText.enableWordWrapping = false;
            _inputText.overflowMode = TextOverflowModes.Overflow;
            FillParent(_inputText.rectTransform);

            _inputField = gameObject.AddComponent<TMP_InputField>();
            _inputField.textViewport = textAreaRect;
            _inputField.textComponent = _inputText;
            _inputField.placeholder = _placeholderText;
            _inputField.targetGraphic = _bgImage;
            _inputField.contentType = contentType;
            _inputField.characterLimit = characterLimit;
            _inputField.caretColor = theme.treasureGold;
            _inputField.selectionColor = BBGThemeProvider.WithAlpha(theme.treasureGold, 0.25f);
            _inputField.caretWidth = 2;
            _inputField.customCaretColor = true;

            var colors = _inputField.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            _inputField.colors = colors;

            _built = true;
        }

        #endregion

        #region Error Flash

        private System.Collections.IEnumerator ErrorFlashRoutine()
        {
            var theme = BBGThemeProvider.Current;
            Color original = _borderImage.color;
            Color errorColor = theme.danger;
            int cycles = theme.errorShakeCycles;
            float cycleDuration = theme.errorShakeDuration;

            for (int i = 0; i < cycles; i++)
            {
                _borderImage.color = errorColor;
                yield return new WaitForSecondsRealtime(cycleDuration);
                _borderImage.color = original;
                yield return new WaitForSecondsRealtime(cycleDuration);
            }

            _borderImage.color = original;
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
