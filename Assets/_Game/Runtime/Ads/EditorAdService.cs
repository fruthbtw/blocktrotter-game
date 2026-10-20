using System;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Ads
{
    /// <summary>
    /// Stand-in used in the editor and desktop builds: draws a grey "TEST BANNER"
    /// bar and full-screen "TEST AD" panels so ad placement and flow can be checked
    /// without the SDK. Never shown on devices that have AdMob installed.
    /// </summary>
    public sealed class EditorAdService : IAdService
    {
        private static readonly Vector2 BannerSize = new Vector2(960, 150);   // ~320x50pt at 3x

        private readonly RectTransform _layer;
        private RectTransform _banner;

        /// <summary>Draws into <paramref name="layer"/> (a full-screen canvas child above the screens).</summary>
        public EditorAdService(RectTransform layer) => _layer = layer;

        /// <inheritdoc/>
        public bool IsRewardedReady => true;

        /// <inheritdoc/>
        public bool PrivacyOptionsRequired => true;   // so the settings button can be seen in the editor

        /// <inheritdoc/>
        public void Initialize(Action onReady) => onReady?.Invoke();

        /// <inheritdoc/>
        public void ShowBanner()
        {
            if (_banner != null) { _banner.gameObject.SetActive(true); return; }
            _banner = UiFactory.Rect("TestBanner", _layer, Vector2.zero, BannerSize);
            _banner.anchorMin = _banner.anchorMax = new Vector2(0.5f, 0f);
            _banner.pivot = new Vector2(0.5f, 0f);
            UiFactory.Image(_banner, new Color(0.2f, 0.2f, 0.2f, 0.9f)).sprite = null;
            UiFactory.Label(_banner, "TEST BANNER (editor)", 40, Color.white);
        }

        /// <inheritdoc/>
        public void HideBanner()
        {
            if (_banner != null) _banner.gameObject.SetActive(false);
        }

        /// <inheritdoc/>
        public void ShowInterstitial(Action onClosed) => FullScreen("TEST INTERSTITIAL (editor)", "Close", () => onClosed?.Invoke());

        /// <inheritdoc/>
        public void ShowRewarded(Action onRewarded, Action onDone) =>
            FullScreen("TEST REWARDED AD (editor)\nreward granted on close", "Close", () =>
            {
                onRewarded?.Invoke();
                onDone?.Invoke();
            });

        /// <inheritdoc/>
        public void ShowPrivacyOptions(Action onClosed) =>
            FullScreen("TEST PRIVACY OPTIONS (editor)\nThe Google consent form opens here on devices.", "Close", () => onClosed?.Invoke());

        private void FullScreen(string text, string closeText, Action onClose)
        {
            RectTransform overlay = UiFactory.Stretch("TestAd", _layer);
            UiFactory.Dim(overlay, new Color(0.1f, 0.1f, 0.1f, 0.97f));
            RectTransform label = UiFactory.Rect("Text", overlay, new Vector2(0, 120), new Vector2(900, 300));
            UiFactory.Label(label, text, 48, Color.white);
            Button close = UiFactory.Button("Close", overlay, new Vector2(0, -250), new Vector2(400, 130), closeText, 48,
                new Color(0.35f, 0.35f, 0.35f), Color.white, () => { });
            close.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                onClose();
            });
        }
    }
}
