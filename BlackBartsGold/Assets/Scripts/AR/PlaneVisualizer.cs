// ============================================================================
// PlaneVisualizer.cs
// Black Bart's Gold - AR Plane Visualization (Debug)
// Path: Assets/Scripts/AR/PlaneVisualizer.cs
// ============================================================================
// Visualizes detected AR planes for debugging. In production, planes are
// invisible but we need to see them during development to verify AR works.
// Reference: BUILD-GUIDE.md Prompt 2.2
// ============================================================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages visualization of AR detected planes.
    /// For debugging only - disable in production builds.
    /// </summary>
    public class PlaneVisualizer : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to AR Plane Manager")]
        private ARPlaneManager planeManager;
        
        [Header("Visualization Settings")]
        [SerializeField]
        [Tooltip("Enable plane visualization")]
        private bool showPlanes = true;
        
        [SerializeField]
        [Tooltip("Material for horizontal planes (floors, tables)")]
        private Material horizontalPlaneMaterial;
        
        [SerializeField]
        [Tooltip("Material for vertical planes (walls)")]
        private Material verticalPlaneMaterial;
        
        [SerializeField]
        [Tooltip("Color for horizontal planes")]
        private Color horizontalColor = new Color(0f, 1f, 0f, 0.3f); // Green semi-transparent
        
        [SerializeField]
        [Tooltip("Color for vertical planes")]
        private Color verticalColor = new Color(0f, 0.5f, 1f, 0.3f); // Blue semi-transparent
        
        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Show plane boundaries")]
        private bool showBoundaries = true;
        
        [SerializeField]
        [Tooltip("Show plane centers")]
        private bool showCenters = true;
        
        [SerializeField]
        [Tooltip("Debug log plane events")]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Number of currently detected planes
        /// </summary>
        public int PlaneCount => planeManager != null ? planeManager.trackables.count : 0;
        
        /// <summary>
        /// Are planes currently visible?
        /// </summary>
        public bool PlanesVisible => showPlanes;
        
        /// <summary>
        /// Total area of all detected planes (sq meters)
        /// </summary>
        public float TotalPlaneArea { get; private set; } = 0f;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<TrackableId, LineRenderer> boundaryRenderers = new Dictionary<TrackableId, LineRenderer>();
        private Dictionary<TrackableId, GameObject> centerMarkers = new Dictionary<TrackableId, GameObject>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Find plane manager if not assigned
            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
            
            if (planeManager == null)
            {
                Debug.LogError("[PlaneVisualizer] ARPlaneManager not found!");
                enabled = false;
                return;
            }
            
            // Create default materials if not assigned
            CreateDefaultMaterials();
        }
        
        private void OnEnable()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged += OnPlanesChanged;
            }
            
            // Apply initial visibility
            SetPlanesVisible(showPlanes);
        }
        
        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;
            }
        }
        
        private void Update()
        {
            // Update total area
            UpdateTotalArea();
        }
        
        #endregion
        
        #region Plane Events
        
        /// <summary>
        /// Called when planes are added, updated, or removed
        /// </summary>
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Handle added planes
            foreach (var plane in args.added)
            {
                OnPlaneAdded(plane);
            }
            
            // Handle updated planes
            foreach (var plane in args.updated)
            {
                OnPlaneUpdated(plane);
            }
            
            // Handle removed planes
            foreach (var plane in args.removed)
            {
                OnPlaneRemoved(plane);
            }
        }
        
        private void OnPlaneAdded(ARPlane plane)
        {
            if (debugMode)
            {
                string type = plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp ? "Horizontal" : 
                              plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.Vertical ? "Vertical" : "Other";
                Debug.Log($"[PlaneVisualizer] ✅ Plane added: {plane.trackableId} ({type}, {plane.size.x:F1}m x {plane.size.y:F1}m)");
            }
            
            // Apply material based on alignment
            ApplyPlaneMaterial(plane);
            
            // Create debug visuals
            if (showBoundaries)
            {
                CreateBoundaryRenderer(plane);
            }
            
            if (showCenters)
            {
                CreateCenterMarker(plane);
            }
        }
        
        private void OnPlaneUpdated(ARPlane plane)
        {
            // Update boundary renderer
            if (boundaryRenderers.TryGetValue(plane.trackableId, out LineRenderer renderer))
            {
                UpdateBoundaryRenderer(plane, renderer);
            }
            
            // Update center marker
            if (centerMarkers.TryGetValue(plane.trackableId, out GameObject marker))
            {
                marker.transform.position = plane.center;
            }
        }
        
        private void OnPlaneRemoved(ARPlane plane)
        {
            if (debugMode)
            {
                Debug.Log($"[PlaneVisualizer] ❌ Plane removed: {plane.trackableId}");
            }
            
            // Clean up boundary renderer
            if (boundaryRenderers.TryGetValue(plane.trackableId, out LineRenderer renderer))
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
                boundaryRenderers.Remove(plane.trackableId);
            }
            
            // Clean up center marker
            if (centerMarkers.TryGetValue(plane.trackableId, out GameObject marker))
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
                centerMarkers.Remove(plane.trackableId);
            }
        }
        
        #endregion
        
        #region Visualization
        
        /// <summary>
        /// Toggle plane visibility
        /// </summary>
        public void TogglePlanes()
        {
            SetPlanesVisible(!showPlanes);
        }
        
        /// <summary>
        /// Set plane visibility
        /// </summary>
        public void SetPlanesVisible(bool visible)
        {
            showPlanes = visible;
            
            if (planeManager == null) return;
            
            // Set plane prefab visibility
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
            
            // Set boundary renderers visibility
            foreach (var kvp in boundaryRenderers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.enabled = visible && showBoundaries;
                }
            }
            
            // Set center markers visibility
            foreach (var kvp in centerMarkers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(visible && showCenters);
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"[PlaneVisualizer] Planes visible: {visible}");
            }
        }
        
        /// <summary>
        /// Apply appropriate material to plane based on alignment
        /// </summary>
        private void ApplyPlaneMaterial(ARPlane plane)
        {
            MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            
            bool isHorizontal = plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp ||
                               plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalDown;
            
            if (isHorizontal && horizontalPlaneMaterial != null)
            {
                renderer.material = horizontalPlaneMaterial;
            }
            else if (!isHorizontal && verticalPlaneMaterial != null)
            {
                renderer.material = verticalPlaneMaterial;
            }
        }
        
        /// <summary>
        /// Create default materials if not assigned
        /// </summary>
        private void CreateDefaultMaterials()
        {
            if (horizontalPlaneMaterial == null)
            {
                horizontalPlaneMaterial = CreateTransparentMaterial(horizontalColor);
            }
            
            if (verticalPlaneMaterial == null)
            {
                verticalPlaneMaterial = CreateTransparentMaterial(verticalColor);
            }
        }
        
        /// <summary>
        /// Create a simple transparent material
        /// </summary>
        private Material CreateTransparentMaterial(Color color)
        {
            // Use standard shader with transparency
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            Material mat = new Material(shader);
            mat.color = color;
            
            // Set up for transparency
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            return mat;
        }
        
        #endregion
        
        #region Boundary Rendering
        
        /// <summary>
        /// Create line renderer for plane boundary
        /// </summary>
        private void CreateBoundaryRenderer(ARPlane plane)
        {
            GameObject boundaryObj = new GameObject($"Boundary_{plane.trackableId}");
            boundaryObj.transform.SetParent(plane.transform, false);
            
            LineRenderer lineRenderer = boundaryObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            
            UpdateBoundaryRenderer(plane, lineRenderer);
            
            boundaryRenderers[plane.trackableId] = lineRenderer;
        }
        
        /// <summary>
        /// Update boundary renderer positions
        /// </summary>
        private void UpdateBoundaryRenderer(ARPlane plane, LineRenderer renderer)
        {
            var boundary = plane.boundary;
            
            if (boundary.Length == 0)
            {
                renderer.positionCount = 0;
                return;
            }
            
            renderer.positionCount = boundary.Length;
            
            for (int i = 0; i < boundary.Length; i++)
            {
                // Convert 2D boundary to 3D (on plane surface)
                Vector3 pos = new Vector3(boundary[i].x, 0, boundary[i].y);
                renderer.SetPosition(i, pos);
            }
        }
        
        #endregion
        
        #region Center Markers
        
        /// <summary>
        /// Create center marker for plane
        /// </summary>
        private void CreateCenterMarker(ARPlane plane)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Center_{plane.trackableId}";
            marker.transform.position = plane.center;
            marker.transform.localScale = Vector3.one * 0.05f;
            
            // Remove collider
            Collider col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Set material
            MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.red;
            }
            
            centerMarkers[plane.trackableId] = marker;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Calculate total area of all planes
        /// </summary>
        private void UpdateTotalArea()
        {
            if (planeManager == null) return;
            
            float total = 0f;
            foreach (var plane in planeManager.trackables)
            {
                total += plane.size.x * plane.size.y;
            }
            TotalPlaneArea = total;
        }
        
        /// <summary>
        /// Get the largest horizontal plane (good for placing objects)
        /// </summary>
        public ARPlane GetLargestHorizontalPlane()
        {
            if (planeManager == null) return null;
            
            ARPlane largest = null;
            float maxArea = 0f;
            
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment != UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)
                    continue;
                
                float area = plane.size.x * plane.size.y;
                if (area > maxArea)
                {
                    maxArea = area;
                    largest = plane;
                }
            }
            
            return largest;
        }
        
        /// <summary>
        /// Get plane at position (raycast)
        /// </summary>
        public ARPlane GetPlaneAtPosition(Vector3 position, float maxDistance = 0.5f)
        {
            if (planeManager == null) return null;
            
            ARPlane closest = null;
            float minDist = maxDistance;
            
            foreach (var plane in planeManager.trackables)
            {
                float dist = Vector3.Distance(plane.center, position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = plane;
                }
            }
            
            return closest;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print plane info
        /// </summary>
        [ContextMenu("Debug: Print Plane Info")]
        public void DebugPrintPlaneInfo()
        {
            Debug.Log("=== AR Planes ===");
            Debug.Log($"Total Planes: {PlaneCount}");
            Debug.Log($"Total Area: {TotalPlaneArea:F2} sq meters");
            Debug.Log($"Visible: {PlanesVisible}");
            
            if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    Debug.Log($"  - {plane.trackableId}: {plane.alignment}, {plane.size.x:F2}x{plane.size.y:F2}m");
                }
            }
            Debug.Log("=================");
        }
        
        #endregion
    }
}
