using ObstacleLevel;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// The obstacle level's Crystal panel: the count as "collected / total", and one slot per Crystal.
    ///   A Crystal is collected -> its slot lights up
    ///   Not yet collected      -> its slot stays grey
    /// Only as many slots as there are Crystals in the level are shown.
    /// When the last one is collected, the hint tells the Player the Star has appeared.
    /// </summary>
    public class CrystalPanel : MonoBehaviour
    {
        [SerializeField]
        private Text countText;

        [Tooltip("The line under the slots. It changes when every Crystal is collected.")]
        [SerializeField]
        private Text hintText;

        [SerializeField]
        private string starAppearedHint = "The Star has appeared!";

        [Tooltip("One slot per Crystal, left to right.")]
        [SerializeField]
        private Image[] slots;

        [SerializeField]
        private Color collectedColor = new(0.13f, 0.5f, 0.54f);

        [SerializeField]
        private Color missingColor = new(0.27f, 0.33f, 0.37f);

        private void OnEnable()
        {
            ObstacleLevelEvents.CrystalCountChanged += Show;
        }

        private void OnDisable()
        {
            ObstacleLevelEvents.CrystalCountChanged -= Show;
        }

        private void Show(int collected, int total)
        {
            countText.text = $"{collected} / {total}";

            if (collected == total)
            {
                hintText.text = starAppearedHint;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(i < total);

                if (i < collected)
                {
                    slots[i].color = collectedColor;
                }
                else
                {
                    slots[i].color = missingColor;
                }
            }
        }
    }
}
