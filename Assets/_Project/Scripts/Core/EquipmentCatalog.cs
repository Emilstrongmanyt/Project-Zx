using UnityEngine;

namespace ProjectZx.Core
{
    public enum EquipmentSlot
    {
        Ring,
        Necklace
    }

    public enum EquipmentId
    {
        None = 0,
        /// <summary>Sparkles.png — gold ring with gem.</summary>
        FortuneRing = 1,
        /// <summary>Sparkles2.png — multi-gem ring band.</summary>
        PrismRing = 2,
        /// <summary>Necklace.png — jeweled amulet.</summary>
        JadeNecklace = 3,
        /// <summary>Skull Necklace.png — skull pendant.</summary>
        SkullNecklace = 4
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
        public readonly int BonusMaxHp;

        public EquipmentDef(
            EquipmentId id,
            EquipmentSlot slot,
            string displayName,
            string description,
            float damageMultiplier = 1f,
            float goldFindMultiplier = 1f,
            float attackSpeedMultiplier = 1f,
            int bonusMaxHp = 0)
        {
            Id = id;
            Slot = slot;
            DisplayName = displayName;
            Description = description;
            DamageMultiplier = damageMultiplier;
            GoldFindMultiplier = goldFindMultiplier;
            AttackSpeedMultiplier = attackSpeedMultiplier;
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
            new(EquipmentId.JadeNecklace, EquipmentSlot.Necklace, "Jade Necklace",
                "+20 Max HP", bonusMaxHp: 20),
            new(EquipmentId.SkullNecklace, EquipmentSlot.Necklace, "Skull Necklace",
                "+10% attack speed", attackSpeedMultiplier: 1.1f)
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
                EquipmentId.FortuneRing => ArtLibrary.Sparkles,
                EquipmentId.PrismRing => ArtLibrary.Sparkles2,
                EquipmentId.JadeNecklace => ArtLibrary.Necklace,
                EquipmentId.SkullNecklace => ArtLibrary.SkullNecklace,
                _ => null
            };
        }

        public static EquipmentId RollRandomDrop()
        {
            if (All.Length == 0) return EquipmentId.None;
            return All[Random.Range(0, All.Length)].Id;
        }

        public static float CombinedDamageMultiplier()
        {
            var m = 1f;
            m *= MultOrOne(GameSave.EquippedRing);
            m *= MultOrOne(GameSave.EquippedNecklace);
            return m;

            static float MultOrOne(EquipmentId id)
            {
                var def = Get(id);
                return def.Id == EquipmentId.None ? 1f : def.DamageMultiplier;
            }
        }

        public static float CombinedGoldFindMultiplier()
        {
            var m = 1f;
            Apply(GameSave.EquippedRing, ref m);
            Apply(GameSave.EquippedNecklace, ref m);
            return m;

            static void Apply(EquipmentId id, ref float mult)
            {
                var def = Get(id);
                if (def.Id != EquipmentId.None)
                    mult *= def.GoldFindMultiplier;
            }
        }

        public static float CombinedAttackSpeedMultiplier()
        {
            var m = 1f;
            Apply(GameSave.EquippedRing, ref m);
            Apply(GameSave.EquippedNecklace, ref m);
            return m;

            static void Apply(EquipmentId id, ref float mult)
            {
                var def = Get(id);
                if (def.Id != EquipmentId.None)
                    mult *= def.AttackSpeedMultiplier;
            }
        }

        public static int CombinedBonusMaxHp()
        {
            var hp = 0;
            hp += Get(GameSave.EquippedRing).BonusMaxHp;
            hp += Get(GameSave.EquippedNecklace).BonusMaxHp;
            return hp;
        }
    }
}
