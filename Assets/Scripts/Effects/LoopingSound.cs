using UnityEngine;

namespace EffectSystem
{
    /// <summary>
    /// A sound that keeps playing while something is happening, like a box sliding or a gate moving.
    ///   SetPlaying(true)  -> fades in and loops
    ///   SetPlaying(false) -> fades out and stops
    /// The short fade stops it from clicking when it starts and stops every physics step.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LoopingSound : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float volume = 0.6f;

        [Tooltip("Seconds to fade in or out.")]
        [SerializeField]
        private float fadeTime = 0.1f;

        private AudioSource source;
        private bool playing;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        public void SetPlaying(bool value)
        {
            playing = value;
            if (playing && !source.isPlaying)
            {
                source.Play();
            }
        }

        private void Update()
        {
            float target = 0f;
            if (playing)
            {
                target = volume;
            }

            source.volume = Mathf.MoveTowards(
                source.volume,
                target,
                volume / fadeTime * Time.deltaTime
            );

            if (!playing && source.volume == 0f && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
