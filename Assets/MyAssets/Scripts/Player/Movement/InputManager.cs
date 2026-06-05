using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputManager : MonoBehaviour
{
    private const string GameplayMapName = "Gameplay";
    private const string MoveActionName = "Move";

    private InputActionMap gameplayActions;
    private InputAction moveAction;
    private Vector2 currentMovement;

    public Vector2 CurrentMovement => currentMovement;
    public bool IsMoving => currentMovement.sqrMagnitude > 0f;

    private void Awake()
    {
        BuildInputActions();
    }

    private void OnEnable()
    {
        if (gameplayActions == null)
        {
            BuildInputActions();
        }

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        gameplayActions.Enable();
    }

    private void OnDisable()
    {
        if (gameplayActions == null || moveAction == null)
        {
            return;
        }

        gameplayActions.Disable();
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;
        currentMovement = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (gameplayActions == null)
        {
            return;
        }

        gameplayActions.Dispose();
        gameplayActions = null;
        moveAction = null;
    }

    private void BuildInputActions()
    {
        gameplayActions = new InputActionMap(GameplayMapName);
        moveAction = gameplayActions.AddAction(MoveActionName, InputActionType.Value);

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        SetMovement(context.ReadValue<Vector2>());
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMovement = Vector2.zero;
    }

    private void SetMovement(Vector2 movement)
    {
        currentMovement = movement.sqrMagnitude > 1f ? movement.normalized : movement;
    }
}
