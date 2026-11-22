using EffectSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// A floor plate that opens its Gate only when a box is standing on it. Each plate is made for one box.
    ///   A box gets on the plate   -> the Gate opens
    ///   The box leaves the plate  -> the Gate goes back to closed
    ///   The Player steps on it    -> nothing happens, must be a box to open the Gate
    /// Intended mechanic: a fully opened Gate stays open, so only a box may open it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField]
        private Gate gate;

        [Tooltip("Plays when a box gets on the plate.")]
        [SerializeField]
        private Effect pressEffect;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true; // it doesn't block them with a solid collider.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PushableBox _))
            {
                pressEffect.Play(transform.position);
                SetGateOpen(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PushableBox _))
            {
                SetGateOpen(false);
            }
        }

        // When the gate finishes opening, it destroys itself and stays open.
        private void SetGateOpen(bool open)
        {
            if (gate != null)
            {
                gate.SetOpen(open);
            }
        }
    }
}
