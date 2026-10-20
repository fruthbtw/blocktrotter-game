using UnityEngine;

namespace Blocktrotter.Data
{
    /// <summary>
    /// Gameplay tuning values. One asset (Assets/_Game/Data/GameConfig.asset) is
    /// referenced by the game screen; change values there, not in code.
    /// </summary>
    /// <example><code>int stars = StarRating.Calculate(moves, par, config.TwoStarRatio);</code></example>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Blocktrotter/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Hints")]
        [Tooltip("Hints a new player starts with.")]
        [Min(0)] public int StartingHints = 3;

        [Header("Stars")]
        [Tooltip("2 stars up to ceil(optimal * ratio) moves; above that 1 star.")]
        [Min(1f)] public float TwoStarRatio = 1.5f;

        [Header("Ads")]
        [Tooltip("Show an interstitial after every N solved levels.")]
        [Min(1)] public int InterstitialEveryLevels = 5;

        [Tooltip("Never show two interstitials closer than this (seconds).")]
        [Min(0f)] public float InterstitialMinIntervalSeconds = 45f;

        [Tooltip("Hints granted for watching one rewarded ad.")]
        [Min(1)] public int HintsPerRewardedAd = 1;

        [Header("Themes")]
        [Tooltip("Levels per theme before the next theme starts.")]
        [Min(1)] public int LevelsPerTheme = 100;

        [Header("Feel")]
        [Tooltip("How quickly blocks snap to their cell (higher = snappier).")]
        [Min(1f)] public float BlockSnapSharpness = 22f;

        [Tooltip("Seconds between the target leaving the board and the solved panel (confetti plays meanwhile).")]
        [Min(0f)] public float SolvedPanelDelay = 0.9f;

        [Header("Celebration")]
        [Tooltip("Confetti pieces per solve.")]
        [Min(0)] public int ConfettiCount = 90;

        [Tooltip("Confetti lifetime in seconds.")]
        [Min(0.1f)] public float ConfettiDuration = 1.4f;

        [Tooltip("Solved panel pop-in duration in seconds.")]
        [Min(0.01f)] public float PanelPopDuration = 0.25f;

        [Tooltip("Seconds between each star popping in.")]
        [Min(0f)] public float StarPopInterval = 0.18f;

        [Header("Legal pages (published from legal-site/ to GitHub Pages)")]
        [Tooltip("Folder URL ending in '/', e.g. https://<user>.github.io/blocktrotter/")]
        public string LegalBaseUrl = "https://fruthbtw.github.io/blocktrotter/";

        /// <summary>Privacy policy URL for a language code ("en"/"tr").</summary>
        public string PrivacyUrl(string language) => LegalBaseUrl + (language == "tr" ? "privacy-tr.html" : "privacy-en.html");

        /// <summary>Terms of service URL for a language code ("en"/"tr").</summary>
        public string TermsUrl(string language) => LegalBaseUrl + (language == "tr" ? "terms-tr.html" : "terms-en.html");

        [Header("Background decor")]
        [Tooltip("Drifting decor pieces (petals, snow, ...) on screen at once.")]
        [Min(0)] public int DecorParticleCount = 18;
    }
}
