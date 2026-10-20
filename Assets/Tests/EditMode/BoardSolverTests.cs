using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class BoardSolverTests
    {
        [Test]
        public void AlreadySolvedBoard_NeedsZeroMoves()
        {
            Board board = LevelCodec.Decode(
                "......" +
                "......" +
                "....AA" +
                "......" +
                "......" +
                "......");

            SolveResult result = BoardSolver.Solve(board);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(0, result.MinMoves);
        }

        [Test]
        public void SingleBlocker_SolvesInTwoMoves()
        {
            // Move C out of row 2, then slide A to the exit.
            Board board = LevelCodec.Decode(
                "......" +
                "...C.." +
                "AA.C.." +
                "......" +
                "......" +
                "......");

            SolveResult result = BoardSolver.Solve(board);

            Assert.AreEqual(2, result.MinMoves);
        }

        [Test]
        public void BlockerPinnedBothWays_SolvesInThreeMoves()
        {
            // C cannot leave row 2 until B (above) or D (below) moves first.
            Board board = LevelCodec.Decode(
                "...BB." +
                "......" +
                "AA.C.." +
                "...C.." +
                "...DD." +
                "......");

            SolveResult result = BoardSolver.Solve(board);

            Assert.AreEqual(3, result.MinMoves);
        }

        [Test]
        public void SolutionMoves_ReplayToASolvedBoard()
        {
            Board board = LevelCodec.Decode(
                "...BB." +
                "......" +
                "AA.C.." +
                "...C.." +
                "...DD." +
                "......");

            SolveResult result = BoardSolver.Solve(board);
            foreach (Move move in result.Moves)
                board = board.Apply(move);

            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void ColumnFullyBlocked_IsUnsolvable()
        {
            // Two length-3 verticals fill column 4; neither can move.
            Board board = LevelCodec.Decode(
                "....B." +
                "....B." +
                "AA..B." +
                "....C." +
                "....C." +
                "....C.");

            SolveResult result = BoardSolver.Solve(board);

            Assert.IsFalse(result.IsSolvable);
            Assert.AreEqual(-1, result.MinMoves);
        }

        [Test]
        public void Hint_IsFirstMoveOfAnOptimalSolution()
        {
            Board board = LevelCodec.Decode(
                "...BB." +
                "......" +
                "AA.C.." +
                "...C.." +
                "...DD." +
                "......");

            Move? hint = BoardSolver.GetHint(board);

            Assert.IsTrue(hint.HasValue);
            Board after = board.Apply(hint.Value);
            Assert.AreEqual(2, BoardSolver.Solve(after).MinMoves);
        }

        [Test]
        public void Hint_OnSolvedBoard_IsNull()
        {
            Board board = LevelCodec.Decode(
                "......" +
                "......" +
                "....AA" +
                "......" +
                "......" +
                "......");

            Assert.IsNull(BoardSolver.GetHint(board));
        }
    }
}
