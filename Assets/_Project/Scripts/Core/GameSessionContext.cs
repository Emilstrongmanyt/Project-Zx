namespace ProjectZx.Core
{
    public enum SurvivalMapKind
    {
        Outside,
        Inside,
        Dungeon,
        /// <summary>Post-Dungeon crypt run: rounds 1–50 ending on the Minotaur.</summary>
        Crypt,
        /// <summary>Post-Crypt endless run: rounds 1–100 with shifting biomes.</summary>
        Unlimited
    }

    /// <summary>
    /// Player-facing map names (Second War lore). Enum values stay stable for saves.
    /// Outside→Emberwilds, Inside→Warded Halls, Dungeon→Ironvault,
    /// Crypt→Silent Ossuary, Unlimited→The Endless Front.
    /// </summary>
    public static class SurvivalMapNames
    {
        public static string DisplayName(SurvivalMapKind kind) => kind switch
        {
            SurvivalMapKind.Inside => "Warded Halls",
            SurvivalMapKind.Dungeon => "Ironvault",
            SurvivalMapKind.Crypt => "Silent Ossuary",
            SurvivalMapKind.Unlimited => "The Endless Front",
            _ => "Emberwilds"
        };

        /// <summary>Short label for HUD round chip / compact UI.</summary>
        public static string ShortName(SurvivalMapKind kind) => kind switch
        {
            SurvivalMapKind.Inside => "Warded Halls",
            SurvivalMapKind.Dungeon => "Ironvault",
            SurvivalMapKind.Crypt => "Ossuary",
            SurvivalMapKind.Unlimited => "Endless Front",
            _ => "Emberwilds"
        };

        public static string SurvivalButtonLabel(SurvivalMapKind kind) =>
            $"{DisplayName(kind)} Survival";
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
        /// <summary>Run Defense talent additive DR (0–0.4).</summary>
        public float RunDamageTakenReduction;
        /// <summary>Run Block talent chance (0–0.5).</summary>
        public float RunBlockChance;
        /// <summary>Bowman Multishot dual-arrow chance (0–0.99).</summary>
        public float RunMultishotChance;
        /// <summary>Bowman Pierce talent extra hits (0–3).</summary>
        public int RunPierceBonus;
        /// <summary>How many Second Wind charges have been consumed this run.</summary>
        public int SecondWindChargesUsed;
        /// <summary>Legacy bool for old snapshots; prefer SecondWindChargesUsed.</summary>
        public bool SecondWindUsed;
        /// <summary>Bitmask of owned <see cref="EpicTalentId"/> values for this run.</summary>
        public int EpicOwnedMask;
        public int PendingEpicChoices;
        public int EpicPicksTaken;
        public bool PhoenixHeartUsed;
        public bool RunIronVeil;
        public float IronVeilAbsorb;
        public float IronVeilCooldown;
        public float RunDamageTakenMultiplier;
        public float RunEpicBossDamageBonus;
        public float RunEpicNormalDamageBonus;
        public float RunExecutionEdgeBonus;
        public bool RunArcaneEcho;
        public bool RunBloodletting;
        public bool RunPhoenixHeart;
        public float InvulnTimer;
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
        /// Optional next map after a stage-clear win recap ("Enter Next Map").
        /// Cleared when the player picks Camp or starts any survival run from hub.
        /// </summary>
        public static bool HasPendingNextMap;
        public static SurvivalMapKind PendingNextMap;

        public static void ClearPendingNextMap()
        {
            HasPendingNextMap = false;
            PendingNextMap = SurvivalMapKind.Outside;
        }

        public static void SetPendingNextMap(SurvivalMapKind map)
        {
            HasPendingNextMap = true;
            PendingNextMap = map;
        }

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
