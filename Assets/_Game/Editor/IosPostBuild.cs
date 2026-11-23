#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Edits the exported Xcode project's Info.plist after an iOS build.
    /// ITSAppUsesNonExemptEncryption = false: the game only uses the HTTPS built into iOS
    /// (ads, links), which is exempt, so App Store Connect stops asking the export-compliance
    /// question on every upload.
    /// </summary>
    public static class IosPostBuild
    {
        /// <summary>Called by Unity after the iOS player (Xcode project) is written.</summary>
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
