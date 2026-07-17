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
            var borderDepth = ArenaBounds.WaterBorderDepth;

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                var pos = new Vector3(originX + col * tileSize, originY + row * tileSize, 0f);
                var isBorder = row < borderDepth || row >= rows - borderDepth
                    || col < borderDepth || col >= cols - borderDepth;
                var tileIndex = col + row * 7;
                Sprite sprite;
                if (isBorder)
                    sprite = ArtLibrary.WaterTile;
                else if (mapKind == SurvivalMapKind.Dungeon)
                    sprite = ArtLibrary.GetDungeonTile(tileIndex);
                else if (mapKind == SurvivalMapKind.Inside)
                    sprite = ArtLibrary.GetInsideTile(tileIndex);
                else
                    sprite = ArtLibrary.GetOutsideTile(tileIndex);

                var tileScale = ArtLibrary.GetTileScale(sprite, tileSize);
                var sortOrder = isBorder ? ArenaBounds.WaterSortOrder : ArenaBounds.FloorSortOrder;
                var tile = CreateSprite($"Tile_{col}_{row}", sprite, pos, tileScale, sortOrder);
                var tileRenderer = tile.GetComponent<SpriteRenderer>();
                if (!isBorder)
                    ApplyFloorMaterial(tileRenderer);
                else
                {
                    tile.AddComponent<WaterTile>();
                    var waterCol = tile.AddComponent<BoxCollider2D>();
                    // Local size so world collider matches one tile after scale is applied.
                    var scale = Mathf.Max(0.001f, tile.transform.localScale.x);
                    waterCol.size = Vector2.one * (tileSize / scale);
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
            ScatterPlaced.Clear();
            var root = new GameObject("ArenaObstacles");
            var rng = new System.Random(90210);

            for (var i = 0; i < stoneCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.4f, 6f, 1.7f,
                    pos => CreateStoneObstacle(new Vector3(pos.x, pos.y, 0f), 1f));

            for (var i = 0; i < treeCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 3f, 5f, 1.7f,
                    pos => CreateTreeObstacle(new Vector3(pos.x, pos.y, 0f), 1f));

            for (var i = 0; i < campfireCount; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 4f, 8f, 1f,
                    pos => CreateCampfireObstacle(new Vector3(pos.x, pos.y, 0f), 0.55f));

            return root;
        }

        public static GameObject ScatterInsideObstacles(float arenaWidth, float arenaHeight)
        {
            ScatterPlaced.Clear();
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
            ScatterPlaced.Clear();
            var root = new GameObject("CryptObstacles");
            var rng = new System.Random(90212);
            var cryptBag = new SpriteVariantBag(ArtLibrary.CryptVariants, rng);

            for (var i = 0; i < 22; i++)
                TryPlaceObstacle(root.transform, rng, arenaWidth, arenaHeight, 2.5f, 5f, 1.8f,
                    pos => CreatePropObstacle("CryptProp", cryptBag.Pick(), new Vector3(pos.x, pos.y, 0f), 1f, 0.34f));

            return root;
        }

        static readonly List<Vector2> ScatterPlaced = new();

        static void TryPlaceObstacle(
            Transform parent,
            System.Random rng,
            float arenaWidth,
            float arenaHeight,
            float minSpacing,
            float minCenterDist,
            float scaleMultiplier,
            System.Func<Vector2, GameObject> create)
        {
            var margin = 2.5f;
            var halfW = arenaWidth * 0.5f - margin;
            var halfH = arenaHeight * 0.5f - margin;

            for (var attempt = 0; attempt < 24; attempt++)
            {
                var pos = new Vector2(
                    ((float)rng.NextDouble() * 2f - 1f) * halfW,
                    ((float)rng.NextDouble() * 2f - 1f) * halfH);

                if (pos.magnitude < minCenterDist) continue;

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

        public static GameObject CreateGrassField(string name, float width, float height, float tileSize = 1f)
        {
            var root = new GameObject(name);
            var cols = Mathf.CeilToInt(width / tileSize);
            var rows = Mathf.CeilToInt(height / tileSize);
            var originX = -(cols * tileSize) * 0.5f + tileSize * 0.5f;
            var originY = -(rows * tileSize) * 0.5f + tileSize * 0.5f;

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                var pos = new Vector3(originX + col * tileSize, originY + row * tileSize, 0f);
                var tile = CreateSprite($"Grass_{col}_{row}", ArtLibrary.GetGrassVariant(col + row * 3), pos, 0.25f, ArenaBounds.FloorSortOrder);
                tile.transform.SetParent(root.transform, true);
            }

            return root;
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

            go.AddComponent<TapMovement>().Configure(!survivalMode, sanitizedHero);
            go.AddComponent<HitFlash>();
            var stats = go.AddComponent<PlayerStats>();
            stats.ConfigureForRun(survivalMode);

            if (survivalMode)
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
                    default:
                        go.AddComponent<PlayerCombat>();
                        break;
                }
            }

            return go;
        }

        public static GameObject CreateEnemy(Vector3 position, int round, bool isBoss, bool isRoundTwentyBoss = false, EnemyZombieKind zombieKind = EnemyZombieKind.Outside)
        {
            Sprite sprite;
            if (isBoss)
                sprite = ArtLibrary.Boss;
            else
                ArtLibrary.GetZombieSprites(zombieKind, out sprite, out _);

            var scale = (isBoss ? 0.55f : 0.32f * 2.5f) * 1.5f;
            if (isBoss) scale *= 1.5f;
            if (isRoundTwentyBoss) scale *= 2.5f;
            var go = CreateSprite(isBoss ? "Boss" : "Zombie", sprite, position, scale, 0);
            go.tag = "Enemy";
            go.AddComponent<YSortRenderer>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.useFullKinematicContacts = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = isRoundTwentyBoss ? 1.1f : isBoss ? 0.7f : 0.4f;

            go.AddComponent<HitFlash>();
            var enemy = go.AddComponent<EnemyActor>();
            enemy.Initialize(round, isBoss, isRoundTwentyBoss, zombieKind);
            return go;
        }

        public static GameObject CreateNpc(string name, Sprite sprite, Vector3 position, System.Action onInteract)
        {
            var go = CreateSprite(name, sprite, position, 0.38f, 6);
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
            var go = CreateSprite("RowZiUnlockNpc", ArtLibrary.GetHeroIdleSprite(PlayableHero.RowZi), position, 0.4f, 9);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;
            col.isTrigger = true;
            go.AddComponent<NpcInteractable>().Initialize(() =>
            {
                if (GameSave.RowZiUnlocked) return;
                GameSave.RowZiUnlocked = true;
                Achievements.UnlockTogetherAgain();
            });
            return go;
        }

        public static GameObject CreatePickup(Vector3 position, PickupType type, int amount)
        {
            var name = type switch
            {
                PickupType.Xp => "XpPickup",
                PickupType.HpPotion => "HpPotionPickup",
                _ => "GoldPickup"
            };

            var go = new GameObject(name);
            go.transform.position = position;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = type == PickupType.Xp ? 0.55f : 0.85f;
            go.AddComponent<LootPickup>().Initialize(type, amount);
            return go;
        }

        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}