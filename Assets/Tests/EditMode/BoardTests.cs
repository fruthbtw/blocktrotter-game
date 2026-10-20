using System;
using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class BoardTests
    {
        // Target on row 2 cols 0-1; vertical block C at col 3 rows 2-3.
        private const string TargetBlockedByVertical =
            "......" +
            "......" +
            "AA.C.." +
            "...C.." +
            "......" +
            "......";

        [Test]
        public void MoveIntoEmptySpace_IsLegal()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            Assert.IsTrue(board.CanApply(new Move(0, 1)));
        }

        [Test]
        public void MoveThroughAnotherBlock_IsRejected()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            // Target can slide 1 right before hitting C at col 3; 2 would overlap.
            Assert.IsFalse(board.CanApply(new Move(0, 2)));
        }

        [Test]
        public void MoveOffTheBoard_IsRejected()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            Assert.IsFalse(board.CanApply(new Move(0, -1)));
            Assert.IsFalse(board.CanApply(new Move(1, 3)));   // C rows 2-3 -> rows 5-6
        }

        [Test]
        public void ApplyIllegalMove_Throws()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            Assert.Throws<InvalidOperationException>(() => board.Apply(new Move(0, 2)));
        }

        [Test]
        public void Apply_ReturnsNewBoardAndLeavesOriginalUnchanged()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            Board next = board.Apply(new Move(1, 2));

            Assert.AreEqual(2, board.Blocks[1].Row);
            Assert.AreEqual(4, next.Blocks[1].Row);
        }

        [Test]
        public void GetMoveRange_ReportsFreeCellsInBothDirections()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            (int back, int fwd) = board.GetMoveRange(1);

            Assert.AreEqual(2, back);
            Assert.AreEqual(2, fwd);
        }

        [Test]
        public void TargetAtRightEdge_IsSolved()
        {
            Board board = LevelCodec.Decode(
                "......" +
                "......" +
                "....AA" +
                "......" +
                "......" +
                "......");

            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void TargetNotAtRightEdge_IsNotSolved()
        {
            Board board = LevelCodec.Decode(TargetBlockedByVertical);

            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void TargetOffTheExitRow_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new Board(new[]
            {
                new Block(0, 0, 2, Orientation.Horizontal, isTarget: true)
            }));
        }

        [Test]
        public void OverlappingBlocks_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new Board(new[]
            {
                new Block(2, 0, 2, Orientation.Horizontal, isTarget: true),
                new Block(1, 1, 2, Orientation.Vertical)
            }));
        }
    }
}
