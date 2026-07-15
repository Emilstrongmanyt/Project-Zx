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

        public static int MaxHp => 100 + HpUpgradeLevel * 15;
        public static float DamageMultiplier => 1f + DamageUpgradeLevel * 0.08f;
        public static float SpeedMultiplier => 1f + SpeedUpgradeLevel * 0.06f;

        public static void BankFromRun(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
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