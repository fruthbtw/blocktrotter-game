using UnityEngine;

namespace Blocktrotter.Data
{
    /// <summary>
    /// Gameplay tuning values. One asset (Assets/_Game/Data/GameConfig.asset) is
    /// referenced by the game screen; change values there, not in code.
    /// </summary>
    /// <example><code>int stars = StarRating.Calculate(moves, par, config.TwoStarRatio);</code></example>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Blocktrotter/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Hints")]
        [Tooltip("Hints a new player starts with.")]
        [Min(0)] public int StartingHints = 3;

        [Header("Stars")]
        [Tooltip("2 stars up to ceil(optimal * ratio) moves; above that 1 star.")]
        [Min(1f)] public float TwoStarRatio = 1.5f;

        [Header("Ads")]
        [Tooltip("Show an interstitial after every N solved levels.")]
        [Min(1)] public int InterstitialEveryLevels = 5;

        [Header("Themes")]
        [Tooltip("Levels per theme before the next theme starts.")]
        [Min(1)] public int LevelsPerTheme = 100;

        [Header("Feel")]
        [Tooltip("How quickly blocks snap to their cell (higher = snappier).")]
        [Min(1f)] public float BlockSnapSharpness = 22f;

        [Tooltip("Seconds between the target leaving the board and the solved panel.")]
        [Min(0f)] public float SolvedPanelDelay = 0.4f;
    }
}
