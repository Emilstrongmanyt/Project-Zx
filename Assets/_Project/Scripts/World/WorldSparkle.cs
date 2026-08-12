using System.Collections.Generic;
using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>Short world-space sparkle burst (quest crow rescue, etc.).</summary>
    public class WorldSparkle : MonoBehaviour
    {
        struct Particle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Spin;
            public float BaseScale;
        }

        readonly List<Particle> _particles = new();

        public static void Play(Vector3 worldPos, int count = 12)
        {
            var go = new GameObject("WorldSparkle");
            go.transform.position = worldPos;
            go.AddComponent<WorldSparkle>().Begin(count);
        }

        void Begin(int count)
        {
            var sprites = new[] { ArtLibrary.Sparkles, ArtLibrary.Sparkles2 };
            for (var i = 0; i < count; i++)
            {
                var sprite = sprites[i % sprites.Length];
                if (sprite == null) continue;

                var child = new GameObject("Sparkle");
                child.transform.SetParent(transform, false);
                child.transform.localPosition = (Vector3)(Random.insideUnitCircle * 0.25f);
                var scale = Random.Range(0.35f, 0.65f);
                child.transform.localScale = Vector3.one * scale;

                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 40;
                child.AddComponent<YSortRenderer>().Configure(20);

                var angle = Random.Range(0f, Mathf.PI * 2f);
                var speed = Random.Range(1.8f, 4.5f);
                _particles.Add(new Particle
                {
                    Transform = child.transform,
                    Renderer = sr,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Life = Random.Range(0.45f, 0.85f),
                    MaxLife = 0.85f,
                    Spin = Random.Range(-220f, 220f),
                    BaseScale = scale
                });
            }

            if (_particles.Count == 0)
                Destroy(gameObject);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life -= dt;
                if (p.Transform != null)
                {
                    p.Transform.localPosition += (Vector3)(p.Velocity * dt);
                    p.Transform.Rotate(0f, 0f, p.Spin * dt);
                    var u = Mathf.Clamp01(p.Life / p.MaxLife);
                    p.Transform.localScale = Vector3.one * (p.BaseScale * (0.55f + 0.45f * u));
                    if (p.Renderer != null)
                    {
                        var c = p.Renderer.color;
                        c.a = u;
                        p.Renderer.color = c;
                    }
                }

                if (p.Life <= 0f)
                {
                    if (p.Transform != null)
                        Destroy(p.Transform.gameObject);
                    _particles.RemoveAt(i);
                }
                else
                {
                    _particles[i] = p;
                }
            }

            if (_particles.Count == 0)
                Destroy(gameObject);
        }
    }
}
