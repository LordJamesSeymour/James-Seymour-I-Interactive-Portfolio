using Portfolio.Cameras;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using PortfolioInputManager = Portfolio.Input.InputManager;
using PortfolioPlayerController = Portfolio.Player.PlayerController;

namespace Portfolio.Editor
{
    public static class PortfolioMovementCameraSetup
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PlayerPrefabPath = "Assets/MyAssets/Prefabs/Player/PlayerObj.prefab";

        [MenuItem("Portfolio/Setup/Movement + Camera/Setup Active Scene")]
        public static void SetupActiveScene()
        {
            PortfolioProjectBootstrap.EnsurePortfolioFolders();

            GameObject playerObject = FindOrCreatePlayerObject();
            ConfigurePlayerObject(playerObject, out PortfolioInputManager inputManager, out PortfolioPlayerController playerController);

            Camera mainCamera = FindOrCreateMainCamera();
            ConfigureMainCamera(mainCamera);

            PlayerCameraController cameraController = FindOrCreatePlayerCameraController();
            Transform followRig = FindOrCreateFollowRig(playerObject.transform);
            ConfigurePlayerCamera(cameraController, playerObject.transform, inputManager, followRig);

            playerController.CameraTransform = mainCamera.transform;

            EditorUtility.SetDirty(playerObject);
            EditorUtility.SetDirty(mainCamera.gameObject);
            EditorUtility.SetDirty(cameraController.gameObject);
            EditorSceneManager.MarkSceneDirty(playerObject.scene);

            Debug.Log("Configured portfolio movement input, CharacterController player, and Cinemachine camera in the active scene.");
        }

        [MenuItem("Portfolio/Setup/Movement + Camera/Setup PlayerObj Prefab")]
        public static void SetupPlayerPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ConfigurePlayerObject(prefabRoot, out _, out _);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                Debug.Log($"Configured movement components on {PlayerPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static void SetupActiveSceneForBatchmode()
        {
            SetupActiveScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static void SetupPlayerPrefabForBatchmode()
        {
            SetupPlayerPrefab();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static GameObject FindOrCreatePlayerObject()
        {
            PortfolioPlayerController playerController = Object.FindFirstObjectByType<PortfolioPlayerController>();
            if (playerController != null)
            {
                return playerController.gameObject;
            }

            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            if (taggedPlayer != null)
            {
                return taggedPlayer;
            }

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PortfolioAvatar_Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -4f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            return player;
        }

        private static void ConfigurePlayerObject(
            GameObject playerObject,
            out PortfolioInputManager inputManager,
            out PortfolioPlayerController playerController)
        {
            playerObject.tag = "Player";

            CharacterController characterController = playerObject.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = playerObject.AddComponent<CharacterController>();
            }

            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.38f;
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;

            inputManager = playerObject.GetComponent<PortfolioInputManager>();
            if (inputManager == null)
            {
                inputManager = playerObject.AddComponent<PortfolioInputManager>();
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions != null)
            {
                SerializedObject serializedInput = new SerializedObject(inputManager);
                SetObject(serializedInput, "inputActions", inputActions);
                serializedInput.ApplyModifiedPropertiesWithoutUndo();
            }

            playerController = playerObject.GetComponent<PortfolioPlayerController>();
            if (playerController == null)
            {
                playerController = playerObject.AddComponent<PortfolioPlayerController>();
            }

            SerializedObject serializedPlayer = new SerializedObject(playerController);
            SetObject(serializedPlayer, "inputManager", inputManager);
            SetObject(serializedPlayer, "visualRoot", FindVisualRoot(playerObject));
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindVisualRoot(GameObject playerObject)
        {
            Transform namedVisual = playerObject.transform.Find("PlayerMesh");
            if (namedVisual != null)
            {
                return namedVisual;
            }

            return playerObject.transform.childCount > 0 ? playerObject.transform.GetChild(0) : null;
        }

        private static Camera FindOrCreateMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            Camera anyCamera = Object.FindFirstObjectByType<Camera>();
            if (anyCamera != null)
            {
                anyCamera.tag = "MainCamera";
                return anyCamera;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 4f, -8f);
            camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            return camera;
        }

        private static void ConfigureMainCamera(Camera mainCamera)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.10f, 0.13f, 0.14f);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 200f;

            CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        private static PlayerCameraController FindOrCreatePlayerCameraController()
        {
            PlayerCameraController existing = Object.FindFirstObjectByType<PlayerCameraController>();
            if (existing != null)
            {
                return existing;
            }

            GameObject cameraObject = new GameObject("CM Portfolio Third Person Camera");
            cameraObject.AddComponent<CinemachineCamera>();
            return cameraObject.AddComponent<PlayerCameraController>();
        }

        private static Transform FindOrCreateFollowRig(Transform playerTransform)
        {
            GameObject rigObject = GameObject.Find("PortfolioCameraFollowRig");
            if (rigObject == null)
            {
                rigObject = new GameObject("PortfolioCameraFollowRig");
            }

            rigObject.transform.position = playerTransform.position + new Vector3(0f, 1.35f, 0f);
            rigObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            return rigObject.transform;
        }

        private static void ConfigurePlayerCamera(
            PlayerCameraController playerCameraController,
            Transform target,
            PortfolioInputManager inputManager,
            Transform followRig)
        {
            CinemachineCamera cinemachineCamera = playerCameraController.GetComponent<CinemachineCamera>();
            CinemachineThirdPersonFollow thirdPersonFollow = playerCameraController.GetComponent<CinemachineThirdPersonFollow>();
            if (thirdPersonFollow == null)
            {
                thirdPersonFollow = playerCameraController.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            }

            CinemachineRotationComposer rotationComposer = playerCameraController.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer == null)
            {
                rotationComposer = playerCameraController.gameObject.AddComponent<CinemachineRotationComposer>();
            }

            cinemachineCamera.Follow = followRig;
            cinemachineCamera.LookAt = target;
            cinemachineCamera.Priority.Value = 10;
            cinemachineCamera.Lens.FieldOfView = 55f;

            thirdPersonFollow.CameraDistance = 6.5f;
            thirdPersonFollow.ShoulderOffset = Vector3.zero;
            thirdPersonFollow.VerticalArmLength = 0.45f;
            thirdPersonFollow.Damping = new Vector3(0.08f, 0.18f, 0.22f);

            rotationComposer.TargetOffset = new Vector3(0f, 1.35f, 0f);
            rotationComposer.Damping = new Vector2(0.12f, 0.12f);

            SerializedObject serializedCamera = new SerializedObject(playerCameraController);
            SetObject(serializedCamera, "target", target);
            SetObject(serializedCamera, "inputManager", inputManager);
            SetObject(serializedCamera, "followRig", followRig);
            SetObject(serializedCamera, "cinemachineCamera", cinemachineCamera);
            SetObject(serializedCamera, "thirdPersonFollow", thirdPersonFollow);
            SetObject(serializedCamera, "rotationComposer", rotationComposer);
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
