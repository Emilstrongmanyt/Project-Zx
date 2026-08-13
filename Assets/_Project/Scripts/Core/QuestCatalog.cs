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
            "Grand Wizard's Peril",
            "Help me brave clanker! I have lost my most special Twin Lightning Pendant my great great grandfather gave to me after the second war. The foul golem beast came through the camp and ran off with it! Please hurry, I cannot cast any spells without it!",
            "The foul golem still has my Twin Lightning Pendant. Defeat the Outside Survival round 20 boss and bring it back!",
            "You found my Twin Lightning Pendant! My spells return — take this gold, brave clanker!",
            "Thank you again for returning my pendant. The camp is safer with heroes like you.",
            "In progress  ·  Retrieve the pendant from Outside R20",
            800,
            () => true);

        public static readonly QuestDefinition GreyWizardsCrow = new(
            QuestId.GreyWizardsCrow,
            "Grey Wizard's Crow",
            "My colleague the Grey Wizard used a crow transformation to spy on the enemy base inside the caverns. He never returned. Please enter Inside Survival, and after round 10 find the dark crow and free him — he will know the way home.",
            "Search Inside Survival after round 10. Tap the dark crow when you are near it to break the spell.",
            "You freed him! The Grey Wizard is back at camp. Take this gold for your courage!",
            "The Grey Wizard rests nearby. Your rescue may yet turn the war.",
            "In progress  ·  Free the crow in Inside Survival (after R10)",
            1000,
            () => GameSave.InsideMapUnlocked);

        public static readonly QuestDefinition KnightsBestFriend = new(
            QuestId.KnightsBestFriend,
            "A Knight's Best Friend",
            "I fled the Dungeon without my greatsword — a knight's best friend. Recover it from the Dungeon Survival final boss and I will share the wizard's fire weapon enchantment, and 1000 gold.",
            "The Dungeon Survival final boss still has my greatsword. Defeat round 40 and bring the blade home.",
            "My greatsword! Take this gold — and the Flame Enchant. May your weapons burn true.",
            "My blade is whole again. Fight well, clanker.",
            "In progress  ·  Recover the greatsword from Dungeon R40 boss",
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

        /// <summary>
        /// Prefer turn-in, then active, then available so a ready crow reward is not
        /// hidden behind another in-progress quest.
        /// </summary>
        public static bool TryGetPrimaryOpenQuest(out QuestDefinition def, out QuestProgress progress)
        {
            if (TryFindByProgress(QuestProgress.ReadyToTurnIn, out def, out progress))
                return true;
            if (TryFindByProgress(QuestProgress.Active, out def, out progress))
                return true;
            if (TryFindByProgress(QuestProgress.Available, out def, out progress))
                return true;

            // Fall back to a completed starter quest so the wizard still greets the player.
            if (TryGet(QuestId.GrandWizardsPeril, out def))
            {
                progress = GetProgress(def.Id);
                return true;
            }

            def = default;
            progress = QuestProgress.Locked;
            return false;
        }

        static bool TryFindByProgress(
            QuestProgress wanted, out QuestDefinition def, out QuestProgress progress)
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

        /// <summary>No talking portrait asset for the knight yet — hide the portrait frame.</summary>
        public static bool UsesQuestPortrait(QuestId id) => id != QuestId.KnightsBestFriend;
    }
}
