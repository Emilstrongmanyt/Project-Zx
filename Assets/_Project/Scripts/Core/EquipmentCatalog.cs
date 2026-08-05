using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Core
{
    public enum EquipmentSlot
    {
        Ring,
        Necklace,
        Cape
    }

    public enum EquipmentId
    {
        None = 0,
        /// <summary>Admurin fortitude ring — +gold find.</summary>
        FortuneRing = 1,
        /// <summary>Admurin triple gem ring — +damage.</summary>
        PrismRing = 2,
        /// <summary>Admurin hearty necklace — +max HP.</summary>
        JadeNecklace = 3,
        /// <summary>Admurin skull charm necklace — +attack speed.</summary>
        SkullNecklace = 4,
        /// <summary>Admurin nimble ring — +move speed.</summary>
        NimbleRing = 5,
        /// <summary>Admurin protector necklace — +max HP.</summary>
        ProtectorNecklace = 6,
        /// <summary>Soft wool cape — damage reduction.</summary>
        WoolCape = 7,
        /// <summary>Sentinel cape — block chance.</summary>
        SentinelCape = 8,
        /// <summary>Heavy ironweave cape — stronger damage reduction.</summary>
        IronweaveCape = 9
        // GuardianCape = 10 reserved (removed so each slot type has 3 items).
    }

    public readonly struct EquipmentDef
    {
        public readonly EquipmentId Id;
        public readonly EquipmentSlot Slot;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly float DamageMultiplier;
        public readonly float GoldFindMultiplier;
        public readonly float AttackSpeedMultiplier;
        public readonly float MoveSpeedMultiplier;
        /// <summary>Additive fraction of damage ignored (0.08 = −8% damage taken).</summary>
        public readonly float DamageReduction;
        /// <summary>Chance 0–1 to fully block an incoming hit.</summary>
        public readonly float BlockChance;
        public readonly int BonusMaxHp;

        public EquipmentDef(
            EquipmentId id,
            EquipmentSlot slot,
            string displayName,
            string description,
            float damageMultiplier = 1f,
            float goldFindMultiplier = 1f,
            float attackSpeedMultiplier = 1f,
            float moveSpeedMultiplier = 1f,
            float damageReduction = 0f,
            float blockChance = 0f,
            int bonusMaxHp = 0)
        {
            Id = id;
            Slot = slot;
            DisplayName = displayName;
            Description = description;
            DamageMultiplier = damageMultiplier;
            GoldFindMultiplier = goldFindMultiplier;
            AttackSpeedMultiplier = attackSpeedMultiplier;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            DamageReduction = damageReduction;
            BlockChance = blockChance;
            BonusMaxHp = bonusMaxHp;
        }
    }

    public static class EquipmentCatalog
    {
        public static readonly EquipmentDef[] All =
        {
            new(EquipmentId.FortuneRing, EquipmentSlot.Ring, "Fortune Ring",
                "+15% gold from kills", goldFindMultiplier: 1.15f),
            new(EquipmentId.PrismRing, EquipmentSlot.Ring, "Prism Ring",
                "+8% damage", damageMultiplier: 1.08f),
            new(EquipmentId.NimbleRing, EquipmentSlot.Ring, "Nimble Ring",
                "+10% move speed", moveSpeedMultiplier: 1.1f),
            new(EquipmentId.JadeNecklace, EquipmentSlot.Necklace, "Jade Necklace",
                "+20 Max HP", bonusMaxHp: 20),
            new(EquipmentId.SkullNecklace, EquipmentSlot.Necklace, "Skull Necklace",
                "+10% attack speed", attackSpeedMultiplier: 1.1f),
            new(EquipmentId.ProtectorNecklace, EquipmentSlot.Necklace, "Protector Necklace",
                "+40 Max HP", bonusMaxHp: 40),
            new(EquipmentId.WoolCape, EquipmentSlot.Cape, "Wool Cape",
                "−8% damage taken", damageReduction: 0.08f),
            new(EquipmentId.SentinelCape, EquipmentSlot.Cape, "Sentinel Cape",
                "+12% block chance", blockChance: 0.12f),
            new(EquipmentId.IronweaveCape, EquipmentSlot.Cape, "Ironweave Cape",
                "−12% damage taken", damageReduction: 0.12f)
        };

        public static EquipmentDef Get(EquipmentId id)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            return default;
        }

        public static bool IsValid(EquipmentId id) => id != EquipmentId.None && Get(id).Id == id;

        public static Sprite GetIcon(EquipmentId id)
        {
            return id switch
            {
                EquipmentId.FortuneRing => ArtLibrary.FortuneRing,
                EquipmentId.PrismRing => ArtLibrary.PrismRing,
                EquipmentId.NimbleRing => ArtLibrary.NimbleRing,
                EquipmentId.JadeNecklace => ArtLibrary.Necklace,
                EquipmentId.SkullNecklace => ArtLibrary.SkullNecklace,
                EquipmentId.ProtectorNecklace => ArtLibrary.ProtectorNecklace,
                EquipmentId.WoolCape => ArtLibrary.WoolCape,
                EquipmentId.SentinelCape => ArtLibrary.SentinelCape,
                EquipmentId.IronweaveCape => ArtLibrary.IronweaveCape,
                _ => null
            };
        }

        /// <summary>
        /// Random equipment drop, excluding items the player already owns/discovered.
        /// Returns <see cref="EquipmentId.None"/> when every item is already owned.
        /// </summary>
        public static EquipmentId RollRandomDrop()
        {
            var pool = new List<EquipmentId>(All.Length);
            for (var i = 0; i < All.Length; i++)
            {
                var id = All[i].Id;
                if (!GameSave.OwnsEquipment(id))
                    pool.Add(id);
            }

            if (pool.Count == 0) return EquipmentId.None;
            return pool[Random.Range(0, pool.Count)];
        }

        static void ForEachEquipped(System.Action<EquipmentDef> apply)
        {
            ApplyOne(GameSave.EquippedRing, apply);
            ApplyOne(GameSave.EquippedNecklace, apply);
            ApplyOne(GameSave.EquippedCape, apply);

            static void ApplyOne(EquipmentId id, System.Action<EquipmentDef> action)
            {
                var def = Get(id);
                if (def.Id != EquipmentId.None)
                    action(def);
            }
        }

        public static float CombinedDamageMultiplier()
        {
            var m = 1f;
            ForEachEquipped(d => m *= d.DamageMultiplier);
            return m;
        }

        public static float CombinedGoldFindMultiplier()
        {
            var m = 1f;
            ForEachEquipped(d => m *= d.GoldFindMultiplier);
            return m;
        }

        public static float CombinedAttackSpeedMultiplier()
        {
            var m = 1f;
            ForEachEquipped(d => m *= d.AttackSpeedMultiplier);
            return m;
        }

        public static float CombinedMoveSpeedMultiplier()
        {
            var m = 1f;
            ForEachEquipped(d => m *= d.MoveSpeedMultiplier);
            return m;
        }

        /// <summary>Sum of equipped additive damage reduction (capped elsewhere at combat time).</summary>
        public static float CombinedDamageReduction()
        {
            var r = 0f;
            ForEachEquipped(d => r += d.DamageReduction);
            return r;
        }

        /// <summary>Sum of equipped block chance (capped elsewhere at combat time).</summary>
        public static float CombinedBlockChance()
        {
            var c = 0f;
            ForEachEquipped(d => c += d.BlockChance);
            return c;
        }

        public static int CombinedBonusMaxHp()
        {
            var hp = 0;
            ForEachEquipped(d => hp += d.BonusMaxHp);
            return hp;
        }
    }
}
