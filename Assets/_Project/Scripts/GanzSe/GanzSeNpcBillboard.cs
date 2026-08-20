using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Replaces the legacy placeholder sprite with a GanzSe baked sprite.
    /// </summary>
    public class GanzSeNpcBillboard : MonoBehaviour
    {
        GanzSeNpcRole _role;
        SpriteRenderer _renderer;
        Vector3 _baseScale = Vector3.one;
        float _idle;
        bool _active;

        public GanzSeNpcRole Role => _role;
        public bool IsActive => _active;

        /// <summary>Returns false if GanzSe could not load — caller keeps a sensible sprite scale.</summary>
        public bool Initialize(GanzSeNpcRole role, float worldHeight = 1.55f)
        {
            _role = role;
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) return false;

            if (!GanzSeRenderStudio.TryGetSprite(role, out var sprite) || sprite == null)
                return false;

            _renderer.sprite = sprite;
            _renderer.enabled = true;
            _renderer.color = Color.white;
            _renderer.drawMode = SpriteDrawMode.Simple;

            var naturalHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
            var scale = worldHeight / naturalHeight;
            _baseScale = new Vector3(scale, scale, 1f);
            transform.localScale = _baseScale;

            _active = true;
            _idle = Random.Range(0f, 10f);
            return true;
        }

        void LateUpdate()
        {
            if (!_active || _renderer == null || !_renderer.enabled) return;
            _idle += Time.unscaledDeltaTime;
            var bob = 1f + Mathf.Sin(_idle * 2.2f) * 0.02f;
            transform.localScale = new Vector3(_baseScale.x * bob, _baseScale.y * bob, 1f);
            _renderer.sortingOrder = ArenaBounds.GetYSortOrder(transform.position.y, 4);
        }
    }
}
