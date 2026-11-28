using System.Collections.Generic;
using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// Marks an object to show on the Minimap as a small icon.
    /// Every enabled icon is in All, so the Minimap can draw them without searching the scene.
    ///   Object spawns or is shown -> its icon appears
    ///   Object is destroyed       -> its icon is gone
    /// The icon can turn with the object (long things like Gates lie across the path like in the level),
    /// and the object can change its color (a Checkpoint turns it green when it is active).
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        private static readonly List<MinimapIcon> all = new();
        public static IReadOnlyList<MinimapIcon> All => all;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Color color = Color.white;

        [Tooltip("Width and height on the map in pixels (about 4 pixels per meter).")]
        [SerializeField]
        private Vector2 size = new(22f, 22f);

        [Tooltip("On: the icon turns with the object, seen from above.")]
        [SerializeField]
        private bool turnWithObject;

        [Tooltip(
            "On: stays on the map's edge when it is far away, to show where it is. Off: hidden."
        )]
        [SerializeField]
        private bool keepOnEdge;

        [Tooltip("On: while it is inside the map (close by), its icon sends out sonar rings.")]
        [SerializeField]
        private bool sonar;

        public Sprite Sprite => sprite;
        public Vector2 Size => size;
        public bool TurnWithObject => turnWithObject;
        public bool KeepOnEdge => keepOnEdge;
        public bool Sonar => sonar;

        public Color Color
        {
            get => color;
            set => color = value;
        }

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
