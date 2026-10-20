using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class GameSessionTests
    {
        // Optimal solution is 3 moves.
        private const string ThreeMoveLevel =
            "...BB." +
            "......" +
            "AA.C.." +
            "...C.." +
            "...DD." +
            "......";

        private static GameSession NewSession() => new GameSession(LevelCodec.Decode(ThreeMoveLevel));

        [Test]
        public void LegalMove_IncrementsMoveCount()
        {
            GameSession session = NewSession();

            Assert.IsTrue(session.TryMove(new Move(1, -3)));   // B: cols 3-4 -> 0-1

            Assert.AreEqual(1, session.MoveCount);
        }

        [Test]
        public void IllegalMove_ChangesNothing()
        {
            GameSession session = NewSession();
            Board before = session.Board;

            Assert.IsFalse(session.TryMove(new Move(0, 3)));   // A would pass through C

            Assert.AreEqual(0, session.MoveCount);
            Assert.AreSame(before, session.Board);
        }

        [Test]
        public void Undo_RestoresPreviousBoardAndCount()
        {
            GameSession session = NewSession();
            Board before = session.Board;
            session.TryMove(new Move(1, -3));

            Assert.IsTrue(session.Undo());

            Assert.AreEqual(0, session.MoveCount);
            Assert.AreSame(before, session.Board);
        }

        [Test]
        public void Undo_WithNoMoves_ReturnsFalse()
        {
            Assert.IsFalse(NewSession().Undo());
        }

        [Test]
        public void Restart_ReturnsToInitialBoardWithZeroMoves()
        {
            GameSession session = NewSession();
            session.TryMove(new Move(1, -3));
            session.TryMove(new Move(2, -2));

            session.Restart();

            Assert.AreEqual(0, session.MoveCount);
            Assert.AreSame(session.InitialBoard, session.Board);
        }

        [Test]
        public void SolvedSession_RejectsMovesAndUndo()
        {
            GameSession session = NewSession();
            var wallet = new HintWallet(3);
            while (!session.IsSolved) session.UseHint(wallet);

            Assert.IsFalse(session.TryMove(new Move(1, 1)));
            Assert.IsFalse(session.Undo());
        }

        [Test]
        public void Hints_SolveLevelInOptimalMoves()
        {
            GameSession session = NewSession();
            var wallet = new HintWallet(3);

            while (!session.IsSolved)
                Assert.IsTrue(session.UseHint(wallet).HasValue);

            Assert.AreEqual(3, session.MoveCount);
            Assert.AreEqual(0, wallet.Count);
        }

        [Test]
        public void Hint_WithEmptyWallet_DoesNothing()
        {
            GameSession session = NewSession();
            var wallet = new HintWallet(0);

            Assert.IsNull(session.UseHint(wallet));
            Assert.AreEqual(0, session.MoveCount);
        }

        [Test]
        public void Hint_OnSolvedLevel_DoesNotSpend()
        {
            GameSession session = NewSession();
            var wallet = new HintWallet(5);
            while (!session.IsSolved) session.UseHint(wallet);
            int left = wallet.Count;

            Assert.IsNull(session.UseHint(wallet));
            Assert.AreEqual(left, wallet.Count);
        }
    }
}
