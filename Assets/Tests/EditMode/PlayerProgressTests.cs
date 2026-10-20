using Blocktrotter.Save;
using NUnit.Framework;
using UnityEngine;

namespace Blocktrotter.Tests
{
    public class PlayerProgressTests
    {
        private const int LevelCount = 10;
        private const int StartingHints = 3;

        [Test]
        public void NewProgress_UnlocksOnlyFirstLevel()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);

            Assert.IsTrue(p.IsUnlocked(0));
            Assert.IsFalse(p.IsUnlocked(1));
            Assert.AreEqual(StartingHints, p.Hints);
        }

        [Test]
        public void SolvingFrontier_UnlocksNextLevel()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);

            p.RecordSolve(0, 2);

            Assert.IsTrue(p.IsUnlocked(1));
            Assert.AreEqual(1, p.FrontierLevel);
        }

        [Test]
        public void ReplayingOldLevel_DoesNotUnlockFurther()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            p.RecordSolve(0, 1);
            p.RecordSolve(1, 1);

            p.RecordSolve(0, 3);

            Assert.AreEqual(3, p.UnlockedCount);
        }

        [Test]
        public void RecordSolve_KeepsBestStars()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            p.RecordSolve(0, 3);

            p.RecordSolve(0, 1);

            Assert.AreEqual(3, p.GetStars(0));
        }

        [Test]
        public void SolvingLastLevel_DoesNotOverflow()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            for (int i = 0; i < LevelCount; i++) p.RecordSolve(i, 3);

            Assert.AreEqual(LevelCount, p.UnlockedCount);
            Assert.AreEqual(LevelCount - 1, p.FrontierLevel);
        }

        [Test]
        public void RangeTotals_CountStarsAndSolves()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            p.RecordSolve(0, 3);
            p.RecordSolve(1, 2);

            Assert.AreEqual(5, p.StarsInRange(0, 5));
            Assert.AreEqual(2, p.SolvedInRange(0, 5));
            Assert.AreEqual(0, p.SolvedInRange(5, 5));
        }

        [Test]
        public void JsonRoundTrip_PreservesProgress()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            p.RecordSolve(0, 3);
            p.Language = "tr";
            p.SoundOn = false;

            var copy = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(p));

            Assert.AreEqual(2, copy.UnlockedCount);
            Assert.AreEqual(3, copy.GetStars(0));
            Assert.AreEqual("tr", copy.Language);
            Assert.IsFalse(copy.SoundOn);
        }

        [Test]
        public void EnsureSize_GrowsWhenLevelsAreAdded()
        {
            PlayerProgress p = PlayerProgress.Create(LevelCount, StartingHints);
            p.RecordSolve(0, 2);

            p.EnsureSize(LevelCount * 2);

            Assert.AreEqual(LevelCount * 2, p.Stars.Length);
            Assert.AreEqual(2, p.GetStars(0));
        }
    }
}
