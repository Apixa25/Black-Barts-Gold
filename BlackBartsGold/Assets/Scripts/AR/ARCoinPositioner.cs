// ============================================================================
// ARCoinPositioner.cs
// Black Bart's Gold - GPS to AR Position Converter (Pokemon GO Pattern)
// Path: Assets/Scripts/AR/ARCoinPositioner.cs
// ============================================================================
// REFACTORED for Pokemon GO materialization pattern.
//
// Key insight: We don't try to position coins at their GPS distance.
// Instead:
//   1. Track GPS distance/bearing for navigation (Direction Indicator uses this)
//   2. When close enough, coin materializes at comfortable viewing distance
//   3. GPS position only matters for determining collection eligibility
//
// The "compass-aligned" pattern is kept for accurate bearing calculation,
// but position is handled by ARCoinRenderer's materialization system.
// ============================================================================

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// GPS-to-AR position converter with Pokemon GO-style materialization support.
    /// 
    /// This component tracks the GPS location of a coin and provides:
    /// - GPS distance to coin (for materialization trigger)
    /// - GPS bearing to coin (for direction indicator)
    /// - AR position calculation (when coin materializes)
    /// </summary>
    public class ARCoinPositioner : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("GPS Data")]
        [SerializeField]
        [Tooltip("Target latitude")]
        private double latitude;
        
        [SerializeField]
        [Tooltip("Target longitude")]
        private double longitude;
        
        [Header("Position Settings")]
        [SerializeField]
        [Tooltip("Height above ground when positioned")]
        private float heightAboveGround = 1.2f;
        
        [SerializeField]
        [Tooltip("Position smoothing time")]
        private float positionSmoothTime = 0.3f;
        
        [Header("Update Settings")]
        [SerializeField]
        [Tooltip("How often to update GPS calculations (seconds)")]
        private float updateInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>Target GPS latitude</summary>
        public double Latitude
        {
            get => latitude;
            set => latitude = value;
        }
        
        /// <summary>Target GPS longitude</summary>
        public double Longitude
        {
            get => longitude;
            set => longitude = value;
        }
        
        /// <summary>GPS distance to coin in meters (the REAL distance)</summary>
        public float GPSDistance { get; private set; } = float.MaxValue;
        
        /// <summary>GPS bearing to coin (degrees, 0=North, 90=East)</summary>
        public float GPSBearing { get; private set; } = 0f;
        
        /// <summary>Is position currently locked (coin materialized and stable)?</summary>
        public bool IsPositionLocked { get; private set; } = false;
        
        /// <summary>Calculated AR world position (only valid after UpdateGPSPosition called)</summary>
        public Vector3 ARWorldPosition { get; private set; } = Vector3.zero;
        
        #endregion
        
        #region Static Compass Alignment
        
        // Initial compass heading (captured at AR session start)
        // Shared across all positioners for consistency
        private static float _initialCompassHeading = 0f;
        private static bool _hasInitialHeading = false;
        
        /// <summary>
        /// Capture initial compass heading. Call once when AR session starts.
        /// </summary>
        public static void CaptureInitialCompassHeading()
        {
            if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
            {
                _initialCompassHeading = Input.compass.trueHeading;
                _hasInitialHeading = true;
                Debug.Log($"[ARCoinPositioner] Captured initial compass heading: {_initialCompassHeading:F1}°");
            }
            else
            {
                _initialCompassHeading = 0f;
                _hasInitialHeading = true;
                Debug.LogWarning("[ARCoinPositioner] Compass not available, using 0° as initial heading");
            }
        }
        
        /// <summary>
        /// Reset compass heading (call when AR session restarts)
        /// </summary>
        public static void ResetCompassHeading()
        {
            _hasInitialHeading = false;
            _initialCompassHeading = 0f;
        }
        
        /// <summary>Has initial compass heading been captured?</summary>
        public static bool HasInitialHeading => _hasInitialHeading;
        
        #endregion
        
        #region Private Fields
        
        private float lastUpdateTime = 0f;
        private Vector3 targetPosition;
        private Vector3 positionVelocity;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            targetPosition = transform.position;
        }
        
        private void Start()
        {
            Debug.Log($"[ARCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Start() on '{gameObject.name}'");
            Debug.Log($"[ARCoinPositioner]   GPS Target: ({latitude:F6}, {longitude:F6})");
            Debug.Log($"[ARCoinPositioner]   HasInitialHeading: {_hasInitialHeading}, InitialHeading: {_initialCompassHeading:F0}°");
            
            // Log AR tracking state
            var arState = UnityEngine.XR.ARFoundation.ARSession.state;
            Debug.Log($"[ARCoinPositioner]   AR Session State: {arState}");
            
            // Log XR tracking devices (are we getting 6DOF?)
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
            Debug.Log($"[ARCoinPositioner]   XR CenterEye Devices: {devices.Count} (need >0 for 6DOF tracking)");
            
            // Enable compass
            Input.compass.enabled = true;
            
            // Capture initial heading if not done
            if (!_hasInitialHeading)
            {
                StartCoroutine(CaptureCompassHeadingCoroutine());
            }
            
            // Initial GPS calculation
            UpdateGPSData();
            
            Debug.Log($"[ARCoinPositioner]   Initial GPSDistance: {GPSDistance:F1}m, GPSBearing: {GPSBearing:F0}°");
        }
        
        private void Update()
        {
            // Throttle updates
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;
            
            // Always update GPS distance and bearing (Direction Indicator needs this)
            UpdateGPSData();
            
            // If position is locked, only smoothly maintain position
            // (ARCoinRenderer handles the materialized position)
            if (IsPositionLocked)
            {
                // Gentle smooth damp to target
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref positionVelocity,
                    positionSmoothTime
                );
            }
        }
        
        #endregion
        
        #region GPS Calculations
        
        /// <summary>
        /// Update GPS distance and bearing to coin.
        /// This is called regularly to keep Direction Indicator accurate.
        /// </summary>
        private void UpdateGPSData()
        {
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null)
            {
                if (debugMode && Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("[ARCoinPositioner] No player location available");
                }
                return;
            }
            
            // Create coin location
            LocationData coinLocation = new LocationData(latitude, longitude);
            
            // Calculate GPS distance (the REAL distance, not AR distance)
            GPSDistance = (float)playerLocation.DistanceTo(coinLocation);
            
            // Calculate GPS bearing (for direction indicator)
            GPSBearing = (float)playerLocation.BearingTo(coinLocation);
            
            // Also calculate the AR world position (in case renderer needs it)
            CalculateARPosition(playerLocation);
        }
        
        /// <summary>
        /// Calculate AR world position from GPS.
        /// Uses compass-aligned positioning (Pokemon GO pattern).
        /// </summary>
        private void CalculateARPosition(LocationData playerLocation)
        {
            if (!_hasInitialHeading) return;
            
            // Adjust bearing for initial compass heading
            float adjustedBearing = GPSBearing - _initialCompassHeading;
            
            // Convert to radians
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate AR position
            // In Unity: +X is right (east), +Z is forward (north from initial heading)
            float x = GPSDistance * Mathf.Sin(bearingRad);
            float z = GPSDistance * Mathf.Cos(bearingRad);
            float y = heightAboveGround;
            
            ARWorldPosition = new Vector3(x, y, z);
        }
        
        /// <summary>
        /// Get current player location from GPSManager
        /// </summary>
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                return GPSManager.Instance.CurrentLocation;
            }
            return null;
        }
        
        #endregion
        
        #region Position Locking
        
        /// <summary>
        /// Lock position - coin stays where it is.
        /// Called by ARCoinRenderer when coin is materialized and close.
        /// </summary>
        public void LockPosition()
        {
            if (IsPositionLocked) return;
            
            IsPositionLocked = true;
            targetPosition = transform.position;
            positionVelocity = Vector3.zero;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] Position LOCKED at {transform.position}");
            }
        }
        
        /// <summary>
        /// Unlock position - allows position updates again.
        /// </summary>
        public void UnlockPosition()
        {
            if (!IsPositionLocked) return;
            
            IsPositionLocked = false;
            
            if (debugMode)
            {
                Debug.Log("[ARCoinPositioner] Position UNLOCKED");
            }
        }
        
        /// <summary>
        /// Set position to a specific point (used during materialization).
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            targetPosition = position;
            transform.position = position;
        }
        
        /// <summary>
        /// Smoothly move to a target position.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            targetPosition = position;
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize with coin data
        /// </summary>
        public void Initialize(Coin coinData)
        {
            if (coinData == null) return;
            
            latitude = coinData.latitude;
            longitude = coinData.longitude;
            heightAboveGround = coinData.heightOffset > 0 ? coinData.heightOffset : 1.2f;
            
            // Reset state
            IsPositionLocked = false;
            GPSDistance = float.MaxValue;
            GPSBearing = 0f;
            
            // Ensure compass heading is captured
            if (!_hasInitialHeading)
            {
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    CaptureInitialCompassHeading();
                }
                else
                {
                    StartCoroutine(CaptureCompassHeadingCoroutine());
                }
            }
            
            // Calculate initial GPS data
            UpdateGPSData();
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] Initialized: ({latitude:F6}, {longitude:F6}), GPS dist: {GPSDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Initialize with raw coordinates
        /// </summary>
        public void Initialize(double lat, double lng, float height = 1.2f)
        {
            latitude = lat;
            longitude = lng;
            heightAboveGround = height;
            
            IsPositionLocked = false;
            UpdateGPSData();
        }
        
        #endregion
        
        #region Compass Capture Coroutine
        
        private IEnumerator CaptureCompassHeadingCoroutine()
        {
            // Wait for compass to stabilize
            yield return new WaitForSeconds(0.5f);
            
            for (int i = 0; i < 10; i++)
            {
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    CaptureInitialCompassHeading();
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            // Fallback
            Debug.LogWarning("[ARCoinPositioner] Could not get compass reading, using 0°");
            _initialCompassHeading = 0f;
            _hasInitialHeading = true;
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== ARCoinPositioner Status ===");
            Debug.Log($"GPS Location: ({latitude:F6}, {longitude:F6})");
            Debug.Log($"GPS Distance: {GPSDistance:F1}m");
            Debug.Log($"GPS Bearing: {GPSBearing:F0}°");
            Debug.Log($"Initial Compass: {_initialCompassHeading:F0}° (captured: {_hasInitialHeading})");
            Debug.Log($"Position Locked: {IsPositionLocked}");
            Debug.Log($"AR World Position: {ARWorldPosition}");
            Debug.Log($"Transform Position: {transform.position}");
            Debug.Log("===============================");
        }
        
        [ContextMenu("Debug: Recapture Compass")]
        public void DebugRecaptureCompass()
        {
            ResetCompassHeading();
            CaptureInitialCompassHeading();
        }
        
        #endregion
    }
}
