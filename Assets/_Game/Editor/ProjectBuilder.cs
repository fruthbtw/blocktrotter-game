using System;
using System.IO;
using Blocktrotter.Data;
using Blocktrotter.Localization;
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
        public const string SoundConfigPath = "Assets/_Game/Data/SoundConfig.asset";
        public const string AudioFolder = "Assets/_Game/Audio/";
        public const string AppIconPath = "Assets/_Game/Art/AppIcon.png";
        public const string ThemesFolder = "Assets/_Game/Data/Themes";
        public const string LocalizationFolder = "Assets/_Game/Data/Localization/";

        /// <summary>iOS bundle ID and Android package name, registered with AdMob by Kayu Studio.</summary>
        public const string BundleId = "com.kayustudio.blocktrotter";
        public const string WindowsBuildPath = "Builds/Win64/Blocktrotter.exe";
        public const string IosBuildPath = "Builds/iOS";

        [MenuItem("Blocktrotter/Build Game Scene")]
        public static void BuildGameScene()
        {
            Directory.CreateDirectory("Assets/_Game/Scenes");
            Directory.CreateDirectory("Assets/_Game/Data/Themes");

            // NewScene(Single) unloads unused assets, so load references after it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadOrCreate<GameConfig>(ConfigPath);
            var adConfig = LoadOrCreate<Ads.AdConfig>(AdConfigPath);
            var sounds = BuildSoundConfig();
            ThemeDefinition[] themes = LoadOrCreateThemes();
            var levels = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelGeneratorMenu.DatabasePath);
            TextAsset[] stringTables = Array.ConvertAll(Localizer.Supported,
                code => AssetDatabase.LoadAssetAtPath<TextAsset>(LocalizationFolder + code + ".txt"));

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
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
            so.FindProperty("_sounds").objectReferenceValue = sounds;
            SerializedProperty themeList = so.FindProperty("_themes");
            themeList.arraySize = themes.Length;
            for (int i = 0; i < themes.Length; i++)
                themeList.GetArrayElementAtIndex(i).objectReferenceValue = themes[i];
            SerializedProperty tableList = so.FindProperty("_stringTables");
            tableList.arraySize = stringTables.Length;
            for (int i = 0; i < stringTables.Length; i++)
                tableList.GetArrayElementAtIndex(i).objectReferenceValue = stringTables[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AdMobSettingsSetup.Configure();
            SetAppIcon();
            PlayerSettings.productName = "Blocktrotter";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, BundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.iOS.buildNumber = "1";
            // iPhone only: no iPad screenshots or iPad review; iPads still run it in iPhone mode.
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
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

        /// <summary>Exports the release Xcode project to <see cref="IosBuildPath"/> (open it on a Mac to archive and upload).</summary>
        [MenuItem("Blocktrotter/Build iOS Xcode Project")]
        public static bool BuildIos()
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = IosBuildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });
            Debug.Log($"[ProjectBuilder] iOS build: {report.summary.result}, {report.summary.totalErrors} errors");
            return report.summary.result == BuildResult.Succeeded;
        }

        public static void BuildIosBatch()
        {
            EditorApplication.Exit(BuildIos() ? 0 : 1);
        }

        /// <summary>
        /// The five worlds, in play order. Palettes are written only when an asset is
        /// first created; after that the asset is the source of truth (tweak it in the Inspector).
        /// </summary>
        private static ThemeDefinition[] LoadOrCreateThemes()
        {
            return new[]
            {
                Art(Skin(Decor(Theme("Garden", "theme.garden", "#F9D8E3", "#FDF1F4", "#3E4C3A", "#566B50", "#2F3A2C", "#4E6B4A", "#ECDCB0", "#2A3327", "#D7263D", "#F2C14E"), DecorStyle.Petals, "#F49AB8", "#FFFFFF"), BlockStyle.Bamboo, "#A7CF6B", "#4F7A2E", "#E4F5B5", "#C8102E", "#E8B93E", "#33402F", "#222B20"), "garden", true, 1f),
                Art(Skin(Decor(Theme("Desert", "theme.desert", "#F6C978", "#FBE7C0", "#7A4B2A", "#9C6436", "#5C3A21", "#B9783F", "#F4E4C1", "#6B4423", "#C8102E", "#FFD54F"), DecorStyle.Dunes, "#E0A458", "#FFF1B8"), BlockStyle.Sandstone, "#E8C58C", "#A9783F", "#FFF0CF", "#C8102E", "#F2C14E", "#8A5A2E", "#5C3A21"), "desert", false, 2f),
                Art(Skin(Decor(Theme("Island", "theme.island", "#7FD8F0", "#E0F7FA", "#0E6E7E", "#138A9C", "#0A4F5A", "#2A9DA8", "#FFF3D6", "#0A4F5A", "#FF5A4E", "#FFE066"), DecorStyle.Waves, "#4FC3D9", "#FFE066"), BlockStyle.SeaGlass, "#6FD6C9", "#1C8A86", "#E6FFFB", "#E63946", "#FFFFFF", "#0F5E6B", "#0A3F48"), "island", false, 1.5f),
                Art(Skin(Decor(Theme("Arctic", "theme.arctic", "#BFE3F5", "#F2FAFF", "#2F5A7A", "#3F739A", "#1F3F57", "#5C8DB3", "#F4FBFF", "#1F3F57", "#E63946", "#FFD166"), DecorStyle.Snow, "#8FBFDF", "#FFFFFF"), BlockStyle.Ice, "#D8F0FB", "#6FA8CF", "#FFFFFF", "#D62839", "#FFFFFF", null, null), "arctic", false, 1.5f),
                Art(Skin(Decor(Theme("Neon", "theme.neon", "#1A1035", "#3B1E6B", "#24124A", "#3A1F73", "#0E0820", "#1B1236", "#2EC4F1", "#070414", "#FF2E88", "#F9F871"), DecorStyle.Skyline, "#1E0F3D", "#F9F871"), BlockStyle.Neon, "#160C2E", "#2EC4F1", "#FFFFFF", "#FF2E88", "#FF5FA8", null, null), "neon", false, 1f),
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

        /// <summary>
        /// Hooks up generated art from Assets/_Game/Art/Themes/&lt;world&gt;-block.png and
        /// &lt;world&gt;-target.png when present and the theme has no texture yet. The texture
        /// layout (turning with the block, repeat size) is owned here and set on every build.
        /// </summary>
        private static ThemeDefinition Art(ThemeDefinition t, string world, bool followsBlock, float cellsPerRepeat)
        {
            t.TextureFollowsBlock = followsBlock;
            t.TextureCellsPerRepeat = cellsPerRepeat;
            EditorUtility.SetDirty(t);
            var block = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ThemeArtImporter.Folder}{world}-block.png");
            var target = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ThemeArtImporter.Folder}{world}-target.png");
            if (t.BlockTexture == null && block != null) { t.BlockTexture = block; EditorUtility.SetDirty(t); }
            if (t.TargetTexture == null && target != null) { t.TargetTexture = target; EditorUtility.SetDirty(t); }
            return t;
        }

        /// <summary>
        /// Fills block-skin fields only while the theme still has the plain style, so Inspector
        /// tweaks survive rebuilds. Board colours are optional (null keeps the current ones);
        /// they are darkened where the new blocks would otherwise blend into the board.
        /// </summary>
        private static ThemeDefinition Skin(ThemeDefinition t, BlockStyle style, string block, string detail, string highlight,
            string target, string targetDetail, string board, string frame)
        {
            if (t.BlockStyle != BlockStyle.Plain) return t;
            t.BlockStyle = style;
            t.BlockColor = Hex(block);
            t.BlockDetailColor = Hex(detail);
            t.BlockHighlightColor = Hex(highlight);
            t.TargetColor = Hex(target);
            t.TargetDetailColor = Hex(targetDetail);
            if (board != null) t.BoardColor = Hex(board);
            if (frame != null) t.BoardFrameColor = Hex(frame);
            EditorUtility.SetDirty(t);
            return t;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        /// <summary>
        /// Maps each sound to its clip in Assets/_Game/Audio (made by tools/synth_sounds.py) and a
        /// starting volume. Entries already in the asset keep their volume and jitter.
        /// </summary>
        private static Audio.SoundConfig BuildSoundConfig()
        {
            var config = LoadOrCreate<Audio.SoundConfig>(SoundConfigPath);
            var defaults = new (Audio.Sfx Sound, string File, float Volume, float Jitter)[]
            {
                (Audio.Sfx.BlockSnap, "block-snap", 0.55f, 0.06f),
                (Audio.Sfx.TargetExit, "target-exit", 0.8f, 0f),
                (Audio.Sfx.Star1, "star-1", 0.7f, 0f),
                (Audio.Sfx.Star2, "star-2", 0.7f, 0f),
                (Audio.Sfx.Star3, "star-3", 0.7f, 0f),
                (Audio.Sfx.LevelSolved, "level-solved", 0.75f, 0f),
                (Audio.Sfx.Button, "button", 0.45f, 0.05f),
                (Audio.Sfx.Hint, "hint", 0.7f, 0f),
                (Audio.Sfx.Undo, "undo", 0.6f, 0.03f),
                (Audio.Sfx.WorldUnlocked, "world-unlocked", 0.8f, 0f),
            };
            var entries = new Audio.SoundConfig.Entry[defaults.Length];
            for (int i = 0; i < defaults.Length; i++)
            {
                Audio.SoundConfig.Entry existing = config.Get(defaults[i].Sound);
                bool keep = existing.Clip != null;
                entries[i] = new Audio.SoundConfig.Entry
                {
                    Sound = defaults[i].Sound,
                    Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}{defaults[i].File}.wav"),
                    Volume = keep ? existing.Volume : defaults[i].Volume,
                    PitchJitter = keep ? existing.PitchJitter : defaults[i].Jitter
                };
            }
            config.SetEntries(entries);
            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>
        /// Uses Assets/_Game/Art/AppIcon.png (1024 px, no alpha; drawn by tools/make_icon.py) as the
        /// default icon, which Unity scales for every platform and size.
        /// </summary>
        private static void SetAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                Debug.LogWarning($"[ProjectBuilder] {AppIconPath} not found; app icon unchanged.");
                return;
            }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

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
