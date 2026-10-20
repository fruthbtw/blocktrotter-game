using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Blocktrotter.UI
{
    /// <summary>
    /// Small helpers for building uGUI hierarchies in code. Positions use a
    /// centre anchor unless stated; sizes are in reference pixels (1080x1920).
    /// </summary>
    /// <example>
    /// <code>
    /// RectTransform panel = UiFactory.Rect("Panel", parent, new Vector2(0, 300), new Vector2(900, 200));
    /// UiFactory.Label(panel, "Level", 48, Color.white);
    /// </code>
    /// </example>
    public static class UiFactory
    {
        private static Font _font;

        /// <summary>Built-in font; dynamic, so Turkish characters fall back to OS glyphs.</summary>
        public static Font DefaultFont => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Empty RectTransform centred on its parent at <paramref name="position"/>.</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>RectTransform stretched over its whole parent.</summary>
        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>Adds a tinted rounded-rect (or other) image to <paramref name="rt"/>.</summary>
        public static Image Image(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : ProceduralSprites.RoundedRect;
            image.type = image.sprite == ProceduralSprites.RoundedRect ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Full-rect, square-cornered tint that blocks touches (dims the screen behind a panel).</summary>
        public static Image Dim(RectTransform rt, Color color)
        {
            Image image = Image(rt, color, raycast: true);
            image.sprite = null;   // plain quad: no rounded corners at the screen edges
            // Overlays live inside the safe area; reach past it so the notch and home bar dim too.
            rt.offsetMin -= Vector2.one * 600f;
            rt.offsetMax += Vector2.one * 600f;
            image.type = UnityEngine.UI.Image.Type.Simple;
            return image;
        }

        /// <summary>Text filling <paramref name="parent"/>, centred.</summary>
        public static Text Label(RectTransform parent, string text, int fontSize, Color color, FontStyle style = FontStyle.Bold)
        {
            RectTransform rt = Stretch("Label", parent);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Three star images in a row, centred on <paramref name="parent"/>; colour them with <see cref="SetStars"/>.</summary>
        public static Image[] StarRow(RectTransform parent, Vector2 center, float size, float spacing)
        {
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform rt = Rect($"Star{i}", parent, center + new Vector2((i - 1) * (size + spacing), 0f), Vector2.one * size);
                stars[i] = Image(rt, Color.white, ProceduralSprites.Star);
            }
            return stars;
        }

        /// <summary>First <paramref name="count"/> stars get <paramref name="on"/>, the rest <paramref name="off"/>.</summary>
        public static void SetStars(Image[] stars, int count, Color on, Color off)
        {
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < count ? on : off;
        }

        /// <summary>
        /// Makes a locked (non-interactable) element look muted but stay opaque, so
        /// background decor never shows through. Replaces Unity's half-transparent
        /// disabled tint for this button.
        /// </summary>
        public static void ApplyLockedLook(Button button, Image background, Color muted, float contentAlpha = 0.45f)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            background.color = muted;
            foreach (Graphic g in button.GetComponentsInChildren<Graphic>())
                if (g != background) g.color = g.color * new Color(1f, 1f, 1f, contentAlpha);
        }

        /// <summary>Rounded button showing a centred icon instead of text.</summary>
        public static Button IconButton(string name, Transform parent, Vector2 position, Vector2 size,
            Sprite icon, float iconSize, Color background, Color iconColor, UnityAction onClick)
        {
            RectTransform rt = Rect(name, parent, position, size);
            Image image = Image(rt, background, raycast: true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            Image(Rect("Icon", rt, Vector2.zero, Vector2.one * iconSize), iconColor, icon);
            return button;
        }

        /// <summary>Small round count badge in the top-right corner of <paramref name="parent"/>; returns its label.</summary>
        public static Text Badge(RectTransform parent, float diameter, Color background, Color textColor)
        {
            RectTransform rt = Rect("Badge", parent, Vector2.zero, Vector2.one * diameter);
            rt.anchorMin = rt.anchorMax = Vector2.one;
            rt.anchoredPosition = new Vector2(-diameter * 0.3f, -diameter * 0.3f);
            Image(rt, background, ProceduralSprites.Circle);
            return Label(rt, "", Mathf.RoundToInt(diameter * 0.55f), textColor);
        }

        /// <summary>Rounded button with a centred label.</summary>
        public static Button Button(string name, Transform parent, Vector2 position, Vector2 size,
            string text, int fontSize, Color background, Color textColor, UnityAction onClick)
        {
            RectTransform rt = Rect(name, parent, position, size);
            Image image = Image(rt, background, raycast: true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            Label(rt, text, fontSize, textColor);
            return button;
        }
    }
}
