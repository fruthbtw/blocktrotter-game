using System;
using UnityEngine;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Button icons drawn in code (white, tint with Image.color): pause, light bulb,
    /// undo arrow, restart arrow. Shapes are defined on a 0..1 square and rasterized
    /// once with supersampling, then cached.
    /// </summary>
    /// <example><code>UiFactory.IconButton("Pause", parent, pos, size, ProceduralIcons.Pause, 90, bg, Color.white, onPause);</code></example>
    public static class ProceduralIcons
    {
        private const int Size = 128;
        private const int Samples = 3;

        private static Sprite _pause, _bulb, _undo, _restart;

        /// <summary>Two vertical bars.</summary>
        public static Sprite Pause => _pause != null ? _pause : (_pause = Rasterize(p =>
            RoundedBox(p, new Vector2(0.34f, 0.5f), new Vector2(0.08f, 0.3f), 0.04f) ||
            RoundedBox(p, new Vector2(0.66f, 0.5f), new Vector2(0.08f, 0.3f), 0.04f)));

        /// <summary>Light bulb: glass, neck and two base bands.</summary>
        public static Sprite Bulb => _bulb != null ? _bulb : (_bulb = Rasterize(p =>
            Vector2.Distance(p, new Vector2(0.5f, 0.6f)) < 0.27f ||                                  // glass
            RoundedBox(p, new Vector2(0.5f, 0.34f), new Vector2(0.13f, 0.045f), 0.02f) ||          // neck
            RoundedBox(p, new Vector2(0.5f, 0.27f), new Vector2(0.12f, 0.035f), 0.03f) ||           // base band 1
            RoundedBox(p, new Vector2(0.5f, 0.17f), new Vector2(0.1f, 0.035f), 0.03f)));            // base band 2

        /// <summary>Counter-clockwise arrow over the top, head pointing down on the left (↶).</summary>
        public static Sprite Undo => _undo != null ? _undo : (_undo = Rasterize(p =>
            Arc(p, new Vector2(0.55f, 0.45f), 0.27f, 0.085f, -20f, 170f) ||
            ArrowHead(p, new Vector2(0.55f, 0.45f), 0.27f, 170f, counterClockwise: true)));

        /// <summary>Almost full clockwise circle with an arrow head (⟳).</summary>
        public static Sprite Restart => _restart != null ? _restart : (_restart = Rasterize(p =>
            Arc(p, new Vector2(0.5f, 0.5f), 0.29f, 0.085f, 120f, 120f + 290f) ||
            ArrowHead(p, new Vector2(0.5f, 0.5f), 0.29f, 120f, counterClockwise: false)));

        // ------------------------------------------------------------ shapes (p in 0..1, y up)

        private static bool RoundedBox(Vector2 p, Vector2 center, Vector2 halfSize, float radius)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - halfSize + Vector2.one * radius;
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) < radius;
        }

        /// <summary>Ring segment from angle a0 to a1 (degrees, CCW from +x).</summary>
        private static bool Arc(Vector2 p, Vector2 c, float r, float thickness, float a0, float a1)
        {
            Vector2 d = p - c;
            if (Mathf.Abs(d.magnitude - r) > thickness / 2f) return false;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (int k = -1; k <= 2; k++)   // accept the angle in any turn the range spans
            {
                float aa = a + 360f * k;
                if (aa >= a0 && aa <= a1) return true;
            }
            return false;
        }

        /// <summary>Triangle at the arc end (angle deg), pointing along the direction of travel.</summary>
        private static bool ArrowHead(Vector2 p, Vector2 c, float r, float angleDeg, bool counterClockwise)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            Vector2 radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 tangent = counterClockwise ? new Vector2(-radial.y, radial.x) : new Vector2(radial.y, -radial.x);
            Vector2 end = c + radial * r;
            const float half = 0.13f, length = 0.17f;
            var tri = new[] { end + radial * half, end - radial * half, end + tangent * length };
            return Inside(tri, p);
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        private static Sprite Rasterize(Func<Vector2, bool> shape)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Samples; sy++)
                        for (int sx = 0; sx < Samples; sx++)
                            if (shape(new Vector2((x + (sx + 0.5f) / Samples) / Size, (y + (sy + 0.5f) / Samples) / Size))) hits++;
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (Samples * Samples)));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
