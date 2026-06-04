using System.Collections.Generic;
using System.Linq;
using Portfolio.Cameras;
using Portfolio.Data;
using Portfolio.Interaction;
using Portfolio.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PortfolioInputManager = Portfolio.Input.InputManager;
using PortfolioPlayerController = Portfolio.Player.PlayerController;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Portfolio.Editor
{
    public static class CreatePortfolioPrototypeScene
    {
        private const string ScenePath = "Assets/Portfolio/Scenes/PortfolioHub.unity";
        private const string ExampleProjectPath = "Assets/Portfolio/ScriptableObjects/Projects/ExampleProject.asset";

        [MenuItem("Portfolio/Prototype/Create PortfolioHub Scene")]
        public static void CreateScene()
        {
            PortfolioProjectBootstrap.EnsurePortfolioFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PortfolioHub";

            Material groundMaterial = GetOrCreateMaterial("Assets/Portfolio/Materials/Greybox_Ground.mat", new Color(0.34f, 0.43f, 0.39f));
            Material playerMaterial = GetOrCreateMaterial("Assets/Portfolio/Materials/Greybox_Player.mat", new Color(0.94f, 0.78f, 0.42f));
            Material buildingMaterial = GetOrCreateMaterial("Assets/Portfolio/Materials/Greybox_ProjectBuilding.mat", new Color(0.42f, 0.62f, 0.86f));

            ProjectEntry exampleProject = GetOrCreateExampleProjectEntry();
            ProjectPopupUI popupUI = CreatePortfolioUi();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Greybox";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PortfolioAvatar_Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -4f);
            player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.38f;
            PortfolioInputManager inputManager = player.AddComponent<PortfolioInputManager>();
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                SerializedObject serializedInput = new SerializedObject(inputManager);
                SetObject(serializedInput, "inputActions", inputActions);
                serializedInput.ApplyModifiedPropertiesWithoutUndo();
            }

            PortfolioPlayerController playerController = player.AddComponent<PortfolioPlayerController>();
            SerializedObject serializedPlayer = new SerializedObject(playerController);
            SetObject(serializedPlayer, "inputManager", inputManager);
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera unityCamera = cameraObject.AddComponent<Camera>();
            unityCamera.clearFlags = CameraClearFlags.SolidColor;
            unityCamera.backgroundColor = new Color(0.10f, 0.13f, 0.14f);
            unityCamera.nearClipPlane = 0.1f;
            unityCamera.farClipPlane = 200f;
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

            GameObject followRig = new GameObject("PortfolioCameraFollowRig");
            followRig.transform.position = player.transform.position + new Vector3(0f, 1.35f, 0f);
            followRig.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            GameObject virtualCameraObject = new GameObject("CM Portfolio Third Person Camera");
            CinemachineCamera cinemachineCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            CinemachineThirdPersonFollow thirdPersonFollow = virtualCameraObject.AddComponent<CinemachineThirdPersonFollow>();
            CinemachineRotationComposer rotationComposer = virtualCameraObject.AddComponent<CinemachineRotationComposer>();
            PlayerCameraController cameraController = virtualCameraObject.AddComponent<PlayerCameraController>();

            cinemachineCamera.Follow = followRig.transform;
            cinemachineCamera.LookAt = player.transform;
            cinemachineCamera.Priority.Value = 10;
            cinemachineCamera.Lens.FieldOfView = 55f;
            thirdPersonFollow.CameraDistance = 6.5f;
            thirdPersonFollow.VerticalArmLength = 0.45f;
            thirdPersonFollow.Damping = new Vector3(0.08f, 0.18f, 0.22f);
            rotationComposer.TargetOffset = new Vector3(0f, 1.35f, 0f);
            rotationComposer.Damping = new Vector2(0.12f, 0.12f);

            SerializedObject serializedCamera = new SerializedObject(cameraController);
            SetObject(serializedCamera, "target", player.transform);
            SetObject(serializedCamera, "inputManager", inputManager);
            SetObject(serializedCamera, "followRig", followRig.transform);
            SetObject(serializedCamera, "cinemachineCamera", cinemachineCamera);
            SetObject(serializedCamera, "thirdPersonFollow", thirdPersonFollow);
            SetObject(serializedCamera, "rotationComposer", rotationComposer);
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();

            playerController.CameraTransform = unityCamera.transform;

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject buildingRoot = new GameObject("ProjectBuilding_Example");
            buildingRoot.transform.position = new Vector3(4f, 0f, 1f);
            ProjectBuilding projectBuilding = buildingRoot.AddComponent<ProjectBuilding>();
            projectBuilding.ProjectEntry = exampleProject;
            projectBuilding.PopupUI = popupUI;

            BoxCollider trigger = buildingRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(3.6f, 2.4f, 3.6f);

            GameObject buildingVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingVisual.name = "BuildingVisual_Greybox";
            buildingVisual.transform.SetParent(buildingRoot.transform, false);
            buildingVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
            buildingVisual.transform.localScale = new Vector3(2.2f, 2f, 2.2f);
            buildingVisual.GetComponent<Renderer>().sharedMaterial = buildingMaterial;

            GameObject prompt = CreateWorldPrompt(buildingRoot.transform);
            projectBuilding.InteractionPrompt = prompt;

            RenderSettings.ambientLight = new Color(0.45f, 0.50f, 0.48f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created prototype portfolio scene at {ScenePath}.");
        }

        public static void CreateSceneForBatchmode()
        {
            CreateScene();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static ProjectEntry GetOrCreateExampleProjectEntry()
        {
            ProjectEntry entry = AssetDatabase.LoadAssetAtPath<ProjectEntry>(ExampleProjectPath);
            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<ProjectEntry>();
                AssetDatabase.CreateAsset(entry, ExampleProjectPath);
            }

            SerializedObject serializedEntry = new SerializedObject(entry);
            SetString(serializedEntry, "projectTitle", "Example Portfolio Building");
            SetString(serializedEntry, "shortDescription", "A placeholder project entry for the first walkable portfolio hub.");
            SetString(serializedEntry, "longDescription", "Use this ScriptableObject as the shape for future project buildings. Replace the URLs with the final video, playable build, GitHub, itch.io, and download links.");
            SetString(serializedEntry, "playableUrl", "https://example.com");
            SetString(serializedEntry, "githubUrl", "https://github.com/");
            SetString(serializedEntry, "itchUrl", "https://itch.io/");
            SetEnum(serializedEntry, "status", (int)ProjectStatus.Prototype);
            serializedEntry.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);

            return entry;
        }

        private static ProjectPopupUI CreatePortfolioUi()
        {
            EnsureEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject("PortfolioUI", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform panel = CreateRect("ProjectPopupPanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(560f, 420f), Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.10f, 0.10f, 0.94f);

            Text title = CreateText(panel, "TitleText", font, "Project Title", 30, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), new Vector2(-48f, 54f));
            title.alignment = TextAnchor.MiddleLeft;

            Text status = CreateText(panel, "StatusText", font, "Prototype", 16, FontStyle.Bold, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-82f, -34f), new Vector2(116f, 32f));
            status.alignment = TextAnchor.MiddleCenter;
            status.color = new Color(0.35f, 0.85f, 0.65f);

            Text shortDescription = CreateText(panel, "ShortDescriptionText", font, "Short description", 18, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-48f, 58f));
            shortDescription.alignment = TextAnchor.UpperLeft;

            Text longDescription = CreateText(panel, "LongDescriptionText", font, "Long description", 16, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -182f), new Vector2(-48f, 138f));
            longDescription.alignment = TextAnchor.UpperLeft;

            Button closeButton = CreateButton(panel, "CloseButton", font, "Close", new Vector2(1f, 1f), new Vector2(86f, 36f), new Vector2(-67f, -30f));
            Button videoButton = CreateButton(panel, "VideoButton", font, "Video", new Vector2(0f, 0f), new Vector2(92f, 40f), new Vector2(70f, 38f));
            Button playableButton = CreateButton(panel, "PlayableButton", font, "Play", new Vector2(0f, 0f), new Vector2(92f, 40f), new Vector2(172f, 38f));
            Button githubButton = CreateButton(panel, "GithubButton", font, "GitHub", new Vector2(0f, 0f), new Vector2(104f, 40f), new Vector2(280f, 38f));
            Button itchButton = CreateButton(panel, "ItchButton", font, "Itch", new Vector2(0f, 0f), new Vector2(92f, 40f), new Vector2(388f, 38f));
            Button downloadButton = CreateButton(panel, "DownloadButton", font, "Download", new Vector2(0f, 0f), new Vector2(116f, 40f), new Vector2(504f, 38f));

            ProjectPopupUI popup = canvasObject.AddComponent<ProjectPopupUI>();
            SerializedObject serializedPopup = new SerializedObject(popup);
            SetObject(serializedPopup, "panelRoot", panel.gameObject);
            SetObject(serializedPopup, "titleText", title);
            SetObject(serializedPopup, "shortDescriptionText", shortDescription);
            SetObject(serializedPopup, "longDescriptionText", longDescription);
            SetObject(serializedPopup, "statusText", status);
            SetObject(serializedPopup, "closeButton", closeButton);
            SetObject(serializedPopup, "videoButton", videoButton);
            SetObject(serializedPopup, "playableButton", playableButton);
            SetObject(serializedPopup, "githubButton", githubButton);
            SetObject(serializedPopup, "itchButton", itchButton);
            SetObject(serializedPopup, "downloadButton", downloadButton);
            serializedPopup.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);
            return popup;
        }

        private static GameObject CreateWorldPrompt(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject prompt = new GameObject("InteractionPrompt", typeof(RectTransform));
            prompt.transform.SetParent(parent, false);
            prompt.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            prompt.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            prompt.transform.localScale = Vector3.one * 0.012f;

            Canvas canvas = prompt.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = prompt.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(220f, 56f);

            Image background = prompt.AddComponent<Image>();
            background.color = new Color(0.06f, 0.08f, 0.08f, 0.88f);

            Text label = CreateText(canvasRect, "PromptText", font, "Press E", 32, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.95f, 0.98f, 0.94f);

            prompt.SetActive(false);
            return prompt;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            return rectTransform;
        }

        private static Text CreateText(RectTransform parent, string name, Font font, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            Text textComponent = textObject.GetComponent<Text>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.color = new Color(0.94f, 0.98f, 0.95f);
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            return textComponent;
        }

        private static Button CreateButton(RectTransform parent, string name, Font font, string label, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            RectTransform rectTransform = CreateRect(name, parent, anchor, size, anchoredPosition);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.32f, 0.30f, 1f);

            Button button = rectTransform.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.25f, 0.42f, 0.38f, 1f);
            colors.pressedColor = new Color(0.17f, 0.25f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text buttonText = CreateText(rectTransform, "Label", font, label, 16, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            buttonText.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
