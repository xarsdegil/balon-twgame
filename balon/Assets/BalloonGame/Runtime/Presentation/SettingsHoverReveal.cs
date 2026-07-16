using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BalloonGame
{
    public class SettingsHoverReveal : MonoBehaviour
    {
        public CanvasGroup settingsButtonGroup;
        public GameObject settingsPanel;
        [Min(1f)] public float fadeSpeed = 8f;

        private void Awake()
        {
            SetButtonState(0f);
        }

        private void Update()
        {
            bool panelOpen = settingsPanel != null && settingsPanel.activeSelf;
            bool pointerInside = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 position = Mouse.current.position.ReadValue();
                pointerInside = Application.isFocused &&
                                position.x >= 0f && position.x <= Screen.width &&
                                position.y >= 0f && position.y <= Screen.height;
            }
#else
            Vector2 position = Input.mousePosition;
            pointerInside = Application.isFocused &&
                            position.x >= 0f && position.x <= Screen.width &&
                            position.y >= 0f && position.y <= Screen.height;
#endif
            float target = pointerInside || panelOpen ? 1f : 0f;
            float alpha = Mathf.MoveTowards(
                settingsButtonGroup.alpha,
                target,
                fadeSpeed * Time.unscaledDeltaTime);
            SetButtonState(alpha);
        }

        private void SetButtonState(float alpha)
        {
            if (settingsButtonGroup == null)
                return;
            settingsButtonGroup.alpha = alpha;
            settingsButtonGroup.interactable = alpha > 0.05f;
            settingsButtonGroup.blocksRaycasts = alpha > 0.05f;
        }
    }
}
