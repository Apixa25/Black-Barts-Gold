using UnityEngine;
using BlackBartsGold.Companion.Models;
using BlackBartsGold.UI;

namespace BlackBartsGold.Companion
{
    /// <summary>
    /// Sends companion lines into a dedicated overlay with fallback to the legacy ARHUD lane.
    /// </summary>
    public class CompanionMessagePresenter
    {
        private readonly float _cooldownSeconds;
        private readonly float _messageDurationSeconds;

        private float _lastShownAt = -999f;
        private int _lastPriority = -1;
        private string _lastMessageId;

        public CompanionMessagePresenter(float cooldownSeconds = 4f, float messageDurationSeconds = 60f)
        {
            _cooldownSeconds = cooldownSeconds;
            _messageDurationSeconds = messageDurationSeconds;
        }

        public bool PresentReply(CompanionReplyNowDto reply)
        {
            if (reply == null || string.IsNullOrEmpty(reply.messageText))
            {
                return false;
            }

            if (!ShouldShow(reply.messageId, reply.priority))
            {
                return false;
            }

            if (!ShowLine(reply.messageText))
            {
                return false;
            }

            RememberShown(reply.messageId, reply.priority);
            return true;
        }

        public bool PresentCandidate(CompanionCandidateMessageDto candidate)
        {
            if (candidate == null || string.IsNullOrEmpty(candidate.messageText))
            {
                return false;
            }

            if (!ShouldShow(candidate.messageId, candidate.priority))
            {
                return false;
            }

            if (!ShowLine(candidate.messageText))
            {
                return false;
            }

            RememberShown(candidate.messageId, candidate.priority);
            return true;
        }

        private bool ShouldShow(string messageId, int priority)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return false;
            }

            if (_lastMessageId == messageId)
            {
                return false;
            }

            float sinceLast = Time.unscaledTime - _lastShownAt;
            if (sinceLast < _cooldownSeconds && priority < _lastPriority)
            {
                return false;
            }

            return true;
        }

        private void RememberShown(string messageId, int priority)
        {
            _lastMessageId = messageId;
            _lastPriority = priority;
            _lastShownAt = Time.unscaledTime;
        }

        private string FormatForHud(string line)
        {
            return $"Black Bart: {line}";
        }

        private bool ShowLine(string line)
        {
            string formatted = FormatForHud(line);

            if (CompanionMessageOverlay.Exists)
            {
                CompanionMessageOverlay.Instance.ShowMessage(formatted, _messageDurationSeconds);
                return true;
            }

            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.ShowMessage(formatted, _messageDurationSeconds);
                return true;
            }

            Debug.LogWarning("[CompanionMessagePresenter] No companion overlay or ARHUD available.");
            return false;
        }
    }
}
