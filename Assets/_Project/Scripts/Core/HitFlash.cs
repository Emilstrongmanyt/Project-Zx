using UnityEngine;

namespace ProjectZx.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        SpriteRenderer _renderer;
        Color _baseColor = Color.white;
        float _timer;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        void Update()
        {
            if (_timer <= 0f || _renderer == null) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                _renderer.color = _baseColor;
        }

        public void Flash(float duration = 0.14f)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) return;

            _baseColor = _renderer.color;
            _renderer.color = Color.white;
            _timer = duration;
        }

        public static void FlashSprite(GameObject go, float duration = 0.14f)
        {
            if (go == null) return;
            var flash = go.GetComponent<HitFlash>();
            if (flash == null) flash = go.AddComponent<HitFlash>();
            flash.Flash(duration);
        }
    }
}