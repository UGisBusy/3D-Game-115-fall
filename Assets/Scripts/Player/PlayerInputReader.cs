using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    /// <summary>
    /// Reads raw input and converts it to camera-relative world directions. No game rules here.
    /// (Not named PlayerInput to avoid clashing with UnityEngine.InputSystem.PlayerInput.)
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        private Transform cameraTransform;

        [Header("Actions")]
        [SerializeField]
        private InputActionReference moveAction; // Vector2 (WASD)

        [SerializeField]
        private InputActionReference jumpAction; // Button (Space)

        [SerializeField]
        private InputActionReference dashAction; // Button (Shift)

        [SerializeField]
        private InputActionReference attackAction; // Button (Left Click)

        /// <summary>Raw WASD vector.</summary>
        public Vector2 MoveRaw => moveAction.action.ReadValue<Vector2>();

        /// <summary>WASD converted to a flat world direction relative to the camera (length 0..1).</summary>
        public Vector3 MoveDirection
        {
            get
            {
                Vector2 raw = MoveRaw;
                Vector3 dir = CameraForwardFlat * raw.y + CameraRightFlat * raw.x;
                return Vector3.ClampMagnitude(dir, 1f);
            }
        }

        public Vector3 CameraForwardFlat => Flatten(cameraTransform.forward);
        public Vector3 CameraRightFlat => Flatten(cameraTransform.right);

        public bool JumpPressed => jumpAction.action.WasPressedThisFrame();
        public bool DashPressed => dashAction.action.WasPressedThisFrame();
        public bool AttackHeld => attackAction.action.IsPressed();
        public bool AttackReleased => attackAction.action.WasReleasedThisFrame();

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            dashAction.action.Enable();
            attackAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            dashAction.action.Disable();
            attackAction.action.Disable();
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
