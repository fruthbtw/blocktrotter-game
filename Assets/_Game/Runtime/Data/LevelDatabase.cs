using System;
using System.Collections.Generic;
using Blocktrotter.Core;
using UnityEngine;

namespace Blocktrotter.Data
{
    /// <summary>
    /// All levels in play order (index 0 = level 1). Written by the editor's level
    /// generator; read at runtime.
    /// </summary>
    /// <example>
    /// <code>
    /// Board board = levelDatabase.LoadBoard(levelIndex);
    /// int par = levelDatabase.Get(levelIndex).MinMoves;
    /// </code>
    /// </example>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Blocktrotter/Level Database")]
    public sealed class LevelDatabase : ScriptableObject
    {
        /// <summary>One stored level.</summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>36-char <see cref="LevelCodec"/> string.</summary>
            public string Code;

            /// <summary>Optimal move count (used for stars).</summary>
            public int MinMoves;
        }

        [SerializeField] private List<Entry> _levels = new List<Entry>();

        /// <summary>Number of levels.</summary>
        public int Count => _levels.Count;

        /// <summary>Level entry at a 0-based index.</summary>
        public Entry Get(int index) => _levels[index];

        /// <summary>Decodes the level at a 0-based index into a playable board.</summary>
        public Board LoadBoard(int index) => LevelCodec.Decode(_levels[index].Code);

        /// <summary>Replaces all levels. Editor tooling only.</summary>
        public void SetLevels(IEnumerable<Entry> levels)
        {
            _levels = new List<Entry>(levels);
        }
    }
}
