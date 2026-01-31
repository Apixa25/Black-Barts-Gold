// ============================================================================
// QuickNavigation.cs
// Black Bart's Gold - Quick Navigation for Testing
// Path: Assets/Scripts/UI/QuickNavigation.cs
// ============================================================================
// Simple navigation component for testing scene flow.
// Attach to buttons to enable quick scene transitions.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Quick navigation component for testing.
    /// Allows buttons to navigate to scenes without full authentication.
    /// </summary>
    public class QuickNavigation : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        [Tooltip("Scene to load when button is clicked")]
        private string targetScene = "MainMenu";
        
        [SerializeField]
        [Tooltip("Use scene name from enum")]
        private bool useSceneEnum = true;
        
        [SerializeField]
        private SceneTarget sceneTarget = SceneTarget.MainMenu;
        
        public enum SceneTarget
        {
            Login,
            Register,
            MainMenu,
            ARHunt,
            Wallet,
            Settings,
            ARTest
        }
        
        private Button button;
        
        private void Awake()
        {
            // DON'T use QuickNavigation in Login/Register scenes - let the proper UI handle it!
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "Login" || currentScene == "Register")
            {
                Debug.Log($"[QuickNavigation] ⚠️ DISABLED in {currentScene} scene - proper auth UI will handle this");
                // Don't attach listener - let LoginUI/RegisterUI handle button clicks
                return;
            }
            
            // Auto-detect target based on GameObject name
            AutoDetectTarget();
            
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
                Debug.Log($"[QuickNavigation] Attached to button '{gameObject.name}', target: {sceneTarget}");
            }
        }
        
        /// <summary>
        /// Auto-detect target scene based on button name
        /// </summary>
        private void AutoDetectTarget()
        {
            string name = gameObject.name.ToLower();
            
            if (name.Contains("starthunt") || name.Contains("hunt") || name.Contains("play"))
            {
                sceneTarget = SceneTarget.ARHunt;
            }
            else if (name.Contains("wallet") || name.Contains("balance"))
            {
                sceneTarget = SceneTarget.Wallet;
            }
            else if (name.Contains("setting"))
            {
                sceneTarget = SceneTarget.Settings;
            }
            else if (name.Contains("login") || name.Contains("signin"))
            {
                sceneTarget = SceneTarget.MainMenu; // Skip to main for testing
            }
            else if (name.Contains("register") || name.Contains("create") || name.Contains("signup"))
            {
                sceneTarget = SceneTarget.Register;
            }
            else if (name.Contains("back") || name.Contains("menu") || name.Contains("home"))
            {
                sceneTarget = SceneTarget.MainMenu;
            }
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }
        
        public void OnClick()
        {
            string sceneName = useSceneEnum ? sceneTarget.ToString() : targetScene;
            Debug.Log($"[QuickNavigation] Button clicked! Loading scene: {sceneName}");
            
            try
            {
                // Use async loading for smoother transitions
                StartCoroutine(LoadSceneAsync(sceneName));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuickNavigation] Failed to load scene '{sceneName}': {e.Message}");
                // Fallback to direct load
                SceneManager.LoadScene(sceneName);
            }
        }
        
        private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[QuickNavigation] Starting async load of: {sceneName}");
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"[QuickNavigation] Failed to start async load for: {sceneName}");
                SceneManager.LoadScene(sceneName);
                yield break;
            }
            
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[QuickNavigation] Loading progress: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[QuickNavigation] Scene loaded successfully: {sceneName}");
        }
        
        /// <summary>
        /// Load a specific scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"[QuickNavigation] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// Load MainMenu scene
        /// </summary>
        public void GoToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// Load ARHunt scene
        /// </summary>
        public void GoToARHunt()
        {
            SceneManager.LoadScene("ARHunt");
        }
        
        /// <summary>
        /// Load Login scene
        /// </summary>
        public void GoToLogin()
        {
            SceneManager.LoadScene("Login");
        }
    }
}
