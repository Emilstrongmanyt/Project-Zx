using ProjectZx.Enemies;
using UnityEngine;

namespace ProjectZx.World
{
    public static class ArenaBounds
    {
        public const float ArenaWidth = 64f;
        public const float ArenaHeight = 48f;
        public const float TileSize = 1f;
        public const float SpawnClearRadius = 1.05f;

        static readonly Collider2D[] OverlapBuffer = new Collider2D[12];

        public static Vector2 ClampToPlayable(Vector2 position)
        {
            var maxX = ArenaWidth * 0.5f - TileSize * 2f;
            var maxY = ArenaHeight * 0.5f - TileSize * 2f;
            return new Vector2(
                Mathf.Clamp(position.x, -maxX, maxX),
                Mathf.Clamp(position.y, -maxY, maxY));
        }

        public static bool IsInsidePlayable(Vector2 position)
        {
            var maxX = ArenaWidth * 0.5f - TileSize * 2f;
            var maxY = ArenaHeight * 0.5f - TileSize * 2f;
            return Mathf.Abs(position.x) <= maxX && Mathf.Abs(position.y) <= maxY;
        }

        public static bool IsClearOfObstacles(Vector2 position, float radius = SpawnClearRadius)
        {
            var count = Physics2D.OverlapCircleNonAlloc(position, radius, OverlapBuffer);
            for (var i = 0; i < count; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null || col.isTrigger) continue;
                if (col.GetComponent<WaterTile>() != null || col.GetComponentInParent<WaterTile>() != null)
                    return false;
                if (col.GetComponent<ArenaObstacle>() != null || col.GetComponentInParent<ArenaObstacle>() != null)
                    return false;
                if (col.GetComponent<EnemyActor>() != null) continue;
                if (col.CompareTag("Player")) continue;
                return false;
            }

            return true;
        }

        public static Vector2 RandomSpawnAround(Vector2 origin, float minDistance, float maxDistance)
        {
            for (var attempt = 0; attempt < 48; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = Random.Range(minDistance, maxDistance);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var candidate = ClampToPlayable(origin + offset);
                if (Vector2.Distance(candidate, origin) < minDistance * 0.75f) continue;
                if (!IsInsidePlayable(candidate)) continue;
                if (!IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            for (var attempt = 0; attempt < 24; attempt++)
            {
                var candidate = ClampToPlayable(origin + Random.insideUnitCircle * minDistance);
                if (!IsInsidePlayable(candidate)) continue;
                if (!IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            return ClampToPlayable(origin);
        }
    }
}