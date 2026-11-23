using System;
using UnityEngine;

namespace Blocktrotter.Audio
{
    /// <summary>Every sound effect the game plays.</summary>
    public enum Sfx
    {
        BlockSnap,
        TargetExit,
        Star1,
        Star2,
        Star3,
        LevelSolved,
        Button,
        Hint,
        Undo,
        WorldUnlocked
    }

    /// <summary>
    /// Clips and volumes for each <see cref="Sfx"/>. The clips are synthesized from scratch
    /// by tools/synth_sounds.py (no third-party samples), so they are entirely ours.
    /// </summary>
    /// <example><code>SoundConfig.Entry e = config.Get(Sfx.Hint); source.PlayOneShot(e.Clip, e.Volume);</code></example>
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "Blocktrotter/Sound Config")]
    public sealed class SoundConfig : ScriptableObject
    {
        /// <summary>One sound: clip, loudness and a little random pitch so repeats don't sound robotic.</summary>
        [Serializable]
        public struct Entry
        {
            public Sfx Sound;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
            [Range(0f, 0.2f)] public float PitchJitter;
        }

        [SerializeField] private Entry[] _entries = new Entry[0];

        /// <summary>All entries (editor tooling and tests).</summary>
        public Entry[] Entries => _entries;

        /// <summary>Entry for a sound; Clip is null if it was never assigned.</summary>
        public Entry Get(Sfx sound)
        {
            foreach (Entry e in _entries)
                if (e.Sound == sound) return e;
            return default;
        }

        /// <summary>Replaces all entries. Editor tooling only.</summary>
        public void SetEntries(Entry[] entries) => _entries = entries;
    }
}
