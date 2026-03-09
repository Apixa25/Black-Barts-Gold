using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlackBartsGold.Companion.Models;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Small runtime-built quick-prompt panel for the AR hunt view.
    /// </summary>
    public class CompanionIntentPanel : MonoBehaviour
    {
        private readonly List<Button> _buttons = new List<Button>();

        private CanvasGroup _canvasGroup;
        private GridLayoutGroup _buttonGrid;
        private TextMeshProUGUI _titleText;

        private void Awake()
        {
            BuildRuntimeUi();
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
            rect.anchoredPosition = new Vector2(0f, 220f);
            rect.sizeDelta = new Vector2(760f, 190f);

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.08f, 0.72f);
            background.raycastTarget = true;

            var titleGo = EnsureChild("Title");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -12f);
            titleRect.sizeDelta = new Vector2(-24f, 34f);

            _titleText = titleGo.GetComponent<TextMeshProUGUI>();
            if (_titleText == null) _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "Ask Black Bart";
            _titleText.fontSize = 22f;
            _titleText.color = new Color(1f, 0.84f, 0f, 1f);
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.enableWordWrapping = false;
            _titleText.fontStyle = FontStyles.Bold;

            var gridGo = EnsureChild("PromptGrid");
            var gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(18f, 16f);
            gridRect.offsetMax = new Vector2(-18f, -52f);

            _buttonGrid = gridGo.GetComponent<GridLayoutGroup>();
            if (_buttonGrid == null) _buttonGrid = gridGo.AddComponent<GridLayoutGroup>();
            _buttonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _buttonGrid.constraintCount = 3;
            _buttonGrid.cellSize = new Vector2(220f, 46f);
            _buttonGrid.spacing = new Vector2(12f, 12f);
            _buttonGrid.childAlignment = TextAnchor.UpperCenter;
            _buttonGrid.startAxis = GridLayoutGroup.Axis.Horizontal;

            var fitter = gridGo.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gridGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
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
            if (_buttonGrid == null)
            {
                return;
            }

            int promptCount = prompts != null ? prompts.Count : 0;
            EnsureButtonPool(promptCount);

            for (int i = 0; i < _buttons.Count; i++)
            {
                bool shouldShow = prompts != null && i < prompts.Count;
                _buttons[i].gameObject.SetActive(shouldShow);
                if (!shouldShow) continue;

                var prompt = prompts[i];
                BindButton(_buttons[i], prompt);
            }

            SetVisible(promptCount > 0);
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
            var buttonGo = new GameObject($"PromptButton{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_buttonGrid.transform, false);

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
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = 19f;
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
                BlackBartCompanionService.EnsureInstance().SubmitIntent(prompt.intentType);
            });
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
