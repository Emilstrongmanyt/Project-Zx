using ProjectZx.Enemies;
using UnityEngine;

namespace ProjectZx.World
{
    public static class ArenaBounds
    {
        public const float ArenaWidth = 64f;
        public const float ArenaHeight = 48f;
        public const float CampWidth = 44f;
        public const float CampHeight = 34f;
        public const float TileSize = 1f;
        public const float SpawnClearRadius = 1.05f;
        public const int FloorSortOrder = -1000;
        public const int WaterSortOrder = -900;
        /// <summary>Outer ring of water tiles around playable land (camp only; survival uses wrap).</summary>
        public const int WaterBorderDepth = 3;
        public const int EntitySortBase = 100;
        public const float SortDepthScale = 40f;

        static readonly Collider2D[] OverlapBuffer = new Collider2D[12];

        /// <summary>
        /// Survival arenas wrap at the map edge (no water border). Camp keeps a finite water ring.
        /// </summary>
        public static bool WorldWrapEnabled { get; private set; }

        public static void SetWorldWrap(bool enabled) => WorldWrapEnabled = enabled;

        public static float WaterMargin => WorldWrapEnabled ? 0f : TileSize * WaterBorderDepth;
        public static float PlayableHalfHeight => ArenaHeight * 0.5f - WaterMargin;

        public static int GetYSortOrder(float worldY, int offset = 0)
        {
            // South (lower Y) draws in front with a positive, stable sorting band above floor tiles.
            var depth = Mathf.RoundToInt((PlayableHalfHeight - worldY) * SortDepthScale);
            return EntitySortBase + depth + offset;
        }

        public static Vector2 ClampToPlayable(Vector2 position)
        {
            if (WorldWrapEnabled)
                return WrapToPlayable(position);

            var maxX = ArenaWidth * 0.5f - WaterMargin;
            var maxY = ArenaHeight * 0.5f - WaterMargin;
            return new Vector2(
                Mathf.Clamp(position.x, -maxX, maxX),
                Mathf.Clamp(position.y, -maxY, maxY));
        }

        /// <summary>Toroidal wrap so survival maps feel never-ending.</summary>
        public static Vector2 WrapToPlayable(Vector2 position)
        {
            var halfW = ArenaWidth * 0.5f;
            var halfH = ArenaHeight * 0.5f;
            position.x = Mathf.Repeat(position.x + halfW, ArenaWidth) - halfW;
            position.y = Mathf.Repeat(position.y + halfH, ArenaHeight) - halfH;
            return position;
        }

        public static bool IsInsidePlayable(Vector2 position)
        {
            var maxX = ArenaWidth * 0.5f - WaterMargin;
            var maxY = ArenaHeight * 0.5f - WaterMargin;
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

        /// <summary>
        /// Survival wave spawn: mixes near/far rings, arena edges, open-field picks, and
        /// flanking angles so packs are harder to predict than a fixed 7–12 ring.
        /// </summary>
        public static Vector2 RandomWaveSpawn(Vector2 playerPos, bool preferDistance = false)
        {
            // Slight origin jitter so multi-spawns in one wave do not share one perfect center.
            var origin = playerPos + Random.insideUnitCircle * 2.8f;
            var roll = Random.value;

            // Bosses / late pressure: bias toward farther entries.
            if (preferDistance)
                roll = Mathf.Min(1f, roll + 0.22f);

            Vector2 candidate;
            if (roll < 0.28f)
                candidate = RandomSpawnAround(origin, 5.5f, 10f);
            else if (roll < 0.5f)
                candidate = RandomSpawnAround(origin, 9f, 15f);
            else if (roll < 0.68f)
                candidate = RandomSpawnAround(origin, 12f, 20f);
            else if (roll < 0.84f)
                candidate = RandomSpawnAtPlayableEdge(playerPos, minDistanceFromPlayer: 6f);
            else if (roll < 0.94f)
                candidate = RandomSpawnInPlayableAwayFrom(playerPos, minDistance: 8f);
            else
                candidate = RandomSpawnFlanking(playerPos, minDistance: 6.5f, maxDistance: 14f);

            // Tiny final jitter so even same-strategy picks do not stack on identical tiles.
            candidate = ClampToPlayable(candidate + Random.insideUnitCircle * 0.55f);
            if (IsInsidePlayable(candidate) && IsClearOfObstacles(candidate)
                && Vector2.Distance(candidate, playerPos) >= 4.5f)
                return candidate;

            return RandomSpawnAround(playerPos, 7f, 14f);
        }

        static Vector2 RandomSpawnAtPlayableEdge(Vector2 playerPos, float minDistanceFromPlayer)
        {
            var maxX = ArenaWidth * 0.5f - WaterMargin - 0.75f;
            var maxY = ArenaHeight * 0.5f - WaterMargin - 0.75f;

            for (var attempt = 0; attempt < 40; attempt++)
            {
                Vector2 candidate;
                var side = Random.Range(0, 4);
                switch (side)
                {
                    case 0: // west
                        candidate = new Vector2(-maxX, Random.Range(-maxY, maxY));
                        break;
                    case 1: // east
                        candidate = new Vector2(maxX, Random.Range(-maxY, maxY));
                        break;
                    case 2: // south
                        candidate = new Vector2(Random.Range(-maxX, maxX), -maxY);
                        break;
                    default: // north
                        candidate = new Vector2(Random.Range(-maxX, maxX), maxY);
                        break;
                }

                candidate = ClampToPlayable(candidate + Random.insideUnitCircle * 1.2f);
                if (Vector2.Distance(candidate, playerPos) < minDistanceFromPlayer) continue;
                if (!IsInsidePlayable(candidate)) continue;
                if (!IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            return RandomSpawnAround(playerPos, minDistanceFromPlayer, minDistanceFromPlayer + 6f);
        }

        static Vector2 RandomSpawnInPlayableAwayFrom(Vector2 playerPos, float minDistance)
        {
            var maxX = ArenaWidth * 0.5f - WaterMargin - 1f;
            var maxY = ArenaHeight * 0.5f - WaterMargin - 1f;

            for (var attempt = 0; attempt < 48; attempt++)
            {
                var candidate = new Vector2(
                    Random.Range(-maxX, maxX),
                    Random.Range(-maxY, maxY));
                if (Vector2.Distance(candidate, playerPos) < minDistance) continue;
                if (!IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            return RandomSpawnAround(playerPos, minDistance, minDistance + 8f);
        }

        static Vector2 RandomSpawnFlanking(Vector2 playerPos, float minDistance, float maxDistance)
        {
            // Prefer left/right of the player's current facing-agnostic axes (cardinal flanks).
            var baseAngle = Random.value < 0.5f
                ? Random.Range(-0.55f, 0.55f) // roughly east/west of vertical
                : Mathf.PI * 0.5f + Random.Range(-0.55f, 0.55f); // roughly north/south of horizontal
            if (Random.value < 0.5f) baseAngle += Mathf.PI;

            for (var attempt = 0; attempt < 32; attempt++)
            {
                var angle = baseAngle + Random.Range(-0.35f, 0.35f);
                var distance = Random.Range(minDistance, maxDistance);
                var candidate = ClampToPlayable(
                    playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
                if (Vector2.Distance(candidate, playerPos) < minDistance * 0.8f) continue;
                if (!IsInsidePlayable(candidate)) continue;
                if (!IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            return RandomSpawnAround(playerPos, minDistance, maxDistance);
        }
    }
}