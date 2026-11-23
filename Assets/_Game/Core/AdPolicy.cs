namespace Blocktrotter.Core
{
    /// <summary>
    /// When an interstitial may be shown. It is due after every <c>solvesPerAd</c>
    /// solved levels, or once <c>maxSecondsBetweenAds</c> have passed since the last
    /// interstitial (or since start-up) — whichever comes first — and never sooner
    /// than <c>minIntervalSeconds</c> after the previous one. The caller only asks at a
    /// natural break (the solved panel's Next), so a timer that runs out mid-puzzle
    /// waits for the level to end. Time is passed in, so the rule is testable without a clock.
    /// </summary>
    /// <example>
    /// <code>
    /// var policy = new AdPolicy(config.InterstitialEveryLevels, config.InterstitialMinIntervalSeconds,
    ///     config.InterstitialEverySeconds, Time.realtimeSinceStartupAsDouble);
    /// policy.RegisterSolve();
    /// if (policy.TryConsumeInterstitial(Time.realtimeSinceStartupAsDouble)) ads.ShowInterstitial(next);
    /// </code>
    /// </example>
    public sealed class AdPolicy
    {
        private readonly int _solvesPerAd;
        private readonly double _minIntervalSeconds;
        private readonly double _maxSecondsBetweenAds;
        private double _lastShownAt = double.NegativeInfinity;
        private double _timerStart;

        /// <summary>Solved levels since the last interstitial.</summary>
        public int SolvesSinceAd { get; private set; }

        /// <summary>
        /// Creates the policy. <paramref name="maxSecondsBetweenAds"/> defaults to no time trigger;
        /// <paramref name="startSeconds"/> is "now" when the game starts (the time trigger counts from it).
        /// </summary>
        public AdPolicy(int solvesPerAd, double minIntervalSeconds,
            double maxSecondsBetweenAds = double.PositiveInfinity, double startSeconds = 0)
        {
            _solvesPerAd = solvesPerAd < 1 ? 1 : solvesPerAd;
            _minIntervalSeconds = minIntervalSeconds;
            _maxSecondsBetweenAds = maxSecondsBetweenAds;
            _timerStart = startSeconds;
        }

        /// <summary>Counts one solved level.</summary>
        public void RegisterSolve() => SolvesSinceAd++;

        /// <summary>
        /// True if an interstitial is due now; if so, resets the solve count, the time
        /// trigger and the cooldown as if it was shown.
        /// </summary>
        public bool TryConsumeInterstitial(double nowSeconds)
        {
            bool dueBySolves = SolvesSinceAd >= _solvesPerAd;
            bool dueByTime = nowSeconds - _timerStart >= _maxSecondsBetweenAds;
            if (!(dueBySolves || dueByTime) || nowSeconds - _lastShownAt < _minIntervalSeconds)
                return false;
            SolvesSinceAd = 0;
            _lastShownAt = nowSeconds;
            _timerStart = nowSeconds;
            return true;
        }
    }
}
