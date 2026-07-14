using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Loads CC0 Kenney placeholder sprites from Resources/Placeholders until final art is ready.
    /// Procedural fallbacks are generated for camp-specific tiles (grass, campfire).
    /// </summary>
    public static class ArtLibrary
    {
        static Sprite _playerIdle;
        static Sprite _playerWalk;
        static Sprite _playerAttack;
        static Sprite _zombie;
        static Sprite _boss;
        static Sprite _wizard;
        static Sprite _knight;
        static Sprite _ground;
        static Sprite _grassTile;
        static Sprite[] _grassVariants;
        static Sprite _campfire;

        public static Sprite PlayerIdle => _playerIdle ??= Load("Placeholders/player_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("Placeholders/player_walk");
        public static Sprite PlayerAttack => _playerAttack ??= Load("Placeholders/player_attack");
        public static Sprite Zombie => _zombie ??= Load("Placeholders/zombie");
        public static Sprite Boss => _boss ??= Load("Placeholders/boss");
        public static Sprite Wizard => _wizard ??= Load("Placeholders/wizard");
        public static Sprite Knight => _knight ??= Load("Placeholders/knight");
        public static Sprite Ground => _ground ??= Load("Placeholders/ground");
        public static Sprite GrassTile => _grassTile ??= LoadOrCreateGrass();
        public static Sprite Campfire => _campfire ??= CreateCampfireSprite();

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

        static Sprite Load(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            return CreateFallback(path);
        }

        static Sprite LoadOrCreateGrass()
        {
            var sprite = Resources.Load<Sprite>("Placeholders/grass_tile");
            return sprite != null ? sprite : CreateGrassTileSprite();
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

        static Sprite CreateFallback(string name)
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var color = name.Contains("boss") ? Color.red
                : name.Contains("zombie") ? new Color(0.3f, 0.7f, 0.3f)
                : name.Contains("wizard") ? new Color(0.55f, 0.25f, 0.85f)
                : name.Contains("knight") ? new Color(0.65f, 0.65f, 0.75f)
                : name.Contains("ground") || name.Contains("grass") ? new Color(0.32f, 0.55f, 0.24f)
                : new Color(0.25f, 0.55f, 0.95f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}