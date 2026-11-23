using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class TutorialScriptTests
    {
        [Test]
        public void TutorialBoard_NeedsExactlyOneMovePerStep()
        {
            Board board = TutorialScript.CreateBoard();

            Assert.AreEqual(TutorialScript.Steps.Count, BoardSolver.Solve(board).MinMoves);
        }

        [Test]
        public void NoStep_IsDoneAtTheStart()
        {
            Board board = TutorialScript.CreateBoard();

            foreach (TutorialStep step in TutorialScript.Steps)
                Assert.IsFalse(step.IsDone(board), step.TextKey);
        }

        [Test]
        public void SuggestedMoves_CompleteEveryStepInOrderAndSolve()
        {
            Board board = TutorialScript.CreateBoard();

            foreach (TutorialStep step in TutorialScript.Steps)
            {
                var move = new Move(step.BlockIndex, step.SuggestedDelta);
                Assert.IsTrue(board.CanApply(move), $"{step.TextKey}: suggested move is illegal");
                board = board.Apply(move);
                Assert.IsTrue(step.IsDone(board), step.TextKey);
            }
            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void SecondVertical_CannotSkipTheHorizontalStep()
        {
            // E sits above C, and D sits below it: C cannot clear the red row until D moves.
            Board board = TutorialScript.CreateBoard().Apply(new Move(TutorialScript.Steps[0].BlockIndex, -1));
            int c = TutorialScript.Steps[2].BlockIndex;

            (int back, int forward) = board.GetMoveRange(c);

            Assert.AreEqual(0, back);
            Assert.AreEqual(0, forward);
        }
    }
}
