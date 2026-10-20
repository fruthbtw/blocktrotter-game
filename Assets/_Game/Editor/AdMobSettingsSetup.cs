using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Fills Google Mobile Ads settings (Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset)
    /// with Google's sample TEST app IDs and the iOS tracking-prompt text, but only
    /// where a field is still empty, so real IDs entered later are never overwritten.
    /// The plugin's settings class is internal, hence the reflection; if a plugin
    /// update renames it, this logs a warning and changes nothing.
    /// Test app IDs from googleads-mobile-unity samples/HelloWorld (checked 2026-10-20).
    /// </summary>
    public static class AdMobSettingsSetup
    {
        private const string TestIosAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string TrackingDescription =
            "This lets us show you ads that are more relevant to you. The game plays the same either way.";

        [MenuItem("Blocktrotter/Configure AdMob Settings (test IDs)")]
        public static void Configure()
        {
            Type settingsType = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            MethodInfo load = settingsType?.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (load == null)
            {
                Debug.LogWarning("[AdMobSettingsSetup] Google Mobile Ads settings type not found — set App IDs via Assets > Google Mobile Ads > Settings.");
                return;
            }

            var settings = (ScriptableObject)load.Invoke(null, null);
            var so = new SerializedObject(settings);
            SetIfEmpty(so, "adMobIOSAppId", TestIosAppId);
            SetIfEmpty(so, "adMobAndroidAppId", TestAndroidAppId);
            SetIfEmpty(so, "userTrackingUsageDescription", TrackingDescription);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[AdMobSettingsSetup] AdMob settings checked (empty fields filled with test values).");
        }

        private static void SetIfEmpty(SerializedObject so, string field, string value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) Debug.LogWarning($"[AdMobSettingsSetup] Field '{field}' not found in plugin settings.");
            else if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = value;
        }
    }
}
