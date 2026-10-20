using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Screens
{
    /// <summary>One card per world with solved count and stars; locked until its first level unlocks.</summary>
    public sealed class WorldSelectScreen : UiScreen
    {
        private const float CardHeight = 230f;
        private const float CardStep = 262f;
        private const float FirstCardY = 520f;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(App.WorldOf(App.Progress.FrontierLevel));

        /// <inheritdoc/>
        protected override void Build()
        {
            BackButton(App.ShowMainMenu);
            RectTransform title = UiFactory.Rect("Title", Root, new Vector2(0, 790), new Vector2(700, 120));
            UiFactory.Label(title, T("worlds.title"), 76, Theme.PanelColor);

            for (int w = 0; w < App.WorldCount; w++)
                BuildCard(w, new Vector2(0, FirstCardY - w * CardStep));
        }

        private void BuildCard(int world, Vector2 position)
        {
            ThemeDefinition theme = App.ThemeOfWorld(world);
            int start = App.WorldStart(world), size = App.WorldSize(world);
            bool unlocked = App.Progress.IsUnlocked(start);

            RectTransform card = UiFactory.Rect($"World{world}", Root, position, new Vector2(940, CardHeight));
            Image bg = UiFactory.Image(card, theme.PanelColor, raycast: true);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = unlocked;
            int w = world;
            button.onClick.AddListener(() => App.ShowLevels(w));

            // Mini board swatch in the world's colours.
            RectTransform swatch = UiFactory.Rect("Swatch", card, new Vector2(-340, 0), new Vector2(170, 170));
            UiFactory.Image(swatch, theme.BoardColor);
            RectTransform target = UiFactory.Rect("Target", swatch, new Vector2(-15, 0), new Vector2(110, 50));
            UiFactory.Image(target, theme.TargetColor);
            RectTransform block = UiFactory.Rect("Block", swatch, new Vector2(55, 30), new Vector2(40, 100));
            UiFactory.Image(block, theme.BlockColor);

            RectTransform name = UiFactory.Rect("Name", card, new Vector2(130, 45), new Vector2(680, 80));
            Text nameLabel = UiFactory.Label(name, T(theme.NameKey), 54, theme.TextColor);
            nameLabel.alignment = TextAnchor.MiddleLeft;

            RectTransform info = UiFactory.Rect("Info", card, new Vector2(130, -45), new Vector2(680, 70));
            string text = unlocked
                ? T("worlds.progress", App.Progress.SolvedInRange(start, size), size, App.Progress.StarsInRange(start, size), size * 3)
                : T("worlds.locked");
            Text infoLabel = UiFactory.Label(info, text, 36, theme.ExitColor, FontStyle.Normal);
            infoLabel.resizeTextForBestFit = true;   // long translations shrink instead of wrapping
            infoLabel.resizeTextMinSize = 26;
            infoLabel.resizeTextMaxSize = 36;
            infoLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoLabel.verticalOverflow = VerticalWrapMode.Truncate;
            infoLabel.alignment = TextAnchor.MiddleLeft;

            if (!unlocked)
            {
                var group = card.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0.55f;
            }
        }
    }
}
