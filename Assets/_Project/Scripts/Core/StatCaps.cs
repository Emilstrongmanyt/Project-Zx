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
        public const int CryptMaxRound = 50;
        public const int UnlimitedMaxRound = 100;

        /// <summary>
        /// 0 = default, 1 = Inside survival cleared (gateway entered),
        /// 2 = Dungeon survival cleared (Crypt portal entered),
        /// 3 = Crypt survival cleared (victory gate entered).
        /// </summary>
        public static int ProgressionTier
        {
            get
            {
                if (GameSave.CryptSurvivalCleared) return 3;
                if (GameSave.DungeonSurvivalCleared) return 2;
                if (GameSave.InsideSurvivalCleared) return 1;
                return 0;
            }
        }

        public const float CryptPermanentMaxSpeedMultiplier = 2.75f;
        public const float CryptPermanentMaxDamageMultiplier = 5.5f;
        public const float CryptPermanentMaxAttackRangeMultiplier = 2.75f;

        public static float PermanentMaxSpeedMultiplier => ProgressionTier switch
        {
            3 => CryptPermanentMaxSpeedMultiplier,
            2 => DungeonPermanentMaxSpeedMultiplier,
            1 => InsidePermanentMaxSpeedMultiplier,
            _ => BasePermanentMaxSpeedMultiplier
        };

        public static float PermanentMaxDamageMultiplier => ProgressionTier switch
        {
            3 => CryptPermanentMaxDamageMultiplier,
            2 => DungeonPermanentMaxDamageMultiplier,
            1 => InsidePermanentMaxDamageMultiplier,
            _ => BasePermanentMaxDamageMultiplier
        };

        public static float PermanentMaxAttackRangeMultiplier => ProgressionTier switch
        {
            3 => CryptPermanentMaxAttackRangeMultiplier,
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
