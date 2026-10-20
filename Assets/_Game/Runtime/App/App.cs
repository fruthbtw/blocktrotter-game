using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.Localization;
using Blocktrotter.Save;
using Blocktrotter.Screens;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Blocktrotter.AppShell
{
    /// <summary>
    /// Scene root: owns shared services (levels, config, themes, localization,
    /// saved progress, hints) and switches between screens. One instance lives in
    /// the Game scene.
    /// Launch arguments for run-and-observe captures:
    /// <c>--screen menu|worlds|levels|settings</c>, <c>--world N</c> (1-based),
    /// <c>--level N</c> (1-based, opens the game screen), <c>--lang en|tr</c>,
    /// <c>--demo-progress N</c> (pretend the first N levels are solved; not saved).
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        [SerializeField] private LevelDatabase _levels;
        [SerializeField] private GameConfig _config;
        [SerializeField] private ThemeDefinition[] _themes;
        [SerializeField] private TextAsset _englishStrings;
        [SerializeField] private TextAsset _turkishStrings;

        private IProgressStore _store;
        private bool _saveEnabled = true;
        private Image _background;
        private RectTransform _screenLayer, _overlayLayer;
        private UiScreen _current, _overlay;
        private readonly System.Collections.Generic.Dictionary<ThemeDefinition, Sprite> _gradients =
            new System.Collections.Generic.Dictionary<ThemeDefinition, Sprite>();

        /// <summary>All levels.</summary>
        public LevelDatabase Levels => _levels;

        /// <summary>Gameplay tuning.</summary>
        public GameConfig Config => _config;

        /// <summary>Localized strings.</summary>
        public Localizer Loc { get; private set; }

        /// <summary>Saved progress (call <see cref="SaveProgress"/> after changing it).</summary>
        public PlayerProgress Progress { get; private set; }

        /// <summary>Hints the player owns; written back to progress on save.</summary>
        public HintWallet Hints { get; private set; }

        /// <summary>Number of worlds (themes).</summary>
        public int WorldCount => Mathf.CeilToInt((float)_levels.Count / _config.LevelsPerTheme);

        /// <summary>World index (0-based) a level belongs to.</summary>
        public int WorldOf(int levelIndex) => levelIndex / _config.LevelsPerTheme;

        /// <summary>First level index of a world.</summary>
        public int WorldStart(int world) => world * _config.LevelsPerTheme;

        /// <summary>Number of levels in a world.</summary>
        public int WorldSize(int world) => Mathf.Min(_config.LevelsPerTheme, _levels.Count - WorldStart(world));

        /// <summary>Theme for a world; wraps if there are more worlds than themes.</summary>
        public ThemeDefinition ThemeOfWorld(int world) => _themes[world % _themes.Length];

        private void Start()
        {
            Application.targetFrameRate = 60;
            _store = new PlayerPrefsProgressStore();
            Progress = _store.Load() ?? PlayerProgress.Create(_levels.Count, _config.StartingHints);
            Progress.EnsureSize(_levels.Count);
            Hints = new HintWallet(Progress.Hints);
            ApplyDemoProgress();

            Loc = new Localizer(_englishStrings, _turkishStrings);
            string lang = Arg("--lang") ?? Progress.Language;
            Loc.SetLanguage(string.IsNullOrEmpty(lang) ? Localizer.DetectLanguage() : lang);

            EnsureEventSystem();
            BuildCanvas();
            OpenFromArgs();
        }

        // ------------------------------------------------------------ navigation

        /// <summary>Shows the main menu.</summary>
        public void ShowMainMenu() => Show(new MainMenuScreen());

        /// <summary>Shows the world list.</summary>
        public void ShowWorlds() => Show(new WorldSelectScreen());

        /// <summary>Shows the level grid of one world.</summary>
        public void ShowLevels(int world) => Show(new LevelSelectScreen(world));

        /// <summary>Opens a level if it is unlocked.</summary>
        public void PlayLevel(int levelIndex)
        {
            if (Progress.IsUnlocked(levelIndex)) Show(new GameScreen(levelIndex));
        }

        /// <summary>Opens the settings panel over the current screen.</summary>
        public void OpenSettings()
        {
            CloseOverlay();
            _overlay = new SettingsOverlay(_current.Theme);
            _overlay.Show(this, _overlayLayer);
        }

        /// <summary>Closes the settings panel.</summary>
        public void CloseOverlay()
        {
            _overlay?.Hide();
            _overlay = null;
        }

        /// <summary>Switches language, saves it and rebuilds visible UI.</summary>
        public void SetLanguage(string code)
        {
            Loc.SetLanguage(code);
            Progress.Language = Loc.Language;
            SaveProgress();
            _current.Rebuild();
            _overlay?.Rebuild();
        }

        /// <summary>Writes progress (including hints) to the device.</summary>
        public void SaveProgress()
        {
            Progress.Hints = Hints.Count;
            if (_saveEnabled) _store.Save(Progress);
        }

        private void Show(UiScreen screen)
        {
            CloseOverlay();
            _current?.Hide();
            _current = screen;
            screen.Show(this, _screenLayer);   // binds App first; Theme depends on it
            if (!_gradients.TryGetValue(screen.Theme, out Sprite gradient))
                _gradients[screen.Theme] = gradient = ProceduralSprites.VerticalGradient(screen.Theme.BackgroundBottom, screen.Theme.BackgroundTop);
            _background.sprite = gradient;
        }

        // ------------------------------------------------------------ setup

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            _background = UiFactory.Image(UiFactory.Stretch("Background", canvas.transform), Color.white,
                ProceduralSprites.VerticalGradient(Color.white, Color.white));
            _screenLayer = UiFactory.Stretch("Screens", canvas.transform);
            _screenLayer.gameObject.AddComponent<SafeAreaFitter>();
            _overlayLayer = UiFactory.Stretch("Overlays", canvas.transform);
        }

        private void OpenFromArgs()
        {
            int world = int.TryParse(Arg("--world"), out int w) ? Mathf.Clamp(w - 1, 0, WorldCount - 1) : 0;
            if (int.TryParse(Arg("--level"), out int level))
            {
                Show(new GameScreen(Mathf.Clamp(level - 1, 0, _levels.Count - 1)));
                return;
            }
            switch (Arg("--screen"))
            {
                case "worlds": ShowWorlds(); break;
                case "levels": ShowLevels(world); break;
                case "settings": ShowMainMenu(); OpenSettings(); break;
                default: ShowMainMenu(); break;
            }
        }

        private void ApplyDemoProgress()
        {
            if (!int.TryParse(Arg("--demo-progress"), out int solved)) return;
            _saveEnabled = false;   // a capture must never overwrite real progress
            Progress = PlayerProgress.Create(_levels.Count, _config.StartingHints);
            for (int i = 0; i < Mathf.Min(solved, _levels.Count); i++)
                Progress.RecordSolve(i, 1 + i % 3);
            Hints = new HintWallet(Progress.Hints);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static string Arg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
