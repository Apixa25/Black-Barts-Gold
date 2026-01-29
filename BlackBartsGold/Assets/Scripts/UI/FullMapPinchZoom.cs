// ============================================================================
// FullMapPinchZoom.cs
// Black Bart's Gold - Pinch-to-Zoom Handler for Full Map
// Path: Assets/Scripts/UI/FullMapPinchZoom.cs
// Created: 2026-01-27 - Two-finger zoom support
// ============================================================================
// Handles pinch gestures to zoom the full map in and out.
// Also supports mouse scroll wheel for editor testing.
// ============================================================================

using UnityEngine;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Handles pinch-to-zoom gestures for the full map view.
    /// Attach to the full map panel.
    /// </summary>
    public class FullMapPinchZoom : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float pinchThreshold = 30f; // Pixels of movement before zoom
        [SerializeField] private float scrollZoomSpeed = 0.1f;
        
        private UIManager _uiManager;
        private bool _isPinching = false;
        private float _lastPinchDistance = 0f;
        private float _accumulatedPinch = 0f;
        
        public void Initialize(UIManager manager)
        {
            _uiManager = manager;
            Debug.Log("[PinchZoom] Initialized for full map");
        }
        
        private void Update()
        {
            if (_uiManager == null) return;
            if (!gameObject.activeInHierarchy) return;
            
            // Handle touch pinch zoom
            HandleTouchPinch();
            
            // Handle mouse scroll wheel (for editor testing)
            HandleMouseScroll();
        }
        
        private void HandleTouchPinch()
        {
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                // Calculate current distance between touches
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                
                if (!_isPinching)
                {
                    // Start pinching
                    _isPinching = true;
                    _lastPinchDistance = currentDistance;
                    _accumulatedPinch = 0f;
                    Debug.Log("[PinchZoom] Pinch started");
                }
                else
                {
                    // Continue pinching - accumulate distance change
                    float delta = currentDistance - _lastPinchDistance;
                    _accumulatedPinch += delta;
                    _lastPinchDistance = currentDistance;
                    
                    // Check if we've accumulated enough to trigger zoom
                    if (Mathf.Abs(_accumulatedPinch) > pinchThreshold)
                    {
                        int zoomDelta = _accumulatedPinch > 0 ? 1 : -1;
                        _uiManager.ChangeMapZoom(zoomDelta);
                        _accumulatedPinch = 0f; // Reset accumulator
                        Debug.Log($"[PinchZoom] Zoom triggered: {zoomDelta}");
                    }
                }
            }
            else
            {
                // No longer pinching
                if (_isPinching)
                {
                    _isPinching = false;
                    Debug.Log("[PinchZoom] Pinch ended");
                }
            }
        }
        
        private void HandleMouseScroll()
        {
            // Only in editor or when using mouse
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int zoomDelta = scroll > 0 ? 1 : -1;
                _uiManager.ChangeMapZoom(zoomDelta);
                Debug.Log($"[PinchZoom] Mouse scroll zoom: {zoomDelta}");
            }
        }
        
        private void OnDisable()
        {
            _isPinching = false;
            _accumulatedPinch = 0f;
        }
    }
}
