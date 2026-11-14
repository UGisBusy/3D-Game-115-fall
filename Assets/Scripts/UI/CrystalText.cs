using ObstacleLevel;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows how many Crystals the Player has collected as plain text.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class CrystalText : MonoBehaviour
    {
        private Text text;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

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
            text.text = $"Crystals: {collected}/{total}";
        }
    }
}
