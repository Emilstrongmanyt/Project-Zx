namespace ProjectZx.Core
{
    public enum SurvivalMapKind
    {
        Outside,
        Inside,
        Dungeon,
        /// <summary>Post-Dungeon endless run: rounds 1–100 with shifting biomes.</summary>
        Unlimited
    }

    public struct SurvivalRunSnapshot
    {
        public bool HasData;
        public int MaxHp;
        public int CurrentHp;
        public int RunXp;
        public int RunGold;
        public int Level;
        public int XpToNext;
        public int PendingLevelUpChoices;
        public float RunSpeedMultiplier;
        public float RunDamageMultiplier;
        public float RunAttackSpeedMultiplier;
        public float RunAttackRangeMultiplier;
        public float RunLootRangeMultiplier;
        public float RunCritChance;
        public float RunCritMultiplier;
        public float RunLifesteal;
        public float RunBossDamageBonus;
        public float RunExecuteBonus;
        public float RunGoldFindMultiplier;
        public float RunXpMultiplier;
        public float RunRegenPerSecond;
        public bool RunShieldUnlocked;
        public float RunBerserkBonus;
        /// <summary>How many Second Wind charges have been consumed this run.</summary>
        public int SecondWindChargesUsed;
        /// <summary>Legacy bool for old snapshots; prefer SecondWindChargesUsed.</summary>
        public bool SecondWindUsed;
    }

    public static class GameSessionContext
    {
        public static SurvivalMapKind SurvivalMap { get; set; } = SurvivalMapKind.Outside;
        public static PlayerClass SelectedClass { get; set; } = PlayerClass.Batter;
        public static PlayableHero SelectedHero { get; set; } = PlayableHero.RollZy;
        public static bool FreshSurvivalRun { get; set; } = true;
        /// <summary>
        /// When starting a fresh run, RunLoop increments from this value (0 → round 1).
        /// All map transitions (door / gateway / hub select) start fresh at 0.
        /// </summary>
        public static int StartingRound { get; set; }
        public static int CarryRound { get; set; }
        public static SurvivalRunSnapshot RunSnapshot;

        /// <summary>
        /// Visual / enemy biome for Unlimited mode by round.
        /// R1–20 Outside, R21–50 Inside, R51–100 Dungeon.
        /// </summary>
        public static SurvivalMapKind GetUnlimitedBiome(int round)
        {
            if (round <= 20) return SurvivalMapKind.Outside;
            if (round <= 50) return SurvivalMapKind.Inside;
            return SurvivalMapKind.Dungeon;
        }

        public static SurvivalMapKind GetVisualBiome(SurvivalMapKind mapKind, int round) =>
            mapKind == SurvivalMapKind.Unlimited ? GetUnlimitedBiome(round) : mapKind;
    }
}
