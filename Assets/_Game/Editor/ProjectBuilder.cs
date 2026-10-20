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
        public const string GardenThemePath = "Assets/_Game/Data/Themes/Theme_Garden.asset";
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
            var theme = LoadOrCreate<ThemeDefinition>(GardenThemePath);
            var levels = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelGeneratorMenu.DatabasePath);
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(EnglishPath);
            var tr = AssetDatabase.LoadAssetAtPath<TextAsset>(TurkishPath);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.BackgroundBottom;

            var screen = new GameObject("GameScreen").AddComponent<GameScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_levels").objectReferenceValue = levels;
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_theme").objectReferenceValue = theme;
            so.FindProperty("_englishStrings").objectReferenceValue = en;
            so.FindProperty("_turkishStrings").objectReferenceValue = tr;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

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
