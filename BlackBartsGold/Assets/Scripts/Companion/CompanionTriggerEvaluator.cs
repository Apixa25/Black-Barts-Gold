using System;
using System.Collections.Generic;
using BlackBartsGold.Companion.Models;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Evaluates locally-triggered Black Bart follow-up lines without another round-trip.
    /// </summary>
    public class CompanionTriggerEvaluator
    {
        private readonly List<CompanionCandidateMessageDto> _pendingCandidates = new List<CompanionCandidateMessageDto>();
        private readonly HashSet<string> _consumedMessageIds = new HashSet<string>();

        public void ReplaceCandidates(List<CompanionCandidateMessageDto> candidates)
        {
            _pendingCandidates.Clear();
            _consumedMessageIds.Clear();

            if (candidates == null) return;

            foreach (var candidate in candidates)
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.messageId))
                {
                    continue;
                }

                _pendingCandidates.Add(candidate);
            }
        }

        public void Clear()
        {
            _pendingCandidates.Clear();
            _consumedMessageIds.Clear();
        }

        public List<CompanionCandidateMessageDto> ConsumeDistanceReadyMessages(float distanceMeters)
        {
            var ready = new List<CompanionCandidateMessageDto>();

            foreach (var candidate in _pendingCandidates)
            {
                if (candidate == null || candidate.triggerType != "distance_under_meters")
                {
                    continue;
                }

                if (_consumedMessageIds.Contains(candidate.messageId))
                {
                    continue;
                }

                float threshold = ConvertTriggerValueToFloat(candidate.triggerValue);
                if (threshold <= 0f) continue;

                if (distanceMeters <= threshold)
                {
                    _consumedMessageIds.Add(candidate.messageId);
                    ready.Add(candidate);
                }
            }

            ready.Sort((left, right) => right.priority.CompareTo(left.priority));
            return ready;
        }

        public List<CompanionCandidateMessageDto> ConsumeEventReadyMessages(string eventType)
        {
            var ready = new List<CompanionCandidateMessageDto>();

            foreach (var candidate in _pendingCandidates)
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.triggerType))
                {
                    continue;
                }

                if (_consumedMessageIds.Contains(candidate.messageId))
                {
                    continue;
                }

                if (!string.Equals(candidate.triggerType, eventType, StringComparison.Ordinal))
                {
                    continue;
                }

                _consumedMessageIds.Add(candidate.messageId);
                ready.Add(candidate);
            }

            ready.Sort((left, right) => right.priority.CompareTo(left.priority));
            return ready;
        }

        private float ConvertTriggerValueToFloat(object triggerValue)
        {
            if (triggerValue == null) return 0f;

            if (triggerValue is double doubleValue) return (float)doubleValue;
            if (triggerValue is float floatValue) return floatValue;
            if (triggerValue is long longValue) return longValue;
            if (triggerValue is int intValue) return intValue;

            return float.TryParse(triggerValue.ToString(), out var parsed) ? parsed : 0f;
        }
    }
}
