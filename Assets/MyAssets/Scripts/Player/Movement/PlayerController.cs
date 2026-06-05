using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour
{
    // Top movement speed in Unity units per second.
    [SerializeField] private float maxSpeed = 4.5f;

    // How quickly the player reaches max speed while input is held.
    [SerializeField] private float acceleration = 55f;

    // How quickly the player stops after input is released.
    [SerializeField] private float deceleration = 70f;

    // Small input values below this are ignored, mainly for analog sticks.
    [SerializeField] private float inputDeadZone = 0.05f;

    // Extra velocity softening. Keep at 0 for the snappiest arcade feel.
    [SerializeField, Range(0f, 0.25f)] private float movementSmoothing = 0f;

    // Removes contact friction so the player slides cleanly along tilemap edges.
    [SerializeField] private bool useZeroFrictionMaterial = true;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private InputManager inputManager;
    private Vector2 currentVelocity;
    private PhysicsMaterial2D zeroFrictionMaterial;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        EnsureCollider();
        ConfigureRigidbody();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        EnsureCollider();
        ConfigureRigidbody();

        inputManager = GetComponent<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError($"{nameof(PlayerController)} on {name} needs an {nameof(InputManager)} on the same GameObject.", this);
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (inputManager == null)
        {
            return;
        }

        Vector2 movementInput = GetCleanMovementInput(inputManager.CurrentMovement);
        Vector2 targetVelocity = movementInput * maxSpeed;
        float velocityChangeRate = movementInput.sqrMagnitude > 0f ? acceleration : deceleration;

        Vector2 nextVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            velocityChangeRate * Time.fixedDeltaTime);

        if (movementSmoothing > 0f)
        {
            nextVelocity = Vector2.Lerp(nextVelocity, currentVelocity, movementSmoothing);
        }

        currentVelocity = nextVelocity;
        body.linearVelocity = currentVelocity;
    }

    private void OnDisable()
    {
        currentVelocity = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private Vector2 GetCleanMovementInput(Vector2 movementInput)
    {
        if (movementInput.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return Vector2.zero;
        }

        return movementInput.sqrMagnitude > 1f ? movementInput.normalized : movementInput;
    }

    private void EnsureCollider()
    {
        bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        ApplyZeroFrictionMaterial();
    }

    private void ConfigureRigidbody()
    {
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void ApplyZeroFrictionMaterial()
    {
        if (!useZeroFrictionMaterial || bodyCollider == null)
        {
            return;
        }

        zeroFrictionMaterial ??= new PhysicsMaterial2D("Player Zero Friction")
        {
            friction = 0f,
            bounciness = 0f
        };

        bodyCollider.sharedMaterial = zeroFrictionMaterial;
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
        inputDeadZone = Mathf.Max(0f, inputDeadZone);
    }
}
