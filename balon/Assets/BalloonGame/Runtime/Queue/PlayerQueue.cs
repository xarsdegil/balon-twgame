using System.Collections.Generic;
using System.Linq;

namespace BalloonGame
{
    public sealed class PlayerQueue
    {
        private readonly Queue<ChatPlayer> players = new Queue<ChatPlayer>();
        private readonly HashSet<string> queuedIds = new HashSet<string>();

        public int Count => players.Count;
        public bool IsEmpty => players.Count == 0;

        public bool TryEnqueue(ChatPlayer player, int maximumLength)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.userId))
                return false;
            if (players.Count >= maximumLength || queuedIds.Contains(player.userId))
                return false;

            players.Enqueue(player);
            queuedIds.Add(player.userId);
            return true;
        }

        public bool TryDequeue(out ChatPlayer player)
        {
            if (players.Count == 0)
            {
                player = null;
                return false;
            }

            player = players.Dequeue();
            queuedIds.Remove(player.userId);
            return true;
        }

        public bool Contains(string userId) => queuedIds.Contains(userId);

        public IReadOnlyList<ChatPlayer> Snapshot(int count = 6)
        {
            return players.Take(count).ToArray();
        }

        public void Clear()
        {
            players.Clear();
            queuedIds.Clear();
        }
    }
}

