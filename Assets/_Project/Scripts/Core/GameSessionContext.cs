namespace ProjectZx.Core
{
    public enum SurvivalMapKind
    {
        Outside,
        Inside,
        Dungeon
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
    }
}
