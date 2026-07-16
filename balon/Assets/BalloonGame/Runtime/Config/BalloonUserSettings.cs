using System;

namespace BalloonGame
{
    [Serializable]
    public class BalloonUserSettings
    {
        public string command;
        public string backgroundColorHex;
        public bool transparentBackground;
        public string balloonColorHex;
        public bool showPlayerNames;
        public bool oneTurnPerUserPerRound;
        public float userCooldownSeconds;
        public int minimumInflations;
        public int maximumInflations;
        public int maximumQueueLength;
        public float inflationDuration;
        public bool clearQueueAfterExplosion;
    }
}

