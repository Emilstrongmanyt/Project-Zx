using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Loads CC0 Kenney placeholder sprites from Resources/Placeholders until final art is ready.
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

        public static Sprite PlayerIdle => _playerIdle ??= Load("Placeholders/player_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("Placeholders/player_walk");
        public static Sprite PlayerAttack => _playerAttack ??= Load("Placeholders/player_attack");
        public static Sprite Zombie => _zombie ??= Load("Placeholders/zombie");
        public static Sprite Boss => _boss ??= Load("Placeholders/boss");
        public static Sprite Wizard => _wizard ??= Load("Placeholders/wizard");
        public static Sprite Knight => _knight ??= Load("Placeholders/knight");
        public static Sprite Ground => _ground ??= Load("Placeholders/ground");

        static Sprite Load(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            return CreateFallback(path);
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
                : name.Contains("ground") ? new Color(0.45f, 0.32f, 0.2f)
                : new Color(0.25f, 0.55f, 0.95f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}