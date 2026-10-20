namespace Blocktrotter.Core
{
    /// <summary>
    /// When an interstitial may be shown: after every <c>solvesPerAd</c> solved
    /// levels, and never sooner than <c>minIntervalSeconds</c> after the previous
    /// one. Time is passed in, so the rule is testable without a clock.
    /// </summary>
    /// <example>
    /// <code>
    /// var policy = new AdPolicy(config.InterstitialEveryLevels, config.InterstitialMinIntervalSeconds);
    /// policy.RegisterSolve();
    /// if (policy.TryConsumeInterstitial(Time.realtimeSinceStartupAsDouble)) ads.ShowInterstitial(next);
    /// </code>
    /// </example>
    public sealed class AdPolicy
    {
        private readonly int _solvesPerAd;
        private readonly double _minIntervalSeconds;
        private double _lastShownAt = double.NegativeInfinity;

        /// <summary>Solved levels since the last interstitial.</summary>
        public int SolvesSinceAd { get; private set; }

        /// <summary>Creates the policy.</summary>
        public AdPolicy(int solvesPerAd, double minIntervalSeconds)
        {
            _solvesPerAd = solvesPerAd < 1 ? 1 : solvesPerAd;
            _minIntervalSeconds = minIntervalSeconds;
        }

        /// <summary>Counts one solved level.</summary>
        public void RegisterSolve() => SolvesSinceAd++;

        /// <summary>
        /// True if an interstitial is due now; if so, resets the counter and the
        /// cooldown as if it was shown.
        /// </summary>
        public bool TryConsumeInterstitial(double nowSeconds)
        {
            if (SolvesSinceAd < _solvesPerAd || nowSeconds - _lastShownAt < _minIntervalSeconds)
                return false;
            SolvesSinceAd = 0;
            _lastShownAt = nowSeconds;
            return true;
        }
    }
}
