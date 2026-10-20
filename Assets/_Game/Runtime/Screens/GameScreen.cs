using System.Collections;
using Blocktrotter.AppShell;
using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.Gameplay;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Screens
{
    /// <summary>
    /// The play screen: top panel, board, bottom buttons, pause menu and solved
    /// panel. Owns the <see cref="GameSession"/>; the board view only raises requests.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        // Layout in reference pixels (1080x1920 canvas).
        private const float BoardSize = 960f;
        private const float BoardFrame = 24f;
        private const float BoardCenterY = 20f;
        private const float TopPanelY = 700f;
        private static readonly Vector2 TopPanelSize = new Vector2(1000, 280);
        private const float ButtonRowY = -640f;
        private static readonly Vector2 ButtonSize = new Vector2(230, 150);

        private int _levelIndex;
        private GameSession _session;

        private BoardView _board;
        private Text _levelLabel, _movesLabel, _hintLabel;
        private Button _prevButton, _nextButton;
        private RectTransform _pauseOverlay, _solvedOverlay;
        private Text _solvedMoves;
        private Image[] _solvedStars;

        /// <summary>Opens the screen on a 0-based level index.</summary>
        public GameScreen(int levelIndex) => _levelIndex = levelIndex;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(App.WorldOf(_levelIndex));

        /// <inheritdoc/>
        protected override void Build()
        {
            if (_session == null) _session = new GameSession(App.Levels.LoadBoard(_levelIndex));
            BuildTopPanel();
            BuildBoard();
            BuildBottomButtons();
            _pauseOverlay = BuildPauseOverlay();
            _solvedOverlay = BuildSolvedOverlay();
            _board.Show(_session.Board);
            if (_session.IsSolved) _board.InputLocked = true;
            RefreshHud();
        }

        private void LoadLevel(int index)
        {
            if (!App.Progress.IsUnlocked(index)) return;
            if (App.WorldOf(index) != App.WorldOf(_levelIndex))
            {
                App.PlayLevel(index);   // new world: new theme, so a fresh screen
                return;
            }
            _levelIndex = index;
            _session = new GameSession(App.Levels.LoadBoard(index));
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
            {
                int stars = StarRating.Calculate(_session.MoveCount, Par, App.Config.TwoStarRatio);
                App.Progress.RecordSolve(_levelIndex, stars);
                App.SaveProgress();
                _board.PlayExit(() => _board.StartCoroutine(ShowSolvedAfterDelay(stars)));
            }
        }

        private IEnumerator ShowSolvedAfterDelay(int stars)
        {
            yield return new WaitForSecondsRealtime(App.Config.SolvedPanelDelay);
            _solvedMoves.text = T("solved.moves", _session.MoveCount, Par);
            UiFactory.SetStars(_solvedStars, stars, Theme.ExitColor, new Color(0f, 0f, 0f, 0.3f));
            _solvedOverlay.gameObject.SetActive(true);
            RefreshHud();
        }

        private int Par => App.Levels.Get(_levelIndex).MinMoves;

        private void OnHint()
        {
            if (_board.InputLocked || !_session.UseHint(App.Hints).HasValue) return;
            App.SaveProgress();
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
            _board.InputLocked = paused || _session.IsSolved;
        }

        private void RefreshHud()
        {
            _levelLabel.text = T("game.level", _levelIndex + 1);
            _movesLabel.text = T("game.moves", _session.MoveCount, Par);
            _hintLabel.text = T("game.hint", App.Hints.Count);
            _prevButton.interactable = _levelIndex > 0;
            _nextButton.interactable = _levelIndex + 1 < App.Levels.Count && App.Progress.IsUnlocked(_levelIndex + 1);
        }

        // ---------------------------------------------------------------- UI

        private void BuildTopPanel()
        {
            RectTransform panel = UiFactory.Rect("TopPanel", Root, new Vector2(0, TopPanelY), TopPanelSize);
            UiFactory.Image(panel, Theme.PanelColor);

            RectTransform levelRect = UiFactory.Rect("Level", panel, new Vector2(0, 40), new Vector2(600, 120));
            _levelLabel = UiFactory.Label(levelRect, "", 84, Theme.TextColor);
            RectTransform movesRect = UiFactory.Rect("Moves", panel, new Vector2(0, -70), new Vector2(700, 70));
            _movesLabel = UiFactory.Label(movesRect, "", 44, Theme.TextColor, FontStyle.Normal);

            _prevButton = ThemedButton("Prev", panel, new Vector2(-400, 0), new Vector2(140, 140), "<", 80,
                () => LoadLevel(_levelIndex - 1));
            _nextButton = ThemedButton("Next", panel, new Vector2(400, 0), new Vector2(140, 140), ">", 80,
                () => LoadLevel(_levelIndex + 1));
        }

        private void BuildBoard()
        {
            RectTransform frame = UiFactory.Rect("BoardFrame", Root, new Vector2(0, BoardCenterY),
                Vector2.one * (BoardSize + BoardFrame * 2));
            UiFactory.Image(frame, Theme.BoardFrameColor);
            RectTransform inner = UiFactory.Rect("Board", frame, Vector2.zero, Vector2.one * BoardSize);
            UiFactory.Image(inner, Theme.BoardColor);

            _board = inner.gameObject.AddComponent<BoardView>();
            _board.Init(Theme, App.Config);
            _board.OnMoveRequested += OnMoveRequested;
        }

        private void BuildBottomButtons()
        {
            float step = ButtonSize.x + 30f;
            float x = -1.5f * step;
            ThemedButton("Pause", Root, new Vector2(x, ButtonRowY), ButtonSize, T("game.pause"), 40, () => SetPaused(true));
            Button hint = ThemedButton("Hint", Root, new Vector2(x + step, ButtonRowY), ButtonSize, "", 40, OnHint);
            _hintLabel = hint.GetComponentInChildren<Text>();
            ThemedButton("Undo", Root, new Vector2(x + 2 * step, ButtonRowY), ButtonSize, T("game.undo"), 40, OnUndo);
            ThemedButton("Restart", Root, new Vector2(x + 3 * step, ButtonRowY), ButtonSize, T("game.restart"), 40, OnRestart);
        }

        private RectTransform BuildPauseOverlay()
        {
            RectTransform overlay = Overlay("PauseOverlay", out RectTransform panel, 980);
            Title(panel, T("pause.title"), 360);
            ThemedButton("Resume", panel, new Vector2(0, 170), new Vector2(560, 140), T("pause.resume"), 52,
                () => SetPaused(false), primary: true);
            ThemedButton("Restart", panel, new Vector2(0, 0), new Vector2(560, 140), T("pause.restart"), 52, OnRestart);
            ThemedButton("Levels", panel, new Vector2(0, -170), new Vector2(560, 140), T("pause.levels"), 52,
                () => App.ShowLevels(App.WorldOf(_levelIndex)));
            ThemedButton("Menu", panel, new Vector2(0, -340), new Vector2(560, 140), T("pause.menu"), 52, App.ShowMainMenu);
            return overlay;
        }

        private RectTransform BuildSolvedOverlay()
        {
            RectTransform overlay = Overlay("SolvedOverlay", out RectTransform panel, 900);
            Title(panel, T("solved.title"), 330);
            _solvedStars = UiFactory.StarRow(panel, new Vector2(0, 170), 130f, 20f);
            RectTransform moves = UiFactory.Rect("Moves", panel, new Vector2(0, 40), new Vector2(760, 80));
            _solvedMoves = UiFactory.Label(moves, "", 44, Theme.TextColor, FontStyle.Normal);
            ThemedButton("Next", panel, new Vector2(0, -120), new Vector2(560, 140), T("solved.next"), 56,
                () => LoadLevel(Mathf.Min(_levelIndex + 1, App.Levels.Count - 1)), primary: true);
            ThemedButton("Replay", panel, new Vector2(0, -290), new Vector2(560, 120), T("solved.replay"), 46, OnRestart);
            return overlay;
        }

        private RectTransform Overlay(string name, out RectTransform panel, float height)
        {
            RectTransform overlay = UiFactory.Stretch(name, Root);
            UiFactory.Dim(overlay, new Color(0f, 0f, 0f, 0.6f));
            panel = UiFactory.Rect("Panel", overlay, Vector2.zero, new Vector2(860, height));
            UiFactory.Image(panel, Theme.PanelColor);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private void Title(RectTransform panel, string text, float y)
        {
            RectTransform rt = UiFactory.Rect("Title", panel, new Vector2(0, y), new Vector2(760, 110));
            UiFactory.Label(rt, text, 64, Theme.TextColor);
        }
    }
}
