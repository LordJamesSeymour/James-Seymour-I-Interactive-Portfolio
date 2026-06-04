using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Portfolio.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PortfolioAvatarController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private bool faceMovementDirection = true;
        [SerializeField] private float turnSpeed = 16f;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 movementInput = ReadMovementInput();
            Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = (movement * moveSpeed) + (Vector3.up * verticalVelocity);
            characterController.Move(velocity * Time.deltaTime);

            if (faceMovementDirection && movement.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }

        private static Vector2 ReadMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    input.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    input.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    input.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    input.y += 1f;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            input.x += Input.GetAxisRaw("Horizontal");
            input.y += Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
