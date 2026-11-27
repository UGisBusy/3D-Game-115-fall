using EffectSystem;
using UISystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Independent Env Obj. A gate that blocks the path while it is closed. Its PressurePlate opens and closes it with SetOpen.
    ///   Plate pressed  -> sinks straight down until it is completely under the ground, and stays there while the plate is pressed
    ///   Plate released -> rises back and blocks the path again, also halfway through opening
    ///   Fully open     -> hidden from the minimap, shown again as soon as it rises
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(MinimapIcon))]
    public class Gate : MonoBehaviour
    {
        private const float SinkMargin = 0.1f; // how far below the ground its top ends, so it doesn't flicker with the ground

        [SerializeField]
        private float moveSpeed = 4f;

        [Tooltip("Plays while the gate is moving.")]
        [SerializeField]
        private LoopingSound moveSound;

        private Rigidbody rb;
        private MinimapIcon mapIcon;
        private Vector3 shutPosition;
        private Vector3 openPosition;
        private Vector3 targetPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            mapIcon = GetComponent<MinimapIcon>();
            rb.isKinematic = true; // the Player can't move it and it blocks the Player
            rb.interpolation = RigidbodyInterpolation.Interpolate; // moves in FixedUpdate
            shutPosition = rb.position;
            openPosition =
                shutPosition + Vector3.down * (GetComponent<Collider>().bounds.size.y + SinkMargin); // its whole height
            targetPosition = shutPosition;
        }

        public void SetOpen(bool open)
        {
            if (open)
            {
                targetPosition = openPosition;
            }
            else
            {
                targetPosition = shutPosition;
            }
        }

        private void FixedUpdate()
        {
            bool moving = rb.position != targetPosition;
            moveSound.SetPlaying(moving);
            mapIcon.enabled = moving || targetPosition == shutPosition; // hide in the map only while fully under the ground

            if (moving)
            {
                rb.MovePosition(
                    Vector3.MoveTowards(
                        rb.position,
                        targetPosition,
                        moveSpeed * Time.fixedDeltaTime
                    )
                );
            }
        }
    }
}
