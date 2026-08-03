namespace ProjectZx.Core
{
    /// <summary>
    /// Weapon material tiers (Admurin armory sets). Higher tiers swap sprites and grant
    /// damage + attack-speed perks. Equipped tier is always the highest unlocked.
    /// </summary>
    public enum WeaponMaterialTier
    {
        Wooden = 0,
        Iron = 1,
        Steel = 2,
        Copper = 3,
        Silver = 4,
        Gold = 5,
        Cobalt = 6,
        Platinum = 7,
        Adamantine = 8,
        Crimson = 9,
        /// <summary>Unlimited R100 capstone — AOE splash. Highest equipped tier.</summary>
        Fateful = 10,
        /// <summary>Shipped art set; reserved for a future intermediate unlock.</summary>
        Altair = 11,
        /// <summary>Shipped art set; reserved for a future intermediate unlock.</summary>
        Angelic = 12
    }

    /// <summary>
    /// Permanent weapon upgrades from Dungeon + Unlimited Survival depth.
    /// No attack-range bonus per tier — damage scales hard, attack speed lightly.
    /// Fateful (Unlimited R100) adds AOE splash on hit.
    /// </summary>
    public static class WeaponCatalog
    {
        public const int IronUnlockDungeonRound = 30;
        public const int SteelUnlockUnlimitedRound = 20;

        public const float AoeSplashRadius = 1.85f;
        public const float AoeSplashDamageFraction = 0.45f;

        /// <summary>+20% damage per tier step above Wooden (additive on base).</summary>
        public const float DamageBonusPerTier = 0.20f;
        /// <summary>+2.5% attack speed per tier step above Wooden.</summary>
        public const float AttackSpeedBonusPerTier = 0.025f;

        /// <summary>
        /// Full progression using every Admurin material folder.
        /// Iron = Dungeon R30; remaining = Unlimited R20–R100 (every 10 rounds).
        /// R90–R100 span Crimson → Altair → Angelic → Fateful so all sets are used:
        /// R90 Crimson, R100 steps the three legendaries with Fateful as the equipped capstone.
        /// </summary>
        public static WeaponMaterialTier GetUnlockedTier()
        {
            var tier = WeaponMaterialTier.Wooden;

            if (IsIronUnlocked())
                tier = WeaponMaterialTier.Iron;

            var u = GameSave.UnlimitedHighestRoundReached;
            if (u >= 20) tier = WeaponMaterialTier.Steel;
            if (u >= 30) tier = WeaponMaterialTier.Copper;
            if (u >= 40) tier = WeaponMaterialTier.Silver;
            if (u >= 50) tier = WeaponMaterialTier.Gold;
            if (u >= 60) tier = WeaponMaterialTier.Cobalt;
            if (u >= 70) tier = WeaponMaterialTier.Platinum;
            if (u >= 80) tier = WeaponMaterialTier.Adamantine;
            if (u >= 90) tier = WeaponMaterialTier.Crimson;
            // R100 capstone. Altair/Angelic art is in Resources for a future mid-tier unlock.
            if (u >= 100) tier = WeaponMaterialTier.Fateful;

            return tier;
        }

        public static bool IsIronUnlocked() =>
            GameSave.DungeonHighestRoundReached >= IronUnlockDungeonRound
            || GameSave.DungeonSurvivalCleared;

        public static bool HasAoeSplash() => GetUnlockedTier() == WeaponMaterialTier.Fateful;

        /// <summary>Power rank for damage/AS (Altair/Angelic reserved sets do not inflate equipped power).</summary>
        public static int TierIndex(WeaponMaterialTier tier)
        {
            if (tier == WeaponMaterialTier.Altair || tier == WeaponMaterialTier.Angelic)
                return (int)WeaponMaterialTier.Fateful;
            return (int)tier;
        }

        public static string GetTierName(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Fateful => "Fateful",
            WeaponMaterialTier.Angelic => "Angelic",
            WeaponMaterialTier.Altair => "Altair",
            WeaponMaterialTier.Crimson => "Crimson",
            WeaponMaterialTier.Adamantine => "Adamantine",
            WeaponMaterialTier.Platinum => "Platinum",
            WeaponMaterialTier.Cobalt => "Cobalt",
            WeaponMaterialTier.Gold => "Gold",
            WeaponMaterialTier.Silver => "Silver",
            WeaponMaterialTier.Copper => "Copper",
            WeaponMaterialTier.Steel => "Steel",
            WeaponMaterialTier.Iron => "Iron",
            _ => "Wooden"
        };

        public static string GetPerkSummary(WeaponMaterialTier tier)
        {
            if (tier == WeaponMaterialTier.Wooden) return "Base weapons";
            var dmg = RoundPct(DamageMultiplier(tier) - 1f);
            var aspd = RoundPct(AttackSpeedMultiplier(tier) - 1f);
            var aoe = tier >= WeaponMaterialTier.Fateful ? ", AOE splash" : "";
            return $"+{dmg}% damage, +{aspd}% attack speed{aoe}";
        }

        static int RoundPct(float fraction) =>
            (int)System.Math.Round(fraction * 100f);

        public static float DamageMultiplier(WeaponMaterialTier tier) =>
            1f + TierIndex(tier) * DamageBonusPerTier;

        public static float AttackSpeedMultiplier(WeaponMaterialTier tier) =>
            1f + TierIndex(tier) * AttackSpeedBonusPerTier;

        /// <summary>Weapon materials do not modify attack range.</summary>
        public static float AttackRangeMultiplier(WeaponMaterialTier tier) => 1f;

        public static float DamageMultiplier() => DamageMultiplier(GetUnlockedTier());
        public static float AttackSpeedMultiplier() => AttackSpeedMultiplier(GetUnlockedTier());
        public static float AttackRangeMultiplier() => 1f;

        static string MaterialSuffix(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Iron => "iron",
            WeaponMaterialTier.Steel => "steel",
            WeaponMaterialTier.Copper => "copper",
            WeaponMaterialTier.Silver => "silver",
            WeaponMaterialTier.Gold => "gold",
            WeaponMaterialTier.Cobalt => "cobalt",
            WeaponMaterialTier.Platinum => "platinum",
            WeaponMaterialTier.Adamantine => "adamantine",
            WeaponMaterialTier.Crimson => "crimson",
            WeaponMaterialTier.Altair => "altair",
            WeaponMaterialTier.Angelic => "angelic",
            WeaponMaterialTier.Fateful => "fateful",
            _ => null
        };

        public static string GetResourceName(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            var baseName = playerClass switch
            {
                PlayerClass.Batter => "weapon_bat",
                PlayerClass.Spearman => "weapon_spear",
                PlayerClass.Bowman => "weapon_bow",
                PlayerClass.Magician => "weapon_staff",
                PlayerClass.Samurai => "weapon_katana",
                _ => "weapon_bat"
            };

            // Samurai has no wooden set — Iron_Weapon22 is the base art through Iron tier.
            if (playerClass == PlayerClass.Samurai && tier <= WeaponMaterialTier.Iron)
                return "Items/Admurin/weapon_katana";

            var suffix = MaterialSuffix(tier);
            if (string.IsNullOrEmpty(suffix))
                return $"Items/Admurin/{baseName}";

            return $"Items/Admurin/{baseName}_{suffix}";
        }

        public static string GetResourceName(PlayerClass playerClass) =>
            GetResourceName(playerClass, GetUnlockedTier());

        public static string TryNotifyUnlimitedTierUnlock(int previousBest, int newBest)
        {
            string best = null;
            var bestRound = -1;

            void Consider(int round, string message)
            {
                if (previousBest < round && newBest >= round && round >= bestRound)
                {
                    bestRound = round;
                    best = message;
                }
            }

            Consider(20, "Steel weapons unlocked! (+40% damage, +5% attack speed)");
            Consider(30, "Copper weapons unlocked!");
            Consider(40, "Silver weapons unlocked!");
            Consider(50, "Gold weapons unlocked!");
            Consider(60, "Cobalt weapons unlocked!");
            Consider(70, "Platinum weapons unlocked!");
            Consider(80, "Adamantine weapons unlocked!");
            Consider(90, "Crimson weapons unlocked!");
            Consider(100, "Fateful weapons unlocked! (+AOE splash on hit)");
            return best;
        }

        public static string TryNotifyDungeonIronUnlock(int previousBest, int newBest)
        {
            if (previousBest < IronUnlockDungeonRound && newBest >= IronUnlockDungeonRound)
                return "Iron weapons unlocked! (+20% damage, +2.5% attack speed)";
            return null;
        }

        public static string GetUnlockProgressSummary()
        {
            var iron = IsIronUnlocked() ? "Iron ✓" : $"Iron @ Dungeon R{IronUnlockDungeonRound}";
            return $"{iron} · Unlimited best R{GameSave.UnlimitedHighestRoundReached} (Steel R20 … Fateful R100)";
        }
    }
}
