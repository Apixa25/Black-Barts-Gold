// ============================================================================
// LoginSceneSetup.cs
// Black Bart's Gold - Login Scene Complete Setup
// Path: Assets/Scripts/UI/LoginSceneSetup.cs
// ============================================================================
// Properly sets up all Login UI elements at runtime with correct positioning.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    public class LoginSceneSetup : MonoBehaviour
    {
        // Colors from project vision
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        private readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        private readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);

        private void Start()
        {
            Debug.Log("[LoginSceneSetup] Setting up Login UI...");
            
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupLoginButton();
            SetupCreateAccountButton();
            
            Debug.Log("[LoginSceneSetup] Login UI setup complete!");
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupBackground()
        {
            var bg = transform.Find("Background");
            if (bg == null) return;

            var rect = bg.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Stretch to fill entire screen
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var image = bg.GetComponent<Image>();
            if (image != null)
            {
                image.color = DeepSeaBlue;
                // CRITICAL: Disable raycast so it doesn't block button clicks!
                image.raycastTarget = false;
            }
            
            // Move background to be first sibling so buttons render on top
            bg.SetAsFirstSibling();
        }

        private void SetupTitle()
        {
            var title = transform.Find("TitleText");
            if (title == null) return;

            var rect = title.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -200);
                rect.sizeDelta = new Vector2(900, 150);
            }

            var text = title.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = "🏴‍☠️ Ahoy, Matey! 🏴‍☠️";
                text.fontSize = 64;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = GoldColor;
                text.raycastTarget = false; // Don't block touches
            }
        }

        private void SetupLoginButton()
        {
            var btn = transform.Find("LoginButton");
            if (btn == null) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, 50);
                rect.sizeDelta = new Vector2(600, 100);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = GoldColor;
            }

            // Setup button text
            SetupButtonText(btn, "⚓ SET SAIL", 36);
        }

        private void SetupCreateAccountButton()
        {
            var btn = transform.Find("CreateAccountButton");
            if (btn == null) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, -80);
                rect.sizeDelta = new Vector2(500, 80);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = Parchment;
            }

            // Setup button text
            SetupButtonText(btn, "🏴‍☠️ Join the Crew", 28);
        }

        private void SetupButtonText(Transform button, string label, int fontSize)
        {
            // Find or create text child
            var textTransform = button.Find("Text");
            if (textTransform == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(button, false);
                textTransform = textGO.transform;
            }

            // Ensure RectTransform
            var textRect = textTransform.GetComponent<RectTransform>();
            if (textRect == null)
            {
                textRect = textTransform.gameObject.AddComponent<RectTransform>();
            }

            // Stretch to fill button
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            // Ensure TMP_Text
            var tmpText = textTransform.GetComponent<TMP_Text>();
            if (tmpText == null)
            {
                tmpText = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
            }

            tmpText.text = label;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = DarkBrown;
        }
    }
}
