using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blocktrotter.UI
{
    /// <summary>
    /// "Chunky" press feel for factory buttons: the face sits on a darker lip and
    /// sinks into it while held, with a small squash of the whole button. A
    /// non-interactable button drops its face onto the lip and hides it, so it
    /// reads as flat. Built by <see cref="UiFactory"/>; not meant to be added by hand.
    /// </summary>
    public sealed class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, IPointerEnterHandler
    {
        private const float Speed = 28f;          // how fast the face follows the finger (1/s)
        private const float SinkFraction = 0.75f; // share of the lip the face sinks into when pressed
        private const float PressedScale = 0.97f;

        private Button _button;
        private RectTransform _face;
        private Graphic _lip;
        private Graphic _gloss;
        private float _lipDepth;
        private bool _held;
        private bool _inside;
        private float _t;   // 0 = up, 1 = pressed

        /// <summary>Wires the parts made by <see cref="UiFactory"/>.</summary>
        public void Init(Button button, RectTransform face, Graphic lip, Graphic gloss, float lipDepth)
        {
            _button = button;
            _face = face;
            _lip = lip;
            _gloss = gloss;
            _lipDepth = lipDepth;
            Apply(0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _held = true;
            _inside = true;
        }

        public void OnPointerUp(PointerEventData eventData) => _held = false;
        public void OnPointerExit(PointerEventData eventData) => _inside = false;
        public void OnPointerEnter(PointerEventData eventData) => _inside = true;

        private void OnDisable()
        {
            _held = false;
            _t = 0f;
            if (_face != null) Apply(0f);
        }

        private void Update()
        {
            if (_button == null) return;
            bool interactable = _button.IsInteractable();
            bool changed = _lip.enabled != interactable;
            if (changed) _lip.enabled = _gloss.enabled = interactable;

            float target = !interactable ? 1f : (_held && _inside ? 1f : 0f);
            if (!changed && Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, Speed * Time.unscaledDeltaTime);
            Apply(_t);
        }

        private void Apply(float t)
        {
            float sink = _lipDepth * SinkFraction * t;
            _face.offsetMin = new Vector2(0f, _lipDepth - sink);
            _face.offsetMax = new Vector2(0f, -sink);
            bool interactable = _button == null || _button.IsInteractable();
            float scale = interactable ? Mathf.Lerp(1f, PressedScale, t) : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
