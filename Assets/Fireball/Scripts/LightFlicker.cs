using UnityEngine;

namespace FireballFX
{
    /// <summary>Makes a light flicker like fire.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [Range(0f, 1f)] public float amount = 0.3f;
        public float speed = 12f;

        Light lightSource;
        float baseIntensity;
        float seed;

        void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            lightSource.intensity = baseIntensity * (1f - amount + n * amount * 2f);
        }
    }
}
