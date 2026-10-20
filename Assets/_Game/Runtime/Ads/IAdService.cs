using System;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// Everything the game needs from an ad network. Callbacks always arrive on the
    /// main thread. Implementations: <see cref="EditorAdService"/> (editor/desktop
    /// stand-in) and AdMobAdService (devices, when the Google Mobile Ads package is installed).
    /// </summary>
    /// <example>
    /// <code>
    /// ads.Initialize(() =&gt; ads.ShowBanner());
    /// ads.ShowRewarded(onRewarded: () =&gt; hints.Add(1), onDone: RefreshHud);
    /// </code>
    /// </example>
    public interface IAdService
    {
        /// <summary>Runs consent (UMP, ATT) and SDK start-up, then calls <paramref name="onReady"/>.</summary>
        void Initialize(Action onReady);

        /// <summary>Shows the bottom banner (loads it if needed).</summary>
        void ShowBanner();

        /// <summary>Hides the banner.</summary>
        void HideBanner();

        /// <summary>Shows an interstitial if one is loaded; <paramref name="onClosed"/> always runs (immediately if none).</summary>
        void ShowInterstitial(Action onClosed);

        /// <summary>True if a rewarded ad is loaded and can be shown.</summary>
        bool IsRewardedReady { get; }

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onRewarded"/> runs only if the player earned the reward;
        /// <paramref name="onDone"/> always runs when the ad flow ends.
        /// </summary>
        void ShowRewarded(Action onRewarded, Action onDone);

        /// <summary>True when the privacy regulations require a "Privacy options" entry point (UMP).</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary>Re-opens the consent form so the player can change their choice.</summary>
        void ShowPrivacyOptions(Action onClosed);
    }
}
