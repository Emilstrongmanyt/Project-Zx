using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Core
{
    public enum EpicTalentId
    {
        None = 0,
        Bloodforged = 1,
        IronVeil = 2,
        ExecutionersEdge = 3,
        GildedGreed = 4,
        TempestStrikes = 5,
        SoulDrain = 6,
        BossBreaker = 7,
        ArcaneEcho = 8,
        PhoenixHeart = 9,
        TreasureMagnet = 10,
        Bloodletting = 11
    }

    /// <summary>
    /// Boss-crystal epic talents: stronger than level-up picks, run-scoped, max 3 per run.
    /// </summary>
    public static class EpicTalentCatalog
    {
        public const int MaxPicksPerRun = 3;
        public const int ChoicesOffered = 3;

        public static readonly EpicTalentId[] All =
        {
            EpicTalentId.Bloodforged,
            EpicTalentId.IronVeil,
            EpicTalentId.ExecutionersEdge,
            EpicTalentId.GildedGreed,
            EpicTalentId.TempestStrikes,
            EpicTalentId.SoulDrain,
            EpicTalentId.BossBreaker,
            EpicTalentId.ArcaneEcho,
            EpicTalentId.PhoenixHeart,
            EpicTalentId.TreasureMagnet,
            EpicTalentId.Bloodletting
        };

        public static string GetTitle(EpicTalentId id) => id switch
        {
            EpicTalentId.Bloodforged => "Bloodforged",
            EpicTalentId.IronVeil => "Iron Veil",
            EpicTalentId.ExecutionersEdge => "Executioner's Edge",
            EpicTalentId.GildedGreed => "Gilded Greed",
            EpicTalentId.TempestStrikes => "Tempest Strikes",
            EpicTalentId.SoulDrain => "Soul Drain",
            EpicTalentId.BossBreaker => "Boss Breaker",
            EpicTalentId.ArcaneEcho => "Arcane Echo",
            EpicTalentId.PhoenixHeart => "Phoenix Heart",
            EpicTalentId.TreasureMagnet => "Treasure Magnet",
            EpicTalentId.Bloodletting => "Bloodletting",
            _ => id.ToString()
        };

        public static string GetDescription(EpicTalentId id) => id switch
        {
            EpicTalentId.Bloodforged => "+25% damage, but take +10% damage",
            EpicTalentId.IronVeil => "Absorb 30% Max HP every 20s",
            EpicTalentId.ExecutionersEdge => "+40% damage to enemies under 30% HP",
            EpicTalentId.GildedGreed => "+40% gold and +20% XP",
            EpicTalentId.TempestStrikes => "+25% attack speed, +15% move speed",
            EpicTalentId.SoulDrain => "+8% lifesteal, +10 Max HP",
            EpicTalentId.BossBreaker => "+35% vs bosses, +10% vs normals",
            EpicTalentId.ArcaneEcho => "25% chance to echo hit for 50% damage",
            EpicTalentId.PhoenixHeart => "Once: revive at 40% HP with 2s i-frames",
            EpicTalentId.TreasureMagnet => "+50% loot collect range",
            EpicTalentId.Bloodletting => "Hits and hits taken bleed 20% over 2s",
            _ => ""
        };

        public static string GetButtonLabel(EpicTalentId id) =>
            $"{GetTitle(id)}\n{GetDescription(id)}";

        public static bool IsUnique(EpicTalentId id) =>
            id is EpicTalentId.PhoenixHeart or EpicTalentId.IronVeil or EpicTalentId.Bloodletting
                or EpicTalentId.ArcaneEcho or EpicTalentId.Bloodforged or EpicTalentId.ExecutionersEdge
                or EpicTalentId.BossBreaker or EpicTalentId.TreasureMagnet or EpicTalentId.GildedGreed
                or EpicTalentId.TempestStrikes or EpicTalentId.SoulDrain;

        public static bool HasTalent(int ownedMask, EpicTalentId id)
        {
            if (id == EpicTalentId.None) return false;
            return (ownedMask & (1 << (int)id)) != 0;
        }

        public static int WithTalent(int ownedMask, EpicTalentId id)
        {
            if (id == EpicTalentId.None) return ownedMask;
            return ownedMask | (1 << (int)id);
        }

        public static List<EpicTalentId> RollChoices(int ownedMask, int count = ChoicesOffered)
        {
            var pool = new List<EpicTalentId>(All.Length);
            for (var i = 0; i < All.Length; i++)
            {
                var id = All[i];
                if (IsUnique(id) && HasTalent(ownedMask, id)) continue;
                pool.Add(id);
            }

            if (pool.Count == 0)
            {
                // Fallback if every unique is taken — re-offer stacking-safe greed/speed style.
                pool.Add(EpicTalentId.GildedGreed);
                pool.Add(EpicTalentId.TempestStrikes);
                pool.Add(EpicTalentId.SoulDrain);
            }

            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, Mathf.Min(count, pool.Count));
        }
    }
}
