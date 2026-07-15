namespace ProjectZx.Core
{
    public enum SurvivalMapKind
    {
        Outside,
        Inside
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
    }

    public static class GameSessionContext
    {
        public static SurvivalMapKind SurvivalMap { get; set; } = SurvivalMapKind.Outside;
        public static bool FreshSurvivalRun { get; set; } = true;
        public static int CarryRound { get; set; }
        public static SurvivalRunSnapshot RunSnapshot;
    }
}