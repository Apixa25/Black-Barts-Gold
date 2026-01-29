// ============================================================================
// ARRaycastController.cs
// Black Bart's Gold - AR Raycast System
// Path: Assets/Scripts/AR/ARRaycastController.cs
// ============================================================================
// Handles raycasting from the screen center (crosshairs) to detect AR planes
// and game objects. Used for coin targeting and placement.
// Reference: BUILD-GUIDE.md Prompt 2.3
// ============================================================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using System;
using BlackBartsGold.Core;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages AR raycasting for targeting coins and detecting surfaces.
    /// Performs raycasts from screen center (crosshairs) each frame.
    /// </summary>
    public class ARRaycastController : MonoBehaviour
    {
        #region Singleton
        
        private static ARRaycastController _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ARRaycastController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ARRaycastController>();
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("References")]
        [SerializeField]
        [Tooltip("AR Raycast Manager component")]
        private ARRaycastManager raycastManager;
        
        [SerializeField]
        [Tooltip("Camera for raycasting (usually AR Camera)")]
        private Camera arCamera;
        
        [Header("Raycast Settings")]
        [SerializeField]
        [Tooltip("Layer mask for coin raycasts")]
        private LayerMask coinLayerMask = ~0;
        
        [SerializeField]
        [Tooltip("Maximum distance for coin detection (meters)")]
        private float maxCoinDistance = 50f;
        
        [SerializeField]
        [Tooltip("Maximum distance for plane detection (meters)")]
        private float maxPlaneDistance = 100f;
        
        [Header("Screen Position")]
        [SerializeField]
        [Tooltip("Use screen center for raycasts (true) or custom position (false)")]
        private bool useScreenCenter = true;
        
        [SerializeField]
        [Tooltip("Custom screen position if not using center (0-1 normalized)")]
        private Vector2 customScreenPosition = new Vector2(0.5f, 0.5f);
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        [SerializeField]
        private bool drawDebugRay = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Screen position for raycasts (pixels)
        /// </summary>
        public Vector2 RaycastScreenPosition
        {
            get
            {
                if (useScreenCenter)
                {
                    return new Vector2(Screen.width / 2f, Screen.height / 2f);
                }
                return new Vector2(customScreenPosition.x * Screen.width, 
                                   customScreenPosition.y * Screen.height);
            }
        }
        
        /// <summary>
        /// Currently hovered coin (if any)
        /// </summary>
        public GameObject CurrentHoveredCoin { get; private set; }
        
        /// <summary>
        /// Is currently hovering over a coin?
        /// </summary>
        public bool IsHoveringCoin => CurrentHoveredCoin != null;
        
        /// <summary>
        /// Last AR plane hit
        /// </summary>
        public ARRaycastHit? LastPlaneHit { get; private set; }
        
        /// <summary>
        /// Was a plane hit this frame?
        /// </summary>
        public bool HasPlaneHit => LastPlaneHit.HasValue;
        
        /// <summary>
        /// Position where ray hits plane (or coin)
        /// </summary>
        public Vector3 HitPosition { get; private set; }
        
        /// <summary>
        /// Normal of hit surface
        /// </summary>
        public Vector3 HitNormal { get; private set; }
        
        /// <summary>
        /// Distance to hit point
        /// </summary>
        public float HitDistance { get; private set; }
        
        /// <summary>
        /// Ray direction from camera
        /// </summary>
        public Ray CurrentRay { get; private set; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when starting to hover over a coin
        /// </summary>
        public event Action<GameObject> OnCoinHovered;
        
        /// <summary>
        /// Fired when stopping hovering over a coin
        /// </summary>
        public event Action OnCoinUnhovered;
        
        /// <summary>
        /// Fired when a coin is tapped/selected
        /// </summary>
        public event Action<GameObject> OnCoinSelected;
        
        /// <summary>
        /// Fired when a plane is hit
        /// </summary>
        public event Action<ARRaycastHit> OnPlaneHit;
        
        /// <summary>
        /// Fired when tap occurs on empty space
        /// </summary>
        public event Action<Vector2> OnEmptyTap;
        
        #endregion
        
        #region Private Fields
        
        private List<ARRaycastHit> arHits = new List<ARRaycastHit>();
        private GameObject previousHoveredCoin;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find references
            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }
            
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
            
            if (raycastManager == null)
            {
                Debug.LogWarning("[ARRaycastController] ARRaycastManager not found!");
            }
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
        
        private void Update()
        {
            // Perform raycasts
            PerformRaycasts();
            
            // Check for coin hover changes
            CheckHoverState();
            
            // Check for taps
            CheckForTap();
            
            // Debug visualization
            if (drawDebugRay && debugMode)
            {
                DrawDebugRay();
            }
        }
        
        #endregion
        
        #region Raycasting
        
        /// <summary>
        /// Perform all raycasts for this frame
        /// </summary>
        private void PerformRaycasts()
        {
            if (arCamera == null) return;
            
            Vector2 screenPos = RaycastScreenPosition;
            CurrentRay = arCamera.ScreenPointToRay(screenPos);
            
            // Reset hit info
            LastPlaneHit = null;
            HitPosition = Vector3.zero;
            HitNormal = Vector3.up;
            HitDistance = maxPlaneDistance;
            
            // 1. Raycast for coins (Physics raycast)
            RaycastForCoins();
            
            // 2. Raycast for AR planes
            RaycastForPlanes(screenPos);
        }
        
        /// <summary>
        /// Raycast to find coins
        /// </summary>
        private void RaycastForCoins()
        {
            RaycastHit hit;
            
            if (Physics.Raycast(CurrentRay, out hit, maxCoinDistance, coinLayerMask))
            {
                // Check if it's a coin
                if (hit.collider.CompareTag("Coin") || 
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Coin"))
                {
                    CurrentHoveredCoin = hit.collider.gameObject;
                    HitPosition = hit.point;
                    HitNormal = hit.normal;
                    HitDistance = hit.distance;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ARRaycast] 🪙 Coin detected: {CurrentHoveredCoin.name} at {HitDistance:F1}m");
                    }
                    return;
                }
            }
            
            // No coin hit
            CurrentHoveredCoin = null;
        }
        
        /// <summary>
        /// Raycast to find AR planes
        /// </summary>
        private void RaycastForPlanes(Vector2 screenPos)
        {
            if (raycastManager == null) return;
            
            arHits.Clear();
            
            // Raycast against all trackables (planes, feature points)
            if (raycastManager.Raycast(screenPos, arHits, TrackableType.PlaneWithinPolygon))
            {
                if (arHits.Count > 0)
                {
                    ARRaycastHit closestHit = arHits[0];
                    LastPlaneHit = closestHit;
                    
                    // Only update hit position if we didn't hit a coin
                    if (CurrentHoveredCoin == null)
                    {
                        HitPosition = closestHit.pose.position;
                        HitNormal = closestHit.pose.up;
                        HitDistance = closestHit.distance;
                    }
                    
                    OnPlaneHit?.Invoke(closestHit);
                }
            }
        }
        
        #endregion
        
        #region Hover State
        
        /// <summary>
        /// Check for hover state changes
        /// </summary>
        private void CheckHoverState()
        {
            if (CurrentHoveredCoin != previousHoveredCoin)
            {
                // Hover state changed
                if (previousHoveredCoin != null)
                {
                    // Stopped hovering
                    OnCoinUnhovered?.Invoke();
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ARRaycast] Stopped hovering: {previousHoveredCoin.name}");
                    }
                }
                
                if (CurrentHoveredCoin != null)
                {
                    // Started hovering
                    OnCoinHovered?.Invoke(CurrentHoveredCoin);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ARRaycast] Started hovering: {CurrentHoveredCoin.name}");
                    }
                }
                
                previousHoveredCoin = CurrentHoveredCoin;
            }
        }
        
        #endregion
        
        #region Tap Detection
        
        /// <summary>
        /// Check for tap input using new Input System.
        /// IMPORTANT: Skip processing if touch is over UI elements!
        /// This allows radar, buttons, etc. to receive their clicks.
        /// </summary>
        private void CheckForTap()
        {
            bool tapped = false;
            Vector2 tapPosition = Vector2.zero;
            
            // Check for touch (new Input System)
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                var touch = activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // ============================================================
                    // FIX: Check if touch is over UI BEFORE processing as AR tap!
                    // This allows RadarUI, buttons, etc. to receive clicks properly.
                    // ============================================================
                    if (IsPointerOverUI(touch.screenPosition))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[ARRaycast] Touch over UI at {touch.screenPosition} - letting UI handle it");
                        }
                        return; // Let the EventSystem handle this touch
                    }
                    
                    tapped = true;
                    tapPosition = touch.screenPosition;
                }
            }
            // Check for mouse (editor testing) - new Input System
            else
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    
                    // Also check for UI in editor
                    if (IsPointerOverUI(mousePos))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[ARRaycast] Mouse over UI - letting UI handle it");
                        }
                        return;
                    }
                    
                    tapped = true;
                    tapPosition = mousePos;
                }
            }
            
            if (!tapped) return;
            
            // Process tap
            ProcessTap(tapPosition);
        }
        
        /// <summary>
        /// Check if a screen position is over any UI element.
        /// Uses raycast against UI for reliability with new Input System.
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            // Method 1: Use EventSystem pointer check
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                // For touch input (new Input System)
                var activeTouches = Touch.activeTouches;
                if (activeTouches.Count > 0)
                {
                    // Use touch ID for pointer check
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(activeTouches[0].touchId))
                    {
                        return true;
                    }
                }
                // For mouse input
                else if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1))
                {
                    return true;
                }
            }
            
            // Method 2: Raycast against UI (fallback)
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = screenPosition;
            
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
            
            // Check if any hit is a UI element
            foreach (var result in results)
            {
                if (result.gameObject != null)
                {
                    // Check if it's on a Canvas (UI layer is usually 5, but check by component)
                    if (result.gameObject.GetComponentInParent<Canvas>() != null)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Process a tap at screen position
        /// </summary>
        private void ProcessTap(Vector2 screenPosition)
        {
            // Check if tapping near screen center (on crosshairs target)
            Vector2 center = RaycastScreenPosition;
            float distFromCenter = Vector2.Distance(screenPosition, center);
            float threshold = Screen.height * 0.1f; // 10% of screen height
            
            if (distFromCenter <= threshold)
            {
                // Tap on crosshairs area
                if (CurrentHoveredCoin != null)
                {
                    // Coin selected!
                    if (debugMode)
                    {
                        Debug.Log($"[ARRaycast] 🎯 Coin selected: {CurrentHoveredCoin.name}");
                    }
                    OnCoinSelected?.Invoke(CurrentHoveredCoin);
                }
                else
                {
                    // Tap on empty space
                    OnEmptyTap?.Invoke(screenPosition);
                }
            }
            else
            {
                // Tap away from center - could be UI interaction
                OnEmptyTap?.Invoke(screenPosition);
            }
        }
        
        /// <summary>
        /// Manually trigger coin selection (for UI buttons, etc.)
        /// </summary>
        public void SelectCurrentCoin()
        {
            if (CurrentHoveredCoin != null)
            {
                OnCoinSelected?.Invoke(CurrentHoveredCoin);
            }
        }
        
        #endregion
        
        #region Public Raycast Methods
        
        /// <summary>
        /// Raycast from a specific screen position
        /// </summary>
        public bool RaycastFromScreen(Vector2 screenPosition, out RaycastHit hit)
        {
            if (arCamera == null)
            {
                hit = default;
                return false;
            }
            
            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, maxCoinDistance, coinLayerMask);
        }
        
        /// <summary>
        /// Raycast for AR planes from screen position
        /// </summary>
        public bool RaycastForPlane(Vector2 screenPosition, out ARRaycastHit hit)
        {
            hit = default;
            
            if (raycastManager == null) return false;
            
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                if (hits.Count > 0)
                {
                    hit = hits[0];
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all coins within a screen radius
        /// </summary>
        public List<GameObject> GetCoinsInScreenRadius(Vector2 screenCenter, float pixelRadius)
        {
            List<GameObject> coins = new List<GameObject>();
            
            if (arCamera == null) return coins;
            
            // Find all coins
            GameObject[] allCoins = GameObject.FindGameObjectsWithTag("Coin");
            
            foreach (var coin in allCoins)
            {
                Vector3 screenPos = arCamera.WorldToScreenPoint(coin.transform.position);
                
                // Check if in front of camera
                if (screenPos.z < 0) continue;
                
                float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
                if (dist <= pixelRadius)
                {
                    coins.Add(coin);
                }
            }
            
            return coins;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Draw debug ray in scene view
        /// </summary>
        private void DrawDebugRay()
        {
            if (!Application.isPlaying) return;
            
            Color rayColor = CurrentHoveredCoin != null ? Color.green : 
                            HasPlaneHit ? Color.yellow : Color.red;
            
            Debug.DrawRay(CurrentRay.origin, CurrentRay.direction * HitDistance, rayColor);
            
            if (HasPlaneHit || CurrentHoveredCoin != null)
            {
                // Draw hit marker
                Debug.DrawLine(HitPosition + Vector3.up * 0.1f, 
                              HitPosition - Vector3.up * 0.1f, Color.magenta);
                Debug.DrawLine(HitPosition + Vector3.left * 0.1f, 
                              HitPosition - Vector3.left * 0.1f, Color.magenta);
                Debug.DrawLine(HitPosition + Vector3.forward * 0.1f, 
                              HitPosition - Vector3.forward * 0.1f, Color.magenta);
            }
        }
        
        /// <summary>
        /// Debug: Print raycast info
        /// </summary>
        [ContextMenu("Debug: Print Raycast Info")]
        public void DebugPrintInfo()
        {
            Debug.Log("=== AR Raycast Info ===");
            Debug.Log($"Screen Position: {RaycastScreenPosition}");
            Debug.Log($"Hovering Coin: {(CurrentHoveredCoin != null ? CurrentHoveredCoin.name : "None")}");
            Debug.Log($"Has Plane Hit: {HasPlaneHit}");
            Debug.Log($"Hit Position: {HitPosition}");
            Debug.Log($"Hit Distance: {HitDistance:F2}m");
            Debug.Log("=======================");
        }
        
        #endregion
    }
}
