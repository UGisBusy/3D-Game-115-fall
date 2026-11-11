using System.Collections.Generic;
using UnityEngine;

namespace FireballFX
{
    /// <summary>
    /// Impact effect: fades out the flash light, pushes nearby rigidbodies, then removes itself.
    /// The particle systems on the children play automatically.
    /// </summary>
    public class FireballExplosion : MonoBehaviour
    {
        [Tooltip("Seconds before the whole effect is destroyed.")]
        public float lifetime = 2.5f;

        [Header("Light flash")]
        public Light flashLight;
        public float lightDuration = 0.35f;

        [Header("Physics push (0 = off)")]
        public float force = 3f;
        public float forceRadius = 2f;

        float startIntensity;
        float age;

        void Start()
        {
            if (flashLight != null)
                startIntensity = flashLight.intensity;
            if (force > 0f)
                PushRigidbodies();
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (flashLight == null || !flashLight.enabled)
                return;

            age += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(age / lightDuration);
            flashLight.intensity = startIntensity * k * k;
            if (k <= 0f)
                flashLight.enabled = false;
        }

        void PushRigidbodies()
        {
            var pushed = new HashSet<Rigidbody>();
            foreach (
                Collider c in Physics.OverlapSphere(
                    transform.position,
                    forceRadius,
                    ~0,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                Rigidbody rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic && pushed.Add(rb))
                    rb.AddExplosionForce(
                        force,
                        transform.position,
                        forceRadius,
                        0.3f,
                        ForceMode.Impulse
                    );
            }
        }
    }
}
