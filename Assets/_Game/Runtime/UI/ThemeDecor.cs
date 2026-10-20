using Blocktrotter.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Background decoration for a world, drawn from simple shapes in the theme's
    /// decor colours: falling petals or snow, pyramids, waves, a city skyline.
    /// Sits between the background gradient and the screens; never takes input.
    /// </summary>
    /// <example><code>decor.Apply(theme, config.DecorParticleCount);</code></example>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ThemeDecor : MonoBehaviour
    {
        private const float FallSpeedMin = 60f, FallSpeedMax = 140f;   // reference px / s
        private const float SwayAmplitude = 40f, SwayFrequency = 0.8f;

        private RectTransform _rt;
        private RectTransform[] _drifters = new RectTransform[0];
        private float[] _speed, _phase, _baseX;   // _baseX is 0..1 across the width
        private float _time;

        /// <summary>Clears the current decor and draws the given theme's.</summary>
        public void Apply(ThemeDefinition theme, int particleCount)
        {
            _rt = (RectTransform)transform;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _drifters = new RectTransform[0];

            switch (theme.Decor)
            {
                case DecorStyle.Petals: Drifters(particleCount, theme.DecorColor, ProceduralSprites.Circle, new Vector2(34, 22)); break;
                case DecorStyle.Snow: Drifters(particleCount, theme.DecorColor, ProceduralSprites.Circle, new Vector2(18, 18)); break;
                case DecorStyle.Dunes: Pyramids(theme); break;
                case DecorStyle.Waves: Waves(theme); break;
                case DecorStyle.Skyline: Skyline(theme); break;
            }
        }

        private void Update()
        {
            if (_drifters.Length == 0) return;
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            Rect area = _rt.rect;   // read live: the canvas may still be resizing on the first frames
            float half = area.height / 2f + 40f;
            for (int i = 0; i < _drifters.Length; i++)
            {
                Vector2 p = _drifters[i].anchoredPosition;
                p.y -= _speed[i] * dt;
                if (p.y < -half) p.y = half;   // wrap to the top
                p.x = Mathf.Lerp(area.xMin, area.xMax, _baseX[i]) + Mathf.Sin(_time * SwayFrequency + _phase[i]) * SwayAmplitude;
                _drifters[i].anchoredPosition = p;
                _drifters[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time + _phase[i]) * 35f);
            }
        }

        private void Drifters(int count, Color color, Sprite sprite, Vector2 size)
        {
            Rect area = _rt.rect;
            _drifters = new RectTransform[count];
            _speed = new float[count];
            _phase = new float[count];
            _baseX = new float[count];
            var rng = new System.Random(count);   // same layout every time; no Random state shared
            for (int i = 0; i < count; i++)
            {
                _baseX[i] = (float)rng.NextDouble();
                float y = Mathf.Lerp(area.yMin, area.yMax, (float)rng.NextDouble());
                RectTransform rt = Shape("Drifter", new Vector2(Mathf.Lerp(area.xMin, area.xMax, _baseX[i]), y), size, color * new Color(1, 1, 1, 0.75f), sprite);
                _speed[i] = Mathf.Lerp(FallSpeedMin, FallSpeedMax, (float)rng.NextDouble());
                _phase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
                _drifters[i] = rt;
            }
        }

        private void Pyramids(ThemeDefinition theme)
        {
            Shape("Sun", new Vector2(300, -260), new Vector2(220, 220), theme.DecorAccentColor, ProceduralSprites.Circle, Edge.Top);
            Shape("PyramidBack", new Vector2(-330, 0), new Vector2(520, 330), theme.DecorColor * new Color(1, 1, 1, 0.55f), ProceduralSprites.Triangle, Edge.Bottom);
            Shape("PyramidFront", new Vector2(250, 0), new Vector2(700, 440), theme.DecorColor * new Color(1, 1, 1, 0.8f), ProceduralSprites.Triangle, Edge.Bottom);
        }

        private void Waves(ThemeDefinition theme)
        {
            Shape("Sun", new Vector2(-300, -260), new Vector2(220, 220), theme.DecorAccentColor, ProceduralSprites.Circle, Edge.Top);
            for (int row = 0; row < 2; row++)
                for (int i = -4; i <= 4; i++)
                    Shape("Wave", new Vector2(i * 150 + row * 75, 40 + row * 70), new Vector2(190, 120),
                        theme.DecorColor * new Color(1, 1, 1, row == 0 ? 0.85f : 0.5f), ProceduralSprites.Circle, Edge.Bottom);
        }

        private void Skyline(ThemeDefinition theme)
        {
            float[] heights = { 380, 520, 300, 640, 450, 560, 340, 480 };
            float width = 150f;
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -540 + width / 2f + i * width;
                RectTransform building = Shape("Building", new Vector2(x, heights[i] / 2f), new Vector2(width - 12, heights[i]),
                    theme.DecorColor, null, Edge.Bottom);
                // A few lit windows per building, deterministic pattern.
                for (int w = 0; w < 6; w++)
                {
                    if ((i * 7 + w * 3) % 4 == 0) continue;
                    var win = UiFactory.Rect("Window", building, new Vector2((w % 2 == 0 ? -1 : 1) * 30, heights[i] / 2f - 60 - (w / 2) * 70),
                        new Vector2(26, 34));
                    UiFactory.Image(win, theme.DecorAccentColor * new Color(1, 1, 1, 0.8f), null).sprite = null;
                }
            }
        }

        private enum Edge { Center, Top, Bottom }

        /// <summary>
        /// Adds a shape. Top/Bottom anchor it to that screen edge (pos is relative to the
        /// edge), so it stays put whatever the final canvas height turns out to be.
        /// </summary>
        private RectTransform Shape(string name, Vector2 pos, Vector2 size, Color color, Sprite sprite, Edge edge = Edge.Center)
        {
            RectTransform rt = UiFactory.Rect(name, transform, pos, size);
            if (edge == Edge.Bottom)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                if (sprite == ProceduralSprites.Triangle) rt.pivot = new Vector2(0.5f, 0f);
            }
            else if (edge == Edge.Top)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            }
            Image image = UiFactory.Image(rt, color, sprite);
            image.raycastTarget = false;
            return rt;
        }
    }
}
