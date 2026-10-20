using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.Localization;
using Blocktrotter.UI;
using UnityEngine;

namespace Blocktrotter.Screens
{
    /// <summary>Panel over the current screen: language and sound. (Privacy links come in phase 7.)</summary>
    public sealed class SettingsOverlay : UiScreen
    {
        private readonly ThemeDefinition _theme;

        /// <summary>Uses the theme of the screen it covers.</summary>
        public SettingsOverlay(ThemeDefinition theme) => _theme = theme;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => _theme;

        /// <inheritdoc/>
        protected override void Build()
        {
            UiFactory.Dim(Root, new Color(0f, 0f, 0f, 0.6f));
            RectTransform panel = UiFactory.Rect("Panel", Root, Vector2.zero, new Vector2(860, 820));
            UiFactory.Image(panel, Theme.PanelColor);

            RectTransform title = UiFactory.Rect("Title", panel, new Vector2(0, 300), new Vector2(760, 110));
            UiFactory.Label(title, T("settings.title"), 68, Theme.TextColor);

            string next = App.Loc.Language == Localizer.Turkish ? Localizer.English : Localizer.Turkish;
            ThemedButton("Language", panel, new Vector2(0, 120), new Vector2(640, 140),
                T("settings.language", T("lang." + App.Loc.Language)), 50, () => App.SetLanguage(next));

            ThemedButton("Sound", panel, new Vector2(0, -50), new Vector2(640, 140),
                T(App.Progress.SoundOn ? "settings.sound_on" : "settings.sound_off"), 50, ToggleSound);

            ThemedButton("Close", panel, new Vector2(0, -260), new Vector2(640, 140),
                T("settings.close"), 54, App.CloseOverlay, primary: true);
        }

        private void ToggleSound()
        {
            App.Progress.SoundOn = !App.Progress.SoundOn;
            App.SaveProgress();
            Rebuild();
        }
    }
}
