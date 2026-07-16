using System;
using UnityEngine;

namespace BalloonGame
{
    public enum BalloonGameState
    {
        Waiting,
        Inflating,
        CheckingExplosion,
        Exploding,
        Resetting
    }

    [Serializable]
    public class ChatPlayer
    {
        public string userId;
        public string username;
        public string displayName;
        public Color nameColor = Color.white;

        public ChatPlayer(string id, string login, string display, Color color)
        {
            userId = id;
            username = login;
            displayName = string.IsNullOrWhiteSpace(display) ? login : display;
            nameColor = color;
        }
    }

    [Serializable]
    public class BalloonRoundData
    {
        public int roundNumber;
        public int currentInflationCount;
        public int explosionThreshold;
        public float currentBalloonSize = 1f;
        public string lastPlayerId;
    }
}

