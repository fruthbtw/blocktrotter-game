using Blocktrotter.Core;
using UnityEngine;

namespace Blocktrotter.EditorTools
{
    /// <summary>Editor-only tuning for the level generator; see <see cref="LevelGenerationOptions"/>.</summary>
    public sealed class LevelGenerationSettings : ScriptableObject
    {
        [Min(1)] public int LevelCount = 500;
        [Min(1)] public int MinMoves = 2;
        [Min(1)] public int MaxMoves = 30;
        [Min(0.1f)] public float CurveExponent = 1.3f;
        [Range(1, 15)] public int MinBlocks = 6;
        [Range(1, 15)] public int MaxBlocks = 13;
        [Range(0f, 1f)] public float LongBlockChance = 0.3f;
        [Min(1)] public int MaxLevelsPerCluster = 2;
        [Min(1000)] public int MaxClusterStates = 300_000;
        [Min(1)] public int MaxAttempts = 200_000;
        public int Seed = 20261020;

        /// <summary>Copies these values into the pure-C# options object.</summary>
        public LevelGenerationOptions ToOptions() => new LevelGenerationOptions
        {
            LevelCount = LevelCount,
            MinMoves = MinMoves,
            MaxMoves = MaxMoves,
            CurveExponent = CurveExponent,
            MinBlocks = MinBlocks,
            MaxBlocks = MaxBlocks,
            LongBlockChance = LongBlockChance,
            MaxLevelsPerCluster = MaxLevelsPerCluster,
            MaxClusterStates = MaxClusterStates,
            MaxAttempts = MaxAttempts,
            Seed = Seed
        };
    }
}
