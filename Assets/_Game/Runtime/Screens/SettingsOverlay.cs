using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;

namespace Blocktrotter.Screens
{
    /// <summary>Panel over the current screen: language, sound, how to play, legal pages, contact and (where required) privacy options.</summary>
    public sealed class SettingsOverlay : UiScreen
    {
        private readonly ThemeDefinition _theme;
        private bool _pickingLanguage;

        /// <summary>Uses the theme of the screen it covers.</summary>
        public SettingsOverlay(ThemeDefinition theme) => _theme = theme;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => _theme;

        /// <inheritdoc/>
        protected override void Build()
        {
            UiFactory.Dim(Root, new Color(0f, 0f, 0f, 0.6f));
            if (_pickingLanguage)
            {
                BuildLanguagePicker();
                return;
            }
            bool privacyOptions = App.Ads.PrivacyOptionsRequired;
            RectTransform panel = UiFactory.Rect("Panel", Root, Vector2.zero, new Vector2(860, privacyOptions ? 1600 : 1440));
            UiFactory.Image(panel, Theme.PanelColor);
            float top = privacyOptions ? 710 : 630;

            RectTransform title = UiFactory.Rect("Title", panel, new Vector2(0, top), new Vector2(760, 110));
            UiFactory.Label(title, T("settings.title"), 68, Theme.TextColor);

            ThemedButton("Language", panel, new Vector2(0, top - 180), new Vector2(640, 140),
                T("settings.language", T("lang." + App.Loc.Language)), 50, () => ShowLanguagePicker(true));

            ThemedButton("Sound", panel, new Vector2(0, top - 340), new Vector2(640, 140),
                T(App.Progress.SoundOn ? "settings.sound_on" : "settings.sound_off"), 50, ToggleSound);

            string lang = App.Loc.Language;
            ThemedButton("HowToPlay", panel, new Vector2(0, top - 500), new Vector2(640, 130),
                T("settings.how_to_play"), 44, () => App.ShowTutorial());
            ThemedButton("Privacy", panel, new Vector2(0, top - 650), new Vector2(640, 130),
                T("settings.privacy_policy"), 44, () => Application.OpenURL(App.Config.PrivacyUrl(lang)));
            ThemedButton("Terms", panel, new Vector2(0, top - 800), new Vector2(640, 130),
                T("settings.terms"), 44, () => Application.OpenURL(App.Config.TermsUrl(lang)));
            ThemedButton("Contact", panel, new Vector2(0, top - 950), new Vector2(640, 130),
                T("settings.contact"), 44, () => Application.OpenURL(App.Config.SupportMailto(Application.version)));
            if (privacyOptions)
                ThemedButton("PrivacyOptions", panel, new Vector2(0, top - 1100), new Vector2(640, 130),
                    T("settings.privacy_options"), 44, () => App.Ads.ShowPrivacyOptions(null));

            ThemedButton("Close", panel, new Vector2(0, -top + 100), new Vector2(640, 140),
                T("settings.close"), 54, App.CloseOverlay, primary: true);
        }

        /// <summary>Two-column list of every language, each written in its own language; the active one is highlighted.</summary>
        private void BuildLanguagePicker()
        {
            var languages = App.Loc.Languages;
            int rows = (languages.Count + 1) / 2;
            const float rowStep = 170f;
            float height = 420f + rows * rowStep;
            RectTransform panel = UiFactory.Rect("LanguagePanel", Root, Vector2.zero, new Vector2(860, height));
            UiFactory.Image(panel, Theme.PanelColor);
            float top = height / 2f - 95f;

            RectTransform title = UiFactory.Rect("Title", panel, new Vector2(0, top), new Vector2(760, 110));
            UiFactory.Label(title, T("settings.choose_language"), 64, Theme.TextColor);

            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i];
                var pos = new Vector2(i % 2 == 0 ? -195f : 195f, top - 170f - (i / 2) * rowStep);
                ThemedButton("Lang_" + code, panel, pos, new Vector2(360, 140), T("lang." + code), 46,
                    () => { ShowLanguagePicker(false); App.SetLanguage(code); }, primary: code == App.Loc.Language);
            }

            ThemedButton("Back", panel, new Vector2(0, -height / 2f + 100f), new Vector2(640, 140),
                T("settings.back"), 54, () => ShowLanguagePicker(false));
        }

        /// <summary>Switches the panel between the settings list and the language picker.</summary>
        public void ShowLanguagePicker(bool show)
        {
            _pickingLanguage = show;
            Rebuild();
        }

        private void ToggleSound()
        {
            App.Progress.SoundOn = !App.Progress.SoundOn;
            App.SaveProgress();
            Rebuild();
        }
    }
}
