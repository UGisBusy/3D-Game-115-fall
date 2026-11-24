using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows the Player's HP as hearts: one heart per HP point.
    ///   HP left -> the heart is full
    ///   HP lost -> the heart is faded
    /// Only as many hearts as the Player's max HP are shown.
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        [Tooltip("One heart image per HP point, left to right.")]
        [SerializeField]
        private Image[] hearts;

        [SerializeField]
        private Color fullColor = new(0.94f, 0.35f, 0.42f);

        [SerializeField]
        private Color emptyColor = new(1f, 1f, 1f, 0.25f);

        private void OnEnable()
        {
            PlayerEvents.HpChanged += Show;
        }

        private void OnDisable()
        {
            PlayerEvents.HpChanged -= Show;
        }

        private void Show(int hp, int maxHp)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < maxHp);

                if (i < hp)
                {
                    hearts[i].color = fullColor;
                }
                else
                {
                    hearts[i].color = emptyColor;
                }
            }
        }
    }
}
