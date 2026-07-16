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
    public static class BalloonPrototypeSceneBuilder
    {
        private const string SettingsPath = "Assets/BalloonGame/Config/BalloonGameSettings.asset";
        private const string ScenePath = "Assets/Scenes/BalloonGame.unity";

        [InitializeOnLoadMethod]
        private static void InitializeSceneMaintenance()
        {
            EditorApplication.delayCall += RebuildOldLayoutWhenSafe;
        }

        private static void RebuildOldLayoutWhenSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            if (activeScene.path == ScenePath)
            {
                GameObject oldPump = GameObject.Find("Hand Pump (fully editable)");
                GameObject oldQueue = GameObject.Find("Queue Accent Line");
                if (oldPump != null || oldQueue != null)
                    BuildScene();
                return;
            }

            if (!File.Exists(ScenePath) && !activeScene.isDirty)
                BuildScene();
        }

        [MenuItem("Tools/Balloon Game/Build Minimal Square Scene")]
        public static void BuildScene()
        {
            BalloonGameSettings settings = LoadOrCreateSettings();
            ApplySquarePlayerSettings(settings);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = settings.backgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            GameObject canvasObject = new GameObject(
                "Balloon Game Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(settings.squareResolution, settings.squareResolution);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform content = Rect(
                "Square Game Area (screen shake target)",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            Image background = ImageRect(
                "Background",
                content,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                settings.backgroundColor);
            background.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text commandText = Label(
                settings.command,
                content,
                font,
                58,
                FontStyle.Bold,
                settings.primaryTextColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 326f),
                new Vector2(720f, 90f));

            CreateBalloon(
                content,
                settings,
                out RectTransform assembly,
                out RectTransform visualRoot,
                out CanvasGroup balloonGroup,
                out Graphic balloonBody,
                out Graphic highlight,
                out BalloonExplosionEffect explosionEffect);

            Text playerText = Label(
                string.Empty,
                content,
                font,
                34,
                FontStyle.Bold,
                settings.primaryTextColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -338f),
                new Vector2(760f, 64f));
            playerText.gameObject.name = "Current Inflating Player";

            GameObject systems = new GameObject("Game Systems");
            TwitchChatService twitch = systems.AddComponent<TwitchChatService>();
            CommandManager commands = systems.AddComponent<CommandManager>();
            BalloonController balloon = systems.AddComponent<BalloonController>();
            GameHudController hud = systems.AddComponent<GameHudController>();
            BalloonGameManager game = systems.AddComponent<BalloonGameManager>();
            LocalTestInput testInput = systems.AddComponent<LocalTestInput>();

            twitch.settings = settings;

            commands.settings = settings;
            commands.twitchChatService = twitch;

            balloon.settings = settings;
            balloon.balloonAssembly = assembly;
            balloon.visualRoot = visualRoot;
            balloon.balloonCanvasGroup = balloonGroup;
            balloon.balloonBody = balloonBody;
            balloon.balloonHighlight = highlight;
            balloon.explosionEffect = explosionEffect;
            balloon.shakeTarget = content;

            hud.settings = settings;
            hud.commandManager = commands;
            hud.twitchService = twitch;
            hud.commandText = commandText;
            hud.currentPlayerText = playerText;

            game.settings = settings;
            game.commandManager = commands;
            game.balloonController = balloon;
            game.hud = hud;

            testInput.commandManager = commands;
            testInput.gameManager = game;

            CreateSettingsInterface(
                canvasObject.transform,
                settings,
                font,
                camera,
                background,
                commandText,
                balloon);

            EditorSceneManager.SaveScene(scene, ScenePath);
            const string loginScenePath = "Assets/Scenes/TwitchLogin.unity";
            EditorBuildSettings.scenes = File.Exists(loginScenePath)
                ? new[]
                {
                    new EditorBuildSettingsScene(loginScenePath, true),
                    new EditorBuildSettingsScene(ScenePath, true)
                }
                : new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = systems;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Minimal square Balloon Game scene created at " + ScenePath);
        }

        private static void CreateSettingsInterface(
            Transform parent,
            BalloonGameSettings settings,
            Font font,
            Camera gameCamera,
            Image background,
            Text commandText,
            BalloonController balloonController)
        {
            Color panelColor = new Color32(20, 24, 43, 250);
            Color fieldColor = new Color32(38, 43, 70, 255);
            Color textColor = new Color32(247, 244, 255, 255);
            Color mutedColor = new Color32(174, 179, 202, 255);
            Color accentColor = new Color32(145, 70, 255, 255);

            Button openButton = Button(
                "AYAR",
                parent,
                font,
                fieldColor,
                textColor,
                new Vector2(1f, 1f),
                new Vector2(-78f, -54f),
                new Vector2(118f, 52f));
            openButton.gameObject.name = "Hover Settings Button";
            CanvasGroup buttonGroup = openButton.gameObject.AddComponent<CanvasGroup>();

            Image panelBackground = ImageRect(
                "Settings Panel",
                parent,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.025f, 0.03f, 0.065f, 0.94f));
            RectTransform panel = panelBackground.rectTransform;

            RectTransform sheet = Rect(
                "Settings Sheet",
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(820f, 970f));
            Image sheetImage = sheet.gameObject.AddComponent<Image>();
            sheetImage.color = panelColor;

            Label(
                "OYUN AYARLARI",
                sheet,
                font,
                38,
                FontStyle.Bold,
                textColor,
                TextAnchor.MiddleLeft,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-340f, -55f),
                new Vector2(520f, 60f));

            Button closeButton = Button(
                "KAPAT",
                sheet,
                font,
                fieldColor,
                textColor,
                new Vector2(1f, 1f),
                new Vector2(-76f, -58f),
                new Vector2(116f, 48f));

            float y = 344f;
            const float step = 58f;
            InputField commandInput = SettingsInput("Komut", settings.command, y);
            y -= step;
            InputField backgroundInput = SettingsInput(
                "Arka plan rengi",
                "#" + ColorUtility.ToHtmlStringRGB(settings.backgroundColor),
                y);
            y -= step;
            Toggle transparentToggle = SettingsToggle("Şeffaf arka plan", false, y);
            y -= step;
            InputField balloonColorInput = SettingsInput(
                "Balon rengi",
                "#" + ColorUtility.ToHtmlStringRGB(settings.balloonColor),
                y);
            y -= step;
            Toggle showNamesToggle = SettingsToggle("Oyuncu adını göster", settings.showPlayerNames, y);
            y -= step;
            Toggle oneTurnToggle = SettingsToggle(
                "Tur başına tek katılım",
                settings.oneTurnPerUserPerRound,
                y);
            y -= step;
            InputField cooldownInput = SettingsInput(
                "Kullanıcı bekleme süresi",
                settings.userCooldownSeconds.ToString("0.##"),
                y);
            y -= step;
            InputField minimumInput = SettingsInput(
                "Minimum şişirme",
                settings.minimumInflations.ToString(),
                y);
            y -= step;
            InputField maximumInput = SettingsInput(
                "Maksimum şişirme",
                settings.maximumInflations.ToString(),
                y);
            y -= step;
            InputField queueInput = SettingsInput(
                "Maksimum kuyruk",
                settings.maximumQueueLength.ToString(),
                y);
            y -= step;
            InputField durationInput = SettingsInput(
                "Şişirme süresi",
                settings.inflationDuration.ToString("0.##"),
                y);
            y -= step;
            Toggle clearQueueToggle = SettingsToggle(
                "Patlamada kuyruğu temizle",
                settings.clearQueueAfterExplosion,
                y);

            Button resetButton = Button(
                "VARSAYILAN",
                sheet,
                font,
                fieldColor,
                textColor,
                new Vector2(0.5f, 0f),
                new Vector2(-155f, 67f),
                new Vector2(240f, 62f));
            Button saveButton = Button(
                "KAYDET",
                sheet,
                font,
                accentColor,
                textColor,
                new Vector2(0.5f, 0f),
                new Vector2(155f, 67f),
                new Vector2(240f, 62f));
            Text feedback = Label(
                string.Empty,
                sheet,
                font,
                17,
                FontStyle.Normal,
                mutedColor,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 20f),
                new Vector2(600f, 30f));

            BalloonSettingsPanel controller = parent.gameObject.AddComponent<BalloonSettingsPanel>();
            controller.settings = settings;
            controller.gameCamera = gameCamera;
            controller.backgroundImage = background;
            controller.commandDisplay = commandText;
            controller.balloonController = balloonController;
            controller.panel = panelBackground.gameObject;
            controller.openButton = openButton;
            controller.closeButton = closeButton;
            controller.saveButton = saveButton;
            controller.resetButton = resetButton;
            controller.feedbackText = feedback;
            controller.commandInput = commandInput;
            controller.backgroundColorInput = backgroundInput;
            controller.transparentBackgroundToggle = transparentToggle;
            controller.balloonColorInput = balloonColorInput;
            controller.showPlayerNamesToggle = showNamesToggle;
            controller.oneTurnPerRoundToggle = oneTurnToggle;
            controller.cooldownInput = cooldownInput;
            controller.minimumInflationsInput = minimumInput;
            controller.maximumInflationsInput = maximumInput;
            controller.maximumQueueInput = queueInput;
            controller.inflationDurationInput = durationInput;
            controller.clearQueueToggle = clearQueueToggle;

            SettingsHoverReveal hover = parent.gameObject.AddComponent<SettingsHoverReveal>();
            hover.settingsButtonGroup = buttonGroup;
            hover.settingsPanel = panelBackground.gameObject;
            panelBackground.gameObject.SetActive(false);

            InputField SettingsInput(string title, string value, float rowY)
            {
                Label(
                    title,
                    sheet,
                    font,
                    19,
                    FontStyle.Normal,
                    mutedColor,
                    TextAnchor.MiddleLeft,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-335f, rowY),
                    new Vector2(350f, 44f));
                return Input(
                    title,
                    value,
                    sheet,
                    font,
                    fieldColor,
                    textColor,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(205f, rowY),
                    new Vector2(300f, 44f));
            }

            Toggle SettingsToggle(string title, bool value, float rowY)
            {
                Label(
                    title,
                    sheet,
                    font,
                    19,
                    FontStyle.Normal,
                    mutedColor,
                    TextAnchor.MiddleLeft,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-335f, rowY),
                    new Vector2(500f, 44f));
                return Toggle(
                    title,
                    sheet,
                    value,
                    fieldColor,
                    accentColor,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(330f, rowY));
            }
        }

        private static void CreateBalloon(
            Transform parent,
            BalloonGameSettings settings,
            out RectTransform assembly,
            out RectTransform visualRoot,
            out CanvasGroup balloonGroup,
            out Graphic balloonBody,
            out Graphic highlight,
            out BalloonExplosionEffect explosionEffect)
        {
            assembly = Rect(
                "Balloon Assembly (animated)",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -55f),
                new Vector2(420f, 520f));

            visualRoot = Rect(
                "Balloon Visual Root (scale animation)",
                assembly,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(286f, 354f));
            balloonGroup = visualRoot.gameObject.AddComponent<CanvasGroup>();

            EditableShapeGraphic body = Shape(
                "Balloon Body",
                visualRoot,
                EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(286f, 334f),
                settings.balloonColor);
            balloonBody = body;

            EditableShapeGraphic gloss = Shape(
                "Balloon Highlight",
                visualRoot,
                EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f),
                new Vector2(-61f, 73f),
                new Vector2(62f, 108f),
                settings.balloonHighlightColor);
            gloss.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            highlight = gloss;

            EditableShapeGraphic knot = Shape(
                "Balloon Knot",
                visualRoot,
                EditableShape.Triangle,
                new Vector2(0.5f, 0f),
                new Vector2(0f, -2f),
                new Vector2(44f, 36f),
                settings.balloonColor);
            knot.rotationDegrees = 180f;

            RectTransform shardRoot = Rect(
                "Explosion Shards (edit children)",
                assembly,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            explosionEffect = shardRoot.gameObject.AddComponent<BalloonExplosionEffect>();

            for (int i = 0; i < settings.explosionShardCount; i++)
            {
                EditableShapeGraphic shard = Shape(
                    "Shard " + (i + 1).ToString("00"),
                    shardRoot,
                    i % 3 == 0 ? EditableShape.Triangle : EditableShape.Diamond,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(18f + (i % 4) * 5f, 26f + (i % 3) * 8f),
                    settings.balloonColor);
                shard.gameObject.SetActive(false);
                explosionEffect.shards.Add(shard.rectTransform);
            }
        }

        private static BalloonGameSettings LoadOrCreateSettings()
        {
            BalloonGameSettings settings = AssetDatabase.LoadAssetAtPath<BalloonGameSettings>(SettingsPath);
            if (settings != null)
                return settings;

            Directory.CreateDirectory("Assets/BalloonGame/Config");
            settings = ScriptableObject.CreateInstance<BalloonGameSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static void ApplySquarePlayerSettings(BalloonGameSettings settings)
        {
            PlayerSettings.defaultScreenWidth = settings.squareResolution;
            PlayerSettings.defaultScreenHeight = settings.squareResolution;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
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
            image.raycastTarget = false;
            return image;
        }

        private static EditableShapeGraphic Shape(
            string name,
            Transform parent,
            EditableShape shape,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, anchoredPosition, size);
            rect.gameObject.AddComponent<CanvasRenderer>();
            EditableShapeGraphic graphic = rect.gameObject.AddComponent<EditableShapeGraphic>();
            graphic.shape = shape;
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
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
            RectTransform rect = Rect("Inflation Command", parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static InputField Input(
            string name,
            string value,
            Transform parent,
            Font font,
            Color background,
            Color textColor,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            Image image = ImageRect(name + " Input", parent, anchor, anchor, position, size, background);
            image.raycastTarget = true;
            InputField input = image.gameObject.AddComponent<InputField>();
            Text text = Label(
                value,
                image.transform,
                font,
                19,
                FontStyle.Normal,
                textColor,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(16f, 0f),
                new Vector2(-32f, 0f));
            text.gameObject.name = name + " Value";
            input.textComponent = text;
            input.text = value;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static Toggle Toggle(
            string name,
            Transform parent,
            bool value,
            Color background,
            Color checkColor,
            Vector2 anchor,
            Vector2 position)
        {
            Image box = ImageRect(
                name + " Toggle",
                parent,
                anchor,
                anchor,
                position,
                new Vector2(42f, 42f),
                background);
            box.raycastTarget = true;
            Toggle toggle = box.gameObject.AddComponent<Toggle>();
            Image check = ImageRect(
                "Checkmark",
                box.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-12f, -12f),
                checkColor);
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            return toggle;
        }

        private static Button Button(
            string title,
            Transform parent,
            Font font,
            Color background,
            Color textColor,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            Image image = ImageRect(title + " Button", parent, anchor, anchor, position, size, background);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.2f);
            button.colors = colors;
            Label(
                title,
                image.transform,
                font,
                17,
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
