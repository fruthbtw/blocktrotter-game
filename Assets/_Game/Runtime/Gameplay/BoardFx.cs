using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Gameplay
{
    /// <summary>
    /// Idle life on the red block: a light sweep across it every few seconds and, on
    /// neon worlds, a pulsing glow. Purely visual; uses unscaled time.
    /// </summary>
    public sealed class TargetIdleFx : MonoBehaviour
    {
        private const float SweepSeconds = 0.7f;

        private RectTransform _shine, _face;
        private Image _glow;
        private float _period, _clock;

        /// <summary>Sweeps <paramref name="shine"/> across <paramref name="face"/>; pulses <paramref name="glow"/> if given.</summary>
        public void Init(RectTransform face, RectTransform shine, Image glow, float period)
        {
            _face = face;
            _shine = shine;
            _glow = glow;
            _period = period;
            _clock = period * 0.5f;   // first sweep soon after the level appears
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _clock += dt;
            float w = _face.rect.width;
            float t = Mathf.Repeat(_clock, _period) / SweepSeconds;
            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.4f, w * 1.4f, Mathf.Clamp01(t)), _shine.anchoredPosition.y);
            _shine.gameObject.SetActive(t <= 1f);
            if (_glow != null)
            {
                Color c = _glow.color;
                c.a = 0.45f + 0.25f * Mathf.Sin(_clock * 3.2f);
                _glow.color = c;
            }
        }
    }

    /// <summary>A fading copy of the red block left behind while it races to the exit.</summary>
    public sealed class TrailGhost : MonoBehaviour
    {
        private Image _image;
        private float _life, _age, _startAlpha;

        /// <summary>Starts fading from the image's current alpha over <paramref name="life"/> seconds.</summary>
        public void Init(float life)
        {
            _image = GetComponent<Image>();
            _life = life;
            _startAlpha = _image.color.a;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            Color c = _image.color;
            c.a = _startAlpha * Mathf.Clamp01(1f - _age / _life);
            _image.color = c;
            if (_age >= _life) Destroy(gameObject);
        }
    }

    /// <summary>The exit gate's chevrons: a gentle idle pulse and a bright flash when the red block passes.</summary>
    public sealed class GateFx : MonoBehaviour
    {
        private Image[] _chevrons;
        private Image _flash;
        private Color _color;
        private float _clock, _flashAge = float.MaxValue;
        private const float FlashSeconds = 0.45f;

        /// <summary>Animates <paramref name="chevrons"/>; <paramref name="flash"/> is shown on <see cref="Flash"/>.</summary>
        public void Init(Image[] chevrons, Image flash, Color color)
        {
            _chevrons = chevrons;
            _flash = flash;
            _color = color;
            _flash.color = new Color(color.r, color.g, color.b, 0f);
        }

        /// <summary>Bursts the gate open as the red block passes through.</summary>
        public void Flash() => _flashAge = 0f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _clock += dt;
            _flashAge += dt;
            float flash = Mathf.Clamp01(1f - _flashAge / FlashSeconds);
            for (int i = 0; i < _chevrons.Length; i++)
            {
                // Chevrons light up one after another, pointing the way out.
                float wave = 0.35f + 0.65f * Mathf.Clamp01(Mathf.Sin(_clock * 3f - i * 0.9f));
                Color c = Color.Lerp(_color, Color.white, flash);
                c.a = Mathf.Max(wave, flash);
                _chevrons[i].color = c;
                _chevrons[i].rectTransform.localScale = Vector3.one * (1f + flash * 0.5f);
            }
            Color f = _flash.color;
            f.a = flash * 0.9f;
            _flash.color = f;
            _flash.rectTransform.localScale = Vector3.one * (1f + (1f - flash) * 1.6f);
        }
    }
}
