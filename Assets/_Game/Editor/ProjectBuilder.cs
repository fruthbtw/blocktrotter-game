using System.IO;
using Blocktrotter.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Creates the data assets and the Game scene, and builds a Windows player for
    /// run-and-observe screenshots. Menu items under "Blocktrotter/", or batch:
    /// <c>-executeMethod Blocktrotter.EditorTools.ProjectBuilder.BuildGameSceneBatch</c>
    /// <c>-executeMethod Blocktrotter.EditorTools.ProjectBuilder.BuildWindowsBatch</c>
    /// </summary>
    public static class ProjectBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Game.unity";
        public const string ConfigPath = "Assets/_Game/Data/GameConfig.asset";
        public const string AdConfigPath = "Assets/_Game/Data/AdConfig.asset";
        public const string ThemesFolder = "Assets/_Game/Data/Themes";
        public const string EnglishPath = "Assets/_Game/Data/Localization/en.txt";
        public const string TurkishPath = "Assets/_Game/Data/Localization/tr.txt";
        public const string WindowsBuildPath = "Builds/Win64/Blocktrotter.exe";

        [MenuItem("Blocktrotter/Build Game Scene")]
        public static void BuildGameScene()
        {
            Directory.CreateDirectory("Assets/_Game/Scenes");
            Directory.CreateDirectory("Assets/_Game/Data/Themes");

            // NewScene(Single) unloads unused assets, so load references after it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadOrCreate<GameConfig>(ConfigPath);
            var adConfig = LoadOrCreate<Ads.AdConfig>(AdConfigPath);
            ThemeDefinition[] themes = LoadOrCreateThemes();
            var levels = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelGeneratorMenu.DatabasePath);
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(EnglishPath);
            var tr = AssetDatabase.LoadAssetAtPath<TextAsset>(TurkishPath);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = themes[0].BackgroundBottom;

            var app = new GameObject("App").AddComponent<AppShell.App>();
            var so = new SerializedObject(app);
            so.FindProperty("_levels").objectReferenceValue = levels;
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_adConfig").objectReferenceValue = adConfig;
            SerializedProperty themeList = so.FindProperty("_themes");
            themeList.arraySize = themes.Length;
            for (int i = 0; i < themes.Length; i++)
                themeList.GetArrayElementAtIndex(i).objectReferenceValue = themes[i];
            so.FindProperty("_englishStrings").objectReferenceValue = en;
            so.FindProperty("_turkishStrings").objectReferenceValue = tr;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AdMobSettingsSetup.Configure();
            PlayerSettings.productName = "Blocktrotter";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log($"[ProjectBuilder] Scene saved to {ScenePath} (levels: {(levels != null ? levels.Count : 0)})");
        }

        [MenuItem("Blocktrotter/Build Windows Player (screenshots)")]
        public static bool BuildWindows()
        {
            // Portrait window for desktop testing only; mobile ignores these.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log($"[ProjectBuilder] Windows build: {report.summary.result}, {report.summary.totalErrors} errors");
            return report.summary.result == BuildResult.Succeeded;
        }

        public static void BuildGameSceneBatch()
        {
            BuildGameScene();
            EditorApplication.Exit(0);
        }

        public static void BuildWindowsBatch()
        {
            EditorApplication.Exit(BuildWindows() ? 0 : 1);
        }

        /// <summary>
        /// The five worlds, in play order. Palettes are written only when an asset is
        /// first created; after that the asset is the source of truth (tweak it in the Inspector).
        /// </summary>
        private static ThemeDefinition[] LoadOrCreateThemes()
        {
            return new[]
            {
                Decor(Theme("Garden", "theme.garden", "#F9D8E3", "#FDF1F4", "#3E4C3A", "#566B50", "#2F3A2C", "#4E6B4A", "#ECDCB0", "#2A3327", "#D7263D", "#F2C14E"), DecorStyle.Petals, "#F49AB8", "#FFFFFF"),
                Decor(Theme("Desert", "theme.desert", "#F6C978", "#FBE7C0", "#7A4B2A", "#9C6436", "#5C3A21", "#B9783F", "#F4E4C1", "#6B4423", "#C8102E", "#FFD54F"), DecorStyle.Dunes, "#E0A458", "#FFF1B8"),
                Decor(Theme("Island", "theme.island", "#7FD8F0", "#E0F7FA", "#0E6E7E", "#138A9C", "#0A4F5A", "#2A9DA8", "#FFF3D6", "#0A4F5A", "#FF5A4E", "#FFE066"), DecorStyle.Waves, "#4FC3D9", "#FFE066"),
                Decor(Theme("Arctic", "theme.arctic", "#BFE3F5", "#F2FAFF", "#2F5A7A", "#3F739A", "#1F3F57", "#5C8DB3", "#F4FBFF", "#1F3F57", "#E63946", "#FFD166"), DecorStyle.Snow, "#8FBFDF", "#FFFFFF"),
                Decor(Theme("Neon", "theme.neon", "#1A1035", "#3B1E6B", "#24124A", "#3A1F73", "#0E0820", "#1B1236", "#2EC4F1", "#070414", "#FF2E88", "#F9F871"), DecorStyle.Skyline, "#1E0F3D", "#F9F871"),
            };
        }

        private static ThemeDefinition Theme(string file, string nameKey, string bgTop, string bgBottom, string panel,
            string button, string frame, string board, string block, string shadow, string target, string exit)
        {
            string path = $"{ThemesFolder}/Theme_{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
            if (existing != null) return existing;

            var t = ScriptableObject.CreateInstance<ThemeDefinition>();
            t.NameKey = nameKey;
            t.BackgroundTop = Hex(bgTop);
            t.BackgroundBottom = Hex(bgBottom);
            t.PanelColor = Hex(panel);
            t.ButtonColor = Hex(button);
            t.BoardFrameColor = Hex(frame);
            t.BoardColor = Hex(board);
            t.BlockColor = Hex(block);
            t.BlockShadowColor = Hex(shadow);
            t.TargetColor = Hex(target);
            t.ExitColor = Hex(exit);
            t.TextColor = Color.white;
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        /// <summary>Fills decor fields only if the asset has none yet, so Inspector tweaks survive rebuilds.</summary>
        private static ThemeDefinition Decor(ThemeDefinition t, DecorStyle style, string color, string accent)
        {
            if (t.Decor != DecorStyle.None) return t;
            t.Decor = style;
            t.DecorColor = Hex(color);
            t.DecorAccentColor = Hex(accent);
            EditorUtility.SetDirty(t);
            return t;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
