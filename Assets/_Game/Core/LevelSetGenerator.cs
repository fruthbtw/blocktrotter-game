using System;
using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>Tuning for <see cref="LevelSetGenerator"/>. Values come from the editor settings asset.</summary>
    public sealed class LevelGenerationOptions
    {
        /// <summary>Number of levels to produce.</summary>
        public int LevelCount = 500;

        /// <summary>Optimal move count of the first level.</summary>
        public int MinMoves = 2;

        /// <summary>Optimal move count of the last level.</summary>
        public int MaxMoves = 30;

        /// <summary>Curve shape for the difficulty ramp (1 = linear, &gt;1 = slower start).</summary>
        public double CurveExponent = 1.3;

        /// <summary>Fewest non-target blocks on a random board.</summary>
        public int MinBlocks = 6;

        /// <summary>Most non-target blocks on a random board.</summary>
        public int MaxBlocks = 13;

        /// <summary>Chance (0–1) that a placed block is 3 cells long instead of 2.</summary>
        public double LongBlockChance = 0.3;

        /// <summary>Most levels taken from one cluster, so the set stays varied.</summary>
        public int MaxLevelsPerCluster = 2;

        /// <summary>Clusters larger than this are skipped to bound time and memory.</summary>
        public int MaxClusterStates = 300_000;

        /// <summary>Random boards to try before giving up.</summary>
        public int MaxAttempts = 200_000;

        /// <summary>Seed; the same seed and options give the same level set.</summary>
        public int Seed = 20261020;

        /// <summary>Candidate puzzles compared (by <see cref="PuzzleDifficulty"/>) for each level picked.</summary>
        public int CandidatesPerPick = 12;

        /// <summary>Puzzles with fewer detours than this are rejected (0 allows "just clear the row" puzzles).</summary>
        public int MinDetours = 1;

        /// <summary>Score weight per detour move (blocker-of-a-blocker).</summary>
        public float DetourWeight = 3f;

        /// <summary>Score weight per reversal (a block moved back the other way).</summary>
        public float ReversalWeight = 2f;

        /// <summary>Score weight per doubling of positions the solver explored.</summary>
        public float EffortWeight = 1f;
    }

    /// <summary>One generated level.</summary>
    public readonly struct GeneratedLevel
    {
        /// <summary>36-char <see cref="LevelCodec"/> string.</summary>
        public readonly string Code;

        /// <summary>Optimal move count.</summary>
        public readonly int MinMoves;

        /// <summary>Thinking-difficulty score (see <see cref="PuzzleDifficulty.Score"/>); orders levels with equal move counts.</summary>
        public readonly float Score;

        /// <summary>Creates a level entry.</summary>
        public GeneratedLevel(string code, int minMoves, float score = 0f)
        {
            Code = code;
            MinMoves = minMoves;
            Score = score;
        }
    }

    /// <summary>Outcome of a generation run.</summary>
    public sealed class LevelSetResult
    {
        /// <summary>Levels ordered easiest first. Shorter than requested if <see cref="Missing"/> is non-empty.</summary>
        public IReadOnlyList<GeneratedLevel> Levels { get; }

        /// <summary>Move counts that could not be filled, with how many were still needed.</summary>
        public IReadOnlyDictionary<int, int> Missing { get; }

        /// <summary>Random boards tried.</summary>
        public int Attempts { get; }

        /// <summary>True when every requested level was produced.</summary>
        public bool IsComplete => Missing.Count == 0;

        internal LevelSetResult(IReadOnlyList<GeneratedLevel> levels, IReadOnlyDictionary<int, int> missing, int attempts)
        {
            Levels = levels;
            Missing = missing;
            Attempts = attempts;
        }
    }

    /// <summary>
    /// Builds an original level set: random boards are explored as clusters and
    /// positions at the exact move counts the difficulty ramp asks for are kept.
    /// For each pick several candidates are compared and the one that makes the
    /// player think most (detours, reversals, search effort) wins; "just clear the
    /// row" puzzles are rejected.
    /// </summary>
    /// <example>
    /// <code>
    /// LevelSetResult r = LevelSetGenerator.Generate(new LevelGenerationOptions { LevelCount = 500 });
    /// if (!r.IsComplete) { /* report r.Missing */ }
    /// </code>
    /// </example>
    public static class LevelSetGenerator
    {
        /// <summary>Optimal move count wanted for the level at <paramref name="index"/> (0-based).</summary>
        public static int TargetMoves(int index, LevelGenerationOptions o)
        {
            if (o.LevelCount <= 1) return o.MinMoves;
            double t = Math.Pow((double)index / (o.LevelCount - 1), o.CurveExponent);
            return (int)Math.Round(o.MinMoves + (o.MaxMoves - o.MinMoves) * t);
        }

        /// <summary>Runs the generator. Deterministic for a given options object.</summary>
        public static LevelSetResult Generate(LevelGenerationOptions o, Action<int, int> progress = null)
        {
            var needed = new SortedDictionary<int, int>();
            for (int i = 0; i < o.LevelCount; i++)
            {
                int m = TargetMoves(i, o);
                needed[m] = needed.TryGetValue(m, out int c) ? c + 1 : 1;
            }

            var rng = new Random(o.Seed);
            var found = new List<GeneratedLevel>();
            var codes = new HashSet<string>();
            int remaining = o.LevelCount;
            int attempts = 0;

            while (remaining > 0 && attempts < o.MaxAttempts)
            {
                attempts++;
                Board start = RandomBoard(rng, rng.Next(o.MinBlocks, o.MaxBlocks + 1), o.LongBlockChance);
                StateCluster cluster = StateCluster.Explore(start, o.MaxClusterStates);
                if (cluster == null || cluster.MaxDistance < 1) continue;

                int taken = 0;
                // Hardest first: high move counts are the scarce ones.
                for (int d = Math.Min(cluster.MaxDistance, o.MaxMoves); d >= o.MinMoves && taken < o.MaxLevelsPerCluster; d--)
                {
                    if (!needed.TryGetValue(d, out int want) || want == 0) continue;
                    GeneratedLevel? best = PickMostDemanding(cluster, d, rng, o, codes);
                    if (best == null) continue;

                    codes.Add(best.Value.Code);
                    found.Add(best.Value);
                    needed[d] = want - 1;
                    remaining--;
                    taken++;
                }
                progress?.Invoke(o.LevelCount - remaining, attempts);
            }

            // Easiest first; at equal move counts, the less demanding puzzle first.
            found.Sort((a, b) => a.MinMoves != b.MinMoves ? a.MinMoves.CompareTo(b.MinMoves) : a.Score.CompareTo(b.Score));

            var levels = found;
            var missing = new Dictionary<int, int>();
            foreach (var pair in needed)
                if (pair.Value > 0) missing[pair.Key] = pair.Value;
            return new LevelSetResult(levels, missing, attempts);
        }

        /// <summary>
        /// Samples up to <see cref="LevelGenerationOptions.CandidatesPerPick"/> unused positions at
        /// distance <paramref name="d"/> and returns the highest-scoring one with enough detours, or null.
        /// </summary>
        private static GeneratedLevel? PickMostDemanding(StateCluster cluster, int d, Random rng,
            LevelGenerationOptions o, HashSet<string> usedCodes)
        {
            GeneratedLevel? best = null;
            int count = cluster.CountAtDistance(d);
            for (int c = 0; c < Math.Min(o.CandidatesPerPick, count); c++)
            {
                Board board = cluster.GetBoardAtDistance(d, rng.Next(count));
                string code = LevelCodec.Encode(board);
                if (usedCodes.Contains(code)) continue;

                PuzzleDifficulty difficulty = PuzzleDifficulty.Analyze(board);
                if (difficulty.MinMoves != d || difficulty.Detours < o.MinDetours) continue;

                float score = difficulty.Score(o.DetourWeight, o.ReversalWeight, o.EffortWeight);
                if (best == null || score > best.Value.Score)
                    best = new GeneratedLevel(code, d, score);
            }
            return best;
        }

        /// <summary>
        /// Random legal board: target on the exit row, then non-overlapping blocks.
        /// No horizontal blocks on the exit row, since one right of the target makes
        /// the board unsolvable and one left of it never matters.
        /// </summary>
        public static Board RandomBoard(Random rng, int blockCount, double longBlockChance)
        {
            int size = Board.DefaultSize, exitRow = Board.DefaultExitRow;
            var occupied = new bool[size, size];
            var blocks = new List<Block>();

            var target = new Block(exitRow, rng.Next(0, size - 3), 2, Orientation.Horizontal, isTarget: true);
            Place(target, occupied);
            blocks.Add(target);

            for (int tries = 0; blocks.Count <= blockCount && tries < blockCount * 50; tries++)
            {
                int length = rng.NextDouble() < longBlockChance ? 3 : 2;
                var orientation = rng.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                int row = orientation == Orientation.Vertical ? rng.Next(size - length + 1) : rng.Next(size);
                int col = orientation == Orientation.Horizontal ? rng.Next(size - length + 1) : rng.Next(size);
                if (orientation == Orientation.Horizontal && row == exitRow) continue;

                var block = new Block(row, col, length, orientation);
                if (Overlaps(block, occupied)) continue;
                Place(block, occupied);
                blocks.Add(block);
            }
            return new Board(blocks, size, exitRow);
        }

        private static bool Overlaps(Block b, bool[,] occupied)
        {
            for (int k = 0; k < b.Length; k++)
            {
                int r = b.Orientation == Orientation.Vertical ? b.Row + k : b.Row;
                int c = b.Orientation == Orientation.Horizontal ? b.Col + k : b.Col;
                if (occupied[r, c]) return true;
            }
            return false;
        }

        private static void Place(Block b, bool[,] occupied)
        {
            for (int k = 0; k < b.Length; k++)
            {
                int r = b.Orientation == Orientation.Vertical ? b.Row + k : b.Row;
                int c = b.Orientation == Orientation.Horizontal ? b.Col + k : b.Col;
                occupied[r, c] = true;
            }
        }
    }
}
