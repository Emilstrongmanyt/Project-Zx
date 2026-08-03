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
        const string WhirlwindKey = "zx_whirlwind";
        const string PiercingShotKey = "zx_piercing_shot";
        const string FrostTipKey = "zx_frost_tip";
        const string GoldMagnetKey = "zx_gold_magnet";
        const string ThickHideKey = "zx_thick_hide";
        const string SecondWindKey = "zx_second_wind";
        const string CampfireBlessingKey = "zx_campfire_blessing";
        const string InsideClearedKey = "zx_inside_cleared";
        const string DungeonClearedKey = "zx_dungeon_cleared";
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
        const string OwnedEquipmentKey = "zx_owned_equipment";
        const string EquippedRingKey = "zx_equipped_ring";
        const string EquippedNecklaceKey = "zx_equipped_necklace";

        public static int LastRunGoldBanked { get; set; }

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
                PlayerPrefs.SetInt(DungeonClearedKey, value ? 1 : 0);
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

            if (def.Slot == EquipmentSlot.Ring)
                EquippedRing = id;
            else
                EquippedNecklace = id;
        }

        public static void UnequipSlot(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.Ring)
                EquippedRing = EquipmentId.None;
            else
                EquippedNecklace = EquipmentId.None;
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

        public static PlayableHero? GetStandbyHero()
        {
            if (!RowZiUnlocked) return null;
            return SelectedHero == PlayableHero.RollZy ? PlayableHero.RowZi : PlayableHero.RollZy;
        }

        public static string GetHeroDisplayName(PlayableHero hero)
        {
            return hero == PlayableHero.RowZi ? "RowZi" : "RollZy";
        }

        public static PlayableHero SanitizeHero(PlayableHero hero)
        {
            if (hero == PlayableHero.RowZi && !RowZiUnlocked) return PlayableHero.RollZy;
            return hero;
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

        /// <summary>Best Unlimited Survival round reached (drives weapon material unlocks).</summary>
        public static int UnlimitedHighestRoundReached
        {
            get => PlayerPrefs.GetInt(UnlimitedHighestRoundKey, 0);
            set { PlayerPrefs.SetInt(UnlimitedHighestRoundKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static void RecordEnemyKill(bool isBoss)
        {
            if (isBoss)
                LifetimeBossKills++;
            else
                LifetimeZombieKills++;

            Achievements.EvaluateKillAchievements();
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

        public static void BankFromRun(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            LifetimeGoldEarned += amount;
            LastRunGoldBanked = amount;
        }

        public static bool TrySpendGold(int cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            return true;
        }
    }
}