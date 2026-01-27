// ============================================================================
// ARCoinPlacer.cs
// Black Bart's Gold - Proper AR Coin Placement System
// Path: Assets/Scripts/AR/ARCoinPlacer.cs
// ============================================================================
// Places coins on detected AR planes using anchors (Pokémon GO style).
// This is the CORRECT way to do AR - GPS for navigation, anchors for display.
// ============================================================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages proper AR coin placement using plane detection and anchors.
    /// 
    /// ARCHITECTURE (Pokémon GO Style):
    /// 1. Mini-map shows coin locations (GPS-based navigation)
    /// 2. When player is close enough, coin can be "placed" in AR
    /// 3. AR plane detection finds a real surface
    /// 4. Coin is anchored to that surface - stays stable!
    /// </summary>
    public class ARCoinPlacer : MonoBehaviour
    {
        #region Singleton
        
        private static ARCoinPlacer _instance;
        public static ARCoinPlacer Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("AR References")]
        [SerializeField]
        [Tooltip("AR Raycast Manager for finding planes")]
        private ARRaycastManager raycastManager;
        
        [SerializeField]
        [Tooltip("AR Anchor Manager for creating stable anchors")]
        private ARAnchorManager anchorManager;
        
        [SerializeField]
        [Tooltip("AR Plane Manager for plane detection")]
        private ARPlaneManager planeManager;
        
        [Header("Placement Settings")]
        [SerializeField]
        [Tooltip("Distance from camera to place coins (meters)")]
        private float placementDistance = 3f;
        
        [SerializeField]
        [Tooltip("GPS distance threshold to allow coin placement (meters)")]
        private float gpsProximityThreshold = 20f;
        
        [SerializeField]
        [Tooltip("Minimum plane area required for placement (sq meters)")]
        private float minPlaneArea = 0.5f;
        
        [Header("Coin Prefab")]
        [SerializeField]
        [Tooltip("Prefab to spawn for coins (uses default if null)")]
        private GameObject coinPrefab;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Are AR planes currently detected?
        /// </summary>
        public bool HasDetectedPlanes => planeManager != null && planeManager.trackables.count > 0;
        
        /// <summary>
        /// Number of detected planes
        /// </summary>
        public int DetectedPlaneCount => planeManager != null ? planeManager.trackables.count : 0;
        
        /// <summary>
        /// Coins currently placed in AR (anchored)
        /// </summary>
        public List<PlacedCoin> PlacedCoins => _placedCoins;
        
        #endregion
        
        #region Private Fields
        
        private List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();
        private List<PlacedCoin> _placedCoins = new List<PlacedCoin>();
        private Camera _arCamera;
        
        #endregion
        
        #region Data Classes
        
        /// <summary>
        /// Represents a coin that has been placed in AR with an anchor
        /// </summary>
        public class PlacedCoin
        {
            public string CoinId;
            public Coin CoinData;
            public GameObject CoinObject;
            public ARAnchor Anchor;
            public float PlacedTime;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find AR components if not assigned
            if (raycastManager == null)
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (anchorManager == null)
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            
            _arCamera = Camera.main;
            if (_arCamera == null)
                _arCamera = FindFirstObjectByType<Camera>();
        }
        
        private void Start()
        {
            Log("ARCoinPlacer initialized - Pokémon GO style placement ready!");
            Log($"  - Raycast Manager: {(raycastManager != null ? "OK" : "MISSING")}");
            Log($"  - Anchor Manager: {(anchorManager != null ? "OK" : "MISSING")}");
            Log($"  - Plane Manager: {(planeManager != null ? "OK" : "MISSING")}");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Check if a coin can be placed in AR (player is close enough via GPS)
        /// </summary>
        public bool CanPlaceCoin(Coin coinData)
        {
            if (coinData == null) return false;
            
            // Check GPS proximity
            var gpsManager = GPSManager.Instance;
            if (gpsManager == null || gpsManager.CurrentLocation == null)
            {
                Log("Cannot place coin - GPS not available");
                return false;
            }
            
            var playerLoc = gpsManager.CurrentLocation;
            var coinLoc = new LocationData(coinData.latitude, coinData.longitude);
            float distance = (float)playerLoc.DistanceTo(coinLoc);
            
            if (distance > gpsProximityThreshold)
            {
                Log($"Cannot place coin - too far ({distance:F0}m > {gpsProximityThreshold}m)");
                return false;
            }
            
            // Check if planes are detected
            if (!HasDetectedPlanes)
            {
                Log("Cannot place coin - no AR planes detected yet");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Place a coin in AR on a detected plane.
        /// Falls back to camera-relative placement if no plane is hit.
        /// </summary>
        public PlacedCoin PlaceCoinInAR(Coin coinData)
        {
            // Skip strict CanPlaceCoin check - we want to always allow placing
            // Just check basics
            if (coinData == null)
            {
                Log("Cannot place coin - coinData is null");
                return null;
            }
            
            // Check if already placed
            if (IsCoinPlaced(coinData.id))
            {
                Log($"Coin {coinData.id} already placed in AR");
                return GetPlacedCoin(coinData.id);
            }
            
            // Find a suitable plane via raycast from screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            
            if (TryGetPlacementPose(screenCenter, out Pose placementPose))
            {
                Log($"Raycast hit plane at {placementPose.position}");
                return PlaceCoinAtPose(coinData, placementPose);
            }
            
            // Fallback 1: Try to place on largest horizontal plane
            ARPlane bestPlane = GetBestPlaneForPlacement();
            if (bestPlane != null && _arCamera != null)
            {
                // Place at plane center, adjusted toward camera
                Vector3 dirToCamera = (_arCamera.transform.position - bestPlane.center).normalized;
                dirToCamera.y = 0;
                Vector3 placementPos = bestPlane.center + dirToCamera * placementDistance * 0.5f;
                placementPos.y = bestPlane.center.y;
                
                Log($"Using best plane fallback at {placementPos}");
                Pose planePose = new Pose(placementPos, Quaternion.identity);
                return PlaceCoinAtPose(coinData, planePose);
            }
            
            // ================================================================
            // FALLBACK 2: Place at FIXED WORLD POSITION for testing
            // This will tell us if world-space objects stay in place!
            // ================================================================
            
            // Use a FIXED ABSOLUTE world position
            // This should NOT follow the camera at all
            Vector3 fixedWorldPos = new Vector3(0f, 0.5f, 1.5f); // 1.5m ahead, 0.5m high
            
            Log($"Using FIXED WORLD POSITION fallback - placing at {fixedWorldPos}");
            Pose fallbackPose = new Pose(fixedWorldPos, Quaternion.identity);
            return PlaceCoinAtPose(coinData, fallbackPose);
        }
        
        /// <summary>
        /// Place coin at a specific pose (position + rotation)
        /// Uses EXISTING coin from CoinManager instead of creating new one
        /// </summary>
        public PlacedCoin PlaceCoinAtPose(Coin coinData, Pose pose)
        {
            Debug.Log($"[ARCoinPlacer] ═══════════════════════════════════════════════════");
            Debug.Log($"[ARCoinPlacer] 🪙 REVEALING COIN: {coinData.id}");
            Debug.Log($"[ARCoinPlacer] 📍 Target pose position: {pose.position}");
            
            // Find the EXISTING coin controller for this coin
            CoinController existingCoin = null;
            if (CoinManager.Instance != null)
            {
                foreach (var controller in CoinManager.Instance.ActiveCoins)
                {
                    if (controller != null && controller.CoinId == coinData.id)
                    {
                        existingCoin = controller;
                        Debug.Log($"[ARCoinPlacer] ✅ Found existing coin: {controller.name}");
                        break;
                    }
                }
            }
            
            GameObject coinObject;
            
            if (existingCoin != null)
            {
                coinObject = existingCoin.gameObject;
            }
            else
            {
                Debug.LogWarning($"[ARCoinPlacer] ⚠️ Existing coin not found! Creating new visual...");
                coinObject = CreateCoinVisual(coinData);
            }
            
            // ================================================================
            // SIMPLIFIED WORLD-SPACE PLACEMENT (NO ANCHORS FOR NOW)
            // ================================================================
            // The anchor system may not be working correctly. Let's just place
            // the coin at a fixed world position to verify it stays in place.
            // ================================================================
            
            Camera cam = _arCamera ?? Camera.main ?? FindFirstObjectByType<Camera>();
            Vector3 worldPosition;
            
            if (pose.position != Vector3.zero && pose.position.magnitude < 100f)
            {
                // Use the raycast pose position directly in world space
                worldPosition = pose.position;
                Debug.Log($"[ARCoinPlacer] 📍 Using raycast pose: {worldPosition}");
            }
            else if (cam != null)
            {
                // Fallback: Place 2 meters in front of camera
                Vector3 camFwd = cam.transform.forward;
                camFwd.y = 0;
                if (camFwd.magnitude < 0.1f) camFwd = Vector3.forward;
                camFwd.Normalize();
                
                worldPosition = cam.transform.position + camFwd * 2f;
                worldPosition.y = cam.transform.position.y - 0.3f;
                Debug.Log($"[ARCoinPlacer] 📍 Using camera-relative fallback: {worldPosition}");
            }
            else
            {
                worldPosition = new Vector3(0, 1, 2); // Absolute fallback
                Debug.LogWarning("[ARCoinPlacer] ⚠️ No camera! Using absolute fallback position.");
            }
            
            // ================================================================
            // CRITICAL: Unparent from any previous parent and place in WORLD SPACE
            // This ensures the coin doesn't follow the camera!
            // ================================================================
            coinObject.transform.SetParent(null); // WORLD SPACE - no parent!
            coinObject.transform.position = worldPosition;
            coinObject.transform.rotation = Quaternion.identity;
            coinObject.transform.localScale = Vector3.one;
            
            Debug.Log($"[ARCoinPlacer] 🌍 Coin placed in WORLD SPACE at: {coinObject.transform.position}");
            Debug.Log($"[ARCoinPlacer]    Parent is now: {(coinObject.transform.parent == null ? "NULL (world space)" : coinObject.transform.parent.name)}");
            
            // ================================================================
            // Make the coin VISIBLE
            // ================================================================
            if (existingCoin != null)
            {
                existingCoin.SetDisplayMode(CoinController.CoinDisplayMode.Anchored);
            }
            
            // Track placed coin (no anchor for now)
            PlacedCoin placed = new PlacedCoin
            {
                CoinId = coinData.id,
                CoinData = coinData,
                CoinObject = coinObject,
                Anchor = null, // Not using anchors for now
                PlacedTime = Time.time
            };
            
            _placedCoins.Add(placed);
            
            Debug.Log($"[ARCoinPlacer] ✅ COIN REVEAL COMPLETE at world position: {coinObject.transform.position}");
            Debug.Log($"[ARCoinPlacer] ═══════════════════════════════════════════════════");
            
            return placed;
        }
        
        /// <summary>
        /// Remove a placed coin
        /// </summary>
        public void RemovePlacedCoin(string coinId)
        {
            PlacedCoin placed = GetPlacedCoin(coinId);
            if (placed == null) return;
            
            if (placed.CoinObject != null)
                Destroy(placed.CoinObject);
            
            if (placed.Anchor != null)
                Destroy(placed.Anchor.gameObject);
            
            _placedCoins.Remove(placed);
            
            Log($"Removed placed coin: {coinId}");
        }
        
        /// <summary>
        /// Clear all placed coins
        /// </summary>
        public void ClearAllPlacedCoins()
        {
            foreach (var placed in _placedCoins)
            {
                if (placed.CoinObject != null)
                    Destroy(placed.CoinObject);
                if (placed.Anchor != null)
                    Destroy(placed.Anchor.gameObject);
            }
            _placedCoins.Clear();
            Log("Cleared all placed coins");
        }
        
        /// <summary>
        /// Check if a coin is already placed
        /// </summary>
        public bool IsCoinPlaced(string coinId)
        {
            return _placedCoins.Exists(p => p.CoinId == coinId);
        }
        
        /// <summary>
        /// Get a placed coin by ID
        /// </summary>
        public PlacedCoin GetPlacedCoin(string coinId)
        {
            return _placedCoins.Find(p => p.CoinId == coinId);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Try to get a placement pose via AR raycast
        /// </summary>
        private bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
        {
            pose = default;
            
            if (raycastManager == null) return false;
            
            _raycastHits.Clear();
            
            if (raycastManager.Raycast(screenPosition, _raycastHits, TrackableType.PlaneWithinPolygon))
            {
                if (_raycastHits.Count > 0)
                {
                    pose = _raycastHits[0].pose;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the best plane for coin placement
        /// </summary>
        private ARPlane GetBestPlaneForPlacement()
        {
            if (planeManager == null) return null;
            
            ARPlane best = null;
            float maxArea = minPlaneArea;
            
            foreach (var plane in planeManager.trackables)
            {
                // Prefer horizontal planes (ground, tables)
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                    continue;
                
                float area = plane.size.x * plane.size.y;
                if (area > maxArea)
                {
                    maxArea = area;
                    best = plane;
                }
            }
            
            return best;
        }
        
        /// <summary>
        /// Create an AR anchor at the given pose
        /// </summary>
        private ARAnchor CreateAnchorAtPose(Pose pose)
        {
            if (anchorManager == null)
            {
                Log("Warning: No ARAnchorManager - cannot create anchor");
                return null;
            }
            
            // Create anchor GameObject
            GameObject anchorObj = new GameObject("CoinAnchor");
            anchorObj.transform.position = pose.position;
            anchorObj.transform.rotation = pose.rotation;
            
            // Add ARAnchor component
            ARAnchor anchor = anchorObj.AddComponent<ARAnchor>();
            
            return anchor;
        }
        
        /// <summary>
        /// Create the visual representation of a coin
        /// </summary>
        private GameObject CreateCoinVisual(Coin coinData)
        {
            GameObject coinObj;
            
            if (coinPrefab != null)
            {
                coinObj = Instantiate(coinPrefab);
            }
            else
            {
                // Create default coin visual - big gold sphere
                coinObj = new GameObject($"Coin_{coinData.id}");
                
                // Create sphere visual
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "CoinVisual";
                visual.transform.SetParent(coinObj.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // 50cm sphere
                
                // Gold material
                MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Unlit/Color");
                    if (shader == null) shader = Shader.Find("Standard");
                    
                    renderer.material = new Material(shader);
                    renderer.material.color = new Color(1f, 0.84f, 0f); // Gold
                }
                
                // Remove sphere's collider, add proper one to parent
                Collider col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                SphereCollider coinCol = coinObj.AddComponent<SphereCollider>();
                coinCol.radius = 0.3f;
            }
            
            coinObj.tag = "Coin";
            
            // Add CoinController if not present
            CoinController controller = coinObj.GetComponent<CoinController>();
            if (controller == null)
            {
                controller = coinObj.AddComponent<CoinController>();
            }
            
            // Initialize with data
            controller.Initialize(coinData, false, true);
            
            return coinObj;
        }
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ARCoinPlacer] {message}");
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== ARCoinPlacer Status ===");
            Debug.Log($"Detected Planes: {DetectedPlaneCount}");
            Debug.Log($"Placed Coins: {_placedCoins.Count}");
            Debug.Log($"Raycast Manager: {(raycastManager != null ? "OK" : "MISSING")}");
            Debug.Log($"Anchor Manager: {(anchorManager != null ? "OK" : "MISSING")}");
            Debug.Log("===========================");
        }
        
        #endregion
    }
}
