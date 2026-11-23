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

        [Tooltip("Also show an interstitial once this many seconds have passed since the last one (checked only at the solved panel's Next, never mid-puzzle).")]
        [Min(1f)] public float InterstitialEverySeconds = 200f;

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

        [Header("Red block exit")]
        [Tooltip("Seconds the red block pulls back before launching (anticipation).")]
        [Min(0f)] public float ExitWindupSeconds = 0.14f;

        [Tooltip("Seconds from launch until the red block is through the gate.")]
        [Min(0.05f)] public float ExitLaunchSeconds = 0.34f;

        [Tooltip("Seconds between trail copies left behind during the launch.")]
        [Min(0.01f)] public float ExitTrailInterval = 0.03f;

        [Tooltip("Seconds each trail copy takes to fade.")]
        [Min(0.05f)] public float ExitTrailFade = 0.28f;

        [Tooltip("Seconds between light sweeps across the red block while playing.")]
        [Min(0.5f)] public float TargetShinePeriod = 4f;

        [Header("Celebration")]
        [Tooltip("Confetti pieces per solve.")]
        [Min(0)] public int ConfettiCount = 90;

        [Tooltip("Confetti lifetime in seconds.")]
        [Min(0.1f)] public float ConfettiDuration = 1.4f;

        [Tooltip("Solved panel pop-in duration in seconds.")]
        [Min(0.01f)] public float PanelPopDuration = 0.25f;

        [Tooltip("Seconds between each star popping in.")]
        [Min(0f)] public float StarPopInterval = 0.18f;

        [Header("Legal pages (hosted on the Kayu Studio GitHub Pages site)")]
        [Tooltip("Folder URL ending in '/', e.g. https://kayustudio.github.io/blocktrotter/")]
        public string LegalBaseUrl = "https://kayustudio.github.io/blocktrotter/";

        /// <summary>Privacy policy URL for a language code ("en"/"tr").</summary>
        public string PrivacyUrl(string language) => LegalBaseUrl + (language == "tr" ? "privacy-tr.html" : "privacy.html");

        /// <summary>Terms of service URL for a language code ("en"/"tr").</summary>
        public string TermsUrl(string language) => LegalBaseUrl + (language == "tr" ? "terms-tr.html" : "terms.html");

        [Tooltip("Address the Settings 'Contact Us' button opens in the mail app.")]
        public string SupportEmail = "kayustudio.support@gmail.com";

        /// <summary>mailto: link to <see cref="SupportEmail"/> with the game and version as the subject.</summary>
        public string SupportMailto(string version) =>
            "mailto:" + SupportEmail + "?subject=" + System.Uri.EscapeDataString("Blocktrotter " + version);

        [Header("Background decor")]
        [Tooltip("Drifting decor pieces (petals, snow, ...) on screen at once.")]
        [Min(0)] public int DecorParticleCount = 18;
    }
}
