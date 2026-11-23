using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class PuzzleDifficultyTests
    {
        // Four verticals stand in the exit row and each can simply drop down:
        // the "just clear the row" puzzle the player found too easy (old level 1 shape).
        private const string ClearTheRow =
            "......" +
            "...C.E" +
            "AABCDE" +
            "..BCD." +
            "..B.D." +
            "......";

        // C blocks the target, but C itself is pinned by B above and D below:
        // one of those must move first (a blocker-of-a-blocker).
        private const string PinnedBlocker =
            "...BB." +
            "......" +
            "AA.C.." +
            "...C.." +
            "...DD." +
            "......";

        [Test]
        public void ClearTheRowPuzzle_HasNoDetours()
        {
            PuzzleDifficulty d = PuzzleDifficulty.Analyze(LevelCodec.Decode(ClearTheRow));

            Assert.AreEqual(4, d.DirectBlockers);
            Assert.AreEqual(5, d.MinMoves);
            Assert.AreEqual(0, d.Detours);
        }

        [Test]
        public void PinnedBlocker_CountsOneDetour()
        {
            PuzzleDifficulty d = PuzzleDifficulty.Analyze(LevelCodec.Decode(PinnedBlocker));

            Assert.AreEqual(1, d.DirectBlockers);
            Assert.AreEqual(3, d.MinMoves);
            Assert.AreEqual(1, d.Detours);
        }

        [Test]
        public void DetourScore_RanksPinnedBlockerAboveClearTheRow()
        {
            PuzzleDifficulty easy = PuzzleDifficulty.Analyze(LevelCodec.Decode(ClearTheRow));
            PuzzleDifficulty pinned = PuzzleDifficulty.Analyze(LevelCodec.Decode(PinnedBlocker));

            // Detours only: the 3-move pinned puzzle outranks the 5-move clear-the-row one.
            Assert.Greater(pinned.Score(1f, 0f, 0f), easy.Score(1f, 0f, 0f));
        }

        [Test]
        public void SolvedBoard_HasNoBlockersOrDetours()
        {
            PuzzleDifficulty d = PuzzleDifficulty.Analyze(LevelCodec.Decode(
                "......" +
                "......" +
                "....AA" +
                "......" +
                "......" +
                "......"));

            Assert.AreEqual(0, d.DirectBlockers);
            Assert.AreEqual(0, d.MinMoves);
            Assert.AreEqual(0, d.Detours);
        }

        [Test]
        public void Solver_ReportsExploredStates()
        {
            SolveResult r = BoardSolver.Solve(LevelCodec.Decode(PinnedBlocker));

            Assert.Greater(r.StatesExplored, r.MinMoves);
        }
    }
}
