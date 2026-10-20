using System.Collections;
using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.Gameplay;
using Blocktrotter.Localization;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Blocktrotter
{
    /// <summary>
    /// The play screen (plan phase 3): top panel, board, bottom buttons, pause
    /// overlay and a basic solved panel. Builds its UI in code and owns the
    /// <see cref="GameSession"/>; views only raise requests.
    /// Launch argument <c>--level N</c> (1-based) opens a given level.
    /// </summary>
    public sealed class GameScreen : MonoBehaviour
    {
        // Layout in reference pixels (CanvasScaler 1080x1920, Expand).
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);
        private const float BoardSize = 960f;
        private const float BoardFrame = 24f;
        private const float BoardCenterY = 20f;
        private const float TopPanelY = 700f;
        private static readonly Vector2 TopPanelSize = new Vector2(1000, 280);
        private const float ButtonRowY = -640f;
        private static readonly Vector2 ButtonSize = new Vector2(230, 150);

        [SerializeField] private LevelDatabase _levels;
        [SerializeField] private GameConfig _config;
        [SerializeField] private ThemeDefinition _theme;
        [SerializeField] private TextAsset _englishStrings;
        [SerializeField] private TextAsset _turkishStrings;

        private Localizer _loc;
        private HintWallet _hints;
        private GameSession _session;
        private int _levelIndex;

        private BoardView _board;
        private Text _levelLabel, _movesLabel, _hintLabel;
        private Button _prevButton, _nextButton;
        private RectTransform _pauseOverlay, _solvedOverlay;
        private Text _solvedMoves, _solvedStars;

        private void Start()
        {
            _loc = new Localizer(_englishStrings, _turkishStrings);
            _loc.SetLanguage(Localizer.DetectLanguage());
            _hints = new HintWallet(_config.StartingHints);

            EnsureEventSystem();
            BuildUi();
            LoadLevel(Mathf.Clamp(LevelFromArgs() - 1, 0, _levels.Count - 1));
        }

        private void LoadLevel(int index)
        {
            _levelIndex = index;
            _session = new GameSession(_levels.LoadBoard(index));
            _board.Show(_session.Board);
            _pauseOverlay.gameObject.SetActive(false);
            _solvedOverlay.gameObject.SetActive(false);
            RefreshHud();
        }

        private void OnMoveRequested(Move move)
        {
            if (_session.TryMove(move))
                AfterBoardChanged();
            else
                _board.Sync(_session.Board);
        }

        private void AfterBoardChanged()
        {
            _board.Sync(_session.Board);
            RefreshHud();
            if (_session.IsSolved)
                _board.PlayExit(() => StartCoroutine(ShowSolvedAfterDelay()));
        }

        private IEnumerator ShowSolvedAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_config.SolvedPanelDelay);
            int par = _levels.Get(_levelIndex).MinMoves;
            int stars = StarRating.Calculate(_session.MoveCount, par, _config.TwoStarRatio);
            _solvedMoves.text = _loc.Get("solved.moves", _session.MoveCount, par);
            _solvedStars.text = _loc.Get("solved.stars", stars);
            _solvedOverlay.gameObject.SetActive(true);
        }

        private void OnHint()
        {
            if (_board.InputLocked || !_session.UseHint(_hints).HasValue) return;
            AfterBoardChanged();
        }

        private void OnUndo()
        {
            if (_board.InputLocked || !_session.Undo()) return;
            _board.Sync(_session.Board);
            RefreshHud();
        }

        private void OnRestart()
        {
            _session.Restart();
            _board.Show(_session.Board);
            _pauseOverlay.gameObject.SetActive(false);
            _solvedOverlay.gameObject.SetActive(false);
            RefreshHud();
        }

        private void SetPaused(bool paused)
        {
            _pauseOverlay.gameObject.SetActive(paused);
            _board.InputLocked = paused;
        }

        private void RefreshHud()
        {
            _levelLabel.text = _loc.Get("game.level", _levelIndex + 1);
            _movesLabel.text = _loc.Get("game.moves", _session.MoveCount, _levels.Get(_levelIndex).MinMoves);
            _hintLabel.text = _loc.Get("game.hint", _hints.Count);
            _prevButton.interactable = _levelIndex > 0;
            _nextButton.interactable = _levelIndex < _levels.Count - 1;
        }

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform background = UiFactory.Stretch("Background", canvas.transform);
            UiFactory.Image(background, Color.white, ProceduralSprites.VerticalGradient(_theme.BackgroundBottom, _theme.BackgroundTop));

            RectTransform safe = UiFactory.Stretch("SafeArea", canvas.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildTopPanel(safe);
            BuildBoard(safe);
            BuildBottomButtons(safe);
            _pauseOverlay = BuildPauseOverlay(canvas.transform);
            _solvedOverlay = BuildSolvedOverlay(canvas.transform);
        }

        private void BuildTopPanel(RectTransform parent)
        {
            RectTransform panel = UiFactory.Rect("TopPanel", parent, new Vector2(0, TopPanelY), TopPanelSize);
            UiFactory.Image(panel, _theme.PanelColor);

            RectTransform levelRect = UiFactory.Rect("Level", panel, new Vector2(0, 40), new Vector2(600, 120));
            _levelLabel = UiFactory.Label(levelRect, "", 84, _theme.TextColor);
            RectTransform movesRect = UiFactory.Rect("Moves", panel, new Vector2(0, -70), new Vector2(700, 70));
            _movesLabel = UiFactory.Label(movesRect, "", 44, _theme.TextColor, FontStyle.Normal);

            _prevButton = UiFactory.Button("Prev", panel, new Vector2(-400, 0), new Vector2(140, 140), "<", 80,
                _theme.ButtonColor, _theme.TextColor, () => LoadLevel(_levelIndex - 1));
            _nextButton = UiFactory.Button("Next", panel, new Vector2(400, 0), new Vector2(140, 140), ">", 80,
                _theme.ButtonColor, _theme.TextColor, () => LoadLevel(_levelIndex + 1));
        }

        private void BuildBoard(RectTransform parent)
        {
            RectTransform frame = UiFactory.Rect("BoardFrame", parent, new Vector2(0, BoardCenterY),
                Vector2.one * (BoardSize + BoardFrame * 2));
            UiFactory.Image(frame, _theme.BoardFrameColor);
            RectTransform inner = UiFactory.Rect("Board", frame, Vector2.zero, Vector2.one * BoardSize);
            UiFactory.Image(inner, _theme.BoardColor);

            _board = inner.gameObject.AddComponent<BoardView>();
            _board.Init(_theme, _config);
            _board.OnMoveRequested += OnMoveRequested;
        }

        private void BuildBottomButtons(RectTransform parent)
        {
            float step = ButtonSize.x + 30f;
            float x = -1.5f * step;
            UiFactory.Button("Pause", parent, new Vector2(x, ButtonRowY), ButtonSize, _loc.Get("game.pause"), 40,
                _theme.ButtonColor, _theme.TextColor, () => SetPaused(true));
            Button hint = UiFactory.Button("Hint", parent, new Vector2(x + step, ButtonRowY), ButtonSize, "", 40,
                _theme.ButtonColor, _theme.TextColor, OnHint);
            _hintLabel = hint.GetComponentInChildren<Text>();
            UiFactory.Button("Undo", parent, new Vector2(x + 2 * step, ButtonRowY), ButtonSize, _loc.Get("game.undo"), 40,
                _theme.ButtonColor, _theme.TextColor, OnUndo);
            UiFactory.Button("Restart", parent, new Vector2(x + 3 * step, ButtonRowY), ButtonSize, _loc.Get("game.restart"), 40,
                _theme.ButtonColor, _theme.TextColor, OnRestart);
        }

        private RectTransform BuildPauseOverlay(Transform parent)
        {
            RectTransform overlay = Overlay("PauseOverlay", parent, out RectTransform panel, 760);
            Title(panel, _loc.Get("pause.title"), 200);
            UiFactory.Button("Resume", panel, new Vector2(0, 20), new Vector2(560, 140), _loc.Get("pause.resume"), 52,
                _theme.ButtonColor, _theme.TextColor, () => SetPaused(false));
            UiFactory.Button("Restart", panel, new Vector2(0, -160), new Vector2(560, 140), _loc.Get("pause.restart"), 52,
                _theme.ButtonColor, _theme.TextColor, OnRestart);
            return overlay;
        }

        private RectTransform BuildSolvedOverlay(Transform parent)
        {
            RectTransform overlay = Overlay("SolvedOverlay", parent, out RectTransform panel, 860);
            Title(panel, _loc.Get("solved.title"), 260);
            RectTransform stars = UiFactory.Rect("Stars", panel, new Vector2(0, 110), new Vector2(760, 110));
            _solvedStars = UiFactory.Label(stars, "", 72, _theme.ExitColor);
            RectTransform moves = UiFactory.Rect("Moves", panel, new Vector2(0, 10), new Vector2(760, 80));
            _solvedMoves = UiFactory.Label(moves, "", 44, _theme.TextColor, FontStyle.Normal);
            UiFactory.Button("Next", panel, new Vector2(0, -140), new Vector2(560, 140), _loc.Get("solved.next"), 56,
                _theme.TargetColor, _theme.TextColor, () => LoadLevel(Mathf.Min(_levelIndex + 1, _levels.Count - 1)));
            UiFactory.Button("Replay", panel, new Vector2(0, -310), new Vector2(560, 120), _loc.Get("solved.replay"), 46,
                _theme.ButtonColor, _theme.TextColor, OnRestart);
            return overlay;
        }

        private RectTransform Overlay(string name, Transform parent, out RectTransform panel, float height)
        {
            RectTransform overlay = UiFactory.Stretch(name, parent);
            UiFactory.Image(overlay, new Color(0f, 0f, 0f, 0.6f), raycast: true);   // blocks touches behind
            panel = UiFactory.Rect("Panel", overlay, Vector2.zero, new Vector2(860, height));
            UiFactory.Image(panel, _theme.PanelColor);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private void Title(RectTransform panel, string text, float y)
        {
            RectTransform rt = UiFactory.Rect("Title", panel, new Vector2(0, y), new Vector2(760, 110));
            UiFactory.Label(rt, text, 64, _theme.TextColor);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static int LevelFromArgs()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--level" && int.TryParse(args[i + 1], out int n))
                    return n;
            return 1;
        }
    }
}
