using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    public class SettingsColorPicker : MonoBehaviour
    {
        public GameObject pickerPanel;
        public HsvColorWheel colorWheel;
        public Image selectedColorPreview;
        public Button applyButton;
        public Button cancelButton;

        [Header("Color Fields")]
        public InputField backgroundColorInput;
        public Button backgroundColorButton;
        public Image backgroundColorSwatch;
        public InputField balloonColorInput;
        public Button balloonColorButton;
        public Image balloonColorSwatch;

        [Header("Defaults")]
        public Color defaultPickerColor = new Color32(0, 200, 83, 255);

        private InputField activeInput;
        private Image activeSwatch;
        private Color selectedColor;

        private void Awake()
        {
            selectedColor = defaultPickerColor;
            colorWheel.SetColor(defaultPickerColor);
            selectedColorPreview.color = defaultPickerColor;
            pickerPanel.SetActive(false);
        }

        private void Start()
        {
            backgroundColorButton.onClick.AddListener(() =>
                Open(backgroundColorInput, backgroundColorSwatch));
            balloonColorButton.onClick.AddListener(() =>
                Open(balloonColorInput, balloonColorSwatch));
            applyButton.onClick.AddListener(ApplySelection);
            cancelButton.onClick.AddListener(Close);
            colorWheel.ColorChanged += OnColorChanged;
            backgroundColorInput.onValueChanged.AddListener(_ => RefreshSwatches());
            balloonColorInput.onValueChanged.AddListener(_ => RefreshSwatches());
            RefreshSwatches();
        }

        private void OnDestroy()
        {
            if (colorWheel != null)
                colorWheel.ColorChanged -= OnColorChanged;
        }

        public void RefreshSwatches()
        {
            backgroundColorSwatch.color = ParseColor(backgroundColorInput.text, defaultPickerColor);
            balloonColorSwatch.color = ParseColor(balloonColorInput.text, defaultPickerColor);
        }

        private void Open(InputField input, Image swatch)
        {
            activeInput = input;
            activeSwatch = swatch;
            selectedColor = ParseColor(input.text, defaultPickerColor);
            colorWheel.SetColor(selectedColor);
            selectedColorPreview.color = selectedColor;
            pickerPanel.SetActive(true);
        }

        private void OnColorChanged(Color color)
        {
            selectedColor = color;
            selectedColorPreview.color = color;
        }

        private void ApplySelection()
        {
            if (activeInput != null)
                activeInput.text = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);
            if (activeSwatch != null)
                activeSwatch.color = selectedColor;
            Close();
        }

        private void Close()
        {
            pickerPanel.SetActive(false);
            activeInput = null;
            activeSwatch = null;
        }

        private static Color ParseColor(string value, Color fallback)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (!normalized.StartsWith("#"))
                normalized = "#" + normalized;
            return ColorUtility.TryParseHtmlString(normalized, out Color color) ? color : fallback;
        }
    }
}
