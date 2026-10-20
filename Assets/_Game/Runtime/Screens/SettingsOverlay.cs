using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.Localization;
using Blocktrotter.UI;
using UnityEngine;

namespace Blocktrotter.Screens
{
    /// <summary>Panel over the current screen: language, sound, legal pages and (where required) privacy options.</summary>
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
            bool privacyOptions = App.Ads.PrivacyOptionsRequired;
            RectTransform panel = UiFactory.Rect("Panel", Root, Vector2.zero, new Vector2(860, privacyOptions ? 1300 : 1140));
            UiFactory.Image(panel, Theme.PanelColor);
            float top = privacyOptions ? 560 : 480;

            RectTransform title = UiFactory.Rect("Title", panel, new Vector2(0, top), new Vector2(760, 110));
            UiFactory.Label(title, T("settings.title"), 68, Theme.TextColor);

            string next = App.Loc.Language == Localizer.Turkish ? Localizer.English : Localizer.Turkish;
            ThemedButton("Language", panel, new Vector2(0, top - 180), new Vector2(640, 140),
                T("settings.language", T("lang." + App.Loc.Language)), 50, () => App.SetLanguage(next));

            ThemedButton("Sound", panel, new Vector2(0, top - 340), new Vector2(640, 140),
                T(App.Progress.SoundOn ? "settings.sound_on" : "settings.sound_off"), 50, ToggleSound);

            string lang = App.Loc.Language;
            ThemedButton("Privacy", panel, new Vector2(0, top - 500), new Vector2(640, 130),
                T("settings.privacy_policy"), 44, () => Application.OpenURL(App.Config.PrivacyUrl(lang)));
            ThemedButton("Terms", panel, new Vector2(0, top - 650), new Vector2(640, 130),
                T("settings.terms"), 44, () => Application.OpenURL(App.Config.TermsUrl(lang)));
            if (privacyOptions)
                ThemedButton("PrivacyOptions", panel, new Vector2(0, top - 800), new Vector2(640, 130),
                    T("settings.privacy_options"), 44, () => App.Ads.ShowPrivacyOptions(null));

            ThemedButton("Close", panel, new Vector2(0, -top + 100), new Vector2(640, 140),
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
