using UnityEngine;

namespace ProjectZx.Core
{
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

        public static Sprite PlayerIdle => _playerIdle ??= Load("GameArt/players/male/male_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("GameArt/players/male/male_walking");
        public static Sprite PlayerAttack => _playerAttack ??= Load("GameArt/players/male/male_attacking");
        public static Sprite Zombie => _zombie ??= Load("GameArt/enemies/zombie");
        public static Sprite Boss => _boss ??= Load("GameArt/enemies/boss");
        public static Sprite Wizard => _wizard ??= Load("GameArt/npcs/wizard/wizard_idle");
        public static Sprite Knight => _knight ??= Load("GameArt/npcs/dark_knight/dark_knight_idle");
        public static Sprite Ground => _ground ??= Load("GameArt/effects/blood_pool");

        static Sprite Load(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            return CreateFallback(path);
        }

        static Sprite CreateFallback(string name)
        {
            var tex = new Texture2D(4, 4);
            var color = name.Contains("boss") ? Color.red : name.Contains("zombie") ? new Color(0.3f, 0.7f, 0.3f) : Color.white;
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}