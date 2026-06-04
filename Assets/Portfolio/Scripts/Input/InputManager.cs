using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Portfolio.Input
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class InputManager : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField, Tooltip("Optional project input asset. If left empty, a small WASD/Space/mouse fallback map is created at runtime.")]
        private InputActionAsset inputActions;

        [SerializeField, Tooltip("Action map that contains player movement, look, and jump actions.")]
        private string playerActionMapName = "Player";

        [SerializeField, Tooltip("Vector2 action used for WASD/left stick movement.")]
        private string moveActionName = "Move";

        [SerializeField, Tooltip("Vector2 action used for mouse delta/right stick look.")]
        private string lookActionName = "Look";

        [SerializeField, Tooltip("Button action used for jumping.")]
        private string jumpActionName = "Jump";

        [Header("Cursor")]
        [SerializeField, Tooltip("Lock and hide the cursor while this input manager is active.")]
        private bool lockCursorOnEnable = true;

        [SerializeField, Tooltip("Allow Escape to release the cursor while testing in editor or browser.")]
        private bool escapeUnlocksCursor = true;

        private InputActionMap activePlayerMap;
        private InputActionMap runtimeFallbackMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private bool isSubscribed;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool JumpReleasedThisFrame { get; private set; }

        public event Action JumpPressed;
        public event Action JumpReleased;

        public InputActionAsset InputActions
        {
            get => inputActions;
            set
            {
                if (inputActions == value)
                {
                    return;
                }

                bool wasActive = isActiveAndEnabled;
                if (wasActive)
                {
                    UnsubscribeAndDisable();
                }

                inputActions = value;
                ResolveActions();

                if (wasActive)
                {
                    SubscribeAndEnable();
                }
            }
        }

        private void Awake()
        {
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            SubscribeAndEnable();

            if (lockCursorOnEnable)
            {
                LockCursor();
            }
        }

        private void OnDisable()
        {
            UnsubscribeAndDisable();
        }

        private void OnDestroy()
        {
            runtimeFallbackMap?.Dispose();
            runtimeFallbackMap = null;
        }

        private void Update()
        {
            MoveInput = Vector2.ClampMagnitude(ReadVector2(moveAction), 1f);
            LookInput = ReadVector2(lookAction);
            JumpHeld = jumpAction != null && jumpAction.IsPressed();

            if (escapeUnlocksCursor && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }
        }

        private void LateUpdate()
        {
            JumpPressedThisFrame = false;
            JumpReleasedThisFrame = false;
        }

        public void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void UnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void ResolveActions()
        {
            activePlayerMap = null;
            moveAction = null;
            lookAction = null;
            jumpAction = null;

            if (inputActions != null)
            {
                activePlayerMap = inputActions.FindActionMap(playerActionMapName, false);
                if (activePlayerMap != null)
                {
                    moveAction = activePlayerMap.FindAction(moveActionName, false);
                    lookAction = activePlayerMap.FindAction(lookActionName, false);
                    jumpAction = activePlayerMap.FindAction(jumpActionName, false);
                }
            }

            if (activePlayerMap == null || moveAction == null || lookAction == null || jumpAction == null)
            {
                CreateRuntimeFallbackMap();
                activePlayerMap = runtimeFallbackMap;
                moveAction = runtimeFallbackMap.FindAction(moveActionName, true);
                lookAction = runtimeFallbackMap.FindAction(lookActionName, true);
                jumpAction = runtimeFallbackMap.FindAction(jumpActionName, true);
            }
        }

        private void SubscribeAndEnable()
        {
            if (activePlayerMap == null || jumpAction == null || isSubscribed)
            {
                activePlayerMap?.Enable();
                return;
            }

            jumpAction.performed += HandleJumpPerformed;
            jumpAction.canceled += HandleJumpCanceled;
            isSubscribed = true;
            activePlayerMap.Enable();
        }

        private void UnsubscribeAndDisable()
        {
            if (jumpAction != null && isSubscribed)
            {
                jumpAction.performed -= HandleJumpPerformed;
                jumpAction.canceled -= HandleJumpCanceled;
            }

            activePlayerMap?.Disable();
            isSubscribed = false;
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            JumpHeld = false;
            JumpPressedThisFrame = false;
            JumpReleasedThisFrame = false;
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            JumpHeld = true;
            JumpPressedThisFrame = true;
            JumpPressed?.Invoke();
        }

        private void HandleJumpCanceled(InputAction.CallbackContext context)
        {
            JumpHeld = false;
            JumpReleasedThisFrame = true;
            JumpReleased?.Invoke();
        }

        private void CreateRuntimeFallbackMap()
        {
            if (runtimeFallbackMap != null)
            {
                return;
            }

            runtimeFallbackMap = new InputActionMap(playerActionMapName);

            InputAction fallbackMove = runtimeFallbackMap.AddAction(moveActionName, InputActionType.Value, expectedControlLayout: "Vector2");
            fallbackMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            fallbackMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            InputAction fallbackLook = runtimeFallbackMap.AddAction(lookActionName, InputActionType.Value, expectedControlLayout: "Vector2");
            fallbackLook.AddBinding("<Pointer>/delta");

            InputAction fallbackJump = runtimeFallbackMap.AddAction(jumpActionName, InputActionType.Button, expectedControlLayout: "Button");
            fallbackJump.AddBinding("<Keyboard>/space");
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
