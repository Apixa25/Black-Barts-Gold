// ============================================================================
// OnboardingUI.cs
// Black Bart's Gold - Onboarding Screen Controller
// Path: Assets/Scripts/UI/OnboardingUI.cs
// ============================================================================
// Controls the first-launch onboarding experience. Introduces new users to
// the game with Black Bart branding and gameplay explanation.
// Reference: BUILD-GUIDE.md Sprint 6, Prompt 6.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Onboarding screen controller.
    /// First-time user welcome experience with game introduction.
    /// </summary>
    public class OnboardingUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Welcome Section")]
        [SerializeField]
        private TMP_Text titleText;
        
        [SerializeField]
        private TMP_Text subtitleText;
        
        [SerializeField]
        private TMP_Text welcomeMessageText;
        
        [SerializeField]
        private Image logoImage;
        
        [Header("Main Buttons")]
        [SerializeField]
        private Button loginButton;
        
        [SerializeField]
        private Button createAccountButton;
        
        [SerializeField]
        private Button skipButton;
        
        [Header("How It Works Section")]
        [SerializeField]
        private Button howItWorksButton;
        
        [SerializeField]
        private GameObject howItWorksPanel;
        
        [SerializeField]
        private Button closeHowItWorksButton;
        
        [Header("How It Works Content")]
        [SerializeField]
        private TMP_Text step1Text;
        
        [SerializeField]
        private TMP_Text step2Text;
        
        [SerializeField]
        private TMP_Text step3Text;
        
        [SerializeField]
        private TMP_Text step4Text;
        
        [Header("Features List")]
        [SerializeField]
        private GameObject[] featureItems;
        
        [SerializeField]
        private TMP_Text[] featureTexts;
        
        [Header("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.5f;
        
        [SerializeField]
        private float featureStaggerDelay = 0.2f;
        
        [SerializeField]
        private CanvasGroup mainCanvasGroup;
        
        [Header("Background")]
        [SerializeField]
        private Image backgroundImage;
        
        [SerializeField]
        private ParticleSystem sparkleParticles;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isHowItWorksVisible = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupUI();
            SetupListeners();
            HideHowItWorks();
            
            // Start entrance animation
            StartCoroutine(PlayEntranceAnimation());
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup UI initial state
        /// </summary>
        private void SetupUI()
        {
            // Set title and welcome text
            if (titleText != null)
            {
                titleText.text = "🏴‍☠️ Black Bart's Gold";
            }
            
            if (subtitleText != null)
            {
                subtitleText.text = "The AR Treasure Hunt";
            }
            
            if (welcomeMessageText != null)
            {
                welcomeMessageText.text = "Ahoy, matey! Discover real Bitcoin treasure hidden in the world around ye! " +
                    "Use yer phone's camera to hunt for virtual coins with real value!";
            }
            
            // Setup How It Works content
            SetupHowItWorksContent();
            
            // Setup feature list
            SetupFeatureList();
            
            // Hide main group initially for fade in
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
            }
        }
        
        /// <summary>
        /// Setup How It Works panel content
        /// </summary>
        private void SetupHowItWorksContent()
        {
            if (step1Text != null)
            {
                step1Text.text = "🗺️ <b>EXPLORE</b>\nWalk around your neighborhood to find treasure hotspots";
            }
            
            if (step2Text != null)
            {
                step2Text.text = "📱 <b>HUNT</b>\nUse AR camera to spot virtual coins in the real world";
            }
            
            if (step3Text != null)
            {
                step3Text.text = "💰 <b>COLLECT</b>\nWalk within range and tap to claim your treasure";
            }
            
            if (step4Text != null)
            {
                step4Text.text = "🏦 <b>PROFIT</b>\nCoins have real Bitcoin value you can keep or cash out!";
            }
        }
        
        /// <summary>
        /// Setup feature highlights
        /// </summary>
        private void SetupFeatureList()
        {
            string[] features = new string[]
            {
                "💎 Real Bitcoin value coins",
                "🗺️ GPS-based treasure maps",
                "📱 Augmented Reality hunting",
                "⚡ Daily finds & rewards",
                "🏆 Climb the leaderboards"
            };
            
            if (featureTexts != null)
            {
                for (int i = 0; i < featureTexts.Length && i < features.Length; i++)
                {
                    if (featureTexts[i] != null)
                    {
                        featureTexts[i].text = features[i];
                    }
                }
            }
        }
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }
            
            if (createAccountButton != null)
            {
                createAccountButton.onClick.AddListener(OnCreateAccountClicked);
            }
            
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }
            
            if (howItWorksButton != null)
            {
                howItWorksButton.onClick.AddListener(OnHowItWorksClicked);
            }
            
            if (closeHowItWorksButton != null)
            {
                closeHowItWorksButton.onClick.AddListener(OnCloseHowItWorksClicked);
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Handle login button click
        /// </summary>
        private void OnLoginClicked()
        {
            Log("Login clicked");
            MarkOnboardingSeen();
            SceneLoader.LoadScene(SceneNames.Login);
        }
        
        /// <summary>
        /// Handle create account button click
        /// </summary>
        private void OnCreateAccountClicked()
        {
            Log("Create account clicked");
            MarkOnboardingSeen();
            SceneLoader.LoadScene(SceneNames.Register);
        }
        
        /// <summary>
        /// Handle skip button click (guest mode stub)
        /// </summary>
        private void OnSkipClicked()
        {
            Log("Skip clicked");
            // TODO: Implement guest mode
            // For now, just go to login
            MarkOnboardingSeen();
            SceneLoader.LoadScene(SceneNames.Login);
        }
        
        /// <summary>
        /// Handle How It Works button click
        /// </summary>
        private void OnHowItWorksClicked()
        {
            Log("How it works clicked");
            ShowHowItWorks();
        }
        
        /// <summary>
        /// Handle close How It Works button click
        /// </summary>
        private void OnCloseHowItWorksClicked()
        {
            Log("Close how it works clicked");
            HideHowItWorks();
        }
        
        #endregion
        
        #region How It Works Panel
        
        /// <summary>
        /// Show How It Works panel
        /// </summary>
        private void ShowHowItWorks()
        {
            if (howItWorksPanel != null)
            {
                howItWorksPanel.SetActive(true);
                isHowItWorksVisible = true;
                
                // Animate panel in
                StartCoroutine(AnimateHowItWorksIn());
            }
        }
        
        /// <summary>
        /// Hide How It Works panel
        /// </summary>
        private void HideHowItWorks()
        {
            if (howItWorksPanel != null)
            {
                howItWorksPanel.SetActive(false);
                isHowItWorksVisible = false;
            }
        }
        
        /// <summary>
        /// Animate How It Works panel entrance
        /// </summary>
        private IEnumerator AnimateHowItWorksIn()
        {
            CanvasGroup panelGroup = howItWorksPanel?.GetComponent<CanvasGroup>();
            
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    panelGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                
                panelGroup.alpha = 1f;
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Play entrance animation
        /// </summary>
        private IEnumerator PlayEntranceAnimation()
        {
            // Fade in main canvas
            if (mainCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    mainCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                mainCanvasGroup.alpha = 1f;
            }
            
            // Stagger feature items
            if (featureItems != null)
            {
                foreach (var item in featureItems)
                {
                    if (item != null)
                    {
                        item.SetActive(false);
                    }
                }
                
                yield return new WaitForSeconds(0.3f);
                
                foreach (var item in featureItems)
                {
                    if (item != null)
                    {
                        item.SetActive(true);
                        StartCoroutine(AnimateFeatureItem(item));
                        yield return new WaitForSeconds(featureStaggerDelay);
                    }
                }
            }
            
            // Start sparkle particles
            if (sparkleParticles != null)
            {
                sparkleParticles.Play();
            }
        }
        
        /// <summary>
        /// Animate individual feature item
        /// </summary>
        private IEnumerator AnimateFeatureItem(GameObject item)
        {
            RectTransform rect = item.GetComponent<RectTransform>();
            CanvasGroup group = item.GetComponent<CanvasGroup>();
            
            if (group == null)
            {
                group = item.AddComponent<CanvasGroup>();
            }
            
            // Start state
            group.alpha = 0f;
            Vector3 startPos = rect.localPosition;
            rect.localPosition = startPos + Vector3.left * 50f;
            
            // Animate in
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease out
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                
                group.alpha = easedT;
                rect.localPosition = Vector3.Lerp(startPos + Vector3.left * 50f, startPos, easedT);
                
                yield return null;
            }
            
            group.alpha = 1f;
            rect.localPosition = startPos;
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Mark that user has seen onboarding
        /// </summary>
        private void MarkOnboardingSeen()
        {
            if (SessionManager.Exists)
            {
                SessionManager.Instance.MarkAppLaunched();
            }
            else
            {
                PlayerPrefs.SetInt("has_launched_before", 1);
                PlayerPrefs.Save();
            }
            
            Log("Onboarding marked as seen");
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[OnboardingUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Reset onboarding state
        /// </summary>
        [ContextMenu("Debug: Reset Onboarding")]
        public void DebugResetOnboarding()
        {
            PlayerPrefs.DeleteKey("has_launched_before");
            PlayerPrefs.Save();
            Debug.Log("[OnboardingUI] Onboarding state reset");
        }
        
        /// <summary>
        /// Debug: Replay entrance animation
        /// </summary>
        [ContextMenu("Debug: Replay Animation")]
        public void DebugReplayAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(PlayEntranceAnimation());
        }
        
        #endregion
    }
}
