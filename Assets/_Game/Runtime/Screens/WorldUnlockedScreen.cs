using System.Collections;
using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;

namespace Blocktrotter.Screens
{
    /// <summary>"New world unlocked!" — shown once when the first level of a world opens.</summary>
    public sealed class WorldUnlockedScreen : UiScreen
    {
        private readonly int _world;

        /// <summary>Celebrates the 0-based <paramref name="world"/>.</summary>
        public WorldUnlockedScreen(int world) => _world = world;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(_world);

        /// <inheritdoc/>
        protected override void Build()
        {
            RectTransform panel = UiFactory.Rect("Panel", Root, new Vector2(0, 80), new Vector2(920, 1000));
            UiFactory.Image(panel, Theme.PanelColor);

            RectTransform headline = UiFactory.Rect("Headline", panel, new Vector2(0, 380), new Vector2(860, 110));
            UiFactory.Label(headline, T("world.unlocked"), 60, Theme.ExitColor);

            // Big board swatch in the new world's colours.
            RectTransform swatch = UiFactory.Rect("Swatch", panel, new Vector2(0, 90), new Vector2(380, 380));
            UiFactory.Image(swatch, Theme.BoardColor);
            UiFactory.Image(UiFactory.Rect("Target", swatch, new Vector2(-40, 0), new Vector2(240, 110)), Theme.TargetColor);
            UiFactory.Image(UiFactory.Rect("Block", swatch, new Vector2(120, 60), new Vector2(90, 220)), Theme.BlockColor);

            RectTransform name = UiFactory.Rect("Name", panel, new Vector2(0, -180), new Vector2(860, 120));
            UiFactory.Label(name, T(Theme.NameKey), 80, Theme.TextColor);
            RectTransform sub = UiFactory.Rect("Levels", panel, new Vector2(0, -270), new Vector2(860, 70));
            int first = App.WorldStart(_world) + 1;
            UiFactory.Label(sub, T("world.levels", first, first + App.WorldSize(_world) - 1), 44, Theme.TextColor, FontStyle.Normal);

            ThemedButton("Continue", panel, new Vector2(0, -400), new Vector2(620, 150), T("world.continue"), 58,
                () => App.PlayLevel(App.WorldStart(_world)), primary: true);

            App.StartCoroutine(Celebrate());
        }

        private IEnumerator Celebrate()
        {
            yield return null;   // let layout settle
            if (Root == null) yield break;
            ConfettiBurst.Spawn(Root, new Vector2(0, -300), App.Config.ConfettiCount,
                App.Config.ConfettiDuration * 1.3f, new[] { Theme.TargetColor, Theme.ExitColor, Theme.BlockColor, Theme.DecorColor });
        }
    }
}
