using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows the Player's HP as plain text.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class HpText : MonoBehaviour
    {
        private Text text;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

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
            text.text = $"HP: {hp}/{maxHp}";
        }
    }
}
