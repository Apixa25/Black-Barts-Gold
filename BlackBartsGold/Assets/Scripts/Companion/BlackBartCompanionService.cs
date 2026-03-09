using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlackBartsGold.AR;
using BlackBartsGold.Companion.Models;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.UI;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Orchestrates Black Bart's session, prompt requests, local trigger playback, and audit events.
    /// </summary>
    public class BlackBartCompanionService : MonoBehaviour
    {
        private static BlackBartCompanionService _instance;

        public static BlackBartCompanionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BlackBartCompanionService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("BlackBartCompanionService");
                        _instance = go.AddComponent<BlackBartCompanionService>();
                    }
                }

                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        public static BlackBartCompanionService EnsureInstance()
        {
            return Instance;
        }

        public bool IsSessionActive => !string.IsNullOrEmpty(_companionSessionId);
        public IReadOnlyList<CompanionQuickPromptDto> QuickPrompts => _quickPrompts;

        public event Action<List<CompanionQuickPromptDto>> OnQuickPromptsUpdated;

        private readonly CompanionTriggerEvaluator _triggerEvaluator = new CompanionTriggerEvaluator();
        private readonly CompanionMessagePresenter _presenter = new CompanionMessagePresenter();
        private readonly List<CompanionQuickPromptDto> _quickPrompts = new List<CompanionQuickPromptDto>();

        private string _companionSessionId;
        private string _lastSelectedCoinId;
        private bool _bindingsActive;
        private bool _bootstrapInFlight;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            CompanionApiService.Instance.OnApiError += HandleApiError;

            if (SceneManager.GetActiveScene().name == "ARHunt")
            {
                StartCoroutine(BootstrapForArHunt());
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (CompanionApiService.Exists)
            {
                CompanionApiService.Instance.OnApiError -= HandleApiError;
            }

            UnbindGameplayEvents();
        }

        public async void SubmitIntent(string intentType)
        {
            await SubmitIntentAsync(intentType);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "ARHunt")
            {
                StartCoroutine(BootstrapForArHunt());
                return;
            }

            ClearSessionState();
            UnbindGameplayEvents();
        }

        private IEnumerator BootstrapForArHunt()
        {
            if (_bootstrapInFlight)
            {
                yield break;
            }

            _bootstrapInFlight = true;

            float timeoutAt = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                if (ARHUD.Instance != null && CoinManager.Exists && ProximityManager.Instance != null)
                {
                    break;
                }

                yield return null;
            }

            BindGameplayEvents();
            _bootstrapInFlight = false;

            var currentTarget = CoinManager.Exists ? CoinManager.Instance.TargetCoinData : null;
            _lastSelectedCoinId = currentTarget != null ? currentTarget.id : null;

            _ = StartSessionAsync(forceRestart: true);
        }

        private void BindGameplayEvents()
        {
            if (_bindingsActive) return;
            if (!CoinManager.Exists || ProximityManager.Instance == null) return;

            CoinManager.Instance.OnTargetSet += HandleTargetSet;
            CoinManager.Instance.OnTargetCleared += HandleTargetCleared;
            CoinManager.Instance.OnCollectionReported += HandleCollectionReported;
            ProximityManager.Instance.OnDistanceUpdated += HandleDistanceUpdated;
            _bindingsActive = true;
        }

        private void UnbindGameplayEvents()
        {
            if (!_bindingsActive) return;

            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= HandleTargetSet;
                CoinManager.Instance.OnTargetCleared -= HandleTargetCleared;
                CoinManager.Instance.OnCollectionReported -= HandleCollectionReported;
            }

            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnDistanceUpdated -= HandleDistanceUpdated;
            }

            _bindingsActive = false;
        }

        private async Task StartSessionAsync(bool forceRestart)
        {
            if (!forceRestart && IsSessionActive)
            {
                return;
            }

            var request = BuildStartRequest();
            try
            {
                var envelope = await CompanionApiService.Instance.StartSession(request);
                if (envelope == null || !envelope.success || envelope.data == null)
                {
                    return;
                }

                _companionSessionId = envelope.data.companionSessionId;
                ReplaceQuickPrompts(envelope.data.quickPrompts);
                _triggerEvaluator.ReplaceCandidates(envelope.data.candidateMessages);

                if (_presenter.PresentReply(envelope.data.replyNow))
                {
                    _ = ReportEventAsync("message_shown", envelope.data.replyNow?.messageId, request.selectedCoinId, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlackBartCompanionService] Failed to start session: {ex.Message}");
            }
        }

        private async Task SubmitIntentAsync(string intentType)
        {
            if (string.IsNullOrEmpty(intentType))
            {
                return;
            }

            if (!IsSessionActive)
            {
                await StartSessionAsync(forceRestart: false);
            }

            if (!IsSessionActive)
            {
                return;
            }

            var request = BuildIntentRequest(intentType);
            try
            {
                var envelope = await CompanionApiService.Instance.SubmitIntent(request);
                if (envelope == null || !envelope.success || envelope.data == null)
                {
                    return;
                }

                _triggerEvaluator.ReplaceCandidates(envelope.data.candidateMessages);

                if (_presenter.PresentReply(envelope.data.replyNow))
                {
                    _ = ReportEventAsync("message_shown", envelope.data.replyNow?.messageId, request.selectedCoinId, request.distanceToCoinMeters);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlackBartCompanionService] Failed to submit intent: {ex.Message}");
            }
        }

        private async Task ReportEventAsync(string eventType, string messageId, string coinId, float? distanceToCoinMeters, Dictionary<string, object> payload = null)
        {
            if (!IsSessionActive)
            {
                return;
            }

            try
            {
                var request = BuildEventRequest(eventType, messageId, coinId, distanceToCoinMeters, payload);
                var envelope = await CompanionApiService.Instance.ReportEvent(request);
                if (envelope == null || !envelope.success || envelope.data == null || envelope.data.responsePack == null)
                {
                    return;
                }

                _triggerEvaluator.ReplaceCandidates(envelope.data.responsePack.candidateMessages);

                if (_presenter.PresentReply(envelope.data.responsePack.replyNow))
                {
                    _ = ReportEventAsync("message_shown", envelope.data.responsePack.replyNow?.messageId, coinId, distanceToCoinMeters);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlackBartCompanionService] Failed to report event {eventType}: {ex.Message}");
            }
        }

        private void HandleTargetSet(Coin coin)
        {
            string selectedCoinId = coin != null ? coin.id : null;
            if (selectedCoinId == _lastSelectedCoinId)
            {
                return;
            }

            _lastSelectedCoinId = selectedCoinId;
            _triggerEvaluator.Clear();
            _ = ReportEventAsync("selected_coin_changed", null, selectedCoinId, coin != null ? coin.distanceFromPlayer : null);
        }

        private void HandleTargetCleared()
        {
            _lastSelectedCoinId = null;
            _triggerEvaluator.Clear();
        }

        private void HandleDistanceUpdated(float distance, float bearing)
        {
            if (!IsSessionActive || string.IsNullOrEmpty(_lastSelectedCoinId))
            {
                return;
            }

            var readyMessages = _triggerEvaluator.ConsumeDistanceReadyMessages(distance);
            foreach (var candidate in readyMessages)
            {
                if (_presenter.PresentCandidate(candidate))
                {
                    _ = ReportEventAsync("message_shown", candidate.messageId, _lastSelectedCoinId, distance);
                }
            }
        }

        private void HandleCollectionReported(string coinId, float value, bool success)
        {
            if (!IsSessionActive)
            {
                return;
            }

            string eventType = success ? "coin_collected_success" : "coin_collection_failed";
            var readyMessages = _triggerEvaluator.ConsumeEventReadyMessages(success ? "coin_collected_success" : "coin_collection_failed");
            foreach (var candidate in readyMessages)
            {
                if (_presenter.PresentCandidate(candidate))
                {
                    _ = ReportEventAsync("message_shown", candidate.messageId, coinId, 0f);
                }
            }

            var payload = new Dictionary<string, object>
            {
                { "reportedValue", value },
                { "success", success },
            };

            _ = ReportEventAsync(eventType, null, coinId, 0f, payload);
        }

        private void HandleApiError(string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
            {
                return;
            }

            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.ShowMessage($"Black Bart: {userMessage}", 3.5f);
            }
        }

        private void ReplaceQuickPrompts(List<CompanionQuickPromptDto> prompts)
        {
            _quickPrompts.Clear();
            if (prompts != null)
            {
                _quickPrompts.AddRange(prompts);
            }

            OnQuickPromptsUpdated?.Invoke(new List<CompanionQuickPromptDto>(_quickPrompts));
        }

        private void ClearSessionState()
        {
            _companionSessionId = null;
            _lastSelectedCoinId = null;
            _triggerEvaluator.Clear();
            ReplaceQuickPrompts(null);
        }

        private StartCompanionSessionRequest BuildStartRequest()
        {
            TryGetLocationSnapshot(out var latitude, out var longitude);

            return new StartCompanionSessionRequest
            {
                selectedCoinId = GetSelectedCoinId(),
                latitude = latitude,
                longitude = longitude,
                currentZoneId = null,
                currentCellL17 = null,
            };
        }

        private SubmitCompanionIntentRequest BuildIntentRequest(string intentType)
        {
            TryGetLocationSnapshot(out var latitude, out var longitude);

            return new SubmitCompanionIntentRequest
            {
                companionSessionId = _companionSessionId,
                intentType = intentType,
                selectedCoinId = GetSelectedCoinId(),
                distanceToCoinMeters = GetSelectedCoinDistance(),
                latitude = latitude,
                longitude = longitude,
                currentZoneId = null,
                currentCellL17 = null,
            };
        }

        private ReportCompanionEventRequest BuildEventRequest(
            string eventType,
            string messageId,
            string coinId,
            float? distanceToCoinMeters,
            Dictionary<string, object> payload)
        {
            TryGetLocationSnapshot(out var latitude, out var longitude);

            return new ReportCompanionEventRequest
            {
                companionSessionId = _companionSessionId,
                eventType = eventType,
                messageId = messageId,
                coinId = coinId,
                distanceToCoinMeters = distanceToCoinMeters,
                latitude = latitude,
                longitude = longitude,
                currentZoneId = null,
                currentCellL17 = null,
                payload = payload,
            };
        }

        private string GetSelectedCoinId()
        {
            if (!CoinManager.Exists || CoinManager.Instance.TargetCoinData == null)
            {
                return null;
            }

            return CoinManager.Instance.TargetCoinData.id;
        }

        private float? GetSelectedCoinDistance()
        {
            if (!CoinManager.Exists)
            {
                return null;
            }

            if (CoinManager.Instance.TargetCoin != null)
            {
                return CoinManager.Instance.TargetCoin.DistanceFromPlayer;
            }

            if (CoinManager.Instance.TargetCoinData != null)
            {
                return CoinManager.Instance.TargetCoinData.distanceFromPlayer;
            }

            return null;
        }

        private bool TryGetLocationSnapshot(out double? latitude, out double? longitude)
        {
            latitude = null;
            longitude = null;

            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                latitude = GPSManager.Instance.CurrentLocation.latitude;
                longitude = GPSManager.Instance.CurrentLocation.longitude;
                return true;
            }

            return false;
        }
    }
}
