#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonGame.Editor
{
    public static class BalloonPrototypeSceneBuilder
    {
        private const string SettingsPath = "Assets/BalloonGame/Config/BalloonGameSettings.asset";
        private const string ScenePath = "Assets/Scenes/BalloonGame.unity";

        [InitializeOnLoadMethod]
        private static void BuildFirstPrototypeWhenSafe()
        {
            EditorApplication.delayCall += RemoveLegacyInputModuleFromOpenScene;
            EditorApplication.delayCall += RepairMissingCanvasRenderers;

            if (File.Exists(ScenePath))
                return;

            EditorApplication.delayCall += () =>
            {
                if (File.Exists(ScenePath) || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    Debug.Log("Balloon Game scene was not auto-created because the current scene has unsaved changes. " +
                              "Use Tools > Balloon Game > Build Editable Prototype Scene when ready.");
                    return;
                }
                BuildScene();
            };
        }

        private static void RemoveLegacyInputModuleFromOpenScene()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            bool changed = false;
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                UnityEngine.EventSystems.StandaloneInputModule[] modules =
                    root.GetComponentsInChildren<UnityEngine.EventSystems.StandaloneInputModule>(true);
                foreach (UnityEngine.EventSystems.StandaloneInputModule module in modules)
                {
                    GameObject owner = module.gameObject;
                    Object.DestroyImmediate(module);

                    UnityEngine.EventSystems.EventSystem eventSystem =
                        owner.GetComponent<UnityEngine.EventSystems.EventSystem>();
                    if (eventSystem != null && owner.GetComponents<Component>().Length <= 2)
                        Object.DestroyImmediate(owner);

                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Removed legacy StandaloneInputModule; Balloon Game uses the Input System package.");
        }

        private static void RepairMissingCanvasRenderers()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            bool changed = false;
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                EditableShapeGraphic[] shapes = root.GetComponentsInChildren<EditableShapeGraphic>(true);
                foreach (EditableShapeGraphic shape in shapes)
                {
                    if (shape.GetComponent<CanvasRenderer>() != null)
                        continue;

                    shape.gameObject.AddComponent<CanvasRenderer>();
                    EditorUtility.SetDirty(shape.gameObject);
                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Added missing CanvasRenderer components to editable Balloon Game shapes.");
        }

        [MenuItem("Tools/Balloon Game/Build Editable Prototype Scene")]
        public static void BuildScene()
        {
            BalloonGameSettings settings = LoadOrCreateSettings();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = settings.backgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new GameObject("Balloon Game Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform content = Rect("Scene Content (screen shake target)", canvasObject.transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image background = ImageRect("Background", content, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, settings.backgroundColor);
            background.raycastTarget = false;
            CreateAmbientShapes(content, settings);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateHeader(content, settings, font, out Text roundText, out Text stateText, out Text connectionText);
            CreateQueueRail(content, settings, font, out Text queueCountText, out Text queueListText);
            CreateStage(content, settings, font, out RectTransform assembly, out RectTransform visualRoot,
                out CanvasGroup balloonGroup, out Graphic balloonBody, out Graphic highlight,
                out BalloonExplosionEffect explosionEffect, out Text playerText, out Text resultText,
                out CanvasGroup resultGroup);
            CreatePump(content, settings);
            CreateFooter(content, settings, font, out Text commandText);

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
            hud.stateText = stateText;
            hud.roundText = roundText;
            hud.queueCountText = queueCountText;
            hud.queueListText = queueListText;
            hud.currentPlayerText = playerText;
            hud.commandText = commandText;
            hud.connectionText = connectionText;
            hud.resultText = resultText;
            hud.resultGroup = resultGroup;

            game.settings = settings;
            game.commandManager = commands;
            game.balloonController = balloon;
            game.hud = hud;

            testInput.commandManager = commands;
            testInput.gameManager = game;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = systems;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Editable Balloon Game prototype created at " + ScenePath);
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

        private static void CreateHeader(Transform parent, BalloonGameSettings settings, Font font,
            out Text roundText, out Text stateText, out Text connectionText)
        {
            Text brand = Label("BALON / LIVE", parent, font, 44, FontStyle.Bold, settings.primaryTextColor,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(84f, -62f), new Vector2(520f, 70f));

            Text subtitle = Label("CHAT GERİLİM DENEYİ", parent, font, 16, FontStyle.Bold, settings.accentColor,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(87f, -126f), new Vector2(400f, 32f));

            roundText = Label("TUR 01", parent, font, 18, FontStyle.Bold, settings.mutedTextColor,
                TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-84f, -66f), new Vector2(240f, 36f));
            stateText = Label("OYUNCU BEKLENİYOR", parent, font, 15, FontStyle.Bold, settings.accentColor,
                TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-84f, -105f), new Vector2(300f, 32f));
            connectionText = Label("Yerel test modu", parent, font, 13, FontStyle.Normal, settings.mutedTextColor,
                TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-84f, -140f), new Vector2(360f, 30f));
        }

        private static void CreateQueueRail(Transform parent, BalloonGameSettings settings, Font font,
            out Text queueCountText, out Text queueListText)
        {
            Image line = ImageRect("Queue Accent Line", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(86f, 15f), new Vector2(4f, 520f), settings.accentColor);
            Label("SIRA", parent, font, 15, FontStyle.Bold, settings.mutedTextColor, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 234f), new Vector2(220f, 30f));
            queueCountText = Label("00", parent, font, 72, FontStyle.Bold, settings.primaryTextColor,
                TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(108f, 168f), new Vector2(230f, 90f));
            queueListText = Label("Henüz kimse sırada değil", parent, font, 21, FontStyle.Normal,
                settings.mutedTextColor, TextAnchor.UpperLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(112f, 75f), new Vector2(330f, 260f));
            queueListText.lineSpacing = 1.35f;
        }

        private static void CreateStage(Transform parent, BalloonGameSettings settings, Font font,
            out RectTransform assembly, out RectTransform visualRoot, out CanvasGroup balloonGroup,
            out Graphic balloonBody, out Graphic highlight, out BalloonExplosionEffect explosionEffect,
            out Text playerText, out Text resultText, out CanvasGroup resultGroup)
        {
            RectTransform stage = Rect("Balloon Stage", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(170f, 10f), new Vector2(760f, 820f));

            EditableShapeGraphic halo = Shape("Danger Halo (editable)", stage, EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(610f, 610f),
                new Color(settings.balloonColor.r, settings.balloonColor.g, settings.balloonColor.b, 0.075f));

            EditableShapeGraphic floorShadow = Shape("Floor Shadow", stage, EditableShape.Ellipse,
                new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(340f, 52f),
                new Color(0f, 0f, 0f, 0.34f));

            assembly = Rect("Balloon Assembly (animated)", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 35f), new Vector2(430f, 620f));

            Image stringLine = ImageRect("String", assembly, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 20f), new Vector2(3f, 230f), new Color32(202, 209, 224, 180));
            stringLine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);

            visualRoot = Rect("Balloon Visual Root (scale animation)", assembly,
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(330f, 410f));
            balloonGroup = visualRoot.gameObject.AddComponent<CanvasGroup>();

            EditableShapeGraphic shadow = Shape("Balloon Soft Shadow", visualRoot, EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f), new Vector2(18f, -18f), new Vector2(330f, 390f),
                new Color(0f, 0f, 0f, 0.22f));
            EditableShapeGraphic body = Shape("Balloon Body", visualRoot, EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 390f), settings.balloonColor);
            balloonBody = body;

            EditableShapeGraphic gloss = Shape("Balloon Highlight", visualRoot, EditableShape.Ellipse,
                new Vector2(0.5f, 0.5f), new Vector2(-72f, 90f), new Vector2(78f, 138f),
                settings.balloonHighlightColor);
            gloss.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            highlight = gloss;

            EditableShapeGraphic knot = Shape("Balloon Knot", visualRoot, EditableShape.Triangle,
                new Vector2(0.5f, 0f), new Vector2(0f, -13f), new Vector2(52f, 44f), settings.balloonColor);
            knot.rotationDegrees = 180f;

            RectTransform shardRoot = Rect("Explosion Shards (edit children)", assembly,
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, Vector2.zero);
            explosionEffect = shardRoot.gameObject.AddComponent<BalloonExplosionEffect>();
            for (int i = 0; i < settings.explosionShardCount; i++)
            {
                EditableShapeGraphic shard = Shape("Shard " + (i + 1).ToString("00"), shardRoot,
                    i % 3 == 0 ? EditableShape.Triangle : EditableShape.Diamond,
                    new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(18f + (i % 4) * 5f, 26f + (i % 3) * 8f), settings.balloonColor);
                shard.gameObject.SetActive(false);
                explosionEffect.shards.Add(shard.rectTransform);
            }

            playerText = Label("Sıradaki cesur izleyici bekleniyor…", stage, font, 24, FontStyle.Bold,
                settings.mutedTextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 25f), new Vector2(620f, 50f));

            RectTransform result = Rect("Explosion Result Overlay", stage, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            resultGroup = result.gameObject.AddComponent<CanvasGroup>();
            resultGroup.alpha = 0f;
            resultText = Label("BALONU\nOYUNCU\nPATLATTI!", result, font, 58, FontStyle.Bold,
                settings.primaryTextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 260f));
        }

        private static void CreatePump(Transform parent, BalloonGameSettings settings)
        {
            RectTransform pump = Rect("Hand Pump (fully editable)", parent, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-132f, 118f), new Vector2(260f, 390f));
            ImageRect("Pump Body", pump, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 70f), new Vector2(116f, 230f), new Color32(44, 51, 82, 255));
            ImageRect("Pump Accent", pump, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 70f), new Vector2(14f, 230f), settings.accentColor);
            ImageRect("Pump Handle", pump, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -15f), new Vector2(190f, 28f), settings.primaryTextColor);
            ImageRect("Pump Stem", pump, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -58f), new Vector2(14f, 78f), settings.primaryTextColor);
            ImageRect("Pump Foot", pump, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 36f), new Vector2(210f, 24f), settings.primaryTextColor);
            Image hose = ImageRect("Hose", pump, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(-75f, 92f), new Vector2(175f, 7f), settings.mutedTextColor);
            hose.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        }

        private static void CreateFooter(Transform parent, BalloonGameSettings settings, Font font,
            out Text commandText)
        {
            Label("CHAT'E YAZ", parent, font, 14, FontStyle.Bold, settings.mutedTextColor,
                TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(84f, 80f), new Vector2(220f, 30f));
            commandText = Label("!sisir", parent, font, 46, FontStyle.Bold, settings.accentColor,
                TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(82f, 25f), new Vector2(300f, 60f));
            Label("TEST  SPACE: tek oyuncu   •   A: beş oyuncu   •   P: sonraki turda patlat",
                parent, font, 14, FontStyle.Normal, settings.mutedTextColor, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(760f, 28f));
        }

        private static void CreateAmbientShapes(Transform parent, BalloonGameSettings settings)
        {
            Color faint = new Color(settings.backgroundAccentColor.r, settings.backgroundAccentColor.g,
                settings.backgroundAccentColor.b, 0.28f);
            Shape("Ambient Circle Left", parent, EditableShape.Ellipse, new Vector2(0f, 1f),
                new Vector2(-160f, 130f), new Vector2(620f, 620f), faint);
            Shape("Ambient Circle Right", parent, EditableShape.Ellipse, new Vector2(1f, 0.5f),
                new Vector2(245f, 80f), new Vector2(540f, 540f), faint);
            ImageRect("Top Divider", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(84f, -176f), new Vector2(-168f, 1f), new Color(1f, 1f, 1f, 0.1f));
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta)
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

        private static Image ImageRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static EditableShapeGraphic Shape(string name, Transform parent, EditableShape shape,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, anchoredPosition, size);
            rect.gameObject.AddComponent<CanvasRenderer>();
            EditableShapeGraphic graphic = rect.gameObject.AddComponent<EditableShapeGraphic>();
            graphic.shape = shape;
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
        }

        private static Text Label(string text, Transform parent, Font font, int size, FontStyle style,
            Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = Rect(text.Replace("\n", " ") + " Label", parent, anchorMin, anchorMax,
                anchoredPosition, sizeDelta);
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
    }
}
#endif
