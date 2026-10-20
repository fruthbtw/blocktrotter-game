using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.UI
{
    /// <summary>
    /// One-shot UI confetti: coloured pieces burst upward from a point, tumble
    /// under gravity, fade, then the object destroys itself. Allocates only on
    /// spawn; the per-frame update reuses its arrays.
    /// </summary>
    /// <example><code>ConfettiBurst.Spawn(overlayRect, Vector2.zero, 90, 1.4f, colors);</code></example>
    public sealed class ConfettiBurst : MonoBehaviour
    {
        private const float Gravity = -2600f;           // reference px / s²
        private const float MinSpeed = 900f, MaxSpeed = 2100f;
        private const float SpreadDegrees = 70f;        // either side of straight up
        private static readonly Vector2 PieceSize = new Vector2(46f, 24f);

        private RectTransform[] _pieces;
        private CanvasGroup _group;
        private Vector2[] _velocity;
        private float[] _spin;
        private float _age, _duration;

        /// <summary>Spawns a burst inside <paramref name="parent"/> at <paramref name="origin"/> (local position).</summary>
        public static ConfettiBurst Spawn(RectTransform parent, Vector2 origin, int count, float duration, Color[] colors)
        {
            RectTransform root = UiFactory.Stretch("Confetti", parent);
            var burst = root.gameObject.AddComponent<ConfettiBurst>();
            burst.Init(origin, count, duration, colors);
            return burst;
        }

        private void Init(Vector2 origin, int count, float duration, Color[] colors)
        {
            _duration = duration;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _pieces = new RectTransform[count];
            _velocity = new Vector2[count];
            _spin = new float[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 size = i % 3 == 0 ? Vector2.one * PieceSize.x * 0.8f : PieceSize;
                RectTransform rt = UiFactory.Rect("Piece", transform, origin, size);
                Image img = UiFactory.Image(rt, colors[i % colors.Length], i % 3 == 0 ? ProceduralSprites.Star : null);
                img.raycastTarget = false;
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                float angle = (90f + Random.Range(-SpreadDegrees, SpreadDegrees)) * Mathf.Deg2Rad;
                _velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(MinSpeed, MaxSpeed);
                _spin[i] = Random.Range(-720f, 720f);
                _pieces[i] = rt;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            for (int i = 0; i < _pieces.Length; i++)
            {
                _velocity[i].y += Gravity * dt;
                _velocity[i].x *= 1f - 1.5f * dt;   // air drag so pieces flutter down
                _pieces[i].anchoredPosition += _velocity[i] * dt;
                _pieces[i].Rotate(0f, 0f, _spin[i] * dt);
            }
            _group.alpha = Mathf.Clamp01((_duration - _age) / (_duration * 0.35f));
            if (_age >= _duration) Destroy(gameObject);
        }
    }
}
