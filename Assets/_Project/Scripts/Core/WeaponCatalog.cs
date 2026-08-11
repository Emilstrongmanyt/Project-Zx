namespace ProjectZx.Core
{
    /// <summary>
    /// Weapon material tiers (Admurin armory sets). Higher tiers swap sprites and grant
    /// damage + attack-speed perks. Equipped tier is the highest unlocked for that weapon type
    /// (player class), not a shared global unlock.
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
    /// Per-class weapon upgrades. Progress with a bat only upgrades Batter weapons, etc.
    /// Iron = Dungeon R30 with that class; Unlimited R20–R100 for most materials;
    /// Gold = 50,000 kills with that weapon type.
    /// </summary>
    public static class WeaponCatalog
    {
        public const int IronUnlockDungeonRound = 30;
        public const int SteelUnlockUnlimitedRound = 20;
        public const int GoldUnlockKills = 50000;

        public const float AoeSplashRadius = 1.85f;
        public const float AoeSplashDamageFraction = 0.45f;

        /// <summary>+20% damage per tier step above Wooden (additive on base).</summary>
        public const float DamageBonusPerTier = 0.20f;
        /// <summary>+2.5% attack speed per tier step above Wooden.</summary>
        public const float AttackSpeedBonusPerTier = 0.025f;

        static PlayerClass ActiveClass =>
            GameSessionContext.SelectedClass;

        /// <summary>Highest material unlocked for the active run / selected class weapon.</summary>
        public static WeaponMaterialTier GetUnlockedTier() =>
            GetUnlockedTier(ActiveClass);

        public static WeaponMaterialTier GetUnlockedTier(PlayerClass playerClass)
        {
            GameSave.EnsureWeaponProgressMigrated();

            var tier = WeaponMaterialTier.Wooden;

            if (IsIronUnlocked(playerClass))
                tier = WeaponMaterialTier.Iron;

            var u = GameSave.GetWeaponUnlimitedBest(playerClass);
            if (u >= 20) tier = WeaponMaterialTier.Steel;
            if (u >= 30) tier = WeaponMaterialTier.Copper;
            if (u >= 40) tier = WeaponMaterialTier.Silver;
            if (IsGoldUnlocked(playerClass)) tier = WeaponMaterialTier.Gold;
            if (u >= 60) tier = WeaponMaterialTier.Cobalt;
            if (u >= 70) tier = WeaponMaterialTier.Platinum;
            if (u >= 80) tier = WeaponMaterialTier.Adamantine;
            if (u >= 90) tier = WeaponMaterialTier.Crimson;
            if (u >= 100) tier = WeaponMaterialTier.Fateful;

            return tier;
        }

        public static bool IsIronUnlocked() => IsIronUnlocked(ActiveClass);

        public static bool IsIronUnlocked(PlayerClass playerClass) =>
            GameSave.GetWeaponDungeonBest(playerClass) >= IronUnlockDungeonRound;

        public static bool IsGoldUnlocked(PlayerClass playerClass) =>
            GameSave.GetWeaponKillCount(playerClass) >= GoldUnlockKills;

        public static bool HasAoeSplash() => HasAoeSplash(ActiveClass);

        public static bool HasAoeSplash(PlayerClass playerClass) =>
            GetUnlockedTier(playerClass) == WeaponMaterialTier.Fateful;

        /// <summary>True if any class has unlocked at least this tier (achievements / UI).</summary>
        public static bool AnyClassHasTier(WeaponMaterialTier tier)
        {
            foreach (PlayerClass c in System.Enum.GetValues(typeof(PlayerClass)))
            {
                if (TierIndex(GetUnlockedTier(c)) >= TierIndex(tier))
                    return true;
            }

            return false;
        }

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
        public static float DamageMultiplier(PlayerClass playerClass) =>
            DamageMultiplier(GetUnlockedTier(playerClass));

        public static float AttackSpeedMultiplier() => AttackSpeedMultiplier(GetUnlockedTier());
        public static float AttackSpeedMultiplier(PlayerClass playerClass) =>
            AttackSpeedMultiplier(GetUnlockedTier(playerClass));

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
            GetResourceName(playerClass, GetUnlockedTier(playerClass));

        public static string GetClassDisplayName(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Spearman => "Spearman",
            PlayerClass.Bowman => "Bowman",
            PlayerClass.Magician => "Magician",
            PlayerClass.Samurai => "Samurai",
            _ => "Batter"
        };

        /// <summary>
        /// Banner when a new Unlimited material tier unlocks for the class that earned it.
        /// </summary>
        public static string TryNotifyUnlimitedTierUnlock(
            PlayerClass playerClass, int previousBest, int newBest)
        {
            string best = null;
            var bestRound = -1;
            var weapon = GetClassDisplayName(playerClass);

            void Consider(int round, string message)
            {
                if (previousBest < round && newBest >= round && round >= bestRound)
                {
                    bestRound = round;
                    best = message;
                }
            }

            Consider(20, $"{weapon} Steel unlocked! (+40% damage, +5% attack speed)");
            Consider(30, $"{weapon} Copper unlocked!");
            Consider(40, $"{weapon} Silver unlocked!");
            // Gold is kill-based (not Unlimited R50).
            Consider(60, $"{weapon} Cobalt unlocked!");
            Consider(70, $"{weapon} Platinum unlocked!");
            Consider(80, $"{weapon} Adamantine unlocked!");
            Consider(90, $"{weapon} Crimson unlocked!");
            Consider(100, $"{weapon} Fateful unlocked! (+AOE splash on hit)");
            return best;
        }

        public static string TryNotifyDungeonIronUnlock(
            PlayerClass playerClass, int previousBest, int newBest)
        {
            if (previousBest < IronUnlockDungeonRound && newBest >= IronUnlockDungeonRound)
                return $"{GetClassDisplayName(playerClass)} Iron unlocked! (+20% damage, +2.5% attack speed)";
            return null;
        }

        public static string TryNotifyGoldUnlock(PlayerClass playerClass, int previousKills, int newKills)
        {
            if (previousKills < GoldUnlockKills && newKills >= GoldUnlockKills)
                return $"{GetClassDisplayName(playerClass)} Gold unlocked! ({GoldUnlockKills:N0} kills with this weapon)";
            return null;
        }

        public static string GetUnlockProgressSummary(PlayerClass playerClass)
        {
            GameSave.EnsureWeaponProgressMigrated();
            var iron = IsIronUnlocked(playerClass)
                ? "Iron ✓"
                : $"Iron @ Dungeon R{IronUnlockDungeonRound}";
            var gold = IsGoldUnlocked(playerClass)
                ? "Gold ✓"
                : $"Gold @ {GameSave.GetWeaponKillCount(playerClass):N0}/{GoldUnlockKills:N0} kills";
            var u = GameSave.GetWeaponUnlimitedBest(playerClass);
            return $"{iron} · {gold} · Unlimited best R{u} (Steel R20 … Fateful R100, per weapon)";
        }

        public static string GetUnlockProgressSummary() =>
            GetUnlockProgressSummary(ActiveClass);
    }
}
