using System.Collections.Generic;
using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Short unscaled-time sparkle burst using Sparkles / Sparkles2 art
    /// (level-up panel and shop upgrade purchases).
    /// </summary>
    public class SparkleBurst : MonoBehaviour
    {
        struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Spin;
            public float BaseSize;
        }

        readonly List<Particle> _particles = new();

        public static void Play(Transform parent, Vector2 localCenter, int count = 14)
        {
            if (parent == null) return;
            var go = new GameObject("SparkleBurst");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localCenter;
            rect.sizeDelta = Vector2.zero;
            go.AddComponent<SparkleBurst>().Begin(count);
        }

        void Begin(int count)
        {
            var sprites = new[] { ArtLibrary.Sparkles, ArtLibrary.Sparkles2 };
            for (var i = 0; i < count; i++)
            {
                var sprite = sprites[i % sprites.Length];
                if (sprite == null) continue;

                var go = new GameObject("Sparkle");
                go.transform.SetParent(transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Random.insideUnitCircle * 24f;
                var size = Random.Range(28f, 52f);
                rect.sizeDelta = new Vector2(size, size);

                var image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.color = Color.white;

                var angle = Random.Range(0f, Mathf.PI * 2f);
                var speed = Random.Range(80f, 220f);
                _particles.Add(new Particle
                {
                    Rect = rect,
                    Image = image,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Life = Random.Range(0.55f, 0.95f),
                    MaxLife = 0.95f,
                    Spin = Random.Range(-180f, 180f),
                    BaseSize = size
                });
            }

            if (_particles.Count == 0)
                Destroy(gameObject);
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life -= dt;
                if (p.Life <= 0f || p.Rect == null)
                {
                    if (p.Rect != null) Destroy(p.Rect.gameObject);
                    _particles.RemoveAt(i);
                    continue;
                }

                p.Velocity += new Vector2(0f, 40f) * dt;
                p.Rect.anchoredPosition += p.Velocity * dt;
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.Spin * (1f - p.Life / p.MaxLife));
                var t = Mathf.Clamp01(p.Life / p.MaxLife);
                var s = p.BaseSize * Mathf.Lerp(0.35f, 1.1f, t);
                p.Rect.sizeDelta = new Vector2(s, s);
                if (p.Image != null)
                {
                    var c = p.Image.color;
                    c.a = t;
                    p.Image.color = c;
                }

                _particles[i] = p;
            }

            if (_particles.Count == 0)
                Destroy(gameObject);
        }
    }
}
