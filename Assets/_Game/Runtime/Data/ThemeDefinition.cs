using UnityEngine;

namespace Blocktrotter.Data
{
    /// <summary>
    /// Colours for one world (Japanese garden, Egyptian desert, ...). Everything is
    /// drawn from these colours in code — no third-party art.
    /// </summary>
    /// <example><code>blockImage.color = theme.BlockColor;</code></example>
    [CreateAssetMenu(fileName = "Theme", menuName = "Blocktrotter/Theme")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [Tooltip("Localization key for the theme name.")]
        public string NameKey = "theme.garden";

        [Header("Background")]
        public Color BackgroundTop = new Color32(0xF9, 0xD8, 0xE3, 0xFF);
        public Color BackgroundBottom = new Color32(0xFD, 0xF1, 0xF4, 0xFF);

        [Header("Panels & text")]
        public Color PanelColor = new Color32(0x3E, 0x4C, 0x3A, 0xFF);
        public Color TextColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color ButtonColor = new Color32(0x56, 0x6B, 0x50, 0xFF);

        [Header("Board")]
        public Color BoardFrameColor = new Color32(0x2F, 0x3A, 0x2C, 0xFF);
        public Color BoardColor = new Color32(0x4E, 0x6B, 0x4A, 0xFF);
        public Color ExitColor = new Color32(0xF2, 0xC1, 0x4E, 0xFF);

        [Header("Blocks")]
        public Color BlockColor = new Color32(0xEC, 0xDC, 0xB0, 0xFF);
        public Color BlockShadowColor = new Color32(0x2A, 0x33, 0x27, 0xFF);
        public Color TargetColor = new Color32(0xD7, 0x26, 0x3D, 0xFF);
    }
}
