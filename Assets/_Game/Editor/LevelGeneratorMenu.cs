using System.Diagnostics;
using System.Linq;
using Blocktrotter.Core;
using Blocktrotter.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Menu "Blocktrotter/Generate Levels": regenerates the level database from the
    /// settings asset. Also callable from batch mode:
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod Blocktrotter.EditorTools.LevelGeneratorMenu.GenerateBatch</c>
    /// </summary>
    public static class LevelGeneratorMenu
    {
        public const string SettingsPath = "Assets/_Game/Editor/LevelGenerationSettings.asset";
        public const string DatabasePath = "Assets/_Game/Data/LevelDatabase.asset";

        [MenuItem("Blocktrotter/Generate Levels")]
        public static void GenerateFromMenu()
        {
            try
            {
                int lastShown = -1;   // redraw only when a level is added, not on every board
                Generate((done, total, attempts) =>
                {
                    if (done == lastShown) return;
                    lastShown = done;
                    EditorUtility.DisplayProgressBar(
                        "Generating levels", $"{done}/{total} levels · {attempts} boards tried", (float)done / total);
                });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Batch-mode entry point; exits with code 1 if the set is incomplete.</summary>
        public static void GenerateBatch()
        {
            // Log every few thousand boards so a long batch run can be followed in the log file.
            bool complete = Generate((done, total, attempts) =>
            {
                if (attempts % 2000 == 0) Debug.Log($"[LevelGenerator] progress {done}/{total} levels, {attempts} boards tried");
            });
            if (Application.isBatchMode)
                EditorApplication.Exit(complete ? 0 : 1);
        }

        private static bool Generate(System.Action<int, int, int> progress)
        {
            LevelGenerationSettings settings = LoadOrCreate<LevelGenerationSettings>(SettingsPath);
            LevelGenerationOptions options = settings.ToOptions();

            var timer = Stopwatch.StartNew();
            LevelSetResult result = LevelSetGenerator.Generate(options,
                (done, attempts) => progress?.Invoke(done, options.LevelCount, attempts));
            timer.Stop();

            LevelDatabase database = LoadOrCreate<LevelDatabase>(DatabasePath);
            database.SetLevels(result.Levels.Select(l => new LevelDatabase.Entry { Code = l.Code, MinMoves = l.MinMoves }));
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            string summary = $"[LevelGenerator] {result.Levels.Count}/{options.LevelCount} levels, " +
                             $"{result.Attempts} boards tried, {timer.Elapsed.TotalSeconds:F1}s, " +
                             $"moves {result.Levels.FirstOrDefault().MinMoves}..{result.Levels.LastOrDefault().MinMoves}";
            if (result.IsComplete)
            {
                Debug.Log(summary);
            }
            else
            {
                string missing = string.Join(", ", result.Missing.Select(m => $"{m.Key} moves x{m.Value}"));
                Debug.LogError($"{summary}. INCOMPLETE — missing: {missing}");
            }
            return result.IsComplete;
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
