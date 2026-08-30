using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Persistent camp progression. Only gold and permanent upgrades are saved between runs.
    /// Run XP never touches this class.
    /// </summary>
    public static class GameSave
    {
        const string GoldKey = "zx_gold";
        const string HpLevelKey = "zx_up_hp";
        const string DmgLevelKey = "zx_up_dmg";
        const string SpdLevelKey = "zx_up_spd";
        const string RangeLevelKey = "zx_up_range";
        const string InsideUnlockedKey = "zx_inside_unlocked";
        const string DungeonUnlockedKey = "zx_dungeon_unlocked";
        const string CryptUnlockedKey = "zx_crypt_unlocked";
        const string WhirlwindKey = "zx_whirlwind";
        const string PiercingShotKey = "zx_piercing_shot";
        const string FrostTipKey = "zx_frost_tip";
        const string GoldMagnetKey = "zx_gold_magnet";
        const string ThickHideKey = "zx_thick_hide";
        const string SecondWindKey = "zx_second_wind";
        const string CampfireBlessingKey = "zx_campfire_blessing";
        const string InsideClearedKey = "zx_inside_cleared";
        const string DungeonClearedKey = "zx_dungeon_cleared";
        const string CryptClearedKey = "zx_crypt_cleared";
        const string FlameEnchantKey = "zx_flame_enchant";
        const string UnlimitedUnlockedKey = "zx_unlimited_unlocked";
        const string SpearmanUnlockedKey = "zx_spearman_unlocked";
        const string BowmanUnlockedKey = "zx_bowman_unlocked";
        const string MagicianUnlockedKey = "zx_magician_unlocked";
        const string SamuraiUnlockedKey = "zx_samurai_unlocked";
        const string SelectedClassKey = "zx_selected_class";
        const string ClassRollZyKey = "zx_class_rollzy";
        const string ClassRowZiKey = "zx_class_rowzi";
        const string SelectedHeroKey = "zx_selected_hero";
        const string MovementControlKey = "zx_movement_control";
        const string JoystickPosXKey = "zx_joystick_pos_x";
        const string JoystickPosYKey = "zx_joystick_pos_y";
        const string BgmVolumeKey = "zx_bgm_volume";
        const string SfxVolumeKey = "zx_sfx_volume";
        const string BgmGenreKey = "zx_bgm_genre";
        public const string BgmGenreDnB = "DnB";
        public const string BgmGenreMetal = "Metal";
        const string RowZiUnlockedKey = "zx_rowzi_unlocked";
        const string AttackModeBatterKey = "zx_attack_batter";
        const string AttackModeSpearmanKey = "zx_attack_spearman";
        const string AttackModeBowmanKey = "zx_attack_bowman";
        const string AttackModeMagicianKey = "zx_attack_magician";
        const string AttackModeSamuraiKey = "zx_attack_samurai";
        const string ZombieKillsKey = "zx_lifetime_zombie_kills";
        const string BossKillsKey = "zx_lifetime_boss_kills";
        const string DeathsKey = "zx_lifetime_deaths";
        const string GoldEarnedKey = "zx_lifetime_gold_earned";
        const string HighestRoundKey = "zx_highest_round";
        const string UnlimitedHighestRoundKey = "zx_unlimited_highest_round";
        const string DungeonHighestRoundKey = "zx_dungeon_highest_round";
        const string CryptHighestRoundKey = "zx_crypt_highest_round";
        const string OwnedEquipmentKey = "zx_owned_equipment";
        const string EquippedRingKey = "zx_equipped_ring";
        const string EquippedNecklaceKey = "zx_equipped_necklace";
        const string EquippedCapeKey = "zx_equipped_cape";
        const string EquippedHelmKey = "zx_equipped_helm";
        const string QuestGwpAcceptedKey = "zx_quest_gwp_accepted";
        const string QuestGwpCompletedKey = "zx_quest_gwp_completed";
        const string QuestWardensPathAcceptedKey = "zx_quest_wardens_path_accepted";
        const string QuestWardensPathBossKey = "zx_quest_wardens_path_boss";
        const string QuestWardensPathCompletedKey = "zx_quest_wardens_path_completed";
        const string QuestLyraAcceptedKey = "zx_quest_lyra_accepted";
        const string QuestLyraBossKey = "zx_quest_lyra_boss";
        const string QuestLyraCompletedKey = "zx_quest_lyra_completed";
        const string QuestBrenAcceptedKey = "zx_quest_bren_accepted";
        const string QuestBrenMilestoneKey = "zx_quest_bren_milestone";
        const string QuestBrenCompletedKey = "zx_quest_bren_completed";
        const string QuestCorvinOmenAcceptedKey = "zx_quest_corvin_omen_accepted";
        const string QuestCorvinOmenMilestoneKey = "zx_quest_corvin_omen_milestone";
        const string QuestCorvinOmenCompletedKey = "zx_quest_corvin_omen_completed";
        const string QuestKaelAcceptedKey = "zx_quest_kael_accepted";
        const string QuestKaelMilestoneKey = "zx_quest_kael_milestone";
        const string QuestKaelCompletedKey = "zx_quest_kael_completed";
        const string QuestNessaAcceptedKey = "zx_quest_nessa_accepted";
        const string QuestNessaMilestoneKey = "zx_quest_nessa_milestone";
        const string QuestNessaCompletedKey = "zx_quest_nessa_completed";
        const string QuestGarrickAcceptedKey = "zx_quest_garrick_accepted";
        const string QuestGarrickMilestoneKey = "zx_quest_garrick_milestone";
        const string QuestGarrickCompletedKey = "zx_quest_garrick_completed";
        const string QuestToveAcceptedKey = "zx_quest_tove_accepted";
        const string QuestToveMilestoneKey = "zx_quest_tove_milestone";
        const string QuestToveCompletedKey = "zx_quest_tove_completed";
        const string QuestCrowAcceptedKey = "zx_quest_crow_accepted";
        const string QuestCrowRescuedKey = "zx_quest_crow_rescued";
        const string QuestCrowCompletedKey = "zx_quest_crow_completed";
        const string TwinLightningPendantKey = "zx_item_twin_lightning_pendant";
        const string DungeonKnightReturnedKey = "zx_dungeon_knight_returned";
        const string QuestKnightAcceptedKey = "zx_quest_knight_accepted";
        const string QuestKnightCompletedKey = "zx_quest_knight_completed";
        const string KnightsGreatswordKey = "zx_item_knights_greatsword";
        const string WeaponProgressMigratedKey = "zx_weapon_progress_migrated_v1";
        const string RollZySkinKey = "zx_rollzy_skin";
        const string OnboardingDoneKey = "zx_onboarding_done";
        const string LargeDamageNumbersKey = "zx_large_damage_numbers";
        const string SettingsOpenedKey = "zx_settings_opened";
        const string CharacterCreatedKey = "zx_character_created";
        const string CharacterAppearanceKey = "zx_character_appearance";
        const string CharacterGenderKey = "zx_character_gender";
        public const string CharacterGenderMale = "Male";
        public const string CharacterGenderFemale = "Female";
        const string CharacterMigratedKey = "zx_character_migrated_v1";

        /// <summary>Gold banked from the most recent survival exit (death, retreat, or portal).</summary>
        public static int LastRunGoldBanked { get; set; }

        /// <summary>Round reached on the most recent survival exit (for camp toast / recap).</summary>
        public static int LastRunRound { get; set; }

        /// <summary>Kills during the most recent survival run.</summary>
        public static int LastRunKills { get; set; }

        /// <summary>True if the last survival exit was a death (vs retreat / clear).</summary>
        public static bool LastRunWasDeath { get; set; }

        public static bool OnboardingCompleted
        {
            get => PlayerPrefs.GetInt(OnboardingDoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(OnboardingDoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// True after the player finished (or was migrated through) the HeroEditor creator.
        /// RollZy-only cosmetics; RowZi stays sheet-based.
        /// </summary>
        public static bool CharacterCreated
        {
            get => PlayerPrefs.GetInt(CharacterCreatedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(CharacterCreatedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>JsonUtility dump of HeroEditor CharacterAppearance for RollZy.</summary>
        public static string CharacterAppearanceJson
        {
            get => PlayerPrefs.GetString(CharacterAppearanceKey, string.Empty);
            set
            {
                PlayerPrefs.SetString(CharacterAppearanceKey, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Creator gender preset for RollZy (Male default). Cosmetics only — same Human body.</summary>
        public static string CharacterGender
        {
            get
            {
                var raw = PlayerPrefs.GetString(CharacterGenderKey, CharacterGenderMale);
                if (string.Equals(raw, CharacterGenderFemale, System.StringComparison.OrdinalIgnoreCase))
                    return CharacterGenderFemale;
                return CharacterGenderMale;
            }
            set
            {
                var next = string.Equals(value, CharacterGenderFemale, System.StringComparison.OrdinalIgnoreCase)
                    ? CharacterGenderFemale
                    : CharacterGenderMale;
                PlayerPrefs.SetString(CharacterGenderKey, next);
                PlayerPrefs.Save();
            }
        }

        public static bool IsFemaleCharacter =>
            string.Equals(CharacterGender, CharacterGenderFemale, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Default HeroEditor look for a gender preset (same Human body; face/hair differ).</summary>
        public static string CreateAppearanceJsonForGender(string gender)
        {
            return string.Equals(gender, CharacterGenderFemale, System.StringComparison.OrdinalIgnoreCase)
                ? CreateFemaleAppearanceJson()
                : CreateDefaultAppearanceJson();
        }

        public static string CreateFemaleAppearanceJson()
        {
            return "{\"Hair\":\"Common.Basic.Hair.LongHair\",\"Beard\":null,\"Ears\":\"Common.Basic.Ears.HumanEars\",\"Eyebrows\":\"Common.Basic.Eyebrows.Eyebrows1\",\"Eyes\":\"Common.Basic.Eyes.Female\",\"Mouth\":\"Common.Basic.Mouth.Normal\",\"Head\":\"Common.Basic.Head.Human\",\"HairColor\":{\"r\":90,\"g\":55,\"b\":30,\"a\":255},\"BeardColor\":{\"r\":90,\"g\":55,\"b\":30,\"a\":255},\"EyesColor\":{\"r\":0,\"g\":200,\"b\":255,\"a\":255},\"BodyColor\":{\"r\":255,\"g\":210,\"b\":170,\"a\":255}}";
        }

        /// <summary>
        /// Veterans with existing progress skip the creator and get a default appearance.
        /// Brand-new installs keep CharacterCreated false until the maker completes.
        /// </summary>
        public static void EnsureCharacterAppearanceMigrated()
        {
            if (PlayerPrefs.GetInt(CharacterMigratedKey, 0) == 1) return;

            PlayerPrefs.SetInt(CharacterMigratedKey, 1);

            if (CharacterCreated)
            {
                PlayerPrefs.Save();
                return;
            }

            var isVeteran = OnboardingCompleted
                            || Gold > 0
                            || LifetimeZombieKills > 0
                            || HighestRoundReached > 0
                            || UnlimitedHighestRoundReached > 0
                            || DungeonHighestRoundReached > 0
                            || CryptHighestRoundReached > 0
                            || PlayerPrefs.GetInt(OwnedEquipmentKey, 0) != 0;

            if (isVeteran)
            {
                if (string.IsNullOrEmpty(CharacterAppearanceJson))
                    CharacterAppearanceJson = CreateDefaultAppearanceJson();
                CharacterCreated = true;
            }

            PlayerPrefs.Save();
        }

        public static string CreateDefaultAppearanceJson()
        {
            // Male preset — HeroEditor defaults (BuzzCut + Male eyes + warm body).
            return "{\"Hair\":\"Common.Basic.Hair.BuzzCut\",\"Beard\":null,\"Ears\":\"Common.Basic.Ears.HumanEars\",\"Eyebrows\":\"Common.Basic.Eyebrows.Eyebrows1\",\"Eyes\":\"Common.Basic.Eyes.Male\",\"Mouth\":\"Common.Basic.Mouth.Normal\",\"Head\":\"Common.Basic.Head.Human\",\"HairColor\":{\"r\":150,\"g\":50,\"b\":0,\"a\":255},\"BeardColor\":{\"r\":150,\"g\":50,\"b\":0,\"a\":255},\"EyesColor\":{\"r\":0,\"g\":200,\"b\":255,\"a\":255},\"BodyColor\":{\"r\":255,\"g\":200,\"b\":120,\"a\":255}}";
        }

        /// <summary>
        /// Fixed HeroEditor look for RowZi — same Human rig as RollZy, clearly different hair/face/colors
        /// so she is never the pink robot sheet and never a clone of the player's saved appearance.
        /// </summary>
        public static string CreateRowZiAppearanceJson()
        {
            return "{\"Hair\":\"Common.Basic.Hair.LongHair\",\"Beard\":null,\"Ears\":\"Common.Basic.Ears.HumanEars\",\"Eyebrows\":\"Common.Basic.Eyebrows.Eyebrows1\",\"Eyes\":\"Common.Basic.Eyes.Female\",\"Mouth\":\"Common.Basic.Mouth.Smirk\",\"Head\":\"Common.Basic.Head.Human\",\"HairColor\":{\"r\":45,\"g\":35,\"b\":90,\"a\":255},\"BeardColor\":{\"r\":45,\"g\":35,\"b\":90,\"a\":255},\"EyesColor\":{\"r\":90,\"g\":210,\"b\":140,\"a\":255},\"BodyColor\":{\"r\":255,\"g\":215,\"b\":175,\"a\":255}}";
        }

        /// <summary>Accessibility: larger combat damage floaters.</summary>
        public static bool LargeDamageNumbers
        {
            get => PlayerPrefs.GetInt(LargeDamageNumbersKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(LargeDamageNumbersKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>True after the player has opened Settings at least once (hides joystick tip).</summary>
        public static bool HasOpenedSettings
        {
            get => PlayerPrefs.GetInt(SettingsOpenedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SettingsOpenedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void RecordLastRunSummary(int goldBanked, int round, int kills, bool died)
        {
            LastRunGoldBanked = Mathf.Max(0, goldBanked);
            LastRunRound = Mathf.Max(0, round);
            LastRunKills = Mathf.Max(0, kills);
            LastRunWasDeath = died;
        }

        static string WeaponKillsKey(PlayerClass c) => $"zx_weapon_kills_{(int)c}";
        static string WeaponDungeonKey(PlayerClass c) => $"zx_weapon_dungeon_{(int)c}";
        static string WeaponUnlimitedKey(PlayerClass c) => $"zx_weapon_unlimited_{(int)c}";
        static string WeaponEquippedKey(PlayerClass c) => $"zx_weapon_equipped_{(int)c}";

        /// <summary>
        /// One-time: copy legacy global Dungeon/Unlimited bests onto every class so existing
        /// players keep their weapon tiers. Gold (now kill-based) is seeded if they had R50+.
        /// New progress is always recorded only for the class that earned it.
        /// </summary>
        public static void EnsureWeaponProgressMigrated()
        {
            if (PlayerPrefs.GetInt(WeaponProgressMigratedKey, 0) == 1) return;

            var globalDungeon = DungeonHighestRoundReached;
            var globalUnlimited = UnlimitedHighestRoundReached;
            var seedGoldKills = globalUnlimited >= 50 ? WeaponCatalog.GoldUnlockKills : 0;

            foreach (PlayerClass c in System.Enum.GetValues(typeof(PlayerClass)))
            {
                if (!PlayerPrefs.HasKey(WeaponDungeonKey(c)))
                    PlayerPrefs.SetInt(WeaponDungeonKey(c), globalDungeon);
                if (!PlayerPrefs.HasKey(WeaponUnlimitedKey(c)))
                    PlayerPrefs.SetInt(WeaponUnlimitedKey(c), globalUnlimited);
                if (!PlayerPrefs.HasKey(WeaponKillsKey(c)))
                    PlayerPrefs.SetInt(WeaponKillsKey(c), seedGoldKills);
            }

            PlayerPrefs.SetInt(WeaponProgressMigratedKey, 1);
            PlayerPrefs.Save();
        }

        public static int GetWeaponKillCount(PlayerClass playerClass)
        {
            EnsureWeaponProgressMigrated();
            return Mathf.Max(0, PlayerPrefs.GetInt(WeaponKillsKey(playerClass), 0));
        }

        /// <summary>Returns previous kill count before the increment (for unlock banners).</summary>
        public static int AddWeaponKill(PlayerClass playerClass)
        {
            EnsureWeaponProgressMigrated();
            var prev = GetWeaponKillCount(playerClass);
            PlayerPrefs.SetInt(WeaponKillsKey(playerClass), prev + 1);
            PlayerPrefs.Save();
            return prev;
        }

        public static int GetWeaponDungeonBest(PlayerClass playerClass)
        {
            EnsureWeaponProgressMigrated();
            return Mathf.Max(0, PlayerPrefs.GetInt(WeaponDungeonKey(playerClass), 0));
        }

        public static bool RecordWeaponDungeonRound(PlayerClass playerClass, int round)
        {
            EnsureWeaponProgressMigrated();
            if (round <= GetWeaponDungeonBest(playerClass)) return false;
            PlayerPrefs.SetInt(WeaponDungeonKey(playerClass), round);
            PlayerPrefs.Save();
            return true;
        }

        public static int GetWeaponUnlimitedBest(PlayerClass playerClass)
        {
            EnsureWeaponProgressMigrated();
            return Mathf.Max(0, PlayerPrefs.GetInt(WeaponUnlimitedKey(playerClass), 0));
        }

        public static bool RecordWeaponUnlimitedRound(PlayerClass playerClass, int round)
        {
            EnsureWeaponProgressMigrated();
            if (round <= GetWeaponUnlimitedBest(playerClass)) return false;
            PlayerPrefs.SetInt(WeaponUnlimitedKey(playerClass), round);
            PlayerPrefs.Save();
            return true;
        }

        public static int Gold
        {
            get => PlayerPrefs.GetInt(GoldKey, 0);
            set
            {
                PlayerPrefs.SetInt(GoldKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static int HpUpgradeLevel
        {
            get => PlayerPrefs.GetInt(HpLevelKey, 0);
            set { PlayerPrefs.SetInt(HpLevelKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int DamageUpgradeLevel
        {
            get => PlayerPrefs.GetInt(DmgLevelKey, 0);
            set { PlayerPrefs.SetInt(DmgLevelKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int SpeedUpgradeLevel
        {
            get => PlayerPrefs.GetInt(SpdLevelKey, 0);
            set { PlayerPrefs.SetInt(SpdLevelKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int RangeUpgradeLevel
        {
            get => PlayerPrefs.GetInt(RangeLevelKey, 0);
            set { PlayerPrefs.SetInt(RangeLevelKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static bool InsideMapUnlocked
        {
            get => PlayerPrefs.GetInt(InsideUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(InsideUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool DungeonMapUnlocked
        {
            get => PlayerPrefs.GetInt(DungeonUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DungeonUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool CryptMapUnlocked
        {
            get => PlayerPrefs.GetInt(CryptUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(CryptUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool WhirlwindUnlocked
        {
            get => PlayerPrefs.GetInt(WhirlwindKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(WhirlwindKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool SpearmanUnlocked
        {
            get => PlayerPrefs.GetInt(SpearmanUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SpearmanUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool BowmanUnlocked
        {
            get => PlayerPrefs.GetInt(BowmanUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(BowmanUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool MagicianUnlocked
        {
            get => PlayerPrefs.GetInt(MagicianUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MagicianUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool SamuraiUnlocked
        {
            get => PlayerPrefs.GetInt(SamuraiUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SamuraiUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool PiercingShotUnlocked
        {
            get => PlayerPrefs.GetInt(PiercingShotKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(PiercingShotKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool FrostTipUnlocked
        {
            get => PlayerPrefs.GetInt(FrostTipKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FrostTipKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool GoldMagnetUnlocked
        {
            get => PlayerPrefs.GetInt(GoldMagnetKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(GoldMagnetKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>0 = none, 1 = −15%, 2 = −30%, 3 = −45%. Migrates old bool save (0/1).</summary>
        public static int ThickHideLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ThickHideKey, 0), 0, 3);
            set
            {
                PlayerPrefs.SetInt(ThickHideKey, Mathf.Clamp(value, 0, 3));
                PlayerPrefs.Save();
            }
        }

        public static bool ThickHideUnlocked => ThickHideLevel >= 1;

        /// <summary>0 = none, 1 = one charge/run, 2 = two charges/run.</summary>
        public static int SecondWindLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SecondWindKey, 0), 0, 2);
            set
            {
                PlayerPrefs.SetInt(SecondWindKey, Mathf.Clamp(value, 0, 2));
                PlayerPrefs.Save();
            }
        }

        public static bool SecondWindUnlocked => SecondWindLevel >= 1;

        public static int SecondWindMaxCharges =>
            SecondWindLevel >= 2 ? 2 : SecondWindLevel >= 1 ? 1 : 0;

        /// <summary>Damage taken multiplier after Thick Hide (1, 0.85, 0.70, 0.55).</summary>
        public static float ThickHideDamageTakenMultiplier =>
            ThickHideLevel <= 0 ? 1f : Mathf.Max(0.1f, 1f - 0.15f * ThickHideLevel);

        public static bool InsideSurvivalCleared
        {
            get => PlayerPrefs.GetInt(InsideClearedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(InsideClearedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool DungeonSurvivalCleared
        {
            get => PlayerPrefs.GetInt(DungeonClearedKey, 0) == 1;
            set
            {
                var wasCleared = PlayerPrefs.GetInt(DungeonClearedKey, 0) == 1;
                PlayerPrefs.SetInt(DungeonClearedKey, value ? 1 : 0);
                // First clear auto-selects the upgraded RollZy skin (player can change in Settings).
                if (value && !wasCleared)
                    PlayerPrefs.SetInt(RollZySkinKey, 1);
                PlayerPrefs.Save();
            }
        }

        /// <summary>True when the Dungeon-clear RollZy_two skin is available.</summary>
        public static bool RollZyUpgradedSkinUnlocked => DungeonSurvivalCleared;

        /// <summary>
        /// When true, RollZy uses the upgraded sheet. Only valid after Dungeon clear;
        /// defaults to upgraded on first unlock.
        /// </summary>
        public static bool UseUpgradedRollZySkin
        {
            get
            {
                if (!RollZyUpgradedSkinUnlocked) return false;
                if (!PlayerPrefs.HasKey(RollZySkinKey)) return true;
                return PlayerPrefs.GetInt(RollZySkinKey, 1) == 1;
            }
            set
            {
                if (value && !RollZyUpgradedSkinUnlocked) return;
                PlayerPrefs.SetInt(RollZySkinKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool CryptSurvivalCleared
        {
            get => PlayerPrefs.GetInt(CryptClearedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(CryptClearedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool FlameEnchantUnlocked
        {
            get => PlayerPrefs.GetInt(FlameEnchantKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FlameEnchantKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool UnlimitedMapUnlocked
        {
            get => PlayerPrefs.GetInt(UnlimitedUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(UnlimitedUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool CampfireBlessingUnlocked
        {
            get => PlayerPrefs.GetInt(CampfireBlessingKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(CampfireBlessingKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Permanent pickup radius bonus from Gold Magnet.</summary>
        public static float LootRangeMultiplier => GoldMagnetUnlocked ? 1.25f : 1f;

        /// <summary>Permanent gold pickup bonus from Gold Magnet + equipped ring/necklace.</summary>
        public static float GoldFindMultiplier =>
            (GoldMagnetUnlocked ? 1.25f : 1f) * EquipmentCatalog.CombinedGoldFindMultiplier();

        public static EquipmentId EquippedRing
        {
            get => SanitizeEquipped((EquipmentId)PlayerPrefs.GetInt(EquippedRingKey, 0), EquipmentSlot.Ring);
            set
            {
                var id = SanitizeEquipped(value, EquipmentSlot.Ring);
                PlayerPrefs.SetInt(EquippedRingKey, (int)id);
                PlayerPrefs.Save();
            }
        }

        public static EquipmentId EquippedNecklace
        {
            get => SanitizeEquipped((EquipmentId)PlayerPrefs.GetInt(EquippedNecklaceKey, 0), EquipmentSlot.Necklace);
            set
            {
                var id = SanitizeEquipped(value, EquipmentSlot.Necklace);
                PlayerPrefs.SetInt(EquippedNecklaceKey, (int)id);
                PlayerPrefs.Save();
            }
        }

        public static EquipmentId EquippedCape
        {
            get => SanitizeEquipped((EquipmentId)PlayerPrefs.GetInt(EquippedCapeKey, 0), EquipmentSlot.Cape);
            set
            {
                var id = SanitizeEquipped(value, EquipmentSlot.Cape);
                PlayerPrefs.SetInt(EquippedCapeKey, (int)id);
                PlayerPrefs.Save();
            }
        }

        public static EquipmentId EquippedHelm
        {
            get => SanitizeEquipped((EquipmentId)PlayerPrefs.GetInt(EquippedHelmKey, 0), EquipmentSlot.Helm);
            set
            {
                var id = SanitizeEquipped(value, EquipmentSlot.Helm);
                PlayerPrefs.SetInt(EquippedHelmKey, (int)id);
                PlayerPrefs.Save();
            }
        }

        public static bool OwnsEquipment(EquipmentId id)
        {
            if (id == EquipmentId.None || !EquipmentCatalog.IsValid(id)) return false;
            return (PlayerPrefs.GetInt(OwnedEquipmentKey, 0) & (1 << (int)id)) != 0;
        }

        public static bool UnlockEquipment(EquipmentId id)
        {
            if (id == EquipmentId.None || !EquipmentCatalog.IsValid(id)) return false;
            if (OwnsEquipment(id)) return false;
            var mask = PlayerPrefs.GetInt(OwnedEquipmentKey, 0) | (1 << (int)id);
            PlayerPrefs.SetInt(OwnedEquipmentKey, mask);
            PlayerPrefs.Save();
            return true;
        }

        public static void Equip(EquipmentId id)
        {
            if (id == EquipmentId.None)
                return;

            var def = EquipmentCatalog.Get(id);
            if (def.Id == EquipmentId.None || !OwnsEquipment(id)) return;

            switch (def.Slot)
            {
                case EquipmentSlot.Ring:
                    EquippedRing = id;
                    break;
                case EquipmentSlot.Necklace:
                    EquippedNecklace = id;
                    break;
                case EquipmentSlot.Cape:
                    EquippedCape = id;
                    break;
                case EquipmentSlot.Helm:
                    EquippedHelm = id;
                    break;
            }
        }

        public static void UnequipSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Ring:
                    EquippedRing = EquipmentId.None;
                    break;
                case EquipmentSlot.Necklace:
                    EquippedNecklace = EquipmentId.None;
                    break;
                case EquipmentSlot.Cape:
                    EquippedCape = EquipmentId.None;
                    break;
                case EquipmentSlot.Helm:
                    EquippedHelm = EquipmentId.None;
                    break;
            }
        }

        static EquipmentId SanitizeEquipped(EquipmentId id, EquipmentSlot slot)
        {
            if (id == EquipmentId.None) return EquipmentId.None;
            var def = EquipmentCatalog.Get(id);
            if (def.Id == EquipmentId.None || def.Slot != slot || !OwnsEquipment(id))
                return EquipmentId.None;
            return id;
        }

        /// <summary>Class loadout for the currently selected hero (edits that hero's build).</summary>
        public static PlayerClass SelectedClass
        {
            get => GetHeroClass(SelectedHero);
            set => SetHeroClass(SelectedHero, value);
        }

        /// <summary>Per-hero class loadout used by the active player and companion.</summary>
        public static PlayerClass GetHeroClass(PlayableHero hero)
        {
            EnsureHeroClassMigrated();
            var key = HeroClassKey(hero);
            var stored = (PlayerClass)PlayerPrefs.GetInt(key, (int)PlayerClass.Batter);
            return SanitizeClass(stored);
        }

        public static void SetHeroClass(PlayableHero hero, PlayerClass playerClass)
        {
            EnsureHeroClassMigrated();
            PlayerPrefs.SetInt(HeroClassKey(hero), (int)SanitizeClass(playerClass));
            PlayerPrefs.Save();
        }

        static string HeroClassKey(PlayableHero hero) =>
            hero == PlayableHero.RowZi ? ClassRowZiKey : ClassRollZyKey;

        static void EnsureHeroClassMigrated()
        {
            if (PlayerPrefs.HasKey(ClassRollZyKey) || PlayerPrefs.HasKey(ClassRowZiKey)) return;
            // One-time migrate from the old global class key into both heroes.
            var legacy = SanitizeClass((PlayerClass)PlayerPrefs.GetInt(SelectedClassKey, (int)PlayerClass.Batter));
            PlayerPrefs.SetInt(ClassRollZyKey, (int)legacy);
            PlayerPrefs.SetInt(ClassRowZiKey, (int)legacy);
            PlayerPrefs.Save();
        }

        public static PlayableHero SelectedHero
        {
            get
            {
                var stored = (PlayableHero)PlayerPrefs.GetInt(SelectedHeroKey, (int)PlayableHero.RollZy);
                return SanitizeHero(stored);
            }
            set
            {
                PlayerPrefs.SetInt(SelectedHeroKey, (int)SanitizeHero(value));
                PlayerPrefs.Save();
            }
        }

        public static bool RowZiUnlocked
        {
            get => PlayerPrefs.GetInt(RowZiUnlockedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(RowZiUnlockedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Companion hero for runs/camp décor. Always RowZi when unlocked — no hero swap.</summary>
        public static PlayableHero? GetStandbyHero()
        {
            if (!RowZiUnlocked) return null;
            return PlayableHero.RowZi;
        }

        public static string GetHeroDisplayName(PlayableHero hero)
        {
            return hero == PlayableHero.RowZi ? "RowZi" : "RollZy";
        }

        /// <summary>Player is always RollZy; RowZi is companion-only and mirrors the player loadout.</summary>
        public static PlayableHero SanitizeHero(PlayableHero hero)
        {
            return PlayableHero.RollZy;
        }

        public static MovementControlType SelectedMovementControl
        {
            get
            {
                var stored = PlayerPrefs.GetInt(MovementControlKey, (int)MovementControlType.Joystick);
                return stored == (int)MovementControlType.TapHold
                    ? MovementControlType.TapHold
                    : MovementControlType.Joystick;
            }
            set
            {
                PlayerPrefs.SetInt(MovementControlKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool UsesJoystickMovement => SelectedMovementControl == MovementControlType.Joystick;
        public static bool UsesTapHoldMovement => SelectedMovementControl == MovementControlType.TapHold;

        /// <summary>
        /// Saved joystick anchored position (bottom-right anchor space). Missing keys mean default placement.
        /// </summary>
        public static bool HasCustomJoystickPosition =>
            PlayerPrefs.HasKey(JoystickPosXKey) && PlayerPrefs.HasKey(JoystickPosYKey);

        public static Vector2 JoystickAnchoredPosition
        {
            get => new(
                PlayerPrefs.GetFloat(JoystickPosXKey, 0f),
                PlayerPrefs.GetFloat(JoystickPosYKey, 0f));
            set
            {
                PlayerPrefs.SetFloat(JoystickPosXKey, value.x);
                PlayerPrefs.SetFloat(JoystickPosYKey, value.y);
                PlayerPrefs.Save();
            }
        }

        /// <summary>0–1 master BGM volume (camp settings).</summary>
        public static float BgmVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 0.7f));
            set
            {
                PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>0–1 master SFX volume (camp settings).</summary>
        public static float SfxVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f));
            set
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Survival BGM pack folder name under Resources/Music. Default DnB.</summary>
        public static string BgmGenre
        {
            get
            {
                var raw = PlayerPrefs.GetString(BgmGenreKey, BgmGenreDnB);
                if (string.Equals(raw, BgmGenreMetal, System.StringComparison.OrdinalIgnoreCase))
                    return BgmGenreMetal;
                return BgmGenreDnB;
            }
            set
            {
                var next = string.Equals(value, BgmGenreMetal, System.StringComparison.OrdinalIgnoreCase)
                    ? BgmGenreMetal
                    : BgmGenreDnB;
                PlayerPrefs.SetString(BgmGenreKey, next);
                PlayerPrefs.Save();
            }
        }

        public static PlayerClass SanitizeClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !SpearmanUnlocked) return PlayerClass.Batter;
            if (playerClass == PlayerClass.Bowman && !BowmanUnlocked) return PlayerClass.Batter;
            if (playerClass == PlayerClass.Magician && !MagicianUnlocked) return PlayerClass.Batter;
            if (playerClass == PlayerClass.Samurai && !SamuraiUnlocked) return PlayerClass.Batter;
            return playerClass;
        }

        public static AttackMode GetSelectedAttackMode(PlayerClass playerClass)
        {
            return SanitizeAttackMode(playerClass, (AttackMode)PlayerPrefs.GetInt(GetAttackModeKey(playerClass), (int)AttackMode.Standard));
        }

        public static void SetSelectedAttackMode(PlayerClass playerClass, AttackMode mode)
        {
            PlayerPrefs.SetInt(GetAttackModeKey(playerClass), (int)SanitizeAttackMode(playerClass, mode));
            PlayerPrefs.Save();
        }

        public static AttackMode SanitizeAttackMode(PlayerClass playerClass, AttackMode mode)
        {
            if (!AttackModeCatalog.IsAvailableForClass(playerClass, mode)) return AttackMode.Standard;
            if (!AttackModeCatalog.IsUnlocked(mode)) return AttackMode.Standard;
            return mode;
        }

        static string GetAttackModeKey(PlayerClass playerClass)
        {
            return playerClass switch
            {
                PlayerClass.Spearman => AttackModeSpearmanKey,
                PlayerClass.Bowman => AttackModeBowmanKey,
                PlayerClass.Magician => AttackModeMagicianKey,
                PlayerClass.Samurai => AttackModeSamuraiKey,
                _ => AttackModeBatterKey
            };
        }

        public static int MaxHp => Mathf.Min(StatCaps.PermanentMaxHp, 100 + HpUpgradeLevel * 15);
        public static float DamageMultiplier => Mathf.Min(StatCaps.PermanentMaxDamageMultiplier, 1f + DamageUpgradeLevel * 0.08f);
        public static float SpeedMultiplier => Mathf.Min(StatCaps.PermanentMaxSpeedMultiplier, 1f + SpeedUpgradeLevel * 0.06f);
        /// <summary>Permanent shop attack range: +5% per upgrade level.</summary>
        public static float AttackRangeMultiplier => Mathf.Min(StatCaps.PermanentMaxAttackRangeMultiplier, 1f + RangeUpgradeLevel * 0.05f);

        public static bool IsHpUpgradeMaxed => MaxHp >= StatCaps.PermanentMaxHp;
        public static bool IsDamageUpgradeMaxed => DamageMultiplier >= StatCaps.PermanentMaxDamageMultiplier - 0.001f;
        public static bool IsSpeedUpgradeMaxed => SpeedMultiplier >= StatCaps.PermanentMaxSpeedMultiplier - 0.001f;
        public static bool IsRangeUpgradeMaxed => AttackRangeMultiplier >= StatCaps.PermanentMaxAttackRangeMultiplier - 0.001f;

        public static int LifetimeZombieKills
        {
            get => PlayerPrefs.GetInt(ZombieKillsKey, 0);
            set { PlayerPrefs.SetInt(ZombieKillsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int LifetimeBossKills
        {
            get => PlayerPrefs.GetInt(BossKillsKey, 0);
            set { PlayerPrefs.SetInt(BossKillsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int LifetimeDeaths
        {
            get => PlayerPrefs.GetInt(DeathsKey, 0);
            set { PlayerPrefs.SetInt(DeathsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int LifetimeGoldEarned
        {
            get => PlayerPrefs.GetInt(GoldEarnedKey, 0);
            set { PlayerPrefs.SetInt(GoldEarnedKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int HighestRoundReached
        {
            get => PlayerPrefs.GetInt(HighestRoundKey, 0);
            set { PlayerPrefs.SetInt(HighestRoundKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Best Unlimited Survival round reached (drives Steel+ weapon unlocks).</summary>
        public static int UnlimitedHighestRoundReached
        {
            get => PlayerPrefs.GetInt(UnlimitedHighestRoundKey, 0);
            set { PlayerPrefs.SetInt(UnlimitedHighestRoundKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Best Dungeon Survival round reached (Iron weapons at R30).</summary>
        public static int DungeonHighestRoundReached
        {
            get => PlayerPrefs.GetInt(DungeonHighestRoundKey, 0);
            set { PlayerPrefs.SetInt(DungeonHighestRoundKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Best Crypt Survival round reached.</summary>
        public static int CryptHighestRoundReached
        {
            get => PlayerPrefs.GetInt(CryptHighestRoundKey, 0);
            set { PlayerPrefs.SetInt(CryptHighestRoundKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static void RecordEnemyKill(bool isBoss)
        {
            if (isBoss)
                LifetimeBossKills++;
            else
                LifetimeZombieKills++;

            Achievements.EvaluateKillAchievements();
        }

        /// <summary>
        /// Lifetime kill + per-weapon kill for material unlocks. Returns gold-unlock banner if any.
        /// </summary>
        public static string RecordEnemyKillForWeapon(PlayerClass playerClass, bool isBoss)
        {
            RecordEnemyKill(isBoss);
            var prev = AddWeaponKill(playerClass);
            var now = prev + 1;
            var banner = WeaponCatalog.TryNotifyGoldUnlock(playerClass, prev, now);
            Achievements.EvaluateWeaponTierAchievements();
            return banner;
        }

        public static void RecordDeath() => LifetimeDeaths++;

        public static void RecordHighestRound(int round)
        {
            if (round > HighestRoundReached)
                HighestRoundReached = round;

            Achievements.EvaluateRoundAchievements(round);
        }

        /// <summary>
        /// Records Unlimited Survival depth. Returns true if a new personal best was set.
        /// </summary>
        public static bool RecordUnlimitedRound(int round)
        {
            if (round <= UnlimitedHighestRoundReached) return false;
            UnlimitedHighestRoundReached = round;
            return true;
        }

        /// <summary>
        /// Records Dungeon Survival depth. Returns true if a new personal best was set.
        /// </summary>
        public static bool RecordDungeonRound(int round)
        {
            if (round <= DungeonHighestRoundReached) return false;
            DungeonHighestRoundReached = round;
            return true;
        }

        /// <summary>
        /// Records Crypt Survival depth. Returns true if a new personal best was set.
        /// </summary>
        public static bool RecordCryptRound(int round)
        {
            if (round <= CryptHighestRoundReached) return false;
            CryptHighestRoundReached = round;
            return true;
        }

        public static void BankFromRun(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            LifetimeGoldEarned += amount;
            LastRunGoldBanked = amount;
        }

        /// <summary>Consume camp-return toast data after it is shown once.</summary>
        public static void ClearLastRunToast()
        {
            LastRunGoldBanked = 0;
            LastRunRound = 0;
            LastRunKills = 0;
            LastRunWasDeath = false;
        }

        public static bool TrySpendGold(int cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            return true;
        }

        /// <summary>Grand Wizard's Peril — accepted from the camp quest wizard.</summary>
        public static bool QuestGrandWizardsPerilAccepted
        {
            get => PlayerPrefs.GetInt(QuestGwpAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestGwpAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Grand Wizard's Peril — turned in for gold.</summary>
        public static bool QuestGrandWizardsPerilCompleted
        {
            get => PlayerPrefs.GetInt(QuestGwpCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestGwpCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Quest item recovered from Emberwilds R10 boss (not equippable gear).</summary>
        public static bool HasTwinLightningPendant
        {
            get => PlayerPrefs.GetInt(TwinLightningPendantKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TwinLightningPendantKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>The Warded Path — accepted from Archmage Thalor after the pendant.</summary>
        public static bool QuestWardensPathAccepted
        {
            get => PlayerPrefs.GetInt(QuestWardensPathAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestWardensPathAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Emberwilds R20 boss defeated (door / Warded Halls path opened).</summary>
        public static bool QuestWardensPathBossDefeated
        {
            get => PlayerPrefs.GetInt(QuestWardensPathBossKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestWardensPathBossKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestWardensPathCompleted
        {
            get => PlayerPrefs.GetInt(QuestWardensPathCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestWardensPathCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Lyra's Vigil — accepted from Sister Lyra after Silent Ossuary unlocks.</summary>
        public static bool QuestLyraVigilAccepted
        {
            get => PlayerPrefs.GetInt(QuestLyraAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestLyraAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestLyraVigilBossDefeated
        {
            get => PlayerPrefs.GetInt(QuestLyraBossKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestLyraBossKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestLyraVigilCompleted
        {
            get => PlayerPrefs.GetInt(QuestLyraCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestLyraCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Bren's Watch — accepted from Captain Bren after Endless Front unlocks.</summary>
        public static bool QuestBrensWatchAccepted
        {
            get => PlayerPrefs.GetInt(QuestBrenAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestBrenAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestBrensWatchMilestone
        {
            get => PlayerPrefs.GetInt(QuestBrenMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestBrenMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestBrensWatchCompleted
        {
            get => PlayerPrefs.GetInt(QuestBrenCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestBrenCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Corvin's Omen — Endless Front R75 after Bren's Watch.</summary>
        public static bool QuestCorvinsOmenAccepted
        {
            get => PlayerPrefs.GetInt(QuestCorvinOmenAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCorvinOmenAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestCorvinsOmenMilestone
        {
            get => PlayerPrefs.GetInt(QuestCorvinOmenMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCorvinOmenMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestCorvinsOmenCompleted
        {
            get => PlayerPrefs.GetInt(QuestCorvinOmenCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCorvinOmenCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Kael's Recon — Emberwilds R15 side quest.</summary>
        public static bool QuestKaelsReconAccepted
        {
            get => PlayerPrefs.GetInt(QuestKaelAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestKaelAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestKaelsReconMilestone
        {
            get => PlayerPrefs.GetInt(QuestKaelMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestKaelMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestKaelsReconCompleted
        {
            get => PlayerPrefs.GetInt(QuestKaelCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestKaelCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Nessa's Salve — Warded Halls R15 side quest.</summary>
        public static bool QuestNessasSalveAccepted
        {
            get => PlayerPrefs.GetInt(QuestNessaAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestNessaAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestNessasSalveMilestone
        {
            get => PlayerPrefs.GetInt(QuestNessaMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestNessaMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestNessasSalveCompleted
        {
            get => PlayerPrefs.GetInt(QuestNessaCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestNessaCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Garrick's Anvil — Ironvault R20 side quest.</summary>
        public static bool QuestGarricksAnvilAccepted
        {
            get => PlayerPrefs.GetInt(QuestGarrickAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestGarrickAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestGarricksAnvilMilestone
        {
            get => PlayerPrefs.GetInt(QuestGarrickMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestGarrickMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestGarricksAnvilCompleted
        {
            get => PlayerPrefs.GetInt(QuestGarrickCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestGarrickCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Tove's Chart — Silent Ossuary R25 side quest.</summary>
        public static bool QuestTovesChartAccepted
        {
            get => PlayerPrefs.GetInt(QuestToveAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestToveAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestTovesChartMilestone
        {
            get => PlayerPrefs.GetInt(QuestToveMilestoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestToveMilestoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestTovesChartCompleted
        {
            get => PlayerPrefs.GetInt(QuestToveCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestToveCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Grey Wizard's Crow — accepted from the Grand Wizard.</summary>
        public static bool QuestGreyWizardAccepted
        {
            get => PlayerPrefs.GetInt(QuestCrowAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCrowAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Crow freed on Inside Survival; Grey Wizard returns to camp.</summary>
        public static bool QuestGreyWizardRescued
        {
            get => PlayerPrefs.GetInt(QuestCrowRescuedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCrowRescuedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestGreyWizardCompleted
        {
            get => PlayerPrefs.GetInt(QuestCrowCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestCrowCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Dungeon Survival knight used the door home; appears at camp for his quest.</summary>
        public static bool DungeonKnightReturnedToCamp
        {
            get => PlayerPrefs.GetInt(DungeonKnightReturnedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DungeonKnightReturnedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestKnightsBestFriendAccepted
        {
            get => PlayerPrefs.GetInt(QuestKnightAcceptedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestKnightAcceptedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool QuestKnightsBestFriendCompleted
        {
            get => PlayerPrefs.GetInt(QuestKnightCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(QuestKnightCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Quest item recovered from Dungeon Survival R40 boss (not equippable gear).</summary>
        public static bool HasKnightsGreatsword
        {
            get => PlayerPrefs.GetInt(KnightsGreatswordKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KnightsGreatswordKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Equipped material for a class weapon. Defaults to the highest unlocked tier.
        /// Always clamped so saved values cannot exceed current unlocks.
        /// </summary>
        public static WeaponMaterialTier GetEquippedWeaponTier(PlayerClass playerClass)
        {
            EnsureWeaponProgressMigrated();
            var max = WeaponCatalog.GetUnlockedTier(playerClass);
            if (!PlayerPrefs.HasKey(WeaponEquippedKey(playerClass)))
                return max;

            var stored = (WeaponMaterialTier)PlayerPrefs.GetInt(
                WeaponEquippedKey(playerClass), (int)WeaponMaterialTier.Wooden);
            if (!WeaponCatalog.IsTierUnlocked(playerClass, stored))
                return max;
            return stored;
        }

        public static void SetEquippedWeaponTier(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            if (!WeaponCatalog.IsTierUnlocked(playerClass, tier)) return;
            PlayerPrefs.SetInt(WeaponEquippedKey(playerClass), (int)tier);
            PlayerPrefs.Save();
        }
    }
}