using TMPro;
using UnityEngine;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Dedicated companion message lane so Black Bart lines do not fight with general ARHUD status text.
    /// </summary>
    public class CompanionMessageOverlay : MonoBehaviour
    {
        private static CompanionMessageOverlay _instance;

        public static CompanionMessageOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CompanionMessageOverlay>();
                }

                return _instance;
            }
        }

        public static bool Exists => Instance != null;

        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _messageText;
        private float _messageTimer;
        private bool _isShowing;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolveReferences();
            HideMessage();
        }

        private void Update()
        {
            if (!_isShowing)
            {
                return;
            }

            _messageTimer -= Time.unscaledDeltaTime;
            if (_messageTimer <= 0f)
            {
                HideMessage();
            }
        }

        public void ShowMessage(string message, float durationSeconds)
        {
            ResolveReferences();
            if (_messageText == null)
            {
                Debug.LogWarning("[CompanionMessageOverlay] Message text missing.");
                return;
            }

            _messageText.text = message;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            _messageTimer = Mathf.Max(0.5f, durationSeconds);
            _isShowing = true;
            Debug.Log($"[BBG][CompanionOverlay] Message: {message}");
        }

        public void HideMessage()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            _isShowing = false;
            _messageTimer = 0f;
        }

        private void ResolveReferences()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_messageText == null)
            {
                var textTransform = transform.Find("CompanionMessageText");
                if (textTransform != null)
                {
                    _messageText = textTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }
}
