namespace ProjectZx.Core
{
    public static class StatCaps
    {
        public const int PermanentMaxHp = 600;

        /// <summary>Base permanent caps (before Inside / Dungeon clear progression).</summary>
        public const float BasePermanentMaxSpeedMultiplier = 1.6f;
        public const float BasePermanentMaxDamageMultiplier = 3f;
        public const float BasePermanentMaxAttackRangeMultiplier = 1.6f;

        public const float InsidePermanentMaxSpeedMultiplier = 2f;
        public const float InsidePermanentMaxDamageMultiplier = 4f;
        public const float InsidePermanentMaxAttackRangeMultiplier = 2f;

        public const float DungeonPermanentMaxSpeedMultiplier = 2.5f;
        public const float DungeonPermanentMaxDamageMultiplier = 5f;
        public const float DungeonPermanentMaxAttackRangeMultiplier = 2.5f;

        public const int MaxRunLevel = 100;
        public const int UnlimitedMaxRound = 100;

        /// <summary>
        /// 0 = default, 1 = Inside survival cleared (gateway entered),
        /// 2 = Dungeon survival cleared (victory gate entered).
        /// </summary>
        public static int ProgressionTier
        {
            get
            {
                if (GameSave.DungeonSurvivalCleared) return 2;
                if (GameSave.InsideSurvivalCleared) return 1;
                return 0;
            }
        }

        public static float PermanentMaxSpeedMultiplier => ProgressionTier switch
        {
            2 => DungeonPermanentMaxSpeedMultiplier,
            1 => InsidePermanentMaxSpeedMultiplier,
            _ => BasePermanentMaxSpeedMultiplier
        };

        public static float PermanentMaxDamageMultiplier => ProgressionTier switch
        {
            2 => DungeonPermanentMaxDamageMultiplier,
            1 => InsidePermanentMaxDamageMultiplier,
            _ => BasePermanentMaxDamageMultiplier
        };

        public static float PermanentMaxAttackRangeMultiplier => ProgressionTier switch
        {
            2 => DungeonPermanentMaxAttackRangeMultiplier,
            1 => InsidePermanentMaxAttackRangeMultiplier,
            _ => BasePermanentMaxAttackRangeMultiplier
        };

        public static int RunMaxHp => PermanentMaxHp * 2;
        public static float RunMaxSpeedMultiplier => PermanentMaxSpeedMultiplier * 2f;
        public static float RunMaxDamageMultiplier => PermanentMaxDamageMultiplier * 2f;
        public static float RunMaxAttackRangeMultiplier => PermanentMaxAttackRangeMultiplier * 2f;
    }
}
