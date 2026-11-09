using PlayerSystem;
using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// The code for independent Env Obj spinning blades. They rotate and hurt the Player on touch.
    /// Touching any child collider hurts and pushes the Player.
    /// Does not own invincibility: Player.TakeHit ignores hits during invincibility.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpinningBlade : MonoBehaviour
    {
        [Header("Spin")]
        [SerializeField]
        private float spinSpeed = 90f; // degrees per second

        [Header("Damage")]
        [SerializeField]
        private int damage = 1;

        [SerializeField]
        private float pushSpeed = 10f;

        [Tooltip("How long the Player loses control after being pushed.")]
        [SerializeField]
        private float pushDuration = 0.3f;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true; // The Player can't move it and can block the player.
            rb.interpolation = RigidbodyInterpolation.Interpolate; // rotates in FixedUpdate.
        }

        private void FixedUpdate()
        {
            rb.MoveRotation(
                rb.rotation * Quaternion.Euler(0f, spinSpeed * Time.fixedDeltaTime, 0f) // spin around the y axis
            );
        }

        // Using Stay not Enter because in case the Player still touching it after invincibility ends.
        // Because Enter only runs once, while Stay runs every frame, so the Player can be hit again after invincibility ends.
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.TryGetComponent(out Player player))
            {
                Vector3 awayFromBlade = -collision.GetContact(0).normal; // check the contact point normal, not the player's position.
                Vector3 push =
                    Vector3.ProjectOnPlane(awayFromBlade, Vector3.up).normalized * pushSpeed;

                player.TakeHit( // Hit and knockback player.
                    HitInfo.Create(
                        damage,
                        gameObject,
                        knockbackVelocity: push,
                        knockbackDuration: pushDuration
                    )
                );
            }
        }
    }
}
