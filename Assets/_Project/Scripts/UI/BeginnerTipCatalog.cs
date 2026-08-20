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
            new("shop", "Hint: Talk to Mira the Outfitter (left of the fire) for permanent shop upgrades."),
            new("map_knight", "Hint: Talk to Captain Bren (right of the fire) to start Emberwilds Survival."),
            new("quest_wizard", "Hint: Archmage Thalor offers quests — gold and unique rewards."),
            new("whirlwind", "Hint: Whirlwind is a very powerful upgrade — consider purchasing it first!"),
            new("joystick", "Hint: You can move the joystick in Settings."),
            new("water", "Hint: Heroes may get stuck in water — use Retreat → Unstuck once per run."),
            new("achievements", "Hint: Earning achievements rewards Gold and extra XP (+5% each)."),
            new("rowzi_assist", "Hint: After you meet RowZi, she joins runs and copies your loadout!"),
            new("inside_unlock", "Hint: Clear Emberwilds R20, talk to RowZi, then enter the door for Warded Halls."),
            new("bank_gold", "Hint: Run gold banks when you die, retreat, or take a stage portal home."),
            new("retreat", "Hint: Retreat anytime to bank gold and return to camp safely."),
            new("equipment", "Hint: Find gear in runs, then equip rings / necklaces / capes at the treasure chest."),
            new("pendant_r10", "Hint: Thalor's pendant drops from the Emberwilds R10 boss — turn it in, then clear R20."),
            new("corvin", "Hint: After Warded Halls R10, free the dark crow — then turn in with Corvin at camp."),
            new("aldric", "Hint: Help Sir Aldric leave Ironvault, then recover his greatsword from R40."),
            new("lyra", "Hint: When Silent Ossuary unlocks, Sister Lyra offers Lyra's Vigil — clear the R50 Minotaur."),
            new("bren_watch", "Hint: After The Endless Front unlocks, Captain Bren offers Bren's Watch — survive to round 50."),
        };

        public static int Count => All.Length;

        public static Tip Get(int index) => All[index % All.Length];

        public static bool IsEligible(string id)
        {
            return id switch
            {
                "whirlwind" => !GameSave.WhirlwindUnlocked,
                "rowzi_assist" => GameSave.RowZiUnlocked,
                "quest_wizard" => !GameSave.QuestGrandWizardsPerilCompleted,
                "inside_unlock" => !GameSave.InsideMapUnlocked,
                "pendant_r10" => GameSave.QuestGrandWizardsPerilAccepted
                    && !GameSave.QuestGrandWizardsPerilCompleted,
                "map_knight" => GameSave.HighestRoundReached < 3,
                "shop" => GameSave.HpUpgradeLevel + GameSave.DamageUpgradeLevel < 2,
                "bank_gold" => GameSave.LifetimeDeaths + GameSave.HighestRoundReached < 5,
                "retreat" => GameSave.HighestRoundReached < 8,
                "equipment" => GameSave.HighestRoundReached >= 5,
                "joystick" => !GameSave.HasOpenedSettings,
                "corvin" => GameSave.InsideMapUnlocked && !GameSave.QuestGreyWizardCompleted,
                "aldric" => GameSave.DungeonMapUnlocked && !GameSave.QuestKnightsBestFriendCompleted,
                "lyra" => GameSave.CryptMapUnlocked && !GameSave.QuestLyraVigilCompleted,
                "bren_watch" => GameSave.UnlimitedMapUnlocked && !GameSave.QuestBrensWatchCompleted,
                _ => true
            };
        }
    }
}
