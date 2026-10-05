namespace ProjectZx.Core
{
    public static class StatCaps
    {
        public const int BasePermanentMaxHp = 600;
        /// <summary>Extra shop HP ranks unlocked by clearing Ironvault (12 × +15 HP).</summary>
        public const int IronvaultBonusMaxHp = 180;
        /// <summary>Extra permanent damage ranks after Ironvault (+1.00, about 12 shop ranks).</summary>
        public const float IronvaultBonusDamageMultiplier = 1f;
        /// <summary>Extra permanent attack-range ranks after Ironvault (+0.50, 10 shop ranks).</summary>
        public const float IronvaultBonusAttackRangeMultiplier = 0.5f;

        public static int PermanentMaxHp =>
            IronvaultShopBand ? BasePermanentMaxHp + IronvaultBonusMaxHp : BasePermanentMaxHp;

        /// <summary>True once Ironvault is cleared, including players who have already moved on.</summary>
        public static bool IronvaultShopBand =>
            GameSave.DungeonSurvivalCleared || GameSave.CryptSurvivalCleared;

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

        public static float PermanentMaxDamageMultiplier
        {
            get
            {
                var cap = ProgressionTier switch
                {
                    3 => CryptPermanentMaxDamageMultiplier,
                    2 => DungeonPermanentMaxDamageMultiplier,
                    1 => InsidePermanentMaxDamageMultiplier,
                    _ => BasePermanentMaxDamageMultiplier
                };
                if (IronvaultShopBand)
                    cap += IronvaultBonusDamageMultiplier;
                return cap;
            }
        }

        public static float PermanentMaxAttackRangeMultiplier
        {
            get
            {
                var cap = ProgressionTier switch
                {
                    3 => CryptPermanentMaxAttackRangeMultiplier,
                    2 => DungeonPermanentMaxAttackRangeMultiplier,
                    1 => InsidePermanentMaxAttackRangeMultiplier,
                    _ => BasePermanentMaxAttackRangeMultiplier
                };
                if (IronvaultShopBand)
                    cap += IronvaultBonusAttackRangeMultiplier;
                return cap;
            }
        }

        public static int RunMaxHp => PermanentMaxHp * 2;
        public static float RunMaxSpeedMultiplier => PermanentMaxSpeedMultiplier * 2f;
        public static float RunMaxDamageMultiplier => PermanentMaxDamageMultiplier * 2f;
        public static float RunMaxAttackRangeMultiplier => PermanentMaxAttackRangeMultiplier * 2f;
    }
}
