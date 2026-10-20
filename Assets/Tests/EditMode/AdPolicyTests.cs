using Blocktrotter.Core;
using NUnit.Framework;

namespace Blocktrotter.Tests
{
    public class AdPolicyTests
    {
        private const int SolvesPerAd = 5;
        private const double MinInterval = 45.0;

        private static void Solve(AdPolicy policy, int times)
        {
            for (int i = 0; i < times; i++) policy.RegisterSolve();
        }

        [Test]
        public void FewerSolvesThanThreshold_NoInterstitial()
        {
            var policy = new AdPolicy(SolvesPerAd, MinInterval);
            Solve(policy, SolvesPerAd - 1);

            Assert.IsFalse(policy.TryConsumeInterstitial(1000));
        }

        [Test]
        public void ReachingThreshold_ShowsInterstitialAndResetsCount()
        {
            var policy = new AdPolicy(SolvesPerAd, MinInterval);
            Solve(policy, SolvesPerAd);

            Assert.IsTrue(policy.TryConsumeInterstitial(1000));
            Assert.AreEqual(0, policy.SolvesSinceAd);
        }

        [Test]
        public void FirstInterstitial_IsNotBlockedByCooldown()
        {
            var policy = new AdPolicy(SolvesPerAd, MinInterval);
            Solve(policy, SolvesPerAd);

            Assert.IsTrue(policy.TryConsumeInterstitial(0));
        }

        [Test]
        public void WithinCooldown_WaitsEvenIfDue()
        {
            var policy = new AdPolicy(SolvesPerAd, MinInterval);
            Solve(policy, SolvesPerAd);
            policy.TryConsumeInterstitial(1000);
            Solve(policy, SolvesPerAd);

            Assert.IsFalse(policy.TryConsumeInterstitial(1000 + MinInterval - 1));
            Assert.IsTrue(policy.TryConsumeInterstitial(1000 + MinInterval));
        }

        [Test]
        public void DeclinedByCooldown_KeepsSolveCount()
        {
            var policy = new AdPolicy(SolvesPerAd, MinInterval);
            Solve(policy, SolvesPerAd);
            policy.TryConsumeInterstitial(1000);
            Solve(policy, SolvesPerAd + 2);

            policy.TryConsumeInterstitial(1001);

            Assert.AreEqual(SolvesPerAd + 2, policy.SolvesSinceAd);
        }
    }
}
