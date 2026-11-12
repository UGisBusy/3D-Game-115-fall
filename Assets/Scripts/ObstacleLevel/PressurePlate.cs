using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// A floor plate that opens its Gate only when the Player or a box is standing on it.
    ///   Something steps on the plate  -> the Gate opens
    ///   Plate empty   -> the Gate closed
    /// Intended mechanic: The Player has to leave the plate to reach the Gate, so a box must hold it down.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField]
        private Gate gate;

        private int weightCount; // how many Players and boxes are on the plate now

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true; // it doesn't block them with a solid collider.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsWeight(other))
            {
                weightCount++;
                gate.SetOpen(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsWeight(other))
            {
                weightCount--;

                if (weightCount == 0) // the Player can leave while a box is still on it
                {
                    gate.SetOpen(false);
                }
            }
        }

        private static bool IsWeight(Collider other)
        {
            return other.TryGetComponent(out Player _)
                || other.TryGetComponent(out DashableBox _)
                || other.TryGetComponent(out MovableBox _);
        }
    }
}
