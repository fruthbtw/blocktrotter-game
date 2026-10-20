using System;
using System.Collections.Generic;
using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class LevelSetGeneratorTests
    {
        private static LevelGenerationOptions SmallSet() => new LevelGenerationOptions
        {
            LevelCount = 20,
            MinMoves = 2,
            MaxMoves = 12,
            MaxAttempts = 20_000,
            Seed = 1234
        };

        [Test]
        public void TargetMoves_RampsFromMinToMax()
        {
            LevelGenerationOptions o = SmallSet();

            Assert.AreEqual(o.MinMoves, LevelSetGenerator.TargetMoves(0, o));
            Assert.AreEqual(o.MaxMoves, LevelSetGenerator.TargetMoves(o.LevelCount - 1, o));
            for (int i = 1; i < o.LevelCount; i++)
                Assert.GreaterOrEqual(LevelSetGenerator.TargetMoves(i, o), LevelSetGenerator.TargetMoves(i - 1, o));
        }

        [Test]
        public void Generate_SmallSet_IsCompleteAndStoredMovesMatchSolver()
        {
            LevelSetResult result = LevelSetGenerator.Generate(SmallSet());

            Assert.IsTrue(result.IsComplete, "Missing: " + string.Join(", ", result.Missing));
            foreach (GeneratedLevel level in result.Levels)
                Assert.AreEqual(level.MinMoves, BoardSolver.Solve(LevelCodec.Decode(level.Code)).MinMoves, level.Code);
        }

        [Test]
        public void Generate_SameSeed_GivesSameLevels()
        {
            LevelSetResult a = LevelSetGenerator.Generate(SmallSet());
            LevelSetResult b = LevelSetGenerator.Generate(SmallSet());

            Assert.AreEqual(a.Levels.Count, b.Levels.Count);
            for (int i = 0; i < a.Levels.Count; i++)
                Assert.AreEqual(a.Levels[i].Code, b.Levels[i].Code);
        }

        [Test]
        public void Generate_LevelsAreUniqueAndOrderedEasiestFirst()
        {
            LevelSetResult result = LevelSetGenerator.Generate(SmallSet());

            var codes = new HashSet<string>();
            for (int i = 0; i < result.Levels.Count; i++)
            {
                Assert.IsTrue(codes.Add(result.Levels[i].Code), "Duplicate level " + result.Levels[i].Code);
                if (i > 0)
                    Assert.GreaterOrEqual(result.Levels[i].MinMoves, result.Levels[i - 1].MinMoves);
            }
        }

        [Test]
        public void RandomBoard_NeverPutsHorizontalBlocksOnExitRow()
        {
            var rng = new Random(7);
            for (int n = 0; n < 200; n++)
            {
                Board board = LevelSetGenerator.RandomBoard(rng, 12, 0.3);
                for (int i = 0; i < board.Blocks.Count; i++)
                {
                    Block b = board.Blocks[i];
                    if (!b.IsTarget && b.Orientation == Orientation.Horizontal)
                        Assert.AreNotEqual(board.ExitRow, b.Row);
                }
            }
        }

        [Test]
        public void Cluster_DistancesMatchSolver()
        {
            Board start = LevelCodec.Decode(
                "...BB." +
                "......" +
                "AA.C.." +
                "...C.." +
                "...DD." +
                "......");

            StateCluster cluster = StateCluster.Explore(start, 100_000);

            Assert.IsNotNull(cluster);
            for (int d = 1; d <= cluster.MaxDistance; d++)
                for (int i = 0; i < cluster.CountAtDistance(d); i++)
                    Assert.AreEqual(d, BoardSolver.Solve(cluster.GetBoardAtDistance(d, i)).MinMoves);
        }
    }
}
