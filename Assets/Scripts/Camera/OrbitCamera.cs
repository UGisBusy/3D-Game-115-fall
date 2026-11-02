using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CameraSystem
{
    /// <summary>
    /// Third-person orbit camera.
    ///  - Mouse rotates the camera around a pivot (target position + pivotOffset).
    ///  - Scroll zooms between minDistance and maxDistance.
    ///  - Walls between the pivot and the camera pull it in; it eases back out when clear.
    ///  - The target's model is hidden (shadow kept) when the camera gets very close.
    /// Put it on the Main Camera. Do not parent the camera to the target.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector3 pivotOffset = new(0f, 1.2f, 0f);

        [Header("Input")]
        [SerializeField]
        private InputActionReference lookAction; // Value, Vector2 (Mouse delta)

        [SerializeField]
        private InputActionReference zoomAction; // Value, Axis (Mouse scroll Y)

        [Header("Rotation")]
        [SerializeField]
        private float mouseSensitivity = 0.15f;

        [SerializeField]
        private bool invertY;

        [SerializeField, Range(-89f, 89f)]
        private float minXRotation = -30f;

        [SerializeField, Range(-89f, 89f)]
        private float maxXRotation = 70f;

        [SerializeField]
        private bool lockCursor = true;

        [Header("Zoom")]
        [SerializeField]
        private float distance = 4f;

        [SerializeField]
        private float minDistance = 0f;

        [SerializeField]
        private float maxDistance = 4f;

        [SerializeField]
        private float zoomStep = 0.5f;

        [SerializeField]
        private float zoomSmoothTime = 0.1f;

        [Header("Collision")]
        [SerializeField]
        private LayerMask collisionMask = ~0;

        [Tooltip("Keep this bigger than the camera's near clip plane so walls are never cut open.")]
        [SerializeField]
        private float collisionRadius = 0.2f;

        [Tooltip("How fast the camera moves back out once the obstacle is gone (units / sec).")]
        [SerializeField]
        private float collisionRecoverSpeed = 6f;

        [Header("Hide Model")]
        [Tooltip("Renderers under this are hidden when the camera is close. Empty = the target.")]
        [SerializeField]
        private Transform modelRoot;

        [Tooltip("Hide the model when the camera is closer than this to the pivot.")]
        [SerializeField]
        private float hideDistance = 0.6f;

        public Transform Target => target;

        private float yaw;
        private float pitch;

        private float zoomTarget; // distance chosen by scrolling
        private float zoom; // zoomTarget, smoothed
        private float zoomVelocity;
        private float collisionPull; // how far walls currently pull the camera in from zoom
        private float currentDistance; // final distance: zoom - collisionPull

        private readonly RaycastHit[] hits = new RaycastHit[16];

        private Renderer[] renderers = Array.Empty<Renderer>();
        private ShadowCastingMode[] shadowModes = Array.Empty<ShadowCastingMode>();
        private bool modelHidden;

        private void Awake()
        {
            // Start from the rotation the camera was given in the scene.
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), minXRotation, maxXRotation);

            zoomTarget = zoom = currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            RefreshModelRenderers();
        }

        private void OnEnable()
        {
            if (lookAction == null || zoomAction == null)
            {
                Debug.LogError($"{nameof(OrbitCamera)}: assign Look Action and Zoom Action.", this);
                enabled = false;
                return;
            }

            lookAction.action.Enable();
            zoomAction.action.Enable();
            if (lockCursor)
                SetCursorLocked(true);
        }

        private void OnDisable()
        {
            // Unity objects need == null, not ?. (a "Missing" reference is not C# null).
            if (lookAction != null)
                lookAction.action.Disable();
            if (zoomAction != null)
                zoomAction.action.Disable();
            SetModelHidden(false);
            if (lockCursor)
                SetCursorLocked(false);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            HandleCursor();
            if (!lockCursor || Cursor.lockState == CursorLockMode.Locked)
            {
                HandleRotation();
                HandleZoom();
            }
            zoom = Mathf.SmoothDamp(zoom, zoomTarget, ref zoomVelocity, zoomSmoothTime);

            Vector3 pivot = target.position + pivotOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Snap in at once when blocked (no clipping), ease back out when clear.
            float wantedPull = zoom - GetAllowedDistance(pivot, back, zoom);
            collisionPull =
                wantedPull > collisionPull
                    ? wantedPull
                    : Mathf.MoveTowards(
                        collisionPull,
                        wantedPull,
                        collisionRecoverSpeed * Time.deltaTime
                    );
            currentDistance = zoom - collisionPull;

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
            SetModelHidden(currentDistance < hideDistance);
        }

        // ---------------- Public API ----------------

        /// <summary>Follow a new target. modelRoot = renderers to hide when close (null = the target).</summary>
        public void SetTarget(Transform newTarget, Transform newModelRoot = null)
        {
            SetModelHidden(false);
            target = newTarget;
            modelRoot = newModelRoot;
            RefreshModelRenderers();
        }

        /// <summary>Call after adding or removing renderers on the model (e.g. equipping a weapon).</summary>
        public void RefreshModelRenderers()
        {
            bool wasHidden = modelHidden;
            SetModelHidden(false);

            Transform root = modelRoot != null ? modelRoot : target;
            renderers =
                root != null
                    ? root.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();
            shadowModes = new ShadowCastingMode[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                shadowModes[i] = renderers[i].shadowCastingMode;

            SetModelHidden(wasHidden);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // ---------------- Internals ----------------

        private void HandleRotation()
        {
            // Mouse delta is already "per frame", so no Time.deltaTime here.
            Vector2 delta = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;
            yaw = Mathf.Repeat(yaw + delta.x, 360f);
            pitch += invertY ? delta.y : -delta.y;
            pitch = Mathf.Clamp(pitch, minXRotation, maxXRotation);
        }

        private void HandleZoom()
        {
            // Use only the sign: scroll size per notch differs between platforms and settings.
            float scroll = zoomAction.action.ReadValue<float>();
            if (scroll != 0f)
                zoomTarget = Mathf.Clamp(
                    zoomTarget - Mathf.Sign(scroll) * zoomStep,
                    minDistance,
                    maxDistance
                );
        }

        private void HandleCursor()
        {
            if (!lockCursor)
                return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (
                Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
            )
                SetCursorLocked(true);
        }

        /// <summary>How far the camera can go from the pivot along dir before hitting something.</summary>
        private float GetAllowedDistance(Vector3 pivot, Vector3 dir, float wanted)
        {
            int count = Physics.SphereCastNonAlloc(
                pivot,
                collisionRadius,
                dir,
                hits,
                wanted,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

            float allowed = wanted;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.distance <= 0f)
                    continue; // already overlapping at the pivot
                if (hit.collider.transform.IsChildOf(target))
                    continue; // the target itself
                if (hit.distance < allowed)
                    allowed = hit.distance;
            }
            return allowed;
        }

        private void SetModelHidden(bool hidden)
        {
            if (hidden == modelHidden)
                return;
            modelHidden = hidden;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;

                if (shadowModes[i] == ShadowCastingMode.Off)
                    r.forceRenderingOff = hidden; // had no shadow, just hide it
                else
                    r.shadowCastingMode = hidden ? ShadowCastingMode.ShadowsOnly : shadowModes[i];
            }
        }

        private void OnValidate()
        {
            maxXRotation = Mathf.Max(maxXRotation, minXRotation);
            minDistance = Mathf.Max(0f, minDistance);
            maxDistance = Mathf.Max(maxDistance, minDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            collisionRadius = Mathf.Max(0f, collisionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + pivotOffset, 0.1f);
        }
    }
}
