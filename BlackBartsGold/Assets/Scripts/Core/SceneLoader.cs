// ============================================================================
// SceneLoader.cs
// Black Bart's Gold - Scene Loading Utilities
// Path: Assets/Scripts/Core/SceneLoader.cs
// ============================================================================
// Static utility class for scene loading operations. Provides synchronous
// and asynchronous scene loading with optional loading screen support.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Static utility class for loading scenes.
    /// Provides both synchronous and async loading with progress tracking.
    /// </summary>
    public static class SceneLoader
    {
        #region Events
        
        /// <summary>
        /// Progress update during async load (0-1)
        /// </summary>
        public static event Action<float> OnLoadProgress;
        
        /// <summary>
        /// Fired when scene load completes
        /// </summary>
        public static event Action<string> OnLoadComplete;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is a scene currently being loaded?
        /// </summary>
        public static bool IsLoading { get; private set; } = false;
        
        /// <summary>
        /// Current load progress (0-1)
        /// </summary>
        public static float LoadProgress { get; private set; } = 0f;
        
        #endregion
        
        #region Synchronous Loading
        
        /// <summary>
        /// Load a scene by SceneNames enum (synchronous)
        /// </summary>
        public static void LoadScene(SceneNames scene)
        {
            LoadScene(scene.ToString());
        }
        
        /// <summary>
        /// Load a scene by name string (synchronous)
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene, ignoring request for: {sceneName}");
                return;
            }
            
            Debug.Log($"[SceneLoader] 🚀 Loading scene: {sceneName}");
            
            try
            {
                SceneManager.LoadScene(sceneName);
                OnLoadComplete?.Invoke(sceneName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneLoader] ❌ Failed to load scene '{sceneName}': {e.Message}");
            }
        }
        
        #endregion
        
        #region Asynchronous Loading
        
        /// <summary>
        /// Load a scene asynchronously by SceneNames enum
        /// </summary>
        public static void LoadSceneAsync(SceneNames scene, MonoBehaviour caller, bool showLoadingScreen = true)
        {
            LoadSceneAsync(scene.ToString(), caller, showLoadingScreen);
        }
        
        /// <summary>
        /// Load a scene asynchronously by name string
        /// </summary>
        public static void LoadSceneAsync(string sceneName, MonoBehaviour caller, bool showLoadingScreen = true)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene, ignoring request for: {sceneName}");
                return;
            }
            
            caller.StartCoroutine(LoadSceneAsyncCoroutine(sceneName, showLoadingScreen));
        }
        
        /// <summary>
        /// Coroutine for async scene loading
        /// </summary>
        private static IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool showLoadingScreen)
        {
            IsLoading = true;
            LoadProgress = 0f;
            
            Debug.Log($"[SceneLoader] 🚀 Starting async load: {sceneName}");
            
            // Optional: Show loading screen
            if (showLoadingScreen)
            {
                // TODO: Show loading UI
                // LoadingScreen.Show();
            }
            
            // Start async load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"[SceneLoader] ❌ Failed to start async load for: {sceneName}");
                IsLoading = false;
                yield break;
            }
            
            // Don't allow scene activation until ready
            asyncLoad.allowSceneActivation = false;
            
            // Track progress
            while (!asyncLoad.isDone)
            {
                // Progress goes from 0 to 0.9 during load
                // Then jumps to 1.0 when allowSceneActivation is true
                LoadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnLoadProgress?.Invoke(LoadProgress);
                
                // When progress reaches 0.9, scene is ready
                if (asyncLoad.progress >= 0.9f)
                {
                    LoadProgress = 1f;
                    OnLoadProgress?.Invoke(LoadProgress);
                    
                    // Small delay for visual feedback
                    yield return new WaitForSeconds(0.2f);
                    
                    // Activate scene
                    asyncLoad.allowSceneActivation = true;
                }
                
                yield return null;
            }
            
            Debug.Log($"[SceneLoader] ✅ Scene loaded: {sceneName}");
            
            // Hide loading screen
            if (showLoadingScreen)
            {
                // TODO: Hide loading UI
                // LoadingScreen.Hide();
            }
            
            IsLoading = false;
            OnLoadComplete?.Invoke(sceneName);
        }
        
        #endregion
        
        #region Additive Loading
        
        /// <summary>
        /// Load a scene additively (doesn't unload current scene)
        /// </summary>
        public static void LoadSceneAdditive(SceneNames scene)
        {
            LoadSceneAdditive(scene.ToString());
        }
        
        /// <summary>
        /// Load a scene additively by name
        /// </summary>
        public static void LoadSceneAdditive(string sceneName)
        {
            Debug.Log($"[SceneLoader] 📦 Loading additive scene: {sceneName}");
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
        
        /// <summary>
        /// Unload an additively loaded scene
        /// </summary>
        public static void UnloadScene(SceneNames scene)
        {
            UnloadScene(scene.ToString());
        }
        
        /// <summary>
        /// Unload an additively loaded scene by name
        /// </summary>
        public static void UnloadScene(string sceneName)
        {
            Debug.Log($"[SceneLoader] 🗑️ Unloading scene: {sceneName}");
            SceneManager.UnloadSceneAsync(sceneName);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get the currently active scene name
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
        
        /// <summary>
        /// Get the currently active scene as SceneNames enum
        /// </summary>
        public static SceneNames? GetCurrentScene()
        {
            string sceneName = GetCurrentSceneName();
            if (Enum.TryParse(sceneName, out SceneNames scene))
            {
                return scene;
            }
            return null;
        }
        
        /// <summary>
        /// Check if a scene exists in build settings
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Reload the current scene
        /// </summary>
        public static void ReloadCurrentScene()
        {
            string currentScene = GetCurrentSceneName();
            Debug.Log($"[SceneLoader] 🔄 Reloading scene: {currentScene}");
            LoadScene(currentScene);
        }
        
        #endregion
    }
}
