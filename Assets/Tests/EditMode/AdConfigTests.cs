using Blocktrotter.Ads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blocktrotter.Tests
{
    public class AdConfigTests
    {
        private const string AssetPath = "Assets/_Game/Data/AdConfig.asset";
        private const string GoogleTestPublisher = "ca-app-pub-3940256099942544";
        private const string KayuPublisher = "ca-app-pub-8655761428673093";

        private AdConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<AdConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void DevelopmentBuild_UsesGoogleTestUnits()
        {
            // The editor counts as a development build (Debug.isDebugBuild is true).
            _config.TestAdsInDevelopmentBuilds = true;

            StringAssert.StartsWith(GoogleTestPublisher, _config.BannerUnitId);
            StringAssert.StartsWith(GoogleTestPublisher, _config.InterstitialUnitId);
            StringAssert.StartsWith(GoogleTestPublisher, _config.RewardedUnitId);
            Assert.IsTrue(_config.UsesTestIds);
        }

        [Test]
        public void TestAdsOff_UsesTheConfiguredIosUnits()
        {
            _config.TestAdsInDevelopmentBuilds = false;

            Assert.AreEqual(_config.IosBannerId, _config.BannerUnitId);
            Assert.AreEqual(_config.IosInterstitialId, _config.InterstitialUnitId);
            Assert.AreEqual(_config.IosRewardedId, _config.RewardedUnitId);
        }

        [Test]
        public void ShippedAsset_HasTheRealUnitsAndTestAdsInDevelopment()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AdConfig>(AssetPath);

            StringAssert.StartsWith(KayuPublisher, asset.IosBannerId);
            StringAssert.StartsWith(KayuPublisher, asset.IosInterstitialId);
            StringAssert.StartsWith(KayuPublisher, asset.IosRewardedId);
            StringAssert.StartsWith(KayuPublisher, asset.AndroidBannerId);
            StringAssert.StartsWith(KayuPublisher, asset.AndroidInterstitialId);
            StringAssert.StartsWith(KayuPublisher, asset.AndroidRewardedId);
            Assert.IsTrue(asset.TestAdsInDevelopmentBuilds);
        }
    }
}
