using System.Collections;
using Blocktrotter.AppShell;
using Blocktrotter.Audio;
using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.Gameplay;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Screens
{
    /// <summary>
    /// The guided first puzzle (<see cref="TutorialScript"/>): a welcome card, then one
    /// highlighted block per step with an animated touch pointer showing the drag, then
    /// "You're ready!" and on to level 1. Shown on first launch and from Settings.
    /// </summary>
    public sealed class TutorialScreen : UiScreen
    {
        private const float BoardSize = 960f;
        private const float BoardFrame = 24f;
        private const float BoardCenterY = 20f;
        private const float PointerSeconds = 1.3f;

        private readonly bool _showWelcome;
        private GameSession _session;
        private int _step;

        private BoardView _board;
        private Text _instruction;
        private RectTransform _pointer, _welcome, _done;
        private CanvasGroup _pointerGroup;
        private Coroutine _pointerLoop;

        /// <summary>Creates the tutorial; <paramref name="showWelcome"/> opens with the Start/Skip card.</summary>
        public TutorialScreen(bool showWelcome) => _showWelcome = showWelcome;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(0);

        /// <inheritdoc/>
        protected override void Build()
        {
            _session = new GameSession(TutorialScript.CreateBoard());
            _step = 0;

            RectTransform panel = UiFactory.Rect("Instruction", Root, new Vector2(0, 700), new Vector2(1000, 280));
            UiFactory.Image(panel, Theme.PanelColor);
            RectTransform text = UiFactory.Rect("Text", panel, Vector2.zero, new Vector2(920, 240));
            _instruction = UiFactory.Label(text, "", 48, Theme.TextColor);

            BuildBoard();
            ThemedButton("Skip", Root, new Vector2(0, -640), new Vector2(420, 130), T("tutorial.skip"), 46, Finish);

            _welcome = BuildCard("Welcome", T("tutorial.welcome.title"), T("tutorial.welcome.body"),
                T("tutorial.start"), BeginSteps, T("tutorial.skip"), Finish);
            _done = BuildCard("Done", T("tutorial.done.title"), T("tutorial.done.body"),
                T("tutorial.done.play"), PlayFirstLevel, null, null);
            _done.gameObject.SetActive(false);

            if (_showWelcome) _board.InputLocked = true;
            else
            {
                _welcome.gameObject.SetActive(false);
                BeginSteps();
            }
        }

        private void BuildBoard()
        {
            RectTransform frame = UiFactory.Rect("BoardFrame", Root, new Vector2(0, BoardCenterY), Vector2.one * (BoardSize + BoardFrame * 2));
            UiFactory.Image(frame, Theme.BoardFrameColor);
            RectTransform inner = UiFactory.Rect("Board", frame, Vector2.zero, Vector2.one * BoardSize);
            Image boardImage = UiFactory.Image(inner, Color.white, BlockSkins.Board(Theme, Board.DefaultSize));
            boardImage.type = Image.Type.Simple;

            _board = inner.gameObject.AddComponent<BoardView>();
            _board.Init(Theme, App.Config, BoardFrame);
            _board.OnMoveRequested += OnMoveRequested;
            _board.OnExitLaunch += () => App.Sound.Play(Sfx.TargetExit);
            _board.Show(_session.Board);

            // Touch pointer: a filled dot inside a ring, drawn above the blocks.
            _pointer = UiFactory.Rect("Pointer", inner, Vector2.zero, Vector2.one * 110f);
            _pointer.anchorMin = _pointer.anchorMax = new Vector2(0f, 1f);
            UiFactory.Image(_pointer, new Color(1f, 1f, 1f, 0.35f), ProceduralSprites.Circle).raycastTarget = false;
            RectTransform dot = UiFactory.Rect("Dot", _pointer, Vector2.zero, Vector2.one * 56f);
            UiFactory.Image(dot, Color.white, ProceduralSprites.Circle).raycastTarget = false;
            _pointerGroup = _pointer.gameObject.AddComponent<CanvasGroup>();
            _pointerGroup.blocksRaycasts = false;
            _pointer.gameObject.SetActive(false);
        }

        private void BeginSteps()
        {
            _welcome.gameObject.SetActive(false);
            _board.InputLocked = false;
            ShowStep();
        }

        private void ShowStep()
        {
            TutorialStep step = TutorialScript.Steps[_step];
            _instruction.text = T(step.TextKey);
            _board.FocusBlock = step.BlockIndex;
            if (_pointerLoop != null) _board.StopCoroutine(_pointerLoop);
            _pointerLoop = _board.StartCoroutine(AnimatePointer(step));
        }

        /// <summary>Slides the pointer from the block's centre along the suggested drag, over and over.</summary>
        private IEnumerator AnimatePointer(TutorialStep step)
        {
            _pointer.gameObject.SetActive(true);
            _pointer.SetAsLastSibling();
            Block block = _session.Board.Blocks[step.BlockIndex];
            float cell = _board.CellSize;
            Vector2 offset = block.Orientation == Orientation.Horizontal
                ? new Vector2(step.SuggestedDelta * cell, 0f)
                : new Vector2(0f, -step.SuggestedDelta * cell);

            while (true)
            {
                RectTransform rt = _board.BlockRect(step.BlockIndex);
                Vector2 centre = rt.anchoredPosition + new Vector2(rt.sizeDelta.x * 0.5f, -rt.sizeDelta.y * 0.5f);
                for (float t = 0f; t < PointerSeconds; t += Time.unscaledDeltaTime)
                {
                    float k = Mathf.Clamp01(t / (PointerSeconds * 0.75f));
                    float eased = k * k * (3f - 2f * k);
                    _pointer.anchoredPosition = centre + offset * eased;
                    _pointerGroup.alpha = t < 0.15f ? t / 0.15f : Mathf.Clamp01((PointerSeconds - t) / 0.25f);
                    _pointer.localScale = Vector3.one * (t < 0.15f ? 1.15f - t : 1f);   // a little "press" at the start
                    yield return null;
                }
            }
        }

        private void OnMoveRequested(Move move)
        {
            if (!_session.TryMove(move))
            {
                _board.Sync(_session.Board);
                return;
            }
            App.Sound.Play(Sfx.BlockSnap);
            _board.Sync(_session.Board);

            while (_step < TutorialScript.Steps.Count && TutorialScript.Steps[_step].IsDone(_session.Board))
                _step++;

            if (_step < TutorialScript.Steps.Count)
            {
                ShowStep();
                return;
            }

            // Solved: the red block's own exit, confetti, then "You're ready!".
            if (_pointerLoop != null) _board.StopCoroutine(_pointerLoop);
            _pointer.gameObject.SetActive(false);
            _board.FocusBlock = -1;
            _instruction.text = T("tutorial.done.title");
            _board.PlayExit(() => _board.StartCoroutine(Celebrate()));
        }

        private IEnumerator Celebrate()
        {
            App.Sound.Play(Sfx.LevelSolved);
            ConfettiBurst.Spawn(Root, new Vector2(0, BoardCenterY - BoardSize / 2f), App.Config.ConfettiCount,
                App.Config.ConfettiDuration, new[] { Theme.TargetColor, Theme.ExitColor, Theme.BlockColor, Color.white });
            yield return new WaitForSecondsRealtime(App.Config.SolvedPanelDelay);
            if (Root != null) _done.gameObject.SetActive(true);
        }

        private void MarkDone()
        {
            App.Progress.TutorialDone = true;
            App.SaveProgress();
        }

        private void Finish()
        {
            MarkDone();
            App.ShowMainMenu();
        }

        private void PlayFirstLevel()
        {
            MarkDone();
            App.PlayLevel(App.Progress.FrontierLevel);
        }

        /// <summary>Centred card over a dimmed screen: title, body, a primary button and an optional second one.</summary>
        private RectTransform BuildCard(string name, string title, string body, string primary,
            UnityEngine.Events.UnityAction onPrimary, string secondary, UnityEngine.Events.UnityAction onSecondary)
        {
            RectTransform overlay = UiFactory.Stretch(name, Root);
            UiFactory.Dim(overlay, new Color(0f, 0f, 0f, 0.6f));
            RectTransform card = UiFactory.Rect("Card", overlay, Vector2.zero, new Vector2(880, secondary != null ? 820 : 700));
            UiFactory.Image(card, Theme.PanelColor);
            float top = secondary != null ? 300 : 240;

            UiFactory.Label(UiFactory.Rect("Title", card, new Vector2(0, top), new Vector2(800, 110)), title, 62, Theme.ExitColor);
            UiFactory.Label(UiFactory.Rect("Body", card, new Vector2(0, top - 180), new Vector2(780, 220)), body, 44, Theme.TextColor, FontStyle.Normal);
            ThemedButton("Primary", card, new Vector2(0, top - 380), new Vector2(560, 140), primary, 54, onPrimary, primary: true);
            if (secondary != null)
                ThemedButton("Secondary", card, new Vector2(0, top - 540), new Vector2(560, 110), secondary, 44, onSecondary);
            return overlay;
        }
    }
}
