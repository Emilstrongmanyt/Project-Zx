using ProjectZx.Core;

namespace ProjectZx.UI
{
    /// <summary>Camp beginner tips — short rotating hints gated by progression.</summary>
    public static class BeginnerTipCatalog
    {
        public readonly struct Tip
        {
            public readonly string Id;
            public readonly string Text;

            public Tip(string id, string text)
            {
                Id = id;
                Text = text;
            }
        }

        static readonly Tip[] All =
        {
            new("shop", "Hint: Talk to the Wizard (left of the fire) for permanent shop upgrades."),
            new("map_knight", "Hint: Talk to the Knight (right of the fire) to start Outside Survival."),
            new("quest_wizard", "Hint: The Grand Wizard offers quests — gold and unique rewards."),
            new("whirlwind", "Hint: Whirlwind is a very powerful upgrade — consider purchasing it first!"),
            new("joystick", "Hint: You can move the joystick in Settings."),
            new("water", "Hint: Robots may get stuck in water — use Retreat → Unstuck once per run."),
            new("achievements", "Hint: Earning achievements rewards Gold and extra XP (+5% each)."),
            new("dual_builds", "Hint: You can customize both RollZy and RowZi's builds!"),
            new("inside_unlock", "Hint: Clear Outside round 20, talk to RowZi, then enter the door for Inside Survival."),
            new("bank_gold", "Hint: Run gold banks when you die, retreat, or take a stage portal home."),
            new("retreat", "Hint: Retreat anytime to bank gold and return to camp safely."),
            new("equipment", "Hint: Find gear in runs, then equip rings / necklaces / capes at the treasure chest."),
        };

        public static int Count => All.Length;

        public static Tip Get(int index) => All[index % All.Length];

        public static bool IsEligible(string id)
        {
            return id switch
            {
                "whirlwind" => !GameSave.WhirlwindUnlocked,
                "dual_builds" => GameSave.RowZiUnlocked,
                "quest_wizard" => !GameSave.QuestGrandWizardsPerilCompleted,
                "inside_unlock" => !GameSave.InsideMapUnlocked,
                "map_knight" => GameSave.HighestRoundReached < 3,
                "shop" => GameSave.HpUpgradeLevel + GameSave.DamageUpgradeLevel < 2,
                "bank_gold" => GameSave.LifetimeDeaths + GameSave.HighestRoundReached < 5,
                "retreat" => GameSave.HighestRoundReached < 8,
                "equipment" => GameSave.HighestRoundReached >= 5,
                "joystick" => !GameSave.HasOpenedSettings,
                _ => true
            };
        }
    }
}
