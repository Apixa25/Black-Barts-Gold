// ============================================================================
// StartupLogger.cs
// Black Bart's Gold - Startup Diagnostic Logger
// Path: Assets/Scripts/Debug/StartupLogger.cs
// ============================================================================
// Add this to any GameObject to verify it's running.
// Logs ONCE on Start and periodically in Update.
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.Diagnostics
{
    /// <summary>
    /// Simple diagnostic script to verify GameObjects are active.
    /// </summary>
    public class StartupLogger : MonoBehaviour
    {
        [SerializeField] private string logTag = "StartupLogger";
        [SerializeField] private float logInterval = 5f;
        
        private float lastLogTime;
        
        private void Awake()
        {
            UnityEngine.Debug.Log($"[{logTag}] AWAKE on {gameObject.name}");
        }
        
        private void Start()
        {
            UnityEngine.Debug.Log($"[{logTag}] START on {gameObject.name} - Position: {transform.position}");
            
            // Log all sibling components
            var components = GetComponents<MonoBehaviour>();
            foreach (var c in components)
            {
                if (c != this)
                {
                    UnityEngine.Debug.Log($"[{logTag}] Sibling component: {c.GetType().Name}, enabled={c.enabled}");
                }
            }
        }
        
        private void Update()
        {
            if (Time.time - lastLogTime > logInterval)
            {
                lastLogTime = Time.time;
                UnityEngine.Debug.Log($"[{logTag}] UPDATE on {gameObject.name} at {Time.time:F1}s");
            }
        }
    }
}
