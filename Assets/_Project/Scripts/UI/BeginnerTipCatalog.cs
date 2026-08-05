using ProjectZx.Core;

namespace ProjectZx.UI
{
    /// <summary>Camp beginner tips — short rotating hints for new players.</summary>
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
            new("whirlwind", "Hint: Whirlwind is a very powerful upgrade — consider purchasing it first!"),
            new("joystick", "Hint: You can move the joystick in Settings."),
            new("water", "Hint: Robots may get stuck in water — be careful."),
            new("shop", "Hint: Visit the Shop to purchase upgrades!"),
            new("achievements", "Hint: Earning achievements rewards Gold and extra XP (+5% each)."),
            new("dual_builds", "Hint: You can customize both RollZy and RowZi's builds!"),
        };

        public static int Count => All.Length;

        public static Tip Get(int index) => All[index % All.Length];

        public static bool IsEligible(string id)
        {
            return id switch
            {
                "whirlwind" => !GameSave.WhirlwindUnlocked,
                "dual_builds" => GameSave.RowZiUnlocked,
                _ => true
            };
        }
    }
}
