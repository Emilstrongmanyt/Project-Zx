namespace ProjectZx.Core
{
    /// <summary>
    /// Weapon material tiers (Admurin armory sets). Higher tiers swap sprites and grant
    /// damage + attack-speed perks. Unlocks are per class; players equip any unlocked tier
    /// from Build Loadout (defaults to the highest unlocked).
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
    /// Iron = Ironvault R30 with that class; Unlimited R20–R100 for most materials;
    /// Gold = 50,000 kills with that weapon type.
    /// </summary>
    public static class WeaponCatalog
    {
        public const int IronUnlockDungeonRound = 30;
        public const int SteelUnlockUnlimitedRound = 20;
        public const int GoldUnlockKills = 50000;

        /// <summary>Order players should chase. Gold sits between Silver and Cobalt; Altair is a quest relic, not a step.</summary>
        static readonly WeaponMaterialTier[] ProgressionLadder =
        {
            WeaponMaterialTier.Wooden,
            WeaponMaterialTier.Iron,
            WeaponMaterialTier.Steel,
            WeaponMaterialTier.Copper,
            WeaponMaterialTier.Silver,
            WeaponMaterialTier.Gold,
            WeaponMaterialTier.Cobalt,
            WeaponMaterialTier.Platinum,
            WeaponMaterialTier.Adamantine,
            WeaponMaterialTier.Crimson,
            WeaponMaterialTier.Fateful
        };

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

            var best = WeaponMaterialTier.Wooden;
            var bestIndex = TierIndex(best);
            for (var i = 0; i < ProgressionLadder.Length; i++)
            {
                var tier = ProgressionLadder[i];
                if (!IsTierUnlocked(playerClass, tier)) continue;
                var index = TierIndex(tier);
                if (index < bestIndex) continue;
                bestIndex = index;
                best = tier;
            }

            return best;
        }

        public static bool IsIronUnlocked() => IsIronUnlocked(ActiveClass);

        public static bool IsIronUnlocked(PlayerClass playerClass) =>
            GameSave.GetWeaponDungeonBest(playerClass) >= IronUnlockDungeonRound;

        public static bool IsGoldUnlocked(PlayerClass playerClass) =>
            GameSave.GetWeaponKillCount(playerClass) >= GoldUnlockKills;

        /// <summary>Currently equipped material (clamped to unlocks).</summary>
        public static WeaponMaterialTier GetEquippedTier() =>
            GetEquippedTier(ActiveClass);

        public static WeaponMaterialTier GetEquippedTier(PlayerClass playerClass) =>
            GameSave.GetEquippedWeaponTier(playerClass);

        public static bool IsTierUnlocked(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            if (tier == WeaponMaterialTier.Angelic)
                return false;
            if (tier == WeaponMaterialTier.Altair)
                return GameSave.AltairUnlocked;

            EnsureProgress(playerClass);
            return tier switch
            {
                WeaponMaterialTier.Wooden => true,
                WeaponMaterialTier.Iron => IsIronUnlocked(playerClass),
                WeaponMaterialTier.Gold => IsGoldUnlocked(playerClass),
                _ => MeetsUnlimitedRound(playerClass, tier)
            };
        }

        static void EnsureProgress(PlayerClass playerClass) =>
            GameSave.EnsureWeaponProgressMigrated();

        /// <summary>Unlocked tiers for the class, low → high (for loadout cycling).</summary>
        public static System.Collections.Generic.List<WeaponMaterialTier> GetSelectableTiers(
            PlayerClass playerClass)
        {
            var list = new System.Collections.Generic.List<WeaponMaterialTier>(12);
            foreach (WeaponMaterialTier t in System.Enum.GetValues(typeof(WeaponMaterialTier)))
            {
                if (IsTierUnlocked(playerClass, t))
                    list.Add(t);
            }

            list.Sort((a, b) =>
            {
                var byPower = TierIndex(a).CompareTo(TierIndex(b));
                return byPower != 0 ? byPower : ((int)a).CompareTo((int)b);
            });
            return list;
        }

        public static bool HasAoeSplash() => HasAoeSplash(ActiveClass);

        public static bool HasAoeSplash(PlayerClass playerClass) =>
            GetEquippedTier(playerClass) == WeaponMaterialTier.Fateful;

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
            if (tier == WeaponMaterialTier.Altair)
            {
                var dmgA = RoundPct(DamageMultiplier(tier) - 1f);
                var aspdA = RoundPct(AttackSpeedMultiplier(tier) - 1f);
                return $"Ash Crown relic — Fateful power (+{dmgA}% dmg, +{aspdA}% AS), Altair look";
            }

            var dmg = RoundPct(DamageMultiplier(tier) - 1f);
            var aspd = RoundPct(AttackSpeedMultiplier(tier) - 1f);
            // Combat AOE is HasAoeSplash only when equipped == Fateful (not Altair/Angelic).
            var aoe = tier == WeaponMaterialTier.Fateful ? ", AOE splash" : "";
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

        public static float DamageMultiplier() => DamageMultiplier(GetEquippedTier());
        public static float DamageMultiplier(PlayerClass playerClass) =>
            DamageMultiplier(GetEquippedTier(playerClass));

        public static float AttackSpeedMultiplier() => AttackSpeedMultiplier(GetEquippedTier());
        public static float AttackSpeedMultiplier(PlayerClass playerClass) =>
            AttackSpeedMultiplier(GetEquippedTier(playerClass));

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
            GetResourceName(playerClass, GetEquippedTier(playerClass));

        public static string GetClassDisplayName(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Spearman => "Spearman",
            PlayerClass.Bowman => "Bowman",
            PlayerClass.Magician => "Magician",
            PlayerClass.Samurai => "Samurai",
            PlayerClass.Paladin => "Paladin",
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

        /// <summary>Endless Front round that unlocks this material, or 0 when the tier uses another rule.</summary>
        public static int UnlimitedUnlockRound(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Steel => SteelUnlockUnlimitedRound,
            WeaponMaterialTier.Copper => 30,
            WeaponMaterialTier.Silver => 40,
            WeaponMaterialTier.Cobalt => 60,
            WeaponMaterialTier.Platinum => 70,
            WeaponMaterialTier.Adamantine => 80,
            WeaponMaterialTier.Crimson => 90,
            WeaponMaterialTier.Fateful => 100,
            _ => 0
        };

        static bool MeetsUnlimitedRound(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            var round = UnlimitedUnlockRound(tier);
            return round > 0 && GameSave.GetWeaponUnlimitedBest(playerClass) >= round;
        }

        /// <summary>
        /// Next material that is stronger than the highest one already unlocked.
        /// A skipped Gold tier is reported separately so it is not presented as the upgrade.
        /// </summary>
        public static bool TryGetNextTier(PlayerClass playerClass, out WeaponMaterialTier tier)
        {
            GameSave.EnsureWeaponProgressMigrated();
            var highestIndex = TierIndex(GetUnlockedTier(playerClass));
            for (var i = 0; i < ProgressionLadder.Length; i++)
            {
                var step = ProgressionLadder[i];
                if (TierIndex(step) <= highestIndex) continue;
                if (IsTierUnlocked(playerClass, step)) continue;
                tier = step;
                return true;
            }

            tier = WeaponMaterialTier.Fateful;
            return false;
        }

        /// <summary>One sentence: the next stronger material and exactly how this class earns it.</summary>
        public static string GetNextStepLine(PlayerClass playerClass)
        {
            if (!TryGetNextTier(playerClass, out var next))
                return "Weapon path complete — Fateful is the last material for this class.";

            return $"Next: {GetTierName(next)} — {GetUnlockInstruction(playerClass, next)}";
        }

        /// <summary>Loadout block: equipped perk, the next step, and the step after that.</summary>
        public static string GetLoadoutProgressText(PlayerClass playerClass)
        {
            var equipped = GetEquippedTier(playerClass);
            var text = $"Equipped: {GetTierName(equipped)} · {GetPerkSummary(equipped)}\n{GetNextStepLine(playerClass)}";
            if (TryGetNextTier(playerClass, out var next))
            {
                var after = GetStepAfter(next);
                if (after.HasValue)
                    text += $"\nAfter that: {GetTierName(after.Value)} — {ShortRequirement(after.Value)}.";
            }

            var goldNote = GetSkippedGoldNote(playerClass);
            if (!string.IsNullOrEmpty(goldNote))
                text += $"\n{goldNote}";
            return text;
        }

        public static string GetUnlockProgressSummary(PlayerClass playerClass) =>
            GetLoadoutProgressText(playerClass);

        static WeaponMaterialTier? GetStepAfter(WeaponMaterialTier tier)
        {
            var index = System.Array.IndexOf(ProgressionLadder, tier);
            if (index < 0 || index + 1 >= ProgressionLadder.Length) return null;
            return ProgressionLadder[index + 1];
        }

        static string GetSkippedGoldNote(PlayerClass playerClass)
        {
            if (IsGoldUnlocked(playerClass)) return null;
            if (TierIndex(GetUnlockedTier(playerClass)) <= TierIndex(WeaponMaterialTier.Gold)) return null;
            var kills = GameSave.GetWeaponKillCount(playerClass);
            return $"Gold is still open ({kills:N0} / {GoldUnlockKills:N0} kills with this class). Stronger materials are already unlocked.";
        }

        static string ShortRequirement(WeaponMaterialTier tier)
        {
            if (tier == WeaponMaterialTier.Iron)
                return $"Ironvault round {IronUnlockDungeonRound} with this class";
            if (tier == WeaponMaterialTier.Gold)
                return $"{GoldUnlockKills:N0} kills with this class";
            var round = UnlimitedUnlockRound(tier);
            if (round > 0)
                return $"Endless Front round {round} with this class";
            return GetTierName(tier);
        }

        static string GetUnlockInstruction(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            var who = GetClassDisplayName(playerClass);
            if (tier == WeaponMaterialTier.Iron)
            {
                if (!GameSave.DungeonMapUnlocked)
                    return $"Ironvault is locked. Clear the Warded Halls gateway, then reach round {IronUnlockDungeonRound} there as {who}.";

                var best = GameSave.GetWeaponDungeonBest(playerClass);
                return $"Reach Ironvault round {IronUnlockDungeonRound} as {who} (best R{best}). Other classes do not count.";
            }

            if (tier == WeaponMaterialTier.Gold)
            {
                var kills = GameSave.GetWeaponKillCount(playerClass);
                return $"Defeat {GoldUnlockKills:N0} enemies as {who} ({kills:N0} / {GoldUnlockKills:N0}).";
            }

            var round = UnlimitedUnlockRound(tier);
            if (round <= 0)
                return $"Unlock {GetTierName(tier)} as {who}.";

            if (!GameSave.UnlimitedMapUnlocked)
                return $"The Endless Front is locked. Clear Silent Ossuary round 50, then reach round {round} there as {who}.";

            var frontBest = GameSave.GetWeaponUnlimitedBest(playerClass);
            return $"Reach Endless Front round {round} as {who} (best R{frontBest}). Other classes do not count.";
        }

        public static string GetUnlockProgressSummary() =>
            GetUnlockProgressSummary(ActiveClass);
    }
}
