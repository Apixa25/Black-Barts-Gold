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
        private float _lastPinchDistance = 0f;
        private float _zoomCooldown = 0f;
        
        // MUCH more responsive settings!
        private const float PINCH_ZOOM_THRESHOLD = 35f; // Reduced from 80 - pixels needed per zoom
        private const float ZOOM_COOLDOWN_TIME = 0.12f; // Reduced from 0.3 - faster repeat zooms
        
        public void Initialize(UIManager manager)
        {
            _uiManager = manager;
            Debug.Log("[PinchZoom] Initialized - threshold=35px, cooldown=0.12s");
        }
        
        private void Update()
        {
            if (_uiManager == null)
            {
                _uiManager = FindFirstObjectByType<UIManager>();
                if (_uiManager == null) return;
            }
            
            if (!gameObject.activeInHierarchy) return;
            
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
                    // Start new pinch
                    _isPinching = true;
                    _lastPinchDistance = currentDistance;
                    Debug.Log($"[PinchZoom] START dist={currentDistance:F0}");
                }
                else if (_zoomCooldown <= 0f)
                {
                    // Continue pinching - accumulate delta
                    float delta = currentDistance - _lastPinchDistance;
                    
                    if (Mathf.Abs(delta) >= PINCH_ZOOM_THRESHOLD)
                    {
                        int zoomDelta = delta > 0 ? 1 : -1;
                        Debug.Log($"[PinchZoom] {(zoomDelta > 0 ? "IN" : "OUT")} d={delta:F0}");
                        
                        _uiManager.ChangeMapZoom(zoomDelta);
                        _lastPinchDistance = currentDistance; // Reset for next zoom
                        _zoomCooldown = ZOOM_COOLDOWN_TIME;
                    }
                }
            }
            else if (_isPinching)
            {
                Debug.Log("[PinchZoom] END");
                _isPinching = false;
            }
        }
        
        private void HandleMouseScroll()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && _zoomCooldown <= 0f)
            {
                int zoomDelta = scroll > 0 ? 1 : -1;
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
