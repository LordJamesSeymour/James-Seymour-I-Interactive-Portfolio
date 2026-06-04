using Unity.Cinemachine;
using UnityEngine;
using PortfolioInputManager = Portfolio.Input.InputManager;
using PortfolioPlayerController = Portfolio.Player.PlayerController;

namespace Portfolio.Cameras
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Player or object the camera orbits.")]
        private Transform target;

        [SerializeField, Tooltip("Input source for mouse/right-stick look.")]
        private PortfolioInputManager inputManager;

        [SerializeField, Tooltip("Pivot used by Cinemachine Third Person Follow.")]
        private Transform followRig;

        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
        [SerializeField] private CinemachineRotationComposer rotationComposer;

        [Header("Look")]
        [SerializeField, Tooltip("Initial yaw in degrees.")]
        private float initialYaw;

        [SerializeField, Tooltip("Initial pitch in degrees.")]
        private float initialPitch = 18f;

        [SerializeField, Tooltip("Mouse delta sensitivity in degrees per pixel.")]
        private Vector2 mouseSensitivity = new Vector2(0.14f, 0.12f);

        [SerializeField, Tooltip("Stick/continuous look speed in degrees per second.")]
        private Vector2 continuousLookSpeed = new Vector2(180f, 130f);

        [SerializeField, Tooltip("Invert vertical look input.")]
        private bool invertY;

        [SerializeField, Tooltip("Minimum and maximum pitch in degrees.")]
        private Vector2 pitchLimits = new Vector2(-12f, 58f);

        [SerializeField, Range(0f, 0.4f), Tooltip("Smoothing time for horizontal orbit. Keep 0 for direct prototype camera response.")]
        private float yawSmoothTime = 0f;

        [SerializeField, Range(0f, 0.4f), Tooltip("Smoothing time for vertical orbit. Keep 0 for direct prototype camera response.")]
        private float pitchSmoothTime = 0f;

        [Header("Cinemachine Rig")]
        [SerializeField, Tooltip("World-space focus offset from the target position.")]
        private Vector3 targetOffset = new Vector3(0f, 1.35f, 0f);

        [SerializeField, Min(0.1f), Tooltip("Distance behind the follow rig.")]
        private float cameraDistance = 6.5f;

        [SerializeField, Tooltip("Horizontal shoulder offset. Keep near zero for a neutral portfolio camera.")]
        private float shoulderOffset = 0f;

        [SerializeField, Tooltip("Vertical arm length used by Cinemachine Third Person Follow.")]
        private float verticalArmLength = 0.45f;

        [SerializeField, Tooltip("Cinemachine follow damping on local X/Y/Z.")]
        private Vector3 followDamping = Vector3.zero;

        [SerializeField, Tooltip("Cinemachine aim damping on screen X/Y.")]
        private Vector2 aimDamping = Vector2.zero;

        [SerializeField, Tooltip("Locks the player to the centre of the frame by disabling Cinemachine aim recentering and damping.")]
        private bool lockTargetToCenter = true;

        [SerializeField, Range(35f, 80f), Tooltip("Virtual camera field of view.")]
        private float fieldOfView = 55f;

        private float yaw;
        private float pitch;
        private float smoothedYaw;
        private float smoothedPitch;
        private float yawVelocity;
        private float pitchVelocity;
        private bool anglesInitialized;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                ResolveSceneReferences();
                ConfigureCinemachine();
            }
        }

        public PortfolioInputManager InputManager
        {
            get => inputManager;
            set => inputManager = value;
        }

        public Transform FollowRig
        {
            get => followRig;
            set => followRig = value;
        }

        public float Yaw => smoothedYaw;

        private void Reset()
        {
            ResolveCinemachineReferences();
        }

        private void Awake()
        {
            ResolveCinemachineReferences();
            ResolveSceneReferences();
            InitializeAngles();
            ConfigureCinemachine();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            EnsureFollowRig();
            UpdateLookAngles(Time.deltaTime);
            UpdateFollowRig();
            ConfigureCinemachine();
            ShareCameraReferenceWithPlayer();
        }

        private void ResolveCinemachineReferences()
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            if (thirdPersonFollow == null)
            {
                thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();
            }

            if (thirdPersonFollow == null)
            {
                thirdPersonFollow = gameObject.AddComponent<CinemachineThirdPersonFollow>();
            }

            if (rotationComposer == null)
            {
                rotationComposer = GetComponent<CinemachineRotationComposer>();
            }

            if (rotationComposer == null)
            {
                rotationComposer = gameObject.AddComponent<CinemachineRotationComposer>();
            }
        }

        private void ResolveSceneReferences()
        {
            if (target == null)
            {
                PortfolioPlayerController player = FindFirstObjectByType<PortfolioPlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (inputManager == null && target != null)
            {
                inputManager = target.GetComponent<PortfolioInputManager>();
            }
        }

        private void InitializeAngles()
        {
            if (anglesInitialized)
            {
                return;
            }

            yaw = initialYaw;
            pitch = Mathf.Clamp(initialPitch, pitchLimits.x, pitchLimits.y);
            smoothedYaw = yaw;
            smoothedPitch = pitch;
            anglesInitialized = true;
        }

        private void UpdateLookAngles(float deltaTime)
        {
            Vector2 lookInput = inputManager != null ? inputManager.LookInput : Vector2.zero;
            if (lookInput.sqrMagnitude > 0.0001f)
            {
                if (LooksLikeContinuousInput(lookInput))
                {
                    yaw += lookInput.x * continuousLookSpeed.x * deltaTime;
                    pitch += GetSignedVerticalLook(lookInput.y) * continuousLookSpeed.y * deltaTime;
                }
                else
                {
                    yaw += lookInput.x * mouseSensitivity.x;
                    pitch += GetSignedVerticalLook(lookInput.y) * mouseSensitivity.y;
                }

                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }

            float effectiveYawSmoothTime = lockTargetToCenter ? 0f : yawSmoothTime;
            float effectivePitchSmoothTime = lockTargetToCenter ? 0f : pitchSmoothTime;

            smoothedYaw = effectiveYawSmoothTime <= 0f
                ? yaw
                : Mathf.SmoothDampAngle(smoothedYaw, yaw, ref yawVelocity, effectiveYawSmoothTime);

            smoothedPitch = effectivePitchSmoothTime <= 0f
                ? pitch
                : Mathf.SmoothDampAngle(smoothedPitch, pitch, ref pitchVelocity, effectivePitchSmoothTime);
        }

        private float GetSignedVerticalLook(float inputY)
        {
            return invertY ? inputY : -inputY;
        }

        private static bool LooksLikeContinuousInput(Vector2 lookInput)
        {
            return Mathf.Abs(lookInput.x) <= 1.25f && Mathf.Abs(lookInput.y) <= 1.25f;
        }

        private void EnsureFollowRig()
        {
            if (followRig != null)
            {
                return;
            }

            GameObject rigObject = new GameObject("PortfolioCameraFollowRig");
            followRig = rigObject.transform;
        }

        private void UpdateFollowRig()
        {
            followRig.position = target.position + targetOffset;
            followRig.rotation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0f);
        }

        private void ConfigureCinemachine()
        {
            ResolveCinemachineReferences();

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = followRig != null ? followRig : target;
                cinemachineCamera.LookAt = lockTargetToCenter ? null : target;
                cinemachineCamera.Lens.FieldOfView = fieldOfView;
            }

            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.ShoulderOffset = new Vector3(shoulderOffset, 0f, 0f);
                thirdPersonFollow.VerticalArmLength = verticalArmLength;
                thirdPersonFollow.CameraDistance = cameraDistance;
                thirdPersonFollow.Damping = lockTargetToCenter ? Vector3.zero : followDamping;
            }

            if (rotationComposer != null)
            {
                rotationComposer.enabled = !lockTargetToCenter;
                rotationComposer.TargetOffset = targetOffset;
                rotationComposer.Damping = lockTargetToCenter ? Vector2.zero : aimDamping;
            }
        }

        private void ShareCameraReferenceWithPlayer()
        {
            if (target == null || !target.TryGetComponent(out PortfolioPlayerController playerController))
            {
                return;
            }

            Camera mainCamera = Camera.main;
            playerController.CameraTransform = mainCamera != null ? mainCamera.transform : transform;
        }

        private void OnValidate()
        {
            pitchLimits.y = Mathf.Max(pitchLimits.x, pitchLimits.y);
            cameraDistance = Mathf.Max(0.1f, cameraDistance);
            fieldOfView = Mathf.Clamp(fieldOfView, 35f, 80f);
            followDamping.x = Mathf.Max(0f, followDamping.x);
            followDamping.y = Mathf.Max(0f, followDamping.y);
            followDamping.z = Mathf.Max(0f, followDamping.z);
            aimDamping.x = Mathf.Max(0f, aimDamping.x);
            aimDamping.y = Mathf.Max(0f, aimDamping.y);

            if (Application.isPlaying)
            {
                ConfigureCinemachine();
            }
        }
    }
}
