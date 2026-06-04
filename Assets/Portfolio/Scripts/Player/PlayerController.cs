using UnityEngine;
using PortfolioInputManager = Portfolio.Input.InputManager;

namespace Portfolio.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PortfolioInputManager))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Reads movement and jump actions. Kept separate from movement so input can expand later.")]
        private PortfolioInputManager inputManager;

        [SerializeField, Tooltip("Camera used to make movement relative to the current view.")]
        private Transform cameraTransform;

        [SerializeField, Tooltip("Optional child visual to rotate instead of rotating the whole player object.")]
        private Transform visualRoot;

        [SerializeField, Tooltip("When enabled, keeps a child visual such as PlayerMesh centered in the CharacterController instead of half below the floor.")]
        private bool alignVisualRootToController = true;

        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Top horizontal walking speed in metres per second.")]
        private float moveSpeed = 5.5f;

        [SerializeField, Min(0f), Tooltip("How quickly the player reaches target speed while grounded.")]
        private float groundedAcceleration = 34f;

        [SerializeField, Min(0f), Tooltip("How quickly the player can steer while airborne.")]
        private float airAcceleration = 14f;

        [SerializeField, Tooltip("Rotate the character to face its current horizontal movement direction.")]
        private bool rotateTowardMovement = true;

        [SerializeField, Min(0.01f), Tooltip("Higher values turn the player faster.")]
        private float rotationSharpness = 14f;

        [Header("Jumping")]
        [SerializeField, Min(0f), Tooltip("Jump apex height in metres.")]
        private float jumpHeight = 1.35f;

        [SerializeField, Tooltip("Gravity applied to this controller.")]
        private float gravity = -24f;

        [SerializeField, Min(0f), Tooltip("Maximum downward speed.")]
        private float terminalFallSpeed = 42f;

        [SerializeField, Range(0f, 0.4f), Tooltip("Time after leaving ground where jump is still allowed.")]
        private float coyoteTime = 0.12f;

        [SerializeField, Range(0f, 0.4f), Tooltip("Time a jump press can wait for the player to become grounded.")]
        private float jumpBufferTime = 0.15f;

        [SerializeField, Tooltip("Allow early jump release to shorten the jump.")]
        private bool enableJumpCut = true;

        [SerializeField, Range(0.1f, 1f), Tooltip("Vertical velocity multiplier applied when jump is released early.")]
        private float jumpCutVelocityMultiplier = 0.55f;

        [Header("Grounding")]
        [SerializeField, Tooltip("Layers considered walkable ground.")]
        private LayerMask groundLayers = ~0;

        [SerializeField, Tooltip("Optional probe point. If empty, one is derived from the CharacterController.")]
        private Transform groundProbe;

        [SerializeField, Min(0.01f), Tooltip("Overlap sphere radius for reliable grounded checks.")]
        private float groundProbeRadius = 0.28f;

        [SerializeField, Tooltip("Small vertical offset from the controller foot sphere center.")]
        private float groundProbeVerticalOffset = -0.08f;

        [SerializeField, Tooltip("Small downward velocity used to keep the controller settled on ground.")]
        private float groundedStickVelocity = -2f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Briefly ignore ground after jumping so the takeoff is not immediately cancelled.")]
        private float jumpGroundingLockTime = 0.08f;

        private readonly Collider[] groundHits = new Collider[12];
        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float groundingLockTimer;
        private int lastJumpBufferedFrame = -1;
        private int lastJumpCutFrame = -1;
        private const string DefaultVisualRootName = "PlayerMesh";

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity => horizontalVelocity + (Vector3.up * verticalVelocity);
        public Vector2 MoveInput => inputManager != null ? inputManager.MoveInput : Vector2.zero;

        public PortfolioInputManager InputManager
        {
            get => inputManager;
            set => inputManager = value;
        }

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (inputManager != null)
            {
                inputManager.JumpPressed += BufferJump;
                inputManager.JumpReleased += TryApplyJumpCut;
            }
        }

        private void OnDisable()
        {
            if (inputManager != null)
            {
                inputManager.JumpPressed -= BufferJump;
                inputManager.JumpReleased -= TryApplyJumpCut;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            ResolveCameraReference();
            MirrorInputFrameFlags();
            UpdateJumpTimers(deltaTime);
            UpdateGroundedState(deltaTime);
            TryConsumeBufferedJump();
            ApplyGravity(deltaTime);
            MoveCharacter(MoveInput, deltaTime);
            RotateCharacter(deltaTime);
        }

        private void ResolveReferences()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (inputManager == null)
            {
                inputManager = GetComponent<PortfolioInputManager>();
            }

            ResolveVisualRoot();
            AlignVisualRootToController();
            ResolveCameraReference();
        }

        private void ResolveCameraReference()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null || transform.childCount == 0)
            {
                return;
            }

            Transform namedVisualRoot = transform.Find(DefaultVisualRootName);
            visualRoot = namedVisualRoot != null ? namedVisualRoot : transform.GetChild(0);
        }

        private void AlignVisualRootToController()
        {
            if (!alignVisualRootToController || visualRoot == null || visualRoot == transform || characterController == null)
            {
                return;
            }

            Vector3 localPosition = visualRoot.localPosition;
            localPosition.y = characterController.center.y;
            visualRoot.localPosition = localPosition;
        }

        private void MirrorInputFrameFlags()
        {
            if (inputManager == null)
            {
                return;
            }

            if (inputManager.JumpPressedThisFrame && lastJumpBufferedFrame != Time.frameCount)
            {
                BufferJump();
            }

            if (inputManager.JumpReleasedThisFrame && lastJumpCutFrame != Time.frameCount)
            {
                TryApplyJumpCut();
            }
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);
            }

            if (groundingLockTimer > 0f)
            {
                groundingLockTimer = Mathf.Max(0f, groundingLockTimer - deltaTime);
            }
        }

        private void UpdateGroundedState(float deltaTime)
        {
            bool groundedNow = groundingLockTimer <= 0f && ProbeGround();
            IsGrounded = groundedNow;

            if (groundedNow)
            {
                coyoteTimer = coyoteTime;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = groundedStickVelocity;
                }
            }
            else
            {
                coyoteTimer = Mathf.Max(0f, coyoteTimer - deltaTime);
            }
        }

        private void TryConsumeBufferedJump()
        {
            if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            {
                return;
            }

            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity = jumpVelocity;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            groundingLockTimer = jumpGroundingLockTime;
            IsGrounded = false;
        }

        private void ApplyGravity(float deltaTime)
        {
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
                return;
            }

            verticalVelocity += gravity * deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -terminalFallSpeed);
        }

        private void MoveCharacter(Vector2 moveInput, float deltaTime)
        {
            Vector3 desiredDirection = GetCameraRelativeMoveDirection(moveInput);
            float targetSpeed = moveSpeed * Mathf.Clamp01(moveInput.magnitude);
            Vector3 targetHorizontalVelocity = desiredDirection * targetSpeed;
            float acceleration = IsGrounded ? groundedAcceleration : airAcceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * deltaTime);

            Vector3 motion = (horizontalVelocity + (Vector3.up * verticalVelocity)) * deltaTime;
            CollisionFlags flags = characterController.Move(motion);

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }

            if ((flags & CollisionFlags.Below) != 0 && groundingLockTimer <= 0f)
            {
                IsGrounded = true;
                coyoteTimer = coyoteTime;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = groundedStickVelocity;
                }
            }
        }

        private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Transform reference = cameraTransform != null ? cameraTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up);

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            forward.Normalize();
            right.Normalize();

            Vector3 direction = (forward * moveInput.y) + (right * moveInput.x);
            return Vector3.ClampMagnitude(direction, 1f);
        }

        private void RotateCharacter(float deltaTime)
        {
            if (!rotateTowardMovement)
            {
                return;
            }

            Vector3 flatVelocity = Vector3.ProjectOnPlane(horizontalVelocity, Vector3.up);
            if (flatVelocity.sqrMagnitude <= 0.0025f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
            Transform root = visualRoot != null ? visualRoot : transform;
            float turnBlend = 1f - Mathf.Exp(-rotationSharpness * deltaTime);
            root.rotation = Quaternion.Slerp(root.rotation, targetRotation, turnBlend);
        }

        private void BufferJump()
        {
            jumpBufferTimer = jumpBufferTime;
            lastJumpBufferedFrame = Time.frameCount;
        }

        private void TryApplyJumpCut()
        {
            lastJumpCutFrame = Time.frameCount;
            if (!enableJumpCut || verticalVelocity <= 0f)
            {
                return;
            }

            verticalVelocity *= jumpCutVelocityMultiplier;
        }

        private bool ProbeGround()
        {
            if (characterController != null && characterController.isGrounded)
            {
                return true;
            }

            Vector3 probePosition = groundProbe != null ? groundProbe.position : GetDefaultGroundProbePosition();
            int hitCount = Physics.OverlapSphereNonAlloc(
                probePosition,
                groundProbeRadius,
                groundHits,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundHits[i];
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetDefaultGroundProbePosition()
        {
            if (characterController == null)
            {
                return transform.position + (Vector3.up * groundProbeVerticalOffset);
            }

            float footSphereCenterY = characterController.center.y - (characterController.height * 0.5f) + characterController.radius;
            Vector3 localProbe = new Vector3(
                characterController.center.x,
                footSphereCenterY + groundProbeVerticalOffset,
                characterController.center.z);

            return transform.TransformPoint(localProbe);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            groundedAcceleration = Mathf.Max(0f, groundedAcceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            rotationSharpness = Mathf.Max(0.01f, rotationSharpness);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-0.01f, gravity);
            terminalFallSpeed = Mathf.Max(0f, terminalFallSpeed);
            groundProbeRadius = Mathf.Max(0.01f, groundProbeRadius);
            groundedStickVelocity = Mathf.Min(-0.01f, groundedStickVelocity);
            jumpGroundingLockTime = Mathf.Max(0f, jumpGroundingLockTime);

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            ResolveVisualRoot();
            AlignVisualRootToController();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Vector3 probePosition = groundProbe != null ? groundProbe.position : GetDefaultGroundProbePosition();
            Gizmos.DrawWireSphere(probePosition, groundProbeRadius);
        }
    }
}
