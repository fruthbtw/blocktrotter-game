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
    }

    /// <summary>One generated level.</summary>
    public readonly struct GeneratedLevel
    {
        /// <summary>36-char <see cref="LevelCodec"/> string.</summary>
        public readonly string Code;

        /// <summary>Optimal move count.</summary>
        public readonly int MinMoves;

        /// <summary>Creates a level entry.</summary>
        public GeneratedLevel(string code, int minMoves)
        {
            Code = code;
            MinMoves = minMoves;
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
            var found = new List<(GeneratedLevel Level, int BlockCount)>();
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
                    Board board = cluster.GetBoardAtDistance(d, rng.Next(cluster.CountAtDistance(d)));
                    string code = LevelCodec.Encode(board);
                    if (!codes.Add(code)) continue;

                    found.Add((new GeneratedLevel(code, d), board.Blocks.Count));
                    needed[d] = want - 1;
                    remaining--;
                    taken++;
                }
                progress?.Invoke(o.LevelCount - remaining, attempts);
            }

            // Easiest first; at equal move counts, fewer blocks first.
            found.Sort((a, b) => a.Level.MinMoves != b.Level.MinMoves
                ? a.Level.MinMoves.CompareTo(b.Level.MinMoves)
                : a.BlockCount.CompareTo(b.BlockCount));

            var levels = found.ConvertAll(f => f.Level);
            var missing = new Dictionary<int, int>();
            foreach (var pair in needed)
                if (pair.Value > 0) missing[pair.Key] = pair.Value;
            return new LevelSetResult(levels, missing, attempts);
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
