using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EffectSystem
{
    /// <summary>
    /// A particle effect and a sound that play together, set in the Inspector.
    ///   Play(position) -> the particles appear there and remove themselves when done, and the sound plays
    ///   Stop()         -> the sound stops (the particles finish on their own)
    /// Each Effect plays one sound at a time: playing it again restarts the sound instead of stacking a second one.
    /// Either part can be left empty.
    /// </summary>
    [Serializable]
    public class Effect
    {
        [SerializeField]
        private GameObject particles;

        [SerializeField]
        private AudioClip sound;

        [SerializeField]
        [Range(0f, 1f)]
        private float volume = 0.8f;

        [Tooltip(
            "Keeps playing while the game is frozen behind the Pause, Fail or Complete screen (Time.timeScale 0, AudioListener paused)."
        )]
        [SerializeField]
        private bool playWhilePaused;

        // Made on the first Play and reused after, so playing again doesn't create a new object every time.
        private AudioSource source;

        public void Play(Vector3 position)
        {
            Play(position, Quaternion.identity);
        }

        // rotation: for particles that have a direction, like dust kicked along a push.
        public void Play(Vector3 position, Quaternion rotation)
        {
            if (particles != null)
            {
                GameObject instance = Object.Instantiate(particles, position, rotation);

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

            if (sound == null)
            {
                return;
            }

            if (source == null) // first Play, or the scene that held it was unloaded
            {
                source = new GameObject("Sound " + sound.name).AddComponent<AudioSource>();
                source.clip = sound;
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D, so it is always heard at full volume wherever the camera is
                source.ignoreListenerPause = playWhilePaused;
            }

            source.volume = volume;
            source.Play(); // restarts it if it is still playing
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }
}
