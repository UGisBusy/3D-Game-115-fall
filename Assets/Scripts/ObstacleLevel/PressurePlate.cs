using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// A floor plate that keeps its Gate open while something presses it.
    ///   The Player or a box stands on its red pad -> the Gate opens
    ///   Nothing on the pad anymore               -> the Gate closes again
    /// Pressed when the Player or one box covers at least pressThreshold of the pad, seen from above
    /// (its collider is the pad). Touching the edge does nothing.
    /// To walk through the Gate, the Player has to leave a box on the plate.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField]
        private Gate gate;

        [Tooltip(
            "How much of the red pad the Player or a box must cover to press it. "
        )]
        [SerializeField]
        [Range(0f, 1f)]
        private float pressThreshold = 0.5f; // The Player need to cover at most about 50% of the pad.

        private static readonly Collider[] buffer = new Collider[8];

        private Collider area;
        private bool isPressed;

        private void Awake()
        {
            area = GetComponent<Collider>();
            area.isTrigger = true; // it doesn't block them with a solid collider.
        }

        // Checked every physics step instead of on trigger enter / exit, so the plate only reacts when its state changes:
        // a box that switches between sliding and resting, or teleports back after breaking, can't press it twice.
        private void FixedUpdate()
        {
            bool pressed = IsPressed();

            if (pressed == isPressed)
            {
                return;
            }

            isPressed = pressed;
            gate.SetOpen(pressed);
        }

        // Pressed when the Player or one box covers enough of the pad.
        private bool IsPressed()
        {
            Bounds pad = area.bounds;
            int count = Physics.OverlapBoxNonAlloc(
                pad.center,
                pad.extents,
                buffer,
                Quaternion.identity,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                bool presser =
                    other.TryGetComponent(out Player _) || other.TryGetComponent(out PushableBox _);

                if (presser && Coverage(other.bounds, pad) >= pressThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        // Seen from above: the share of the pad's area that the other object is over, 0 to 1.
        private static float Coverage(Bounds other, Bounds pad)
        {
            float width = Mathf.Min(other.max.x, pad.max.x) - Mathf.Max(other.min.x, pad.min.x);
            float depth = Mathf.Min(other.max.z, pad.max.z) - Mathf.Max(other.min.z, pad.min.z);

            if (width <= 0f || depth <= 0f)
            {
                return 0f;
            }

            return width * depth / (pad.size.x * pad.size.z);
        }
    }
}
