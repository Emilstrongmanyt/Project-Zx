using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Endless survival land via player-centered chunk streaming + floating origin.
    /// Chunk identity and decoration use stable logical (true) world coordinates so
    /// rebase never remints the map and props stay predetermined across seams.
    /// </summary>
    public sealed class SurvivalChunkStreamer : MonoBehaviour
    {
        public const float ChunkSize = 16f;
        public const float TileSize = 1f;
        /// <summary>World-grid cell for predetermined props (same cell → same prop forever).</summary>
        public const float PropCellSize = 4f;
        public const float RebaseThreshold = 64f;
        const int MinRingRadius = 3;
        const float SpawnClearRadius = 4.5f;

        public static SurvivalChunkStreamer Instance { get; private set; }

        readonly Dictionary<long, ChunkRuntime> _active = new();
        readonly Stack<GameObject> _tilePool = new();
        readonly Stack<GameObject> _propPool = new();
        readonly List<long> _scratchKeys = new();
        readonly List<Vector2Int> _wanted = new();
        readonly HashSet<long> _wantedKeys = new();

        Transform _root;
        Transform _player;
        SurvivalMapKind _floorKind;
        SurvivalMapKind _propBiome;
        int _worldSeed;
        Vector2Int _playerChunk;
        /// <summary>unityPos = trueWorld + _originOffset. Accumulates on each floating-origin rebase.</summary>
        Vector2 _originOffset;
        int _ringRadius = MinRingRadius;

        public SurvivalMapKind PropBiome => _propBiome;
        public Vector2 LoadedMin { get; private set; }
        public Vector2 LoadedMax { get; private set; }

        public static SurvivalChunkStreamer Ensure(
            Transform player,
            SurvivalMapKind floorKind,
            SurvivalMapKind propBiome,
            int worldSeed = 90210)
        {
            var existing = Instance;
            if (existing != null)
            {
                existing.Configure(player, floorKind, propBiome, worldSeed);
                existing.ForceRefresh();
                return existing;
            }

            var go = new GameObject("SurvivalChunkStreamer");
            var streamer = go.AddComponent<SurvivalChunkStreamer>();
            streamer.Configure(player, floorKind, propBiome, worldSeed);
            streamer.ForceRefresh();
            return streamer;
        }

        void Awake()
        {
            Instance = this;
            _root = transform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ClearAllChunks();
            while (_tilePool.Count > 0)
            {
                var t = _tilePool.Pop();
                if (t != null) Destroy(t);
            }

            while (_propPool.Count > 0)
            {
                var p = _propPool.Pop();
                if (p != null) Destroy(p);
            }
        }

        void LateUpdate()
        {
            if (_player == null) return;
            MaybeRebase();
            RefreshRingRadius();
            var chunk = TrueWorldToChunk(ToTrue(_player.position));
            if (chunk != _playerChunk)
                _playerChunk = chunk;
            // Always sync — keeps land ahead of the camera even mid-chunk.
            SyncRing();
        }

        public void Configure(
            Transform player,
            SurvivalMapKind floorKind,
            SurvivalMapKind propBiome,
            int worldSeed)
        {
            _player = player;
            _floorKind = floorKind;
            _propBiome = propBiome;
            _worldSeed = worldSeed;
            ArenaBounds.SetStreaming(true);
            RefreshRingRadius();
        }

        /// <summary>Unlimited / mid-run biome change: restyle props without nuking underfoot.</summary>
        public void SetPropBiome(SurvivalMapKind propBiome)
        {
            if (_propBiome == propBiome) return;
            _propBiome = propBiome;
            foreach (var kv in _active)
                RebuildChunkProps(kv.Value);
        }

        public void ForceRefresh()
        {
            if (_player == null) return;
            RefreshRingRadius();
            _playerChunk = TrueWorldToChunk(ToTrue(_player.position));
            SyncRing();
        }

        public bool IsInsideLoaded(Vector2 unityPos)
        {
            return unityPos.x >= LoadedMin.x && unityPos.x <= LoadedMax.x
                   && unityPos.y >= LoadedMin.y && unityPos.y <= LoadedMax.y;
        }

        /// <summary>Spawn point in loaded land at Euclidean distance from origin.</summary>
        public Vector2 RandomSpawnAroundPlayer(Vector2 playerPos, float minDist, float maxDist, bool preferFar = false)
        {
            if (preferFar)
            {
                minDist = Mathf.Max(minDist, 10f);
                maxDist = Mathf.Max(maxDist, minDist + 6f);
            }

            for (var attempt = 0; attempt < 56; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var dist = Random.Range(minDist, maxDist);
                var candidate = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                if (!IsInsideLoaded(candidate)) continue;
                if (Vector2.Distance(candidate, playerPos) < minDist * 0.85f) continue;
                if (!ArenaBounds.IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            for (var attempt = 0; attempt < 32; attempt++)
            {
                var t = Random.value;
                var candidate = new Vector2(
                    Mathf.Lerp(LoadedMin.x + 1f, LoadedMax.x - 1f, t),
                    Mathf.Lerp(LoadedMin.y + 1f, LoadedMax.y - 1f, Random.value));
                if (Vector2.Distance(candidate, playerPos) < minDist) continue;
                if (!ArenaBounds.IsClearOfObstacles(candidate)) continue;
                return candidate;
            }

            return playerPos + Vector2.up * Mathf.Max(8f, minDist);
        }

        void RefreshRingRadius()
        {
            var pad = ArenaBounds.StreamingViewPad;
            // Cover camera pad + one extra chunk so soft-clamp almost never engages.
            var needed = Mathf.CeilToInt((pad + ChunkSize) / ChunkSize);
            _ringRadius = Mathf.Max(MinRingRadius, needed);
        }

        void SyncRing()
        {
            _wanted.Clear();
            _wantedKeys.Clear();
            for (var dy = -_ringRadius; dy <= _ringRadius; dy++)
            for (var dx = -_ringRadius; dx <= _ringRadius; dx++)
            {
                var coord = new Vector2Int(_playerChunk.x + dx, _playerChunk.y + dy);
                _wanted.Add(coord);
                _wantedKeys.Add(ChunkToKey(coord));
            }

            _scratchKeys.Clear();
            foreach (var key in _active.Keys)
                _scratchKeys.Add(key);

            for (var i = 0; i < _scratchKeys.Count; i++)
            {
                var key = _scratchKeys[i];
                if (_wantedKeys.Contains(key)) continue;
                if (_active.TryGetValue(key, out var chunk))
                {
                    UnloadChunk(chunk);
                    _active.Remove(key);
                }
            }

            for (var i = 0; i < _wanted.Count; i++)
            {
                var coord = _wanted[i];
                var key = ChunkToKey(coord);
                if (_active.ContainsKey(key)) continue;
                _active[key] = LoadChunk(coord);
            }

            UpdateLoadedBounds();
        }

        void UpdateLoadedBounds()
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var kv in _active)
            {
                var c = kv.Value.Coord;
                var trueMin = ChunkTrueOrigin(c);
                var unityMin = ToUnity(trueMin);
                min.x = Mathf.Min(min.x, unityMin.x);
                min.y = Mathf.Min(min.y, unityMin.y);
                max.x = Mathf.Max(max.x, unityMin.x + ChunkSize);
                max.y = Mathf.Max(max.y, unityMin.y + ChunkSize);
            }

            if (float.IsInfinity(min.x))
            {
                LoadedMin = new Vector2(-ChunkSize, -ChunkSize);
                LoadedMax = new Vector2(ChunkSize, ChunkSize);
            }
            else
            {
                LoadedMin = min;
                LoadedMax = max;
            }

            ArenaBounds.SetStreamingBounds(LoadedMin, LoadedMax);
        }

        ChunkRuntime LoadChunk(Vector2Int coord)
        {
            var go = new GameObject($"Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(_root, false);
            var trueOrigin = ChunkTrueOrigin(coord);
            go.transform.position = (Vector3)ToUnity(trueOrigin);

            var runtime = new ChunkRuntime
            {
                Coord = coord,
                Root = go.transform,
                Tiles = new List<GameObject>(256),
                Props = new List<GameObject>(16)
            };

            BuildChunkFloor(runtime);
            BuildChunkProps(runtime);
            return runtime;
        }

        void BuildChunkFloor(ChunkRuntime chunk)
        {
            var tilesPerSide = Mathf.RoundToInt(ChunkSize / TileSize);
            var trueOrigin = ChunkTrueOrigin(chunk.Coord);
            for (var row = 0; row < tilesPerSide; row++)
            for (var col = 0; col < tilesPerSide; col++)
            {
                var trueX = trueOrigin.x + col * TileSize + TileSize * 0.5f;
                var trueY = trueOrigin.y + row * TileSize + TileSize * 0.5f;
                // Stable across chunk seams: hash absolute true-world tile cells.
                var tileIx = Mathf.FloorToInt(trueX / TileSize);
                var tileIy = Mathf.FloorToInt(trueY / TileSize);
                var tileIndex = Hash2(tileIx, tileIy, _worldSeed);
                var sprite = FloorSprite(_floorKind, tileIndex);
                if (sprite == null) continue;

                var tile = RentTile();
                tile.name = $"Tile_{tileIx}_{tileIy}";
                tile.transform.SetParent(chunk.Root, false);
                tile.transform.localPosition = new Vector3(
                    col * TileSize + TileSize * 0.5f,
                    row * TileSize + TileSize * 0.5f,
                    0f);
                tile.SetActive(true);

                var sr = tile.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = ArenaBounds.FloorSortOrder;
                GameFactory.ApplyFloorMaterialPublic(sr);
                var scale = ArtLibrary.GetTileScale(sprite, TileSize);
                tile.transform.localScale = Vector3.one * scale;
                chunk.Tiles.Add(tile);
            }
        }

        void BuildChunkProps(ChunkRuntime chunk) => RebuildChunkProps(chunk);

        void RebuildChunkProps(ChunkRuntime chunk)
        {
            for (var i = 0; i < chunk.Props.Count; i++)
                ReturnProp(chunk.Props[i]);
            chunk.Props.Clear();

            var trueOrigin = ChunkTrueOrigin(chunk.Coord);
            var trueMax = trueOrigin + new Vector2(ChunkSize, ChunkSize);

            var cellMinX = Mathf.FloorToInt(trueOrigin.x / PropCellSize);
            var cellMaxX = Mathf.FloorToInt((trueMax.x - 0.001f) / PropCellSize);
            var cellMinY = Mathf.FloorToInt(trueOrigin.y / PropCellSize);
            var cellMaxY = Mathf.FloorToInt((trueMax.y - 0.001f) / PropCellSize);

            var densityThreshold = _propBiome switch
            {
                SurvivalMapKind.Inside => 48,
                SurvivalMapKind.Dungeon => 42,
                SurvivalMapKind.Crypt => 42,
                _ => 38
            };

            for (var cy = cellMinY; cy <= cellMaxY; cy++)
            for (var cx = cellMinX; cx <= cellMaxX; cx++)
            {
                var h = Hash2(cx, cy, _worldSeed ^ ((int)_propBiome * 374761));
                // Predetermined occupancy — not random scatter.
                if ((h % 100) >= densityThreshold) continue;

                var jitterX = ((h >> 8) & 255) / 255f - 0.5f;
                var jitterY = ((h >> 16) & 255) / 255f - 0.5f;
                var truePos = new Vector2(
                    (cx + 0.5f) * PropCellSize + jitterX * PropCellSize * 0.35f,
                    (cy + 0.5f) * PropCellSize + jitterY * PropCellSize * 0.35f);

                // Keep fight space around true-world origin clear.
                if (truePos.sqrMagnitude < SpawnClearRadius * SpawnClearRadius) continue;
                // Only place cells whose center falls inside this chunk (no double-spawn on seams).
                if (truePos.x < trueOrigin.x || truePos.x >= trueMax.x
                    || truePos.y < trueOrigin.y || truePos.y >= trueMax.y)
                    continue;

                var local = truePos - trueOrigin;
                var unity = ToUnity(truePos);
                if (!ArenaBounds.IsClearOfObstacles(unity, 0.9f)) continue;

                var prop = RentProp();
                prop.transform.SetParent(chunk.Root, false);
                prop.transform.localPosition = new Vector3(local.x, local.y, 0f);
                prop.SetActive(true);
                ConfigureProp(prop, h, unity);
                chunk.Props.Add(prop);
            }
        }

        void ConfigureProp(GameObject prop, int seed, Vector2 unityPos)
        {
            var sr = prop.GetComponent<SpriteRenderer>();
            var col = prop.GetComponent<CircleCollider2D>();
            if (sr == null) return;

            Sprite sprite;
            float scale;
            var roll = Mathf.Abs(seed % 100) / 100f;
            // Uniform Cainos pixel props on every survival biome.
            // Scales bumped ~30% so trees/props read as real cover vs the player.
            switch (_propBiome)
            {
                case SurvivalMapKind.Inside:
                    // Warded Halls props need ~2× the prior indoor scale to read as furniture/cover.
                    sprite = roll < 0.55f
                        ? ArtLibrary.GetCainosPropSprite(seed) ?? ArtLibrary.GetRockSprite(seed)
                        : ArtLibrary.GetRockSprite(seed ^ 17);
                    scale = 1.7f;
                    break;
                case SurvivalMapKind.Dungeon:
                case SurvivalMapKind.Crypt:
                    sprite = roll < 0.5f
                        ? ArtLibrary.GetRockSprite(seed) ?? ArtLibrary.GetCainosPropSprite(seed)
                        : ArtLibrary.GetCainosPropSprite(seed ^ 23) ?? ArtLibrary.GetRockSprite(seed);
                    scale = 0.9f;
                    break;
                case SurvivalMapKind.Unlimited:
                    // Same Cainos language as Outside — sparse desert props (rocks / dry bush).
                    if (roll < 0.35f)
                    {
                        sprite = ArtLibrary.GetBushSprite(seed) ?? ArtLibrary.GetRockSprite(seed);
                        scale = 0.65f;
                    }
                    else
                    {
                        sprite = ArtLibrary.GetRockSprite(seed ^ 91);
                        scale = 0.8f;
                    }

                    break;
                default:
                    if (roll < 0.42f)
                    {
                        sprite = ArtLibrary.GetTreeSprite(seed);
                        scale = 1.2f;
                    }
                    else if (roll < 0.62f)
                    {
                        sprite = ArtLibrary.GetBushSprite(seed ^ 44) ?? ArtLibrary.GetRockSprite(seed ^ 44);
                        scale = 0.72f;
                    }
                    else
                    {
                        sprite = ArtLibrary.GetRockSprite(seed ^ 91);
                        scale = 0.85f;
                    }

                    break;
            }

            sr.sprite = sprite;
            sr.sortingOrder = ArenaBounds.GetYSortOrder(unityPos.y, 1);
            prop.transform.localScale = Vector3.one * scale;
            if (col != null)
            {
                // Keep world blocker size proportional to the larger visuals.
                col.radius = Mathf.Clamp(0.28f * scale, 0.32f, 0.55f);
                col.isTrigger = false;
            }

            if (prop.GetComponent<ArenaObstacle>() == null)
                prop.AddComponent<ArenaObstacle>();
        }

        void UnloadChunk(ChunkRuntime chunk)
        {
            if (chunk == null) return;
            for (var i = 0; i < chunk.Tiles.Count; i++)
                ReturnTile(chunk.Tiles[i]);
            chunk.Tiles.Clear();
            for (var i = 0; i < chunk.Props.Count; i++)
                ReturnProp(chunk.Props[i]);
            chunk.Props.Clear();
            if (chunk.Root != null)
                Destroy(chunk.Root.gameObject);
        }

        void ClearAllChunks()
        {
            _scratchKeys.Clear();
            foreach (var key in _active.Keys)
                _scratchKeys.Add(key);
            for (var i = 0; i < _scratchKeys.Count; i++)
            {
                if (_active.TryGetValue(_scratchKeys[i], out var chunk))
                    UnloadChunk(chunk);
            }

            _active.Clear();
        }

        void MaybeRebase()
        {
            var pos = (Vector2)_player.position;
            if (pos.magnitude < RebaseThreshold) return;

            var shift = -pos;
            _originOffset += shift;
            ShiftWorld(shift);
            // Logical chunk identity is unchanged — only Unity transforms moved.
            _playerChunk = TrueWorldToChunk(ToTrue(_player.position));
        }

        void ShiftWorld(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return;

            foreach (var kv in _active)
            {
                if (kv.Value.Root != null)
                    kv.Value.Root.position += (Vector3)delta;
            }

            ShiftTransform(_player, delta);

            // Keep the camera with the world — otherwise one LateUpdate shows the void (black flash)
            // and spawn/soft-clamp logic desyncs until the next frame.
            var cam = Camera.main;
            if (cam != null)
                cam.transform.position += (Vector3)delta;

            var companions = Object.FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None);
            for (var i = 0; i < companions.Length; i++)
            {
                if (companions[i] != null)
                    companions[i].TeleportWithLeader(delta);
            }

            ShiftAll<EnemyActor>(delta);
            ShiftAll<LootPickup>(delta);
            ShiftAll<ArenaDoor>(delta);
            ShiftAll<ArenaGateway>(delta);
            ShiftAll<ArenaCryptPortal>(delta);
            ShiftAll<ArenaVictoryGate>(delta);
            ShiftAll<DarkBirdRescue>(delta);
            ShiftAll<DungeonKnightEncounter>(delta);
            ShiftAll<ArrowProjectile>(delta);
            ShiftAll<BossFireProjectile>(delta);
            ShiftAll<EnemyRangedProjectile>(delta);
            ShiftAll<WorldSparkle>(delta);
        }

        static void ShiftAll<T>(Vector2 delta) where T : Component
        {
            var items = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null) continue;
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
                var next = rb.position + delta;
                rb.position = next;
                t.position = new Vector3(next.x, next.y, t.position.z);
                return;
            }

            t.position += (Vector3)delta;
        }

        GameObject RentTile()
        {
            if (_tilePool.Count > 0)
            {
                var t = _tilePool.Pop();
                if (t != null) return t;
            }

            var go = new GameObject("PooledTile");
            go.AddComponent<SpriteRenderer>();
            return go;
        }

        void ReturnTile(GameObject tile)
        {
            if (tile == null) return;
            tile.SetActive(false);
            tile.transform.SetParent(_root, false);
            _tilePool.Push(tile);
        }

        GameObject RentProp()
        {
            if (_propPool.Count > 0)
            {
                var p = _propPool.Pop();
                if (p != null) return p;
            }

            var go = new GameObject("PooledProp");
            go.AddComponent<SpriteRenderer>();
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            go.AddComponent<ArenaObstacle>();
            go.AddComponent<YSortRenderer>().Configure(1);
            return go;
        }

        void ReturnProp(GameObject prop)
        {
            if (prop == null) return;
            prop.SetActive(false);
            prop.transform.SetParent(_root, false);
            _propPool.Push(prop);
        }

        Vector2 ToTrue(Vector2 unity) => unity - _originOffset;
        Vector2 ToTrue(Vector3 unity) => ToTrue((Vector2)unity);
        Vector2 ToUnity(Vector2 trueWorld) => trueWorld + _originOffset;

        static Vector2 ChunkTrueOrigin(Vector2Int coord)
            => new(coord.x * ChunkSize - ChunkSize * 0.5f, coord.y * ChunkSize - ChunkSize * 0.5f);

        static Sprite FloorSprite(SurvivalMapKind kind, int tileIndex) => kind switch
        {
            SurvivalMapKind.Unlimited => ArtLibrary.GetSandTile(tileIndex),
            SurvivalMapKind.Dungeon => ArtLibrary.GetDungeonTile(tileIndex),
            SurvivalMapKind.Crypt => ArtLibrary.GetDungeonTile(tileIndex),
            SurvivalMapKind.Inside => ArtLibrary.GetInsideTile(tileIndex),
            _ => ArtLibrary.GetOutsideTile(tileIndex)
        };

        static int Hash2(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)seed;
                h ^= (uint)(x * 73856093);
                h ^= (uint)(y * 19349663);
                h *= 0x9e3779b9u;
                h ^= h >> 16;
                return (int)(h & 0x7fffffff);
            }
        }

        public static Vector2Int TrueWorldToChunk(Vector2 trueWorld)
        {
            var x = Mathf.FloorToInt((trueWorld.x + ChunkSize * 0.5f) / ChunkSize);
            var y = Mathf.FloorToInt((trueWorld.y + ChunkSize * 0.5f) / ChunkSize);
            return new Vector2Int(x, y);
        }

        /// <summary>Legacy helper — treats the argument as true-world (pre-rebase Unity coincides).</summary>
        public static Vector2Int WorldToChunk(Vector3 worldPos) => TrueWorldToChunk(worldPos);

        static long ChunkToKey(Vector2Int c) => ((long)c.x << 32) ^ (uint)c.y;

        sealed class ChunkRuntime
        {
            public Vector2Int Coord;
            public Transform Root;
            public List<GameObject> Tiles;
            public List<GameObject> Props;
        }
    }
}
