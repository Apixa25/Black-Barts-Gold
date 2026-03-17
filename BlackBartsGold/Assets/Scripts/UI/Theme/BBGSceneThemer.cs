// ============================================================================
// BBGSceneThemer.cs
// Black Bart's Gold — Automatic UI Theme Upgrade Utility
// Path: Assets/Scripts/UI/Theme/BBGSceneThemer.cs
// ============================================================================
// Drop this component onto any Canvas to auto-upgrade plain buttons, panels,
// and backgrounds to the BBG western-steampunk theme. Runs once after a
// 1-frame delay so existing scene-setup scripts finish first.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace BlackBartsGold.UI
{
    [DisallowMultipleComponent]
    public class BBGSceneThemer : MonoBehaviour
    {
        [Header("What to Theme")]
        [SerializeField] private bool themeButtons = true;
        [SerializeField] private bool themeBackground = true;
        [SerializeField] private bool themeTextColors = true;

        [Header("Background")]
        [SerializeField] private string backgroundObjectName = "Background";

        private bool _applied;

        private IEnumerator Start()
        {
            yield return null;
            if (!_applied) ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (_applied) return;
            _applied = true;

            if (themeBackground) ThemeBackground();
            if (themeButtons) ThemeAllButtons();
            if (themeTextColors) ThemeTextElements();

            Debug.Log($"[BBGSceneThemer] Theme applied to {gameObject.name}");
        }

        private void ThemeBackground()
        {
            var bg = transform.Find(backgroundObjectName);
            if (bg == null) return;

            var img = bg.GetComponent<Image>();
            if (img == null) return;

            img.sprite = BBGSprites.PanelWood;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.2f, 0.15f, 1f);
        }

        private void ThemeAllButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.GetComponent<BBGButton>() != null) continue;

                string lowerName = button.gameObject.name.ToLowerInvariant();
                BBGButtonVariant variant;
                if (lowerName.Contains("start") || lowerName.Contains("save") || lowerName.Contains("confirm"))
                    variant = BBGButtonVariant.Primary;
                else if (lowerName.Contains("cancel") || lowerName.Contains("back") || lowerName.Contains("close"))
                    variant = BBGButtonVariant.Ghost;
                else if (lowerName.Contains("delete") || lowerName.Contains("danger"))
                    variant = BBGButtonVariant.Danger;
                else
                    variant = BBGButtonVariant.Secondary;

                BBGButton.Upgrade(button.gameObject, variant);
            }
        }

        private void ThemeTextElements()
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text.GetComponentInParent<BBGButton>() != null) continue;
                if (text.GetComponentInParent<BBGInputField>() != null) continue;
                if (text.GetComponentInParent<BBGPanel>() != null) continue;

                bool isTitle = text.fontSize >= 40 || text.gameObject.name.ToLowerInvariant().Contains("title");
                if (isTitle)
                    text.color = BBGThemeProvider.Gold;
                else
                    text.color = BBGThemeProvider.Parchment;
            }
        }

        public static BBGSceneThemer AddTo(Canvas canvas)
        {
            if (canvas == null) return null;
            var existing = canvas.GetComponent<BBGSceneThemer>();
            if (existing != null) return existing;
            return canvas.gameObject.AddComponent<BBGSceneThemer>();
        }
    }
}
