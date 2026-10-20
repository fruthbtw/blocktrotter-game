using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class StarRatingTests
    {
        private const float TwoStarRatio = 1.5f;

        [Test]
        public void OptimalMoves_GivesThreeStars()
        {
            Assert.AreEqual(3, StarRating.Calculate(8, 8, TwoStarRatio));
        }

        [Test]
        public void WithinRatio_GivesTwoStars()
        {
            // ceil(8 * 1.5) = 12 is the last two-star count.
            Assert.AreEqual(2, StarRating.Calculate(12, 8, TwoStarRatio));
        }

        [Test]
        public void BeyondRatio_GivesOneStar()
        {
            Assert.AreEqual(1, StarRating.Calculate(13, 8, TwoStarRatio));
        }

        [Test]
        public void RatioRoundsUp_ForOddOptimal()
        {
            // ceil(5 * 1.5) = 8.
            Assert.AreEqual(2, StarRating.Calculate(8, 5, TwoStarRatio));
            Assert.AreEqual(1, StarRating.Calculate(9, 5, TwoStarRatio));
        }
    }
}
