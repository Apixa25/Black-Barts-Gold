using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlackBartsGold.Companion.Models;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Compact launcher + dropdown for Black Bart quick prompts so the AR view
    /// keeps more screen space free when prompts are not actively being used.
    /// </summary>
    public class CompanionIntentPanel : MonoBehaviour
    {
        private readonly List<Button> _buttons = new List<Button>();

        private CanvasGroup _canvasGroup;
        private CanvasGroup _dropdownCanvasGroup;
        private VerticalLayoutGroup _dropdownLayout;
        private Button _toggleButton;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _hintText;
        private TextMeshProUGUI _caretText;
        private bool _isExpanded;
        private int _visiblePromptCount;

        private void Awake()
        {
            BuildRuntimeUi();
            SetExpanded(false);
            SetVisible(false);
        }

        private void Start()
        {
            var service = BlackBartCompanionService.EnsureInstance();
            service.OnQuickPromptsUpdated += HandleQuickPromptsUpdated;
            HandleQuickPromptsUpdated(new List<CompanionQuickPromptDto>(service.QuickPrompts));
        }

        private void OnDestroy()
        {
            if (BlackBartCompanionService.Exists)
            {
                BlackBartCompanionService.Instance.OnQuickPromptsUpdated -= HandleQuickPromptsUpdated;
            }
        }

        private void BuildRuntimeUi()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(460f, 78f);

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
            background.raycastTarget = true;

            _toggleButton = GetComponent<Button>();
            if (_toggleButton == null) _toggleButton = gameObject.AddComponent<Button>();
            _toggleButton.transition = Selectable.Transition.ColorTint;
            _toggleButton.onClick.RemoveAllListeners();
            _toggleButton.onClick.AddListener(ToggleExpanded);

            var toggleColors = _toggleButton.colors;
            toggleColors.normalColor = background.color;
            toggleColors.highlightedColor = new Color(0.16f, 0.12f, 0.06f, 0.92f);
            toggleColors.pressedColor = new Color(0.28f, 0.2f, 0.08f, 0.96f);
            toggleColors.selectedColor = toggleColors.highlightedColor;
            toggleColors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.45f);
            _toggleButton.colors = toggleColors;

            var titleGo = EnsureChild("Title");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.offsetMin = new Vector2(18f, -4f);
            titleRect.offsetMax = new Vector2(-80f, 18f);

            _titleText = titleGo.GetComponent<TextMeshProUGUI>();
            if (_titleText == null) _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "Ask Black Bart";
            _titleText.fontSize = 34f;
            _titleText.color = new Color(1f, 0.84f, 0f, 1f);
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _titleText.enableWordWrapping = false;
            _titleText.fontStyle = FontStyles.Bold;

            var hintGo = EnsureChild("Hint");
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0.5f);
            hintRect.anchorMax = new Vector2(1f, 0.5f);
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.offsetMin = new Vector2(20f, -28f);
            hintRect.offsetMax = new Vector2(-80f, -6f);

            _hintText = hintGo.GetComponent<TextMeshProUGUI>();
            if (_hintText == null) _hintText = hintGo.AddComponent<TextMeshProUGUI>();
            _hintText.text = "Tap to open prompts";
            _hintText.fontSize = 22f;
            _hintText.color = new Color(1f, 0.96f, 0.86f, 0.86f);
            _hintText.alignment = TextAlignmentOptions.MidlineLeft;
            _hintText.enableWordWrapping = false;

            var caretGo = EnsureChild("Caret");
            var caretRect = caretGo.GetComponent<RectTransform>();
            caretRect.anchorMin = new Vector2(1f, 0.5f);
            caretRect.anchorMax = new Vector2(1f, 0.5f);
            caretRect.pivot = new Vector2(1f, 0.5f);
            caretRect.sizeDelta = new Vector2(56f, 40f);
            caretRect.anchoredPosition = new Vector2(-16f, 0f);

            _caretText = caretGo.GetComponent<TextMeshProUGUI>();
            if (_caretText == null) _caretText = caretGo.AddComponent<TextMeshProUGUI>();
            _caretText.text = "▼";
            _caretText.fontSize = 34f;
            _caretText.color = new Color(1f, 0.96f, 0.86f, 1f);
            _caretText.alignment = TextAlignmentOptions.Center;
            _caretText.enableWordWrapping = false;
            _caretText.fontStyle = FontStyles.Bold;

            var dropdownGo = EnsureChild("DropdownPanel");
            var dropdownRect = dropdownGo.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.5f, 1f);
            dropdownRect.anchorMax = new Vector2(0.5f, 1f);
            dropdownRect.pivot = new Vector2(0.5f, 0f);
            dropdownRect.anchoredPosition = new Vector2(0f, 12f);
            dropdownRect.sizeDelta = new Vector2(460f, 0f);

            _dropdownCanvasGroup = dropdownGo.GetComponent<CanvasGroup>();
            if (_dropdownCanvasGroup == null) _dropdownCanvasGroup = dropdownGo.AddComponent<CanvasGroup>();

            var dropdownImage = dropdownGo.GetComponent<Image>();
            if (dropdownImage == null) dropdownImage = dropdownGo.AddComponent<Image>();
            dropdownImage.color = new Color(0.05f, 0.05f, 0.08f, 0.86f);
            dropdownImage.raycastTarget = true;

            _dropdownLayout = dropdownGo.GetComponent<VerticalLayoutGroup>();
            if (_dropdownLayout == null) _dropdownLayout = dropdownGo.AddComponent<VerticalLayoutGroup>();
            _dropdownLayout.padding = new RectOffset(14, 14, 14, 14);
            _dropdownLayout.spacing = 10f;
            _dropdownLayout.childAlignment = TextAnchor.LowerCenter;
            _dropdownLayout.childControlHeight = false;
            _dropdownLayout.childControlWidth = true;
            _dropdownLayout.childForceExpandHeight = false;
            _dropdownLayout.childForceExpandWidth = true;

            var fitter = dropdownGo.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = dropdownGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GameObject EnsureChild(string childName)
        {
            var existing = transform.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(transform, false);
            return child;
        }

        private void HandleQuickPromptsUpdated(List<CompanionQuickPromptDto> prompts)
        {
            if (_dropdownLayout == null)
            {
                return;
            }

            _visiblePromptCount = prompts != null ? prompts.Count : 0;
            EnsureButtonPool(_visiblePromptCount);

            for (int i = 0; i < _buttons.Count; i++)
            {
                bool shouldShow = prompts != null && i < prompts.Count;
                _buttons[i].gameObject.SetActive(shouldShow);
                if (!shouldShow) continue;

                BindButton(_buttons[i], prompts[i]);
            }

            UpdateLauncherHint();
            if (_visiblePromptCount == 0)
            {
                SetExpanded(false);
            }

            SetVisible(_visiblePromptCount > 0);
        }

        private void EnsureButtonPool(int count)
        {
            while (_buttons.Count < count)
            {
                _buttons.Add(CreatePromptButton(_buttons.Count));
            }
        }

        private Button CreatePromptButton(int index)
        {
            var buttonGo = new GameObject(
                $"PromptButton{index + 1}",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(Button));
            buttonGo.transform.SetParent(_dropdownLayout.transform, false);

            var layoutElement = buttonGo.GetComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            layoutElement.preferredHeight = 60f;
            layoutElement.flexibleHeight = 0f;

            var image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.16f, 0.12f, 0.06f, 0.92f);
            image.raycastTarget = true;

            var button = buttonGo.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.26f, 0.2f, 0.1f, 0.96f);
            colors.pressedColor = new Color(0.38f, 0.28f, 0.12f, 0.98f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
            button.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(buttonGo.transform, false);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);

            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = 28f;
            label.color = new Color(1f, 0.96f, 0.86f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.text = "Prompt";

            return button;
        }

        private void BindButton(Button button, CompanionQuickPromptDto prompt)
        {
            if (button == null || prompt == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = !string.IsNullOrEmpty(prompt.shortLabel) ? prompt.shortLabel : prompt.label;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                SetExpanded(false);
                BlackBartCompanionService.EnsureInstance().SubmitIntent(prompt.intentType);
            });
        }

        private void ToggleExpanded()
        {
            if (_visiblePromptCount <= 0)
            {
                return;
            }

            SetExpanded(!_isExpanded);
        }

        private void SetExpanded(bool expanded)
        {
            _isExpanded = expanded && _visiblePromptCount > 0;

            if (_dropdownCanvasGroup != null)
            {
                _dropdownCanvasGroup.alpha = _isExpanded ? 1f : 0f;
                _dropdownCanvasGroup.interactable = _isExpanded;
                _dropdownCanvasGroup.blocksRaycasts = _isExpanded;
            }

            if (_caretText != null)
            {
                _caretText.text = _isExpanded ? "▲" : "▼";
            }

            UpdateLauncherHint();
        }

        private void UpdateLauncherHint()
        {
            if (_hintText == null)
            {
                return;
            }

            if (_visiblePromptCount <= 0)
            {
                _hintText.text = "Waiting for prompts";
                return;
            }

            if (_isExpanded)
            {
                _hintText.text = "Tap a question below";
                return;
            }

            _hintText.text = $"{_visiblePromptCount} quick prompt{(_visiblePromptCount == 1 ? string.Empty : "s")} available";
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;

            if (!visible)
            {
                SetExpanded(false);
            }
        }
    }
}
