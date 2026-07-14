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

        public static GameObject CreateGround(string name, Color tint, float width, float height)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtLibrary.Ground;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(width, height);
            renderer.sortingOrder = -10;
            return go;
        }

        public static GameObject CreateArenaField(string name, float width, float height, float tileSize = 1f)
        {
            var root = new GameObject(name);
            var cols = Mathf.CeilToInt(width / tileSize);
            var rows = Mathf.CeilToInt(height / tileSize);
            var originX = -(cols * tileSize) * 0.5f + tileSize * 0.5f;
            var originY = -(rows * tileSize) * 0.5f + tileSize * 0.5f;

            var baseDirt = new Color(0.3f, 0.22f, 0.15f);
            var darkDirt = new Color(0.24f, 0.17f, 0.11f);
            var edgeDirt = new Color(0.16f, 0.11f, 0.08f);

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                var pos = new Vector3(originX + col * tileSize, originY + row * tileSize, 0f);
                var tile = CreateSprite($"Arena_{col}_{row}", ArtLibrary.Ground, pos, 0.22f, -10);
                var renderer = tile.GetComponent<SpriteRenderer>();
                var isEdge = row == 0 || row == rows - 1 || col == 0 || col == cols - 1;
                var variant = (col + row) % 3;
                renderer.color = isEdge ? edgeDirt : variant == 0 ? baseDirt : darkDirt;
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

        public static GameObject ScatterArenaObstacles(float arenaWidth, float arenaHeight, int count)
        {
            var root = new GameObject("ArenaObstacles");
            var rng = new System.Random(90210);
            var placed = new List<Vector2>();
            var margin = 2.5f;
            var halfW = arenaWidth * 0.5f - margin;
            var halfH = arenaHeight * 0.5f - margin;

            for (var i = 0; i < count; i++)
            {
                for (var attempt = 0; attempt < 24; attempt++)
                {
                    var pos = new Vector2(
                        ((float)rng.NextDouble() * 2f - 1f) * halfW,
                        ((float)rng.NextDouble() * 2f - 1f) * halfH);

                    if (pos.magnitude < 6f) continue;

                    var tooClose = false;
                    foreach (var other in placed)
                    {
                        if (Vector2.Distance(other, pos) >= 2.4f) continue;
                        tooClose = true;
                        break;
                    }

                    if (tooClose) continue;

                    placed.Add(pos);
                    var scale = 0.38f + (float)rng.NextDouble() * 0.42f;
                    var stone = CreateStoneObstacle(new Vector3(pos.x, pos.y, 0f), scale);
                    stone.transform.SetParent(root.transform, true);
                    break;
                }
            }

            return root;
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
            var fire = CreateSprite("Campfire", ArtLibrary.Campfire, position, 0.45f, 2);
            var glow = new GameObject("CampfireGlow");
            glow.transform.SetParent(fire.transform, false);
            glow.transform.localPosition = Vector3.zero;
            var glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = ArtLibrary.Campfire;
            glowRenderer.color = new Color(1f, 0.55f, 0.15f, 0.35f);
            glowRenderer.sortingOrder = 1;
            glow.transform.localScale = Vector3.one * 1.6f;
            return fire;
        }

        public static GameObject CreatePlayer(Vector3 position, bool survivalMode)
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
            var stats = go.AddComponent<PlayerStats>();
            stats.ConfigureForRun(survivalMode);
            if (survivalMode)
            {
                go.AddComponent<PlayerCombat>();
            }

            return go;
        }

        public static GameObject CreateEnemy(Vector3 position, int round, bool isBoss)
        {
            var sprite = isBoss ? ArtLibrary.Boss : ArtLibrary.Zombie;
            var scale = isBoss ? 0.55f : 0.32f;
            var go = CreateSprite(isBoss ? "Boss" : "Zombie", sprite, position, scale, 5);
            go.tag = "Enemy";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.useFullKinematicContacts = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = isBoss ? 0.7f : 0.4f;

            var enemy = go.AddComponent<EnemyActor>();
            enemy.Initialize(round, isBoss);
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
            var go = new GameObject(type == PickupType.Xp ? "XpPickup" : "GoldPickup");
            go.transform.position = position;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            go.AddComponent<LootPickup>().Initialize(type, amount);
            return go;
        }

        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}