using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.UI;
using ProjectZx.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectZx.Core
{
    public static class GameFactory
    {
        static Material _floorMaterial;

        public static GameObject CreateSprite(string name, Sprite sprite, Vector3 position, float scale = 1f, int sortingOrder = 0)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        static void ApplyFloorMaterial(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            _floorMaterial ??= CreateFloorMaterial();
            if (_floorMaterial != null)
                renderer.sharedMaterial = _floorMaterial;
        }

        static Material CreateFloorMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        public static GameObject CreateTiledField(string name, float width, float height, SurvivalMapKind mapKind, float tileSize = 1f)
        {
            var root = new GameObject(name);
            var cols = Mathf.CeilToInt(width / tileSize);
            var rows = Mathf.CeilToInt(height / tileSize);
            var originX = -(cols * tileSize) * 0.5f + tileSize * 0.5f;
            var originY = -(rows * tileSize) * 0.5f + tileSize * 0.5f;
            var borderDepth = Mathf.Max(1, ArenaBounds.WaterBorderDepth);
            // Cache once so every border cell uses the same water sprite (no land fallback).
            var waterSprite = ArtLibrary.WaterTile;
            if (waterSprite == null)
                Debug.LogError("[GameFactory] Water tile sprite failed to load; borders will be missing.");

            var isDungeonLike = mapKind == SurvivalMapKind.Dungeon || mapKind == SurvivalMapKind.Crypt;

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                var pos = new Vector3(originX + col * tileSize, originY + row * tileSize, 0f);
                var isBorder = row < borderDepth || row >= rows - borderDepth
                    || col < borderDepth || col >= cols - borderDepth;
                var tileIndex = col + row * 7;
                Sprite sprite;
                // Crypt/Dungeon borders use dark stone (not water/grass) so portals never look outdoor.
                if (isBorder && isDungeonLike)
                    sprite = ArtLibrary.GetDungeonTile(tileIndex + 3);
                else if (isBorder)
                    sprite = waterSprite;
                else if (mapKind == SurvivalMapKind.Unlimited)
                    sprite = ArtLibrary.GetSandTile(tileIndex);
                else if (isDungeonLike)
                    sprite = ArtLibrary.GetDungeonTile(tileIndex);
                else if (mapKind == SurvivalMapKind.Inside)
                    sprite = ArtLibrary.GetInsideTile(tileIndex);
                else
                    sprite = ArtLibrary.GetOutsideTile(tileIndex);

                if (sprite == null) continue;

                var tileScale = ArtLibrary.GetTileScale(sprite, tileSize);
                var sortOrder = isBorder ? ArenaBounds.WaterSortOrder : ArenaBounds.FloorSortOrder;
                var tile = CreateSprite(
                    isBorder
                        ? (isDungeonLike ? $"Wall_{col}_{row}" : $"Water_{col}_{row}")
                        : $"Tile_{col}_{row}",
                    sprite,
                    pos,
                    tileScale,
                    sortOrder);
                var tileRenderer = tile.GetComponent<SpriteRenderer>();
                // URP 2D needs an explicit sprite material; without it water can vanish on device.
                ApplyFloorMaterial(tileRenderer);
                if (isBorder)
                {
                    // Keep WaterTile component so existing collision/clamp logic stays consistent.
                    tile.AddComponent<WaterTile>();
                    var waterCol = tile.AddComponent<BoxCollider2D>();
                    // Local size so world collider matches one tile after scale is applied.
                    var scale = Mathf.Max(0.001f, tile.transform.localScale.x);
                    waterCol.size = Vector2.one * (tileSize / scale);
                    waterCol.isTrigger = false;
                    // Darken border ring so walls read as solid crypt perimeter.
                    if (isDungeonLike && tileRenderer != null)
                        tileRenderer.color = new Color(0.55f, 0.5f, 0.48f, 1f);
                }
                tile.transform.SetParent(root.transform, true);
            }

            return root;
        }

        public static GameObject CreatePropObstacle(string name, Sprite sprite, Vector3 position, float scale, float colliderRadius = 0.34f)
        {
            var go = CreateSprite(name, sprite, position, scale, 0);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;
            go.AddComponent<ArenaObstacle>();
            go.AddComponent<YSortRenderer>();
            return go;
        }

        public static GameObject CreateStoneObstacle(Vector3 position, float scale, Sprite sprite = null)
        {
            var go = CreateSprite("Stone", sprite ?? ArtLibrary.GetRandomRockSprite(), position, scale, 0);
            go.AddComponent<ArenaObstacle>();
            go.AddComponent<StoneObstacle>();
            return go;
        }

        public static GameObject CreateTreeObstacle(Vector3 position, float scale, Sprite sprite = null)
        {
            var go = CreateSprite("Tree", sprite ?? ArtLibrary.GetRandomTreeSprite(), position, scale, 0);
            go.AddComponent<ArenaObstacle>();
            go.AddComponent<TreeObstacle>();
            return go;
        }

        public static GameObject CreateCampfireObstacle(Vector3 position, float scale = 0.55f)
        {
            var go = CreateSprite("Campfire", ArtLibrary.Campfire, position, scale, 0);
            go.AddComponent<YSortRenderer>();
            var glow = new GameObject("CampfireGlow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = Vector3.zero;
            var glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = ArtLibrary.Campfire;
            glowRenderer.color = new Color(1f, 0.55f, 0.15f, 0.35f);
            glow.AddComponent<YSortRenderer>().Configure(1);
            glow.transform.localScale = Vector3.one * 1.6f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            go.AddComponent<ArenaObstacle>();
            return go;
        }

        public static GameObject ScatterArenaObstacles(float arenaWidth, float arenaHeight, int stoneCount, int treeCount, int campfireCount)
        {
            BeginScatterPass();
            var root = new GameObject("ArenaObstacles");
            var rng = new System.Random(90210);

            // Extra footprint padding so large tree crowns cannot cover reserved NPC pads.
            for (var i = 0; i < stoneCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.6f, 6f, 1.7f,
                    pos => CreateStoneObstacle(new Vector3(pos.x, pos.y, 0f), 1f), footprintPadding: 1.1f);

            for (var i = 0; i < treeCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 3.2f, 5f, 1.7f,
                    pos => CreateTreeObstacle(new Vector3(pos.x, pos.y, 0f), 1f), footprintPadding: 1.8f);

            for (var i = 0; i < campfireCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 4f, 8f, 1f,
                    pos => CreateCampfireObstacle(new Vector3(pos.x, pos.y, 0f), 0.55f), footprintPadding: 0.8f);

            PruneObstaclesInClearings(root.transform);
            return root;
        }

        public static GameObject ScatterInsideObstacles(float arenaWidth, float arenaHeight)
        {
            BeginScatterPass();
            ReserveClearing(Vector2.zero, 4f);
            var root = new GameObject("InsideObstacles");
            var rng = new System.Random(90211);
            var insideBag = new SpriteVariantBag(ArtLibrary.InsidePropVariants, rng);
            var computerBag = new SpriteVariantBag(ArtLibrary.ComputerVariants, rng);
            var warheadBag = new SpriteVariantBag(ArtLibrary.WarheadVariants, rng);

            for (var i = 0; i < 12; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.6f, 5f, 1.8f,
                    pos => CreatePropObstacle("InsideProp", insideBag.Pick(), new Vector3(pos.x, pos.y, 0f), 1f, 0.32f));

            for (var i = 0; i < 8; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.8f, 5f, 1.6f,
                    pos => CreatePropObstacle("Computer", computerBag.Pick(), new Vector3(pos.x, pos.y, 0f), 1f, 0.36f));

            for (var i = 0; i < 6; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 3f, 6f, 1.7f,
                    pos => CreatePropObstacle("Warhead", warheadBag.Pick(), new Vector3(pos.x, pos.y, 0f), 1f, 0.3f));

            return root;
        }

        public static GameObject ScatterCryptObstacles(float arenaWidth, float arenaHeight)
        {
            BeginScatterPass();
            ReserveClearing(Vector2.zero, 4f);
            var root = new GameObject("CryptObstacles");
            var rng = new System.Random(90212);
            // CryptSheet tombs + RA crypt props + ElvGames dungeon walls/torches/traps/boulders.
            var cryptBag = new SpriteVariantBag(ArtLibrary.CryptVariants, rng);

            for (var i = 0; i < 28; i++)
            {
                var scale = 0.95f + (float)rng.NextDouble() * 0.45f;
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.4f, 5f, 1.7f,
                    pos => CreatePropObstacle("CryptProp", cryptBag.Pick(), new Vector3(pos.x, pos.y, 0f), scale, 0.34f),
                    footprintPadding: 0.9f);
            }

            return root;
        }

        static readonly List<Vector2> ScatterPlaced = new();
        static readonly List<Vector2> ScatterClearingCenters = new();
        static readonly List<float> ScatterClearingRadii = new();

        /// <summary>Start a scatter pass. Call <see cref="ReserveClearing"/> first for NPC/spawn zones.</summary>
        public static void BeginScatterPass()
        {
            ScatterPlaced.Clear();
            // Keep reserved clearings until the next explicit reset of reservations.
        }

        public static void ClearScatterReservations()
        {
            ScatterClearingCenters.Clear();
            ScatterClearingRadii.Clear();
            ScatterPlaced.Clear();
        }

        /// <summary>Keep trees/rocks away from NPCs, campfire, and player spawns.</summary>
        public static void ReserveClearing(Vector2 center, float radius)
        {
            ScatterClearingCenters.Add(center);
            ScatterClearingRadii.Add(Mathf.Max(0.5f, radius));
        }

        static bool IsInsideReservedClearing(Vector2 pos, float footprintPadding = 0f)
        {
            for (var i = 0; i < ScatterClearingCenters.Count; i++)
            {
                var limit = ScatterClearingRadii[i] + footprintPadding;
                if (Vector2.Distance(pos, ScatterClearingCenters[i]) < limit)
                    return true;
            }

            return false;
        }

        /// <summary>Destroy any obstacle that still overlaps a reserved NPC/spawn clearing.</summary>
        public static void PruneObstaclesInClearings(Transform root = null)
        {
            if (ScatterClearingCenters.Count == 0) return;

            var obstacles = Object.FindObjectsByType<ArenaObstacle>();
            for (var i = 0; i < obstacles.Length; i++)
            {
                var obstacle = obstacles[i];
                if (obstacle == null) continue;
                if (root != null && !obstacle.transform.IsChildOf(root) && obstacle.transform != root)
                    continue;

                var pos = (Vector2)obstacle.transform.position;
                // Trees are tall/wide after scale — use generous prune radius.
                var isTree = obstacle.GetComponent<TreeObstacle>() != null;
                var pad = isTree ? 2.0f : 1.2f;
                if (!IsInsideReservedClearing(pos, pad)) continue;
                Object.Destroy(obstacle.gameObject);
            }
        }

        static void TryPlaceObstacle(
            Transform parent,
            System.Random rng,
            float arenaWidth,
            float arenaHeight,
            float minSpacing,
            float minCenterDist,
            float scaleMultiplier,
            System.Func<Vector2, GameObject> create,
            float footprintPadding = 0f)
        {
            var margin = 2.5f;
            var halfW = arenaWidth * 0.5f - margin;
            var halfH = arenaHeight * 0.5f - margin;

            for (var attempt = 0; attempt < 64; attempt++)
            {
                var pos = new Vector2(
                    ((float)rng.NextDouble() * 2f - 1f) * halfW,
                    ((float)rng.NextDouble() * 2f - 1f) * halfH);

                if (pos.magnitude < minCenterDist) continue;
                if (IsInsideReservedClearing(pos, footprintPadding)) continue;

                var tooClose = false;
                foreach (var other in ScatterPlaced)
                {
                    if (Vector2.Distance(other, pos) >= minSpacing) continue;
                    tooClose = true;
                    break;
                }

                if (tooClose) continue;

                ScatterPlaced.Add(pos);
                var scale = (0.38f + (float)rng.NextDouble() * 0.42f) * scaleMultiplier;
                var obstacle = create(pos);
                obstacle.transform.localScale = Vector3.one * scale;
                obstacle.transform.SetParent(parent, true);
                return;
            }
        }

        sealed class SpriteVariantBag
        {
            readonly Sprite[] _sprites;
            readonly System.Random _rng;
            readonly List<int> _remaining = new();

            public SpriteVariantBag(Sprite[] sprites, System.Random rng)
            {
                _sprites = sprites;
                _rng = rng;
                Refill();
            }

            public Sprite Pick()
            {
                if (_remaining.Count == 0) Refill();
                var index = _rng.Next(_remaining.Count);
                var spriteIndex = _remaining[index];
                _remaining.RemoveAt(index);
                return _sprites[spriteIndex];
            }

            void Refill()
            {
                _remaining.Clear();
                if (_sprites == null) return;

                for (var i = 0; i < _sprites.Length; i++)
                {
                    if (_sprites[i] != null) _remaining.Add(i);
                }
            }
        }

        public static GameObject CreateArenaDoor(Vector3 position)
        {
            var go = CreateSprite("ArenaDoor", ArtLibrary.Door, position, 0.9f, 8);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.1f, 1.8f);
            col.isTrigger = true;
            return go;
        }

        public static GameObject CreateArenaGateway(Vector3 position)
        {
            var go = CreateSprite("ArenaGateway", ArtLibrary.Gateway, position, 1.15f, 10);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 2.1f);
            col.isTrigger = true;
            return go;
        }

        public static GameObject CreateArenaVictoryGate(Vector3 position)
        {
            var go = CreateSprite("ArenaVictoryGate", ArtLibrary.Gateway, position, 1.2f, 10);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 2.1f);
            col.isTrigger = true;
            // Slight gold tint so it reads as a return portal.
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(1f, 0.92f, 0.55f, 1f);
            return go;
        }

        public static GameObject CreateArenaCryptPortal(Vector3 position)
        {
            var go = CreateSprite("ArenaCryptPortal", ArtLibrary.Gateway, position, 1.18f, 10);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 2.1f);
            col.isTrigger = true;
            // Cool violet so it reads as the Crypt descent portal.
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(0.72f, 0.55f, 1f, 1f);
            return go;
        }

        /// <summary>
        /// Rebuild floor + obstacles for Unlimited mid-run biome transitions.
        /// </summary>
        public static void RebuildSurvivalEnvironment(SurvivalMapKind visualBiome)
        {
            DestroyNamed("OutsideFloor");
            DestroyNamed("InsideFloor");
            DestroyNamed("DungeonFloor");
            DestroyNamed("CryptFloor");
            DestroyNamed("UnlimitedFloor");
            DestroyNamed("ArenaObstacles");
            DestroyNamed("InsideObstacles");
            DestroyNamed("CryptObstacles");

            const float arenaW = ArenaBounds.ArenaWidth;
            const float arenaH = ArenaBounds.ArenaHeight;
            var isInside = visualBiome == SurvivalMapKind.Inside;
            var isDungeon = visualBiome == SurvivalMapKind.Dungeon;
            var isCrypt = visualBiome == SurvivalMapKind.Crypt;
            // Unlimited keeps SandTile for every biome phase; props still follow visualBiome.
            var floorKind = GameSessionContext.SurvivalMap == SurvivalMapKind.Unlimited
                ? SurvivalMapKind.Unlimited
                : visualBiome;
            var floorName = floorKind == SurvivalMapKind.Unlimited
                ? "UnlimitedFloor"
                : isCrypt ? "CryptFloor"
                : isDungeon ? "DungeonFloor"
                : isInside ? "InsideFloor"
                : "OutsideFloor";

            CreateTiledField(floorName, arenaW, arenaH, floorKind, 1f);

            ClearScatterReservations();
            ReserveClearing(Vector2.zero, 4.5f);
            if (isInside)
                ScatterInsideObstacles(arenaW, arenaH);
            else if (isDungeon || isCrypt)
                ScatterCryptObstacles(arenaW, arenaH);
            else
                ScatterArenaObstacles(arenaW, arenaH, 14, 10, 3);

            var cam = Camera.main;
            if (cam != null)
            {
                if (GameSessionContext.SurvivalMap == SurvivalMapKind.Unlimited)
                    cam.backgroundColor = new Color(0.55f, 0.45f, 0.28f);
                else
                    cam.backgroundColor = isDungeon || isCrypt
                        ? new Color(0.08f, 0.07f, 0.1f)
                        : isInside
                            ? new Color(0.2f, 0.16f, 0.12f)
                            : new Color(0.1f, 0.2f, 0.48f);
            }
        }

        static void DestroyNamed(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.Destroy(go);
        }

        /// <summary>
        /// Outdoor camp / hub ground with the same water ring as survival arenas.
        /// </summary>
        public static GameObject CreateGrassField(string name, float width, float height, float tileSize = 1f)
        {
            return CreateTiledField(name, width, height, SurvivalMapKind.Outside, tileSize);
        }

        public static GameObject CreateCampfire(Vector3 position)
        {
            return CreateCampfireObstacle(position, 0.45f);
        }

        public static GameObject CreatePlayer(
            Vector3 position,
            bool survivalMode,
            PlayerClass playerClass = PlayerClass.Batter,
            PlayableHero hero = PlayableHero.RollZy,
            float scale = 0.42f * 1.3f)
        {
            var sanitizedHero = GameSave.SanitizeHero(hero);
            var go = CreateSprite("Player", ArtLibrary.GetHeroIdleSprite(sanitizedHero), position, scale, 0);
            go.tag = "Player";
            go.AddComponent<YSortRenderer>().Configure(2);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.useFullKinematicContacts = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            // Survival still needs NPC taps (RowZi unlock at the R20 door).
            go.AddComponent<TapMovement>().Configure(true, sanitizedHero);
            go.AddComponent<HitFlash>();
            var stats = go.AddComponent<PlayerStats>();
            stats.ConfigureForRun(survivalMode);

            if (survivalMode)
            {
                AttachCombatForClass(go, playerClass);
                if (GameSave.FlameEnchantUnlocked)
                    Combat.FlameEnchantVfx.Attach(go.transform, Combat.FlameEnchantVfx.FlameKind.Weapon, new Vector3(0.35f, 0.15f, 0f), 0.7f);
            }

            return go;
        }

        /// <summary>
        /// Inactive hero companion for survival — follows the player, uses that hero's loadout,
        /// deals 20% damage, and collects loot for the leader.
        /// </summary>
        public static GameObject CreateCompanion(
            Transform leader,
            PlayerStats leaderStats,
            PlayableHero hero,
            PlayerClass playerClass,
            // ~25% smaller than the previous companion scale so the standby hero reads as support.
            float scale = 0.42f * 1.3f * 0.92f * 0.75f)
        {
            if (leader == null || leaderStats == null) return null;

            var sanitizedHero = GameSave.SanitizeHero(hero);
            var spawn = leader.position + Vector3.left * 1.5f;
            var go = CreateSprite(
                $"Companion_{GameSave.GetHeroDisplayName(sanitizedHero)}",
                ArtLibrary.GetHeroIdleSprite(sanitizedHero),
                spawn,
                scale,
                0);
            // No Player tag — enemies only chase the main hero.
            go.AddComponent<YSortRenderer>().Configure(2);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.useFullKinematicContacts = false;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            go.AddComponent<HitFlash>();
            var stats = go.AddComponent<PlayerStats>();
            stats.ConfigureAsCompanion(leaderStats);

            AttachCombatForClass(go, playerClass);

            var follower = go.AddComponent<CompanionFollower>();
            follower.Bind(leader, leaderStats, sanitizedHero);
            return go;
        }

        static void AttachCombatForClass(GameObject go, PlayerClass playerClass)
        {
            var selected = GameSave.SanitizeClass(playerClass);
            switch (selected)
            {
                case PlayerClass.Spearman:
                    go.AddComponent<SpearmanCombat>();
                    break;
                case PlayerClass.Bowman:
                    go.AddComponent<BowmanCombat>();
                    break;
                case PlayerClass.Magician:
                    go.AddComponent<MagicianCombat>();
                    break;
                case PlayerClass.Samurai:
                    go.AddComponent<SamuraiCombat>();
                    break;
                default:
                    go.AddComponent<PlayerCombat>();
                    break;
            }
        }

        public static GameObject CreateEnemy(
            Vector3 position,
            int round,
            bool isBoss,
            bool isRoundTwentyBoss = false,
            EnemyZombieKind zombieKind = EnemyZombieKind.Outside,
            bool isRoundThirtyBoss = false,
            bool isRoundFortyBoss = false,
            bool isRanged = false,
            bool isRoundFiftyBoss = false,
            bool isElite = false)
        {
            Sprite sprite;
            if (isRoundFiftyBoss)
                sprite = ArtLibrary.GetMinotaurBossAnimSet().Idle ?? ArtLibrary.BossB;
            else if (isRoundFortyBoss)
                sprite = ArtLibrary.GetLordBossAnimSet(highPhase: true).Idle ?? ArtLibrary.BossB;
            else if (isBoss)
                sprite = ArtLibrary.GetGolemBossAnimSet().Idle ?? ArtLibrary.Boss;
            else if (isRanged)
            {
                var set = ArtLibrary.GetRangedEnemyAnimSet();
                sprite = set.Idle;
                if (sprite == null)
                    ArtLibrary.GetZombieSprites(zombieKind, out sprite, out _);
            }
            else
            {
                var set = ArtLibrary.GetEnemyAnimSet(zombieKind);
                sprite = set.Idle;
                if (sprite == null)
                    ArtLibrary.GetZombieSprites(zombieKind, out sprite, out _);
            }

            var isStageBoss = isRoundTwentyBoss || isRoundThirtyBoss || isRoundFortyBoss || isRoundFiftyBoss;
            // Sanctum 96–128px sprites: previous footprint ×4 so demons read at combat scale.
            var scale = (isBoss ? 0.55f : 0.32f * 2.5f) * 1.5f * 4f;
            if (isBoss) scale *= 1.5f;
            // Stage bosses share the same large combat footprint.
            if (isStageBoss) scale *= 2.5f;
            // Minotaur sheet is denser; pull scale back slightly so it fits the arena.
            if (isRoundFiftyBoss)
                scale *= 0.72f;
            // R10 / R20 decade bosses were reading too small after the 4× pass.
            if (isBoss && (round == 10 || round == 20) && !isRoundFiftyBoss)
                scale *= 1.7f;
            // Outside survival R20 stage boss was still ~2× too large in TestFlight.
            if (isRoundTwentyBoss)
                scale *= 0.5f;
            // Post-R20 elites read larger than trash packs.
            if (isElite && !isBoss)
                scale *= 1.3f;
            var go = CreateSprite(
                isBoss ? "Boss" : isElite ? "EliteDemon" : isRanged ? "RangedDemon" : "Demon",
                sprite,
                position,
                scale,
                0);
            go.tag = "Enemy";
            go.AddComponent<YSortRenderer>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // No solid enemy-enemy contacts — swarms must stack so every zombie can reach the player.
            rb.useFullKinematicContacts = false;

            // Visual scale is large; keep WORLD hitboxes body-sized (local radius = world / scale).
            var worldHitRadius = isStageBoss ? 1.35f : isBoss ? 0.95f : isElite ? 0.65f : 0.55f;
            if (isBoss && (round == 10 || round == 20) && !isRoundFiftyBoss)
                worldHitRadius *= 1.35f;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = worldHitRadius / Mathf.Max(0.001f, scale);
            col.offset = new Vector2(0f, 0.05f);
            col.isTrigger = true;

            go.AddComponent<HitFlash>();
            var enemy = go.AddComponent<EnemyActor>();
            enemy.Initialize(
                round,
                isBoss,
                isRoundTwentyBoss,
                zombieKind,
                isRoundThirtyBoss,
                isRoundFortyBoss,
                isRanged,
                isRoundFiftyBoss,
                isElite);
            return go;
        }

        public static GameObject CreateNpc(string name, Sprite sprite, Vector3 position, System.Action onInteract, float scale = 0.38f)
        {
            var go = CreateSprite(name, sprite, position, scale, 6);
            go.AddComponent<YSortRenderer>().Configure(3);
            var proximity = go.AddComponent<CircleCollider2D>();
            proximity.isTrigger = true;
            proximity.radius = 2.8f;
            go.AddComponent<NpcInteractable>().Initialize(onInteract);
            return go;
        }

        public static GameObject CreateHeroCampNpc(Vector3 position, PlayableHero hero, float scale = 0.38f)
        {
            var go = CreateSprite($"{GameSave.GetHeroDisplayName(hero)}CampNpc", ArtLibrary.GetHeroIdleSprite(hero), position, scale, 9);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.55f;
            col.isTrigger = true;
            go.AddComponent<NpcInteractable>();
            go.AddComponent<HeroCampNpc>().Initialize(hero);
            return go;
        }

        public static GameObject CreateRowZiUnlockNpc(Vector3 position)
        {
            var go = CreateSprite("RowZiUnlockNpc", ArtLibrary.GetHeroIdleSprite(PlayableHero.RowZi), position, 0.55f, 12);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.85f;
            col.isTrigger = true;
            go.AddComponent<YSortRenderer>().Configure(4);
            go.AddComponent<NpcInteractable>().Initialize(() =>
            {
                if (GameSave.RowZiUnlocked)
                {
                    GameHud.Instance?.ShowBanner("RowZi is already at camp — tap her to swap!", 2.5f);
                    return;
                }

                GameSave.RowZiUnlocked = true;
                Achievements.UnlockTogetherAgain();
                // Keep current hero; standby RowZi appears after return to camp.
                GameHud.Instance?.ShowBanner("RowZi unlocked! Swap heroes at camp.", 3.5f);
            });
            return go;
        }

        public static GameObject CreatePickup(Vector3 position, PickupType type, int amount)
        {
            var name = type switch
            {
                PickupType.Xp => "XpPickup",
                PickupType.HpPotion => "HpPotionPickup",
                PickupType.MapLoot => "MapLootPickup",
                PickupType.Equipment => "EquipmentPickup",
                PickupType.EpicCrystal => "EpicCrystalPickup",
                _ => "GoldPickup"
            };

            var go = new GameObject(name);
            go.transform.position = position;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = type == PickupType.Xp ? 0.55f
                : type == PickupType.MapLoot ? 0.75f
                : type == PickupType.Equipment ? 0.8f
                : type == PickupType.EpicCrystal ? 0.85f
                : 0.85f;
            go.AddComponent<LootPickup>().Initialize(type, amount);
            return go;
        }

        public static GameObject CreateEquipmentPickup(Vector3 position, EquipmentId equipmentId)
        {
            var go = CreatePickup(position, PickupType.Equipment, (int)equipmentId);
            return go;
        }

        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}