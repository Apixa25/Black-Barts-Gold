// ============================================================================
// ARHuntSceneSetup.cs
// Black Bart's Gold - ARHunt Scene Setup
// Path: Assets/Scripts/UI/ARHuntSceneSetup.cs
// ============================================================================
// Sets up the AR Hunt scene HUD overlay at runtime.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    public class ARHuntSceneSetup : MonoBehaviour
    {
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color SemiTransparentBlack = new Color(0, 0, 0, 0.5f);

        private void Start()
        {
            Debug.Log("[ARHuntSceneSetup] Setting up AR HUD...");
            
            SetupCanvas();
            SetupBackButton();
            SetupCrosshairs();
            
            Debug.Log("[ARHuntSceneSetup] AR HUD setup complete!");
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render on top of AR
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupBackButton()
        {
            var btn = transform.Find("BackButton");
            if (btn == null) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position in top-left corner
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(30, -50);
                rect.sizeDelta = new Vector2(120, 60);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = SemiTransparentBlack;
            }

            // Add text
            var textTransform = btn.Find("Text");
            if (textTransform == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btn, false);
                textTransform = textGO.transform;
                
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                var tmpText = textGO.AddComponent<TextMeshProUGUI>();
                tmpText.text = "← Back";
                tmpText.fontSize = 24;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.color = Color.white;
            }
        }

        private void SetupCrosshairs()
        {
            var crosshairs = transform.Find("Crosshairs");
            if (crosshairs == null) return;

            // Add RectTransform if missing
            var rect = crosshairs.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = crosshairs.gameObject.AddComponent<RectTransform>();
            }

            // Center of screen
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100, 100);

            // Add image for crosshairs
            var image = crosshairs.GetComponent<Image>();
            if (image == null)
            {
                image = crosshairs.gameObject.AddComponent<Image>();
            }
            
            image.color = new Color(1, 1, 1, 0.7f);
            image.raycastTarget = false;
            
            // Create simple crosshair using text
            var textTransform = crosshairs.Find("CrosshairText");
            if (textTransform == null)
            {
                var textGO = new GameObject("CrosshairText");
                textGO.transform.SetParent(crosshairs, false);
                
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                var tmpText = textGO.AddComponent<TextMeshProUGUI>();
                tmpText.text = "⊕";
                tmpText.fontSize = 72;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.color = GoldColor;
                tmpText.raycastTarget = false;
            }
            
            // Hide the background image, just show crosshair symbol
            image.color = new Color(0, 0, 0, 0);
        }
    }
}
