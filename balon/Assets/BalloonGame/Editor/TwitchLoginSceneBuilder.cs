#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonGame.Editor
{
    public static class TwitchLoginSceneBuilder
    {
        private const string LoginScenePath = "Assets/Scenes/TwitchLogin.unity";
        private const string GameScenePath = "Assets/Scenes/BalloonGame.unity";
        private const string SettingsPath = "Assets/BalloonGame/Config/BalloonGameSettings.asset";

        [InitializeOnLoadMethod]
        private static void BuildMissingLoginSceneWhenSafe()
        {
            if (File.Exists(LoginScenePath))
                return;

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                    !EditorSceneManager.GetActiveScene().isDirty)
                    BuildLoginScene();
            };
        }

        [MenuItem("Tools/Balloon Game/Build Twitch Login Scene")]
        public static void BuildLoginScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Color backgroundColor = new Color32(14, 18, 37, 255);
            Color surfaceColor = new Color32(27, 31, 54, 255);
            Color twitchColor = new Color32(145, 70, 255, 255);
            Color primaryText = new Color32(247, 244, 255, 255);
            Color mutedText = new Color32(164, 169, 194, 255);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BalloonGameSettings settings =
                AssetDatabase.LoadAssetAtPath<BalloonGameSettings>(SettingsPath);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            GameObject canvasObject = new GameObject(
                "Twitch Login Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = Rect(
                "Login Screen",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            ImageRect(
                "Background",
                root,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                backgroundColor);

            RectTransform loginGroup = Rect(
                "Login Group",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 620f));

            Label(
                "TWITCH BAĞLANTISI",
                loginGroup,
                font,
                48,
                FontStyle.Bold,
                primaryText,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -58f),
                new Vector2(680f, 70f));

            Label(
                "Balon oyununu kanalına bağlamak için Twitch hesabınla giriş yap.",
                loginGroup,
                font,
                22,
                FontStyle.Normal,
                mutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -142f),
                new Vector2(680f, 70f));

            Button loginButton = Button(
                "Twitch ile giriş yap",
                loginGroup,
                font,
                twitchColor,
                primaryText,
                new Vector2(0f, 28f),
                new Vector2(650f, 82f));

            Text deviceCodeText = Label(
                string.Empty,
                loginGroup,
                font,
                46,
                FontStyle.Bold,
                twitchColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -88f),
                new Vector2(650f, 70f));

            Text statusText = Label(
                "Devam etmek için Twitch ile giriş yap.",
                loginGroup,
                font,
                19,
                FontStyle.Normal,
                mutedText,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 52f),
                new Vector2(680f, 80f));

            RectTransform loadingGroup = Rect(
                "Loading Group",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 200f));
            Label(
                "TWITCH OTURUMU KONTROL EDİLİYOR…",
                loadingGroup,
                font,
                28,
                FontStyle.Bold,
                primaryText,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            GameObject controllerObject = new GameObject("Twitch Login Controller");
            TwitchAuthService authService = controllerObject.AddComponent<TwitchAuthService>();
            TwitchLoginController controller = controllerObject.AddComponent<TwitchLoginController>();
            controller.settings = settings;
            controller.loadingGroup = loadingGroup.gameObject;
            controller.loginGroup = loginGroup.gameObject;
            controller.loginButton = loginButton;
            controller.statusText = statusText;
            controller.deviceCodeText = deviceCodeText;

            loginGroup.gameObject.SetActive(false);
            loadingGroup.gameObject.SetActive(true);

            EditorSceneManager.SaveScene(scene, LoginScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoginScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = controllerObject;
            Debug.Log("Twitch login scene created at " + LoginScenePath);
        }

        private static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static Image ImageRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Label(
            string text,
            Transform parent,
            Font font,
            int size,
            FontStyle style,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect = Rect(text + " Label", parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(
            string title,
            Transform parent,
            Font font,
            Color background,
            Color textColor,
            Vector2 position,
            Vector2 size)
        {
            Image image = ImageRect(
                title + " Button",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                background);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.18f);
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.45f);
            button.colors = colors;

            Label(
                title,
                image.transform,
                font,
                25,
                FontStyle.Bold,
                textColor,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            return button;
        }
    }
}
#endif
