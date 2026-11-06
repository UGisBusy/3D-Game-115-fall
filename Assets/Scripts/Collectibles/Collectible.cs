using PlayerSystem;
using UnityEngine;

/// <summary>
/// Pickup with a type. When the Player touches it, raises LevelEvents.Collected and disappears.
/// It has no effect logic itself: listeners (BowAbility, Player, LevelManager) react by Type.
/// Always consumed on touch, even if the Player is already full.
///
/// Setup: a Collider with Is Trigger on (set automatically when the component is added).
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [SerializeField]
    private CollectibleType type;

    [SerializeField]
    private int amount = 1;

    public CollectibleType Type => type;
    public int Amount => amount;

    private bool collected; // the Player can touch with several colliders in one frame

    // Runs when the component is added in the editor.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;

        collected = true;
        LevelEvents.RaiseCollected(this);

        // TODO: VFX / sound hook goes here later (Art).
        Destroy(gameObject);
    }
}
