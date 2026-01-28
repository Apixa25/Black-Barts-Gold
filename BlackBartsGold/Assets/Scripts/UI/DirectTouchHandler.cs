// ============================================================================
// DirectTouchHandler.cs
// Black Bart's Gold - Direct Touch Detection (Pokemon GO Style)
// Path: Assets/Scripts/UI/DirectTouchHandler.cs
// Created: 2026-01-27 - Bulletproof touch handling
// ============================================================================
// This script bypasses Unity's EventSystem entirely and detects touches
// directly using Input.touchCount. This is the most reliable way to handle
// touches on mobile, following Pokemon GO's approach.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Direct touch handler that bypasses Unity's EventSystem.
    /// Attach to any Canvas - it will detect all touches and route them
    /// to the appropriate UI elements.
    /// 
    /// Pokemon GO Pattern: Direct touch detection is more reliable than
    /// Unity's UI system on mobile devices.
    /// </summary>
    public class DirectTouchHandler : MonoBehaviour
    {
        [Header("UI Panels to Detect")]
        [SerializeField] private RectTransform radarPanel;
        [SerializeField] private RectTransform fullMapPanel;
        
        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enableDebugVisuals = true;
        
        [Header("Runtime Status")]
        [SerializeField] private string lastTouchInfo = "No touch yet";
        [SerializeField] private int totalTouchCount = 0;
        
        private Canvas parentCanvas;
        private Camera uiCamera;
        private bool initialized = false;
        
        // Touch state
        private bool wasTouching = false;
        private Vector2 lastTouchPosition;
        
        private void Awake()
        {
            Log("========================================");
            Log("DirectTouchHandler AWAKE");
            Log($"GameObject: {gameObject.name}");
            Log("========================================");
        }
        
        private void Start()
        {
            Log("DirectTouchHandler START - Initializing...");
            Initialize();
        }
        
        private void Initialize()
        {
            // Find parent Canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
            }
            
            if (parentCanvas == null)
            {
                LogError("No Canvas found! Touch detection won't work.");
                return;
            }
            
            Log($"Found Canvas: {parentCanvas.name}, RenderMode: {parentCanvas.renderMode}");
            
            // Get camera based on render mode
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null; // Overlay mode doesn't use camera
                Log("Canvas is ScreenSpaceOverlay - no camera needed");
            }
            else
            {
                uiCamera = parentCanvas.worldCamera;
                if (uiCamera == null)
                {
                    uiCamera = Camera.main;
                }
                Log($"Canvas camera: {(uiCamera != null ? uiCamera.name : "NULL")}");
            }
            
            // Auto-find radar panel if not assigned
            if (radarPanel == null)
            {
                Transform radar = parentCanvas.transform.Find("RadarPanel");
                if (radar != null)
                {
                    radarPanel = radar.GetComponent<RectTransform>();
                    Log($"Auto-found RadarPanel: {radarPanel.name}");
                }
                else
                {
                    LogError("RadarPanel not found in Canvas!");
                }
            }
            
            // Auto-find full map panel if not assigned
            if (fullMapPanel == null)
            {
                Transform fullMap = parentCanvas.transform.Find("FullMapPanel");
                if (fullMap != null)
                {
                    fullMapPanel = fullMap.GetComponent<RectTransform>();
                    Log($"Auto-found FullMapPanel: {fullMapPanel.name}");
                }
            }
            
            // Log radar rect info
            if (radarPanel != null)
            {
                Log($"Radar Rect: pos={radarPanel.anchoredPosition}, size={radarPanel.rect.size}");
                Log($"Radar Screen Bounds: {GetScreenBounds(radarPanel)}");
            }
            
            initialized = true;
            Log("DirectTouchHandler initialized successfully!");
        }
        
        private void Update()
        {
            if (!initialized) return;
            
            // Handle touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    HandleTouchBegan(touch.position);
                }
            }
            // Also handle mouse for editor testing
            else if (Input.GetMouseButtonDown(0))
            {
                HandleTouchBegan(Input.mousePosition);
            }
        }
        
        private void HandleTouchBegan(Vector2 screenPosition)
        {
            totalTouchCount++;
            lastTouchPosition = screenPosition;
            lastTouchInfo = $"Touch #{totalTouchCount} at {screenPosition}";
            
            Log($"=== TOUCH DETECTED ===");
            Log($"Screen Position: {screenPosition}");
            Log($"Screen Size: {Screen.width}x{Screen.height}");
            
            // Check if touch is on radar
            if (radarPanel != null && IsPointInRect(screenPosition, radarPanel))
            {
                Log("TOUCH IS ON RADAR! Opening map...");
                OpenFullMap();
                return;
            }
            
            // Check if touch is on full map (to close it or select coin)
            if (fullMapPanel != null && fullMapPanel.gameObject.activeSelf)
            {
                if (IsPointInRect(screenPosition, fullMapPanel))
                {
                    Log("Touch on FullMap - handling in FullMapUI");
                    // Let FullMapUI handle internal touches
                }
            }
            
            Log("Touch not on any tracked UI element");
        }
        
        /// <summary>
        /// Check if a screen point is inside a RectTransform.
        /// Works with any Canvas render mode.
        /// </summary>
        private bool IsPointInRect(Vector2 screenPoint, RectTransform rect)
        {
            if (rect == null) return false;
            
            // Use RectTransformUtility which handles all canvas modes
            bool contains = RectTransformUtility.RectangleContainsScreenPoint(
                rect, 
                screenPoint, 
                uiCamera // null for overlay, camera for world/camera space
            );
            
            Log($"IsPointInRect({rect.name}): screenPoint={screenPoint}, contains={contains}");
            
            return contains;
        }
        
        /// <summary>
        /// Get screen bounds of a RectTransform for debugging
        /// </summary>
        private Rect GetScreenBounds(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            
            // Convert to screen space
            if (uiCamera != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    corners[i] = uiCamera.WorldToScreenPoint(corners[i]);
                }
            }
            
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        
        /// <summary>
        /// Open the full map UI
        /// </summary>
        private void OpenFullMap()
        {
            Log("Opening Full Map...");
            
            // Try FullMapUI singleton
            if (FullMapUI.Exists)
            {
                Log("FullMapUI.Instance.Show()");
                FullMapUI.Instance.Show();
                return;
            }
            
            // Try to find and activate the panel directly
            if (fullMapPanel != null)
            {
                Log("Activating fullMapPanel directly");
                fullMapPanel.gameObject.SetActive(true);
                return;
            }
            
            // Try ARHUD
            if (ARHUD.Instance != null)
            {
                Log("ARHUD.Instance.OnRadarTapped()");
                ARHUD.Instance.OnRadarTapped();
                return;
            }
            
            LogError("Could not open full map - no handler found!");
        }
        
        /// <summary>
        /// Log helper with prefix
        /// </summary>
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[DirectTouch] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[DirectTouch] {message}");
        }
        
        /// <summary>
        /// Draw debug info on screen
        /// </summary>
        private void OnGUI()
        {
            if (!enableDebugVisuals) return;
            
            // Draw touch info
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 400, 30), $"DirectTouch: {lastTouchInfo}");
            GUI.Label(new Rect(10, 40, 400, 30), $"Total Touches: {totalTouchCount}");
            
            // Draw radar bounds
            if (radarPanel != null)
            {
                Rect bounds = GetScreenBounds(radarPanel);
                GUI.Label(new Rect(10, 70, 400, 30), $"Radar Bounds: {bounds}");
                
                // Draw outline of radar area (flipped Y for GUI)
                float y = Screen.height - bounds.y - bounds.height;
                GUI.Box(new Rect(bounds.x, y, bounds.width, bounds.height), "RADAR");
            }
            
            // Draw last touch position
            if (totalTouchCount > 0)
            {
                float crossSize = 20;
                float y = Screen.height - lastTouchPosition.y; // Flip Y for GUI
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(lastTouchPosition.x - crossSize/2, y - 2, crossSize, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(lastTouchPosition.x - 2, y - crossSize/2, 4, crossSize), Texture2D.whiteTexture);
            }
        }
    }
}
