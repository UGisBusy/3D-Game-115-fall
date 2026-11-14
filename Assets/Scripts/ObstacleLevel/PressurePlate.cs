using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// A floor plate that opens its Gate only when a box is standing on it.
    ///   A box is on the plate   -> the Gate opens
    ///   No box on the plate     -> the Gate goes back to closed
    ///   The Player steps on it  -> nothing happens, must be a box to open the Gate
    /// Intended mechanic: a fully opened Gate stays open, so only a box may open it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField]
        private Gate gate;

        private int boxCount; // how many boxes are on the plate now

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true; // it doesn't block them with a solid collider.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PushableBox _))
            {
                boxCount++;
                SetGateOpen(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PushableBox _))
            {
                boxCount--;

                if (boxCount == 0) // another box can still be on it
                {
                    SetGateOpen(false);
                }
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
