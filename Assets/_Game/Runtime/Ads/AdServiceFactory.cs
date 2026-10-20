using System;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// Hook for the real ad network. The optional Blocktrotter.Ads.AdMob assembly
    /// (compiled only when the Google Mobile Ads package is installed) sets
    /// <see cref="DeviceFactory"/> before the first scene loads; otherwise the
    /// game falls back to <see cref="EditorAdService"/>.
    /// </summary>
    public static class AdServiceFactory
    {
        /// <summary>Creates the device ad service from the config, or null if no network is installed.</summary>
        public static Func<AdConfig, IAdService> DeviceFactory;
    }

    /// <summary>Does nothing; used when ads are disabled.</summary>
    public sealed class NoAdService : IAdService
    {
        /// <inheritdoc/>
        public bool IsRewardedReady => false;
        /// <inheritdoc/>
        public bool PrivacyOptionsRequired => false;
        /// <inheritdoc/>
        public void Initialize(Action onReady) => onReady?.Invoke();
        /// <inheritdoc/>
        public void ShowBanner() { }
        /// <inheritdoc/>
        public void HideBanner() { }
        /// <inheritdoc/>
        public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
        /// <inheritdoc/>
        public void ShowRewarded(Action onRewarded, Action onDone) => onDone?.Invoke();
        /// <inheritdoc/>
        public void ShowPrivacyOptions(Action onClosed) => onClosed?.Invoke();
    }
}
