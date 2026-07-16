using UnityEngine;

namespace BalloonGame
{
    [CreateAssetMenu(fileName = "BalloonLanguageEnglish", menuName = "Balloon Game/Language")]
    public class BalloonLanguage : ScriptableObject
    {
        [Header("Game Defaults")]
        public string defaultCommand = "!inflate";
        public string defaultExplosionChatMessage = "@{player} popped the balloon!";

        [Header("Settings Panel")]
        public string settingsButton = "SETTINGS";
        public string settingsTitle = "GAME SETTINGS";
        public string closeButton = "CLOSE";
        public string saveButton = "SAVE";
        public string resetButton = "RESTORE DEFAULTS";
        public string commandLabel = "Command";
        public string explosionMessageLabel = "Explosion chat message";
        public string backgroundColorLabel = "Background color";
        public string transparentBackgroundLabel = "Transparent background";
        public string balloonColorLabel = "Balloon color";
        public string showPlayerNamesLabel = "Show player names";
        public string oneTurnPerRoundLabel = "One turn per player each round";
        public string cooldownLabel = "Player cooldown (seconds)";
        public string minimumInflationsLabel = "Minimum inflations";
        public string maximumInflationsLabel = "Maximum inflations";
        public string maximumQueueLabel = "Maximum queue size";
        public string inflationDurationLabel = "Inflation duration (seconds)";
        public string clearQueueLabel = "Clear queue after explosion";
        public string settingsSaved = "Settings saved";
        public string defaultsRestored = "Default settings restored";
        public string colorPickerTitle = "COLOR PICKER";
        public string applyColorButton = "USE COLOR";
        public string cancelButton = "CANCEL";

        [Header("Twitch Login")]
        public string loginTitle = "TWITCH CONNECTION";
        public string loginDescription = "Sign in with Twitch to connect the balloon game to your channel.";
        public string loginButton = "SIGN IN WITH TWITCH";
        public string loginReady = "Sign in with Twitch to continue.";
        public string checkingSavedSession = "CHECKING TWITCH SESSION...";
        public string checkingSavedSessionStatus = "Checking saved Twitch session...";
        public string missingClientId = "The Twitch application Client ID is missing.";
        public string clientIdRequired = "A Twitch Client ID is required.";
        public string loginCancelled = "Sign-in cancelled.";
        public string preparingConnection = "Preparing Twitch connection...";
        public string deviceCodeRequestFailed = "Could not request a device sign-in code.";
        public string invalidDeviceCode = "Twitch did not return a valid device code.";
        public string approveInBrowser = "Approve Twitch access in your browser.";
        public string tokenValidationFailed = "The Twitch token could not be validated.";
        public string loginComplete = "Twitch sign-in complete.";
        public string loginFailed = "Twitch sign-in could not be completed.";
        public string loginCodeExpired = "The Twitch sign-in code expired. Try again.";
        public string sessionExpired = "The Twitch session expired. Please sign in again.";

        [Header("Twitch Chat Status")]
        public string localTestMode = "Local test mode";
        public string twitchSettingsMissing = "Twitch settings are missing";
        public string twitchChannelMissing = "Twitch channel name is missing";
        public string twitchCredentialsMissing = "Twitch username or chat token is missing";
        public string disconnected = "Disconnected";
        public string connecting = "Connecting to Twitch...";
        public string connectedToChannel = "Connected to #{channel}";
        public string twitchError = "Twitch error: {error}";
        public string connectionLost = "Twitch connection lost";
        public string unknownViewer = "viewer";

        [Header("Game Status")]
        public string waitingForPlayer = "WAITING FOR PLAYER";
        public string inflating = "INFLATING";
        public string tensionRising = "TENSION RISING";
        public string exploded = "POPPED!";
        public string newRound = "NEW ROUND";
        public string roundFormat = "ROUND {round}";
        public string resultFormat = "{player}\nPOPPED THE BALLOON!";
        public string emptyQueue = "No players in queue";
        public string[] localTestNames =
        {
            "Ballooner", "NightOwl", "PixelCat", "PurpleDragon", "CoffeeBreak",
            "Breeze", "NeonFox", "TinyGiant", "Moonlight", "RedPanda"
        };

        [Header("Command Feedback")]
        public string alreadyQueued = "This player is already in the queue.";
        public string alreadyPlayedThisRound = "This player has already played this round.";
        public string playerOnCooldown = "This player is on cooldown.";
        public string queueFull = "The queue is full.";
    }
}
