using UnityEngine;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Sprites generated at runtime, so the game ships no third-party art.
    /// Results are cached; call from the main thread.
    /// </summary>
    /// <example><code>image.sprite = ProceduralSprites.RoundedRect; image.type = Image.Type.Sliced;</code></example>
    public static class ProceduralSprites
    {
        private const int RoundedSize = 64;
        private const int CornerRadius = 24;

        private static Sprite _roundedRect;
        private static Sprite _circle;
        private static Sprite _star;

        /// <summary>White rounded rectangle with 9-slice borders; tint with Image.color.</summary>
        public static Sprite RoundedRect => _roundedRect != null ? _roundedRect : (_roundedRect = CreateRounded(RoundedSize, CornerRadius));

        /// <summary>White anti-aliased circle.</summary>
        public static Sprite Circle => _circle != null ? _circle : (_circle = CreateRounded(RoundedSize, RoundedSize / 2, sliced: false));

        /// <summary>White five-pointed star, anti-aliased.</summary>
        public static Sprite Star => _star != null ? _star : (_star = CreateStar(64));

        /// <summary>Vertical gradient sprite (bottom to top).</summary>
        public static Sprite VerticalGradient(Color bottom, Color top)
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateStar(int size)
        {
            // Ten points alternating outer/inner radius, first point straight up.
            var poly = new Vector2[10];
            float outer = size * 0.48f, inner = outer * 0.45f;
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                poly[i] = new Vector2(size / 2f + Mathf.Cos(angle) * r, size / 2f + Mathf.Sin(angle) * r);
            }

            const int samples = 4;   // 4x4 supersampling for smooth edges
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < samples; sy++)
                        for (int sx = 0; sx < samples; sx++)
                            if (Inside(poly, new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples))) hits++;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (samples * samples)));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        private static Sprite CreateRounded(int size, int radius, bool sliced = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the nearest corner centre, if inside a corner square.
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }
    }
}
