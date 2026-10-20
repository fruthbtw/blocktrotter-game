using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;

namespace Blocktrotter.Screens
{
    /// <summary>Title, Play (continues at the frontier level), Worlds and Settings.</summary>
    public sealed class MainMenuScreen : UiScreen
    {
        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(App.WorldOf(App.Progress.FrontierLevel));

        /// <inheritdoc/>
        protected override void Build()
        {
            RectTransform plate = UiFactory.Rect("TitlePlate", Root, new Vector2(0, 520), new Vector2(940, 360));
            UiFactory.Image(plate, Theme.PanelColor);
            RectTransform title = UiFactory.Rect("Title", plate, new Vector2(0, 40), new Vector2(900, 180));
            UiFactory.Label(title, "Blocktrotter", 130, Theme.TextColor);
            RectTransform subtitle = UiFactory.Rect("Subtitle", plate, new Vector2(0, -95), new Vector2(900, 80));
            UiFactory.Label(subtitle, T("menu.subtitle"), 52, Theme.ExitColor, FontStyle.Normal);

            // Decorative target block sliding out of a little "exit" — the game in one picture.
            RectTransform block = UiFactory.Rect("Block", Root, new Vector2(-60, 200), new Vector2(300, 120));
            UiFactory.Image(block, Theme.TargetColor);
            RectTransform exit = UiFactory.Rect("Exit", Root, new Vector2(130, 200), new Vector2(24, 140));
            UiFactory.Image(exit, Theme.ExitColor);

            ThemedButton("Play", Root, new Vector2(0, -60), new Vector2(680, 190),
                T("menu.play"), 76, () => App.PlayLevel(App.Progress.FrontierLevel), primary: true);
            RectTransform level = UiFactory.Rect("PlayLevel", Root, new Vector2(0, -190), new Vector2(680, 60));
            UiFactory.Label(level, T("game.level", App.Progress.FrontierLevel + 1), 44, Theme.PanelColor);

            ThemedButton("Worlds", Root, new Vector2(0, -330), new Vector2(680, 150),
                T("menu.worlds"), 60, App.ShowWorlds);
            ThemedButton("Settings", Root, new Vector2(0, -510), new Vector2(680, 150),
                T("menu.settings"), 60, App.OpenSettings);
        }
    }
}
