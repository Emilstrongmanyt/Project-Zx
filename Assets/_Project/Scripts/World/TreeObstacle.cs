using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TreeObstacle : MonoBehaviour
    {
        const float TrunkHeightRatio = 0.22f;
        const float TrunkWidthRatio = 0.3f;

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
                fallback.radius = 0.18f;
                fallback.offset = new Vector2(0f, 0.12f);
                return;
            }

            var bounds = sprite.bounds;
            var trunkHeight = Mathf.Max(0.1f, bounds.size.y * TrunkHeightRatio);
            var trunkWidth = Mathf.Max(0.1f, bounds.size.x * TrunkWidthRatio);
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(trunkWidth, trunkHeight);
            box.offset = new Vector2(bounds.center.x, bounds.min.y + trunkHeight * 0.5f);
        }
    }
}