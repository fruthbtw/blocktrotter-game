using System.Collections.Generic;
using Blocktrotter.Core;
using Blocktrotter.Data;
using UnityEngine;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Paints block faces and boards in code, one look per world (<see cref="BlockStyle"/>):
    /// bamboo, carved sandstone, sea glass, cracked ice and neon. Every pixel is computed
    /// here — no imported art — so the look is ours and cannot be mistaken for another
    /// game's. Sprites are cached per theme, length, orientation and target flag.
    /// </summary>
    /// <example>
    /// <code>
    /// image.sprite = BlockSkins.Block(theme, length: 2, Orientation.Horizontal, isTarget: true);
    /// boardImage.sprite = BlockSkins.Board(theme, 6);
    /// </code>
    /// </example>
    public static class BlockSkins
    {
        /// <summary>Texture pixels per board cell. Blocks are ~170 px per cell on a phone.</summary>
        public const int CellPixels = 128;

        private static readonly Dictionary<(ThemeDefinition, int, Orientation, bool), Sprite> BlockCache =
            new Dictionary<(ThemeDefinition, int, Orientation, bool), Sprite>();
        private static readonly Dictionary<(ThemeDefinition, int), Sprite> BoardCache =
            new Dictionary<(ThemeDefinition, int), Sprite>();

        /// <summary>Face sprite for a block covering <paramref name="length"/> cells.</summary>
        public static Sprite Block(ThemeDefinition theme, int length, Orientation orientation, bool isTarget)
        {
            var key = (theme, length, orientation, isTarget);
            if (!BlockCache.TryGetValue(key, out Sprite sprite))
                BlockCache[key] = sprite = PaintBlock(theme, length, orientation, isTarget);
            return sprite;
        }

        /// <summary>Board sprite: the theme's board colour with grooves between cells.</summary>
        public static Sprite Board(ThemeDefinition theme, int size)
        {
            var key = (theme, size);
            if (!BoardCache.TryGetValue(key, out Sprite sprite))
                BoardCache[key] = sprite = PaintBoard(theme, size);
            return sprite;
        }

        // ------------------------------------------------------------------ blocks

        /// <summary>Pixel context: (l, s) run along / across the block; (x, y) are texture pixels, y up.</summary>
        private struct Px
        {
            public float L, S, X, Y, W, H, Len, Inside;   // Inside = distance to the edge (px)
        }

        private static Sprite PaintBlock(ThemeDefinition t, int length, Orientation o, bool target)
        {
            int c = CellPixels;
            int w = o == Orientation.Horizontal ? length * c : c;
            int h = o == Orientation.Horizontal ? c : length * c;
            float radius = c * 0.2f;
            var pixels = new Color32[w * h];
            Vector2[] cracks = t.BlockStyle == BlockStyle.Ice ? CrackSegments(w, h, length * 31 + (int)o * 7) : null;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f, w, h, radius);
                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage <= 0f) { pixels[y * w + x] = default; continue; }

                    var p = new Px
                    {
                        X = x, Y = y, W = w, H = h, Len = length * c, Inside = -d,
                        L = o == Orientation.Horizontal ? x : h - 1 - y,   // along: left→right / top→bottom
                        S = o == Orientation.Horizontal ? y : x            // across
                    };
                    Color col = target ? PaintTarget(t, p) : PaintBlockBody(t, p, cracks);
                    col.a *= coverage;
                    pixels[y * w + x] = col;
                }
            }
            return MakeSprite(pixels, w, h);
        }

        private static Color PaintBlockBody(ThemeDefinition t, Px p, Vector2[] cracks)
        {
            float c = CellPixels;
            if (t.BlockTexture != null)
            {
                Color tex = Sample(t.BlockTexture, p, t.TextureFollowsBlock, t.TextureCellsPerRepeat);
                if (t.BlockStyle == BlockStyle.Neon) return NeonEdge(tex, t.BlockDetailColor, p);   // keep the glowing tube
                if (p.Inside < 4f) tex = Color.Lerp(tex, t.BlockDetailColor, 0.45f);            // crisp outline
                return Light(tex, p, 0.2f, 0.22f);
            }
            Color baseCol = t.BlockColor, detail = t.BlockDetailColor, hi = t.BlockHighlightColor;
            Color col;
            switch (t.BlockStyle)
            {
                case BlockStyle.Bamboo:
                {
                    col = baseCol;
                    col = Color.Lerp(col, detail, Hash(0, (int)(p.S / 3f), 11) < 0.25f ? 0.12f : 0f);   // fibres
                    float across = p.S / c;
                    if (across > 0.58f && across < 0.72f) col = Color.Lerp(col, hi, 0.35f);              // sheen
                    for (float node = c; node < p.Len - 1; node += c)                                       // nodes
                    {
                        float dn = Mathf.Abs(p.L - node);
                        if (dn < 4f) col = Color.Lerp(col, detail, 0.85f);
                        else if (dn < 8f) col = Color.Lerp(col, hi, 0.4f);
                    }
                    return Light(col, p, 0.18f, 0.22f);
                }
                case BlockStyle.Sandstone:
                {
                    col = Color.Lerp(baseCol, detail, (Hash((int)p.X, (int)p.Y, 3) - 0.5f) * 0.16f + 0.08f);
                    int course = p.S < c * 0.5f ? 0 : 1;
                    float brick = c * 0.75f, offset = course * brick * 0.5f;
                    bool joint = Mathf.Abs(p.S - c * 0.5f) < 2f ||
                                 Mathf.Repeat(p.L + offset, brick) < 3f;
                    if (joint) col = Color.Lerp(col, detail, 0.55f);
                    // A carved mark in each cell: a ring, then a bar, alternating.
                    float cellCentre = (Mathf.Floor(p.L / c) + 0.5f) * c;
                    var local = new Vector2(p.L - cellCentre, p.S - c * 0.5f);
                    bool ringCell = ((int)(p.L / c)) % 2 == 0;
                    bool carved = ringCell
                        ? Mathf.Abs(local.magnitude - c * 0.14f) < 2.5f
                        : Mathf.Abs(local.x) < 2.5f && Mathf.Abs(local.y) < c * 0.17f;
                    if (carved) col = Color.Lerp(col, detail, 0.7f);
                    return Light(col, p, 0.14f, 0.2f);
                }
                case BlockStyle.SeaGlass:
                {
                    float centre = 1f - Mathf.Abs(p.S / c - 0.5f) * 2f;
                    col = Color.Lerp(baseCol, hi, centre * 0.25f + (Hash((int)p.X / 2, (int)p.Y / 2, 5) - 0.5f) * 0.08f);
                    var blob = new Vector2((p.X - p.W * 0.25f) / (p.W * 0.18f), (p.Y - p.H * 0.72f) / (p.H * 0.12f));
                    col = Color.Lerp(col, Color.white, Mathf.Clamp01(1f - blob.magnitude) * 0.55f);           // gleam
                    if (p.Inside < 5f) col = Color.Lerp(col, detail, 0.45f);                                   // rim
                    col.a = 0.86f;
                    return Light(col, p, 0.1f, 0.15f);
                }
                case BlockStyle.Ice:
                {
                    col = Color.Lerp(baseCol, hi, p.Y / p.H * 0.35f);
                    if (Hash((int)p.X, (int)p.Y, 9) > 0.985f) col = Color.white;                             // frost
                    if (NearSegments(new Vector2(p.X, p.Y), cracks, 1.6f)) col = Color.Lerp(col, detail, 0.6f);
                    if (p.Inside < 4f) col = Color.Lerp(col, detail, 0.35f);
                    col.a = 0.92f;
                    return Light(col, p, 0.22f, 0.12f);
                }
                case BlockStyle.Neon:
                    return NeonBody(baseCol, detail, p);
                default:
                    return Light(baseCol, p, 0.16f, 0.2f);
            }
        }

        private static Color PaintTarget(ThemeDefinition t, Px p)
        {
            float c = CellPixels;
            Color red = t.TargetColor, detail = t.TargetDetailColor;
            Color col = red;
            if (t.TargetTexture != null)
            {
                col = Sample(t.TargetTexture, p, t.TextureFollowsBlock, t.TargetTextureCellsPerRepeat);
                if (t.BlockStyle == BlockStyle.Neon) return NeonEdge(col, detail, p);
                if (t.BlockStyle == BlockStyle.Bamboo &&
                    (InBand(p.L, c * 0.18f, c * 0.1f) || InBand(p.L, p.Len - c * 0.28f, c * 0.1f))) col = detail;
                col = Color.Lerp(col, Color.white, Mathf.Clamp01((p.Y / p.H - 0.6f) * 1.2f) * 0.3f);   // gloss
                return Light(col, p, 0.2f, 0.25f);
            }
            switch (t.BlockStyle)
            {
                case BlockStyle.Bamboo:   // red lacquer with two gold bands
                    if (InBand(p.L, c * 0.18f, c * 0.1f) || InBand(p.L, p.Len - c * 0.28f, c * 0.1f)) col = detail;
                    col = Color.Lerp(col, Color.white, Mathf.Clamp01((p.Y / p.H - 0.6f) * 1.2f) * 0.35f);   // gloss
                    return Light(col, p, 0.2f, 0.25f);
                case BlockStyle.Sandstone:   // band of alternating gold and lapis stripes near each end
                {
                    Color lapis = new Color(0.12f, 0.24f, 0.55f);
                    bool inBand = p.L < c * 0.62f && p.L > c * 0.14f || p.L > p.Len - c * 0.62f && p.L < p.Len - c * 0.14f;
                    if (inBand) col = Mathf.Repeat(p.L, 14f) < 7f ? detail : lapis;
                    return Light(col, p, 0.16f, 0.22f);
                }
                case BlockStyle.SeaGlass:   // life-ring stripes
                    if (Mathf.Repeat((p.L + p.S) / (c * 0.45f), 2f) < 1f) col = detail;
                    if (p.Inside < 4f) col = Color.Lerp(col, red * 0.6f, 0.5f);
                    return Light(col, p, 0.18f, 0.2f);
                case BlockStyle.Ice:   // wavy snow cap on top
                {
                    float snowLine = p.H - c * (0.22f + 0.06f * Mathf.Sin(p.X * 0.09f));
                    if (p.Y > snowLine) col = Color.Lerp(detail, new Color(0.8f, 0.9f, 1f), Mathf.Clamp01((snowLine + 6 - p.Y) / 6f) * 0.5f);
                    return Light(col, p, 0.2f, 0.2f);
                }
                case BlockStyle.Neon:
                    return NeonBody(red * 0.35f + new Color(0, 0, 0, 0.65f), detail, p);
                default:
                    return Light(col, p, 0.2f, 0.22f);
            }
        }

        /// <summary>Bright neon tube along the edge with a short inner glow, over any body colour.</summary>
        private static Color NeonEdge(Color body, Color edge, Px p)
        {
            body.a = 1f;
            if (p.Inside < 5f) return Color.Lerp(edge, Color.white, 0.25f);
            if (p.Inside < 16f) return Color.Lerp(body, edge, (1f - (p.Inside - 5f) / 11f) * 0.55f);
            return body;
        }

        private static Color NeonBody(Color body, Color edge, Px p)
        {
            Color col = body;
            col.a = 1f;
            float c = CellPixels;
            if (Mathf.Repeat(p.L, c * 0.25f) < 1.5f || Mathf.Repeat(p.S, c * 0.25f) < 1.5f)
                col = Color.Lerp(col, edge, 0.12f);                                       // faint inner grid
            if (p.Inside < 5f) return Color.Lerp(edge, Color.white, 0.25f);              // bright tube
            if (p.Inside < 16f) col = Color.Lerp(col, edge, (1f - (p.Inside - 5f) / 11f) * 0.55f);   // inner glow
            return col;
        }

        /// <summary>Top bevel highlight, bottom bevel shade and a soft top-to-bottom gradient.</summary>
        private static Color Light(Color col, Px p, float topLight, float bottomShade)
        {
            float a = col.a;
            const float bevel = 9f;
            float fromTop = p.H - 1 - p.Y, fromBottom = p.Y;
            col *= Mathf.Lerp(0.9f, 1.04f, p.Y / p.H);
            if (fromTop < bevel) col = Color.Lerp(col, Color.white, topLight * (1f - fromTop / bevel));
            if (fromBottom < bevel) col = Color.Lerp(col, Color.black, bottomShade * (1f - fromBottom / bevel));
            col.a = a;
            return col;
        }

        // ------------------------------------------------------------------ board

        private static Sprite PaintBoard(ThemeDefinition t, int size)
        {
            int c = CellPixels / 2;
            int n = size * c;
            var pixels = new Color32[n * n];
            Color groove = t.BoardColor * 0.8f;
            groove.a = 1f;
            Color lit = Color.Lerp(t.BoardColor, Color.white, 0.06f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f, n, n, c * 0.25f);
                    float coverage = Mathf.Clamp01(0.5f - d);
                    float lx = x % c, ly = y % c;
                    Color col = t.BoardColor;
                    if (lx < 2 || lx > c - 3 || ly < 2 || ly > c - 3) col = groove;            // groove between cells
                    else if (ly > c - 8) col = lit;                                              // light lip on each cell
                    if (t.BlockStyle == BlockStyle.Neon && (lx < 1 || ly < 1)) col = Color.Lerp(col, t.BlockDetailColor, 0.35f);
                    col.a = coverage;
                    pixels[y * n + x] = col;
                }
            }
            return MakeSprite(pixels, n, n);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Reads a seamless texture repeating every <paramref name="cellsPerRepeat"/> cells. With
        /// <paramref name="followsBlock"/> its vertical axis runs along the block (a bamboo stalk
        /// follows the block whichever way it lies); otherwise it stays upright on screen.
        /// Samples a mip close to the on-screen size to avoid shimmering fine detail.
        /// </summary>
        private static Color Sample(Texture2D tex, Px p, bool followsBlock, float cellsPerRepeat)
        {
            float span = CellPixels * cellsPerRepeat;
            int mip = Mathf.Clamp(Mathf.FloorToInt(Mathf.Log(tex.width / span, 2f)), 0, tex.mipmapCount - 1);
            return followsBlock
                ? tex.GetPixelBilinear(p.S / span, -p.L / span, mip)
                : tex.GetPixelBilinear(p.X / span, p.Y / span, mip);
        }

        private static bool InBand(float v, float start, float width) => v >= start && v < start + width;

        private static float RoundedRectDistance(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
            float qy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>Deterministic 0..1 noise for a pixel; the same inputs always give the same value.</summary>
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>A few jagged crack polylines, fixed per seed, as consecutive point pairs.</summary>
        private static Vector2[] CrackSegments(int w, int h, int seed)
        {
            var rng = new System.Random(seed);
            var points = new List<Vector2>();
            for (int crack = 0; crack < 2; crack++)
            {
                var p = new Vector2((float)rng.NextDouble() * w, (float)rng.NextDouble() * h);
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                for (int step = 0; step < 4; step++)
                {
                    angle += ((float)rng.NextDouble() - 0.5f) * 1.4f;
                    Vector2 next = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * CellPixels * 0.22f;
                    points.Add(p);
                    points.Add(next);
                    p = next;
                }
            }
            return points.ToArray();
        }

        private static bool NearSegments(Vector2 p, Vector2[] segments, float width)
        {
            for (int i = 0; i + 1 < segments.Length; i += 2)
            {
                Vector2 a = segments[i], b = segments[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if ((a + ab * t - p).sqrMagnitude < width * width) return true;
            }
            return false;
        }

        private static Sprite MakeSprite(Color32[] pixels, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);   // no longer readable: frees the CPU copy
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
