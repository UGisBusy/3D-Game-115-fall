using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EffectSystem
{
    /// <summary>
    /// A particle effect and a sound that play together, set in the Inspector.
    ///   Play(position) -> the particles appear there and remove themselves when done,
    ///                     and one of the sounds plays (a random one, so repeats don't sound the same)
    /// Either part can be left empty.
    /// </summary>
    [Serializable]
    public class Effect
    {
        [SerializeField]
        private GameObject particles;

        [Tooltip("One of them is picked at random each time.")]
        [SerializeField]
        private AudioClip[] sounds;

        [SerializeField]
        [Range(0f, 1f)]
        private float volume = 0.8f;

        public void Play(Vector3 position)
        {
            if (particles != null)
            {
                Object.Instantiate(particles, position, Quaternion.identity);
            }

            // Played at the camera (where the listener is), so it is always heard at full volume.
            if (sounds.Length > 0 && Camera.main != null)
            {
                AudioClip sound = sounds[Random.Range(0, sounds.Length)];
                AudioSource.PlayClipAtPoint(sound, Camera.main.transform.position, volume);
            }
        }
    }
}
