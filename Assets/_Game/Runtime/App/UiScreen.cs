using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.AppShell
{
    /// <summary>
    /// Base for full-screen views. The UI is built in code under <see cref="Root"/>
    /// and can be rebuilt at any time (e.g. after a language change) without
    /// losing the screen's own state fields.
    /// </summary>
    public abstract class UiScreen
    {
        /// <summary>Owning app (services and navigation).</summary>
        protected App App { get; private set; }

        /// <summary>Root of this screen's UI; destroyed on hide/rebuild.</summary>
        protected RectTransform Root { get; private set; }

        /// <summary>Theme used for this screen's colours and background.</summary>
        public abstract Data.ThemeDefinition Theme { get; }

        /// <summary>Creates the UI under <paramref name="parent"/>.</summary>
        public void Show(App app, RectTransform parent)
        {
            App = app;
            Root = UiFactory.Stretch(GetType().Name, parent);
            Build();
        }

        /// <summary>Destroys and recreates the UI in place.</summary>
        public void Rebuild()
        {
            RectTransform parent = (RectTransform)Root.parent;
            Hide();
            Show(App, parent);
        }

        /// <summary>Destroys the UI.</summary>
        public virtual void Hide()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
            Root = null;
        }

        /// <summary>Builds the screen's UI under <see cref="Root"/>.</summary>
        protected abstract void Build();

        /// <summary>Localized text shortcut.</summary>
        protected string T(string key, params object[] args) => App.Loc.Get(key, args);

        /// <summary>Standard themed button.</summary>
        protected Button ThemedButton(string name, RectTransform parent, Vector2 pos, Vector2 size, string text, int fontSize,
            UnityEngine.Events.UnityAction onClick, bool primary = false)
        {
            return UiFactory.Button(name, parent, pos, size, text, fontSize,
                primary ? Theme.TargetColor : Theme.ButtonColor, Theme.TextColor, onClick);
        }

        /// <summary>Small round "back" button in the top-left corner.</summary>
        protected Button BackButton(UnityEngine.Events.UnityAction onClick)
        {
            Button back = ThemedButton("Back", Root, Vector2.zero, new Vector2(140, 140), "<", 72, onClick);
            var rt = (RectTransform)back.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(110, -110);
            return back;
        }
    }
}
