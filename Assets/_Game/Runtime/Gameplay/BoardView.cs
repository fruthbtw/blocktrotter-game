using System;
using System.Collections;
using System.Collections.Generic;
using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blocktrotter.Gameplay
{
    /// <summary>
    /// Draws a <see cref="Board"/> with the world's block skins and turns finger drags
    /// into move requests. It never changes game state: it raises
    /// <see cref="OnMoveRequested"/> and the owner answers by calling <see cref="Sync"/>.
    /// The red block gets its own exit: a wind-up, a stretched dash with a trail, and
    /// a flash at the gate.
    /// </summary>
    /// <example>
    /// <code>
    /// boardView.Init(theme, config, frameThickness: 24f);
    /// boardView.OnMoveRequested += m =&gt; { session.TryMove(m); boardView.Sync(session.Board); };
    /// boardView.Show(session.Board);
    /// </code>
    /// </example>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        private const float BlockPadding = 6f;
        private const float ShadowOffset = 8f;
        private const float NeonGlowSpread = 26f;

        private readonly List<BlockView> _blocks = new List<BlockView>();
        private RectTransform _rt;
        private ThemeDefinition _theme;
        private GameConfig _config;
        private float _frameThickness;
        private Board _board;
        private RectTransform _gate;
        private GateFx _gateFx;

        private BlockView _active;
        private int _activePointer;
        private Vector2 _dragStartLocal;
        private Vector2 _dragStartPosition;
        private int _rangeBack, _rangeForward;
        private Coroutine _exitRoutine;

        /// <summary>Raised when a drag ends at least one cell away from where it started.</summary>
        public event Action<Move> OnMoveRequested;

        /// <summary>Raised when the red block launches toward the gate (after its wind-up).</summary>
        public event Action OnExitLaunch;

        /// <summary>Ease speed used by blocks (from GameConfig).</summary>
        public float SnapSharpness { get; private set; } = 20f;

        /// <summary>When true, drags are ignored (panels open, exit animation playing).</summary>
        public bool InputLocked { get; set; }

        private float Cell => _rt.rect.width / _board.Size;

        /// <summary>Sets theme, feel values and the width of the frame the exit gate cuts through.</summary>
        public void Init(ThemeDefinition theme, GameConfig config, float frameThickness)
        {
            _rt = (RectTransform)transform;
            _theme = theme;
            _config = config;
            _frameThickness = frameThickness;
            SnapSharpness = config.BlockSnapSharpness;
        }

        /// <summary>Rebuilds all block views for a new level and places them instantly.</summary>
        public void Show(Board board)
        {
            if (_exitRoutine != null) StopCoroutine(_exitRoutine);
            _exitRoutine = null;
            foreach (BlockView b in _blocks) Destroy(b.gameObject);
            _blocks.Clear();
            for (int i = _rt.childCount - 1; i >= 0; i--)
                if (_rt.GetChild(i).GetComponent<TrailGhost>() != null) Destroy(_rt.GetChild(i).gameObject);
            _board = board;
            _active = null;
            InputLocked = false;
            BuildGate();

            for (int i = 0; i < board.Blocks.Count; i++)
                _blocks.Add(CreateBlock(board.Blocks[i], i));
            Sync(board, snap: true);
        }

        /// <summary>Moves every block view to match <paramref name="board"/>.</summary>
        public void Sync(Board board, bool snap = false)
        {
            _board = board;
            for (int i = 0; i < _blocks.Count; i++)
                _blocks[i].SetTarget(CellPosition(board.Blocks[i]), snap);
        }

        /// <summary>Plays the red block's exit, then calls <paramref name="onDone"/>.</summary>
        public void PlayExit(Action onDone)
        {
            InputLocked = true;
            _exitRoutine = StartCoroutine(ExitSequence(onDone));
        }

        /// <summary>
        /// Wind-up (pull back, squash) → launch (accelerate, stretch, leave a trail) →
        /// gate flash as it passes → fade out beyond the board.
        /// </summary>
        private IEnumerator ExitSequence(Action onDone)
        {
            BlockView view = _blocks[_board.TargetIndex];
            Block target = _board.Blocks[_board.TargetIndex];
            var root = (RectTransform)view.transform;
            Vector2 home = CellPosition(target);
            Vector2 size = root.sizeDelta;

            // Wait for the last slide to settle so the wind-up starts from the right place.
            float settle = 0f;
            while (!view.AtTarget && settle < 0.3f) { settle += Time.unscaledDeltaTime; yield return null; }

            // 1. Wind-up: ease back a fifth of a cell while squashing.
            for (float t = 0f; t < _config.ExitWindupSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / _config.ExitWindupSeconds * Mathf.PI * 0.5f);
                Place(view, root, home + new Vector2(-Cell * 0.2f * k, 0f), size, new Vector2(1f - 0.08f * k, 1f + 0.1f * k));
                yield return null;
            }

            // 2. Launch: accelerate past the gate, stretched, dropping trail copies.
            OnExitLaunch?.Invoke();
            Vector2 from = home + new Vector2(-Cell * 0.2f, 0f);
            Vector2 to = home + new Vector2(Cell * (target.Length + 1.2f), 0f);
            float gateX = _rt.rect.width;
            bool flashed = false;
            float nextGhost = 0f;
            for (float t = 0f; t < _config.ExitLaunchSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / _config.ExitLaunchSeconds;
                float eased = k * k * (2.2f - 1.2f * k);                       // quick ease-in, slight ease-out
                Vector2 pos = Vector2.LerpUnclamped(from, to, eased);
                float stretch = 1f + 0.3f * Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI);
                Place(view, root, pos, size, new Vector2(stretch, 1f / Mathf.Sqrt(stretch)));

                if (t >= nextGhost)
                {
                    SpawnGhost(view, root);
                    nextGhost = t + _config.ExitTrailInterval;
                }
                if (!flashed && pos.x + size.x * 0.6f >= gateX)
                {
                    flashed = true;
                    _gateFx.Flash();
                }
                view.SetAlpha(Mathf.Clamp01(1f - (pos.x - gateX) / (Cell * 1.2f)));
                yield return null;
            }
            view.SetAlpha(0f);
            if (!flashed) _gateFx.Flash();
            _exitRoutine = null;
            onDone?.Invoke();
        }

        /// <summary>Positions and scales a block about its centre (its pivot is the top-left corner).</summary>
        private static void Place(BlockView view, RectTransform root, Vector2 topLeft, Vector2 size, Vector2 scale)
        {
            root.localScale = new Vector3(scale.x, scale.y, 1f);
            view.SetDragPosition(topLeft + new Vector2((1f - scale.x) * size.x * 0.5f, -(1f - scale.y) * size.y * 0.5f));
        }

        private void SpawnGhost(BlockView view, RectTransform root)
        {
            Image face = view.Face;
            RectTransform ghost = UiFactory.Rect("TrailGhost", _rt, Vector2.zero, root.sizeDelta);
            ghost.anchorMin = ghost.anchorMax = ghost.pivot = new Vector2(0f, 1f);
            ghost.anchoredPosition = root.anchoredPosition;
            ghost.localScale = root.localScale;
            ghost.SetSiblingIndex(root.GetSiblingIndex());   // behind the block
            Image img = UiFactory.Image(ghost, new Color(1f, 1f, 1f, 0.45f), face.sprite);
            img.type = Image.Type.Simple;
            ghost.gameObject.AddComponent<TrailGhost>().Init(_config.ExitTrailFade);
        }

        internal void BeginDrag(BlockView view, PointerEventData e)
        {
            if (InputLocked || _active != null || _board.IsSolved) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out _dragStartLocal)) return;
            _active = view;
            _activePointer = e.pointerId;
            _dragStartPosition = CellPosition(_board.Blocks[view.Index]);
            (_rangeBack, _rangeForward) = _board.GetMoveRange(view.Index);
        }

        internal void Drag(BlockView view, PointerEventData e)
        {
            if (view != _active || e.pointerId != _activePointer) return;
            view.SetDragPosition(_dragStartPosition + AxisOffset(view, e));
        }

        internal void EndDrag(BlockView view, PointerEventData e)
        {
            if (view != _active || e.pointerId != _activePointer) return;
            Vector2 offset = AxisOffset(view, e);
            _active = null;
            view.ReleaseDrag();

            Block block = _board.Blocks[view.Index];
            float along = block.Orientation == Orientation.Horizontal ? offset.x : -offset.y;
            int cells = Mathf.RoundToInt(along / Cell);
            if (cells != 0)
                OnMoveRequested?.Invoke(new Move(view.Index, cells));
            else
                Sync(_board);
        }

        /// <summary>Finger offset projected on the block's axis and clamped to free cells.</summary>
        private Vector2 AxisOffset(BlockView view, PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out Vector2 local);
            Vector2 delta = local - _dragStartLocal;
            float min = -_rangeBack * Cell, max = _rangeForward * Cell;
            return _board.Blocks[view.Index].Orientation == Orientation.Horizontal
                ? new Vector2(Mathf.Clamp(delta.x, min, max), 0f)
                : new Vector2(0f, -Mathf.Clamp(-delta.y, min, max));   // rows grow downward, UI y grows upward
        }

        private Vector2 CellPosition(Block b) => new Vector2(b.Col * Cell + BlockPadding, -(b.Row * Cell + BlockPadding));

        private BlockView CreateBlock(Block block, int index)
        {
            Vector2 size = block.Orientation == Orientation.Horizontal
                ? new Vector2(block.Length * Cell, Cell)
                : new Vector2(Cell, block.Length * Cell);
            size -= Vector2.one * (BlockPadding * 2f);
            bool neon = _theme.BlockStyle == BlockStyle.Neon;

            // Root moves; shadow/glow and face are children so they move together.
            RectTransform root = TopLeft($"Block{index}", _rt, size);
            Image glow = null;
            if (neon)
            {
                RectTransform halo = TopLeft("Glow", root, size + Vector2.one * NeonGlowSpread * 2f);
                halo.anchoredPosition = new Vector2(-NeonGlowSpread, NeonGlowSpread);
                Color edge = block.IsTarget ? _theme.TargetDetailColor : _theme.BlockDetailColor;
                glow = UiFactory.Image(halo, new Color(edge.r, edge.g, edge.b, block.IsTarget ? 0.6f : 0.35f), ProceduralSprites.SoftGlow);
                glow.type = Image.Type.Sliced;
            }
            else
            {
                RectTransform shadow = TopLeft("Shadow", root, size);
                shadow.anchoredPosition = new Vector2(0f, -ShadowOffset);
                Color shade = _theme.BlockShadowColor;
                UiFactory.Image(shadow, new Color(shade.r, shade.g, shade.b, 0.55f));
            }

            RectTransform face = TopLeft("Face", root, size);
            Image faceImage = UiFactory.Image(face, Color.white,
                BlockSkins.Block(_theme, block.Length, block.Orientation, block.IsTarget), raycast: true);
            faceImage.type = Image.Type.Simple;

            var view = root.gameObject.AddComponent<BlockView>();
            view.Init(this, index, faceImage);
            face.gameObject.AddComponent<DragForwarder>().Target = view;

            if (block.IsTarget) AddTargetIdleFx(root, face, faceImage, glow);
            return view;
        }

        /// <summary>Clips a soft light band to the red block's shape and sweeps it across now and then.</summary>
        private void AddTargetIdleFx(RectTransform root, RectTransform face, Image faceImage, Image glow)
        {
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform shine = UiFactory.Rect("Shine", face, Vector2.zero, new Vector2(Cell * 0.35f, face.sizeDelta.y * 2.2f));
            shine.anchorMin = shine.anchorMax = new Vector2(0f, 0.5f);
            shine.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Image band = UiFactory.Image(shine, new Color(1f, 1f, 1f, 0.38f), ProceduralSprites.SoftGlow);
            band.type = Image.Type.Sliced;
            root.gameObject.AddComponent<TargetIdleFx>().Init(face, shine, glow, _config.TargetShinePeriod);
        }

        /// <summary>
        /// The exit as a gate: an opening cut through the frame on the exit row, two posts,
        /// and chevrons pointing out that pulse in sequence.
        /// </summary>
        private void BuildGate()
        {
            if (_gate != null) Destroy(_gate.gameObject);
            float cell = Cell;
            float h = cell - BlockPadding * 2f;
            float top = -(_board.ExitRow * cell + BlockPadding);
            float x = _rt.rect.width;

            _gate = TopLeft("Gate", _rt, new Vector2(_frameThickness + 4f, h));
            _gate.anchoredPosition = new Vector2(x - 2f, top);
            UiFactory.Image(_gate, _theme.BoardColor).sprite = null;          // opening in the frame

            float postH = _frameThickness * 0.9f;
            foreach (float y in new[] { postH, -h })   // one post just above the opening, one just below
            {
                RectTransform post = TopLeft("Post", _gate, new Vector2(_frameThickness + 18f, postH));
                post.anchoredPosition = new Vector2(-6f, y);
                UiFactory.Image(post, _theme.ExitColor);
            }

            var chevrons = new Image[2];
            for (int i = 0; i < chevrons.Length; i++)
            {
                RectTransform ch = UiFactory.Rect($"Chevron{i}", _gate, Vector2.zero, new Vector2(h * 0.32f, h * 0.32f));
                ch.anchorMin = ch.anchorMax = new Vector2(0f, 0.5f);
                ch.anchoredPosition = new Vector2(_frameThickness * 0.15f + i * h * 0.26f, 0f);
                ch.localRotation = Quaternion.Euler(0f, 0f, -90f);           // triangle points right
                chevrons[i] = UiFactory.Image(ch, _theme.ExitColor, ProceduralSprites.Triangle);
                chevrons[i].type = Image.Type.Simple;
            }

            RectTransform flash = UiFactory.Rect("Flash", _gate, Vector2.zero, new Vector2(h * 1.3f, h * 1.3f));
            flash.anchorMin = flash.anchorMax = new Vector2(0.3f, 0.5f);
            Image flashImage = UiFactory.Image(flash, Color.white, ProceduralSprites.SoftGlow);
            flashImage.type = Image.Type.Sliced;

            _gateFx = _gate.gameObject.AddComponent<GateFx>();
            _gateFx.Init(chevrons, flashImage, _theme.ExitColor);
        }

        private static RectTransform TopLeft(string name, Transform parent, Vector2 size)
        {
            RectTransform rt = UiFactory.Rect(name, parent, Vector2.zero, size);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        /// <summary>Forwards drag events from a block's face (the raycast target) to its moving root.</summary>
        private sealed class DragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public BlockView Target;
            public void OnBeginDrag(PointerEventData e) => Target.OnBeginDrag(e);
            public void OnDrag(PointerEventData e) => Target.OnDrag(e);
            public void OnEndDrag(PointerEventData e) => Target.OnEndDrag(e);
        }
    }
}
