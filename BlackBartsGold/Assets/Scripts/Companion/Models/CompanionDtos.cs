using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BlackBartsGold.Companion.Models
{
    [Serializable]
    public class CompanionApiEnvelope<T>
    {
        public bool success;
        public T data;
        public string error;
        public string details;
        public string timestamp;
    }

    [Serializable]
    public class CompanionQuickPromptDto
    {
        [JsonProperty("intent_type")]
        public string intentType;

        [JsonProperty("label")]
        public string label;

        [JsonProperty("short_label")]
        public string shortLabel;
    }

    [Serializable]
    public class CompanionReplyNowDto
    {
        [JsonProperty("message_id")]
        public string messageId;

        [JsonProperty("message_type")]
        public string messageType;

        [JsonProperty("message_text")]
        public string messageText;

        [JsonProperty("voice_text")]
        public string voiceText;

        [JsonProperty("priority")]
        public int priority;

        [JsonProperty("tap_action")]
        public string tapAction;

        [JsonProperty("expires_at")]
        public string expiresAt;
    }

    [Serializable]
    public class CompanionCandidateMessageDto
    {
        [JsonProperty("message_id")]
        public string messageId;

        [JsonProperty("trigger_type")]
        public string triggerType;

        [JsonProperty("trigger_value")]
        public object triggerValue;

        [JsonProperty("message_text")]
        public string messageText;

        [JsonProperty("voice_text")]
        public string voiceText;

        [JsonProperty("priority")]
        public int priority;
    }

    [Serializable]
    public class CompanionMetaDto
    {
        [JsonProperty("risk_level")]
        public string riskLevel;

        [JsonProperty("recommended_action")]
        public string recommendedAction;

        [JsonProperty("selected_coin_id")]
        public string selectedCoinId;
    }

    [Serializable]
    public class CompanionResponsePackDto
    {
        [JsonProperty("reply_now")]
        public CompanionReplyNowDto replyNow;

        [JsonProperty("candidate_messages")]
        public List<CompanionCandidateMessageDto> candidateMessages = new List<CompanionCandidateMessageDto>();

        [JsonProperty("meta")]
        public CompanionMetaDto meta;
    }

    [Serializable]
    public class StartCompanionSessionRequest
    {
        public string action = "start_session";
        public string selectedCoinId;
        public double? latitude;
        public double? longitude;
        public string currentZoneId;
        public string currentCellL17;
    }

    [Serializable]
    public class SubmitCompanionIntentRequest
    {
        public string action = "submit_intent";
        public string companionSessionId;
        public string intentType;
        public string selectedCoinId;
        public float? distanceToCoinMeters;
        public double? latitude;
        public double? longitude;
        public string currentZoneId;
        public string currentCellL17;
    }

    [Serializable]
    public class ReportCompanionEventRequest
    {
        public string action = "report_event";
        public string companionSessionId;
        public string eventType;
        public string messageId;
        public string coinId;
        public float? distanceToCoinMeters;
        public double? latitude;
        public double? longitude;
        public string currentZoneId;
        public string currentCellL17;
        public Dictionary<string, object> payload;
    }

    [Serializable]
    public class CompanionSessionDataDto : CompanionResponsePackDto
    {
        [JsonProperty("companion_session_id")]
        public string companionSessionId;

        [JsonProperty("quick_prompts")]
        public List<CompanionQuickPromptDto> quickPrompts = new List<CompanionQuickPromptDto>();
    }

    [Serializable]
    public class CompanionIntentResponseDataDto : CompanionResponsePackDto
    {
        [JsonProperty("companion_session_id")]
        public string companionSessionId;
    }

    [Serializable]
    public class CompanionReportEventDataDto
    {
        [JsonProperty("acknowledged")]
        public bool acknowledged;

        [JsonProperty("companion_session_id")]
        public string companionSessionId;

        [JsonProperty("response_pack")]
        public CompanionResponsePackDto responsePack;
    }
}
