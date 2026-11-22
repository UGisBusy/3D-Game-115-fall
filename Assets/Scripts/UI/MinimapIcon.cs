using System.Collections.Generic;
using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// Marks an object to show on the Minimap as a small icon.
    /// Every enabled icon is in All, so the Minimap can draw them without searching the scene.
    ///   Object spawns or is shown -> its icon appears
    ///   Object is destroyed       -> its icon is gone
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        private static readonly List<MinimapIcon> all = new();
        public static IReadOnlyList<MinimapIcon> All => all;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Color color = Color.white;

        [Tooltip(
            "On: stays on the map's edge when it is far away, to show where it is. Off: hidden."
        )]
        [SerializeField]
        private bool keepOnEdge;

        public Sprite Sprite => sprite;
        public Color Color => color;
        public bool KeepOnEdge => keepOnEdge;

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}
