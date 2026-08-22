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
        /// Legacy torus wrap. Survival now uses <see cref="StreamingEnabled"/> instead.
        /// Camp: finite water ring, clamp only.
        /// </summary>
        public static bool WorldWrapEnabled { get; private set; }

        /// <summary>Chunk-streamed endless survival (no edge teleport).</summary>
        public static bool StreamingEnabled { get; private set; }

        static Vector2 _streamMin = new(-32f, -32f);
        static Vector2 _streamMax = new(32f, 32f);

        /// <summary>Floor tile root — co-moved on wrap so the camera frame stays continuous.</summary>
        static Transform _floorRoot;
        /// <summary>Props / obstacles root — co-moved with the floor on wrap.</summary>
        static Transform _propsRoot;

        public static void SetWorldWrap(bool enabled)
        {
            WorldWrapEnabled = enabled;
            if (enabled)
                StreamingEnabled = false;
            if (!enabled)
                ClearWorldRoots();
        }

        public static void SetStreaming(bool enabled)
        {
            StreamingEnabled = enabled;
            if (enabled)
            {
                WorldWrapEnabled = false;
                ClearWorldRoots();
            }
        }

        public static void SetStreamingBounds(Vector2 min, Vector2 max)
        {
            _streamMin = min;
            _streamMax = max;
        }

        public static void RegisterWorldRoots(Transform floorRoot, Transform propsRoot)
        {
            _floorRoot = floorRoot;
            _propsRoot = propsRoot;
        }

        public static void ClearWorldRoots()
        {
            _floorRoot = null;
            _propsRoot = null;
        }

        /// <summary>Shortest signed offset from <paramref name="from"/> to <paramref name="to"/> on the torus.</summary>
        public static Vector2 ToroidalDelta(Vector2 from, Vector2 to)
        {
            if (!WorldWrapEnabled)
                return to - from;

            var dx = to.x - from.x;
            var dy = to.y - from.y;
            var w = ArenaWidth;
            var h = ArenaHeight;
            if (dx > w * 0.5f) dx -= w;
            else if (dx < -w * 0.5f) dx += w;
            if (dy > h * 0.5f) dy -= h;
            else if (dy < -h * 0.5f) dy += h;
            return new Vector2(dx, dy);
        }

        public static float ToroidalDistance(Vector2 from, Vector2 to)
            => ToroidalDelta(from, to).magnitude;

        public static float ToroidalDistanceSqr(Vector2 from, Vector2 to)
            => ToroidalDelta(from, to).sqrMagnitude;

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
            wrapDelta = Vector2.zero;

            // Endless streaming: free move inside loaded chunks (soft pad from unloaded void).
            if (StreamingEnabled)
            {
                const float pad = 0.75f;
                constrained = new Vector2(
                    Mathf.Clamp(position.x, _streamMin.x + pad, _streamMax.x - pad),
                    Mathf.Clamp(position.y, _streamMin.y + pad, _streamMax.y - pad));
                return;
            }

            if (!WorldWrapEnabled)
            {
                var maxX = PlayableHalfWidth;
                var maxY = PlayableHalfHeight;
                constrained = new Vector2(
                    Mathf.Clamp(position.x, -maxX, maxX),
                    Mathf.Clamp(position.y, -maxY, maxY));
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
        /// After the player wraps: co-move combat units, quest props, projectiles,
        /// AND the floor/props roots so the rendered frame stays continuous.
        /// Mobiles are then re-wrapped into playable so nothing sits off-map and
        /// "pops" back a frame later.
        /// </summary>
        public static void ApplyWorldWrapDelta(Vector2 wrapDelta)
        {
            if (!WorldWrapEnabled || wrapDelta.sqrMagnitude < 0.25f) return;

            // Scenery first — same delta as the player keeps underfoot tiles/props stable on camera.
            ShiftTransform(_floorRoot, wrapDelta);
            ShiftTransform(_propsRoot, wrapDelta);

            // Companion first — force RB + transform so assist hero never trails a map away.
            var companions = Object.FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None);
            for (var i = 0; i < companions.Length; i++)
            {
                if (companions[i] != null)
                    companions[i].TeleportWithLeader(wrapDelta);
            }

            ShiftAllAndRewrap<EnemyActor>(wrapDelta);
            ShiftAllAndRewrap<LootPickup>(wrapDelta);
            ShiftAllAndRewrap<ArenaDoor>(wrapDelta);
            ShiftAllAndRewrap<ArenaGateway>(wrapDelta);
            ShiftAllAndRewrap<ArenaCryptPortal>(wrapDelta);
            ShiftAllAndRewrap<ArenaVictoryGate>(wrapDelta);
            ShiftAllAndRewrap<DarkBirdRescue>(wrapDelta);
            ShiftAllAndRewrap<DungeonKnightEncounter>(wrapDelta);
            ShiftAllAndRewrap<ArrowProjectile>(wrapDelta);
            ShiftAllAndRewrap<BossFireProjectile>(wrapDelta);
            ShiftAllAndRewrap<EnemyRangedProjectile>(wrapDelta);
            ShiftAllAndRewrap<WorldSparkle>(wrapDelta);
        }

        static void ShiftAllAndRewrap<T>(Vector2 delta) where T : Component
        {
            var items = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null) continue;
                if (item.CompareTag("Player") && item.GetComponent<CompanionFollower>() == null)
                    continue;
                // Props live under _propsRoot and already moved with the root.
                if (_propsRoot != null && item.transform.IsChildOf(_propsRoot))
                    continue;
                ShiftTransform(item.transform, delta);
                RewrapMobile(item.transform);
            }
        }

        static void RewrapMobile(Transform t)
        {
            if (t == null) return;
            var p = (Vector2)t.position;
            var wrapped = WrapToPlayable(p);
            if ((wrapped - p).sqrMagnitude < 0.0001f) return;
            var rb = t.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = wrapped;
                t.position = new Vector3(wrapped.x, wrapped.y, t.position.z);
            }
            else
            {
                t.position = new Vector3(wrapped.x, wrapped.y, t.position.z);
            }
        }

        public static void ShiftTransform(Transform t, Vector2 delta)
        {
            if (t == null || delta.sqrMagnitude < 0.0001f) return;
            var rb = t.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var next = rb.position + delta;
                rb.position = next;
                // Keep transform in sync the same frame (kinematic RB can lag a step).
                t.position = new Vector3(next.x, next.y, t.position.z);
                return;
            }

            t.position += (Vector3)delta;
        }

        public static bool IsInsidePlayable(Vector2 position)
        {
            if (StreamingEnabled)
            {
                const float pad = 0.5f;
                return position.x >= _streamMin.x + pad && position.x <= _streamMax.x - pad
                       && position.y >= _streamMin.y + pad && position.y <= _streamMax.y - pad;
            }

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
            if (StreamingEnabled && SurvivalChunkStreamer.Instance != null)
                return SurvivalChunkStreamer.Instance.RandomSpawnAroundPlayer(
                    origin, minDistance, maxDistance);

            for (var attempt = 0; attempt < 48; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = Random.Range(minDistance, maxDistance);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var candidate = ClampToPlayable(origin + offset);
                var dist = WorldWrapEnabled
                    ? ToroidalDistance(candidate, origin)
                    : Vector2.Distance(candidate, origin);
                if (dist < minDistance * 0.75f) continue;
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
            if (StreamingEnabled && SurvivalChunkStreamer.Instance != null)
            {
                var min = preferDistance ? 10f : 6f;
                var max = preferDistance ? 18f : 14f;
                var roll = Random.value;
                if (roll < 0.35f)
                    return SurvivalChunkStreamer.Instance.RandomSpawnAroundPlayer(playerPos, min, max, preferFar: preferDistance);
                if (roll < 0.7f)
                    return SurvivalChunkStreamer.Instance.RandomSpawnAroundPlayer(playerPos, min + 2f, max + 4f, preferFar: preferDistance);
                return SurvivalChunkStreamer.Instance.RandomSpawnAroundPlayer(playerPos, 7f, 16f, preferFar: preferDistance);
            }

            var origin = playerPos + Random.insideUnitCircle * 2.8f;
            var rollLegacy = Random.value;

            if (preferDistance)
                rollLegacy = Mathf.Min(1f, rollLegacy + 0.22f);

            Vector2 candidate;
            if (rollLegacy < 0.28f)
                candidate = RandomSpawnAround(origin, 5.5f, 10f);
            else if (rollLegacy < 0.5f)
                candidate = RandomSpawnAround(origin, 9f, 15f);
            else if (rollLegacy < 0.68f)
                candidate = RandomSpawnAround(origin, 12f, 20f);
            else if (rollLegacy < 0.84f)
                candidate = RandomSpawnAtPlayableEdge(playerPos, minDistanceFromPlayer: 6f);
            else if (rollLegacy < 0.94f)
                candidate = RandomSpawnInPlayableAwayFrom(playerPos, minDistance: 8f);
            else
                candidate = RandomSpawnFlanking(playerPos, minDistance: 6.5f, maxDistance: 14f);

            candidate = ClampToPlayable(candidate + Random.insideUnitCircle * 0.55f);
            var nearPlayer = WorldWrapEnabled
                ? ToroidalDistance(candidate, playerPos)
                : Vector2.Distance(candidate, playerPos);
            if (IsInsidePlayable(candidate) && IsClearOfObstacles(candidate) && nearPlayer >= 4.5f)
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
