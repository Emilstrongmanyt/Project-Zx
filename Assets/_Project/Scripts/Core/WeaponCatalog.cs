namespace ProjectZx.Core
{
    /// <summary>
    /// Weapon material tiers. Wooden is the base Admurin set; Iron / Steel unlock
    /// via Unlimited Survival progression. Higher tiers swap sprites and grant perks.
    /// Future materials (Gold, Cobalt, …) can extend this enum.
    /// </summary>
    public enum WeaponMaterialTier
    {
        Wooden = 0,
        Iron = 1,
        Steel = 2
    }

    /// <summary>
    /// Permanent weapon upgrades unlocked by Unlimited Survival depth.
    /// Equipped tier is always the highest unlocked (auto-equip for v1).
    /// </summary>
    public static class WeaponCatalog
    {
        /// <summary>Reach this Unlimited round to unlock Iron weapons for all classes.</summary>
        public const int IronUnlockUnlimitedRound = 20;
        /// <summary>Reach this Unlimited round to unlock Steel weapons for all classes.</summary>
        public const int SteelUnlockUnlimitedRound = 50;

        public static WeaponMaterialTier GetUnlockedTier()
        {
            var best = GameSave.UnlimitedHighestRoundReached;
            if (best >= SteelUnlockUnlimitedRound) return WeaponMaterialTier.Steel;
            if (best >= IronUnlockUnlimitedRound) return WeaponMaterialTier.Iron;
            return WeaponMaterialTier.Wooden;
        }

        public static string GetTierName(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => "Steel",
            WeaponMaterialTier.Iron => "Iron",
            _ => "Wooden"
        };

        public static string GetPerkSummary(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => "+25% damage, +10% attack speed, +8% range",
            WeaponMaterialTier.Iron => "+12% damage, +5% attack speed",
            _ => "Base weapons"
        };

        public static float DamageMultiplier(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => 1.25f,
            WeaponMaterialTier.Iron => 1.12f,
            _ => 1f
        };

        public static float AttackSpeedMultiplier(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => 1.10f,
            WeaponMaterialTier.Iron => 1.05f,
            _ => 1f
        };

        public static float AttackRangeMultiplier(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => 1.08f,
            _ => 1f
        };

        public static float DamageMultiplier() => DamageMultiplier(GetUnlockedTier());
        public static float AttackSpeedMultiplier() => AttackSpeedMultiplier(GetUnlockedTier());
        public static float AttackRangeMultiplier() => AttackRangeMultiplier(GetUnlockedTier());

        /// <summary>
        /// Resources path under Items/Admurin for the held weapon at the given tier.
        /// Null resource means fall back to the class default (wooden / iron katana).
        /// </summary>
        public static string GetResourceName(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            // Samurai starts on Iron_Weapon22 (no wooden match); steel is the first visual upgrade.
            if (playerClass == PlayerClass.Samurai)
            {
                return tier >= WeaponMaterialTier.Steel
                    ? "Items/Admurin/weapon_katana_steel"
                    : "Items/Admurin/weapon_katana";
            }

            var baseName = playerClass switch
            {
                PlayerClass.Batter => "weapon_bat",
                PlayerClass.Spearman => "weapon_spear",
                PlayerClass.Bowman => "weapon_bow",
                PlayerClass.Magician => "weapon_staff",
                _ => "weapon_bat"
            };

            return tier switch
            {
                WeaponMaterialTier.Steel => $"Items/Admurin/{baseName}_steel",
                WeaponMaterialTier.Iron => $"Items/Admurin/{baseName}_iron",
                _ => $"Items/Admurin/{baseName}"
            };
        }

        public static string GetResourceName(PlayerClass playerClass) =>
            GetResourceName(playerClass, GetUnlockedTier());

        /// <summary>
        /// Returns a banner string if progressing from previousBest to newBest unlocks a material tier.
        /// Prefer the highest newly unlocked tier when both thresholds are crossed at once.
        /// </summary>
        public static string TryNotifyTierUnlock(int previousBest, int newBest)
        {
            if (previousBest < SteelUnlockUnlimitedRound && newBest >= SteelUnlockUnlimitedRound)
                return "Steel weapons unlocked! (+25% damage, +10% attack speed, +8% range)";
            if (previousBest < IronUnlockUnlimitedRound && newBest >= IronUnlockUnlimitedRound)
                return "Iron weapons unlocked! (+12% damage, +5% attack speed)";
            return null;
        }
    }
}
