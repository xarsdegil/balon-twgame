using UnityEngine;

namespace BalloonGame
{
    [CreateAssetMenu(fileName = "BalloonGameSettings", menuName = "Balloon Game/Game Settings")]
    public class BalloonGameSettings : ScriptableObject
    {
        [Header("Twitch")]
        [Tooltip("Public Twitch application Client ID. End users never enter this value.")]
        public string twitchClientId = "";
        [Tooltip("Channel name without #. Leave empty while using local test mode.")]
        public string twitchChannel = "";
        [Tooltip("The Twitch account that owns the OAuth token. This may be a dedicated bot account.")]
        public string twitchUsername = "";
        public string command = "!sisir";
        [Tooltip("User Access Token with chat:read. Prefer the TWITCH_OAUTH_TOKEN environment variable.")]
        public string oauthToken = "";
        public bool connectToTwitchOnStart;

        [Header("Round Rules")]
        [Min(1)] public int minimumInflations = 3;
        [Min(2)] public int maximumInflations = 14;
        [Min(1)] public int maximumQueueLength = 20;
        [Min(0f)] public float userCooldownSeconds = 10f;
        public bool clearQueueAfterExplosion = true;
        public bool oneTurnPerUserPerRound = true;

        [Header("Inflation")]
        [Min(0.05f)] public float inflationDuration = 0.75f;
        [Min(0f)] public float delayBetweenPlayers = 0.35f;
        [Range(0.01f, 0.25f)] public float minimumGrowth = 0.045f;
        [Range(0.01f, 0.25f)] public float maximumGrowth = 0.08f;
        [Min(1f)] public float maximumVisualScale = 2.15f;
        public AnimationCurve inflationEase = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.2f),
            new Keyframe(0.72f, 1.05f, 0.2f, 0.2f),
            new Keyframe(1f, 1f, -0.3f, 0f));

        [Header("Balloon Motion")]
        [Range(0f, 0.2f)] public float squashStretchAmount = 0.065f;
        [Range(0f, 20f)] public float idleSwayDegrees = 2.4f;
        [Range(0f, 30f)] public float dangerShakePixels = 8f;
        [Min(0.1f)] public float idleSwaySpeed = 1.25f;

        [Header("Explosion")]
        [Min(0.05f)] public float preExplosionHold = 0.14f;
        [Min(0.1f)] public float explosionDuration = 0.85f;
        [Min(0f)] public float resultDisplayDuration = 2.2f;
        [Range(0f, 40f)] public float screenShakePixels = 14f;
        [Min(1)] public int explosionShardCount = 18;

        [Header("Presentation")]
        public Color backgroundColor = new Color32(14, 18, 37, 255);
        public Color backgroundAccentColor = new Color32(43, 50, 85, 255);
        public Color balloonColor = new Color32(255, 76, 119, 255);
        public Color balloonHighlightColor = new Color32(255, 197, 211, 210);
        public Color accentColor = new Color32(95, 232, 206, 255);
        public Color primaryTextColor = new Color32(247, 244, 255, 255);
        public Color mutedTextColor = new Color32(164, 169, 194, 255);
        public bool showPlayerNames = true;

        [Header("Application")]
        [Range(512, 2160)] public int squareResolution = 1080;
        [Range(30, 120)] public int targetFrameRate = 60;
        public bool runInBackground = true;

        private void OnValidate()
        {
            maximumInflations = Mathf.Max(minimumInflations, maximumInflations);
            maximumGrowth = Mathf.Max(minimumGrowth, maximumGrowth);
        }
    }
}
