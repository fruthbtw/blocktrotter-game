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
