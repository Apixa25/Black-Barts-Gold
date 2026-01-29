// ============================================================================
// FullMapPinchZoom.cs
// Black Bart's Gold - Pinch-to-Zoom Handler for Full Map
// Path: Assets/Scripts/UI/FullMapPinchZoom.cs
// Created: 2026-01-27 - Two-finger zoom support
// ============================================================================

using UnityEngine;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    public class FullMapPinchZoom : MonoBehaviour
    {
        private UIManager _uiManager;
        private bool _isPinching = false;
        private float _initialPinchDistance = 0f;
        private float _lastZoomTriggerDistance = 0f;
        private float _zoomCooldown = 0f;
        
        // Pinch settings - tuned for responsiveness
        private const float PINCH_ZOOM_THRESHOLD = 80f; // Pixels of pinch change per zoom level
        private const float ZOOM_COOLDOWN_TIME = 0.3f;  // Seconds between zoom triggers
        
        public void Initialize(UIManager manager)
        {
            _uiManager = manager;
            Debug.Log("[PinchZoom] Initialized!");
        }
        
        private void Update()
        {
            if (_uiManager == null)
            {
                // Try to find UIManager if not set
                _uiManager = FindFirstObjectByType<UIManager>();
                if (_uiManager == null) return;
            }
            
            if (!gameObject.activeInHierarchy) return;
            
            // Update cooldown
            if (_zoomCooldown > 0f)
            {
                _zoomCooldown -= Time.deltaTime;
            }
            
            HandleTouchPinch();
            HandleMouseScroll();
        }
        
        private void HandleTouchPinch()
        {
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                
                if (!_isPinching)
                {
                    // Start new pinch gesture
                    _isPinching = true;
                    _initialPinchDistance = currentDistance;
                    _lastZoomTriggerDistance = currentDistance;
                    Debug.Log($"[PinchZoom] START - distance: {currentDistance:F0}px");
                }
                else
                {
                    // Continue pinching - check for zoom trigger
                    float deltaFromLast = currentDistance - _lastZoomTriggerDistance;
                    
                    if (Mathf.Abs(deltaFromLast) >= PINCH_ZOOM_THRESHOLD && _zoomCooldown <= 0f)
                    {
                        int zoomDelta = deltaFromLast > 0 ? 1 : -1;
                        Debug.Log($"[PinchZoom] ZOOM {(zoomDelta > 0 ? "IN" : "OUT")} - delta: {deltaFromLast:F0}px");
                        
                        _uiManager.ChangeMapZoom(zoomDelta);
                        _lastZoomTriggerDistance = currentDistance;
                        _zoomCooldown = ZOOM_COOLDOWN_TIME;
                    }
                }
            }
            else if (_isPinching)
            {
                // Pinch ended
                float totalDelta = 0f;
                if (Input.touchCount == 0)
                {
                    Debug.Log("[PinchZoom] END");
                }
                _isPinching = false;
            }
        }
        
        private void HandleMouseScroll()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && _zoomCooldown <= 0f)
            {
                int zoomDelta = scroll > 0 ? 1 : -1;
                Debug.Log($"[PinchZoom] Mouse scroll: {zoomDelta}");
                _uiManager.ChangeMapZoom(zoomDelta);
                _zoomCooldown = ZOOM_COOLDOWN_TIME;
            }
        }
        
        private void OnDisable()
        {
            _isPinching = false;
        }
    }
}
