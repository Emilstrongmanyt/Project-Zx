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

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                var pos = new Vector3(originX + col * tileSize, originY + row * tileSize, 0f);
                var isBorder = row == 0 || row == rows - 1 || col == 0 || col == cols - 1;
                var tileIndex = col + row * 7;
                Sprite sprite;
                if (isBorder)
                    sprite = ArtLibrary.WaterTile;
                else if (mapKind == SurvivalMapKind.Inside)
                    sprite = ArtLibrary.GetInsideTile(tileIndex);
                else
                    sprite = ArtLibrary.GetOutsideTile(tileIndex);

                var tileScale = ArtLibrary.GetTileScale(sprite, tileSize);
                var tile = CreateSprite($"Tile_{col}_{row}", sprite, pos, tileScale, -10);
                ApplyFloorMaterial(tile.GetComponent<SpriteRenderer>());
                if (isBorder)
                {
                    tile.AddComponent<WaterTile>();
                    var waterCol = tile.AddComponent<BoxCollider2D>();
                    var tileRenderer = tile.GetComponent<SpriteRenderer>();
                    waterCol.size = tileRenderer != null && tileRenderer.sprite != null
                        ? tileRenderer.sprite.bounds.size
                        : Vector2.one;
                }
                tile.transform.SetParent(root.transform, true);
            }

            return root;
        }

        public static GameObject CreateStoneObstacle(Vector3 position, float scale)
        {
            var go = CreateSprite("Stone", ArtLibrary.Stone, position, scale, 2);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            return go;
        }

        public static GameObject CreateTreeObstacle(Vector3 position, float scale)
        {
            var go = CreateSprite("Tree", ArtLibrary.Tree, position, scale, 3);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.38f;
            return go;
        }

        public static GameObject CreateCampfireObstacle(Vector3 position, float scale = 0.55f)
        {
            var go = CreateSprite("Campfire", ArtLibrary.Campfire, position, scale, 4);
            var glow = new GameObject("CampfireGlow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = Vector3.zero;
            var glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = ArtLibrary.Campfire;
            glowRenderer.color = new Color(1f, 0.55f, 0.15f, 0.35f);
            glowRenderer.sortingOrder = 5;
            glow.transform.localScale = Vector3.one * 1.6f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            return go;
        }

        public static GameObject ScatterArenaObstacles(float arenaWidth, float arenaHeight, int stoneCount, int treeCount, int campfireCount)
        {
            var root = new GameObject("ArenaObstacles");
            var rng = new System.Random(90210);
            var placed = new List<Vector2>();
            var margin = 2.5f;
            var halfW = arenaWidth * 0.5f - margin;
            var halfH = arenaHeight * 0.5f - margin;

            void TryPlace(System.Func<Vector2, GameObject> create, float minSpacing, float minCenterDist, float scaleMultiplier = 1f)
            {
                for (var attempt = 0; attempt < 24; attempt++)
                {
                    var pos = new Vector2(
                        ((float)rng.NextDouble() * 2f - 1f) * halfW,
                        ((float)rng.NextDouble() * 2f - 1f) * halfH);

                    if (pos.magnitude < minCenterDist) continue;

                    var tooClose = false;
                    foreach (var other in placed)
                    {
                        if (Vector2.Distance(other, pos) >= minSpacing) continue;
                        tooClose = true;
                        break;
                    }

                    if (tooClose) continue;

                    placed.Add(pos);
                    var scale = (0.38f + (float)rng.NextDouble() * 0.42f) * scaleMultiplier;
                    var obstacle = create(pos);
                    obstacle.transform.localScale = Vector3.one * scale;
                    obstacle.transform.SetParent(root.transform, true);
                    return;
                }
            }

            for (var i = 0; i < stoneCount; i++)
                TryPlace(pos => CreateStoneObstacle(new Vector3(pos.x, pos.y, 0f), 1f), 2.4f, 6f, 2f);

            for (var i = 0; i < treeCount; i++)
                TryPlace(pos => CreateTreeObstacle(new Vector3(pos.x, pos.y, 0f), 1f), 3f, 5f, 2f);

            for (var i = 0; i < campfireCount; i++)
                TryPlace(pos => CreateCampfireObstacle(new Vector3(pos.x, pos.y, 0f), 0.55f), 4f, 8f);

            return root;
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
                var tile = CreateSprite($"Grass_{col}_{row}", ArtLibrary.GetGrassVariant(col + row * 3), pos, 0.25f, -10);
                tile.transform.SetParent(root.transform, true);
            }

            return root;
        }

        public static GameObject CreateCampfire(Vector3 position)
        {
            return CreateCampfireObstacle(position, 0.45f);
        }

        public static GameObject CreatePlayer(Vector3 position, bool survivalMode, PlayerClass playerClass = PlayerClass.Batter)
        {
            var go = CreateSprite("Player", ArtLibrary.PlayerIdle, position, 0.35f, 10);
            go.tag = "Player";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.useFullKinematicContacts = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            go.AddComponent<TapMovement>().Configure(!survivalMode);
            go.AddComponent<HitFlash>();
            var stats = go.AddComponent<PlayerStats>();
            stats.ConfigureForRun(survivalMode);

            if (survivalMode)
            {
                var selected = playerClass;
                if (selected == PlayerClass.Spearman && !GameSave.SpearmanUnlocked)
                    selected = PlayerClass.Batter;

                if (selected == PlayerClass.Spearman)
                    go.AddComponent<SpearmanCombat>();
                else
                    go.AddComponent<PlayerCombat>();
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
            if (isRoundTwentyBoss) scale *= 2.5f;
            var go = CreateSprite(isBoss ? "Boss" : "Zombie", sprite, position, scale, 5);
            go.tag = "Enemy";

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