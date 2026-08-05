using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Permanent shop prices. Repeatable upgrades scale linearly with owned level.
    /// </summary>
    public static class ShopCosts
    {
        // HP / Damage base costs reduced 40% for smoother early progression.
        public const int HpUpgrade = 60;
        public const int DamageUpgrade = 90;
        public const int SpeedUpgrade = 120;
        public const int RangeUpgrade = 110;
        public const int Whirlwind = 250;
        public const int PiercingShot = 4000;
        public const int FrostTip = 3000;
        public const int GoldMagnet = 400;
        public const int ThickHide = 360;
        public const int SecondWind = 800;
        public const int CampfireBlessing = 600;

        /// <summary>
        /// Linear cost for the next purchase of a level-based upgrade.
        /// Level 0 → base×1, level 1 → base×2, level 2 → base×3, …
        /// </summary>
        public static int Linear(int baseCost, int ownedLevel)
        {
            var tier = Mathf.Max(0, ownedLevel) + 1;
            return Mathf.Max(1, baseCost * tier);
        }

        public static int NextHpCost => Linear(HpUpgrade, GameSave.HpUpgradeLevel);
        public static int NextDamageCost => Linear(DamageUpgrade, GameSave.DamageUpgradeLevel);
        public static int NextSpeedCost => Linear(SpeedUpgrade, GameSave.SpeedUpgradeLevel);
        public static int NextRangeCost => Linear(RangeUpgrade, GameSave.RangeUpgradeLevel);

        /// <summary>Cost to buy the next Thick Hide tier (ownedLevel is current level 0–2).</summary>
        public static int NextThickHideCost => Linear(ThickHide, GameSave.ThickHideLevel);

        /// <summary>Cost to buy the next Second Wind tier (ownedLevel is current level 0–1).</summary>
        public static int NextSecondWindCost => Linear(SecondWind, GameSave.SecondWindLevel);
    }
}
