using System;
using System.Collections.Generic;
using Blocktrotter.Core;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blocktrotter.Gameplay
{
    /// <summary>
    /// Draws a <see cref="Board"/> and turns finger drags into move requests. It
    /// never changes game state: it raises <see cref="OnMoveRequested"/> and the
    /// owner answers by calling <see cref="Sync"/> with the resulting board.
    /// </summary>
    /// <example>
    /// <code>
    /// boardView.Init(theme, config);
    /// boardView.OnMoveRequested += m =&gt; { session.TryMove(m); boardView.Sync(session.Board); };
    /// boardView.Show(session.Board);
    /// </code>
    /// </example>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        private const float BlockPadding = 6f;
        private const float ShadowOffset = 8f;

        private readonly List<BlockView> _blocks = new List<BlockView>();
        private RectTransform _rt;
        private ThemeDefinition _theme;
        private Board _board;
        private RectTransform _exitMarker;

        private BlockView _active;
        private int _activePointer;
        private Vector2 _dragStartLocal;
        private Vector2 _dragStartPosition;
        private int _rangeBack, _rangeForward;

        private Action _exitDone;
        private BlockView _exiting;

        /// <summary>Raised when a drag ends at least one cell away from where it started.</summary>
        public event Action<Move> OnMoveRequested;

        /// <summary>Ease speed used by blocks (from GameConfig).</summary>
        public float SnapSharpness { get; private set; } = 20f;

        /// <summary>When true, drags are ignored (panels open, exit animation playing).</summary>
        public bool InputLocked { get; set; }

        private float Cell => _rt.rect.width / _board.Size;

        /// <summary>Sets colours and feel values. Call once before <see cref="Show"/>.</summary>
        public void Init(ThemeDefinition theme, GameConfig config)
        {
            _rt = (RectTransform)transform;
            _theme = theme;
            SnapSharpness = config.BlockSnapSharpness;
        }

        /// <summary>Rebuilds all block views for a new level and places them instantly.</summary>
        public void Show(Board board)
        {
            foreach (BlockView b in _blocks) Destroy(b.gameObject);
            _blocks.Clear();
            _board = board;
            _active = null;
            _exiting = null;
            _exitDone = null;
            InputLocked = false;
            BuildExitMarker();

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

        /// <summary>Slides the target block out through the exit, then calls <paramref name="onDone"/>.</summary>
        public void PlayExit(Action onDone)
        {
            InputLocked = true;
            _exiting = _blocks[_board.TargetIndex];
            Block target = _board.Blocks[_board.TargetIndex];
            Vector2 outside = CellPosition(target) + new Vector2(Cell * (target.Length + 0.5f), 0f);
            _exiting.SetTarget(outside, alpha: 0f);
            _exitDone = onDone;
        }

        private void Update()
        {
            if (_exiting == null || !_exiting.AtTarget) return;
            Action done = _exitDone;
            _exiting = null;
            _exitDone = null;
            done?.Invoke();
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

            // Root moves; shadow and face are children so they move together.
            RectTransform root = TopLeft($"Block{index}", _rt, size);
            RectTransform shadow = TopLeft("Shadow", root, size);
            shadow.anchoredPosition = new Vector2(0f, -ShadowOffset);
            UiFactory.Image(shadow, _theme.BlockShadowColor);

            RectTransform face = TopLeft("Face", root, size);
            UiFactory.Image(face, block.IsTarget ? _theme.TargetColor : _theme.BlockColor, raycast: true);

            var view = root.gameObject.AddComponent<BlockView>();
            view.Init(this, index);
            face.gameObject.AddComponent<DragForwarder>().Target = view;
            return view;
        }

        private void BuildExitMarker()
        {
            if (_exitMarker != null) Destroy(_exitMarker.gameObject);
            float cell = Cell;
            _exitMarker = TopLeft("Exit", _rt, new Vector2(cell * 0.18f, cell - BlockPadding * 2f));
            _exitMarker.anchoredPosition = new Vector2(_rt.rect.width + cell * 0.06f, -(_board.ExitRow * cell + BlockPadding));
            UiFactory.Image(_exitMarker, _theme.ExitColor);
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
