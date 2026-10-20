using System;
using UnityEngine;

namespace Blocktrotter.Save
{
    /// <summary>
    /// Everything saved on the device: unlocked levels, best stars, hints and
    /// settings. Serialized with JsonUtility by <see cref="PlayerPrefsProgressStore"/>.
    /// Nothing leaves the device.
    /// </summary>
    /// <example>
    /// <code>
    /// progress.RecordSolve(levelIndex, stars);   // keeps best stars, unlocks the next level
    /// store.Save(progress);
    /// </code>
    /// </example>
    [Serializable]
    public sealed class PlayerProgress
    {
        /// <summary>Save format version, for future migrations.</summary>
        public int Version = 1;

        /// <summary>Levels 0..UnlockedCount-1 are playable.</summary>
        public int UnlockedCount = 1;

        /// <summary>Best stars per level (0 = not solved yet).</summary>
        public int[] Stars = new int[0];

        /// <summary>Hints owned.</summary>
        public int Hints;

        /// <summary>Language code, or empty to follow the device language.</summary>
        public string Language = "";

        /// <summary>Sound effects on/off.</summary>
        public bool SoundOn = true;

        /// <summary>New progress for a game with <paramref name="levelCount"/> levels.</summary>
        public static PlayerProgress Create(int levelCount, int startingHints)
        {
            var p = new PlayerProgress { Hints = startingHints };
            p.EnsureSize(levelCount);
            return p;
        }

        /// <summary>Grows or shrinks the star table to the level count (e.g. after an update adds levels).</summary>
        public void EnsureSize(int levelCount)
        {
            if (Stars == null) Stars = new int[0];
            if (Stars.Length != levelCount) Array.Resize(ref Stars, levelCount);
            UnlockedCount = Mathf.Clamp(UnlockedCount, 1, Mathf.Max(1, levelCount));
        }

        /// <summary>True if the level at a 0-based index can be played.</summary>
        public bool IsUnlocked(int index) => index >= 0 && index < UnlockedCount;

        /// <summary>Best stars for a level (0 if unsolved).</summary>
        public int GetStars(int index) => Stars[index];

        /// <summary>The level a "Play" button should open: the first unsolved unlocked level, else the last one.</summary>
        public int FrontierLevel => Mathf.Clamp(UnlockedCount - 1, 0, Stars.Length - 1);

        /// <summary>Stores a solve: keeps the best star count and unlocks the following level.</summary>
        public void RecordSolve(int index, int stars)
        {
            Stars[index] = Mathf.Max(Stars[index], Mathf.Clamp(stars, 1, 3));
            UnlockedCount = Mathf.Min(Stars.Length, Mathf.Max(UnlockedCount, index + 2));
        }

        /// <summary>Total best stars over a range of levels.</summary>
        public int StarsInRange(int start, int count)
        {
            int sum = 0;
            for (int i = start; i < start + count && i < Stars.Length; i++) sum += Stars[i];
            return sum;
        }

        /// <summary>Number of solved levels in a range.</summary>
        public int SolvedInRange(int start, int count)
        {
            int solved = 0;
            for (int i = start; i < start + count && i < Stars.Length; i++)
                if (Stars[i] > 0) solved++;
            return solved;
        }
    }
}
