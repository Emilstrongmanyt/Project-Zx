using UnityEngine;

namespace ProjectZx.Core
{
    public struct HeroSpriteSet
    {
        public Sprite Idle;
        public Sprite WalkA;
        public Sprite WalkB;
        public bool FacesRightByDefault;
    }

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
            _katana = null;
            _bow = null;
            _arrow = null;
            _gateway = null;
            _stone = null;
            _tree = null;
            _treeVariants = null;
            _rockVariants = null;
            _computerVariants = null;
            _insidePropVariants = null;
            _warheadVariants = null;
            _cryptVariants = null;
            _rollZySprites = null;
            _rowZiSprites = null;
            _door = null;
            _shopUi = null;
            _levelUpUi = null;
            _challengeBoardUi = null;
            _outsideTiles = null;
            _insideTiles = null;
            _dungeonTiles = null;
            _waterTile = null;
            _fireBreathFrames = null;
            _zombieHit = null;
            _zombieInside = null;
            _zombieInsideHit = null;
            _zombieInside2 = null;
            _zombieInside2Hit = null;
            _bossHit = null;
            _bossAttackingHit = null;
            _goldCoin = null;
            _goldCoinDropped = null;
            _hpHeart = null;
            _hpHeartDropped = null;
            _xpGem = null;
            _pinkCrystal = null;
            _btnPrimary = null;
            _btn220x52 = null;
            _btn200x52 = null;
            _btn360x56 = null;
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
        static Sprite _katana;
        static Sprite _bow;
        static Sprite _arrow;
        static Sprite _gateway;
        static Sprite _stone;
        static Sprite _tree;
        static Sprite[] _treeVariants;
        static Sprite[] _rockVariants;
        static Sprite[] _computerVariants;
        static Sprite[] _insidePropVariants;
        static Sprite[] _warheadVariants;
        static Sprite[] _cryptVariants;
        static Sprite[] _rollZySprites;
        static Sprite[] _rowZiSprites;
        static Sprite _door;
        static Sprite _shopUi;
        static Sprite _levelUpUi;
        static Sprite _challengeBoardUi;
        static Sprite[] _outsideTiles;
        static Sprite[] _insideTiles;
        static Sprite[] _dungeonTiles;
        static Sprite _waterTile;
        static Sprite[] _fireBreathFrames;
        static Sprite _zombieHit;
        static Sprite _zombieInside;
        static Sprite _zombieInsideHit;
        static Sprite _zombieInside2;
        static Sprite _zombieInside2Hit;
        static Sprite _bossHit;
        static Sprite _bossAttackingHit;
        static Sprite _goldCoin;
        static Sprite _goldCoinDropped;
        static Sprite _hpHeart;
        static Sprite _hpHeartDropped;
        static Sprite _xpGem;
        static Sprite _pinkCrystal;
        static Sprite _btnPrimary;
        static Sprite _btn220x52;
        static Sprite _btn200x52;
        static Sprite _btn360x56;

        public static Sprite PlayerIdle => _playerIdle ??= Load("Placeholders/player_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("Placeholders/player_walk");
        public static Sprite PlayerAttack => _playerAttack ??= Load("Placeholders/player_attack");
        public static Sprite Zombie => _zombie ??= Load("Art/zombie_j", "ZombieJ", "Placeholders/zombie");
        public static Sprite ZombieHit => _zombieHit ??= Load("ZombieJHit", "Art/zombie_j_hit", "ZombieJ");
        public static Sprite ZombieInside => _zombieInside ??= Load("ZombieJ_Inside");
        public static Sprite ZombieInsideHit => _zombieInsideHit ??= Load("ZombieJ_InsideHit", "ZombieJ_Inside");
        public static Sprite ZombieInside2 => _zombieInside2 ??= Load("ZombieJ_Inside2");
        public static Sprite ZombieInside2Hit => _zombieInside2Hit ??= Load("ZombieJ_Inside2Hit", "ZombieJ_Inside2");
        public static Sprite Boss => _boss ??= Load("Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite BossHit => _bossHit ??= Load("BossJHit", "BossJ");
        public static Sprite BossAttacking => _bossAttacking ??= Load("Art/boss_j_attacking", "BossJAttacking", "Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite BossAttackingHit => _bossAttackingHit ??= Load("BossJAttackingHit", "BossJAttacking");
        public static Sprite GoldCoin => _goldCoin ??= Load("GoldCoin");
        public static Sprite GoldCoinDropped => _goldCoinDropped ??= Load("GoldCoinDropped", "GoldCoin");
        public static Sprite HpHeart => _hpHeart ??= Load("HeartHP", "HPHeart");
        public static Sprite HpHeartDropped => _hpHeartDropped ??= Load("HPHeartDropped", "HeartHP", "HPHeart");
        public static Sprite XpGem => _xpGem ??= LoadOrCreateXpGem();
        public static Sprite PinkCrystal => _pinkCrystal ??= CreatePinkCrystalSprite();
        public static Sprite BtnPrimary => _btnPrimary ??= Load("btn_primary");
        public static Sprite Btn220x52 => _btn220x52 ??= Load("btn_220x52", "btn_primary");
        public static Sprite Btn200x52 => _btn200x52 ??= Load("btn_200x52", "btn_primary");
        public static Sprite Btn360x56 => _btn360x56 ??= Load("btn_360x56", "btn_primary");
        public static Sprite Wizard => _wizard ??= Load("Placeholders/wizard", "WizardNpc");
        public static Sprite Knight => _knight ??= Load("Placeholders/knight", "KnightNpc");
        /// <summary>World NPC: achievement board (not the wizard placeholder).</summary>
        public static Sprite AchievementKeeper => _achievementKeeper ??= LoadOrCreateAchievementBoardNpc();
        public static Sprite Ground => _ground ??= Load("Placeholders/ground");
        public static Sprite GrassTile => _grassTile ??= LoadOrCreateGrass();
        public static Sprite Campfire => _campfire ??= CreateCampfireSprite();
        public static Sprite BaseballBat => _baseballBat ??= LoadOrCreateBat();
        public static Sprite Spear => _spear ??= CreateSpearSprite();
        public static Sprite Katana => _katana ??= CreateKatanaSprite();
        public static Sprite Bow => _bow ??= LoadOrCreateBow();
        public static Sprite Arrow => _arrow ??= CreateArrowSprite();
        public static Sprite Gateway => _gateway ??= LoadOrCreateGateway();
        public static Sprite Stone => _stone ??= GetSheetVariant("RockSheet", 10, 0) ?? CreateStoneSprite();
        public static Sprite Tree => _tree ??= GetSheetVariant("TreeSheet", 9, 0) ?? CreateTreeSprite();

        public static Sprite[] TreeVariants => _treeVariants ??= LoadSheetSprites("TreeSheet", 9);
        public static Sprite[] RockVariants => _rockVariants ??= LoadSheetSprites("RockSheet", 10);
        public static Sprite[] ComputerVariants => _computerVariants ??= LoadSheetSprites("ComputerSheet", 8);
        public static Sprite[] InsidePropVariants => _insidePropVariants ??= LoadSheetSprites("Inside1Sheet", 9);
        public static Sprite[] WarheadVariants => _warheadVariants ??= LoadSheetSprites("WarheadSheet", 8);
        public static Sprite[] CryptVariants => _cryptVariants ??= LoadSheetSprites("CryptSheet", 9);

        // Outside trees/rocks use sheet variants #1 and #2 only (indices 0 and 1).
        public static Sprite GetRandomTreeSprite() => PickFromFirstTwo(TreeVariants) ?? CreateTreeSprite();

        public static Sprite GetRandomRockSprite() => PickFromFirstTwo(RockVariants) ?? CreateStoneSprite();

        public static Sprite GetRandomComputerSprite() => PickRandom(ComputerVariants);

        public static Sprite GetRandomInsidePropSprite() => PickRandom(InsidePropVariants);

        public static Sprite GetRandomWarheadSprite() => PickRandom(WarheadVariants);

        public static Sprite GetRandomCryptSprite() => PickRandom(CryptVariants);

        public static int GetVariantCount(Sprite[] variants) =>
            variants == null ? 0 : System.Array.FindAll(variants, sprite => sprite != null).Length;

        public static HeroSpriteSet GetHeroSprites(PlayableHero hero)
        {
            if (hero == PlayableHero.RowZi)
            {
                _rowZiSprites ??= LoadHeroSheetSprites("RowZi", 8);
                return new HeroSpriteSet
                {
                    Idle = _rowZiSprites[0],
                    WalkA = _rowZiSprites[2],
                    WalkB = _rowZiSprites[3],
                    FacesRightByDefault = false
                };
            }

            _rollZySprites ??= LoadHeroSheetSprites("RollZy", 8);
            return new HeroSpriteSet
            {
                Idle = _rollZySprites[0],
                WalkA = _rollZySprites[1],
                WalkB = _rollZySprites[2],
                FacesRightByDefault = true
            };
        }

        public static Sprite GetHeroIdleSprite(PlayableHero hero) => GetHeroSprites(hero).Idle;
        public static Sprite Door => _door ??= CreateDoorSprite();
        public static Sprite ShopUi => _shopUi ??= Load("Art/shop_ui", "ShopUI");
        public static Sprite LevelUpUi => _levelUpUi ??= Load("Art/level_up_ui", "LevelUpUI");
        public static Sprite ChallengeBoardUi => _challengeBoardUi ??= Load("Art/challenge_board_ui", "ChallengeBoardUI");
        // Never fall back to land tiles — that made water borders look "removed".
        public static Sprite WaterTile
        {
            get
            {
                if (_waterTile != null) return _waterTile;
                _waterTile = LoadTile("Art/tile1_water", "tile1Water");
                // Last resort: solid blue so borders never become grass/land.
                if (_waterTile == null || IsLikelyLandTile(_waterTile))
                    _waterTile = CreateTileFallback("tile1_water");
                return _waterTile;
            }
        }

        static bool IsLikelyLandTile(Sprite sprite)
        {
            // Guard against a bad asset swap: if average pixel is clearly green grass, reject.
            if (sprite == null || sprite.texture == null) return true;
            try
            {
                var tex = sprite.texture;
                if (!tex.isReadable) return false;
                var sample = tex.GetPixel(tex.width / 2, tex.height / 2);
                return sample.g > sample.b + 0.12f && sample.g > sample.r + 0.05f;
            }
            catch
            {
                return false;
            }
        }

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

        public static Sprite GetDungeonTile(int index)
        {
            _dungeonTiles ??= new[]
            {
                CreateTileFallback("dungeon_cave_a"),
                CreateTileFallback("dungeon_cave_b")
            };
            return _dungeonTiles[Mathf.Abs(index) % _dungeonTiles.Length];
        }

        public static Sprite GetFireBreathFrame(int frame)
        {
            EnsureFireBreathFrames();
            var index = Mathf.Abs(frame) % _fireBreathFrames.Length;
            return _fireBreathFrames[index];
        }

        /// <summary>
        /// Fire breath art tip points left (-X). Pivot is on the right/base (mouth) so
        /// rotation swings the stream out from the boss toward the player.
        /// </summary>
        static void EnsureFireBreathFrames()
        {
            if (_fireBreathFrames != null) return;

            _fireBreathFrames = new Sprite[4];
            for (var i = 0; i < 4; i++)
            {
                var src = Load($"FireBreath{i + 1}");
                if (src != null && src.texture != null)
                {
                    // Pivot on the base (right edge) for left-pointing flame art.
                    _fireBreathFrames[i] = Sprite.Create(
                        src.texture,
                        src.rect,
                        new Vector2(1f, 0.5f),
                        src.pixelsPerUnit > 0f ? src.pixelsPerUnit : TilePixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect);
                    _fireBreathFrames[i].name = $"FireBreath{i + 1}_aim";
                }
                else
                {
                    _fireBreathFrames[i] = CreateFireBreathFrameSprite(i);
                }
            }
        }

        public static void GetZombieSprites(EnemyZombieKind kind, out Sprite idle, out Sprite hit)
        {
            switch (kind)
            {
                case EnemyZombieKind.InsideElite:
                    idle = ZombieInside2;
                    hit = ZombieInside2Hit;
                    break;
                case EnemyZombieKind.Inside:
                    idle = ZombieInside;
                    hit = ZombieInsideHit;
                    break;
                default:
                    idle = Zombie;
                    hit = ZombieHit;
                    break;
            }
        }

        static Sprite GetSheetVariant(string sheetName, int expectedCount, int index)
        {
            var sprites = LoadSheetSprites(sheetName, expectedCount);
            if (sprites == null || sprites.Length == 0) return null;

            var clamped = Mathf.Clamp(index, 0, sprites.Length - 1);
            if (sprites[clamped] != null) return sprites[clamped];

            foreach (var sprite in sprites)
            {
                if (sprite != null) return sprite;
            }

            return null;
        }

        static Sprite PickFromFirstTwo(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return null;
            var first = sprites[0];
            var second = sprites.Length > 1 ? sprites[1] : null;
            if (first == null) return second;
            if (second == null) return first;
            return Random.Range(0, 2) == 0 ? first : second;
        }

        static Sprite[] LoadHeroSheetSprites(string sheetName, int count)
        {
            var sprites = new Sprite[count];
            var loaded = 0;
            for (var i = 0; i < count; i++)
            {
                sprites[i] = Load($"{sheetName}_{i}", sheetName);
                if (sprites[i] != null) loaded++;
            }

            return loaded > 0 ? sprites : null;
        }

        static Sprite[] LoadSheetSprites(string sheetName, int expectedCount)
        {
            var fromSheet = Resources.LoadAll<Sprite>(sheetName);
            if (fromSheet != null && fromSheet.Length > 0)
            {
                var prefix = sheetName + "_";
                var filtered = new System.Collections.Generic.List<Sprite>();
                foreach (var sprite in fromSheet)
                {
                    if (sprite == null) continue;
                    if (!sprite.name.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                    filtered.Add(sprite);
                }

                if (filtered.Count > 0)
                {
                    filtered.Sort((a, b) => GetSheetSpriteIndex(a.name, prefix).CompareTo(GetSheetSpriteIndex(b.name, prefix)));
                    return filtered.ToArray();
                }
            }

            return LoadHeroSheetSprites(sheetName, expectedCount);
        }

        static int GetSheetSpriteIndex(string spriteName, string prefix)
        {
            if (!spriteName.StartsWith(prefix, System.StringComparison.Ordinal)) return int.MaxValue;
            return int.TryParse(spriteName.Substring(prefix.Length), out var index) ? index : int.MaxValue;
        }

        static Sprite PickRandom(Sprite[] sprites)
        {
            return PickRandom(sprites, null);
        }

        public static Sprite PickRandom(Sprite[] sprites, System.Random rng)
        {
            if (sprites == null || sprites.Length == 0) return null;

            var validCount = 0;
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null) validCount++;
            }

            if (validCount == 0) return null;

            var target = rng != null ? rng.Next(validCount) : Random.Range(0, validCount);
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                if (target == 0) return sprites[i];
                target--;
            }

            return null;
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

        static Sprite LoadOrCreateBow()
        {
            // Prefer a high-contrast procedural bow — the old placeholder was nearly invisible in-game.
            return CreateBowSprite();
        }

        static Sprite LoadOrCreateGateway()
        {
            var sprite = Load("GatewaySprite", "Art/GatewaySprite");
            return sprite != null ? sprite : CreateGatewayFallbackSprite();
        }

        /// <summary>Large, high-contrast recurve bow for the bowman weapon slot.</summary>
        static Sprite CreateBowSprite()
        {
            const int w = 48;
            const int h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var woodDark = new Color(0.28f, 0.14f, 0.05f, 1f);
            var wood = new Color(0.62f, 0.36f, 0.12f, 1f);
            var woodLight = new Color(0.88f, 0.58f, 0.22f, 1f);
            var grip = new Color(0.18f, 0.12f, 0.08f, 1f);
            var stringColor = new Color(1f, 0.96f, 0.78f, 1f);
            var stringEdge = new Color(0.95f, 0.82f, 0.25f, 1f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            // Thick D-shaped limbs (open toward +X).
            for (var y = 2; y <= 37; y++)
            {
                var t = Mathf.Abs(y - 19.5f) / 17.5f;
                var limbX = Mathf.RoundToInt(Mathf.Lerp(8f, 38f, t * t));
                for (var tX = 0; tX < 4; tX++)
                {
                    var c = tX == 0 ? woodDark : tX == 3 ? woodLight : wood;
                    Set(limbX + tX, y, c);
                    Set(limbX + tX, y + 1, c);
                }
            }

            // Grip block near pivot.
            for (var y = 15; y <= 24; y++)
            for (var x = 6; x <= 14; x++)
                Set(x, y, grip);

            // Bright bowstring on the right edge.
            for (var y = 4; y <= 35; y++)
            {
                Set(40, y, stringEdge);
                Set(41, y, stringColor);
                Set(42, y, stringEdge);
            }

            // Tip nocks.
            for (var x = 36; x <= 42; x++)
            {
                Set(x, 3, woodLight);
                Set(x, 36, woodLight);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.12f, 0.5f), 16f);
        }

        /// <summary>Bright arrow projectile — wood shaft, metal tip, red fletching.</summary>
        static Sprite CreateArrowSprite()
        {
            const int w = 36;
            const int h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var shaft = new Color(0.78f, 0.55f, 0.22f, 1f);
            var shaftEdge = new Color(0.45f, 0.28f, 0.1f, 1f);
            var tip = new Color(0.85f, 0.9f, 0.95f, 1f);
            var tipEdge = new Color(0.45f, 0.5f, 0.55f, 1f);
            var fletch = new Color(0.95f, 0.18f, 0.18f, 1f);
            var fletchDark = new Color(0.55f, 0.05f, 0.05f, 1f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            // Shaft (horizontal, tip on +X).
            for (var x = 4; x <= 28; x++)
            {
                Set(x, 4, shaftEdge);
                Set(x, 5, shaft);
                Set(x, 6, shaft);
                Set(x, 7, shaftEdge);
            }

            // Metal tip.
            for (var i = 0; i < 6; i++)
            {
                var x = 29 + i;
                var half = Mathf.Max(0, 3 - i);
                for (var dy = -half; dy <= half; dy++)
                    Set(x, 5 + dy + 1, i >= 4 ? tipEdge : tip);
            }

            // Fletching on the nock.
            for (var x = 1; x <= 6; x++)
            {
                Set(x, 2, fletchDark);
                Set(x, 3, fletch);
                Set(x, 8, fletch);
                Set(x, 9, fletchDark);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.15f, 0.5f), 16f);
        }

        static Sprite CreateGatewayFallbackSprite()
        {
            const int w = 32;
            const int h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var stone = new Color(0.22f, 0.2f, 0.28f, 1f);
            var stoneLight = new Color(0.4f, 0.38f, 0.5f, 1f);
            var portal = new Color(0.35f, 0.15f, 0.75f, 1f);
            var portalCore = new Color(0.75f, 0.45f, 1f, 1f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            for (var y = 2; y < 38; y++)
            {
                Set(3, y, stone); Set(4, y, stoneLight);
                Set(27, y, stoneLight); Set(28, y, stone);
            }

            for (var x = 3; x <= 28; x++)
            {
                Set(x, 2, stone); Set(x, 37, stone);
            }

            for (var y = 5; y < 35; y++)
            for (var x = 7; x < 25; x++)
            {
                var cx = (x - 15.5f) / 8f;
                var cy = (y - 20f) / 14f;
                if (cx * cx + cy * cy > 1f) continue;
                Set(x, y, Mathf.Abs(cx) + Mathf.Abs(cy) < 0.55f ? portalCore : portal);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        static Sprite LoadOrCreateXpGem()
        {
            var sprite = Resources.Load<Sprite>("Placeholders/xp_gem");
            return sprite != null ? sprite : CreateXpGemSprite();
        }

        static Sprite CreatePinkCrystalSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < size && y >= 0 && y < size) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var core = new Color(1f, 0.55f, 0.9f, 1f);
            var mid = new Color(0.95f, 0.28f, 0.72f, 1f);
            var edge = new Color(0.62f, 0.08f, 0.48f, 1f);
            var shine = new Color(1f, 0.88f, 0.98f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                Set(x, y, clear);

            // Tall diamond crystal.
            for (var y = 1; y <= 14; y++)
            {
                var t = y <= 8 ? (y - 1) / 7f : (14 - y) / 6f;
                var half = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 5f, t)));
                for (var x = 8 - half; x <= 8 + half; x++)
                {
                    var edgeDist = Mathf.Abs(x - 8f) / half;
                    var c = edgeDist > 0.75f ? edge : edgeDist > 0.35f ? mid : core;
                    Set(x, y, c);
                }
            }

            Set(7, 11, shine);
            Set(8, 12, shine);
            Set(6, 9, shine);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite LoadOrCreateAchievementBoardNpc()
        {
            // Prefer a dedicated board asset; otherwise build a standing trophy board for the camp NPC.
            // Do not fall back to ChallengeBoardUI (flat UI plate — wrong for a world prop).
            var loaded = TryLoadSprite("Art/achievement_keeper", TilePixelsPerUnit)
                         ?? TryLoadSprite("AchievementKeeper", TilePixelsPerUnit);
            return loaded != null ? loaded : CreateAchievementBoardNpcSprite();
        }

        /// <summary>Standing achievement board world sprite (pivot at feet).</summary>
        static Sprite CreateAchievementBoardNpcSprite()
        {
            const int w = 32;
            const int h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var post = new Color(0.38f, 0.24f, 0.12f, 1f);
            var board = new Color(0.28f, 0.16f, 0.4f, 1f);
            var boardLight = new Color(0.4f, 0.24f, 0.55f, 1f);
            var frame = new Color(0.9f, 0.7f, 0.25f, 1f);
            var star = new Color(1f, 0.88f, 0.35f, 1f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            // Post / stake
            for (var y = 0; y <= 12; y++)
            {
                Set(14, y, post);
                Set(15, y, post);
                Set(16, y, post);
            }

            // Board face
            for (var y = 12; y <= 36; y++)
            for (var x = 4; x <= 27; x++)
            {
                var edge = x <= 5 || x >= 26 || y <= 13 || y >= 35;
                Set(x, y, edge ? frame : (y > 24 ? boardLight : board));
            }

            // Star / trophy mark
            Set(15, 28, star); Set(16, 28, star);
            Set(14, 27, star); Set(15, 27, star); Set(16, 27, star); Set(17, 27, star);
            Set(15, 26, star); Set(16, 26, star);
            Set(13, 25, star); Set(18, 25, star);
            Set(15, 24, star); Set(16, 24, star);
            Set(15, 22, star); Set(16, 22, star);
            Set(14, 21, star); Set(15, 21, star); Set(16, 21, star); Set(17, 21, star);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 16f);
        }

        static Sprite CreateXpGemSprite()
        {
            const int size = 14;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < size && y >= 0 && y < size) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var core = new Color(0.45f, 0.82f, 1f);
            var mid = new Color(0.2f, 0.55f, 0.95f);
            var edge = new Color(0.1f, 0.28f, 0.62f);
            var shine = new Color(0.78f, 0.95f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                Set(x, y, clear);

            Set(6, 12, mid); Set(7, 12, mid);
            for (var y = 3; y <= 11; y++)
            {
                var width = y < 6 ? y - 2 : y > 9 ? 12 - y : 4;
                var start = 7 - width / 2;
                for (var x = start; x < start + width; x++)
                {
                    Color c = y >= 8 ? edge : y >= 5 ? mid : core;
                    if (x == start + width - 1 && y > 4) c = edge;
                    Set(x, y, c);
                }
            }

            Set(6, 8, shine); Set(7, 7, shine);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
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
            // Procedural fallback draws tip-right; pivot left so it matches +X aim without +180.
            // Runtime authored frames use right pivot + 180°; this fallback is only if load fails.
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

        static Sprite CreateKatanaSprite()
        {
            const int w = 36;
            const int h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var blade = new Color(0.82f, 0.86f, 0.9f);
            var bladeEdge = new Color(0.95f, 0.97f, 1f);
            var guard = new Color(0.55f, 0.18f, 0.16f);
            var hilt = new Color(0.22f, 0.16f, 0.12f);
            var wrap = new Color(0.12f, 0.12f, 0.14f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            // Hilt / wrap
            for (var x = 0; x < 8; x++)
            {
                Set(x, 3, hilt);
                Set(x, 4, x % 2 == 0 ? wrap : hilt);
            }

            // Tsuba (guard)
            Set(8, 2, guard); Set(8, 3, guard); Set(8, 4, guard); Set(8, 5, guard);
            Set(9, 1, guard); Set(9, 2, guard); Set(9, 5, guard); Set(9, 6, guard);

            // Curved blade
            for (var x = 10; x < 34; x++)
            {
                var rise = (x - 10) / 12;
                Set(x, 3 + rise / 3, blade);
                Set(x, 4 + rise / 3, bladeEdge);
            }

            Set(34, 4, bladeEdge); Set(35, 4, bladeEdge); Set(35, 5, bladeEdge);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.1f, 0.5f), 4f);
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

            var isDungeon = name.Contains("dungeon");
            var isInside = name.Contains("inside");
            var isWater = name.Contains("water");
            var baseColor = isWater
                ? new Color(0.12f, 0.22f, 0.52f)
                : isDungeon
                    ? new Color(0.14f, 0.13f, 0.16f)
                : isInside
                    ? new Color(0.34f, 0.28f, 0.2f)
                    : new Color(0.24f, 0.48f, 0.2f);
            var accent = isWater
                ? new Color(0.2f, 0.34f, 0.62f)
                : isDungeon
                    ? new Color(0.22f, 0.2f, 0.24f)
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