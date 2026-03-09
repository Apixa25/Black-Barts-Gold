using System;
using System.Threading.Tasks;
using UnityEngine;
using BlackBartsGold.Companion.Models;
using BlackBartsGold.Core;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Companion-specific API wrapper for Black Bart prompt and event traffic.
    /// </summary>
    public class CompanionApiService : MonoBehaviour
    {
        private static CompanionApiService _instance;

        public static CompanionApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CompanionApiService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CompanionApiService");
                        _instance = go.AddComponent<CompanionApiService>();
                    }
                }

                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        public event Action<string> OnApiError;

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

        public async Task<CompanionApiEnvelope<CompanionSessionDataDto>> StartSession(StartCompanionSessionRequest request)
        {
            return await Post<CompanionSessionDataDto>(request);
        }

        public async Task<CompanionApiEnvelope<CompanionIntentResponseDataDto>> SubmitIntent(SubmitCompanionIntentRequest request)
        {
            return await Post<CompanionIntentResponseDataDto>(request);
        }

        public async Task<CompanionApiEnvelope<CompanionReportEventDataDto>> ReportEvent(ReportCompanionEventRequest request)
        {
            return await Post<CompanionReportEventDataDto>(request);
        }

        private async Task<CompanionApiEnvelope<T>> Post<T>(object requestBody)
        {
            try
            {
                return await ApiClient.Instance.Post<CompanionApiEnvelope<T>>(ApiConfig.Player.COMPANION, requestBody);
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[CompanionApiService] API error: {ex.Message}");
                OnApiError?.Invoke(ex.UserMessage);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompanionApiService] Unexpected error: {ex.Message}");
                OnApiError?.Invoke("Black Bart lost the trail for a moment.");
                throw;
            }
        }
    }
}
