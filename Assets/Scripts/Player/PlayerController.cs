using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person player movement:
///   Move = WASD
///   Jump = Space
///   Crouch = C
///   Dash = Left Shift
///   Attack = Left Mouse
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    Transform cameraTransform; // camera used for camera-relative movement

    [SerializeField]
    Transform crouchVisual; // placeholder model that gets squashed when crouching

    [Header("Movement")]
    [SerializeField]
    float walkSpeed = 5f;

    [SerializeField]
    float crouchSpeed = 2.5f;

    [SerializeField]
    float acceleration = 30f;

    [SerializeField]
    float rotationSpeed = 720f; // degrees per second the player turns

    [Header("Jump & Gravity")]
    [SerializeField]
    float jumpHeight = 1.5f;

    [SerializeField]
    float gravity = -20f;

    [SerializeField]
    float maxFallSpeed = -50f;

    [SerializeField]
    float coyoteTime = 0.1f; // small time after walking off a ledge

    [Header("Crouch")]
    [SerializeField]
    bool crouchIsToggle = false;

    [SerializeField]
    float standingHeight = 2f;

    [SerializeField]
    float crouchHeight = 1.2f;

    [SerializeField]
    float crouchTransitionSpeed = 10f;

    [Header("Dash")]
    [SerializeField]
    float dashSpeed = 20f;

    [SerializeField]
    float dashDuration = 0.5f;

    [SerializeField]
    float dashCooldown = 3f;

    [Tooltip("Snap the character to face the dash direction")]
    [SerializeField]
    bool faceDashDirection = true;

    [Header("Attack")]
    [SerializeField]
    float attackDamage = 10f;

    [SerializeField]
    float attackCooldown = 0.5f;

    [SerializeField]
    float attackRange = 1.2f;

    [SerializeField]
    float attackRadius = 0.8f;

    [SerializeField]
    LayerMask attackMask = ~0;

    [Tooltip("Turn on = Turn to camera forward when attacking")]
    [SerializeField]
    bool faceCameraOnAttack = false;

    // Internal state variables
    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsDashing { get; private set; }
    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
    public float DashCooldownRemaining => Mathf.Max(0f, lastDashTime + dashCooldown - Time.time);

    CharacterController controller;
    InputAction moveAction,
        jumpAction,
        crouchAction,
        dashAction,
        attackAction;

    Vector3 horizontalVelocity;
    float verticalVelocity;
    float lastGroundedTime = float.NegativeInfinity;
    bool wantsCrouch;

    Vector3 dashDirection;
    float dashTimer;
    float lastDashTime = float.NegativeInfinity;
    float lastAttackTime = float.NegativeInfinity;

    Renderer[] renderers;
    readonly List<Renderer> hiddenRenderers = new();
    Vector3 crouchVisualBaseScale,
        crouchVisualBasePos;
    readonly Collider[] overlapBuffer = new Collider[32];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        renderers = GetComponentsInChildren<Renderer>(true);

        if (crouchVisual != null)
        {
            crouchVisualBaseScale = crouchVisual.localScale;
            crouchVisualBasePos = crouchVisual.localPosition;
        }

        SetControllerHeight(standingHeight);

        var actions = InputSystem.actions; // calling the input system file in the unity
        moveAction = actions.FindAction("Player/Move", true);
        jumpAction = actions.FindAction("Player/Jump", true);
        crouchAction = actions.FindAction("Player/Crouch", true);
        dashAction = actions.FindAction("Player/Sprint", true);
        attackAction = actions.FindAction("Player/Attack", true);
    }

    private void OnEnable() => InputSystem.actions.FindActionMap("Player", true).Enable(); // enables the player input map

    private void OnDisable()
    {
        if (IsDashing)
        {
            EndDash(); // reset dash state and visibility
        }
    }

    private void Update()
    {
        if (IsDashing)
        {
            UpdateDash();
            return;
        }

        HandleCrouch();
        HandleMovement();
        HandleJumpAndGravity();

        Vector3 motion = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime; // crouching logic
        CollisionFlags flags = controller.Move(motion);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) // if the character hits the ceiling
        {
            verticalVelocity = 0f;
        }

        UpdateGrounded(); // update status flag

        if (dashAction.WasPressedThisFrame() && DashCooldownRemaining <= 0f)
        {
            StartDash();
        }
        else if (attackAction.WasPressedThisFrame() && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    // ---------------- Movement ----------------

    private void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f); // reads the movement input (A/W/S/D)
        Vector3 forward = GetCameraFlatForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward); // converts screen-relative 2D direction into camera-relative 3D world direction
        Vector3 moveDir = forward * input.y + right * input.x;

        float speed = IsCrouching ? crouchSpeed : walkSpeed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            moveDir * speed,
            acceleration * Time.deltaTime
        ); // smooth acceleration

        // Rotate to face where we are moving
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void HandleJumpAndGravity()
    {
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // keep the controller pressed onto the ground
        }

        bool canJump = Time.time - lastGroundedTime <= coyoteTime; // grounded covers standing on the floor

        if (jumpAction.WasPressedThisFrame() && canJump && !IsCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // calculates vertical jump using standard kinematic formula
            lastGroundedTime = float.NegativeInfinity;
            IsGrounded = false;
        }

        verticalVelocity = Mathf.Max(verticalVelocity + gravity * Time.deltaTime, maxFallSpeed); // applies gravity
    }

    private void UpdateGrounded() // update grounded state and updates coyote timer
    {
        IsGrounded = controller.isGrounded;

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    // ---------------- Crouch ----------------

    private void HandleCrouch()
    {
        if (crouchIsToggle)
        {
            if (crouchAction.WasPressedThisFrame())
            {
                wantsCrouch = !wantsCrouch;
            }
        }
        else
        {
            wantsCrouch = crouchAction.IsPressed();
        }

        if (wantsCrouch)
        {
            IsCrouching = true; // initiates crouch
        }
        else if (IsCrouching && CanStandUp()) // if no ceiling
        {
            IsCrouching = false;
        }

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;

        if (!Mathf.Approximately(controller.height, targetHeight))
        {
            SetControllerHeight(
                Mathf.MoveTowards(
                    controller.height,
                    targetHeight,
                    crouchTransitionSpeed * Time.deltaTime
                )
            ); // smoothly transitions character height over time
        }
    }

    private void SetControllerHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height * 0.5f, 0f);

        if (crouchVisual != null)
        {
            float ratio = height / standingHeight;

            crouchVisual.localScale = new Vector3(
                crouchVisualBaseScale.x,
                crouchVisualBaseScale.y * ratio,
                crouchVisualBaseScale.z
            );
            crouchVisual.localPosition = new Vector3(
                crouchVisualBasePos.x,
                crouchVisualBasePos.y * ratio,
                crouchVisualBasePos.z
            );
        }
    }

    private bool CanStandUp() // set bool that true when can standup
    {
        float radius = controller.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (controller.height - radius);
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);

        int count = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            overlapBuffer,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] != controller && !overlapBuffer[i].transform.IsChildOf(transform))
            {
                return false;
            }
        }
        return true;
    }

    // ---------------- Dash ----------------

    private void StartDash()
    {
        dashDirection = GetCameraFlatForward();
        IsDashing = true;
        dashTimer = dashDuration;
        lastDashTime = Time.time;
        verticalVelocity = 0f; // gravity is off during the dash

        if (faceDashDirection)
        {
            transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);
        }

        SetVisible(false);
    }

    private void UpdateDash()
    {
        controller.Move(dashDirection * (dashSpeed * Time.deltaTime)); // only move along the dash direction
        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        IsDashing = false;
        horizontalVelocity = dashDirection * walkSpeed; // keep a bit of momentum
        verticalVelocity = 0f;
        SetVisible(true);
        UpdateGrounded();
    }

    private void SetVisible(bool visible)
    {
        if (!visible)
        {
            hiddenRenderers.Clear();
            foreach (var r in renderers)
            {
                if (r != null && r.enabled)
                {
                    r.enabled = false;
                    hiddenRenderers.Add(r);
                }
            }
        }
        else
        {
            foreach (var r in hiddenRenderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }

            hiddenRenderers.Clear();
        }
    }

    // ---------------- Attack ----------------

    private void Attack()
    {
        lastAttackTime = Time.time;
        Debug.Log("[PlayerController] Attack!!!!");

        if (faceCameraOnAttack)
        {
            transform.rotation = Quaternion.LookRotation(GetCameraFlatForward(), Vector3.up);
        }

        Vector3 center = GetAttackCenter();
        int count = Physics.OverlapSphereNonAlloc(
            center,
            attackRadius,
            overlapBuffer,
            attackMask,
            QueryTriggerInteraction.Collide
        );
        var alreadyHit = new HashSet<IDamageable>();

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].transform.IsChildOf(transform))
            {
                continue;
            }

            var target = overlapBuffer[i].GetComponentInParent<IDamageable>();

            if (target != null && alreadyHit.Add(target))
            {
                target.TakeDamage(attackDamage, gameObject);
            }
        }
    }

    private Vector3 GetAttackCenter() =>
        transform.position
        + Vector3.up * (controller != null ? controller.height * 0.5f : 1f)
        + transform.forward * attackRange;

    private Vector3 GetCameraFlatForward()
    {
        if (cameraTransform == null)
        {
            return transform.forward;
        }

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

        return forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackCenter(), attackRadius);
    }
}
