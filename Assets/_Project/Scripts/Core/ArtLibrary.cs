using System.Collections.Generic;
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
    /// Sanctum Pixel monster animation pack (stand / walk / attack / hit).
    /// </summary>
    public struct MonsterAnimSet
    {
        public Sprite Idle;
        public Sprite Hit;
        public Sprite Attack;
        public Sprite HitAttack;
        public Sprite[] StandFrames;
        public Sprite[] WalkFrames;
        public Sprite[] AttackFrames;
        public bool FacesRightByDefault;
        /// <summary>Bats / winged demons — immune to chill/slow.</summary>
        public bool IsFlying;
        public bool IsValid => Idle != null;
    }

    /// <summary>
    /// Loads NARt art from Resources/Art with procedural fallbacks for camp-specific tiles.
    /// Curated Admurin item sprites live under Resources/Items/Admurin.
    /// Demon / golem / lord packs live under Resources/Monsters.
    /// </summary>
    public static class ArtLibrary
    {
        public const float TilePixelsPerUnit = 64f;
        const string Admurin = "Items/Admurin/";
        const string Monsters = "Monsters/";

        static readonly string[] OutsideDemonSets =
        {
            "outside/warrior_1", "outside/warrior_2", "outside/warrior_3",
            "outside/thin_demon_1", "outside/thin_demon_2", "outside/fat_demon_1"
        };

        static readonly string[] InsideDemonSets =
        {
            "inside/axe_1", "inside/axe_2", "inside/claw_1",
            "inside/claw_2", "inside/demon_bat_1", "inside/warlock_1"
        };

        static readonly string[] EliteDemonSets =
        {
            "elite/big_spike_1", "elite/big_spike_2", "elite/demon_big_1",
            "elite/demon_big_2", "elite/demon_wing_1", "elite/double_sword_1"
        };

        static readonly string[] GolemBossSets = { "boss/golem_1", "boss/golem_2", "boss/golem_3" };
        static readonly string[] LordBossHighSets = { "boss/lord_1", "boss/lord_3" };
        static readonly string[] LordBossLowSets = { "boss/lord_5", "boss/lord_3" };
        /// <summary>Caster / flier packs used for ranged projectile enemies.</summary>
        static readonly string[] RangedDemonSets =
        {
            "inside/warlock_1", "inside/demon_bat_1", "elite/demon_wing_1"
        };

        static readonly Dictionary<string, MonsterAnimSet> MonsterSetCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches()
        {
            MonsterSetCache.Clear();
            _playerIdle = null;
            _playerWalk = null;
            _playerAttack = null;
            _zombie = null;
            _boss = null;
            _bossAttacking = null;
            _bossB = null;
            _bossB2 = null;
            _weaponFireFrames = null;
            _enemyBurnFrames = null;
            _bossFireBoltFrames = null;
            _wizard = null;
            _knight = null;
            _achievementKeeper = null;
            _ground = null;
            _grassTile = null;
            _grassVariants = null;
            _campfire = null;
            _weaponSpriteCache.Clear();
            _skullProjectiles = null;
            _arrow = null;
            _sparkles = null;
            _sparkles2 = null;
            _necklace = null;
            _skullNecklace = null;
            _fortuneRing = null;
            _prismRing = null;
            _staff = null;
            _treasureChest = null;
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
            _rollZyUpgradedSprites = null;
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
            _epicCrystal = null;
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
        static readonly Dictionary<string, Sprite> _weaponSpriteCache = new();
        static Sprite[] _skullProjectiles;
        static Sprite _arrow;
        static Sprite _sparkles;
        static Sprite _sparkles2;
        static Sprite _necklace;
        static Sprite _skullNecklace;
        static Sprite _fortuneRing;
        static Sprite _prismRing;
        static Sprite _staff;
        static Sprite _treasureChest;
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
        static Sprite[] _rollZyUpgradedSprites;
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
        static Sprite[] _weaponFireFrames;
        static Sprite[] _enemyBurnFrames;
        static Sprite[] _bossFireBoltFrames;
        static Sprite _zombieHit;
        static Sprite _zombieInside;
        static Sprite _zombieInsideHit;
        static Sprite _zombieInside2;
        static Sprite _zombieInside2Hit;
        static Sprite _bossHit;
        static Sprite _bossAttackingHit;
        static Sprite _bossB;
        static Sprite _bossB2;
        static Sprite _goldCoin;
        static Sprite _goldCoinDropped;
        static Sprite _hpHeart;
        static Sprite _hpHeartDropped;
        static Sprite _xpGem;
        static Sprite _pinkCrystal;
        static Sprite _epicCrystal;
        static Sprite _btnPrimary;
        static Sprite _btn220x52;
        static Sprite _btn200x52;
        static Sprite _btn360x56;

        public static Sprite PlayerIdle => _playerIdle ??= Load("Placeholders/player_idle");
        public static Sprite PlayerWalk => _playerWalk ??= Load("Placeholders/player_walk");
        public static Sprite PlayerAttack => _playerAttack ??= Load("Placeholders/player_attack");
        /// <summary>Outside demon idle (legacy name kept for callers).</summary>
        public static Sprite Zombie => _zombie ??= GetEnemyAnimSet(EnemyZombieKind.Outside).Idle
            ?? Load("Art/zombie_j", "ZombieJ", "Placeholders/zombie");
        public static Sprite ZombieHit => _zombieHit ??= GetEnemyAnimSet(EnemyZombieKind.Outside).Hit
            ?? Load("ZombieJHit", "Art/zombie_j_hit", "ZombieJ");
        public static Sprite ZombieInside => _zombieInside ??= GetEnemyAnimSet(EnemyZombieKind.Inside).Idle
            ?? Load("ZombieJ_Inside");
        public static Sprite ZombieInsideHit => _zombieInsideHit ??= GetEnemyAnimSet(EnemyZombieKind.Inside).Hit
            ?? Load("ZombieJ_InsideHit", "ZombieJ_Inside");
        public static Sprite ZombieInside2 => _zombieInside2 ??= GetEnemyAnimSet(EnemyZombieKind.InsideElite).Idle
            ?? Load("ZombieJ_Inside2");
        public static Sprite ZombieInside2Hit => _zombieInside2Hit ??= GetEnemyAnimSet(EnemyZombieKind.InsideElite).Hit
            ?? Load("ZombieJ_Inside2Hit", "ZombieJ_Inside2");
        /// <summary>Stage boss — Golem pack.</summary>
        public static Sprite Boss => _boss ??= GetGolemBossAnimSet().Idle
            ?? Load("Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite BossHit => _bossHit ??= GetGolemBossAnimSet().Hit
            ?? Load("BossJHit", "BossJ");
        public static Sprite BossAttacking => _bossAttacking ??= GetGolemBossAnimSet().Attack
            ?? Load("Art/boss_j_attacking", "BossJAttacking", "Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite BossAttackingHit => _bossAttackingHit ??= GetGolemBossAnimSet().HitAttack
            ?? Load("BossJAttackingHit", "BossJAttacking");
        /// <summary>Dungeon R40 Lord — high HP phase.</summary>
        public static Sprite BossB => _bossB ??= GetLordBossAnimSet(highPhase: true).Idle
            ?? Load("BossB", "Art/boss_j", "BossJ", "Placeholders/boss");
        /// <summary>Dungeon R40 Lord — low HP phase.</summary>
        public static Sprite BossB2 => _bossB2 ??= GetLordBossAnimSet(highPhase: false).Idle
            ?? Load("BossB2", "BossB", "Art/boss_j", "BossJ", "Placeholders/boss");
        public static Sprite GoldCoin => _goldCoin ??= Load(Admurin + "gold_bag", "GoldCoin");
        public static Sprite GoldCoinDropped => _goldCoinDropped ??= Load(Admurin + "gold_bag", "GoldCoinDropped", "GoldCoin");
        public static Sprite HpHeart => _hpHeart ??= Load(Admurin + "hp_potion", "HeartHP", "HPHeart");
        public static Sprite HpHeartDropped => _hpHeartDropped ??= Load(Admurin + "hp_potion", "HPHeartDropped", "HeartHP", "HPHeart");
        /// <summary>XP drop gem — Admurin sapphire sized for ground pickups (not tile PPU).</summary>
        public static Sprite XpGem => _xpGem ??=
            LoadLootSprite(Admurin + "xp_gem", 1.2f) ?? LoadOrCreateXpGem();
        /// <summary>Map loot crystal — Admurin morganite sized for ground pickups.</summary>
        public static Sprite PinkCrystal => _pinkCrystal ??=
            LoadLootSprite(Admurin + "pink_crystal", 1.35f) ?? CreatePinkCrystalSprite();
        /// <summary>Boss epic talent crystal — Admurin amethyst, slightly larger than pink loot.</summary>
        public static Sprite EpicCrystal => _epicCrystal ??=
            LoadLootSprite(Admurin + "epic_crystal", 1.25f) ?? PinkCrystal;
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
        /// <summary>Batter weapon — material tier from Unlimited Survival unlocks.</summary>
        public static Sprite BaseballBat => GetClassWeaponSprite(PlayerClass.Batter);
        // worldLength is in local sprite units; player root is ~0.55 scale so ~2.6 local ≈ 1.4 world.
        /// <summary>Spearman weapon — material tier from Unlimited Survival unlocks.</summary>
        public static Sprite Spear => GetClassWeaponSprite(PlayerClass.Spearman);
        /// <summary>Samurai weapon — Iron base, Steel from Unlimited Survival.</summary>
        public static Sprite Katana => GetClassWeaponSprite(PlayerClass.Samurai);
        /// <summary>Bowman weapon — material tier from Unlimited Survival unlocks.</summary>
        public static Sprite Bow => GetClassWeaponSprite(PlayerClass.Bowman);
        /// <summary>
        /// Arrow projectile (−25% size). Always a horizontal tip-on-+X sprite: both
        /// Admurin and Resources/Arrow.png are authored ~45° diagonal and would make
        /// Atan2 flight look permanently skewed.
        /// </summary>
        public static Sprite Arrow => _arrow ??= CreateHorizontalCombatArrow(1.29375f);

        /// <summary>
        /// Admurin skull projectiles for enemy/boss bolts (human → demon → titan sets).
        /// Sized for combat readability (~0.55 world units).
        /// </summary>
        public static Sprite GetSkullProjectile(int index)
        {
            EnsureSkullProjectiles();
            if (_skullProjectiles == null || _skullProjectiles.Length == 0)
                return GetBossFireBoltFrame(index);
            return _skullProjectiles[Mathf.Abs(index) % _skullProjectiles.Length];
        }

        public static Sprite GetRandomSkullProjectile() =>
            GetSkullProjectile(Random.Range(0, 12));

        static void EnsureSkullProjectiles()
        {
            if (_skullProjectiles != null) return;

            // Prefer combat-scaled Resources copies; fall back to full Admurin path names.
            var names = new[]
            {
                "skull_human", "skull_horned", "skull_demon", "skull_cyclops",
                "skull_canine", "skull_aquatic", "skull_titan_orc", "skull_orc_horned",
                "skull_orc_horned_b", "skull_titan_cyclops", "skull_titan_foureyed", "skull_titan_aquatic"
            };

            var list = new List<Sprite>(names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                // ~0.55 world units — readable without covering the player.
                var sprite = LoadWeaponSprite(Admurin + names[i], new Vector2(0.5f, 0.5f), 0.55f)
                             ?? LoadWeaponSprite(names[i], new Vector2(0.5f, 0.5f), 0.55f);
                if (sprite != null) list.Add(sprite);
            }

            _skullProjectiles = list.Count > 0 ? list.ToArray() : System.Array.Empty<Sprite>();
        }
        /// <summary>Magician weapon — material tier from Unlimited Survival unlocks.</summary>
        public static Sprite Staff => GetClassWeaponSprite(PlayerClass.Magician);

        /// <summary>
        /// Loads the held weapon for the current material tier (Iron/Steel from Unlimited depth).
        /// Falls back to wooden/base art if a higher-tier asset is missing.
        /// </summary>
        public static Sprite GetClassWeaponSprite(PlayerClass playerClass)
        {
            var path = WeaponCatalog.GetResourceName(playerClass);
            var sprite = LoadClassWeaponFromPath(playerClass, path);
            if (sprite != null) return sprite;

            var wooden = WeaponCatalog.GetResourceName(playerClass, WeaponMaterialTier.Wooden);
            if (wooden != path)
                sprite = LoadClassWeaponFromPath(playerClass, wooden);
            return sprite ?? CreateClassWeaponFallback(playerClass);
        }

        static Sprite LoadClassWeaponFromPath(PlayerClass playerClass, string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (_weaponSpriteCache.TryGetValue(resourcePath, out var cached) && cached != null)
                return cached;

            Sprite loaded = playerClass switch
            {
                PlayerClass.Batter => LoadWeaponSprite(resourcePath, new Vector2(0.12f, 0.5f), 3.6f),
                PlayerClass.Spearman => LoadWeaponSprite(resourcePath, new Vector2(0.1f, 0.5f), 4.125f)
                    ?? LoadWeaponSprite("Spear", new Vector2(0.1f, 0.5f), 4.125f),
                PlayerClass.Bowman => LoadWeaponSprite(resourcePath, new Vector2(0.35f, 0.5f), 2.1375f, flipHorizontal: true),
                PlayerClass.Magician => LoadWeaponSprite(resourcePath, new Vector2(0.12f, 0.5f), 3.525f),
                PlayerClass.Samurai => LoadWeaponSprite(resourcePath, new Vector2(0.1f, 0.5f), 3.75f)
                    ?? LoadWeaponSprite("Sword", new Vector2(0.1f, 0.5f), 3.75f),
                _ => LoadWeaponSprite(resourcePath, new Vector2(0.12f, 0.5f), 3.6f)
            };

            if (loaded != null)
                _weaponSpriteCache[resourcePath] = loaded;
            return loaded;
        }

        static Sprite CreateClassWeaponFallback(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Spearman => CreateSpearSprite(),
            PlayerClass.Bowman => LoadOrCreateBow(),
            PlayerClass.Magician => CreateSpearSprite(),
            PlayerClass.Samurai => CreateKatanaSprite(),
            _ => LoadOrCreateBat()
        };
        public static Sprite Sparkles => _sparkles ??= TryLoadSprite("Sparkles", TilePixelsPerUnit);
        public static Sprite Sparkles2 => _sparkles2 ??= TryLoadSprite("Sparkles2", TilePixelsPerUnit);
        /// <summary>Fortune Ring icon — Admurin fortitude ring (Sparkles remain VFX-only).</summary>
        public static Sprite FortuneRing => _fortuneRing ??=
            TryLoadSprite(Admurin + "fortune_ring", TilePixelsPerUnit) ?? Sparkles;
        /// <summary>Prism Ring icon — Admurin triple gem ring.</summary>
        public static Sprite PrismRing => _prismRing ??=
            TryLoadSprite(Admurin + "prism_ring", TilePixelsPerUnit) ?? Sparkles2;
        public static Sprite Necklace => _necklace ??=
            TryLoadSprite(Admurin + "jade_necklace", TilePixelsPerUnit) ?? TryLoadSprite("Necklace", TilePixelsPerUnit);
        public static Sprite SkullNecklace => _skullNecklace ??=
            TryLoadSprite(Admurin + "skull_necklace", TilePixelsPerUnit) ?? TryLoadSprite("Skull Necklace", TilePixelsPerUnit);
        public static Sprite TreasureChest => _treasureChest ??= CreateTreasureChestSprite();
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

        /// <summary>
        /// Neutral body pose only (sheet frame 0). Frames 1–7 raise/extend arms with tools —
        /// never use those for idle or movement (matches classic RollZy handling: Idle + WalkA/B
        /// slots only, with WalkA identical to idle).
        /// </summary>
        const int HeroNeutralFrame = 0;

        public static HeroSpriteSet GetHeroSprites(PlayableHero hero)
        {
            if (hero == PlayableHero.RowZi)
            {
                _rowZiSprites ??= LoadOrderedHeroSheet("RowZi_new")
                                  ?? LoadOrderedHeroSheet("RowZi")
                                  ?? LoadHeroSheetSprites("RowZi", 8);
                return BuildHeroMovementSet(_rowZiSprites, facesRightByDefault: true);
            }

            // Upgraded RollZy after clearing Dungeon survival (Dungeon Clearer achievement path).
            if (GameSave.DungeonSurvivalCleared)
            {
                _rollZyUpgradedSprites ??= LoadOrderedHeroSheet("RollZy_two")
                                           ?? LoadHeroSheetSprites("RollZy_two", 8);
                if (_rollZyUpgradedSprites != null && _rollZyUpgradedSprites.Length > 0
                    && _rollZyUpgradedSprites[0] != null)
                    return BuildHeroMovementSet(_rollZyUpgradedSprites, facesRightByDefault: true);
            }

            _rollZySprites ??= LoadOrderedHeroSheet("RollZy")
                               ?? LoadHeroSheetSprites("RollZy", 8);
            return BuildHeroMovementSet(_rollZySprites, facesRightByDefault: true);
        }

        /// <summary>
        /// Classic RollZy method: only Idle + WalkA + WalkB. WalkA is the same sprite as idle;
        /// WalkB is the same neutral pose (no arm-up / arm-out frames from the 8-frame sheet).
        /// </summary>
        static HeroSpriteSet BuildHeroMovementSet(Sprite[] frames, bool facesRightByDefault)
        {
            Sprite Frame(int i)
            {
                if (frames == null || frames.Length == 0) return null;
                if (i >= 0 && i < frames.Length && frames[i] != null) return frames[i];
                return frames[0];
            }

            var neutral = Frame(HeroNeutralFrame);
            return new HeroSpriteSet
            {
                Idle = neutral,
                WalkA = neutral,
                WalkB = neutral,
                FacesRightByDefault = facesRightByDefault
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
            // Inside survival: diamond checkerboard floor (Resources root asset).
            _insideTiles ??= BuildTileSet("Diamond Checkerboard Tile");
            return _insideTiles[Mathf.Abs(index) % _insideTiles.Length];
        }

        public static Sprite GetDungeonTile(int index)
        {
            // Dungeon survival: the other new floor tile only (no alternating set).
            _dungeonTiles ??= BuildTileSet("Roof Tiles");
            return _dungeonTiles[Mathf.Abs(index) % _dungeonTiles.Length];
        }

        static Sprite[] BuildTileSet(params string[] paths)
        {
            var list = new System.Collections.Generic.List<Sprite>(paths.Length);
            for (var i = 0; i < paths.Length; i++)
            {
                // Prefer full-texture rebuild at tile PPU so updated Resources art always applies.
                var sprite = LoadFloorTileSprite(paths[i]) ?? TryLoadSprite(paths[i], TilePixelsPerUnit);
                if (sprite != null) list.Add(sprite);
            }

            if (list.Count == 0)
                list.Add(CreateTileFallback("fallback_tile"));
            return list.ToArray();
        }

        /// <summary>
        /// Loads a floor tile from Resources as a full-rect sprite at <see cref="TilePixelsPerUnit"/>.
        /// Uses the texture (not a cached multi-sprite sub-asset) so art swaps under the same name show up.
        /// </summary>
        static Sprite LoadFloorTileSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                // Multi-sprite assets still expose the texture via LoadAll.
                var sprites = Resources.LoadAll<Sprite>(path);
                if (sprites != null && sprites.Length > 0 && sprites[0] != null && sprites[0].texture != null)
                    texture = sprites[0].texture;
            }

            if (texture == null) return null;

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                TilePixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;
            return sprite;
        }

        public static Sprite GetFireBreathFrame(int frame)
        {
            EnsureFireBreathFrames();
            var index = Mathf.Abs(frame) % _fireBreathFrames.Length;
            return _fireBreathFrames[index];
        }

        public static Sprite GetWeaponFireFrame(int frame)
        {
            EnsureWeaponFireFrames();
            return _weaponFireFrames[Mathf.Abs(frame) % _weaponFireFrames.Length];
        }

        public static Sprite GetEnemyBurnFrame(int frame)
        {
            EnsureEnemyBurnFrames();
            return _enemyBurnFrames[Mathf.Abs(frame) % _enemyBurnFrames.Length];
        }

        public static Sprite GetBossFireBoltFrame(int frame)
        {
            EnsureBossFireBoltFrames();
            return _bossFireBoltFrames[Mathf.Abs(frame) % _bossFireBoltFrames.Length];
        }

        static void EnsureWeaponFireFrames()
        {
            if (_weaponFireFrames != null) return;
            _weaponFireFrames = new Sprite[4];
            for (var i = 0; i < 4; i++)
                _weaponFireFrames[i] = Load($"WeaponFire{i + 1}") ?? CreateTinyFlameSprite(i, 12, 16);
        }

        static void EnsureEnemyBurnFrames()
        {
            if (_enemyBurnFrames != null) return;
            _enemyBurnFrames = new Sprite[4];
            for (var i = 0; i < 4; i++)
                _enemyBurnFrames[i] = Load($"EnemyBurn{i + 1}") ?? CreateTinyFlameSprite(i, 14, 14);
        }

        static void EnsureBossFireBoltFrames()
        {
            if (_bossFireBoltFrames != null) return;
            _bossFireBoltFrames = new Sprite[3];
            for (var i = 0; i < 3; i++)
            {
                // Prefer dedicated bolt art; fall back to FireBreath frames as a simple placeholder.
                var dedicated = Load($"BossFireBolt{i + 1}");
                if (dedicated != null)
                {
                    _bossFireBoltFrames[i] = dedicated;
                    continue;
                }

                var breath = Load($"FireBreath{i + 1}");
                if (breath != null && breath.texture != null)
                {
                    // Compact projectile: center pivot, slightly smaller visual.
                    _bossFireBoltFrames[i] = Sprite.Create(
                        breath.texture,
                        breath.rect,
                        new Vector2(0.5f, 0.5f),
                        (breath.pixelsPerUnit > 0f ? breath.pixelsPerUnit : TilePixelsPerUnit) * 1.8f,
                        0,
                        SpriteMeshType.FullRect);
                    _bossFireBoltFrames[i].name = $"BossFireBolt_placeholder_{i + 1}";
                }
                else
                {
                    _bossFireBoltFrames[i] = CreateTinyFlameSprite(i, 16, 10);
                }
            }
        }

        static Sprite CreateTinyFlameSprite(int frame, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var core = new Color(1f, 0.95f, 0.55f, 1f);
            var mid = new Color(1f, 0.55f, 0.12f, 1f);
            var edge = new Color(0.95f, 0.2f, 0.05f, 0.9f);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

            var cx = w / 2;
            var flicker = frame % 4;
            for (var y = 1; y < h - 1; y++)
            {
                var width = Mathf.Max(1, (h - y) / 3 + (flicker == y % 4 ? 1 : 0));
                for (var dx = -width; dx <= width; dx++)
                {
                    var x = cx + dx;
                    if (x < 0 || x >= w) continue;
                    var t = (float)y / h;
                    var c = t > 0.65f ? core : t > 0.35f ? mid : edge;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 8f);
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
            var set = GetEnemyAnimSet(kind);
            if (set.IsValid)
            {
                idle = set.Idle;
                hit = set.Hit ?? set.Idle;
                return;
            }

            // Legacy ZombieJ fallbacks if Resources/Monsters is missing.
            switch (kind)
            {
                case EnemyZombieKind.InsideElite:
                    idle = TryLoadSprite("ZombieJ_Inside2", TilePixelsPerUnit)
                           ?? TryLoadSprite("Placeholders/zombie", TilePixelsPerUnit);
                    hit = TryLoadSprite("ZombieJ_Inside2Hit", TilePixelsPerUnit) ?? idle;
                    break;
                case EnemyZombieKind.Inside:
                    idle = TryLoadSprite("ZombieJ_Inside", TilePixelsPerUnit)
                           ?? TryLoadSprite("Placeholders/zombie", TilePixelsPerUnit);
                    hit = TryLoadSprite("ZombieJ_InsideHit", TilePixelsPerUnit) ?? idle;
                    break;
                default:
                    idle = TryLoadSprite("ZombieJ", TilePixelsPerUnit)
                           ?? TryLoadSprite("Art/zombie_j", TilePixelsPerUnit)
                           ?? TryLoadSprite("Placeholders/zombie", TilePixelsPerUnit);
                    hit = TryLoadSprite("ZombieJHit", TilePixelsPerUnit) ?? idle;
                    break;
            }
        }

        /// <summary>Random demon pack for regular enemies by map tier.</summary>
        public static MonsterAnimSet GetEnemyAnimSet(EnemyZombieKind kind)
        {
            var pool = kind switch
            {
                EnemyZombieKind.InsideElite => EliteDemonSets,
                EnemyZombieKind.Inside => InsideDemonSets,
                _ => OutsideDemonSets
            };
            return LoadRandomMonsterSet(pool);
        }

        /// <summary>Warlock / bat / wing packs for late-map ranged enemies.</summary>
        public static MonsterAnimSet GetRangedEnemyAnimSet() =>
            LoadRandomMonsterSet(RangedDemonSets);

        /// <summary>Golem boss set (Outside R20 / Inside R30 / regular bosses).</summary>
        public static MonsterAnimSet GetGolemBossAnimSet() =>
            LoadRandomMonsterSet(GolemBossSets);

        /// <summary>Lord boss set for Dungeon R40 (phase by HP).</summary>
        public static MonsterAnimSet GetLordBossAnimSet(bool highPhase) =>
            LoadRandomMonsterSet(highPhase ? LordBossHighSets : LordBossLowSets);

        static MonsterAnimSet LoadRandomMonsterSet(string[] pool)
        {
            if (pool == null || pool.Length == 0)
                return default;

            // Prefer a random loaded set; fall through pool if a folder is missing.
            var start = Random.Range(0, pool.Length);
            for (var n = 0; n < pool.Length; n++)
            {
                var key = pool[(start + n) % pool.Length];
                var set = LoadMonsterAnimSet(key);
                if (set.IsValid) return set;
            }

            return default;
        }

        /// <summary>
        /// Loads a curated Resources/Monsters/{relative} animation folder.
        /// Frames: stand_*, walk_*, attack_* (hit uses mid attack frame).
        /// </summary>
        public static MonsterAnimSet LoadMonsterAnimSet(string relativeFolder)
        {
            if (string.IsNullOrEmpty(relativeFolder)) return default;
            if (MonsterSetCache.TryGetValue(relativeFolder, out var cached) && cached.IsValid)
                return cached;

            var folder = Monsters + relativeFolder.Trim('/');
            var stand = LoadFrameSequence(folder, "stand", 6);
            var walk = LoadFrameSequence(folder, "walk", 6);
            var attack = LoadFrameSequence(folder, "attack", 6);
            // Lord packs also ship attack2 / sword — use as extra attack flair if main attack missing.
            if (attack.Length == 0)
                attack = LoadFrameSequence(folder, "attack2", 6);
            if (attack.Length == 0)
                attack = LoadFrameSequence(folder, "sword", 6);

            var idle = FirstOrNull(stand) ?? FirstOrNull(walk) ?? FirstOrNull(attack);
            if (idle == null)
            {
                MonsterSetCache[relativeFolder] = default;
                return default;
            }

            var hit = attack.Length >= 3 ? attack[2] : attack.Length > 0 ? attack[0] : idle;
            var hitAttack = attack.Length >= 5 ? attack[4] : hit;
            var attackPose = attack.Length >= 4 ? attack[3] : attack.Length > 0 ? attack[^1] : idle;

            var folderKey = relativeFolder.ToLowerInvariant();
            var set = new MonsterAnimSet
            {
                Idle = idle,
                Hit = hit,
                Attack = attackPose,
                HitAttack = hitAttack,
                StandFrames = stand.Length > 0 ? stand : new[] { idle },
                WalkFrames = walk.Length > 0 ? walk : stand.Length > 0 ? stand : new[] { idle },
                AttackFrames = attack.Length > 0 ? attack : new[] { attackPose },
                FacesRightByDefault = true,
                IsFlying = folderKey.Contains("bat") || folderKey.Contains("wing")
            };
            MonsterSetCache[relativeFolder] = set;
            return set;
        }

        static Sprite[] LoadFrameSequence(string folder, string prefix, int maxFrames)
        {
            var frames = new List<Sprite>(maxFrames);
            for (var i = 1; i <= maxFrames; i++)
            {
                var sprite = TryLoadSprite($"{folder}/{prefix}_{i}", TilePixelsPerUnit);
                if (sprite == null) break;
                frames.Add(sprite);
            }

            return frames.ToArray();
        }

        static Sprite FirstOrNull(Sprite[] frames)
        {
            if (frames == null) return null;
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) return frames[i];
            }

            return null;
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

        /// <summary>
        /// Loads a multi-sprite sheet from Resources and sorts by trailing frame index
        /// (supports "RollZy_0", "RollZy_two_3", "RowZi 1_0", etc.).
        /// </summary>
        static Sprite[] LoadOrderedHeroSheet(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName)) return null;
            var fromSheet = Resources.LoadAll<Sprite>(sheetName);
            if (fromSheet == null || fromSheet.Length == 0) return null;

            var filtered = new System.Collections.Generic.List<Sprite>(fromSheet.Length);
            foreach (var sprite in fromSheet)
            {
                if (sprite != null) filtered.Add(sprite);
            }

            if (filtered.Count == 0) return null;
            filtered.Sort((a, b) => GetTrailingFrameIndex(a.name).CompareTo(GetTrailingFrameIndex(b.name)));
            return filtered.ToArray();
        }

        static Sprite[] LoadSheetSprites(string sheetName, int expectedCount)
        {
            var ordered = LoadOrderedHeroSheet(sheetName);
            if (ordered != null && ordered.Length > 0) return ordered;
            return LoadHeroSheetSprites(sheetName, expectedCount);
        }

        static int GetTrailingFrameIndex(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return int.MaxValue;
            var underscore = spriteName.LastIndexOf('_');
            if (underscore < 0 || underscore >= spriteName.Length - 1) return int.MaxValue;
            return int.TryParse(spriteName[(underscore + 1)..], out var index) ? index : int.MaxValue;
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

        /// <summary>
        /// Loads a ground-loot icon and rebuilds it at a readable world size.
        /// Tiny Admurin singles at tile PPU (64) are nearly invisible as pickups.
        /// </summary>
        static Sprite LoadLootSprite(string resourceName, float worldSize)
        {
            return LoadWeaponSprite(resourceName, new Vector2(0.5f, 0.5f), worldSize);
        }

        /// <summary>
        /// Loads a held weapon sprite and rebuilds it at a combat-readable world length.
        /// Uploaded 16×16 art at 100 PPU is only ~0.16 units — effectively invisible on the hero.
        /// Optional horizontal flip (bow only) when grip/tip orientation needs correction.
        /// </summary>
        static Sprite LoadWeaponSprite(string resourceName, Vector2 pivot, float worldLength, bool flipHorizontal = false)
        {
            if (string.IsNullOrEmpty(resourceName) || worldLength < 0.05f) return null;

            Sprite source = null;
            var multi = Resources.LoadAll<Sprite>(resourceName);
            if (multi != null && multi.Length > 0)
                source = multi[0];
            if (source == null)
                source = Resources.Load<Sprite>(resourceName);

            if (source != null)
            {
                var rect = source.textureRect;
                Texture2D tex = source.texture;
                if (flipHorizontal)
                {
                    var flipped = CreateHorizontallyFlippedTexture(source.texture, rect);
                    if (flipped != null)
                    {
                        tex = flipped;
                        rect = new Rect(0f, 0f, flipped.width, flipped.height);
                    }
                }

                var ppu = Mathf.Max(1f, rect.width / worldLength);
                var rebuilt = Sprite.Create(
                    tex,
                    rect,
                    pivot,
                    ppu,
                    0,
                    SpriteMeshType.FullRect);
                if (rebuilt != null)
                {
                    rebuilt.name = resourceName + (flipHorizontal ? "_WeaponFlip" : "_Weapon");
                    return rebuilt;
                }

                // If Create fails (platform/readability), fall back to the imported sprite.
                return source;
            }

            var texture = Resources.Load<Texture2D>(resourceName);
            if (texture == null) return null;

            if (flipHorizontal)
            {
                var flipped = CreateHorizontallyFlippedTexture(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height));
                if (flipped != null)
                    texture = flipped;
            }

            var fullPpu = Mathf.Max(1f, texture.width / worldLength);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                pivot,
                fullPpu,
                0,
                SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Copies a texture rect and mirrors it on X so weapon tips point +X from the grip pivot.
        /// </summary>
        static Texture2D CreateHorizontallyFlippedTexture(Texture2D source, Rect rect)
        {
            if (source == null) return null;

            var width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            var height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            var x = Mathf.RoundToInt(rect.x);
            var y = Mathf.RoundToInt(rect.y);

            Color[] pixels;
            try
            {
                pixels = source.GetPixels(x, y, width, height);
            }
            catch
            {
                // Texture not readable — skip flip and keep original orientation.
                return null;
            }

            var flipped = new Color[pixels.Length];
            for (var row = 0; row < height; row++)
            {
                var rowStart = row * width;
                for (var col = 0; col < width; col++)
                    flipped[rowStart + col] = pixels[rowStart + (width - 1 - col)];
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = source.name + "_flipX"
            };
            tex.SetPixels(flipped);
            tex.Apply(false, true);
            return tex;
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

        /// <summary>Bright arrow projectile — wood shaft, metal tip on +X, red fletching.</summary>
        static Sprite CreateArrowSprite() => CreateHorizontalCombatArrow(2.25f);

        /// <summary>Horizontal combat arrow: tip on +X, sized to worldLength units.</summary>
        static Sprite CreateHorizontalCombatArrow(float worldLength)
        {
            const int w = 40;
            const int h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

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
            for (var x = 5; x <= 30; x++)
            {
                Set(x, 4, shaftEdge);
                Set(x, 5, shaft);
                Set(x, 6, shaft);
                Set(x, 7, shaftEdge);
            }

            // Metal tip.
            for (var i = 0; i < 7; i++)
            {
                var x = 31 + i;
                var half = Mathf.Max(0, 3 - i / 2);
                for (var dy = -half; dy <= half; dy++)
                    Set(x, 5 + dy + 1, i >= 5 ? tipEdge : tip);
            }

            // Fletching on the nock (−X).
            for (var x = 1; x <= 6; x++)
            {
                Set(x, 2, fletchDark);
                Set(x, 3, fletch);
                Set(x, 8, fletch);
                Set(x, 9, fletchDark);
            }

            tex.Apply();
            var ppu = Mathf.Max(1f, w / Mathf.Max(0.05f, worldLength));
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.12f, 0.5f),
                ppu,
                0,
                SpriteMeshType.FullRect);
            if (sprite != null) sprite.name = "Arrow_Horizontal";
            return sprite;
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

        static Sprite CreateTreasureChestSprite()
        {
            const int w = 28;
            const int h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            void Set(int x, int y, Color c)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
            }

            var clear = new Color(0, 0, 0, 0);
            var wood = new Color(0.45f, 0.28f, 0.12f);
            var woodDark = new Color(0.28f, 0.16f, 0.07f);
            var metal = new Color(0.78f, 0.68f, 0.22f);
            var lid = new Color(0.55f, 0.34f, 0.14f);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Set(x, y, clear);

            for (var y = 2; y < 14; y++)
            for (var x = 3; x < 25; x++)
                Set(x, y, y < 4 || y > 11 ? woodDark : wood);

            for (var y = 14; y < 21; y++)
            for (var x = 2; x < 26; x++)
                Set(x, y, lid);

            for (var y = 6; y < 12; y++)
            for (var x = 12; x < 16; x++)
                Set(x, y, metal);

            Set(13, 8, woodDark); Set(14, 8, woodDark);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 4f);
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