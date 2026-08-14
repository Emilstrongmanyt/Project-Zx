using ProjectZx.Combat;
using ProjectZx.Enemies;
using ProjectZx.Player;
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
        /// <summary>Outer ring of water tiles around playable land (camp only).</summary>
        public const int WaterBorderDepth = 3;
        /// <summary>
        /// Extra biome floor past the wrap edge. Wider on X so landscape cameras
        /// never show the tile edge before the invisible teleport border.
        /// </summary>
        public const int VisualSkirtDepthX = 28;
        public const int VisualSkirtDepthY = 16;
        public const int EntitySortBase = 100;
        public const float SortDepthScale = 40f;

        static readonly Collider2D[] OverlapBuffer = new Collider2D[12];

        /// <summary>
        /// Survival: wrap (teleport to opposite side) at the playable edge.
        /// Camp: finite water ring, clamp only.
        /// </summary>
        public static bool WorldWrapEnabled { get; private set; }

        public static void SetWorldWrap(bool enabled) => WorldWrapEnabled = enabled;

        public static float WaterMargin => WorldWrapEnabled ? 0f : TileSize * WaterBorderDepth;

        public static float PlayableHalfWidth => ArenaWidth * 0.5f - WaterMargin;
        public static float PlayableHalfHeight => ArenaHeight * 0.5f - WaterMargin;

        public static float VisualFieldWidth =>
            WorldWrapEnabled ? ArenaWidth + VisualSkirtDepthX * TileSize * 2f : ArenaWidth;
        public static float VisualFieldHeight =>
            WorldWrapEnabled ? ArenaHeight + VisualSkirtDepthY * TileSize * 2f : ArenaHeight;

        public static int GetYSortOrder(float worldY, int offset = 0)
        {
            var depth = Mathf.RoundToInt((PlayableHalfHeight - worldY) * SortDepthScale);
            return EntitySortBase + depth + offset;
        }

        public static Vector2 ClampToPlayable(Vector2 position)
        {
            ConstrainPosition(position, out var constrained, out _);
            return constrained;
        }

        /// <summary>
        /// Clamp (camp) or wrap (survival). When wrapping, <paramref name="wrapDelta"/> is the
        /// teleport offset so callers can co-move companions / combat with the player.
        /// </summary>
        public static void ConstrainPosition(Vector2 position, out Vector2 constrained, out Vector2 wrapDelta)
        {
            if (!WorldWrapEnabled)
            {
                var maxX = PlayableHalfWidth;
                var maxY = PlayableHalfHeight;
                constrained = new Vector2(
                    Mathf.Clamp(position.x, -maxX, maxX),
                    Mathf.Clamp(position.y, -maxY, maxY));
                wrapDelta = Vector2.zero;
                return;
            }

            constrained = WrapToPlayable(position);
            wrapDelta = constrained - position;
            // Ignore tiny float noise; real wraps are full arena steps.
            if (wrapDelta.sqrMagnitude < 0.25f)
                wrapDelta = Vector2.zero;
        }

        /// <summary>
        /// Invisible wrap border: leaving one edge teleports to the opposite side.
        /// </summary>
        public static Vector2 WrapToPlayable(Vector2 position)
        {
            var halfW = ArenaWidth * 0.5f;
            var halfH = ArenaHeight * 0.5f;
            position.x = Mathf.Repeat(position.x + halfW, ArenaWidth) - halfW;
            position.y = Mathf.Repeat(position.y + halfH, ArenaHeight) - halfH;
            return position;
        }

        /// <summary>
        /// After the player wraps, shift the rest of the world by the same delta so relative
        /// positions stay continuous (companion, enemies, loot, props, portals).
        /// </summary>
        public static void ApplyWorldWrapDelta(Vector2 wrapDelta)
        {
            if (!WorldWrapEnabled || wrapDelta.sqrMagnitude < 0.25f) return;

            ShiftAll<CompanionFollower>(wrapDelta);
            ShiftAll<EnemyActor>(wrapDelta);
            ShiftAll<LootPickup>(wrapDelta);
            ShiftAll<ArenaObstacle>(wrapDelta);
            ShiftAll<ArenaDoor>(wrapDelta);
            ShiftAll<ArenaGateway>(wrapDelta);
            ShiftAll<ArenaCryptPortal>(wrapDelta);
            ShiftAll<ArenaVictoryGate>(wrapDelta);
            ShiftAll<DarkBirdRescue>(wrapDelta);
            ShiftAll<DungeonKnightEncounter>(wrapDelta);
            ShiftAll<ArrowProjectile>(wrapDelta);
            ShiftAll<BossFireProjectile>(wrapDelta);
            ShiftAll<EnemyRangedProjectile>(wrapDelta);
        }

        static void ShiftAll<T>(Vector2 delta) where T : Component
        {
            var items = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null) continue;
                // Never shift the player root (companion is separate).
                if (item.CompareTag("Player") && item.GetComponent<CompanionFollower>() == null)
                    continue;
                ShiftTransform(item.transform, delta);
            }
        }

        static void ShiftTransform(Transform t, Vector2 delta)
        {
            if (t == null) return;
            var rb = t.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position += delta;
                return;
            }

            t.position += (Vector3)delta;
        }

        public static bool IsInsidePlayable(Vector2 position)
        {
            return Mathf.Abs(position.x) <= PlayableHalfWidth
                   && Mathf.Abs(position.y) <= PlayableHalfHeight;
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

        public static Vector2 RandomWaveSpawn(Vector2 playerPos, bool preferDistance = false)
        {
            var origin = playerPos + Random.insideUnitCircle * 2.8f;
            var roll = Random.value;

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

            candidate = ClampToPlayable(candidate + Random.insideUnitCircle * 0.55f);
            if (IsInsidePlayable(candidate) && IsClearOfObstacles(candidate)
                && Vector2.Distance(candidate, playerPos) >= 4.5f)
                return candidate;

            return RandomSpawnAround(playerPos, 7f, 14f);
        }

        static Vector2 RandomSpawnAtPlayableEdge(Vector2 playerPos, float minDistanceFromPlayer)
        {
            var maxX = PlayableHalfWidth - 0.75f;
            var maxY = PlayableHalfHeight - 0.75f;

            for (var attempt = 0; attempt < 40; attempt++)
            {
                Vector2 candidate;
                var side = Random.Range(0, 4);
                switch (side)
                {
                    case 0:
                        candidate = new Vector2(-maxX, Random.Range(-maxY, maxY));
                        break;
                    case 1:
                        candidate = new Vector2(maxX, Random.Range(-maxY, maxY));
                        break;
                    case 2:
                        candidate = new Vector2(Random.Range(-maxX, maxX), -maxY);
                        break;
                    default:
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
            var maxX = PlayableHalfWidth - 1f;
            var maxY = PlayableHalfHeight - 1f;

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
            var baseAngle = Random.value < 0.5f
                ? Random.Range(-0.55f, 0.55f)
                : Mathf.PI * 0.5f + Random.Range(-0.55f, 0.55f);
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
