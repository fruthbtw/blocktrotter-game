using UnityEngine;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// AdMob ad unit IDs. Defaults are Google's official TEST units
    /// (https://developers.google.com/admob/unity/test-ads, checked 2026-10-20):
    /// they always fill and clicking them never risks the account. Replace with the
    /// real IDs from the AdMob console before release. App IDs are set separately in
    /// Assets &gt; Google Mobile Ads &gt; Settings.
    /// </summary>
    /// <example><code>string unit = config.InterstitialUnitId;</code></example>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Blocktrotter/Ad Config")]
    public sealed class AdConfig : ScriptableObject
    {
        [Tooltip("Off = no ads at all (e.g. for screenshots or a future 'remove ads' purchase).")]
        public bool AdsEnabled = true;

        [Header("iOS")]
        public string IosBannerId = "ca-app-pub-3940256099942544/2934735716";
        public string IosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
        public string IosRewardedId = "ca-app-pub-3940256099942544/1712485313";

        [Header("Android")]
        public string AndroidBannerId = "ca-app-pub-3940256099942544/6300978111";
        public string AndroidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public string AndroidRewardedId = "ca-app-pub-3940256099942544/5224354917";

        /// <summary>True while any unit still uses a Google test ID (shown as a warning at startup).</summary>
        public bool UsesTestIds => BannerUnitId.StartsWith("ca-app-pub-3940256099942544");

        /// <summary>Banner unit for the current platform.</summary>
        public string BannerUnitId => Application.platform == RuntimePlatform.Android ? AndroidBannerId : IosBannerId;

        /// <summary>Interstitial unit for the current platform.</summary>
        public string InterstitialUnitId => Application.platform == RuntimePlatform.Android ? AndroidInterstitialId : IosInterstitialId;

        /// <summary>Rewarded unit for the current platform.</summary>
        public string RewardedUnitId => Application.platform == RuntimePlatform.Android ? AndroidRewardedId : IosRewardedId;
    }
}
