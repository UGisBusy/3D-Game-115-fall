using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// A round map in the corner of the screen. It shows only what matters for finding the way:
    ///   Floors and walls -> the level's own models, seen from above
    ///   Objects with a MinimapIcon (Crystals, Hearts, boxes, plates...) -> icons on top
    ///   Trees, rocks, clouds and other decoration -> not shown
    /// A second camera looks straight down from above the Player and draws into a texture the map image shows.
    /// It only draws the MapLayer: at the start, the models of the level's floors and walls are put on it.
    ///   The Player moves -> the camera follows, so the Player is always in the middle
    ///   The Player turns -> the arrow in the middle turns with them; north stays up
    /// Icons with Sonar on (Crystals) send out rings while they are inside the map.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        // Layers in Project Settings > Tags and Layers. The map camera draws only these two.
        private const string MapLayer = "Minimap"; // the level's floor and wall models, the main camera draws them too
        private const string MapOnlyLayer = "MinimapOnly"; // the floor shadows, the main camera never draws them

        // A floor shadow sits this far under its floor's top: hidden under the floor, above the floors below it.
        private const float ShadowDepth = 0.1f;

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
            "Outline around icons and sonar rings, so they show on both the background and the floors."
        )]
        [SerializeField]
        private Color iconOutline = Color.black;

        [Tooltip("How high above the Player the map camera floats.")]
        [SerializeField]
        private float height = 40f;

        [Tooltip("Width and height of the map texture in pixels.")]
        [SerializeField]
        private int resolution = 1024;

        [Tooltip(
            "Drawn under every floating floor on the map only, a bit to the side, so the floors look like floating islands."
        )]
        [SerializeField]
        private Material floorShadow;

        [Tooltip(
            "Where a floor's shadow sits from the floor, in meters: x to the right, y up on the map."
        )]
        [SerializeField]
        private Vector2 floorShadowOffset = new(0.6f, -0.6f);

        [Header("Radar")]
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
            mapImage.texture = texture;

            Image grid = CreateImage("RadarGrid", mapImage.transform, radarGrid);
            grid.rectTransform.anchorMin = Vector2.zero;
            grid.rectTransform.anchorMax = Vector2.one;
            grid.rectTransform.sizeDelta = Vector2.zero; // covers the whole map, under the icons
            grid.color = gridColor;

            AddOutline(playerArrow.GetComponent<Image>()); // like the icons, so it shows on the background and the floors

            int mapOnly = 1 << LayerMask.NameToLayer(MapOnlyLayer);
            mapCamera.cullingMask = 1 << LayerMask.NameToLayer(MapLayer) | mapOnly;
            Camera.main.cullingMask &= ~mapOnly; // the floor shadows would stick out under the islands in the game
        }

        // Start, not Awake: the floor shadows check what is under each floor, so every collider must be in the physics scene.
        private void Start()
        {
            ShowLevelOnMap(LayerMask.NameToLayer(MapLayer), LayerMask.NameToLayer(MapOnlyLayer));
        }

        // LateUpdate, so the camera follows where the Player ended up this frame.
        private void LateUpdate()
        {
            Transform target = player.transform;
            mapCamera.transform.position = target.position + Vector3.up * height;
            playerArrow.localEulerAngles = new Vector3(0f, 0f, -target.eulerAngles.y);
            ShowIcons(target.position);
        }

        // Puts the models of the level's floors and walls (the static box colliders) on the map layer,
        // and gives every floating floor a shadow. Only models without a collider move, so physics and ground checks don't change.
        private void ShowLevelOnMap(int mapLayer, int mapOnlyLayer)
        {
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

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

                foreach (Renderer model in box.GetComponentsInChildren<Renderer>())
                {
                    if (model.enabled && !model.TryGetComponent(out Collider _))
                    {
                        model.gameObject.layer = mapLayer;
                    }
                }

                if (IsFloating(box.bounds))
                {
                    AddFloorShadow(box.bounds, mapOnlyLayer, quad);
                }
            }
        }

        // Nothing right under its bottom: a floating floor. Walls, ledges and spike trays stand on something.
        // The ray starts inside the box, and a ray never hits a collider it starts in.
        private static bool IsFloating(Bounds box)
        {
            return !Physics.Raycast(
                new Vector3(box.center.x, box.min.y + 0.05f, box.center.z),
                Vector3.down,
                0.2f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        // A flat dark copy of the floor's outline, just under its top and shifted to the side, seen only by the map camera.
        private void AddFloorShadow(Bounds floor, int layer, Mesh quad)
        {
            GameObject shadow = new("FloorShadow", typeof(MeshFilter), typeof(MeshRenderer));
            shadow.layer = layer;
            shadow.transform.SetPositionAndRotation(
                new Vector3(
                    floor.center.x + floorShadowOffset.x,
                    floor.max.y - ShadowDepth,
                    floor.center.z + floorShadowOffset.y
                ),
                Quaternion.Euler(90f, 0f, 0f) // the quad faces up
            );
            shadow.transform.localScale = new Vector3(floor.size.x, floor.size.z, 1f);
            shadow.GetComponent<MeshFilter>().sharedMesh = quad;

            MeshRenderer shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = floorShadow;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
