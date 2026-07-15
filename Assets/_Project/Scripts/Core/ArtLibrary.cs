using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Loads NARt art from Resources/Art with procedural fallbacks for camp-specific tiles.
    /// </summary>
    public static class ArtLibrary
    {
        public const float TilePixelsPerUnit = 64f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches()
        {
            _playerIdle = null;
            _playerWalk = null;
            _playerAttack = null;
            _zombie = null;
            _boss = null;
            _bossAttacking = null;
            _wizard = null;
            _knight = null;
            _achievementKeeper = null;
            _ground = null;
            _grassTile = null;
            _grassVariants = null;
            _campfire = null;
            _baseballBat = null;
            _spear = null;
            _stone = null;
            _tree = null;
            _door = null;
            _shopUi = null;
            _levelUpUi = null;
            _challengeBoardUi = null;
            _outsideTiles = null;
            _insideTiles = null;
            _waterTile = null;
            _fireBreathFrames = null;
        }

        static Sprite _playerIdle;
        static Sprite _playerWalk;
        static Sprite _playerAttack;
        static Sprite _zombie;
        static Sprite _boss;
        static Sprite _bossAttacking;
        static Sprite _wizard;
        static Sprite _knight;
        static Sprite _achievementKeeper;
        static Sprite _ground;
        static Sprite _grassTile;
        static Sprite[] _grassVariants;
        static Sprite _campfire;
        static Sprite _baseballBat;
        static Sprite _spear;
        static Sprite _stone;
        static Sprite _tree;
        static Sprite _door;
        static Sprite _shopUi;
        static Sprite _levelUpUi;
        static Sprite _challengeBoardUi;
        static Sprite[] _outsideTiles;
        static Sprite[] _insideTiles;
        static Sprite _waterTile;
        static Sprite[] _fireBreathFrames;

        public static Sprite PlayerIdle => _playerIdle ??= Load("Placeholders/player_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("Placeholders/player_walk");
        public static Sprite PlayerAttack => _playerAttack ??= Load("Placeholders/player_attack");
        public static Sprite Zombie => _zombie ??= Load("Art/zombie_j", "ZombieJ", "Placeholders/zombie");
        public static Sprite Boss => _boss ??= Load("Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite BossAttacking => _bossAttacking ??= Load("Art/boss_j_attacking", "BossJAttacking", "Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite Wizard => _wizard ??= Load("Placeholders/wizard");
        public static Sprite Knight => _knight ??= Load("Placeholders/knight");
        public static Sprite AchievementKeeper => _achievementKeeper ??= Load("Art/achievement_keeper", "AchievementKeeper", "Placeholders/wizard");
        public static Sprite Ground => _ground ??= Load("Placeholders/ground");
        public static Sprite GrassTile => _grassTile ??= LoadOrCreateGrass();
        public static Sprite Campfire => _campfire ??= CreateCampfireSprite();
        public static Sprite BaseballBat => _baseballBat ??= LoadOrCreateBat();
        public static Sprite Spear => _spear ??= CreateSpearSprite();
        public static Sprite Stone => _stone ??= CreateStoneSprite();
        public static Sprite Tree => _tree ??= CreateTreeSprite();
        public static Sprite Door => _door ??= CreateDoorSprite();
        public static Sprite ShopUi => _shopUi ??= Load("Art/shop_ui", "ShopUI");
        public static Sprite LevelUpUi => _levelUpUi ??= Load("Art/level_up_ui", "LevelUpUI");
        public static Sprite ChallengeBoardUi => _challengeBoardUi ??= Load("Art/challenge_board_ui", "ChallengeBoardUI");
        public static Sprite WaterTile => _waterTile ??= LoadTile("Art/tile1_water", "tile1Water", "Art/tile1_outside", "tile1Outside");

        public static Sprite GetGrassVariant(int index)
        {
            _grassVariants ??= new[]
            {
                LoadOrCreateGrass(),
                Load("Placeholders/grass_tile_b"),
                Load("Placeholders/grass_tile_c")
            };
            return _grassVariants[Mathf.Abs(index) % _grassVariants.Length];
        }

        public static Sprite GetOutsideTile(int index)
        {
            _outsideTiles ??= new[]
            {
                LoadTile("Art/tile1_outside", "tile1Outside"),
                LoadTile("Art/tile2_outside", "tile2Outside"),
                LoadTile("Art/tile3_outside", "tile3Outside")
            };
            return _outsideTiles[Mathf.Abs(index) % _outsideTiles.Length];
        }

        public static Sprite GetInsideTile(int index)
        {
            _insideTiles ??= new[]
            {
                LoadTile("Art/tile1_inside", "tile1Inside"),
                LoadTile("Art/tile2_inside", "tile2Inside")
            };
            return _insideTiles[Mathf.Abs(index) % _insideTiles.Length];
        }

        public static Sprite GetFireBreathFrame(int frame)
        {
            _fireBreathFrames ??= CreateFireBreathFrames();
            return _fireBreathFrames[Mathf.Abs(frame) % _fireBreathFrames.Length];
        }

        static Sprite Load(string path, params string[] fallbackPaths)
        {
            var sprite = TryLoadSprite(path, TilePixelsPerUnit);
            if (sprite != null) return sprite;

            foreach (var fallback in fallbackPaths)
            {
                if (string.IsNullOrEmpty(fallback)) continue;
                sprite = TryLoadSprite(fallback, TilePixelsPerUnit);
                if (sprite != null) return sprite;
            }

            return CreateFallback(path);
        }

        static Sprite LoadTile(string path, params string[] fallbackPaths)
        {
            var sprite = TryLoadSprite(path, TilePixelsPerUnit);
            if (sprite != null) return sprite;

            foreach (var fallback in fallbackPaths)
            {
                if (string.IsNullOrEmpty(fallback)) continue;
                sprite = TryLoadSprite(fallback, TilePixelsPerUnit);
                if (sprite != null) return sprite;
            }

            return CreateTileFallback(path);
        }

        static Sprite TryLoadSprite(string path, float pixelsPerUnit)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0) return sprites[0];

            var texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
            }

            var leaf = path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;
            if (leaf != path)
            {
                sprite = Resources.Load<Sprite>(leaf);
                if (sprite != null) return sprite;

                texture = Resources.Load<Texture2D>(leaf);
                if (texture != null)
                {
                    return Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit);
                }
            }

            return null;
        }

        public static float GetTileScale(Sprite sprite, float tileSize = 1f)
        {
            if (sprite == null) return 1f;
            var width = sprite.bounds.size.x;
            return width > 0.001f ? tileSize / width : 1f;
        }

        static Sprite LoadOrCreateGrass()
        {
            var sprite = Resources.Load<Sprite>("Placeholders/grass_tile");
            return sprite != null ? sprite : CreateGrassTileSprite();
        }

        static Sprite LoadOrCreateBat()
        {
            var sprite = Resources.Load<Sprite>("Placeholders/baseball_bat");
            return sprite != null ? sprite : CreateBaseballBatSprite();
        }

        static Sprite[] CreateFireBreathFrames()
        {
            var frames = new Sprite[4];
            for (var i = 0; i < frames.Length; i++)
                frames[i] = CreateFireBreathFrameSprite(i);
            return frames;
        }

        static Sprite CreateFireBreathFrameSprite(int frame)
        {
            const int w = 48;
            const int h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var core = new Color(1f, 0.95f, 0.45f, 0.95f);
            var flame = new Color(1f, 0.55f, 0.1f, 0.9f);
            var edge = new Color(0.9f, 0.2f, 0.05f, 0.75f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            var reach = 18 + frame * 5;
            for (var x = 8; x < reach; x++)
            {
                var t = (float)(x - 8) / Mathf.Max(1, reach - 8);
                var halfHeight = Mathf.RoundToInt(Mathf.Lerp(3f, 9f, 1f - t) + frame * 0.5f);
                for (var y = h / 2 - halfHeight; y <= h / 2 + halfHeight; y++)
                {
                    var c = t < 0.35f ? core : t < 0.7f ? flame : edge;
                    Set(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 32f);
        }

        static Sprite CreateTreeSprite()
        {
            const int w = 20;
            const int h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var trunk = new Color(0.42f, 0.28f, 0.14f);
            var leaves = new Color(0.22f, 0.52f, 0.2f);
            var leavesLight = new Color(0.34f, 0.66f, 0.28f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            Set(9, 2, trunk); Set(10, 2, trunk);
            Set(9, 3, trunk); Set(10, 3, trunk);
            Set(9, 4, trunk); Set(10, 4, trunk);
            Set(9, 5, trunk); Set(10, 5, trunk);
            Set(8, 6, trunk); Set(9, 6, trunk); Set(10, 6, trunk); Set(11, 6, trunk);

            for (var y = 7; y <= 18; y++)
            for (var x = 4; x <= 15; x++)
            {
                var dx = Mathf.Abs(x - 9.5f);
                var dy = Mathf.Abs(y - 12f);
                if (dx + dy * 0.7f > 7.5f) continue;
                Set(x, y, (x + y) % 3 == 0 ? leavesLight : leaves);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 16f);
        }

        static Sprite CreateDoorSprite()
        {
            const int w = 24;
            const int h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var frame = new Color(0.35f, 0.22f, 0.12f);
            var wood = new Color(0.52f, 0.34f, 0.18f);
            var glow = new Color(0.95f, 0.72f, 0.2f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            for (var y = 2; y < 30; y++)
            {
                Set(4, y, frame); Set(19, y, frame);
            }
            for (var x = 4; x <= 19; x++)
            {
                Set(x, 2, frame); Set(x, 29, frame);
            }

            for (var y = 4; y < 28; y++)
            for (var x = 6; x < 18; x++)
                Set(x, y, wood);

            Set(12, 16, glow);
            Set(11, 15, glow); Set(13, 15, glow);
            Set(11, 16, glow); Set(13, 16, glow);
            Set(12, 17, glow);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        static Sprite CreateGrassTileSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var rng = new System.Random(42);
            var baseGreen = new Color(0.28f, 0.52f, 0.22f);
            var darkGreen = new Color(0.22f, 0.44f, 0.18f);
            var lightGreen = new Color(0.34f, 0.6f, 0.28f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var n = rng.Next(100);
                var color = n < 15 ? darkGreen : n < 35 ? lightGreen : baseGreen;
                if (n < 4) color = new Color(0.45f, 0.7f, 0.3f);
                tex.SetPixel(x, y, color);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 4f);
        }

        static Sprite CreateSpearSprite()
        {
            const int w = 40;
            const int h = 6;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var shaft = new Color(0.48f, 0.32f, 0.16f);
            var shaftDark = new Color(0.36f, 0.24f, 0.12f);
            var tip = new Color(0.72f, 0.74f, 0.78f);
            var binding = new Color(0.28f, 0.2f, 0.14f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            for (var x = 2; x < 34; x++)
            {
                Set(x, 2, shaftDark);
                Set(x, 3, shaft);
            }

            Set(0, 2, binding); Set(1, 2, binding); Set(0, 3, binding); Set(1, 3, binding);
            Set(34, 1, tip); Set(35, 1, tip); Set(36, 0, tip); Set(37, 0, tip); Set(38, 1, tip); Set(39, 1, tip);
            Set(34, 2, tip); Set(35, 2, tip); Set(36, 1, tip); Set(37, 1, tip); Set(38, 2, tip); Set(39, 2, tip);
            Set(34, 3, tip); Set(35, 3, tip); Set(36, 2, tip); Set(37, 2, tip); Set(38, 3, tip); Set(39, 3, tip);
            Set(35, 4, tip); Set(36, 3, tip); Set(37, 3, tip); Set(38, 4, tip);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.08f, 0.5f), 4f);
        }

        static Sprite CreateBaseballBatSprite()
        {
            const int w = 20;
            const int h = 6;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var wood = new Color(0.55f, 0.34f, 0.18f);
            var woodDark = new Color(0.42f, 0.26f, 0.12f);
            var tape = new Color(0.2f, 0.2f, 0.22f);
            var knob = new Color(0.35f, 0.22f, 0.1f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            Set(0, 2, knob); Set(0, 3, knob);
            Set(1, 2, tape); Set(1, 3, tape);
            Set(2, 2, tape); Set(2, 3, tape);
            for (var x = 3; x < 16; x++)
            {
                Set(x, 2, woodDark);
                Set(x, 3, wood);
            }
            Set(16, 1, wood); Set(17, 1, wood); Set(18, 0, wood);
            Set(16, 2, wood); Set(17, 2, wood); Set(18, 1, wood); Set(19, 1, wood);
            Set(16, 3, wood); Set(17, 3, wood); Set(18, 2, wood); Set(19, 2, wood);
            Set(17, 4, woodDark); Set(18, 3, woodDark); Set(19, 3, woodDark);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.12f, 0.5f), 4f);
        }

        static Sprite CreateStoneSprite()
        {
            const int size = 14;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < size && y >= 0 && y < size) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var rock = new Color(0.45f, 0.43f, 0.4f);
            var dark = new Color(0.32f, 0.3f, 0.28f);
            var light = new Color(0.58f, 0.55f, 0.5f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                Set(x, y, clear);

            Set(4, 3, dark); Set(5, 3, rock); Set(6, 3, rock); Set(7, 3, rock); Set(8, 3, dark);
            Set(3, 4, dark); Set(4, 4, rock); Set(5, 4, light); Set(6, 4, rock); Set(7, 4, rock); Set(8, 4, dark); Set(9, 4, dark);
            Set(3, 5, rock); Set(4, 5, rock); Set(5, 5, rock); Set(6, 5, light); Set(7, 5, rock); Set(8, 5, dark); Set(9, 5, dark);
            Set(4, 6, dark); Set(5, 6, rock); Set(6, 6, rock); Set(7, 6, rock); Set(8, 6, dark);
            Set(5, 7, dark); Set(6, 7, dark); Set(7, 7, dark);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.4f), 4f);
        }

        static Sprite CreateCampfireSprite()
        {
            const int w = 16;
            const int h = 20;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var log = new Color(0.42f, 0.26f, 0.14f);
            var ember = new Color(0.95f, 0.45f, 0.1f);
            var flame = new Color(1f, 0.78f, 0.2f);
            var core = new Color(1f, 0.92f, 0.55f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            Set(6, 2, log); Set(7, 2, log); Set(8, 2, log); Set(9, 2, log);
            Set(5, 3, log); Set(6, 3, log); Set(9, 3, log); Set(10, 3, log);
            Set(7, 4, ember); Set(8, 4, ember);
            Set(6, 5, flame); Set(7, 5, core); Set(8, 5, core); Set(9, 5, flame);
            Set(6, 6, flame); Set(7, 6, core); Set(8, 6, flame);
            Set(7, 7, ember); Set(8, 7, ember);
            Set(7, 8, ember);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 4f);
        }

        static Sprite CreateTileFallback(string name)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var isInside = name.Contains("inside");
            var isWater = name.Contains("water");
            var baseColor = isWater
                ? new Color(0.12f, 0.22f, 0.52f)
                : isInside
                    ? new Color(0.34f, 0.28f, 0.2f)
                    : new Color(0.24f, 0.48f, 0.2f);
            var accent = isWater
                ? new Color(0.2f, 0.34f, 0.62f)
                : isInside
                    ? new Color(0.42f, 0.34f, 0.24f)
                    : new Color(0.3f, 0.58f, 0.26f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var checker = ((x / 8) + (y / 8)) % 2 == 0;
                tex.SetPixel(x, y, checker ? baseColor : accent);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), TilePixelsPerUnit);
        }

        static Sprite CreateFallback(string name)
        {
            if (name.Contains("tile") || name.Contains("outside") || name.Contains("inside") || name.Contains("water"))
                return CreateTileFallback(name);

            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var color = name.Contains("boss") ? Color.red
                : name.Contains("zombie") ? new Color(0.3f, 0.7f, 0.3f)
                : name.Contains("wizard") ? new Color(0.55f, 0.25f, 0.85f)
                : name.Contains("knight") ? new Color(0.65f, 0.65f, 0.75f)
                : name.Contains("ground") || name.Contains("grass") ? new Color(0.32f, 0.55f, 0.24f)
                : new Color(0.32f, 0.55f, 0.24f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}