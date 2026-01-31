// ============================================================================
// RegisterButtonHandler.cs
// Simple Register scene controller - creates UI and handles navigation
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace BlackBartsGold.UI
{
    public class RegisterButtonHandler : MonoBehaviour
    {
        private Button backButton;
        
        private IEnumerator Start()
        {
            Debug.Log("[RegisterButtonHandler] Start - waiting a frame...");
            
            // Wait a frame to let other systems initialize
            yield return null;
            
            Debug.Log("[RegisterButtonHandler] Creating UI...");
            CreateUI();
        }
        
        private void CreateUI()
        {
            try
            {
                // Find existing canvas or create new one
                Canvas canvas = FindAnyObjectByType<Canvas>();
                
                if (canvas == null)
                {
                    Debug.Log("[RegisterButtonHandler] Creating canvas...");
                    var canvasGO = new GameObject("RegisterCanvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
                
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                Debug.Log($"[RegisterButtonHandler] Using canvas: {canvas.name}");
                
                // Create a full-screen panel with dark background
                var panel = new GameObject("Panel");
                panel.transform.SetParent(canvas.transform, false);
                var panelRect = panel.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                var panelImage = panel.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.15f, 0.25f, 1f);
                
                // Create title
                var title = new GameObject("Title");
                title.transform.SetParent(canvas.transform, false);
                var titleRect = title.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0.75f);
                titleRect.anchorMax = new Vector2(0.5f, 0.75f);
                titleRect.sizeDelta = new Vector2(500, 80);
                var titleText = title.AddComponent<TextMeshProUGUI>();
                titleText.text = "Join the Crew!";
                titleText.fontSize = 42;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.84f, 0f, 1f);
                
                // Create info text
                var info = new GameObject("Info");
                info.transform.SetParent(canvas.transform, false);
                var infoRect = info.AddComponent<RectTransform>();
                infoRect.anchorMin = new Vector2(0.5f, 0.55f);
                infoRect.anchorMax = new Vector2(0.5f, 0.55f);
                infoRect.sizeDelta = new Vector2(400, 100);
                var infoText = info.AddComponent<TextMeshProUGUI>();
                infoText.text = "Registration coming soon!\n\nTap below to return.";
                infoText.fontSize = 22;
                infoText.alignment = TextAlignmentOptions.Center;
                infoText.color = Color.white;
                
                // Create back button
                var btnGO = new GameObject("BackButton");
                btnGO.transform.SetParent(canvas.transform, false);
                var btnRect = btnGO.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0.3f);
                btnRect.anchorMax = new Vector2(0.5f, 0.3f);
                btnRect.sizeDelta = new Vector2(280, 55);
                var btnImage = btnGO.AddComponent<Image>();
                btnImage.color = new Color(0.2f, 0.5f, 0.3f, 1f);
                backButton = btnGO.AddComponent<Button>();
                backButton.targetGraphic = btnImage;
                
                // Button text
                var btnText = new GameObject("Text");
                btnText.transform.SetParent(btnGO.transform, false);
                var btnTextRect = btnText.AddComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;
                var btnTextTMP = btnText.AddComponent<TextMeshProUGUI>();
                btnTextTMP.text = "Back to Login";
                btnTextTMP.fontSize = 22;
                btnTextTMP.alignment = TextAlignmentOptions.Center;
                btnTextTMP.color = Color.white;
                
                // Wire button
                backButton.onClick.AddListener(() => {
                    Debug.Log("[RegisterButtonHandler] Back clicked!");
                    SceneManager.LoadScene("Login");
                });
                
                Debug.Log("[RegisterButtonHandler] ✅ UI created and button wired!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RegisterButtonHandler] Error creating UI: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
