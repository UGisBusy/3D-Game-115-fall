using UnityEngine;

/// <summary>
/// Visual idle animation: floats up and down and spins around an axis.
/// Put it on the Model child, not the root, so the trigger collider stays still.
/// Moves relative to the starting local position/rotation, so the Model's own offset and tilt are kept.
/// </summary>
[DisallowMultipleComponent]
public class CollectibleFloat : MonoBehaviour
{
    [Header("Spin")]
    [SerializeField]
    private float spinSpeed = 90f; // degrees per second, 0 = no spin, negative = reverse

    [SerializeField]
    private Vector3 spinAxis = Vector3.up; // in the parent's space

    [Header("Float")]
    [SerializeField]
    private float floatHeight = 0.15f; // distance from the middle to the top, 0 = no float

    [SerializeField]
    private float floatSpeed = 0.5f; // up-down cycles per second

    [Header("Start")]
    [SerializeField]
    private bool randomStart = true; // so many collectibles don't move in sync

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private float time;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        if (randomStart)
            time = Random.Range(0f, 100f);
    }

    private void Update()
    {
        time += Time.deltaTime;

        Vector3 axis = spinAxis.sqrMagnitude > 0.0001f ? spinAxis.normalized : Vector3.up;
        float angle = spinSpeed * time % 360f;
        transform.localRotation = Quaternion.AngleAxis(angle, axis) * startLocalRotation;

        float height = Mathf.Sin(time * floatSpeed * 2f * Mathf.PI) * floatHeight;
        transform.localPosition = startLocalPosition + Vector3.up * height;
    }
}
