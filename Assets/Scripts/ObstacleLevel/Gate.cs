using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Independent Env Obj. A gate that blocks the path while it is closed.
    ///   Closed       -> stay in the place, the Player can't pass
    ///   Opening      -> slides away by openOffset. If its Plate is empty again in the middle opening process, it closes again
    ///   Fully open   -> the gate is destroyed and can't respawn again.
    ///   A PressurePlate tells it with SetOpen.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Gate : MonoBehaviour
    {
        [Tooltip("Where the gate goes when it opens, from its shut position.")]
        [SerializeField]
        private Vector3 openOffset = new(0f, -3f, 0f); // go down

        [SerializeField]
        private float moveSpeed = 4f;

        private Rigidbody rb;
        private Vector3 shutPosition;
        private Vector3 targetPosition;
        private bool isOpening;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true; // the Player can't move it and it blocks the Player
            rb.interpolation = RigidbodyInterpolation.Interpolate; // moves in FixedUpdate
            shutPosition = rb.position;
            targetPosition = shutPosition;
        }

        public void SetOpen(bool open)
        {
            isOpening = open;

            if (open)
            {
                targetPosition = shutPosition + openOffset;
            }
            else
            {
                targetPosition = shutPosition;
            }
        }

        private void FixedUpdate()
        {
            Vector3 next = Vector3.MoveTowards(
                rb.position,
                targetPosition,
                moveSpeed * Time.fixedDeltaTime
            );
            rb.MovePosition(next);

            if (isOpening && next == targetPosition) // it reached the open position
            {
                Destroy(gameObject);
            }
        }
    }
}
