using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    [DefaultExecutionOrder(-1000)]
    public class BalloonSettingsPanel : MonoBehaviour
    {
        [Header("Game References")]
        public BalloonGameSettings settings;
        public Camera gameCamera;
        public Image backgroundImage;
        public Text commandDisplay;
        public BalloonController balloonController;
        public SettingsColorPicker colorPicker;

        [Header("Panel")]
        public GameObject panel;
        public Button openButton;
        public Button closeButton;
        public Button saveButton;
        public Button resetButton;
        public Text feedbackText;

        [Header("Fields")]
        public InputField commandInput;
        public InputField explosionChatMessageInput;
        public InputField backgroundColorInput;
        public Toggle transparentBackgroundToggle;
        public InputField balloonColorInput;
        public Toggle showPlayerNamesToggle;
        public Toggle oneTurnPerRoundToggle;
        public InputField cooldownInput;
        public InputField minimumInflationsInput;
        public InputField maximumInflationsInput;
        public InputField maximumQueueInput;
        public InputField inflationDurationInput;
        public Toggle clearQueueToggle;

        private BalloonUserSettings defaults;

        private void Awake()
        {
            defaults = CaptureCurrentSettings();
            if (BalloonUserSettingsStore.TryLoad(out BalloonUserSettings saved))
            {
                MigrateLegacyTurkishDefaults(saved);
                Apply(saved);
            }
            else
                Apply(defaults);
        }

        private void Start()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(Save);
            resetButton.onClick.AddListener(ResetToDefaults);
            panel.SetActive(false);
            PopulateFields(CaptureCurrentSettings());
        }

        private void Open()
        {
            PopulateFields(CaptureCurrentSettings());
            feedbackText.text = string.Empty;
            panel.SetActive(true);
        }

        private void Close()
        {
            panel.SetActive(false);
        }

        private void Save()
        {
            BalloonUserSettings values = ReadFields();
            Apply(values);
            BalloonUserSettingsStore.Save(values);
            PopulateFields(values);
            feedbackText.text = settings.language.settingsSaved;
        }

        private void ResetToDefaults()
        {
            BalloonUserSettingsStore.Delete();
            Apply(defaults);
            PopulateFields(defaults);
            feedbackText.text = settings.language.defaultsRestored;
        }

        private BalloonUserSettings ReadFields()
        {
            int minimum = ParseInt(minimumInflationsInput.text, settings.minimumInflations, 1, 100);
            int maximum = ParseInt(maximumInflationsInput.text, settings.maximumInflations, minimum, 100);
            return new BalloonUserSettings
            {
                command = NormalizeCommand(commandInput.text),
                explosionChatMessage = NormalizeExplosionMessage(explosionChatMessageInput.text),
                backgroundColorHex = NormalizeColor(backgroundColorInput.text, settings.backgroundColor),
                transparentBackground = transparentBackgroundToggle.isOn,
                balloonColorHex = NormalizeColor(balloonColorInput.text, settings.balloonColor),
                showPlayerNames = showPlayerNamesToggle.isOn,
                oneTurnPerUserPerRound = oneTurnPerRoundToggle.isOn,
                userCooldownSeconds = ParseFloat(cooldownInput.text, settings.userCooldownSeconds, 0f, 600f),
                minimumInflations = minimum,
                maximumInflations = maximum,
                maximumQueueLength = ParseInt(maximumQueueInput.text, settings.maximumQueueLength, 1, 500),
                inflationDuration = ParseFloat(inflationDurationInput.text, settings.inflationDuration, 0.05f, 10f),
                clearQueueAfterExplosion = clearQueueToggle.isOn
            };
        }

        private void Apply(BalloonUserSettings values)
        {
            settings.command = NormalizeCommand(values.command);
            settings.explosionChatMessage = NormalizeExplosionMessage(values.explosionChatMessage);
            settings.backgroundColor = ParseColor(values.backgroundColorHex, defaults != null
                ? ParseColor(defaults.backgroundColorHex, settings.backgroundColor)
                : settings.backgroundColor);
            settings.balloonColor = ParseColor(values.balloonColorHex, defaults != null
                ? ParseColor(defaults.balloonColorHex, settings.balloonColor)
                : settings.balloonColor);
            settings.showPlayerNames = values.showPlayerNames;
            settings.oneTurnPerUserPerRound = values.oneTurnPerUserPerRound;
            settings.userCooldownSeconds = Mathf.Clamp(values.userCooldownSeconds, 0f, 600f);
            settings.minimumInflations = Mathf.Clamp(values.minimumInflations, 1, 100);
            settings.maximumInflations = Mathf.Clamp(
                values.maximumInflations,
                settings.minimumInflations,
                100);
            settings.maximumQueueLength = Mathf.Clamp(values.maximumQueueLength, 1, 500);
            settings.inflationDuration = Mathf.Clamp(values.inflationDuration, 0.05f, 10f);
            settings.clearQueueAfterExplosion = values.clearQueueAfterExplosion;

            Color visibleBackground = settings.backgroundColor;
            visibleBackground.a = values.transparentBackground ? 0f : 1f;
            if (backgroundImage != null)
                backgroundImage.color = visibleBackground;
            if (gameCamera != null)
                gameCamera.backgroundColor = visibleBackground;
            if (commandDisplay != null)
                commandDisplay.text = settings.command;
            if (balloonController != null)
                balloonController.RefreshColors();
        }

        private BalloonUserSettings CaptureCurrentSettings()
        {
            bool transparent = backgroundImage != null
                ? backgroundImage.color.a < 0.01f
                : settings.backgroundColor.a < 0.01f;
            return new BalloonUserSettings
            {
                command = settings.command,
                explosionChatMessage = settings.explosionChatMessage,
                backgroundColorHex = ColorUtility.ToHtmlStringRGB(settings.backgroundColor),
                transparentBackground = transparent,
                balloonColorHex = ColorUtility.ToHtmlStringRGB(settings.balloonColor),
                showPlayerNames = settings.showPlayerNames,
                oneTurnPerUserPerRound = settings.oneTurnPerUserPerRound,
                userCooldownSeconds = settings.userCooldownSeconds,
                minimumInflations = settings.minimumInflations,
                maximumInflations = settings.maximumInflations,
                maximumQueueLength = settings.maximumQueueLength,
                inflationDuration = settings.inflationDuration,
                clearQueueAfterExplosion = settings.clearQueueAfterExplosion
            };
        }

        private void PopulateFields(BalloonUserSettings values)
        {
            commandInput.text = values.command;
            explosionChatMessageInput.text = NormalizeExplosionMessage(values.explosionChatMessage);
            backgroundColorInput.text = "#" + values.backgroundColorHex.TrimStart('#');
            transparentBackgroundToggle.isOn = values.transparentBackground;
            balloonColorInput.text = "#" + values.balloonColorHex.TrimStart('#');
            showPlayerNamesToggle.isOn = values.showPlayerNames;
            oneTurnPerRoundToggle.isOn = values.oneTurnPerUserPerRound;
            cooldownInput.text = values.userCooldownSeconds.ToString("0.##");
            minimumInflationsInput.text = values.minimumInflations.ToString();
            maximumInflationsInput.text = values.maximumInflations.ToString();
            maximumQueueInput.text = values.maximumQueueLength.ToString();
            inflationDurationInput.text = values.inflationDuration.ToString("0.##");
            clearQueueToggle.isOn = values.clearQueueAfterExplosion;
            if (colorPicker != null)
                colorPicker.RefreshSwatches();
        }

        private string NormalizeCommand(string value)
        {
            string command = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(command))
                command = settings.language.defaultCommand;
            return command.StartsWith("!") ? command : "!" + command;
        }

        private string NormalizeExplosionMessage(string value)
        {
            string message = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return string.IsNullOrWhiteSpace(message)
                ? settings.language.defaultExplosionChatMessage
                : message;
        }

        private void MigrateLegacyTurkishDefaults(BalloonUserSettings values)
        {
            if (values.command == "!sisir")
                values.command = settings.language.defaultCommand;
            if (values.explosionChatMessage == "@{player} balonu patlatti!")
                values.explosionChatMessage = settings.language.defaultExplosionChatMessage;
        }

        private static string NormalizeColor(string value, Color fallback)
        {
            Color color = ParseColor(value, fallback);
            return ColorUtility.ToHtmlStringRGB(color);
        }

        private static Color ParseColor(string value, Color fallback)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (!normalized.StartsWith("#"))
                normalized = "#" + normalized;
            return ColorUtility.TryParseHtmlString(normalized, out Color color) ? color : fallback;
        }

        private static int ParseInt(string value, int fallback, int minimum, int maximum)
        {
            return int.TryParse(value, out int parsed)
                ? Mathf.Clamp(parsed, minimum, maximum)
                : Mathf.Clamp(fallback, minimum, maximum);
        }

        private static float ParseFloat(string value, float fallback, float minimum, float maximum)
        {
            string normalized = (value ?? string.Empty).Replace(',', '.');
            return float.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float parsed)
                ? Mathf.Clamp(parsed, minimum, maximum)
                : Mathf.Clamp(fallback, minimum, maximum);
        }
    }
}
