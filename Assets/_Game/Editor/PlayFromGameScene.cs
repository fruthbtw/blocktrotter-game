using UnityEditor;
using UnityEditor.SceneManagement;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Makes the editor's Play button always start from the Game scene, whatever
    /// scene is open. Editor-only; has no effect on builds.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromGameScene
    {
        static PlayFromGameScene()
        {
            EditorApplication.delayCall += () =>
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectBuilder.ScenePath);
        }
    }
}
