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
        const string InsideUnlockedKey = "zx_inside_unlocked";
        const string DungeonUnlockedKey = "zx_dungeon_unlocked";
        const string WhirlwindKey = "zx_whirlwind";
        const string PiercingShotKey = "zx_piercing_shot";
        const string FrostTipKey = "zx_frost_tip";
        const string GoldMagnetKey = "zx_gold_magnet";
        const string ThickHideKey = "zx_thick_hide";
        const string SecondWindKey = "zx_second_wind";
        const string CampfireBlessingKey = "zx_campfire_blessing";
        const string SpearmanUnlockedKey = "zx_spearman_unlocked";
        const string BowmanUnlockedKey = "zx_bowman_unlocked";
        const string MagicianUnlockedKey = "zx_magician_unlocked";
        const string SelectedClassKey = "zx_selected_class";
        const string SelectedHeroKey = "zx_selected_hero";
        const string MovementControlKey = "zx_movement_control";
        const string RowZiUnlockedKey = "zx_rowzi_unlocked";
        const string AttackModeBatterKey = "zx_attack_batter";
        const string AttackModeSpearmanKey = "zx_attack_spearman";
        const string AttackModeBowmanKey = "zx_attack_bowman";
        const string AttackModeMagicianKey = "zx_attack_magician";
        const string ZombieKillsKey = "zx_lifetime_zombie_kills";
        const string BossKillsKey = "zx_lifetime_boss_kills";
        const string DeathsKey = "zx_lifetime_deaths";
        const string GoldEarnedKey = "zx_lifetime_gold_earned";
        const string HighestRoundKey = "zx_highest_round";

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

        public static bool ThickHideUnlocked
        {
            get => PlayerPrefs.GetInt(ThickHideKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ThickHideKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool SecondWindUnlocked
        {
            get => PlayerPrefs.GetInt(SecondWindKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SecondWindKey, value ? 1 : 0);
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

        /// <summary>Permanent gold pickup bonus from Gold Magnet.</summary>
        public static float GoldFindMultiplier => GoldMagnetUnlocked ? 1.25f : 1f;

        public static PlayerClass SelectedClass
        {
            get
            {
                var stored = (PlayerClass)PlayerPrefs.GetInt(SelectedClassKey, (int)PlayerClass.Batter);
                return SanitizeClass(stored);
            }
            set
            {
                PlayerPrefs.SetInt(SelectedClassKey, (int)SanitizeClass(value));
                PlayerPrefs.Save();
            }
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

        public static PlayerClass SanitizeClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !SpearmanUnlocked) return PlayerClass.Batter;
            if (playerClass == PlayerClass.Bowman && !BowmanUnlocked) return PlayerClass.Batter;
            if (playerClass == PlayerClass.Magician && !MagicianUnlocked) return PlayerClass.Batter;
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
                _ => AttackModeBatterKey
            };
        }

        public static int MaxHp => Mathf.Min(StatCaps.PermanentMaxHp, 100 + HpUpgradeLevel * 15);
        public static float DamageMultiplier => Mathf.Min(StatCaps.PermanentMaxDamageMultiplier, 1f + DamageUpgradeLevel * 0.08f);
        public static float SpeedMultiplier => Mathf.Min(StatCaps.PermanentMaxSpeedMultiplier, 1f + SpeedUpgradeLevel * 0.06f);

        public static bool IsHpUpgradeMaxed => MaxHp >= StatCaps.PermanentMaxHp;
        public static bool IsDamageUpgradeMaxed => DamageMultiplier >= StatCaps.PermanentMaxDamageMultiplier - 0.001f;
        public static bool IsSpeedUpgradeMaxed => SpeedMultiplier >= StatCaps.PermanentMaxSpeedMultiplier - 0.001f;

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