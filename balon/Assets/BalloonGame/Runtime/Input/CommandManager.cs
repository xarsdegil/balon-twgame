using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BalloonGame
{
    public class CommandManager : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;
        public TwitchChatService twitchChatService;
        public BalloonGameManager gameManager;
        public LocalTestInput localTestInput;

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

            if (TryHandleOwnerTestCommand(player, message))
                return;

            if (!MatchesCommand(message, settings.command))
                return;

            TrySubmit(player);
        }

        private bool TryHandleOwnerTestCommand(ChatPlayer player, string message)
        {
            bool addFive = MatchesCommand(message, settings.ownerAddFiveCommand);
            bool popNext = MatchesCommand(message, settings.ownerPopNextCommand);
            if (!addFive && !popNext)
                return false;

            if (!settings.enableOwnerTestCommands || !IsAuthenticatedOwner(player))
                return true;

            if (addFive && localTestInput != null)
                localTestInput.AddFiveTestPlayers();
            else if (popNext && gameManager != null)
                gameManager.ForceExplosionOnNextTurn();
            return true;
        }

        private static bool IsAuthenticatedOwner(ChatPlayer player)
        {
            TwitchAuthSession session = TwitchAuthService.Instance != null
                ? TwitchAuthService.Instance.Session
                : null;
            return player != null && session != null &&
                   !string.IsNullOrWhiteSpace(player.userId) &&
                   !string.IsNullOrWhiteSpace(session.userId) &&
                   string.Equals(player.userId, session.userId, StringComparison.Ordinal);
        }

        public bool TrySubmit(ChatPlayer player)
        {
            if (settings == null || player == null)
                return false;

            float now = Time.unscaledTime;
            if (settings.oneTurnPerUserPerRound &&
                (playedThisRound.Contains(player.userId) || playerQueue.Contains(player.userId)))
                return Reject(settings.language.alreadyPlayedThisRound);
            if (lastCommandTimes.TryGetValue(player.userId, out float lastTime) &&
                now - lastTime < settings.userCooldownSeconds)
                return Reject(settings.language.playerOnCooldown);
            if (!playerQueue.TryEnqueue(player, settings.maximumQueueLength))
                return Reject(settings.language.queueFull);

            lastCommandTimes[player.userId] = now;
            QueueChanged?.Invoke();
            return true;
        }

        private static bool MatchesCommand(string message, string configuredCommand)
        {
            string normalized = RemoveInvisibleCharacters(message).Trim();
            string command = (configuredCommand ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(command))
                return false;
            if (normalized.Equals(command, StringComparison.OrdinalIgnoreCase))
                return true;
            return normalized.Length > command.Length &&
                   normalized.StartsWith(command, StringComparison.OrdinalIgnoreCase) &&
                   char.IsWhiteSpace(normalized[command.Length]);
        }

        private static string RemoveInvisibleCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
            foreach (char character in value)
            {
                UnicodeCategory category = char.GetUnicodeCategory(character);
                if (category != UnicodeCategory.Format)
                    builder.Append(character);
            }
            return builder.ToString();
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
