using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectZx.Core
{
    public enum QuestId
    {
        GrandWizardsPeril = 1,
        GreyWizardsCrow = 2,
        KnightsBestFriend = 3,
        /// <summary>Thalor follow-up: Emberwilds R20 opens the path to Warded Halls.</summary>
        WardensPath = 4,
        /// <summary>Sister Lyra: silence the Silent Ossuary R50 Minotaur.</summary>
        LyraVigil = 5,
        /// <summary>Captain Bren: hold The Endless Front through round 50.</summary>
        BrensWatch = 6
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
        public const string KnightsGreatswordName = "Knight's Greatsword";

        public static readonly QuestDefinition GrandWizardsPeril = new(
            QuestId.GrandWizardsPeril,
            "Thalor's Pendant",
            "Adventurer — I am Archmage Thalor. After the Second War, this campfire is the last free light. A golem tore from the Emberwilds and stole my Twin Lightning Pendant — without it my circle is muted. Hunt the Emberwilds round 10 boss and bring the pendant home!",
            "The Emberwilds R10 boss still clutches my Twin Lightning Pendant. Strike it down and return to me at the campfire.",
            "The pendant sings again! My thanks — and this gold. But the wound in the wilds still yawns… speak with me when you are ready.",
            "Thalor keeps the circle warm. The Emberwilds still need a stronger hand.",
            "In progress  ·  Retrieve Thalor's pendant from Emberwilds R10",
            800,
            () => true);

        public static readonly QuestDefinition WardensPath = new(
            QuestId.WardensPath,
            "The Warded Path",
            "With the pendant restored, we can press the Emberwilds wound. Defeat the round 20 boss — seal that breach and a door will open into the Warded Halls, my old inner sanctum, now overrun. Gold and a rumor await your return.",
            "Defeat the Emberwilds R20 boss. Talk to RowZi at the door, then enter — the Warded Halls wait beyond.",
            "The path is open! Take this gold. Corvin — an Ashen Seer — flew into those halls as a crow and never returned. When you are ready, I will tell you more.",
            "The Warded Halls stand open. Ask when the wilds call again.",
            "In progress  ·  Clear Emberwilds R20 to open the Warded Halls",
            450,
            () => GameSave.QuestGrandWizardsPerilCompleted);

        public static readonly QuestDefinition GreyWizardsCrow = new(
            QuestId.GreyWizardsCrow,
            "Corvin's Crow",
            "Ashen Seer Corvin flew as a crow to spy on the foe in the Warded Halls — and never returned. Enter Warded Halls Survival; after round 10 find the dark crow and free him. He knows the path home.",
            "Warded Halls after round 10: tap the dark crow when you are close to break the glamour.",
            "You broke the glamour. I am Corvin — flesh again, and in your debt. Take this gold. The Ashen Seer will not forget.",
            "I watch the treeline for what the Second War left unfinished. Call on me when the dead stir.",
            "In progress  ·  Free Corvin the crow in Warded Halls (after R10)",
            1000,
            () => GameSave.InsideMapUnlocked);

        public static readonly QuestDefinition KnightsBestFriend = new(
            QuestId.KnightsBestFriend,
            "Aldric's Greatsword",
            "I am Sir Aldric of the broken oath. Ironvault was our holdfast in the Second War — I fled when the vault gates failed, and left my greatsword in the beast's grip. Recover it from the Ironvault round 40 boss. Restore my honor, and I will share Flame Enchant — plus 1000 gold.",
            "The Ironvault R40 boss still bears my greatsword — a knight's true companion. Cut it down and bring the blade to me north of camp.",
            "Steel and honor restored! Take the gold — and Flame Enchant. May your weapons burn true against whatever wakes next.",
            "Aldric stands ready by the fire. Ironvault remembers. Fight well, adventurer.",
            "In progress  ·  Recover Aldric's greatsword from Ironvault R40 boss",
            1000,
            () => GameSave.DungeonKnightReturnedToCamp);

        public static readonly QuestDefinition LyraVigil = new(
            QuestId.LyraVigil,
            "Lyra's Vigil",
            "I am Sister Lyra, keeper of quiet graves. The Silent Ossuary was sealed after the Second War — now something pounds the stone from within. Enter Ossuary Survival and defeat the round 50 Minotaur. Silence that wound, and I will reward you — the Endless Front still waits beyond.",
            "The Silent Ossuary R50 Minotaur still bellows under the stone. End its vigil, then return to me at camp.",
            "The ossuary sleeps again. Take this gold. When you are ready, Captain Bren can send you to The Endless Front — the war's last open wound.",
            "Lyra tends the ashes. The dead are quieter… for now.",
            "In progress  ·  Defeat Silent Ossuary R50 Minotaur",
            1200,
            () => GameSave.CryptMapUnlocked);

        public static readonly QuestDefinition BrensWatch = new(
            QuestId.BrensWatch,
            "Bren's Watch",
            "Captain Bren reporting. The Endless Front is where the Second War never ended — shifting biomes, no mercy. Hold the line through round 50 and return. Do that, and this campfire stays lit a while longer. Gold for every soul who stands with us.",
            "The Endless Front — survive to round 50, then report back to me at camp. Use the campfire if you need travel while we talk.",
            "Fifty rounds on the Front… and you still stand. Take the gold, soldier. The war is not over — but tonight, the fire is yours.",
            "Bren keeps the maps. The Endless Front never sleeps — neither do we.",
            "In progress  ·  Reach Endless Front round 50",
            1500,
            () => GameSave.UnlimitedMapUnlocked);

        static readonly QuestDefinition[] AllQuests =
        {
            GrandWizardsPeril,
            WardensPath,
            GreyWizardsCrow,
            KnightsBestFriend,
            LyraVigil,
            BrensWatch
        };

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

                case QuestId.WardensPath:
                    if (GameSave.QuestWardensPathCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestWardensPathAccepted) return QuestProgress.Available;
                    return GameSave.QuestWardensPathBossDefeated || GameSave.InsideMapUnlocked
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

                case QuestId.LyraVigil:
                    if (GameSave.QuestLyraVigilCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestLyraVigilAccepted) return QuestProgress.Available;
                    return GameSave.QuestLyraVigilBossDefeated || GameSave.CryptSurvivalCleared
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.BrensWatch:
                    if (GameSave.QuestBrensWatchCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestBrensWatchAccepted) return QuestProgress.Available;
                    return GameSave.QuestBrensWatchMilestone
                        || GameSave.UnlimitedHighestRoundReached >= 50
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                default:
                    return QuestProgress.Locked;
            }
        }

        /// <summary>
        /// Thalor's camp pool: pendant + gate always; crow only while still Available/Active
        /// (turn-in and completed crow dialogue belong to Corvin).
        /// </summary>
        public static QuestId[] GetThalorQuestIds()
        {
            var crow = GetProgress(QuestId.GreyWizardsCrow);
            if (crow == QuestProgress.Available || crow == QuestProgress.Active)
            {
                return new[]
                {
                    QuestId.GrandWizardsPeril,
                    QuestId.WardensPath,
                    QuestId.GreyWizardsCrow
                };
            }

            return new[]
            {
                QuestId.GrandWizardsPeril,
                QuestId.WardensPath
            };
        }

        /// <summary>Ashen Seer Corvin — crow turn-in and aftermath.</summary>
        public static readonly QuestId[] CorvinQuestIds =
        {
            QuestId.GreyWizardsCrow
        };

        public static readonly QuestId[] KnightQuestIds =
        {
            QuestId.KnightsBestFriend
        };

        public static readonly QuestId[] LyraQuestIds =
        {
            QuestId.LyraVigil
        };

        public static readonly QuestId[] BrenQuestIds =
        {
            QuestId.BrensWatch
        };

        /// <summary>Legacy helper — Thalor pool.</summary>
        public static bool TryGetPrimaryOpenQuest(out QuestDefinition def, out QuestProgress progress)
            => TryGetPrimaryOpenQuest(GetThalorQuestIds(), out def, out progress);

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

        /// <summary>Compact multi-quest status list for a giver's dialogue panel.</summary>
        public static string BuildQuestLog(QuestId[] pool, QuestId focusId)
        {
            if (pool == null || pool.Length == 0) return string.Empty;
            var sb = new StringBuilder();
            var first = true;
            for (var i = 0; i < pool.Length; i++)
            {
                if (!TryGet(pool[i], out var def)) continue;
                var progress = GetProgress(def.Id);
                if (progress == QuestProgress.Locked) continue;
                if (!first) sb.Append('\n');
                first = false;
                var mark = def.Id == focusId ? ">" : "-";
                sb.Append(mark).Append(' ').Append(def.Title).Append(" — ").Append(ProgressLabel(progress));
            }

            return sb.ToString();
        }

        public static string ProgressLabel(QuestProgress progress) => progress switch
        {
            QuestProgress.Available => "Available",
            QuestProgress.Active => "In progress",
            QuestProgress.ReadyToTurnIn => "Ready to turn in",
            QuestProgress.Completed => "Completed",
            _ => "Locked"
        };

        public static string RewardSummary(QuestDefinition def)
        {
            if (def.Id == QuestId.KnightsBestFriend)
                return $"{def.GoldReward} Gold + Flame Enchant";
            return $"{def.GoldReward} Gold";
        }

        public static bool TryAccept(QuestId id)
        {
            if (GetProgress(id) != QuestProgress.Available) return false;
            switch (id)
            {
                case QuestId.GrandWizardsPeril:
                    GameSave.QuestGrandWizardsPerilAccepted = true;
                    return true;
                case QuestId.WardensPath:
                    GameSave.QuestWardensPathAccepted = true;
                    return true;
                case QuestId.GreyWizardsCrow:
                    GameSave.QuestGreyWizardAccepted = true;
                    return true;
                case QuestId.KnightsBestFriend:
                    GameSave.QuestKnightsBestFriendAccepted = true;
                    return true;
                case QuestId.LyraVigil:
                    GameSave.QuestLyraVigilAccepted = true;
                    return true;
                case QuestId.BrensWatch:
                    GameSave.QuestBrensWatchAccepted = true;
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
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.WardensPath:
                    if (!GameSave.QuestWardensPathBossDefeated && !GameSave.InsideMapUnlocked)
                        return false;
                    GameSave.QuestWardensPathBossDefeated = true;
                    GameSave.QuestWardensPathCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.GreyWizardsCrow:
                    if (!GameSave.QuestGreyWizardRescued) return false;
                    GameSave.QuestGreyWizardCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.KnightsBestFriend:
                    if (!GameSave.HasKnightsGreatsword) return false;
                    GameSave.HasKnightsGreatsword = false;
                    GameSave.QuestKnightsBestFriendCompleted = true;
                    GameSave.FlameEnchantUnlocked = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.LyraVigil:
                    if (!GameSave.QuestLyraVigilBossDefeated && !GameSave.CryptSurvivalCleared)
                        return false;
                    GameSave.QuestLyraVigilBossDefeated = true;
                    GameSave.QuestLyraVigilCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.BrensWatch:
                    if (!GameSave.QuestBrensWatchMilestone && GameSave.UnlimitedHighestRoundReached < 50)
                        return false;
                    GameSave.QuestBrensWatchMilestone = true;
                    GameSave.QuestBrensWatchCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                default:
                    return false;
            }
        }

        static void AwardGold(int amount, out int goldAwarded)
        {
            goldAwarded = amount;
            GameSave.Gold += amount;
            GameSave.LifetimeGoldEarned += amount;
        }

        public static bool ShouldDropTwinLightningPendant(bool isOutsideRoundTenBoss)
        {
            if (!isOutsideRoundTenBoss) return false;
            if (GetProgress(QuestId.GrandWizardsPeril) != QuestProgress.Active) return false;
            return !GameSave.HasTwinLightningPendant;
        }

        public static void NotifyOutsideRoundTwentyCleared()
        {
            GameSave.QuestWardensPathBossDefeated = true;
        }

        public static void NotifyCryptRoundFiftyCleared()
        {
            GameSave.QuestLyraVigilBossDefeated = true;
        }

        /// <summary>Call when Endless Front depth is recorded.</summary>
        public static void NotifyUnlimitedRound(int round)
        {
            if (round < 50) return;
            if (GetProgress(QuestId.BrensWatch) != QuestProgress.Active) return;
            GameSave.QuestBrensWatchMilestone = true;
        }

        public static bool ShouldSpawnDarkBird(SurvivalMapKind mapKind, int round)
        {
            if (mapKind != SurvivalMapKind.Inside) return false;
            if (round < 10) return false;
            if (GetProgress(QuestId.GreyWizardsCrow) != QuestProgress.Active) return false;
            return !GameSave.QuestGreyWizardRescued;
        }

        public static bool ShouldDropKnightsGreatsword(bool isDungeonRoundFortyBoss)
        {
            if (!isDungeonRoundFortyBoss) return false;
            if (GetProgress(QuestId.KnightsBestFriend) != QuestProgress.Active) return false;
            return !GameSave.HasKnightsGreatsword;
        }

        public static bool UsesQuestPortrait(QuestId id) => true;

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
