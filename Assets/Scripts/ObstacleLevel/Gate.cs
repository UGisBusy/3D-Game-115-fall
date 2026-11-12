using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Independent Env Obj. A gate that blocks the path while it is closed.
    ///   Closed -> stays at the position it has in the scene, the Player can't pass
    ///   Open -> slides away by openOffset, then the path is free
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
            rb.MovePosition(
                Vector3.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime)
            );
        }
    }
}
