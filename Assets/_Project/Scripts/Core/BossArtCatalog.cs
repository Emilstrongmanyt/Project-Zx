using ProjectZx.Enemies;

namespace ProjectZx.Core
{
    /// <summary>
    /// Decade / mid-boss art rotation from Rogue Adventure packs (Resources/RogueBosses).
    /// Stage quest bosses (Outside R20, Dungeon R40, Crypt R50) keep their dedicated sets.
    /// </summary>
    public static class BossArtCatalog
    {
        public enum RogueBossId
        {
            AncientBear = 1,
            Osiris = 2,
            ChompBug = 3,
            ToxicVermin = 5,
            Werewolf = 6,
            Molarbeast = 7,
            Abysslime = 8,
            TitanGuard = 9,
            PumpkinJack = 10
        }

        /// <summary>
        /// Returns true when this boss should use a Rogue Adventure pack instead of golem/lord/minotaur.
        /// </summary>
        public static bool TryGetDecadeBossSet(
            SurvivalMapKind map,
            int round,
            bool isRoundTwentyBoss,
            bool isRoundThirtyBoss,
            bool isRoundFortyBoss,
            bool isRoundFiftyBoss,
            out MonsterAnimSet set)
        {
            set = default;

            // Preserve quest / stage identities.
            if (isRoundTwentyBoss || isRoundFortyBoss || isRoundFiftyBoss)
                return false;

            RogueBossId? id = null;

            if (isRoundThirtyBoss)
                id = RogueBossId.TitanGuard;
            else if (round > 0 && round % 10 == 0)
                id = ResolveDecadeBoss(map, round);

            if (id == null) return false;

            set = ArtLibrary.GetRogueBossAnimSet(id.Value);
            return set.IsValid;
        }

        static RogueBossId ResolveDecadeBoss(SurvivalMapKind map, int round)
        {
            return map switch
            {
                SurvivalMapKind.Outside => round switch
                {
                    10 => RogueBossId.Werewolf,
                    _ => RogueBossId.AncientBear
                },
                SurvivalMapKind.Inside => round switch
                {
                    10 => RogueBossId.ChompBug,
                    20 => RogueBossId.ToxicVermin,
                    _ => RogueBossId.ChompBug
                },
                SurvivalMapKind.Dungeon => round switch
                {
                    10 => RogueBossId.Molarbeast,
                    20 => RogueBossId.Abysslime,
                    30 => RogueBossId.PumpkinJack,
                    _ => RogueBossId.Molarbeast
                },
                SurvivalMapKind.Crypt => round switch
                {
                    10 => RogueBossId.Osiris,
                    20 => RogueBossId.AncientBear,
                    30 => RogueBossId.Werewolf,
                    40 => RogueBossId.TitanGuard,
                    _ => RogueBossId.Abysslime
                },
                SurvivalMapKind.Unlimited => (round / 10 % 5) switch
                {
                    0 => RogueBossId.Werewolf,
                    1 => RogueBossId.Molarbeast,
                    2 => RogueBossId.Abysslime,
                    3 => RogueBossId.TitanGuard,
                    _ => RogueBossId.PumpkinJack
                },
                _ => RogueBossId.Werewolf
            };
        }
    }
}
