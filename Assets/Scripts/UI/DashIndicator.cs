using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows when the Player can dash again.
    ///   Player dashes   -> the ring empties
    ///   Cooldown passes -> the ring fills back up; full means the dash is ready
    /// </summary>
    public class DashIndicator : MonoBehaviour
    {
        [SerializeField]
        private Player player;

        [Tooltip("An image set to Filled, Radial 360.")]
        [SerializeField]
        private Image ring;

        private void Update()
        {
            DashAbility dash = player.Dash;
            ring.fillAmount = 1f - dash.CooldownRemaining / dash.Cooldown; // the image clamps it to 0..1
        }
    }
}
