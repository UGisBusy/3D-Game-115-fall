using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// A round map in the corner of the screen.
    /// A second camera looks straight down from above the Player and draws into a texture,
    /// and the map image shows that texture.
    ///   The Player moves -> the camera follows, so the Player is always in the middle
    ///   The Player turns -> the arrow in the middle turns with them; north stays up
    /// Objects with a MinimapIcon (Hearts, Crystals) are drawn on top as icons,
    /// because they are too small to see from the map camera.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
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

        [Tooltip("Width and height of one icon in pixels.")]
        [SerializeField]
        private float iconSize = 22f;

        [Tooltip("How high above the Player the map camera floats.")]
        [SerializeField]
        private float height = 40f;

        [Tooltip("Width and height of the map texture in pixels.")]
        [SerializeField]
        private int resolution = 256;

        private RenderTexture texture;
        private readonly List<Image> iconImages = new();

        private void Awake()
        {
            texture = new RenderTexture(resolution, resolution, 16);
            mapCamera.targetTexture = texture;
            mapImage.texture = texture;
        }

        // LateUpdate, so the camera follows where the Player ended up this frame.
        private void LateUpdate()
        {
            Transform target = player.transform;
            mapCamera.transform.position = target.position + Vector3.up * height;
            playerArrow.localEulerAngles = new Vector3(0f, 0f, -target.eulerAngles.y);
            ShowIcons(target.position);
        }

        // One image per icon, reused every frame. Extra images are hidden.
        private void ShowIcons(Vector3 center)
        {
            float mapWidth = mapImage.rectTransform.rect.width;
            float pixelsPerMeter = mapWidth / (2f * mapCamera.orthographicSize);
            float edge = (mapWidth - iconSize) / 2f;
            IReadOnlyList<MinimapIcon> icons = MinimapIcon.All;

            for (int i = 0; i < icons.Count; i++)
            {
                if (i == iconImages.Count)
                {
                    iconImages.Add(CreateIconImage());
                }

                Vector3 offset = icons[i].transform.position - center;
                Vector2 point = new Vector2(offset.x, offset.z) * pixelsPerMeter;
                bool outside = point.magnitude > edge;

                Image image = iconImages[i];
                image.gameObject.SetActive(!outside || icons[i].KeepOnEdge);
                image.rectTransform.anchoredPosition = Vector2.ClampMagnitude(point, edge);
                image.sprite = icons[i].Sprite;
                image.color = icons[i].Color;
            }

            for (int i = icons.Count; i < iconImages.Count; i++)
            {
                iconImages[i].gameObject.SetActive(false);
            }
        }

        private Image CreateIconImage()
        {
            GameObject icon = new("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(iconLayer, false);

            Image image = icon.GetComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
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
