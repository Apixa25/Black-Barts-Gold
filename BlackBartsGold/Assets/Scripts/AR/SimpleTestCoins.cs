// ============================================================================
// SimpleTestCoins.cs
// Black Bart's Gold - Simple Test Coin Spawner
// Path: Assets/Scripts/AR/SimpleTestCoins.cs
// ============================================================================
// Super simple coin spawner for testing AR visuals.
// Creates gold coins directly without any dependencies.
// ============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Simple test coin spawner - creates visible gold coins in AR.
    /// No dependencies, just works!
    /// </summary>
    public class SimpleTestCoins : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float spawnDelay = 1.5f;
        [SerializeField] private int numberOfCoins = 3;
        
        [Header("Coin Appearance")]
        [SerializeField] private float coinRadius = 0.15f;
        [SerializeField] private float coinHeight = 0.03f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.05f;
        
        private List<GameObject> spawnedCoins = new List<GameObject>();
        private Camera arCamera;
        
        private void Start()
        {
            arCamera = Camera.main;
            
            if (spawnOnStart)
            {
                Invoke(nameof(SpawnTestCoins), spawnDelay);
            }
        }
        
        /// <summary>
        /// Spawn test coins in front of the camera
        /// </summary>
        [ContextMenu("Spawn Test Coins")]
        public void SpawnTestCoins()
        {
            Debug.Log("[SimpleTestCoins] Spawning coins...");
            
            if (arCamera == null)
            {
                arCamera = Camera.main;
                if (arCamera == null)
                {
                    Debug.LogError("[SimpleTestCoins] No camera found!");
                    return;
                }
            }
            
            // Spawn coins at different positions in front of camera
            // Distance in meters (3-6 feet = 1-2 meters)
            float[] distances = { 1.0f, 1.5f, 2.0f };
            float[] angles = { -20f, 0f, 20f }; // Spread them out horizontally
            float[] values = { 1.00f, 5.00f, 10.00f };
            
            int count = Mathf.Min(numberOfCoins, distances.Length);
            
            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetSpawnPosition(distances[i], angles[i]);
                GameObject coin = CreateCoin(position, values[i]);
                spawnedCoins.Add(coin);
                
                Debug.Log($"[SimpleTestCoins] Spawned ${values[i]} coin at {position}");
            }
            
            Debug.Log($"[SimpleTestCoins] Spawned {count} coins!");
        }
        
        /// <summary>
        /// Get spawn position relative to camera
        /// </summary>
        private Vector3 GetSpawnPosition(float distance, float horizontalAngle)
        {
            // Get camera position and forward direction
            Vector3 camPos = arCamera.transform.position;
            Vector3 camForward = arCamera.transform.forward;
            Vector3 camRight = arCamera.transform.right;
            
            // Calculate position in front of camera
            Vector3 forward = camForward * distance;
            Vector3 right = camRight * (Mathf.Tan(horizontalAngle * Mathf.Deg2Rad) * distance);
            
            Vector3 position = camPos + forward + right;
            
            // Place coins at roughly waist height (lower than camera)
            position.y = camPos.y - 0.5f;
            
            return position;
        }
        
        /// <summary>
        /// Create a gold coin at the given position
        /// </summary>
        private GameObject CreateCoin(Vector3 position, float value)
        {
            // Create coin container
            GameObject coinObj = new GameObject($"TestCoin_${value}");
            coinObj.transform.position = position;
            
            // Add the coin visual (cylinder)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "CoinVisual";
            visual.transform.SetParent(coinObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(coinRadius * 2, coinHeight, coinRadius * 2);
            
            // Make it gold!
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material goldMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                goldMat.color = new Color(1f, 0.84f, 0f); // Gold color
                goldMat.SetFloat("_Smoothness", 0.8f);
                goldMat.SetFloat("_Metallic", 1f);
                renderer.material = goldMat;
            }
            
            // Remove collider (we don't need physics for visuals)
            Collider col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Add value label
            CreateValueLabel(coinObj, value);
            
            // Add animation component
            coinObj.AddComponent<CoinAnimation>().Initialize(rotationSpeed, bobSpeed, bobAmount);
            
            return coinObj;
        }
        
        /// <summary>
        /// Create a floating value label above the coin
        /// </summary>
        private void CreateValueLabel(GameObject parent, float value)
        {
            GameObject labelObj = new GameObject("ValueLabel");
            labelObj.transform.SetParent(parent.transform);
            labelObj.transform.localPosition = new Vector3(0, 0.2f, 0);
            
            // Create TextMesh for 3D text
            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = $"${value:F2}";
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.02f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            
            // Make it always face camera
            labelObj.AddComponent<BillboardText>();
        }
        
        /// <summary>
        /// Clear all spawned coins
        /// </summary>
        [ContextMenu("Clear Coins")]
        public void ClearCoins()
        {
            foreach (var coin in spawnedCoins)
            {
                if (coin != null) Destroy(coin);
            }
            spawnedCoins.Clear();
            Debug.Log("[SimpleTestCoins] Coins cleared");
        }
        
        /// <summary>
        /// Respawn coins (clear and spawn new ones)
        /// </summary>
        [ContextMenu("Respawn Coins")]
        public void RespawnCoins()
        {
            ClearCoins();
            SpawnTestCoins();
        }
    }
    
    /// <summary>
    /// Simple coin animation - spin and bob
    /// </summary>
    public class CoinAnimation : MonoBehaviour
    {
        private float rotationSpeed;
        private float bobSpeed;
        private float bobAmount;
        private Vector3 startPos;
        
        public void Initialize(float rotation, float bob, float amount)
        {
            rotationSpeed = rotation;
            bobSpeed = bob;
            bobAmount = amount;
            startPos = transform.position;
        }
        
        private void Update()
        {
            // Spin
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    /// <summary>
    /// Makes text always face the camera
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        private Camera mainCam;
        
        private void Start()
        {
            mainCam = Camera.main;
        }
        
        private void Update()
        {
            if (mainCam != null)
            {
                transform.LookAt(mainCam.transform);
                transform.Rotate(0, 180, 0); // Flip to face camera correctly
            }
        }
    }
}
