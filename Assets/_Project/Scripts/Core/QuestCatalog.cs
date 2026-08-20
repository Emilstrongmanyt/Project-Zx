using System;
using System.Collections.Generic;

namespace ProjectZx.Core
{
    public enum QuestId
    {
        GrandWizardsPeril = 1,
        GreyWizardsCrow = 2,
        KnightsBestFriend = 3
    }

    public enum QuestProgress
    {
        Locked = 0,
        Available = 1,
        Active = 2,
        ReadyToTurnIn = 3,
        Completed = 4
    }

    public readonly struct QuestDefinition
    {
        public QuestId Id { get; }
        public string Title { get; }
        public string OfferText { get; }
        public string ActiveText { get; }
        public string TurnInText { get; }
        public string CompletedText { get; }
        public string ActiveStatusHint { get; }
        public int GoldReward { get; }
        public Func<bool> IsUnlocked { get; }

        public QuestDefinition(
            QuestId id,
            string title,
            string offerText,
            string activeText,
            string turnInText,
            string completedText,
            string activeStatusHint,
            int goldReward,
            Func<bool> isUnlocked)
        {
            Id = id;
            Title = title;
            OfferText = offerText;
            ActiveText = activeText;
            TurnInText = turnInText;
            CompletedText = completedText;
            ActiveStatusHint = activeStatusHint;
            GoldReward = goldReward;
            IsUnlocked = isUnlocked ?? (() => false);
        }
    }

    /// <summary>
    /// Camp quest definitions and progression helpers.
    /// New quests register here; visibility is gated by <see cref="QuestDefinition.IsUnlocked"/>.
    /// </summary>
    public static class QuestCatalog
    {
        public const string TwinLightningPendantName = "Twin Lightning Pendant";

        public static readonly QuestDefinition GrandWizardsPeril = new(
            QuestId.GrandWizardsPeril,
            "Thalor's Pendant",
            "Adventurer — I am Archmage Thalor. A golem tore through camp and stole my Twin Lightning Pendant, heirloom of the second war. Without it my circle is muted. Hunt the Outside Survival round 20 boss and bring the pendant home!",
            "The Outside R20 golem still clutches my Twin Lightning Pendant. Strike it down and return to me at the campfire.",
            "The pendant sings again! My thanks — and this gold. Camp owes you, hero.",
            "Thalor keeps the circle warm. Speak again if the wilds call you to new work.",
            "In progress  ·  Retrieve Thalor's pendant from Outside R20",
            800,
            () => true);

        public static readonly QuestDefinition GreyWizardsCrow = new(
            QuestId.GreyWizardsCrow,
            "Corvin's Crow",
            "Ashen Seer Corvin flew as a crow to spy on the cavern foe — and never returned. Enter Inside Survival; after round 10 find the dark crow and free him. He knows the path home.",
            "Inside Survival after round 10: tap the dark crow when you are close to break the glamour.",
            "Corvin is flesh again and resting by camp. Gold for your courage — the Ashen Seer will not forget.",
            "Corvin watches the treeline. Your rescue may yet tip the war.",
            "In progress  ·  Free Corvin the crow in Inside Survival (after R10)",
            1000,
            () => GameSave.InsideMapUnlocked);

        public static readonly QuestDefinition KnightsBestFriend = new(
            QuestId.KnightsBestFriend,
            "Aldric's Greatsword",
            "I am Sir Aldric. I fled the Dungeon without my greatsword — a knight's true companion. Recover it from the Dungeon Survival round 40 boss and I will share Flame Enchant, plus 1000 gold.",
            "The Dungeon R40 boss still bears my greatsword. Cut it down and bring the blade to me north of camp.",
            "Steel and honor restored! Take the gold — and Flame Enchant. May your weapons burn true.",
            "Aldric stands ready. Fight well, adventurer.",
            "In progress  ·  Recover Aldric's greatsword from Dungeon R40 boss",
            1000,
            () => GameSave.DungeonKnightReturnedToCamp);

        public const string KnightsGreatswordName = "Knight's Greatsword";

        static readonly QuestDefinition[] AllQuests =
            { GrandWizardsPeril, GreyWizardsCrow, KnightsBestFriend };

        public static IReadOnlyList<QuestDefinition> All => AllQuests;

        public static bool TryGet(QuestId id, out QuestDefinition def)
        {
            for (var i = 0; i < AllQuests.Length; i++)
            {
                if (AllQuests[i].Id != id) continue;
                def = AllQuests[i];
                return true;
            }

            def = default;
            return false;
        }

        public static QuestProgress GetProgress(QuestId id)
        {
            if (!TryGet(id, out var def)) return QuestProgress.Locked;
            if (!def.IsUnlocked()) return QuestProgress.Locked;

            switch (id)
            {
                case QuestId.GrandWizardsPeril:
                    if (GameSave.QuestGrandWizardsPerilCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestGrandWizardsPerilAccepted) return QuestProgress.Available;
                    return GameSave.HasTwinLightningPendant
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.GreyWizardsCrow:
                    if (GameSave.QuestGreyWizardCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestGreyWizardAccepted) return QuestProgress.Available;
                    return GameSave.QuestGreyWizardRescued
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.KnightsBestFriend:
                    if (GameSave.QuestKnightsBestFriendCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestKnightsBestFriendAccepted) return QuestProgress.Available;
                    return GameSave.HasKnightsGreatsword
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                default:
                    return QuestProgress.Locked;
            }
        }

        /// <summary>Quests offered only by the Grand Wizard (Purple Wizard) at camp.</summary>
        public static readonly QuestId[] GrandWizardQuestIds =
        {
            QuestId.GrandWizardsPeril,
            QuestId.GreyWizardsCrow
        };

        /// <summary>Quests offered only by Knight1 at camp.</summary>
        public static readonly QuestId[] KnightQuestIds =
        {
            QuestId.KnightsBestFriend
        };

        /// <summary>
        /// Prefer turn-in, then active, then available so a ready crow reward is not
        /// hidden behind another in-progress quest. Scoped to a giver's quest list.
        /// </summary>
        public static bool TryGetPrimaryOpenQuest(out QuestDefinition def, out QuestProgress progress)
            => TryGetPrimaryOpenQuest(GrandWizardQuestIds, out def, out progress);

        public static bool TryGetPrimaryOpenQuest(
            QuestId[] pool, out QuestDefinition def, out QuestProgress progress)
        {
            if (pool == null || pool.Length == 0)
            {
                def = default;
                progress = QuestProgress.Locked;
                return false;
            }

            if (TryFindByProgress(pool, QuestProgress.ReadyToTurnIn, out def, out progress))
                return true;
            if (TryFindByProgress(pool, QuestProgress.Active, out def, out progress))
                return true;
            if (TryFindByProgress(pool, QuestProgress.Available, out def, out progress))
                return true;

            // All done / locked: prefer the latest completed quest in the pool
            // (e.g. Grey Wizard crow after Grand Wizard peril), not always quest #1.
            for (var i = pool.Length - 1; i >= 0; i--)
            {
                if (!TryGet(pool[i], out var completedDef)) continue;
                var status = GetProgress(completedDef.Id);
                if (status != QuestProgress.Completed) continue;
                def = completedDef;
                progress = status;
                return true;
            }

            if (TryGet(pool[0], out def))
            {
                progress = GetProgress(def.Id);
                return true;
            }

            def = default;
            progress = QuestProgress.Locked;
            return false;
        }

        static bool TryFindByProgress(
            QuestId[] pool,
            QuestProgress wanted,
            out QuestDefinition def,
            out QuestProgress progress)
        {
            for (var i = 0; i < pool.Length; i++)
            {
                if (!TryGet(pool[i], out var candidate)) continue;
                var status = GetProgress(candidate.Id);
                if (status != wanted) continue;
                def = candidate;
                progress = status;
                return true;
            }

            def = default;
            progress = QuestProgress.Locked;
            return false;
        }

        public static bool TryAccept(QuestId id)
        {
            if (GetProgress(id) != QuestProgress.Available) return false;
            switch (id)
            {
                case QuestId.GrandWizardsPeril:
                    GameSave.QuestGrandWizardsPerilAccepted = true;
                    return true;
                case QuestId.GreyWizardsCrow:
                    GameSave.QuestGreyWizardAccepted = true;
                    return true;
                case QuestId.KnightsBestFriend:
                    GameSave.QuestKnightsBestFriendAccepted = true;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryTurnIn(QuestId id, out int goldAwarded)
        {
            goldAwarded = 0;
            if (GetProgress(id) != QuestProgress.ReadyToTurnIn) return false;
            if (!TryGet(id, out var def)) return false;

            switch (id)
            {
                case QuestId.GrandWizardsPeril:
                    if (!GameSave.HasTwinLightningPendant) return false;
                    GameSave.HasTwinLightningPendant = false;
                    GameSave.QuestGrandWizardsPerilCompleted = true;
                    goldAwarded = def.GoldReward;
                    GameSave.Gold += goldAwarded;
                    GameSave.LifetimeGoldEarned += goldAwarded;
                    return true;

                case QuestId.GreyWizardsCrow:
                    if (!GameSave.QuestGreyWizardRescued) return false;
                    GameSave.QuestGreyWizardCompleted = true;
                    goldAwarded = def.GoldReward;
                    GameSave.Gold += goldAwarded;
                    GameSave.LifetimeGoldEarned += goldAwarded;
                    return true;

                case QuestId.KnightsBestFriend:
                    if (!GameSave.HasKnightsGreatsword) return false;
                    GameSave.HasKnightsGreatsword = false;
                    GameSave.QuestKnightsBestFriendCompleted = true;
                    GameSave.FlameEnchantUnlocked = true;
                    goldAwarded = def.GoldReward;
                    GameSave.Gold += goldAwarded;
                    GameSave.LifetimeGoldEarned += goldAwarded;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Outside Survival R20 golem drops the pendant while the quest is active.</summary>
        public static bool ShouldDropTwinLightningPendant(bool isOutsideRoundTwentyBoss)
        {
            if (!isOutsideRoundTwentyBoss) return false;
            if (GetProgress(QuestId.GrandWizardsPeril) != QuestProgress.Active) return false;
            return !GameSave.HasTwinLightningPendant;
        }

        /// <summary>Inside Survival crow after R10 while the rescue quest is active.</summary>
        public static bool ShouldSpawnDarkBird(SurvivalMapKind mapKind, int round)
        {
            if (mapKind != SurvivalMapKind.Inside) return false;
            if (round < 10) return false;
            if (GetProgress(QuestId.GreyWizardsCrow) != QuestProgress.Active) return false;
            return !GameSave.QuestGreyWizardRescued;
        }

        /// <summary>Dungeon Survival R40 boss drops the greatsword while the knight quest is active.</summary>
        public static bool ShouldDropKnightsGreatsword(bool isDungeonRoundFortyBoss)
        {
            if (!isDungeonRoundFortyBoss) return false;
            if (GetProgress(QuestId.KnightsBestFriend) != QuestProgress.Active) return false;
            return !GameSave.HasKnightsGreatsword;
        }

        /// <summary>All quest givers show a GanzSe character (or sprite fallback) in the dialogue frame.</summary>
        public static bool UsesQuestPortrait(QuestId id) => true;

        /// <summary>
        /// Compact in-run / camp objective line for the HUD tracker.
        /// Prefers ready-to-turn-in, then active quests across all givers.
        /// </summary>
        public static string BuildHudObjectiveLine()
        {
            if (TryFindHudQuest(QuestProgress.ReadyToTurnIn, out var readyDef, out _))
                return $"Quest: {readyDef.Title} — ready to turn in!";

            if (TryFindHudQuest(QuestProgress.Active, out var activeDef, out _))
            {
                var hint = string.IsNullOrEmpty(activeDef.ActiveStatusHint)
                    ? activeDef.Title
                    : activeDef.ActiveStatusHint;
                return $"Quest: {hint}";
            }

            return string.Empty;
        }

        public static bool TryGetReadyToTurnInQuest(out QuestDefinition def)
        {
            return TryFindHudQuest(QuestProgress.ReadyToTurnIn, out def, out _);
        }

        public static bool TryGetActiveQuest(out QuestDefinition def)
        {
            return TryFindHudQuest(QuestProgress.Active, out def, out _);
        }

        public static bool HasReadyToTurnInQuest() =>
            TryGetReadyToTurnInQuest(out _);

        static bool TryFindHudQuest(
            QuestProgress wanted,
            out QuestDefinition def,
            out QuestProgress progress)
        {
            for (var i = 0; i < AllQuests.Length; i++)
            {
                var candidate = AllQuests[i];
                var status = GetProgress(candidate.Id);
                if (status != wanted) continue;
                def = candidate;
                progress = status;
                return true;
            }

            def = default;
            progress = QuestProgress.Locked;
            return false;
        }
    }
}
