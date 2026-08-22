using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Player-centered chunk ring with floating origin. Replaces survival torus wrap.
    /// </summary>
    public sealed class SurvivalChunkStreamer : MonoBehaviour
    {
        public const float ChunkSize = 16f;
        public const int RingRadius = 2; // 5x5 chunks
        public const float RebaseThreshold = 48f;
        public const float TileSize = 1f;

        public static SurvivalChunkStreamer Instance { get; private set; }

        readonly Dictionary<long, ChunkRuntime> _active = new();
        readonly Stack<GameObject> _tilePool = new();
        readonly Stack<GameObject> _propPool = new();
        readonly List<long> _scratchKeys = new();
        readonly List<Vector2Int> _wanted = new();

        Transform _root;
        Transform _player;
        SurvivalMapKind _floorKind;
        SurvivalMapKind _propBiome;
        int _worldSeed;
        Vector2Int _playerChunk;
        Vector2 _logicalOriginShift;

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
            var chunk = WorldToChunk(_player.position);
            if (chunk != _playerChunk)
            {
                _playerChunk = chunk;
                SyncRing();
            }
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
        }

        /// <summary>Unlimited / mid-run biome change: restyle props without nuking underfoot.</summary>
        public void SetPropBiome(SurvivalMapKind propBiome)
        {
            if (_propBiome == propBiome) return;
            _propBiome = propBiome;
            // Rebuild props on all active chunks; keep floor (Unlimited sand stays).
            foreach (var kv in _active)
                RebuildChunkProps(kv.Value);
        }

        public void ForceRefresh()
        {
            if (_player == null) return;
            _playerChunk = WorldToChunk(_player.position);
            SyncRing();
        }

        public bool IsInsideLoaded(Vector2 worldPos)
        {
            return worldPos.x >= LoadedMin.x && worldPos.x <= LoadedMax.x
                   && worldPos.y >= LoadedMin.y && worldPos.y <= LoadedMax.y;
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

            // Fallback: any clear point in loaded ring at mid distance.
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

        void SyncRing()
        {
            _wanted.Clear();
            for (var dy = -RingRadius; dy <= RingRadius; dy++)
            for (var dx = -RingRadius; dx <= RingRadius; dx++)
                _wanted.Add(new Vector2Int(_playerChunk.x + dx, _playerChunk.y + dy));

            _scratchKeys.Clear();
            foreach (var key in _active.Keys)
                _scratchKeys.Add(key);

            for (var i = 0; i < _scratchKeys.Count; i++)
            {
                var key = _scratchKeys[i];
                var coord = KeyToChunk(key);
                var keep = false;
                for (var w = 0; w < _wanted.Count; w++)
                {
                    if (_wanted[w].x == coord.x && _wanted[w].y == coord.y)
                    {
                        keep = true;
                        break;
                    }
                }

                if (!keep && _active.TryGetValue(key, out var chunk))
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
                var x0 = c.x * ChunkSize - ChunkSize * 0.5f;
                var y0 = c.y * ChunkSize - ChunkSize * 0.5f;
                min.x = Mathf.Min(min.x, x0);
                min.y = Mathf.Min(min.y, y0);
                max.x = Mathf.Max(max.x, x0 + ChunkSize);
                max.y = Mathf.Max(max.y, y0 + ChunkSize);
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
            // Chunk (0,0) is centered on world origin so spawn sits in open land.
            var origin = new Vector3(
                coord.x * ChunkSize - ChunkSize * 0.5f,
                coord.y * ChunkSize - ChunkSize * 0.5f,
                0f);
            go.transform.position = origin;

            var runtime = new ChunkRuntime
            {
                Coord = coord,
                Root = go.transform,
                Tiles = new List<GameObject>(256),
                Props = new List<GameObject>(12)
            };

            BuildChunkFloor(runtime);
            BuildChunkProps(runtime);
            return runtime;
        }

        void BuildChunkFloor(ChunkRuntime chunk)
        {
            var tilesPerSide = Mathf.RoundToInt(ChunkSize / TileSize);
            var logical = chunk.Coord;
            for (var row = 0; row < tilesPerSide; row++)
            for (var col = 0; col < tilesPerSide; col++)
            {
                var local = new Vector3(
                    col * TileSize + TileSize * 0.5f,
                    row * TileSize + TileSize * 0.5f,
                    0f);
                var tileIndex = HashTile(logical.x, logical.y, col, row);
                var sprite = FloorSprite(_floorKind, tileIndex);
                if (sprite == null) continue;

                var tile = RentTile();
                tile.name = $"Tile_{col}_{row}";
                tile.transform.SetParent(chunk.Root, false);
                tile.transform.localPosition = local;
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

        void BuildChunkProps(ChunkRuntime chunk)
        {
            RebuildChunkProps(chunk);
        }

        void RebuildChunkProps(ChunkRuntime chunk)
        {
            for (var i = 0; i < chunk.Props.Count; i++)
                ReturnProp(chunk.Props[i]);
            chunk.Props.Clear();

            var rng = new System.Random(ChunkSeed(chunk.Coord));
            var count = _propBiome switch
            {
                SurvivalMapKind.Inside => 5,
                SurvivalMapKind.Dungeon => 6,
                SurvivalMapKind.Crypt => 6,
                _ => 7
            };

            for (var i = 0; i < count; i++)
            {
                var lx = (float)rng.NextDouble() * ChunkSize;
                var ly = (float)rng.NextDouble() * ChunkSize;
                var local = new Vector3(lx, ly, 0f);
                var world = (Vector2)(chunk.Root.position + local);
                // Keep fight space around world origin clear.
                if (world.sqrMagnitude < 4.5f * 4.5f) continue;
                if (!ArenaBounds.IsClearOfObstacles(world, 0.9f)) continue;

                var prop = RentProp();
                prop.transform.SetParent(chunk.Root, false);
                prop.transform.localPosition = local;
                prop.SetActive(true);
                ConfigureProp(prop, rng.Next());
                chunk.Props.Add(prop);
            }
        }

        void ConfigureProp(GameObject prop, int seed)
        {
            var sr = prop.GetComponent<SpriteRenderer>();
            var col = prop.GetComponent<CircleCollider2D>();
            if (sr == null) return;

            Sprite sprite;
            float scale;
            var roll = Mathf.Abs(seed % 100) / 100f;
            switch (_propBiome)
            {
                case SurvivalMapKind.Inside:
                    sprite = roll < 0.55f
                        ? ArtLibrary.GetRandomInsidePropSprite()
                        : roll < 0.8f
                            ? ArtLibrary.GetRandomComputerSprite()
                            : ArtLibrary.GetRandomWarheadSprite();
                    if (sprite == null) sprite = ArtLibrary.Stone;
                    scale = 0.55f;
                    break;
                case SurvivalMapKind.Dungeon:
                case SurvivalMapKind.Crypt:
                    sprite = ArtLibrary.GetRandomCryptSprite() ?? ArtLibrary.Stone;
                    scale = 0.6f;
                    break;
                default:
                    sprite = roll < 0.45f
                        ? ArtLibrary.GetRandomTreeSprite()
                        : ArtLibrary.GetRandomRockSprite();
                    if (sprite == null) sprite = ArtLibrary.Stone;
                    scale = roll < 0.45f ? 0.7f : 0.55f;
                    break;
            }

            sr.sprite = sprite;
            sr.sortingOrder = ArenaBounds.GetYSortOrder(prop.transform.position.y, 1);
            prop.transform.localScale = Vector3.one * scale;
            if (col != null)
            {
                col.radius = 0.35f;
                col.isTrigger = false;
            }

            var obstacle = prop.GetComponent<ArenaObstacle>();
            if (obstacle == null)
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
            _logicalOriginShift += shift;
            ShiftWorld(shift);
            _playerChunk = WorldToChunk(_player.position);
            SyncRing();
        }

        void ShiftWorld(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return;

            // Chunks
            foreach (var kv in _active)
            {
                if (kv.Value.Root != null)
                    kv.Value.Root.position += (Vector3)delta;
            }

            // Player
            ShiftTransform(_player, delta);

            // Companions
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

        static Sprite FloorSprite(SurvivalMapKind kind, int tileIndex) => kind switch
        {
            SurvivalMapKind.Unlimited => ArtLibrary.GetSandTile(tileIndex),
            SurvivalMapKind.Dungeon => ArtLibrary.GetDungeonTile(tileIndex),
            SurvivalMapKind.Crypt => ArtLibrary.GetDungeonTile(tileIndex),
            SurvivalMapKind.Inside => ArtLibrary.GetInsideTile(tileIndex),
            _ => ArtLibrary.GetOutsideTile(tileIndex)
        };

        static int HashTile(int cx, int cy, int col, int row)
            => Mathf.Abs((cx * 73856093) ^ (cy * 19349663) ^ (col * 83492791) ^ (row * 39916801)) % 64;

        int ChunkSeed(Vector2Int coord)
            => _worldSeed ^ (coord.x * 73856093) ^ (coord.y * 19349663) ^ ((int)_propBiome * 83492791);

        public static Vector2Int WorldToChunk(Vector3 worldPos)
        {
            var x = Mathf.FloorToInt((worldPos.x + ChunkSize * 0.5f) / ChunkSize);
            var y = Mathf.FloorToInt((worldPos.y + ChunkSize * 0.5f) / ChunkSize);
            return new Vector2Int(x, y);
        }

        static long ChunkToKey(Vector2Int c) => ((long)c.x << 32) ^ (uint)c.y;

        static Vector2Int KeyToChunk(long key)
        {
            var x = (int)(key >> 32);
            var y = (int)(key & 0xffffffff);
            return new Vector2Int(x, y);
        }

        sealed class ChunkRuntime
        {
            public Vector2Int Coord;
            public Transform Root;
            public List<GameObject> Tiles;
            public List<GameObject> Props;
        }
    }
}
