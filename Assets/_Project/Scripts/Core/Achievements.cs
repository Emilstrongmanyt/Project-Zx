using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectZx.Core
{
    public enum AchievementId
    {
        ZombieStomper25,
        ZombieStomper100,
        ZombieStomper500,
        ZombieStomper1000,
        ZombieStomper5000,
        ZombieStomper10000,
        BossSlayer1,
        BossSlayer5,
        BossSlayer10,
        BossSlayer25,
        BossSlayer50,
        BossSlayer100,
        RoundPioneer10,
        RoundPioneer20,
        RoundPioneer30,
        RoundPioneer40,
        RoundPioneer50,
        RoundPioneer60,
        RoundPioneer70,
        RoundPioneer80,
        DungeonDelver,
        InsideArcher,
        TogetherAgain,
        DungeonClearer,
        EndlessHorizon,
        // Weapon material tier unlocks (matches WeaponCatalog progression).
        IronArsenal,
        SteelArsenal,
        CopperArsenal,
        SilverArsenal,
        GoldArsenal,
        CobaltArsenal,
        PlatinumArsenal,
        AdamantineArsenal,
        CrimsonArsenal,
        FatefulArsenal
    }

    public readonly struct AchievementDef
    {
        public readonly AchievementId Id;
        public readonly string Title;
        public readonly string Description;

        public AchievementDef(AchievementId id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    public static class Achievements
    {
        const string UnlockPrefix = "zx_ach_";
        public const int CompletionGoldReward = 250;

        static readonly AchievementDef[] Catalog =
        {
            new(AchievementId.ZombieStomper25, "Zombie Stomper I", "Defeat 25 zombies."),
            new(AchievementId.ZombieStomper100, "Zombie Stomper II", "Defeat 100 zombies."),
            new(AchievementId.ZombieStomper500, "Zombie Stomper III", "Defeat 500 zombies."),
            new(AchievementId.ZombieStomper1000, "Zombie Stomper IV", "Defeat 1,000 zombies."),
            new(AchievementId.ZombieStomper5000, "Zombie Stomper V", "Defeat 5,000 zombies."),
            new(AchievementId.ZombieStomper10000, "Zombie Stomper VI", "Defeat 10,000 zombies."),
            new(AchievementId.BossSlayer1, "Boss Slayer I", "Defeat 1 boss."),
            new(AchievementId.BossSlayer5, "Boss Slayer II", "Defeat 5 bosses."),
            new(AchievementId.BossSlayer10, "Boss Slayer III", "Defeat 10 bosses."),
            new(AchievementId.BossSlayer25, "Boss Slayer IV", "Defeat 25 bosses."),
            new(AchievementId.BossSlayer50, "Boss Slayer V", "Defeat 50 bosses."),
            new(AchievementId.BossSlayer100, "Boss Slayer VI", "Defeat 100 bosses."),
            new(AchievementId.RoundPioneer10, "Round Pioneer X", "Reach round 10."),
            new(AchievementId.RoundPioneer20, "Round Pioneer XX", "Reach round 20."),
            new(AchievementId.RoundPioneer30, "Round Pioneer XXX", "Reach round 30."),
            new(AchievementId.RoundPioneer40, "Round Pioneer XL", "Reach round 40."),
            new(AchievementId.RoundPioneer50, "Round Pioneer L", "Reach round 50."),
            new(AchievementId.RoundPioneer60, "Round Pioneer LX", "Reach round 60."),
            new(AchievementId.RoundPioneer70, "Round Pioneer LXX", "Reach round 70."),
            new(AchievementId.RoundPioneer80, "Round Pioneer LXXX", "Reach round 80."),
            new(AchievementId.DungeonDelver, "Dungeon Delver", "Clear Outside round 20 and enter the door to Inside Survival."),
            new(AchievementId.InsideArcher, "Inside Archer", "Clear round 30 on Inside survival."),
            new(AchievementId.TogetherAgain, "Together Again", "Meet RowZi at the Outside round 20 door."),
            new(AchievementId.DungeonClearer, "Dungeon Clearer", "Defeat the Dungeon round 40 boss and enter the victory portal."),
            new(AchievementId.EndlessHorizon, "Endless Horizon", "Unlock Unlimited Survival after clearing Dungeon."),
            new(AchievementId.IronArsenal, "Iron Arsenal", "Unlock Iron weapons (Dungeon Survival round 30)."),
            new(AchievementId.SteelArsenal, "Steel Arsenal", "Unlock Steel weapons (Unlimited Survival round 20)."),
            new(AchievementId.CopperArsenal, "Copper Arsenal", "Unlock Copper weapons (Unlimited Survival round 30)."),
            new(AchievementId.SilverArsenal, "Silver Arsenal", "Unlock Silver weapons (Unlimited Survival round 40)."),
            new(AchievementId.GoldArsenal, "Gold Arsenal", "Unlock Gold weapons (Unlimited Survival round 50)."),
            new(AchievementId.CobaltArsenal, "Cobalt Arsenal", "Unlock Cobalt weapons (Unlimited Survival round 60)."),
            new(AchievementId.PlatinumArsenal, "Platinum Arsenal", "Unlock Platinum weapons (Unlimited Survival round 70)."),
            new(AchievementId.AdamantineArsenal, "Adamantine Arsenal", "Unlock Adamantine weapons (Unlimited Survival round 80)."),
            new(AchievementId.CrimsonArsenal, "Crimson Arsenal", "Unlock Crimson weapons (Unlimited Survival round 90)."),
            new(AchievementId.FatefulArsenal, "Fateful Arsenal", "Unlock Fateful weapons with AOE splash (Unlimited Survival round 100).")
        };

        static readonly int[] ZombieThresholds = { 25, 100, 500, 1000, 5000, 10000 };
        static readonly AchievementId[] ZombieTiers =
        {
            AchievementId.ZombieStomper25,
            AchievementId.ZombieStomper100,
            AchievementId.ZombieStomper500,
            AchievementId.ZombieStomper1000,
            AchievementId.ZombieStomper5000,
            AchievementId.ZombieStomper10000
        };

        static readonly int[] BossThresholds = { 1, 5, 10, 25, 50, 100 };
        static readonly AchievementId[] BossTiers =
        {
            AchievementId.BossSlayer1,
            AchievementId.BossSlayer5,
            AchievementId.BossSlayer10,
            AchievementId.BossSlayer25,
            AchievementId.BossSlayer50,
            AchievementId.BossSlayer100
        };

        static readonly int[] RoundThresholds = { 10, 20, 30, 40, 50, 60, 70, 80 };
        static readonly AchievementId[] RoundTiers =
        {
            AchievementId.RoundPioneer10,
            AchievementId.RoundPioneer20,
            AchievementId.RoundPioneer30,
            AchievementId.RoundPioneer40,
            AchievementId.RoundPioneer50,
            AchievementId.RoundPioneer60,
            AchievementId.RoundPioneer70,
            AchievementId.RoundPioneer80
        };

        public static IReadOnlyList<AchievementDef> All => Catalog;

        public static event Action<AchievementDef> OnUnlocked;

        public static AchievementDef GetDef(AchievementId id)
        {
            foreach (var def in Catalog)
            {
                if (def.Id == id) return def;
            }

            return Catalog[0];
        }

        public static bool IsUnlocked(AchievementId id) =>
            PlayerPrefs.GetInt(UnlockPrefix + id, 0) == 1;

        public static int UnlockedCount
        {
            get
            {
                var count = 0;
                foreach (var def in Catalog)
                {
                    if (IsUnlocked(def.Id)) count++;
                }

                return count;
            }
        }

        /// <summary>+5% run XP permanently per completed achievement.</summary>
        public static float AchievementXpMultiplier => 1f + UnlockedCount * 0.05f;

        public static bool TryUnlock(AchievementId id)
        {
            if (IsUnlocked(id)) return false;
            PlayerPrefs.SetInt(UnlockPrefix + id, 1);
            PlayerPrefs.Save();
            // Permanent bank gold — available in camp even if unlocked mid-run.
            GameSave.Gold += CompletionGoldReward;
            GameSave.LifetimeGoldEarned += CompletionGoldReward;
            OnUnlocked?.Invoke(GetDef(id));
            return true;
        }

        public static void Unlock(AchievementId id) => TryUnlock(id);

        public static void EvaluateKillAchievements()
        {
            for (var i = 0; i < ZombieThresholds.Length; i++)
            {
                if (GameSave.LifetimeZombieKills >= ZombieThresholds[i])
                    TryUnlock(ZombieTiers[i]);
            }

            for (var i = 0; i < BossThresholds.Length; i++)
            {
                if (GameSave.LifetimeBossKills >= BossThresholds[i])
                    TryUnlock(BossTiers[i]);
            }
        }

        public static void EvaluateRoundAchievements(int round)
        {
            for (var i = 0; i < RoundThresholds.Length; i++)
            {
                if (round >= RoundThresholds[i])
                    TryUnlock(RoundTiers[i]);
            }
        }

        public static void UnlockDungeonDelver() => TryUnlock(AchievementId.DungeonDelver);

        public static void UnlockInsideArcher() => TryUnlock(AchievementId.InsideArcher);

        public static void UnlockTogetherAgain() => TryUnlock(AchievementId.TogetherAgain);

        public static void UnlockDungeonClearer() => TryUnlock(AchievementId.DungeonClearer);

        public static void UnlockEndlessHorizon() => TryUnlock(AchievementId.EndlessHorizon);

        /// <summary>
        /// Unlocks arsenal achievements for any weapon tier the player has already earned.
        /// Safe to call repeatedly (TryUnlock is idempotent).
        /// </summary>
        public static void EvaluateWeaponTierAchievements()
        {
            if (WeaponCatalog.IsIronUnlocked())
                TryUnlock(AchievementId.IronArsenal);

            var unlimited = GameSave.UnlimitedHighestRoundReached;
            if (unlimited >= 20) TryUnlock(AchievementId.SteelArsenal);
            if (unlimited >= 30) TryUnlock(AchievementId.CopperArsenal);
            if (unlimited >= 40) TryUnlock(AchievementId.SilverArsenal);
            if (unlimited >= 50) TryUnlock(AchievementId.GoldArsenal);
            if (unlimited >= 60) TryUnlock(AchievementId.CobaltArsenal);
            if (unlimited >= 70) TryUnlock(AchievementId.PlatinumArsenal);
            if (unlimited >= 80) TryUnlock(AchievementId.AdamantineArsenal);
            if (unlimited >= 90) TryUnlock(AchievementId.CrimsonArsenal);
            if (unlimited >= 100) TryUnlock(AchievementId.FatefulArsenal);
        }

        public static string BuildPanelText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Unlocked {UnlockedCount}/{Catalog.Length}");
            sb.AppendLine($"Each unlock rewards {CompletionGoldReward} gold.");
            sb.AppendLine($"+5% run XP per achievement (now x{AchievementXpMultiplier:0.##}).");
            sb.AppendLine();

            foreach (var def in Catalog)
            {
                var status = IsUnlocked(def.Id) ? "[X]" : "[ ]";
                sb.AppendLine($"{status} {def.Title}");
                sb.AppendLine($"    {def.Description}");
            }

            return sb.ToString();
        }
    }
}