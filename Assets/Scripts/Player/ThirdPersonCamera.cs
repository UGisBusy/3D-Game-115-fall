using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script for orbit or rotater camera that follows the player.
/// No camera clipping.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    Transform target;

    [SerializeField]
    Vector3 pivotOffset = new(0f, 1.6f, 0f); // point on the target the camera orbits around

    [SerializeField]
    float followSmoothTime = 0.05f; // damping time used by Vector3.SmoothDamp to smoothly follow the player

    [Header("Orbit: Rotation Sensitivity")]
    [SerializeField]
    float mouseSensitivity = 0.15f;

    [SerializeField]
    float gamepadSensitivity = 180f; // degrees per second at full stick tilt

    [SerializeField]
    bool invertY = false;

    [SerializeField]
    float minPitch = -30f;

    [SerializeField]
    float maxPitch = 70f;

    [Header("Camera Distance")]
    [SerializeField]
    float distance = 5f;

    [SerializeField]
    float minDistance = 2f;

    [SerializeField]
    float maxDistance = 8f;

    [SerializeField]
    float zoomSpeed = 0.01f;

    [Header("Collision")]
    [SerializeField]
    float collisionRadius = 0.25f;

    [SerializeField]
    LayerMask collisionMask = ~0;

    [Header("Cursor")]
    [SerializeField]
    bool lockCursor = true; // lock and hide the cursor while playing, esc cancel it.

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    InputAction lookAction; // to store the input system
    float yaw,
        pitch = 15f; // horizontal and vertical angel
    Vector3 pivot,
        pivotVelocity;
    float currentDistance;

    private void Awake()
    {
        lookAction = InputSystem.actions.FindAction("Player/Look", true); // call the input system from the unity
        yaw = transform.eulerAngles.y;
        currentDistance = distance;

        if (target != null)
        {
            pivot = target.position + pivotOffset; // initializes pivot
        }
    }

    private void OnEnable()
    {
        if (lockCursor)
        {
            SetCursorLocked(true);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleCursor();
        HandleLook();
        HandleZoom();

        pivot = Vector3.SmoothDamp(
            pivot,
            target.position + pivotOffset,
            ref pivotVelocity,
            followSmoothTime
        ); // mvoes pivot toward
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f); // converts the pitch and yaw angels into 3D rotation
        Vector3 back = rotation * Vector3.back; // calculates backward direction

        // Pull the camera in if something is between it and the player.
        float wanted = distance;
        float allowed = wanted;

        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            collisionRadius,
            back,
            wanted,
            collisionMask,
            QueryTriggerInteraction.Ignore
        ); // check for blocking geometry

        foreach (var hit in hits) // iterates through hit objects
        {
            if (hit.transform.IsChildOf(target))
            {
                continue;
            }
            if (hit.distance > 0f && hit.distance < allowed)
            {
                allowed = hit.distance; // ignores hits on the player character itself
            }
        }
        // Snap in quickly when blocked, ease back out when clear.
        currentDistance =
            allowed < currentDistance // if camera blocked
                ? allowed // snaps the camera in immediately to avoid clipping
                : Mathf.Lerp(currentDistance, allowed, 10f * Time.deltaTime);

        transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
    }

    private void HandleLook() // input handlers
    {
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 look = lookAction.ReadValue<Vector2>(); // reads the 2D delta vector from mouse movement
        bool fromGamepad = lookAction.activeControl?.device is Gamepad;

        look *= fromGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += look.x;

        pitch += invertY ? look.y : -look.y;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
        {
            return;
        }

        float scroll = Mouse.current.scroll.ReadValue().y; // reads mouse scroll wheel

        if (scroll != 0f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    private void HandleCursor()
    {
        if (!lockCursor)
        {
            return;
        }

        // Lock cursor validation
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // pressed ESC
        {
            SetCursorLocked(false);
        }
        else if (
            Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked
        )
        {
            SetCursorLocked(true);
        }
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;

        Cursor.visible = !locked;
    }
}
