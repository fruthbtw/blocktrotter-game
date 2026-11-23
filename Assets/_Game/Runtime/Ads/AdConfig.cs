using UnityEngine;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// AdMob ad unit IDs: the real Kayu Studio units for iOS and Android. Development
    /// builds use Google's official TEST units instead
    /// (https://developers.google.com/admob/unity/test-ads, checked 2026-10-20), so tapping
    /// ads while testing never risks the account. App IDs are set separately in
    /// Assets &gt; Google Mobile Ads &gt; Settings.
    /// </summary>
    /// <example><code>string unit = config.InterstitialUnitId;</code></example>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Blocktrotter/Ad Config")]
    public sealed class AdConfig : ScriptableObject
    {
        [Tooltip("Off = no ads at all (e.g. for screenshots or a future 'remove ads' purchase).")]
        public bool AdsEnabled = true;

        [Header("iOS (Kayu Studio AdMob, com.kayustudio.blocktrotter)")]
        public string IosBannerId = "ca-app-pub-8655761428673093/5255350500";
        public string IosInterstitialId = "ca-app-pub-8655761428673093/2549296113";
        public string IosRewardedId = "ca-app-pub-8655761428673093/3389615390";

        [Header("Android (Kayu Studio AdMob, com.kayustudio.blocktrotter)")]
        public string AndroidBannerId = "ca-app-pub-8655761428673093/2644791196";
        public string AndroidInterstitialId = "ca-app-pub-8655761428673093/9110835032";
        public string AndroidRewardedId = "ca-app-pub-8655761428673093/2515517427";

        [Tooltip("Development builds show Google's test ads, so tapping them while testing never risks the AdMob account.")]
        public bool TestAdsInDevelopmentBuilds = true;

        private const string TestIosBanner = "ca-app-pub-3940256099942544/2934735716";
        private const string TestIosInterstitial = "ca-app-pub-3940256099942544/4411468910";
        private const string TestIosRewarded = "ca-app-pub-3940256099942544/1712485313";
        private const string TestAndroidBanner = "ca-app-pub-3940256099942544/6300978111";
        private const string TestAndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        private const string TestAndroidRewarded = "ca-app-pub-3940256099942544/5224354917";

        private static bool IsAndroid => Application.platform == RuntimePlatform.Android;

        private bool ForceTestAds => TestAdsInDevelopmentBuilds && Debug.isDebugBuild;

        /// <summary>True while the current platform shows a Google test ad unit (shown as a warning at startup).</summary>
        public bool UsesTestIds => BannerUnitId.StartsWith("ca-app-pub-3940256099942544");

        /// <summary>Banner unit for the current platform (a test unit in development builds).</summary>
        public string BannerUnitId => ForceTestAds ? (IsAndroid ? TestAndroidBanner : TestIosBanner)
            : IsAndroid ? AndroidBannerId : IosBannerId;

        /// <summary>Interstitial unit for the current platform (a test unit in development builds).</summary>
        public string InterstitialUnitId => ForceTestAds ? (IsAndroid ? TestAndroidInterstitial : TestIosInterstitial)
            : IsAndroid ? AndroidInterstitialId : IosInterstitialId;

        /// <summary>Rewarded unit for the current platform (a test unit in development builds).</summary>
        public string RewardedUnitId => ForceTestAds ? (IsAndroid ? TestAndroidRewarded : TestIosRewarded)
            : IsAndroid ? AndroidRewardedId : IosRewardedId;
    }
}
