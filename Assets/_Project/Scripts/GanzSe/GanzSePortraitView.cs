using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Quest dialogue portrait: RawImage fed by the GanzSe studio RT (idle sway from studio).
    /// </summary>
    public class GanzSePortraitView : MonoBehaviour
    {
        RawImage _raw;
        Image _fallbackImage;
        GanzSeNpcRole _role;
        bool _active;

        public void Bind(RawImage raw, Image fallbackImage)
        {
            _raw = raw;
            _fallbackImage = fallbackImage;
        }

        public void Show(GanzSeNpcRole role)
        {
            _role = role;
            _active = true;

            if (GanzSeRenderStudio.TryGetTexture(role, out var rt) && rt != null && _raw != null)
            {
                _raw.texture = rt;
                _raw.enabled = true;
                _raw.color = Color.white;
                if (_fallbackImage != null)
                    _fallbackImage.enabled = false;
                return;
            }

            // Prefab missing — keep legacy sprite portrait.
            if (_raw != null) _raw.enabled = false;
            if (_fallbackImage != null) _fallbackImage.enabled = true;
        }

        public void Hide()
        {
            _active = false;
            if (_raw != null)
            {
                _raw.enabled = false;
                _raw.texture = null;
            }
        }

        void LateUpdate()
        {
            if (!_active || _raw == null || !_raw.enabled) return;
            // Soft bob complementary to studio yaw sway.
            var t = Time.unscaledTime;
            var bob = 1f + Mathf.Sin(t * 2.4f) * 0.015f;
            _raw.rectTransform.localScale = new Vector3(bob, bob, 1f);
        }
    }
}
