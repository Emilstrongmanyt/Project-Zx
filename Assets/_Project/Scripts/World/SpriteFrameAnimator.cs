using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>Simple world-space sprite sheet loop (camp NPCs, props).</summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _fps = 4f;
        float _timer;
        int _index;

        public void Initialize(Sprite[] frames, float fps = 4f)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _frames = frames;
            _fps = Mathf.Max(0.5f, fps);
            _timer = 0f;
            _index = 0;
            if (_renderer != null && _frames != null && _frames.Length > 0)
                _renderer.sprite = _frames[0];
        }

        void Update()
        {
            if (_renderer == null || _frames == null || _frames.Length <= 1) return;
            _timer += Time.deltaTime;
            var step = 1f / _fps;
            if (_timer < step) return;
            _timer -= step;
            _index = (_index + 1) % _frames.Length;
            _renderer.sprite = _frames[_index];
        }
    }
}
