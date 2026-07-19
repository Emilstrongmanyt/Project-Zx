namespace ProjectZx.Core
{
    public enum AttackMode
    {
        Standard = 0,
        Whirlwind = 1,
        PiercingShot = 2
    }

    public static class AttackModeCatalog
    {
        public static bool IsUnlocked(AttackMode mode)
        {
            return mode switch
            {
                AttackMode.Standard => true,
                AttackMode.Whirlwind => GameSave.WhirlwindUnlocked,
                AttackMode.PiercingShot => GameSave.PiercingShotUnlocked,
                _ => false
            };
        }

        public static bool IsAvailableForClass(PlayerClass playerClass, AttackMode mode)
        {
            if (mode == AttackMode.Standard) return true;
            if (mode == AttackMode.Whirlwind)
                return playerClass == PlayerClass.Batter
                       || playerClass == PlayerClass.Spearman
                       || playerClass == PlayerClass.Samurai;
            if (mode == AttackMode.PiercingShot)
                return playerClass == PlayerClass.Bowman;
            return false;
        }

        public static AttackMode GetSpecialModeForClass(PlayerClass playerClass)
        {
            return playerClass switch
            {
                PlayerClass.Bowman => AttackMode.PiercingShot,
                PlayerClass.Batter or PlayerClass.Spearman or PlayerClass.Samurai => AttackMode.Whirlwind,
                _ => AttackMode.Standard
            };
        }

        public static string GetLabel(AttackMode mode, PlayerClass playerClass)
        {
            return mode switch
            {
                AttackMode.Whirlwind when playerClass == PlayerClass.Samurai => "Triple Slash",
                AttackMode.Whirlwind => "Whirlwind (360°)",
                AttackMode.PiercingShot => "Piercing Shot",
                _ => "Standard"
            };
        }

        public static string GetDescription(PlayerClass playerClass, AttackMode mode)
        {
            return mode switch
            {
                AttackMode.Whirlwind when playerClass == PlayerClass.Samurai =>
                    "Three 180° katana swipes per attack — buy Whirlwind in shop, then equip here.",
                AttackMode.Whirlwind =>
                    "Full 360° spin cleave — buy in shop, then equip here.",
                AttackMode.PiercingShot =>
                    "Primary hit plus 50% damage to one enemy behind.",
                AttackMode.Standard when playerClass == PlayerClass.Bowman =>
                    "Single focused arrow shot.",
                AttackMode.Standard when playerClass == PlayerClass.Spearman =>
                    "Spear swing — hits all enemies in a 180° arc.",
                AttackMode.Standard when playerClass == PlayerClass.Samurai =>
                    "Double katana swipe — two 180° arc hits per attack.",
                AttackMode.Standard when playerClass == PlayerClass.Magician =>
                    "Single splash spell (coming soon).",
                _ => "Single-target bat swing."
            };
        }

        public static string GetLockedHint(AttackMode mode)
        {
            return mode switch
            {
                AttackMode.Whirlwind => $"Buy Whirlwind in shop ({ShopCosts.Whirlwind}g)",
                AttackMode.PiercingShot => $"Buy Piercing Shot in shop ({ShopCosts.PiercingShot}g)",
                _ => string.Empty
            };
        }
    }
}
