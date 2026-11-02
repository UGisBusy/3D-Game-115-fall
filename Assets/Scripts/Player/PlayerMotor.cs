using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Physics only. Never decides whether an action is allowed; states and abilities decide that.
    ///
    /// Each FixedUpdate the velocity is built as:
    ///   if override:  velocity = override
    ///   else:         horizontal = moveInput * moveSpeed * speedMultiplier + external
    ///                 vertical   = current y + gravity (if enabled)
    ///
    /// Setup notes:
    ///  - Rigidbody: useGravity = false (custom gravity), freeze rotation X/Y/Z, interpolate, continuous collision.
    ///  - Give the capsule a zero-friction PhysicsMaterial so the Player doesn't stick to walls.
    ///  - Rigidbody mass decides how hard the Player pushes Movable objects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField]
        private float moveSpeed = 4.5f;

        [SerializeField]
        private float turnSpeed = 720f; // degrees / sec

        [Header("Jump & Gravity")]
        [SerializeField]
        private float jumpVelocity = 7.5f;

        [SerializeField]
        private float gravity = -20f;

        [SerializeField]
        private float maxFallSpeed = -22.5f;

        [Header("Ground Check")]
        [SerializeField]
        private Vector3 groundCheckOffset = new(0f, 0.05f, 0f);

        [SerializeField]
        private float groundCheckRadius = 0.225f;

        [SerializeField]
        private LayerMask groundMask;

        [Header("Step")]
        [SerializeField]
        private float maxStepHeight = 0.25f;

        [Header("External Forces")]
        [SerializeField]
        private float externalDamping = 8f; // how fast knockback/impulses fade

        private Rigidbody rb;
        private readonly List<MovementModifier> modifiers = new();

        private Vector3 moveInput; // set by states, flat world direction, length 0..1
        private Vector3? velocityOverride; // dash
        private Vector3 externalVelocity; // knockback, impulses
        private bool gravityEnabled = true;
        private bool jumpRequested;

        private readonly List<ContactPoint> contactBuffer = new();
        private readonly List<Vector3> blockingNormals = new(); // flat normals of too-high contacts, from the last physics step

        // --- Read-only state ---
        public bool IsGrounded { get; private set; }
        public Vector3 Velocity => rb.linearVelocity;
        public bool CanJump => IsGrounded && !IsJumpBlocked;

        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var mod in modifiers)
                    m *= mod.SpeedMultiplier;
                return m;
            }
        }

        public bool IsJumpBlocked
        {
            get
            {
                foreach (var mod in modifiers)
                    if (mod.BlockJump)
                        return true;
                return false;
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.freezeRotation = true;
        }

        // ---------------- Commands (called by states / abilities) ----------------

        // Desired move direction this frame. Pass Vector3.zero to stand still.
        public void SetMoveInput(Vector3 worldDirection) => moveInput = worldDirection;

        // Returns false if not allowed (in the air or blocked by a modifier).
        public bool TryJump()
        {
            if (!CanJump)
                return false;
            jumpRequested = true; // applied in FixedUpdate
            return true;
        }

        public void SetVelocityOverride(Vector3 velocity) => velocityOverride = velocity;

        public void ClearVelocityOverride() => velocityOverride = null;

        public void SetGravityEnabled(bool enabled) => gravityEnabled = enabled;

        // Adds a velocity that fades over time (knockback, blade push).
        public void AddImpulse(Vector3 velocity) => externalVelocity += velocity;

        public void AddModifier(MovementModifier mod)
        {
            if (!modifiers.Contains(mod))
                modifiers.Add(mod);
        }

        public void RemoveModifier(MovementModifier mod) => modifiers.Remove(mod);

        // Stop all motion now (used on death).
        public void Stop()
        {
            moveInput = Vector3.zero;
            externalVelocity = Vector3.zero;
            velocityOverride = null;
            jumpRequested = false;
            rb.linearVelocity = Vector3.zero;
        }

        // Full reset for spawn.
        public void ResetMotor()
        {
            Stop();
            modifiers.Clear();
            gravityEnabled = true;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            blockingNormals.Clear(); // contacts from the old position no longer apply
        }

        // ---------------- Simulation ----------------

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGround();

            // Fade knockback / impulses toward zero.
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, externalDamping * dt);
            if (externalVelocity.sqrMagnitude < 0.0001f)
                externalVelocity = Vector3.zero;

            Vector3 velocity;
            if (velocityOverride.HasValue)
            {
                velocity = velocityOverride.Value;
            }
            else
            {
                Vector3 horizontal = moveInput * (moveSpeed * SpeedMultiplier);
                horizontal += new Vector3(externalVelocity.x, 0f, externalVelocity.z);

                float vertical = rb.linearVelocity.y;
                if (jumpRequested)
                {
                    vertical = jumpVelocity;
                    jumpRequested = false;
                }
                else if (gravityEnabled)
                {
                    vertical = Mathf.Max(vertical + gravity * dt, maxFallSpeed);
                }

                // Vertical part of an impulse is applied once as a pop, then consumed.
                vertical += externalVelocity.y;
                externalVelocity.y = 0f;

                velocity = new Vector3(horizontal.x, vertical, horizontal.z);
            }

            // Contacts were collected after the last physics step; use them once, then clear.
            rb.linearVelocity = RemoveBlockedMovement(velocity);
            blockingNormals.Clear();
            ApplyFacing(velocity, dt); // face where we wanted to go, not the blocked result
        }

        // ---------------- Step limit ----------------

        private void OnCollisionEnter(Collision collision) => CollectBlockingContacts(collision);

        private void OnCollisionStay(Collision collision) => CollectBlockingContacts(collision);

        private void CollectBlockingContacts(Collision collision)
        {
            // Keep pushing Movable objects (non-kinematic rigidbodies).
            if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
                return;

            collision.GetContacts(contactBuffer);
            foreach (ContactPoint contact in contactBuffer)
            {
                if (contact.point.y - rb.position.y <= maxStepHeight)
                    continue; // low edge: allowed to step up onto it

                Vector3 n = contact.normal; // points from the obstacle toward the Player
                n.y = 0f;
                if (n.sqrMagnitude > 0.0001f)
                    blockingNormals.Add(n.normalized);
            }
        }

        // Removes the part of the horizontal velocity that goes into a too-high contact.
        private Vector3 RemoveBlockedMovement(Vector3 v)
        {
            foreach (Vector3 n in blockingNormals)
            {
                float into = v.x * n.x + v.z * n.z;
                if (into < 0f)
                {
                    v.x -= into * n.x;
                    v.z -= into * n.z;
                }
            }
            return v;
        }

        private void ApplyFacing(Vector3 velocity, float dt)
        {
            Vector3? facing = null;
            foreach (var mod in modifiers)
                if (mod.FacingDirection.HasValue)
                    facing = mod.FacingDirection;

            if (!facing.HasValue)
            {
                Vector3 flat = velocityOverride.HasValue ? velocity : moveInput;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    facing = flat;
            }

            if (!facing.HasValue)
                return;

            Quaternion target = Quaternion.LookRotation(facing.Value.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, turnSpeed * dt));
        }

        private bool CheckGround()
        {
            return Physics.CheckSphere(
                transform.position + groundCheckOffset,
                groundCheckRadius,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
        }
    }
}
