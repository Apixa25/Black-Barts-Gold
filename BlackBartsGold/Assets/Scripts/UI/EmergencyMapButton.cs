// ============================================================================
// EmergencyMapButton.cs  
// Black Bart's Gold - Emergency fallback button for map access
// Path: Assets/Scripts/UI/EmergencyMapButton.cs
// Created: 2026-01-27 - Guaranteed visible map button
// ============================================================================
// This creates a large, impossible-to-miss button using OnGUI.
// OnGUI bypasses the entire Canvas/EventSystem and draws directly.
// If this button appears and works, we know scripts are running.
// If this button doesn't appear, the APK wasn't rebuilt properly.
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Emergency map button that uses OnGUI - the most basic Unity rendering.
    /// This is a diagnostic tool to verify:
    /// 1. Scripts are being compiled into the build
    /// 2. MonoBehaviour lifecycle (Awake/Start/Update) is running
    /// 3. Basic touch input is working
    /// 
    /// If you can see and tap this button, your code is running.
    /// If you can't see this button, the APK needs to be rebuilt.
    /// </summary>
    public class EmergencyMapButton : MonoBehaviour
    {
        private static EmergencyMapButton _instance;
        
        [Header("Button Settings")]
        public bool showButton = true;
        public bool showDebugInfo = true;
        
        private int tapCount = 0;
        private string statusText = "Waiting for tap...";
        private float buttonFlashTimer = 0f;
        private bool isMapOpen = false;
        
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        
        private void Awake()
        {
            // Singleton - persist across scenes
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Debug.Log("!!! EmergencyMapButton AWAKE !!!");
            Debug.Log("!!! If you see this, scripts ARE running !!!");
            Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }
        
        private void Start()
        {
            Debug.Log("!!! EmergencyMapButton START !!!");
        }
        
        private void Update()
        {
            buttonFlashTimer += Time.deltaTime;
        }
        
        private void OnGUI()
        {
            // Initialize styles if needed
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 32;
                buttonStyle.fontStyle = FontStyle.Bold;
                
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 18;
                labelStyle.normal.textColor = Color.white;
            }
            
            // Flash the button color
            float flash = Mathf.PingPong(buttonFlashTimer * 2f, 1f);
            
            if (showButton)
            {
                // Large button in bottom-right corner
                float btnWidth = 200;
                float btnHeight = 80;
                float margin = 20;
                float x = Screen.width - btnWidth - margin;
                float y = Screen.height - btnHeight - margin - 100; // Above potential radar
                
                // Background flash
                GUI.color = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(0.4f, 0.8f, 1f), flash);
                
                string btnText = isMapOpen ? "CLOSE MAP" : "OPEN MAP";
                
                if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), btnText, buttonStyle))
                {
                    OnButtonPressed();
                }
                
                // Reset color
                GUI.color = Color.white;
            }
            
            if (showDebugInfo)
            {
                // Debug info panel at top
                float panelWidth = 400;
                float panelHeight = 100;
                
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.DrawTexture(new Rect(10, 10, panelWidth, panelHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;
                
                GUI.Label(new Rect(20, 15, panelWidth - 20, 25), 
                    "EMERGENCY MAP BUTTON ACTIVE", labelStyle);
                GUI.Label(new Rect(20, 40, panelWidth - 20, 25), 
                    $"Taps: {tapCount} | Status: {statusText}", labelStyle);
                GUI.Label(new Rect(20, 65, panelWidth - 20, 25), 
                    $"Screen: {Screen.width}x{Screen.height} | Time: {Time.time:F1}s", labelStyle);
            }
        }
        
        private void OnButtonPressed()
        {
            tapCount++;
            Debug.Log($"!!! EmergencyMapButton PRESSED - tap #{tapCount} !!!");
            
            if (!isMapOpen)
            {
                // Try to open map
                OpenMap();
            }
            else
            {
                // Try to close map
                CloseMap();
            }
        }
        
        private void OpenMap()
        {
            statusText = "Opening map...";
            Debug.Log("!!! EmergencyMapButton: Opening map !!!");
            
            // Try FullMapUI singleton
            if (FullMapUI.Exists)
            {
                FullMapUI.Instance.Show();
                isMapOpen = true;
                statusText = "Map opened via FullMapUI";
                return;
            }
            
            // Try to find FullMapPanel directly
            var fullMapPanel = GameObject.Find("FullMapPanel");
            if (fullMapPanel != null)
            {
                fullMapPanel.SetActive(true);
                isMapOpen = true;
                statusText = "Map opened via GameObject.Find";
                return;
            }
            
            // Try ARHUD
            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.OnRadarTapped();
                isMapOpen = true;
                statusText = "Map opened via ARHUD";
                return;
            }
            
            statusText = "ERROR: No map found!";
            Debug.LogError("!!! EmergencyMapButton: Could not find any map to open !!!");
        }
        
        private void CloseMap()
        {
            statusText = "Closing map...";
            Debug.Log("!!! EmergencyMapButton: Closing map !!!");
            
            if (FullMapUI.Exists && FullMapUI.Instance.IsVisible)
            {
                FullMapUI.Instance.Hide();
                isMapOpen = false;
                statusText = "Map closed";
                return;
            }
            
            var fullMapPanel = GameObject.Find("FullMapPanel");
            if (fullMapPanel != null && fullMapPanel.activeSelf)
            {
                fullMapPanel.SetActive(false);
                isMapOpen = false;
                statusText = "Map closed";
                return;
            }
            
            isMapOpen = false;
            statusText = "Map already closed";
        }
        
        /// <summary>
        /// Create this component on any GameObject to enable the emergency button.
        /// Call this from any script to ensure the button exists.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance == null)
            {
                var go = new GameObject("EmergencyMapButton");
                go.AddComponent<EmergencyMapButton>();
            }
        }
    }
}
