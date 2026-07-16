using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalloonGame
{
    public class CommandManager : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;
        public TwitchChatService twitchChatService;

        public event Action QueueChanged;
        public event Action<string> CommandRejected;

        private readonly PlayerQueue playerQueue = new PlayerQueue();
        private readonly HashSet<string> playedThisRound = new HashSet<string>();
        private readonly Dictionary<string, float> lastCommandTimes = new Dictionary<string, float>();

        public int QueueCount => playerQueue.Count;

        private void OnEnable()
        {
            if (twitchChatService != null)
                twitchChatService.MessageReceived += OnChatMessage;
        }

        private void OnDisable()
        {
            if (twitchChatService != null)
                twitchChatService.MessageReceived -= OnChatMessage;
        }

        private void OnChatMessage(ChatPlayer player, string message)
        {
            if (settings == null)
                return;

            string normalized = (message ?? string.Empty).Trim();
            if (!normalized.Equals(settings.command.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            TrySubmit(player);
        }

        public bool TrySubmit(ChatPlayer player)
        {
            if (settings == null || player == null)
                return false;

            float now = Time.unscaledTime;
            if (playerQueue.Contains(player.userId))
                return Reject("Bu kullanıcı zaten sırada.");
            if (settings.oneTurnPerUserPerRound && playedThisRound.Contains(player.userId))
                return Reject("Bu kullanıcı bu turda oynadı.");
            if (lastCommandTimes.TryGetValue(player.userId, out float lastTime) &&
                now - lastTime < settings.userCooldownSeconds)
                return Reject("Kullanıcı bekleme süresinde.");
            if (!playerQueue.TryEnqueue(player, settings.maximumQueueLength))
                return Reject("Kuyruk dolu.");

            lastCommandTimes[player.userId] = now;
            QueueChanged?.Invoke();
            return true;
        }

        public bool TryGetNextPlayer(out ChatPlayer player)
        {
            bool success = playerQueue.TryDequeue(out player);
            if (!success)
                return false;

            playedThisRound.Add(player.userId);
            QueueChanged?.Invoke();
            return true;
        }

        public IReadOnlyList<ChatPlayer> GetQueuePreview(int count = 6)
        {
            return playerQueue.Snapshot(count);
        }

        public void BeginNewRound(bool clearQueue)
        {
            playedThisRound.Clear();
            if (clearQueue)
                playerQueue.Clear();
            QueueChanged?.Invoke();
        }

        private bool Reject(string reason)
        {
            CommandRejected?.Invoke(reason);
            return false;
        }
    }
}

