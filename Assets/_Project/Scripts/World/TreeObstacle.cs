using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TreeObstacle : MonoBehaviour
    {
        /// <summary>Small rounded trunk so large crowns do not trap the player at the base.</summary>
        const float TrunkRadiusRatio = 0.11f;
        const float MinTrunkRadius = 0.12f;
        const float MaxTrunkRadius = 0.28f;

        void Awake()
        {
            var renderer = GetComponent<SpriteRenderer>();
            var sprite = renderer != null ? renderer.sprite : null;
            SetupTrunkCollider(sprite);

            if (GetComponent<YSortRenderer>() == null)
            {
                var sortBias = sprite != null ? sprite.bounds.size.y * 0.12f : 0f;
                gameObject.AddComponent<YSortRenderer>().Configure(0, sortBias);
            }
        }

        void SetupTrunkCollider(Sprite sprite)
        {
            foreach (var col in GetComponents<Collider2D>())
                Destroy(col);

            if (sprite == null)
            {
                var fallback = gameObject.AddComponent<CircleCollider2D>();
                fallback.radius = 0.16f;
                fallback.offset = new Vector2(0f, 0.1f);
                return;
            }

            var bounds = sprite.bounds;
            // Circle at the trunk base — rounded contact instead of a sticky box.
            var radius = Mathf.Clamp(
                Mathf.Min(bounds.size.x, bounds.size.y) * TrunkRadiusRatio,
                MinTrunkRadius,
                MaxTrunkRadius);
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.offset = new Vector2(bounds.center.x, bounds.min.y + radius * 0.85f);
        }
    }
}
