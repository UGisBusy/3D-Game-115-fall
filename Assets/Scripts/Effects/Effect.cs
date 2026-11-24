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

        [Tooltip(
            "Keeps playing while the game is frozen (Time.timeScale 0, AudioListener paused), "
        )]
        [SerializeField]
        private bool playWhilePaused;

        public void Play(Vector3 position)
        {
            if (particles != null)
            {
                GameObject instance = Object.Instantiate(particles, position, Quaternion.identity);
                if (playWhilePaused)
                {
                    foreach (
                        ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>(
                            true
                        )
                    )
                    {
                        ParticleSystem.MainModule main = system.main;
                        main.useUnscaledTime = true;
                    }
                }
            }

            // Played at the camera (where the listener is), so it is always heard at full volume.
            if (sounds.Length > 0 && Camera.main != null)
            {
                AudioClip sound = sounds[Random.Range(0, sounds.Length)];
                Vector3 at = Camera.main.transform.position;
                if (playWhilePaused)
                    PlayIgnoringPause(sound, at, volume);
                else
                    AudioSource.PlayClipAtPoint(sound, at, volume);
            }
        }

        // Like AudioSource.PlayClipAtPoint, but keeps playing while AudioListener.pause is on.
        private static void PlayIgnoringPause(AudioClip clip, Vector3 position, float volume)
        {
            var go = new GameObject("One shot audio (plays while paused)");
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f; // 3D, same as PlayClipAtPoint
            source.ignoreListenerPause = true;
            source.Play();
            Object.Destroy(go, clip.length); // scaled time: removed once the game runs again, or with the scene
        }
    }
}
