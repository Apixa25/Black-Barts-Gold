// ============================================================================
// DirectTouchHandler.cs
// Black Bart's Gold - Direct Touch Detection (Pokemon GO Style)
// Path: Assets/Scripts/UI/DirectTouchHandler.cs
// Created: 2026-01-27 - Bulletproof touch handling
// Updated: 2026-01-29 - Fixed for new Input System
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Direct touch handler that bypasses Unity's EventSystem.
    /// Uses the new Input System's EnhancedTouch API.
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
        private Vector2 lastTouchPosition;
        
        private void Awake()
        {
            Log("========================================");
            Log("DirectTouchHandler AWAKE");
            Log($"GameObject: {gameObject.name}");
            Log("========================================");
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
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
            
            // Handle touch input (new Input System)
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                var touch = activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    HandleTouchBegan(touch.screenPosition);
                }
            }
            // Handle mouse for editor testing (new Input System)
            else
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    HandleTouchBegan(mouse.position.ReadValue());
                }
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
                }
            }
            
            Log("Touch not on any tracked UI element");
        }
        
        private bool IsPointInRect(Vector2 screenPoint, RectTransform rect)
        {
            if (rect == null) return false;
            
            bool contains = RectTransformUtility.RectangleContainsScreenPoint(
                rect, 
                screenPoint, 
                uiCamera
            );
            
            Log($"IsPointInRect({rect.name}): screenPoint={screenPoint}, contains={contains}");
            
            return contains;
        }
        
        private Rect GetScreenBounds(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            
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
        
        private void OpenFullMap()
        {
            Log("Opening Full Map...");
            
            if (FullMapUI.Exists)
            {
                Log("FullMapUI.Instance.Show()");
                FullMapUI.Instance.Show();
                return;
            }
            
            if (fullMapPanel != null)
            {
                Log("Activating fullMapPanel directly");
                fullMapPanel.gameObject.SetActive(true);
                return;
            }
            
            if (ARHUD.Instance != null)
            {
                Log("ARHUD.Instance.OnRadarTapped()");
                ARHUD.Instance.OnRadarTapped();
                return;
            }
            
            LogError("Could not open full map - no handler found!");
        }
        
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
        
        private void OnGUI()
        {
            if (!enableDebugVisuals) return;
            
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 400, 30), $"DirectTouch: {lastTouchInfo}");
            GUI.Label(new Rect(10, 40, 400, 30), $"Total Touches: {totalTouchCount}");
            
            if (radarPanel != null)
            {
                Rect bounds = GetScreenBounds(radarPanel);
                GUI.Label(new Rect(10, 70, 400, 30), $"Radar Bounds: {bounds}");
                
                float y = Screen.height - bounds.y - bounds.height;
                GUI.Box(new Rect(bounds.x, y, bounds.width, bounds.height), "RADAR");
            }
            
            if (totalTouchCount > 0)
            {
                float crossSize = 20;
                float y = Screen.height - lastTouchPosition.y;
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(lastTouchPosition.x - crossSize/2, y - 2, crossSize, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(lastTouchPosition.x - 2, y - crossSize/2, 4, crossSize), Texture2D.whiteTexture);
            }
        }
    }
}
