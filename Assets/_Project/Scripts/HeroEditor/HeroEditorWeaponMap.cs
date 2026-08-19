using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.HeroEditor
{
    /// <summary>
    /// Maps Project Zx class + weapon tier to HeroEditor SpriteCollection weapon Ids.
    /// Combat math stays in WeaponCatalog; this only swaps held art / anim part.
    /// </summary>
    public static class HeroEditorWeaponMap
    {
        public readonly struct WeaponVisual
        {
            public readonly string SpriteId;
            /// <summary>True = Bow part; false = melee 1H or 2H.</summary>
            public readonly bool IsBow;
            /// <summary>True = MeleeWeapon2H (spear/polearm); ignored when IsBow.</summary>
            public readonly bool IsTwoHanded;
            /// <summary>Optional paint tint for [Paint] weapons.</summary>
            public readonly Color? Paint;

            public WeaponVisual(string spriteId, bool isBow, bool isTwoHanded, Color? paint = null)
            {
                SpriteId = spriteId;
                IsBow = isBow;
                IsTwoHanded = isTwoHanded;
                Paint = paint;
            }
        }

        public static WeaponVisual GetVisual(PlayerClass playerClass, WeaponMaterialTier tier)
        {
            var paint = PaintForTier(tier);
            return playerClass switch
            {
                PlayerClass.Spearman => new WeaponVisual(SpearId(tier), false, true, paint),
                PlayerClass.Bowman => new WeaponVisual(BowId(tier), true, false, null),
                PlayerClass.Magician => new WeaponVisual(WandId(tier), false, false, paint),
                PlayerClass.Samurai => new WeaponVisual(KatanaId(tier), false, false, paint),
                _ => new WeaponVisual(BatSwordId(tier), false, false, paint)
            };
        }

        static string BatSwordId(WeaponMaterialTier tier) => tier switch
        {
            <= WeaponMaterialTier.Wooden => "FantasyHeroes.Basic.MeleeWeapon1H.TrainingSword [Paint]",
            <= WeaponMaterialTier.Copper => "FantasyHeroes.Basic.MeleeWeapon1H.FamilySword",
            <= WeaponMaterialTier.Gold => "FantasyHeroes.Basic.MeleeWeapon1H.KnightSword [Paint]",
            <= WeaponMaterialTier.Adamantine => "FantasyHeroes.Basic.MeleeWeapon1H.GuardSword1 [Paint]",
            _ => "FantasyHeroes.Knights.MeleeWeapon1H.BalancedSword [Paint]"
        };

        static string SpearId(WeaponMaterialTier tier) => tier switch
        {
            <= WeaponMaterialTier.Iron => "FantasyHeroes.Basic.MeleeWeapon2H.Spear",
            <= WeaponMaterialTier.Silver => "FantasyHeroes.Basic.MeleeWeapon2H.CataphractSpear",
            <= WeaponMaterialTier.Platinum => "FantasyHeroes.Basic.MeleeWeapon2H.SiegeSpear",
            _ => "FantasyHeroes.Basic.MeleeWeapon2H.Halberd"
        };

        static string KatanaId(WeaponMaterialTier tier) => tier switch
        {
            <= WeaponMaterialTier.Iron => "FantasyHeroes.Basic.MeleeWeapon1H.ShortIronKatana [Paint]",
            <= WeaponMaterialTier.Gold => "FantasyHeroes.Samurai.MeleeWeapon1H.Katana1 [Paint]",
            <= WeaponMaterialTier.Platinum => "FantasyHeroes.Samurai.MeleeWeapon1H.Katana2 [Paint]",
            _ => "FantasyHeroes.Samurai.MeleeWeapon1H.Katana3 [Paint]"
        };

        static string BowId(WeaponMaterialTier tier) => tier switch
        {
            <= WeaponMaterialTier.Wooden => "FantasyHeroes.Basic.Bow.HunterShortBow",
            <= WeaponMaterialTier.Copper => "FantasyHeroes.Basic.Bow.HunterBow",
            <= WeaponMaterialTier.Gold => "FantasyHeroes.Basic.Bow.RangerBow",
            <= WeaponMaterialTier.Platinum => "FantasyHeroes.Basic.Bow.ScoutBow",
            _ => "FantasyHeroes.Basic.Bow.BattleBow"
        };

        static string WandId(WeaponMaterialTier tier) => tier switch
        {
            <= WeaponMaterialTier.Wooden => "FantasyHeroes.Basic.MeleeWeapon1H.MagicWandTypeA",
            <= WeaponMaterialTier.Copper => "FantasyHeroes.Basic.MeleeWeapon1H.MagicWandTypeB",
            <= WeaponMaterialTier.Gold => "FantasyHeroes.Basic.MeleeWeapon1H.MagicWandTypeC",
            <= WeaponMaterialTier.Platinum => "FantasyHeroes.Basic.MeleeWeapon1H.BishopWand",
            _ => "FantasyHeroes.Basic.MeleeWeapon1H.WarlockWand"
        };

        static Color? PaintForTier(WeaponMaterialTier tier) => tier switch
        {
            WeaponMaterialTier.Wooden => new Color(0.72f, 0.55f, 0.32f),
            WeaponMaterialTier.Iron => new Color(0.75f, 0.78f, 0.82f),
            WeaponMaterialTier.Steel => new Color(0.85f, 0.88f, 0.92f),
            WeaponMaterialTier.Copper => new Color(0.85f, 0.5f, 0.28f),
            WeaponMaterialTier.Silver => new Color(0.9f, 0.92f, 0.95f),
            WeaponMaterialTier.Gold => new Color(1f, 0.84f, 0.2f),
            WeaponMaterialTier.Cobalt => new Color(0.35f, 0.55f, 0.95f),
            WeaponMaterialTier.Platinum => new Color(0.92f, 0.95f, 1f),
            WeaponMaterialTier.Adamantine => new Color(0.55f, 0.85f, 0.95f),
            WeaponMaterialTier.Crimson => new Color(0.85f, 0.15f, 0.2f),
            WeaponMaterialTier.Fateful => new Color(0.7f, 0.35f, 1f),
            _ => (Color?)null
        };
    }
}
