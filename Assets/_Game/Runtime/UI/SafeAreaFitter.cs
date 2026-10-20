using UnityEngine;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Keeps its RectTransform inside <see cref="Screen.safeArea"/> (notch, home
    /// indicator). Put it on a full-screen child of the canvas.
    /// </summary>
    /// <example><code>UiFactory.Stretch("SafeArea", canvas.transform).gameObject.AddComponent&lt;SafeAreaFitter&gt;();</code></example>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (_applied != Screen.safeArea || _screen.x != Screen.width || _screen.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
