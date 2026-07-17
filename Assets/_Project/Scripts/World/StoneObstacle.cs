using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class StoneObstacle : MonoBehaviour
    {
        const float ColliderRadiusRatio = 0.22f;

        void Awake()
        {
            SetupCollider();

            if (GetComponent<YSortRenderer>() == null)
                gameObject.AddComponent<YSortRenderer>();
        }

        void SetupCollider()
        {
            foreach (var existing in GetComponents<Collider2D>())
                Destroy(existing);

            var renderer = GetComponent<SpriteRenderer>();
            var sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null)
            {
                var fallback = gameObject.AddComponent<CircleCollider2D>();
                fallback.radius = 0.35f;
                return;
            }

            var bounds = sprite.bounds;
            var radius = Mathf.Max(0.18f, Mathf.Min(bounds.size.x, bounds.size.y) * ColliderRadiusRatio);
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.offset = new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * 0.35f);
        }
    }
}