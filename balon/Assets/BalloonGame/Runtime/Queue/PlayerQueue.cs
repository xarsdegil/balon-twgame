using System.Collections.Generic;
using System.Linq;

namespace BalloonGame
{
    public sealed class PlayerQueue
    {
        private readonly Queue<ChatPlayer> players = new Queue<ChatPlayer>();

        public int Count => players.Count;
        public bool IsEmpty => players.Count == 0;

        public bool TryEnqueue(ChatPlayer player, int maximumLength)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.userId))
                return false;
            if (players.Count >= maximumLength)
                return false;

            players.Enqueue(player);
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
            return true;
        }

        public bool Contains(string userId) =>
            players.Any(player => player.userId == userId);

        public IReadOnlyList<ChatPlayer> Snapshot(int count = 6)
        {
            return players.Take(count).ToArray();
        }

        public void Clear()
        {
            players.Clear();
        }
    }
}
