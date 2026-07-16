using UnityEngine;

namespace ProjectZx.World
{
    public static class ArenaBounds
    {
        public const float ArenaWidth = 64f;
        public const float ArenaHeight = 48f;
        public const float TileSize = 1f;

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

        public static Vector2 RandomSpawnAround(Vector2 origin, float minDistance, float maxDistance)
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = Random.Range(minDistance, maxDistance);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var candidate = ClampToPlayable(origin + offset);
                if (Vector2.Distance(candidate, origin) >= minDistance * 0.8f && IsInsidePlayable(candidate))
                    return candidate;
            }

            return ClampToPlayable(origin + Random.insideUnitCircle.normalized * minDistance);
        }
    }
}