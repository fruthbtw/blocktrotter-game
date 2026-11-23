using System;
using UnityEngine;

namespace Blocktrotter.Audio
{
    /// <summary>
    /// Plays sound effects through a small pool of AudioSources so overlapping sounds
    /// (snap + star + jingle) never cut each other off. Silent while <see cref="IsEnabled"/>
    /// returns false (the Sound setting).
    /// </summary>
    /// <example>
    /// <code>
    /// var player = gameObject.AddComponent&lt;SoundPlayer&gt;();
    /// player.Init(soundConfig, () =&gt; progress.SoundOn);
    /// player.Play(Sfx.BlockSnap);
    /// </code>
    /// </example>
    public sealed class SoundPlayer : MonoBehaviour
    {
        private const int Voices = 6;

        private SoundConfig _config;
        private Func<bool> _isEnabled;
        private AudioSource[] _sources;
        private int _next;

        /// <summary>True when sounds may play (reads the player's setting each time).</summary>
        public bool IsEnabled => _isEnabled == null || _isEnabled();

        /// <summary>Sets the clips and the on/off check.</summary>
        public void Init(SoundConfig config, Func<bool> isEnabled)
        {
            _config = config;
            _isEnabled = isEnabled;
            // Without a listener Unity plays every clip into silence; make sure one exists.
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f;
            }
        }

        /// <summary>Plays a sound on the next free voice (or the oldest one).</summary>
        public void Play(Sfx sound)
        {
            if (_config == null || !IsEnabled) return;
            SoundConfig.Entry e = _config.Get(sound);
            if (e.Clip == null) return;

            AudioSource source = _sources[_next];
            for (int i = 0; i < Voices; i++)
            {
                AudioSource candidate = _sources[(_next + i) % Voices];
                if (!candidate.isPlaying) { source = candidate; break; }
            }
            _next = (Array.IndexOf(_sources, source) + 1) % Voices;

            source.pitch = 1f + UnityEngine.Random.Range(-e.PitchJitter, e.PitchJitter);
            source.PlayOneShot(e.Clip, e.Volume);
        }
    }
}
