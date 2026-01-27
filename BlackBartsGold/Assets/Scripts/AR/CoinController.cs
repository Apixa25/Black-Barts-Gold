// ============================================================================
// CoinController.cs
// Black Bart's Gold - AR Coin Behavior Controller
// Path: Assets/Scripts/AR/CoinController.cs
// ============================================================================
// Controls individual coin behavior in AR: initialization from data model,
// visual states (normal, locked, hovering), animations, and collection.
// Reference: BUILD-GUIDE.md Prompt 3.2
// ============================================================================

using UnityEngine;
using System;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// GPS accuracy requirements for different operations
    /// </summary>
    public static class GPSAccuracyRequirements
    {
        /// <summary>
        /// Maximum GPS accuracy (in meters) required to collect a coin.
        /// This prevents players from collecting coins when GPS is inaccurate.
        /// Lower = more strict, Higher = more lenient
        /// </summary>
        public const float COLLECTION_ACCURACY = 25f;
        
        /// <summary>
        /// Minimum GPS accuracy to show coins on map (lenient)
        /// </summary>
        public const float DISPLAY_ACCURACY = 200f;
    }

    // CoinVisualState is now defined in Core/Enums.cs
    /// <summary>
    /// Controls an individual coin's behavior and appearance in AR.
    /// Attach to coin prefab root GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CoinController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Visual References")]
        [SerializeField]
        [Tooltip("The 3D coin mesh object")]
        private GameObject coinModel;
        
        [SerializeField]
        [Tooltip("Mesh renderer for material changes")]
        private MeshRenderer coinRenderer;
        
        [SerializeField]
        [Tooltip("Text displaying coin value (billboard)")]
        private TMPro.TMP_Text valueLabel;
        
        [SerializeField]
        [Tooltip("Lock icon overlay for locked coins")]
        private GameObject lockIcon;
        
        [Header("Materials")]
        [SerializeField]
        private Material goldMaterial;
        
        [SerializeField]
        private Material silverMaterial;
        
        [SerializeField]
        private Material bronzeMaterial;
        
        [SerializeField]
        private Material platinumMaterial;
        
        [SerializeField]
        private Material diamondMaterial;
        
        [SerializeField]
        private Material lockedMaterial;
        
        [SerializeField]
        private Material poolMaterial; // Unknown value (shows "?")
        
        [Header("Particle Effects")]
        [SerializeField]
        private ParticleSystem sparkleEffect;
        
        [SerializeField]
        private ParticleSystem collectionEffect;
        
        [SerializeField]
        private ParticleSystem lockedPulseEffect;
        
        [Header("Audio")]
        [SerializeField]
        private AudioSource audioSource;
        
        [SerializeField]
        private AudioClip hoverSound;
        
        [SerializeField]
        private AudioClip collectSound;
        
        [SerializeField]
        private AudioClip lockedSound;
        
        [Header("Animation Settings")]
        [SerializeField]
        private float spinSpeed = 45f; // Degrees per second
        
        [SerializeField]
        private float bobAmplitude = 0.1f; // Up/down distance
        
        [SerializeField]
        private float bobSpeed = 1.5f; // Bobs per second
        
        [SerializeField]
        private float collectAnimDuration = 0.8f;
        
        [Header("State")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Coin data model
        /// </summary>
        public Coin CoinData { get; private set; }
        
        /// <summary>
        /// Unique ID of this coin
        /// </summary>
        public string CoinId => CoinData?.id ?? "";
        
        /// <summary>
        /// Is this coin locked (above player's find limit)?
        /// </summary>
        public bool IsLocked { get; private set; } = false;
        
        /// <summary>
        /// Is this coin within collection range?
        /// </summary>
        public bool IsInRange { get; private set; } = false;
        
        /// <summary>
        /// Is player currently hovering over this coin?
        /// </summary>
        public bool IsHovered { get; private set; } = false;
        
        /// <summary>
        /// Is collection animation playing?
        /// </summary>
        public bool IsCollecting { get; private set; } = false;
        
        /// <summary>
        /// Has this coin been collected?
        /// </summary>
        public bool IsCollected { get; private set; } = false;
        
        /// <summary>
        /// Current visual state
        /// </summary>
        public CoinVisualState CurrentState { get; private set; } = CoinVisualState.Normal;
        
        /// <summary>
        /// Distance from player (meters)
        /// </summary>
        public float DistanceFromPlayer { get; private set; } = float.MaxValue;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when coin is collected
        /// </summary>
        public event Action<CoinController> OnCollected;
        
        /// <summary>
        /// Fired when coin is tapped but locked
        /// </summary>
        public event Action<CoinController> OnLockedTap;
        
        /// <summary>
        /// Fired when coin is tapped but out of range
        /// </summary>
        public event Action<CoinController> OnOutOfRangeTap;
        
        /// <summary>
        /// Fired when hover starts
        /// </summary>
        public event Action<CoinController> OnHoverStart;
        
        /// <summary>
        /// Fired when hover ends
        /// </summary>
        public event Action<CoinController> OnHoverEnd;
        
        #endregion
        
        #region Private Fields
        
        private Vector3 initialPosition;
        private float bobOffset = 0f;
        private Camera mainCamera;
        private Material currentMaterial;
        private Transform cameraTransform;
        
        // Collection animation
        private Vector3 collectStartPos;
        private float collectTimer = 0f;
        
        // ================================================================
        // COIN DISPLAY MODES
        // ================================================================
        // CompassBillboard: Coin floats in correct compass direction (distance viewing)
        // Anchored: Coin is fixed on AR plane (after "Reveal")
        // ================================================================
        
        /// <summary>
        /// Display mode for this coin
        /// </summary>
        public enum CoinDisplayMode
        {
            CompassBillboard,  // Float in compass direction (default for distance)
            Anchored           // Fixed on AR plane (after Reveal)
        }
        
        private CoinDisplayMode _displayMode = CoinDisplayMode.CompassBillboard;
        private float _billboardDistance = 12f; // Visual distance in front of camera (meters)
        private float _billboardMinDistance = 5f; // Don't show billboard if closer than this
        private float _compassSmoothTime = 0.15f; // Smoothing for compass jitter
        private float _smoothedBearing = 0f;
        private float _bearingVelocity = 0f;
        
        /// <summary>
        /// Set the display mode for this coin
        /// </summary>
        public void SetDisplayMode(CoinDisplayMode mode)
        {
            _displayMode = mode;
            Debug.Log($"[CoinController] Coin {CoinId} display mode: {mode}");
            
            // ================================================================
            // When switching to Anchored mode, update initialPosition
            // so the bob animation works correctly from the anchor position
            // ================================================================
            if (mode == CoinDisplayMode.Anchored)
            {
                initialPosition = transform.position;
                Debug.Log($"[CoinController] Coin {CoinId} anchored at: {initialPosition}");
            }
        }
        
        /// <summary>
        /// Is this coin in billboard mode (floating in compass direction)?
        /// </summary>
        public bool IsBillboardMode => _displayMode == CoinDisplayMode.CompassBillboard;
        
        /// <summary>
        /// Is this coin anchored to AR space?
        /// </summary>
        public bool IsAnchored => _displayMode == CoinDisplayMode.Anchored;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            
            // Ensure we have a collider for raycasting
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                // Add sphere collider if none exists
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.2f;
            }
            
            // Set layer for coin detection
            gameObject.layer = LayerMask.NameToLayer("Coin");
            if (gameObject.layer == -1)
            {
                // Coin layer doesn't exist, use default
                gameObject.layer = 0;
            }
            
            // Tag for identification
            gameObject.tag = "Coin";
            
            // ================================================================
            // AUTO-FIND coinModel if not set in Inspector
            // The CoinManager creates a "CoinModel" child for default coins
            // ================================================================
            if (coinModel == null)
            {
                Transform modelTransform = transform.Find("CoinModel");
                if (modelTransform != null)
                {
                    coinModel = modelTransform.gameObject;
                    Debug.Log($"[CoinController] Auto-found CoinModel child object");
                }
                else
                {
                    // Fallback: use first child if exists
                    if (transform.childCount > 0)
                    {
                        coinModel = transform.GetChild(0).gameObject;
                        Debug.Log($"[CoinController] Using first child as CoinModel: {coinModel.name}");
                    }
                }
            }
            
            // Auto-find renderer on coin model
            if (coinRenderer == null && coinModel != null)
            {
                coinRenderer = coinModel.GetComponent<MeshRenderer>();
            }
        }
        
        private void Start()
        {
            initialPosition = transform.position;
            
            // Random bob phase so coins don't all bob in sync
            bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            
            // Start sparkle effect
            if (sparkleEffect != null && !IsLocked)
            {
                sparkleEffect.Play();
            }
        }
        
        private void Update()
        {
            if (IsCollecting)
            {
                UpdateCollectAnimation();
                return;
            }
            
            if (IsCollected) return;
            
            // CRITICAL FIX: Find camera if not found in Awake()
            // This can happen if coin spawns before AR camera is ready
            // or if AR camera isn't tagged as "MainCamera"
            if (cameraTransform == null)
            {
                // Try Camera.main first
                mainCamera = Camera.main;
                
                // Fallback: Find ANY camera if Camera.main is null
                // AR Foundation cameras aren't always tagged as MainCamera
                if (mainCamera == null)
                {
                    mainCamera = FindFirstObjectByType<Camera>();
                    if (mainCamera != null)
                    {
                        Debug.Log($"[CoinController] ⚠️ Camera.main was null! Found fallback camera: {mainCamera.name}");
                    }
                }
                
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                    Debug.Log($"[CoinController] ✅ Found camera: {mainCamera.name} at {cameraTransform.position}");
                }
                else
                {
                    Debug.LogWarning("[CoinController] ❌ No camera found in scene!");
                }
            }
            
            // ================================================================
            // COMPASS-BILLBOARD MODE: Position coin in correct compass direction
            // This is the PROPER way to show coins at a distance - NOT AR anchoring
            // Coin floats in the direction you need to look, relative to camera
            // ================================================================
            if (_displayMode == CoinDisplayMode.CompassBillboard)
            {
                UpdateCompassBillboard();
            }
            
            // Spin animation
            UpdateSpinAnimation();
            
            // Bob animation
            UpdateBobAnimation();
            
            // Billboard value label to camera
            UpdateValueLabelBillboard();
            
            // Update distance from player
            UpdateDistanceFromPlayer();
        }
        
        /// <summary>
        /// COMPASS-BILLBOARD MODE: Position coin floating in the correct compass direction
        /// This is the PROPER way to show coins at a distance (like navigation waypoints)
        /// 
        /// IMPORTANT: Since AR camera rotation isn't tracking, we position coins in
        /// WORLD SPACE based on compass direction, not camera space!
        /// 
        /// How it works:
        /// 1. Calculate GPS bearing from player to coin  
        /// 2. Subtract current compass heading to get relative bearing
        /// 3. Position coin at fixed distance in that direction (relative to player)
        /// </summary>
        private void UpdateCompassBillboard()
        {
            if (CoinData == null) 
            {
                Debug.LogWarning($"[CoinController] Billboard skip: CoinData is null");
                return;
            }
            
            if (cameraTransform == null) 
            {
                Debug.LogWarning($"[CoinController] Billboard skip: cameraTransform is null");
                return;
            }
            
            // Get player's GPS location
            var gpsManager = BlackBartsGold.Location.GPSManager.Instance;
            if (gpsManager == null || gpsManager.CurrentLocation == null) 
            {
                Debug.LogWarning($"[CoinController] Billboard skip: No GPS location");
                return;
            }
            
            var playerLoc = gpsManager.CurrentLocation;
            var coinLoc = new LocationData(CoinData.latitude, CoinData.longitude);
            
            // Calculate GPS bearing from player to coin (0° = North, 90° = East)
            float gpsBearing = (float)playerLoc.BearingTo(coinLoc);
            
            // Get current compass heading (direction player is facing)
            float compassHeading = Input.compass.enabled ? Input.compass.trueHeading : 0f;
            
            // Calculate relative bearing (how far off from where player is looking)
            // 0° = directly ahead, 90° = to the right, 180° = behind, -90° = left
            float targetBearing = gpsBearing - compassHeading;
            
            // Normalize to -180 to 180 range
            while (targetBearing > 180f) targetBearing -= 360f;
            while (targetBearing < -180f) targetBearing += 360f;
            
            // Smooth the bearing to reduce compass jitter
            _smoothedBearing = Mathf.SmoothDampAngle(_smoothedBearing, targetBearing, ref _bearingVelocity, _compassSmoothTime);
            
            // Calculate actual GPS distance
            float gpsDistance = (float)playerLoc.DistanceTo(coinLoc);
            
            // ================================================================
            // COMPASS-BASED POSITIONING (AR rotation is broken!)
            // ================================================================
            // Since AR camera rotation isn't tracking, we calculate world direction
            // from COMPASS HEADING instead of camera.forward
            //
            // Unity world: +Z = forward, +X = right
            // Compass: 0° = North, 90° = East
            // So compass 0° = +Z, compass 90° = +X
            
            float viewDistance = 5f; // Fixed distance in front of player
            
            // Calculate world-space "forward" based on compass heading
            float headingRad = compassHeading * Mathf.Deg2Rad;
            Vector3 compassForward = new Vector3(Mathf.Sin(headingRad), 0, Mathf.Cos(headingRad));
            Vector3 compassRight = new Vector3(Mathf.Cos(headingRad), 0, -Mathf.Sin(headingRad));
            
            // Calculate horizontal offset based on relative bearing
            // When bearing is 0° (ahead), coin is centered
            // When bearing is 90° (right), coin shifts right
            // When bearing is -90° (left), coin shifts left
            float maxHorizontalOffset = 3f;
            float horizontalOffset = Mathf.Sin(_smoothedBearing * Mathf.Deg2Rad) * maxHorizontalOffset;
            
            // Vertical offset - coins are slightly below eye level  
            float verticalOffset = -0.3f;
            
            // If coin is behind (bearing > 90° or < -90°), push it to edge
            bool isBehind = Mathf.Abs(_smoothedBearing) > 90f;
            if (isBehind)
            {
                horizontalOffset = Mathf.Sign(_smoothedBearing) * maxHorizontalOffset;
            }
            
            // Use camera position (which does track)
            Vector3 camPos = cameraTransform.position;
            
            // Calculate target position using compass-derived directions
            Vector3 forward = compassForward;
            Vector3 right = compassRight;
            Vector3 up = Vector3.up;
            
            Vector3 targetPosition = camPos 
                + forward * viewDistance 
                + right * horizontalOffset 
                + up * verticalOffset;
            
            // Smoothly move to target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            
            // Scale based on GPS distance - closer coins appear bigger!
            float scaleFactor = Mathf.Clamp(3f - gpsDistance * 0.2f, 0.5f, 3f);
            transform.localScale = Vector3.one * scaleFactor;
            
            // Always face the camera (billboard effect)
            Vector3 lookDir = camPos - transform.position;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDir);
            }
            
            // Debug logging every 2 seconds
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[CoinController] 🪙 Billboard: bearing={_smoothedBearing:F0}°, GPS dist={gpsDistance:F1}m, behind={isBehind}, pos={transform.position}");
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize coin from data model
        /// </summary>
        public void Initialize(Coin coinData, bool locked, bool inRange)
        {
            CoinData = coinData;
            IsLocked = locked;
            IsInRange = inRange;
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Initialized: {coinData.id}, Value: {coinData.GetDisplayValue()}, Locked: {locked}, InRange: {inRange}");
            }
            
            // Set visual appearance
            UpdateVisuals();
            
            // Set value label
            UpdateValueLabel();
            
            // Set initial state
            if (locked)
            {
                SetState(CoinVisualState.Locked);
            }
            else
            {
                SetState(CoinVisualState.Normal);
            }
        }
        
        /// <summary>
        /// Initialize with just basic values (for testing)
        /// </summary>
        public void InitializeSimple(float value, bool locked = false, bool inRange = true)
        {
            var testCoin = Coin.CreateTestCoin(value);
            Initialize(testCoin, locked, inRange);
        }
        
        #endregion
        
        #region Visual Updates
        
        /// <summary>
        /// Update visual appearance based on coin data
        /// </summary>
        private void UpdateVisuals()
        {
            if (CoinData == null) return;
            
            // Apply material based on tier and locked state
            Material targetMaterial = GetMaterialForCoin();
            
            if (coinRenderer != null && targetMaterial != null)
            {
                coinRenderer.material = targetMaterial;
                currentMaterial = targetMaterial;
            }
            
            // Show/hide lock icon
            if (lockIcon != null)
            {
                lockIcon.SetActive(IsLocked);
            }
            
            // Adjust effects
            if (sparkleEffect != null)
            {
                if (IsLocked)
                {
                    sparkleEffect.Stop();
                }
                else
                {
                    sparkleEffect.Play();
                }
            }
            
            if (lockedPulseEffect != null)
            {
                if (IsLocked)
                {
                    lockedPulseEffect.Play();
                }
                else
                {
                    lockedPulseEffect.Stop();
                }
            }
        }
        
        /// <summary>
        /// Get appropriate material for this coin
        /// </summary>
        private Material GetMaterialForCoin()
        {
            if (IsLocked && lockedMaterial != null)
            {
                return lockedMaterial;
            }
            
            if (CoinData == null) return goldMaterial;
            
            // Pool coins show unknown (silver/mystery)
            if (CoinData.coinType == CoinType.Pool && poolMaterial != null)
            {
                return poolMaterial;
            }
            
            // Material based on tier
            return CoinData.currentTier switch
            {
                CoinTier.Bronze => bronzeMaterial ?? goldMaterial,
                CoinTier.Silver => silverMaterial ?? goldMaterial,
                CoinTier.Gold => goldMaterial,
                CoinTier.Platinum => platinumMaterial ?? goldMaterial,
                CoinTier.Diamond => diamondMaterial ?? goldMaterial,
                _ => goldMaterial
            };
        }
        
        /// <summary>
        /// Update value label text
        /// </summary>
        private void UpdateValueLabel()
        {
            if (valueLabel == null) return;
            
            if (CoinData == null)
            {
                valueLabel.text = "?";
                return;
            }
            
            valueLabel.text = CoinData.GetDisplayValue();
            
            // Color based on tier/state
            if (IsLocked)
            {
                valueLabel.color = new Color(0.94f, 0.27f, 0.27f); // Red
            }
            else
            {
                valueLabel.color = GetColorForTier(CoinData.currentTier);
            }
        }
        
        /// <summary>
        /// Get color for coin tier
        /// </summary>
        private Color GetColorForTier(CoinTier tier)
        {
            return tier switch
            {
                CoinTier.Bronze => new Color(0.8f, 0.5f, 0.2f),
                CoinTier.Silver => new Color(0.75f, 0.75f, 0.75f),
                CoinTier.Gold => new Color(1f, 0.84f, 0f),
                CoinTier.Platinum => new Color(0.9f, 0.9f, 0.95f),
                CoinTier.Diamond => new Color(0.5f, 0.8f, 1f),
                _ => Color.white
            };
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set visual state
        /// </summary>
        public void SetState(CoinVisualState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            
            switch (newState)
            {
                case CoinVisualState.Normal:
                    // Reset to normal appearance
                    if (coinModel != null)
                    {
                        coinModel.transform.localScale = Vector3.one;
                    }
                    break;
                    
                case CoinVisualState.Hovering:
                    // Slight scale up
                    if (coinModel != null)
                    {
                        coinModel.transform.localScale = Vector3.one * 1.2f;
                    }
                    break;
                    
                case CoinVisualState.InRange:
                    // Glow effect
                    if (coinModel != null)
                    {
                        coinModel.transform.localScale = Vector3.one * 1.3f;
                    }
                    break;
                    
                case CoinVisualState.Locked:
                    // Locked appearance
                    if (coinModel != null)
                    {
                        coinModel.transform.localScale = Vector3.one * 0.9f;
                    }
                    break;
                    
                case CoinVisualState.Collecting:
                    // Start collection animation
                    break;
            }
        }
        
        /// <summary>
        /// Update locked status
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (IsLocked == locked) return;
            
            IsLocked = locked;
            UpdateVisuals();
            
            if (locked)
            {
                SetState(CoinVisualState.Locked);
            }
            else
            {
                SetState(CoinVisualState.Normal);
            }
        }
        
        /// <summary>
        /// Update in-range status
        /// </summary>
        public void SetInRange(bool inRange)
        {
            IsInRange = inRange;
            
            if (CoinData != null)
            {
                CoinData.isInRange = inRange;
            }
        }
        
        #endregion
        
        #region Hover Handling
        
        /// <summary>
        /// Called when player starts hovering over this coin
        /// </summary>
        public void OnHover()
        {
            if (IsCollecting || IsCollected) return;
            
            IsHovered = true;
            
            if (IsLocked)
            {
                SetState(CoinVisualState.Locked);
            }
            else if (IsInRange)
            {
                SetState(CoinVisualState.InRange);
            }
            else
            {
                SetState(CoinVisualState.Hovering);
            }
            
            // Play hover sound
            PlaySound(hoverSound);
            
            OnHoverStart?.Invoke(this);
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Hover start: {CoinId}");
            }
        }
        
        /// <summary>
        /// Called when player stops hovering over this coin
        /// </summary>
        public void OnUnhover()
        {
            if (IsCollecting || IsCollected) return;
            
            IsHovered = false;
            
            if (IsLocked)
            {
                SetState(CoinVisualState.Locked);
            }
            else
            {
                SetState(CoinVisualState.Normal);
            }
            
            OnHoverEnd?.Invoke(this);
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Hover end: {CoinId}");
            }
        }
        
        #endregion
        
        #region Collection
        
        /// <summary>
        /// Attempt to collect this coin
        /// </summary>
        public bool TryCollect()
        {
            if (IsCollecting || IsCollected)
            {
                return false;
            }
            
            if (IsLocked)
            {
                PlaySound(lockedSound);
                OnLockedTap?.Invoke(this);
                
                if (debugMode)
                {
                    Debug.Log($"[CoinController] Collection blocked - coin is locked: {CoinId}");
                }
                return false;
            }
            
            if (!IsInRange)
            {
                OnOutOfRangeTap?.Invoke(this);
                
                if (debugMode)
                {
                    Debug.Log($"[CoinController] Collection blocked - out of range: {CoinId}");
                }
                return false;
            }
            
            // CHECK GPS ACCURACY - Prevent collection with poor GPS
            // This prevents cheating by collecting coins when GPS is inaccurate
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                float currentAccuracy = GPSManager.Instance.CurrentLocation.horizontalAccuracy;
                if (currentAccuracy > GPSAccuracyRequirements.COLLECTION_ACCURACY)
                {
                    Debug.LogWarning($"[CoinController] Collection blocked - GPS accuracy too low: {currentAccuracy:F0}m (need < {GPSAccuracyRequirements.COLLECTION_ACCURACY}m)");
                    // TODO: Show user feedback "GPS signal too weak to collect"
                    return false;
                }
            }
            
            // Start collection!
            StartCollectAnimation();
            return true;
        }
        
        /// <summary>
        /// Start the collection animation
        /// </summary>
        private void StartCollectAnimation()
        {
            IsCollecting = true;
            SetState(CoinVisualState.Collecting);
            
            collectStartPos = transform.position;
            collectTimer = 0f;
            
            // Play collection effect
            if (collectionEffect != null)
            {
                collectionEffect.Play();
            }
            
            // Stop sparkle
            if (sparkleEffect != null)
            {
                sparkleEffect.Stop();
            }
            
            // Play sound
            PlaySound(collectSound);
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Collection started: {CoinId}");
            }
        }
        
        /// <summary>
        /// Update collection animation
        /// </summary>
        private void UpdateCollectAnimation()
        {
            collectTimer += Time.deltaTime;
            float t = collectTimer / collectAnimDuration;
            
            if (t >= 1f)
            {
                // Animation complete
                FinishCollection();
                return;
            }
            
            // Ease out curve
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Move toward camera
            if (cameraTransform != null)
            {
                Vector3 targetPos = cameraTransform.position + cameraTransform.forward * 0.5f;
                transform.position = Vector3.Lerp(collectStartPos, targetPos, easeT);
            }
            
            // Spin faster
            if (coinModel != null)
            {
                coinModel.transform.Rotate(0, spinSpeed * 5f * Time.deltaTime, 0);
            }
            
            // Scale down
            float scale = Mathf.Lerp(1f, 0f, easeT);
            transform.localScale = Vector3.one * scale;
        }
        
        /// <summary>
        /// Complete the collection
        /// </summary>
        private void FinishCollection()
        {
            IsCollecting = false;
            IsCollected = true;
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Collection complete: {CoinId}, Value: {CoinData?.value ?? 0}");
            }
            
            // Notify listeners
            OnCollected?.Invoke(this);
            
            // Deactivate (CoinManager will handle cleanup)
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Animations
        
        /// <summary>
        /// Spin the coin around Y axis
        /// </summary>
        private void UpdateSpinAnimation()
        {
            if (coinModel == null) return;
            
            float speed = spinSpeed;
            
            // Spin faster when hovered
            if (IsHovered)
            {
                speed *= 2f;
            }
            
            // Spin slower when locked
            if (IsLocked)
            {
                speed *= 0.5f;
            }
            
            coinModel.transform.Rotate(0, speed * Time.deltaTime, 0);
        }
        
        /// <summary>
        /// Bob up and down
        /// ================================================================
        /// FIX: In CompassBillboard mode, DO NOT use initialPosition!
        /// The billboard mode sets the position, and bob should only
        /// add a relative Y offset, NOT reset to the initial spawn position.
        /// ================================================================
        /// </summary>
        private void UpdateBobAnimation()
        {
            // ================================================================
            // CRITICAL FIX: Skip bob animation in billboard mode!
            // The UpdateCompassBillboard() method handles positioning.
            // Using initialPosition here would reset the coin to (0,0,0).
            // ================================================================
            if (_displayMode == CoinDisplayMode.CompassBillboard)
            {
                // In billboard mode, just apply a small vertical oscillation
                // to the COIN MODEL, not the whole transform
                if (coinModel != null)
                {
                    float bobY = Mathf.Sin((Time.time * bobSpeed * Mathf.PI * 2f) + bobOffset) * bobAmplitude;
                    Vector3 localPos = coinModel.transform.localPosition;
                    localPos.y = bobY;
                    coinModel.transform.localPosition = localPos;
                }
                return;
            }
            
            // Only use initialPosition-based bob for Anchored coins
            float bobYAnchored = Mathf.Sin((Time.time * bobSpeed * Mathf.PI * 2f) + bobOffset) * bobAmplitude;
            
            Vector3 pos = initialPosition;
            pos.y += bobYAnchored;
            transform.position = pos;
        }
        
        /// <summary>
        /// Keep value label facing camera
        /// </summary>
        private void UpdateValueLabelBillboard()
        {
            if (valueLabel == null || cameraTransform == null) return;
            
            // Face camera
            valueLabel.transform.LookAt(
                valueLabel.transform.position + cameraTransform.forward
            );
        }
        
        /// <summary>
        /// Play appear animation (scale up from 0)
        /// </summary>
        public void PlayAppearAnimation(float duration = 0.3f)
        {
            StartCoroutine(AppearAnimationCoroutine(duration));
        }
        
        private System.Collections.IEnumerator AppearAnimationCoroutine(float duration)
        {
            transform.localScale = Vector3.zero;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                
                // Overshoot ease
                float scale = 1f + (Mathf.Sin(t * Mathf.PI) * 0.2f);
                if (t >= 1f) scale = 1f;
                
                transform.localScale = Vector3.one * Mathf.Lerp(0, scale, t);
                yield return null;
            }
            
            transform.localScale = Vector3.one;
        }
        
        #endregion
        
        #region Distance Tracking
        
        /// <summary>
        /// Update distance from player camera
        /// </summary>
        private void UpdateDistanceFromPlayer()
        {
            if (cameraTransform == null) return;
            
            DistanceFromPlayer = Vector3.Distance(transform.position, cameraTransform.position);
            
            // Update in-range status based on distance
            // Collection range is 5 meters (from docs)
            bool wasInRange = IsInRange;
            IsInRange = DistanceFromPlayer <= 5f;
            
            if (CoinData != null)
            {
                CoinData.distanceFromPlayer = DistanceFromPlayer;
                CoinData.isInRange = IsInRange;
            }
            
            // State change on range change
            if (wasInRange != IsInRange && IsHovered && !IsLocked)
            {
                SetState(IsInRange ? CoinVisualState.InRange : CoinVisualState.Hovering);
            }
        }
        
        /// <summary>
        /// Get proximity zone based on distance
        /// </summary>
        public ProximityZone GetProximityZone()
        {
            if (DistanceFromPlayer <= 5f) return ProximityZone.Collectible;
            if (DistanceFromPlayer <= 15f) return ProximityZone.Near;
            if (DistanceFromPlayer <= 30f) return ProximityZone.Medium;
            if (DistanceFromPlayer <= 50f) return ProximityZone.Far;
            return ProximityZone.OutOfRange;
        }
        
        #endregion
        
        #region Audio
        
        /// <summary>
        /// Play a sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            
            audioSource.PlayOneShot(clip);
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Force collect this coin
        /// </summary>
        [ContextMenu("Debug: Force Collect")]
        public void DebugForceCollect()
        {
            IsLocked = false;
            IsInRange = true;
            TryCollect();
        }
        
        /// <summary>
        /// Debug: Toggle locked state
        /// </summary>
        [ContextMenu("Debug: Toggle Locked")]
        public void DebugToggleLocked()
        {
            SetLocked(!IsLocked);
        }
        
        /// <summary>
        /// Debug: Print state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log($"=== Coin {CoinId} ===");
            Debug.Log($"Value: {CoinData?.GetDisplayValue() ?? "null"}");
            Debug.Log($"Tier: {CoinData?.currentTier}");
            Debug.Log($"Locked: {IsLocked}");
            Debug.Log($"InRange: {IsInRange}");
            Debug.Log($"Hovered: {IsHovered}");
            Debug.Log($"Distance: {DistanceFromPlayer:F1}m");
            Debug.Log($"State: {CurrentState}");
            Debug.Log("==================");
        }
        
        #endregion
    }
    
}
