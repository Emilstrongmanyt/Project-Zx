using ProjectZx.Core;

namespace ProjectZx.HeroEditor
{
    /// <summary>
    /// Maps Project Zx equipment ids to HeroEditor SpriteCollection entry ids
    /// for equipping cape/helm visuals on the FantasyHeroes Character.
    /// Save enum values stay stable; display names/icons use these pieces.
    /// </summary>
    public static class HeroEditorEquipmentMap
    {
        /// <summary>SpriteCollection cape id for an equipped cape, or null if unequipped/unknown.</summary>
        public static string GetCapeSpriteId(EquipmentId id) => id switch
        {
            EquipmentId.WoolCape => "FantasyHeroes.Basic.Cape.CotttonCape [Paint]",
            EquipmentId.SentinelCape => "FantasyHeroes.Basic.Cape.HeroicCape [Paint]",
            EquipmentId.IronweaveCape => "FantasyHeroes.Basic.Cape.RoyalCape [Paint]",
            EquipmentId.GuardianCape => "FantasyHeroes.Basic.Cape.HeroicCape [Paint]",
            _ => null
        };

        /// <summary>SpriteCollection helmet id for an equipped helm, or null if unequipped/unknown.</summary>
        public static string GetHelmetSpriteId(EquipmentId id) => id switch
        {
            EquipmentId.LeatherHelm => "FantasyHeroes.Basic.Helmet.LeatherHelm",
            EquipmentId.GuardHelm => "FantasyHeroes.Knights.Helmet.GuardHelm [Paint]",
            EquipmentId.IronHelm => "FantasyHeroes.Knights.Helmet.FalconIronHelm",
            EquipmentId.OutriderHelm => "FantasyHeroes.Basic.Helmet.LeatherHelm",
            EquipmentId.BastionHelm => "FantasyHeroes.Knights.Helmet.GuardHelm [Paint]",
            _ => null
        };
    }
}
