namespace ProjectZx.Core
{
    public static class StatCaps
    {
        public const int PermanentMaxHp = 600;
        // 20% lower than original 2.0 cap.
        public const float PermanentMaxSpeedMultiplier = 1.6f;
        public const float PermanentMaxDamageMultiplier = 3f;

        public const int MaxRunLevel = 100;

        public const int RunMaxHp = PermanentMaxHp * 2;
        public const float RunMaxSpeedMultiplier = PermanentMaxSpeedMultiplier * 2f;
        public const float RunMaxDamageMultiplier = PermanentMaxDamageMultiplier * 2f;
    }
}