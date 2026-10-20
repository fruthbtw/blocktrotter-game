using UnityEngine;
using UnityEngine.EventSystems;

namespace Blocktrotter.Gameplay
{
    /// <summary>
    /// One block on screen. Forwards drag input to its <see cref="BoardView"/> and
    /// eases toward the position the board gives it. Holds no game state.
    /// </summary>
    /// <example><code>blockView.Init(boardView, index); blockView.SetTarget(pos, snap: true);</code></example>
    public sealed class BlockView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BoardView _board;
        private RectTransform _rt;
        private CanvasGroup _group;
        private Vector2 _target;
        private float _targetAlpha = 1f;
        private bool _dragging;

        /// <summary>Index of this block in the board's block list.</summary>
        public int Index { get; private set; }

        /// <summary>True when the block has reached its target position.</summary>
        public bool AtTarget => (_rt.anchoredPosition - _target).sqrMagnitude < 0.25f;

        /// <summary>Binds the view to its board and block index.</summary>
        public void Init(BoardView board, int index)
        {
            _board = board;
            Index = index;
            _rt = (RectTransform)transform;
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Sets where the block eases to; <paramref name="snap"/> jumps there immediately.</summary>
        public void SetTarget(Vector2 position, bool snap = false, float alpha = 1f)
        {
            _target = position;
            _targetAlpha = alpha;
            if (snap)
            {
                _rt.anchoredPosition = position;
                _group.alpha = alpha;
            }
        }

        /// <summary>Places the block directly while it follows a finger.</summary>
        public void SetDragPosition(Vector2 position)
        {
            _dragging = true;
            _rt.anchoredPosition = position;
        }

        /// <summary>Ends finger-following; the block eases to its target again.</summary>
        public void ReleaseDrag() => _dragging = false;

        private void Update()
        {
            if (_dragging) return;
            float t = 1f - Mathf.Exp(-_board.SnapSharpness * Time.unscaledDeltaTime);
            _rt.anchoredPosition = AtTarget ? _target : Vector2.Lerp(_rt.anchoredPosition, _target, t);
            _group.alpha = Mathf.Lerp(_group.alpha, _targetAlpha, t);
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData e) => _board.BeginDrag(this, e);

        /// <inheritdoc/>
        public void OnDrag(PointerEventData e) => _board.Drag(this, e);

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData e) => _board.EndDrag(this, e);
    }
}
