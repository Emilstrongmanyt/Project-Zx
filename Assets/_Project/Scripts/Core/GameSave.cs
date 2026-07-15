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
        const string WhirlwindKey = "zx_whirlwind";
        const string SpearmanUnlockedKey = "zx_spearman_unlocked";
        const string SelectedClassKey = "zx_selected_class";
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

        public static PlayerClass SelectedClass
        {
            get
            {
                var stored = (PlayerClass)PlayerPrefs.GetInt(SelectedClassKey, (int)PlayerClass.Batter);
                if (stored == PlayerClass.Spearman && !SpearmanUnlocked)
                    return PlayerClass.Batter;
                return stored;
            }
            set
            {
                if (value == PlayerClass.Spearman && !SpearmanUnlocked)
                    value = PlayerClass.Batter;
                PlayerPrefs.SetInt(SelectedClassKey, (int)value);
                PlayerPrefs.Save();
            }
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
        }

        public static void RecordDeath() => LifetimeDeaths++;

        public static void RecordHighestRound(int round)
        {
            if (round > HighestRoundReached)
                HighestRoundReached = round;
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