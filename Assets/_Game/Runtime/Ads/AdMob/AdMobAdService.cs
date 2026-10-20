using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;   // the package compiles for iOS (and the editor) only
#endif
using UnityEngine;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// Google AdMob implementation of <see cref="IAdService"/> (Google Mobile Ads 11.6.0).
    /// Start-up order: UMP consent (GDPR/UK) → Apple ATT prompt (iOS) → SDK init →
    /// preload interstitial and rewarded. Ads are only requested when UMP says
    /// <c>CanRequestAds()</c>. API checked against developers.google.com/admob/unity
    /// (rewarded, privacy) on 2026-10-20.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private const float AttTimeoutSeconds = 60f;
        private const float RetryDelaySeconds = 30f;

        private readonly AdConfig _config;
        private readonly MonoBehaviour _runner;
        private bool _initialized, _wantBanner;
        private BannerView _banner;
        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;

        /// <summary>Registers this service as the device ad network before the first scene loads.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => AdServiceFactory.DeviceFactory = cfg => new AdMobAdService(cfg);

        private AdMobAdService(AdConfig config)
        {
            _config = config;
            var go = new GameObject("AdMobRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<CoroutineRunner>();
            MobileAds.RaiseAdEventsOnUnityMainThread = true;   // ad callbacks on the main thread
        }

        /// <inheritdoc/>
        public bool IsRewardedReady => _rewarded != null && _rewarded.CanShowAd();

        /// <inheritdoc/>
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <inheritdoc/>
        public void Initialize(Action onReady)
        {
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null) Debug.LogWarning($"[Ads] Consent update failed: {updateError.Message}");
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Debug.LogWarning($"[Ads] Consent form failed: {formError.Message}");
                    _runner.StartCoroutine(AfterConsent(onReady));
                });
            });
        }

        private IEnumerator AfterConsent(Action onReady)
        {
            yield return RequestAttIfNeeded();
            if (!ConsentInformation.CanRequestAds())
            {
                Debug.Log("[Ads] Consent does not allow ad requests; ads stay off.");
                onReady?.Invoke();
                yield break;
            }
            MobileAds.Initialize(_ =>
            {
                _initialized = true;
                LoadInterstitial();
                LoadRewarded();
                if (_wantBanner) ShowBanner();
                onReady?.Invoke();
            });
        }

        /// <summary>Shows Apple's tracking prompt once; the SDK serves non-personalized ads if declined.</summary>
        private static IEnumerator RequestAttIfNeeded()
        {
#if !UNITY_IOS
            yield break;
#else
            if (Application.platform != RuntimePlatform.IPhonePlayer) yield break;
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
                ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED) yield break;

            ATTrackingStatusBinding.RequestAuthorizationTracking();
            // The binding has no callback: wait until the player answers (or give up).
            float waited = 0f;
            while (waited < AttTimeoutSeconds && ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                   ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
#endif
        }

        /// <inheritdoc/>
        public void ShowBanner()
        {
            _wantBanner = true;
            if (!_initialized) return;
            if (_banner == null)
            {
                _banner = new BannerView(_config.BannerUnitId, AdSize.Banner, AdPosition.Bottom);
                _banner.OnBannerAdLoadFailed += e => Debug.LogWarning($"[Ads] Banner failed: {e.GetMessage()}");
                _banner.LoadAd(new AdRequest());
            }
            _banner.Show();
        }

        /// <inheritdoc/>
        public void HideBanner()
        {
            _wantBanner = false;
            _banner?.Hide();
        }

        /// <inheritdoc/>
        public void ShowInterstitial(Action onClosed)
        {
            if (_interstitial == null || !_interstitial.CanShowAd())
            {
                onClosed?.Invoke();
                return;
            }
            InterstitialAd ad = _interstitial;
            _interstitial = null;
            ad.OnAdFullScreenContentClosed += () => { ad.Destroy(); LoadInterstitial(); onClosed?.Invoke(); };
            ad.OnAdFullScreenContentFailed += e => { ad.Destroy(); LoadInterstitial(); onClosed?.Invoke(); };
            ad.Show();
        }

        /// <inheritdoc/>
        public void ShowRewarded(Action onRewarded, Action onDone)
        {
            if (!IsRewardedReady)
            {
                onDone?.Invoke();
                return;
            }
            RewardedAd ad = _rewarded;
            _rewarded = null;
            ad.OnAdFullScreenContentClosed += () => { ad.Destroy(); LoadRewarded(); onDone?.Invoke(); };
            ad.OnAdFullScreenContentFailed += e => { ad.Destroy(); LoadRewarded(); onDone?.Invoke(); };
            ad.Show(_ => onRewarded?.Invoke());
        }

        /// <inheritdoc/>
        public void ShowPrivacyOptions(Action onClosed)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null) Debug.LogWarning($"[Ads] Privacy options form failed: {error.Message}");
                onClosed?.Invoke();
            });
        }

        private void LoadInterstitial()
        {
            InterstitialAd.Load(_config.InterstitialUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[Ads] Interstitial load failed: {error?.GetMessage()}");
                    _runner.StartCoroutine(RetryLater(LoadInterstitial));
                    return;
                }
                _interstitial = ad;
            });
        }

        private void LoadRewarded()
        {
            RewardedAd.Load(_config.RewardedUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[Ads] Rewarded load failed: {error?.GetMessage()}");
                    _runner.StartCoroutine(RetryLater(LoadRewarded));
                    return;
                }
                _rewarded = ad;
            });
        }

        private static IEnumerator RetryLater(Action load)
        {
            yield return new WaitForSecondsRealtime(RetryDelaySeconds);
            load();
        }

        /// <summary>Hosts coroutines for this plain C# service.</summary>
        private sealed class CoroutineRunner : MonoBehaviour { }
    }
}
