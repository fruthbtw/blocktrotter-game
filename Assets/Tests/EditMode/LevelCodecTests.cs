using System;
using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class LevelCodecTests
    {
        private const string Sample =
            "...BB." +
            "......" +
            "AA.C.." +
            "...C.." +
            "...DD." +
            "......";

        [Test]
        public void EncodeAfterDecode_ReturnsOriginalString()
        {
            Assert.AreEqual(Sample, LevelCodec.Encode(LevelCodec.Decode(Sample)));
        }

        [Test]
        public void Decode_ReadsOrientationAndLength()
        {
            Board board = LevelCodec.Decode(Sample);

            Block c = board.Blocks[2];
            Assert.AreEqual(Orientation.Vertical, c.Orientation);
            Assert.AreEqual(2, c.Length);
            Assert.AreEqual(2, c.Row);
            Assert.AreEqual(3, c.Col);
            Assert.IsTrue(board.Blocks[board.TargetIndex].IsTarget);
        }

        [Test]
        public void WrongLength_Throws()
        {
            Assert.Throws<FormatException>(() => LevelCodec.Decode("AA...."));
        }

        [Test]
        public void BlockWithGap_Throws()
        {
            Assert.Throws<FormatException>(() => LevelCodec.Decode(
                "B.B..." +
                "......" +
                "AA...." +
                "......" +
                "......" +
                "......"));
        }

        [Test]
        public void SingleCellBlock_Throws()
        {
            Assert.Throws<FormatException>(() => LevelCodec.Decode(
                "B....." +
                "......" +
                "AA...." +
                "......" +
                "......" +
                "......"));
        }
    }
}
