// ============================================================================
// GameManager.cs
// Black Bart's Gold - Core Game Manager
// Path: Assets/Scripts/Core/GameManager.cs
// ============================================================================
// Singleton that persists across all scenes. Manages game state, scene 
// transitions, and provides centralized access to core systems.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Central game manager - Singleton pattern with DontDestroyOnLoad.
    /// Manages game state, scene flow, and provides access to core systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        
        private static GameManager _instance;
        
        /// <summary>
        /// Singleton instance accessor. Creates instance if none exists.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance
                    _instance = FindFirstObjectByType<GameManager>();
                    
                    if (_instance == null)
                    {
                        // Create new instance
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        Debug.Log("[GameManager] 🏴‍☠️ Created new GameManager instance");
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Game State
        
        /// <summary>
        /// Current state of the game
        /// </summary>
        public GameState CurrentGameState { get; private set; } = GameState.Initializing;
        
        /// <summary>
        /// Is the game currently paused?
        /// </summary>
        public bool IsPaused { get; private set; } = false;
        
        /// <summary>
        /// Is the player currently authenticated?
        /// </summary>
        public bool IsAuthenticated { get; private set; } = false;
        
        /// <summary>
        /// Current scene name
        /// </summary>
        public SceneNames CurrentScene { get; private set; } = SceneNames.MainMenu;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when game state changes
        /// </summary>
        public event Action<GameState> OnGameStateChanged;
        
        /// <summary>
        /// Fired when scene transition starts
        /// </summary>
        public event Action<SceneNames> OnSceneTransitionStarted;
        
        /// <summary>
        /// Fired when scene transition completes
        /// </summary>
        public event Action<SceneNames> OnSceneTransitionCompleted;
        
        /// <summary>
        /// Fired when game is paused/resumed
        /// </summary>
        public event Action<bool> OnPauseStateChanged;
        
        /// <summary>
        /// Fired when authentication state changes
        /// </summary>
        public event Action<bool> OnAuthStateChanged;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[GameManager] 🏴‍☠️ Black Bart's Gold - GameManager initialized!");
            
            // Initialize game
            Initialize();
        }
        
        private void OnEnable()
        {
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from scene loading events
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background - save data
                Debug.Log("[GameManager] App paused - saving data...");
                SaveGameData();
            }
            else
            {
                // App coming to foreground
                Debug.Log("[GameManager] App resumed");
            }
        }
        
        private void OnApplicationQuit()
        {
            Debug.Log("[GameManager] App quitting - saving data...");
            SaveGameData();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the game manager and load saved data
        /// </summary>
        private void Initialize()
        {
            SetGameState(GameState.Initializing);
            
            // Load saved data (will be implemented in SaveSystem)
            LoadGameData();
            
            // Check authentication status
            CheckAuthenticationStatus();
            
            // Set to ready state
            SetGameState(GameState.Ready);
        }
        
        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        private void CheckAuthenticationStatus()
        {
            // Check with AuthService if available
            if (AuthService.Exists && AuthService.Instance.IsLoggedIn)
            {
                IsAuthenticated = true;
                Debug.Log("[GameManager] ✅ User authenticated via AuthService");
                return;
            }
            
            // Fallback: check if we have a saved session token
            string token = PlayerPrefs.GetString("auth_token", "");
            IsAuthenticated = !string.IsNullOrEmpty(token);
            
            Debug.Log($"[GameManager] Authentication status: {IsAuthenticated}");
        }
        
        /// <summary>
        /// Perform startup authentication check
        /// Called from a loading scene or initial scene
        /// </summary>
        public async void PerformStartupAuthCheck()
        {
            Debug.Log("[GameManager] 🔄 Performing startup auth check...");
            SetGameState(GameState.Loading);
            
            // Use SessionManager if available
            if (SessionManager.Exists)
            {
                var result = await SessionManager.Instance.CheckSessionOnStartup();
                
                switch (result)
                {
                    case SessionCheckResult.ValidSession:
                        Debug.Log("[GameManager] ✅ Valid session found");
                        IsAuthenticated = true;
                        SetGameState(GameState.Ready);
                        OnAuthStateChanged?.Invoke(true);
                        LoadScene(SceneNames.MainMenu);
                        break;
                        
                    case SessionCheckResult.ExpiredSession:
                        Debug.Log("[GameManager] ⚠️ Session expired");
                        IsAuthenticated = false;
                        SetGameState(GameState.Ready);
                        OnAuthStateChanged?.Invoke(false);
                        LoadScene(SceneNames.Login);
                        break;
                        
                    case SessionCheckResult.NoSession:
                    default:
                        Debug.Log("[GameManager] 📝 No session - show onboarding");
                        IsAuthenticated = false;
                        SetGameState(GameState.Ready);
                        OnAuthStateChanged?.Invoke(false);
                        LoadScene(SceneNames.Onboarding);
                        break;
                }
            }
            else
            {
                // Fallback without SessionManager
                CheckAuthenticationStatus();
                SetGameState(GameState.Ready);
                
                if (IsAuthenticated)
                {
                    LoadScene(SceneNames.MainMenu);
                }
                else
                {
                    LoadScene(SceneNames.Onboarding);
                }
            }
        }
        
        /// <summary>
        /// Get the appropriate start scene based on auth state
        /// </summary>
        public SceneNames GetStartScene()
        {
            if (SessionManager.Exists)
            {
                return SessionManager.Instance.GetStartScene();
            }
            
            if (IsAuthenticated)
            {
                return SceneNames.MainMenu;
            }
            
            // Check if first launch
            if (PlayerPrefs.GetInt("has_launched_before", 0) == 0)
            {
                return SceneNames.Onboarding;
            }
            
            return SceneNames.Login;
        }
        
        #endregion
        
        #region Game State Management
        
        /// <summary>
        /// Set the current game state
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            
            GameState oldState = CurrentGameState;
            CurrentGameState = newState;
            
            Debug.Log($"[GameManager] State changed: {oldState} → {newState}");
            OnGameStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused) return;
            
            IsPaused = true;
            Time.timeScale = 0f;
            
            Debug.Log("[GameManager] Game paused");
            OnPauseStateChanged?.Invoke(true);
        }
        
        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused) return;
            
            IsPaused = false;
            Time.timeScale = 1f;
            
            Debug.Log("[GameManager] Game resumed");
            OnPauseStateChanged?.Invoke(false);
        }
        
        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }
        
        #endregion
        
        #region Authentication
        
        /// <summary>
        /// Set authenticated state (called by AuthService)
        /// </summary>
        public void SetAuthenticated(bool authenticated)
        {
            if (IsAuthenticated == authenticated) return;
            
            IsAuthenticated = authenticated;
            Debug.Log($"[GameManager] Auth state changed: {authenticated}");
            OnAuthStateChanged?.Invoke(authenticated);
        }
        
        #endregion
        
        #region Scene Management
        
        /// <summary>
        /// Load a scene by name enum
        /// </summary>
        public void LoadScene(SceneNames scene)
        {
            Debug.Log($"[GameManager] 🗺️ Loading scene: {scene}");
            
            OnSceneTransitionStarted?.Invoke(scene);
            CurrentScene = scene;
            
            SceneLoader.LoadScene(scene);
        }
        
        /// <summary>
        /// Load a protected scene (requires authentication)
        /// </summary>
        public void LoadProtectedScene(SceneNames scene)
        {
            if (!IsAuthenticated)
            {
                Debug.LogWarning($"[GameManager] Cannot load protected scene {scene} - not authenticated");
                LoadScene(SceneNames.Login);
                return;
            }
            
            LoadScene(scene);
        }
        
        /// <summary>
        /// Called when a scene finishes loading
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name}");
            
            // Try to parse scene name to enum
            if (Enum.TryParse(scene.name, out SceneNames loadedScene))
            {
                CurrentScene = loadedScene;
                OnSceneTransitionCompleted?.Invoke(loadedScene);
            }
        }
        
        #endregion
        
        #region Data Persistence
        
        /// <summary>
        /// Save all game data
        /// </summary>
        public void SaveGameData()
        {
            // TODO: Implement with SaveSystem
            Debug.Log("[GameManager] 💾 Saving game data...");
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load all game data
        /// </summary>
        public void LoadGameData()
        {
            // TODO: Implement with SaveSystem
            Debug.Log("[GameManager] 📂 Loading game data...");
        }
        
        /// <summary>
        /// Clear all saved data (for logout)
        /// </summary>
        public void ClearAllData()
        {
            Debug.Log("[GameManager] 🗑️ Clearing all saved data...");
            PlayerPrefs.DeleteAll();
            IsAuthenticated = false;
            OnAuthStateChanged?.Invoke(false);
        }
        
        #endregion
        
        #region Quick Access Methods
        
        /// <summary>
        /// Go to main menu
        /// </summary>
        public void GoToMainMenu()
        {
            LoadProtectedScene(SceneNames.MainMenu);
        }
        
        /// <summary>
        /// Start AR treasure hunting
        /// </summary>
        public void StartHunting()
        {
            // TODO: Check gas before allowing hunting
            LoadProtectedScene(SceneNames.ARHunt);
        }
        
        /// <summary>
        /// Open wallet screen
        /// </summary>
        public void OpenWallet()
        {
            LoadProtectedScene(SceneNames.Wallet);
        }
        
        /// <summary>
        /// Open map screen
        /// </summary>
        public void OpenMap()
        {
            LoadProtectedScene(SceneNames.Map);
        }
        
        /// <summary>
        /// Logout and return to login screen
        /// </summary>
        public void Logout()
        {
            Debug.Log("[GameManager] 👋 Logging out...");
            ClearAllData();
            LoadScene(SceneNames.Login);
        }
        
        #endregion
    }
    
    #region Enums
    
    /// <summary>
    /// Game state enum
    /// </summary>
    public enum GameState
    {
        Initializing,
        Ready,
        Loading,
        Playing,
        Paused,
        Error
    }
    
    #endregion
}
