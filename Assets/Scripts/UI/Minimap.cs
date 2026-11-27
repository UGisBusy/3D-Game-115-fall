using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// A round radar in the corner of the screen. It shows only what matters for finding the way:
    ///   Floors and walls -> flat shapes, made once from the level's static box colliders
    ///   Objects with a MinimapIcon (Crystals, Hearts, boxes, plates...) -> icons on top
    ///   Trees, rocks, clouds and other decoration -> not shown (they have no box collider)
    /// The shapes are placed far below the level, out of the main camera's reach, and a second camera
    /// looks straight down at them from above the Player's spot and draws into a texture the map image shows.
    ///   The Player moves -> the camera follows, so the Player is always in the middle
    ///   The Player turns -> the arrow in the middle turns with them; north stays up
    /// Icons with Sonar on (Crystals) send out rings while they are inside the map.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        private const float RadarDepth = -2000f; // where the shapes are, beyond the main camera's far clip
        private const float WallThickness = 2f; // a box thinner than this (and 1 m or taller) is a wall, not a floor

        [SerializeField]
        private Player player;

        [Tooltip("A camera looking straight down, set to orthographic.")]
        [SerializeField]
        private Camera mapCamera;

        [SerializeField]
        private RawImage mapImage;

        [Tooltip("The Player's arrow in the middle of the map.")]
        [SerializeField]
        private RectTransform playerArrow;

        [Tooltip(
            "Where the icons are drawn: inside the map's mask, so they are cut to the circle."
        )]
        [SerializeField]
        private RectTransform iconLayer;

        [Tooltip(
            "Width and height of a sonar ring when it starts, in pixels. Each icon sets its own size."
        )]
        [SerializeField]
        private float iconSize = 22f;

        [Tooltip(
            "Outline around icons and sonar rings, so they show on both the sky and the floors."
        )]
        [SerializeField]
        private Color iconOutline = Color.black;

        [Tooltip("How high above the shapes the map camera floats.")]
        [SerializeField]
        private float height = 40f;

        [Tooltip("Width and height of the map texture in pixels.")]
        [SerializeField]
        private int resolution = 1024;

        [Header("Radar")]
        [Tooltip("Shown where there is no floor: the sky.")]
        [SerializeField]
        private Color background = new(0.53f, 0.78f, 1f);

        [SerializeField]
        private Material floorMaterial;

        [SerializeField]
        private Material wallMaterial;

        [Tooltip("Rings and a crosshair drawn over the map.")]
        [SerializeField]
        private Sprite radarGrid;

        [SerializeField]
        private Color gridColor = Color.black;

        [Tooltip("The ring an icon with Sonar on sends out.")]
        [SerializeField]
        private Sprite sonarRing;

        [Tooltip("Seconds for one ring to grow and fade.")]
        [SerializeField]
        private float sonarPeriod = 1.2f;

        [Tooltip("How big a ring grows.")]
        [SerializeField]
        private float sonarScale = 3f;

        private RenderTexture texture;
        private readonly List<Image> iconImages = new();
        private readonly List<Image> sonarRings = new(); // one per icon image, its child

        private void Awake()
        {
            texture = new RenderTexture(resolution, resolution, 16);
            mapCamera.targetTexture = texture;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = background;
            mapImage.texture = texture;

            Image grid = CreateImage("RadarGrid", mapImage.transform, radarGrid);
            grid.rectTransform.anchorMin = Vector2.zero;
            grid.rectTransform.anchorMax = Vector2.one;
            grid.rectTransform.sizeDelta = Vector2.zero; // covers the whole map, under the icons
            grid.color = gridColor;

            AddOutline(playerArrow.GetComponent<Image>()); // like the icons, so it shows on the sky and the floors

            BuildShapes();
        }

        // LateUpdate, so the camera follows where the Player ended up this frame.
        private void LateUpdate()
        {
            Transform target = player.transform;
            mapCamera.transform.position = new Vector3(
                target.position.x,
                RadarDepth + height,
                target.position.z
            );
            playerArrow.localEulerAngles = new Vector3(0f, 0f, -target.eulerAngles.y);
            ShowIcons(target.position);
        }

        // One flat shape per static box collider of the level: floors and walls.
        // Higher tops are drawn over lower ones, and walls over floors.
        private void BuildShapes()
        {
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            Transform root = new GameObject("RadarShapes").transform;

            foreach (BoxCollider box in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                // Only the level itself. Objects with an icon (a Breakable Wall, a Checkpoint) are shown by their icon,
                // so a broken wall doesn't stay on the map as a shape.
                if (
                    box.isTrigger
                    || box.attachedRigidbody != null
                    || box.TryGetComponent(out MinimapIcon _)
                )
                {
                    continue;
                }

                Bounds b = box.bounds;
                bool wall = Mathf.Min(b.size.x, b.size.z) < WallThickness && b.size.y >= 1f;
                float layer = b.max.y * 0.01f;

                if (wall)
                {
                    layer += 1f;
                }

                GameObject shape = new("Shape", typeof(MeshFilter), typeof(MeshRenderer));
                shape.transform.SetParent(root, false);
                shape.transform.SetPositionAndRotation(
                    new Vector3(b.center.x, RadarDepth + layer, b.center.z),
                    Quaternion.Euler(90f, 0f, 0f) // the quad faces up
                );
                shape.transform.localScale = new Vector3(b.size.x, b.size.z, 1f);
                shape.GetComponent<MeshFilter>().sharedMesh = quad;

                MeshRenderer meshRenderer = shape.GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (wall)
                {
                    meshRenderer.sharedMaterial = wallMaterial;
                }
                else
                {
                    meshRenderer.sharedMaterial = floorMaterial;
                }
            }
        }

        // One image per icon, reused every frame. Extra images are hidden.
        private void ShowIcons(Vector3 center)
        {
            float mapWidth = mapImage.rectTransform.rect.width;
            float pixelsPerMeter = mapWidth / (2f * mapCamera.orthographicSize);
            IReadOnlyList<MinimapIcon> icons = MinimapIcon.All;

            for (int i = 0; i < icons.Count; i++)
            {
                if (i == iconImages.Count)
                {
                    iconImages.Add(CreateIconImage());
                }

                MinimapIcon icon = icons[i];
                Vector3 offset = icon.transform.position - center;
                Vector2 point = new Vector2(offset.x, offset.z) * pixelsPerMeter;
                float edge = (mapWidth - Mathf.Max(icon.Size.x, icon.Size.y)) / 2f;
                bool outside = point.magnitude > edge;

                Image image = iconImages[i];
                image.gameObject.SetActive(!outside || icon.KeepOnEdge);
                image.rectTransform.anchoredPosition = Vector2.ClampMagnitude(point, edge);
                image.rectTransform.sizeDelta = icon.Size;
                image.rectTransform.localEulerAngles = new Vector3(0f, 0f, IconAngle(icon));
                image.sprite = icon.Sprite;
                image.color = icon.Color;
                ShowSonar(sonarRings[i], icon.Sonar && !outside, icon.Color);
            }

            for (int i = icons.Count; i < iconImages.Count; i++)
            {
                iconImages[i].gameObject.SetActive(false);
            }
        }

        // Seen from above with north up, like the Player's arrow. 0 for icons that don't turn.
        private static float IconAngle(MinimapIcon icon)
        {
            if (icon.TurnWithObject)
            {
                return -icon.transform.eulerAngles.y;
            }

            return 0f;
        }

        // A ring that grows from the icon and fades, again every sonarPeriod.
        private void ShowSonar(Image ring, bool on, Color color)
        {
            ring.gameObject.SetActive(on);

            if (!on)
            {
                return;
            }

            float t = Time.time / sonarPeriod % 1f; // 0 to 1, then again
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, sonarScale, t);
            color.a = 1f - t;
            ring.color = color;
        }

        private Image CreateIconImage()
        {
            Image image = CreateImage("Icon", iconLayer, null);
            Image ring = CreateImage("Sonar", image.transform, sonarRing);
            AddOutline(image);
            AddOutline(ring);
            sonarRings.Add(ring);
            return image;
        }

        private void AddOutline(Image image)
        {
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = iconOutline;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        // A RenderTexture is not freed on its own, so it is released with the map.
        // When the scene closes, the map camera can be destroyed before the map.
        private void OnDestroy()
        {
            if (mapCamera != null)
            {
                mapCamera.targetTexture = null;
            }

            texture.Release();
            Destroy(texture);
        }
    }
}
