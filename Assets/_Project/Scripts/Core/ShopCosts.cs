using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Permanent shop prices. Repeatable upgrades scale exponentially with owned level.
    /// </summary>
    public static class ShopCosts
    {
        // HP / Damage base costs reduced 40% for smoother early progression.
        public const int HpUpgrade = 60;
        public const int DamageUpgrade = 90;
        public const int SpeedUpgrade = 120;
        public const int RangeUpgrade = 110;
        public const int Whirlwind = 1000;
        public const int PiercingShot = 4000;
        public const int FrostTip = 3000;
        public const int GoldMagnet = 400;
        public const int ThickHide = 360;
        public const int SecondWind = 800;
        public const int CampfireBlessing = 600;

        /// <summary>
        /// Exponential cost for the next purchase of a level-based upgrade.
        /// Level 0 → base, level 1 → base×2, level 2 → base×4, …
        /// </summary>
        public static int Exponential(int baseCost, int ownedLevel)
        {
            var level = Mathf.Max(0, ownedLevel);
            // Cap exponent so int does not overflow absurd mid-game prices.
            var exp = Mathf.Min(level, 20);
            var mult = 1 << exp;
            return Mathf.Max(1, baseCost * mult);
        }

        public static int NextHpCost => Exponential(HpUpgrade, GameSave.HpUpgradeLevel);
        public static int NextDamageCost => Exponential(DamageUpgrade, GameSave.DamageUpgradeLevel);
        public static int NextSpeedCost => Exponential(SpeedUpgrade, GameSave.SpeedUpgradeLevel);
        public static int NextRangeCost => Exponential(RangeUpgrade, GameSave.RangeUpgradeLevel);

        /// <summary>Cost to buy the next Thick Hide tier (ownedLevel is current level 0–2).</summary>
        public static int NextThickHideCost => Exponential(ThickHide, GameSave.ThickHideLevel);

        /// <summary>Cost to buy the next Second Wind tier (ownedLevel is current level 0–1).</summary>
        public static int NextSecondWindCost => Exponential(SecondWind, GameSave.SecondWindLevel);
    }
}
