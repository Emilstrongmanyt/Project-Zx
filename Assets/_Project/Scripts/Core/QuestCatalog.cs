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
        BrensWatch = 6,
        /// <summary>Scout Kael: push Emberwilds to round 15.</summary>
        KaelsRecon = 7,
        /// <summary>Herbalist Nessa: push Warded Halls to round 15.</summary>
        NessasSalve = 8,
        /// <summary>Smith Garrick: push Ironvault to round 20.</summary>
        GarricksAnvil = 9,
        /// <summary>Cartographer Tove: push Silent Ossuary to round 25.</summary>
        TovesChart = 10,
        /// <summary>Ashen Seer Corvin: hold The Endless Front through round 75 — the dead stir.</summary>
        CorvinsOmen = 11
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
            "Thalor keeps the circle warm. Speak with me for The Warded Path — Emberwilds R20 opens the halls, and Corvin's fate beyond.",
            "In progress  ·  Retrieve Thalor's pendant from Emberwilds R10",
            800,
            () => true);

        public static readonly QuestDefinition WardensPath = new(
            QuestId.WardensPath,
            "The Warded Path",
            "With the pendant restored, we can press the Emberwilds wound. Defeat the round 20 boss — seal that breach and a door will open into the Warded Halls, my old inner sanctum, now overrun. Gold and a rumor await your return.",
            "Clear Emberwilds R20. At the north door: talk to RowZi first, then enter — that opens Warded Halls. Return to Thalor after.",
            "The path is open! Take this gold. Corvin — an Ashen Seer — flew into those halls as a crow and never returned. Speak with me again when you are ready to hunt for him.",
            "The Warded Halls stand open. Free Corvin's crow when you can, press Ironvault when the halls yield, and remember: the Front's omen waits after Bren.",
            "In progress  ·  Emberwilds R20 → talk to RowZi → enter the door",
            450,
            () => GameSave.QuestGrandWizardsPerilCompleted);

        public static readonly QuestDefinition GreyWizardsCrow = new(
            QuestId.GreyWizardsCrow,
            "Corvin's Crow",
            "Ashen Seer Corvin flew as a crow to spy on the foe in the Warded Halls — and never returned. Enter Warded Halls Survival; after round 10 a dark crow appears nearby — free him, then meet him back at this campfire (not Thalor).",
            "Warded Halls after R10: look for the sparkling dark crow near you, tap to free him, then turn in with Corvin at camp.",
            "You broke the glamour. I am Corvin — flesh again, and in your debt. Take this gold. Hold the Front with Bren — when that watch is done, the dead will stir, and I will call.",
            "I watch the treeline for what the Second War left unfinished. Finish Bren's Watch on the Endless Front — then speak with me. The omen is coming.",
            "In progress  ·  Free the crow in Warded Halls (after R10), then talk to Corvin",
            1000,
            () => GameSave.InsideMapUnlocked);

        public static readonly QuestDefinition KnightsBestFriend = new(
            QuestId.KnightsBestFriend,
            "Aldric's Greatsword",
            "I am Sir Aldric of the broken oath. Ironvault was our holdfast in the Second War — I fled when the vault gates failed, and left my greatsword in the beast's grip. Recover it from the Ironvault round 40 boss. Restore my honor, and I will share Flame Enchant — plus 1000 gold.",
            "Ironvault R40 boss still bears my greatsword. Cut it down, pick up the blade, then bring it to me north of camp.",
            "Steel and honor restored! Take the gold — and Flame Enchant (shop / loadout). Beyond Ironvault, Sister Lyra watches the Ossuary when that path opens.",
            "Aldric stands ready by the fire. Ironvault remembers — the Ossuary waits when you are ready. Fight well.",
            "In progress  ·  Recover Aldric's greatsword from Ironvault R40, then talk to Aldric",
            1000,
            () => GameSave.DungeonKnightReturnedToCamp);

        public static readonly QuestDefinition LyraVigil = new(
            QuestId.LyraVigil,
            "Lyra's Vigil",
            "I am Sister Lyra, keeper of quiet graves. The Silent Ossuary was sealed after the Second War — now something pounds the stone from within. Enter Ossuary Survival and defeat the round 50 Minotaur. Silence that wound — the victory gate beyond unlocks The Endless Front.",
            "Silent Ossuary R50 Minotaur: end its vigil, take the victory gate if it appears, then return to me at camp for your reward.",
            "The ossuary sleeps again. Take this gold. Captain Bren (by the maps) can send you to The Endless Front — the war's last open wound. After his watch, Corvin will read the dead.",
            "Lyra tends the ashes. Bren holds the Front; Corvin's omen waits after you stand R50 for the captain.",
            "In progress  ·  Defeat Silent Ossuary R50 Minotaur (then talk to Lyra)",
            1200,
            () => GameSave.CryptMapUnlocked);

        public static readonly QuestDefinition BrensWatch = new(
            QuestId.BrensWatch,
            "Bren's Watch",
            "Captain Bren reporting. The Endless Front is where the Second War never ended — shifting biomes, no mercy. Hold the line through round 50 and return. Do that, and this campfire stays lit a while longer. Gold for every soul who stands with us.",
            "Endless Front Survival — reach round 50, then report to Bren at camp. (Maps button stays available while we talk.)",
            "Fifty rounds on the Front… and you still stand. Take the gold, soldier. The war is not over — Corvin (Ashen Seer by the treeline) senses the dead stirring. Speak with him when you can.",
            "Bren keeps the maps. The Endless Front never sleeps — and Corvin's omen waits by the treeline if you have held R50 for me already.",
            "In progress  ·  Reach Endless Front round 50, then talk to Bren",
            1500,
            () => GameSave.UnlimitedMapUnlocked);

        public static readonly QuestDefinition KaelsRecon = new(
            QuestId.KaelsRecon,
            "Kael's Recon",
            "Scout Kael. Thalor trusts you now — good. I need eyes past the tree line. Push Emberwilds Survival to round 15 and mark what still moves out there. Coin for a clean report.",
            "Emberwilds Survival — reach round 15, then return to me south-west of the fire.",
            "Solid work. The wilds are worse than the maps admit. Take this gold — keep your blade ready for Bren's Front when that path opens.",
            "Kael keeps watch on the Emberwilds trail. Beyond Lyra and Bren, Corvin reads Front omens when the dead stir.",
            "In progress  ·  Reach Emberwilds round 15, then talk to Kael",
            400,
            () => GameSave.QuestGrandWizardsPerilCompleted);

        public static readonly QuestDefinition NessasSalve = new(
            QuestId.NessasSalve,
            "Nessa's Salve",
            "I am Nessa — I brew what the war left broken. The Warded Halls still bleed spore and ash. Survive to round 15 there so I can finish a camp salve from what the halls shed. Gold when you return — the brew stays with the fire, not your pack.",
            "Warded Halls Survival — reach round 15, then find me north of Mira for gold (the salve is for the camp).",
            "The halls gave you enough. This gold is yours — the salve is already steeping by the fire. If a dark crow still watches after R10, free him for Corvin.",
            "Nessa tends herbs by the north stones. Free Corvin's crow in the halls if you have not — then the Front's omen will find you.",
            "In progress  ·  Reach Warded Halls round 15, then talk to Nessa",
            500,
            () => GameSave.InsideMapUnlocked);

        public static readonly QuestDefinition GarricksAnvil = new(
            QuestId.GarricksAnvil,
            "Garrick's Anvil",
            "Name's Garrick. Ironvault ate half my forge when the gates failed. Hold that dungeon through round 20 and prove the vault still answers to steel. I'll pay in gold — and a nod from the anvil.",
            "Ironvault Survival — reach round 20, then report to me east of Thalor.",
            "Twenty rounds and the vault didn't break you. Take the gold. Aldric still wants his greatsword deeper in — R40.",
            "Garrick hammers near the east rise. Ironvault never cools — Aldric's blade, then the Front beyond.",
            "In progress  ·  Reach Ironvault round 20, then talk to Garrick",
            600,
            () => GameSave.DungeonMapUnlocked);

        public static readonly QuestDefinition TovesChart = new(
            QuestId.TovesChart,
            "Tove's Chart",
            "Cartographer Tove. The Silent Ossuary shifts under every torch. Survive through round 25 and tell me what you saw — paths, dead ends, anything that moves. I redraw the chart here; fair gold for a true report.",
            "Silent Ossuary Survival — reach round 25, then return to Tove (you report; the chart stays at camp).",
            "Your marks match the stone. Take this gold. Lyra's vigil still waits at R50 — silence that, and Bren opens the Front; Corvin's omen follows.",
            "Tove redraws the war's wounds by the east path. Lyra → Bren → Corvin: that is the road past the ossuary.",
            "In progress  ·  Reach Silent Ossuary round 25, then talk to Tove",
            700,
            () => GameSave.CryptMapUnlocked);

        public static readonly QuestDefinition CorvinsOmen = new(
            QuestId.CorvinsOmen,
            "Corvin's Omen",
            "The Front held at fifty — and still the ash tastes wrong. I am Corvin. Deeper on the Endless Front the dead stir: bones that should stay buried, banners that should not move. Hold the line through round 75 and return. I must read what walks that far.",
            "Endless Front Survival — reach round 75, then report to Corvin at camp (Bren still holds the map board).",
            "Seventy-five… and the omen is true. Something older than the Second War woke on that line. Take this gold. Rest by the fire — when ash falls wrong again, the Ashen Seer will call.",
            "Corvin watches the Front's deeper dark. The omen at seventy-five was no dream — something older than the Second War walks. The campfire holds. When ash falls wrong again, seek the Ashen Seer.",
            "In progress  ·  Reach Endless Front round 75, then talk to Corvin",
            2000,
            () => GameSave.QuestBrensWatchCompleted && GameSave.QuestGreyWizardCompleted);

        static readonly QuestDefinition[] AllQuests =
        {
            GrandWizardsPeril,
            WardensPath,
            GreyWizardsCrow,
            KnightsBestFriend,
            LyraVigil,
            BrensWatch,
            CorvinsOmen,
            KaelsRecon,
            NessasSalve,
            GarricksAnvil,
            TovesChart
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

                case QuestId.CorvinsOmen:
                    if (GameSave.QuestCorvinsOmenCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestCorvinsOmenAccepted) return QuestProgress.Available;
                    return GameSave.QuestCorvinsOmenMilestone
                        || GameSave.UnlimitedHighestRoundReached >= 75
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.KaelsRecon:
                    if (GameSave.QuestKaelsReconCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestKaelsReconAccepted) return QuestProgress.Available;
                    return GameSave.QuestKaelsReconMilestone
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.NessasSalve:
                    if (GameSave.QuestNessasSalveCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestNessasSalveAccepted) return QuestProgress.Available;
                    return GameSave.QuestNessasSalveMilestone
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.GarricksAnvil:
                    if (GameSave.QuestGarricksAnvilCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestGarricksAnvilAccepted) return QuestProgress.Available;
                    return GameSave.QuestGarricksAnvilMilestone
                        ? QuestProgress.ReadyToTurnIn
                        : QuestProgress.Active;

                case QuestId.TovesChart:
                    if (GameSave.QuestTovesChartCompleted) return QuestProgress.Completed;
                    if (!GameSave.QuestTovesChartAccepted) return QuestProgress.Available;
                    return GameSave.QuestTovesChartMilestone
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

        /// <summary>Ashen Seer Corvin — crow turn-in, then Corvin's Omen after Bren's Watch.</summary>
        public static readonly QuestId[] CorvinQuestIds =
        {
            QuestId.GreyWizardsCrow,
            QuestId.CorvinsOmen
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

        public static readonly QuestId[] KaelQuestIds =
        {
            QuestId.KaelsRecon
        };

        public static readonly QuestId[] NessaQuestIds =
        {
            QuestId.NessasSalve
        };

        public static readonly QuestId[] GarrickQuestIds =
        {
            QuestId.GarricksAnvil
        };

        public static readonly QuestId[] ToveQuestIds =
        {
            QuestId.TovesChart
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

        /// <summary>Giver pool for a quest — used so the objective chip opens the right NPC dialogue.</summary>
        public static QuestId[] GetQuestPoolFor(QuestId id)
        {
            if (id == QuestId.GreyWizardsCrow)
            {
                var crow = GetProgress(QuestId.GreyWizardsCrow);
                if (crow == QuestProgress.ReadyToTurnIn || crow == QuestProgress.Completed)
                    return CorvinQuestIds;
                return GetThalorQuestIds();
            }

            return id switch
            {
                QuestId.KnightsBestFriend => KnightQuestIds,
                QuestId.LyraVigil => LyraQuestIds,
                QuestId.BrensWatch => BrenQuestIds,
                QuestId.CorvinsOmen => CorvinQuestIds,
                QuestId.KaelsRecon => KaelQuestIds,
                QuestId.NessasSalve => NessaQuestIds,
                QuestId.GarricksAnvil => GarrickQuestIds,
                QuestId.TovesChart => ToveQuestIds,
                _ => GetThalorQuestIds()
            };
        }

        /// <summary>Short NPC name for HUD / toast copy.</summary>
        public static string GetGiverDisplayName(QuestId id)
        {
            if (id == QuestId.GreyWizardsCrow)
            {
                var crow = GetProgress(QuestId.GreyWizardsCrow);
                if (crow == QuestProgress.ReadyToTurnIn || crow == QuestProgress.Completed)
                    return "Corvin";
                return "Thalor";
            }

            return id switch
            {
                QuestId.KnightsBestFriend => "Aldric",
                QuestId.LyraVigil => "Lyra",
                QuestId.BrensWatch => "Bren",
                QuestId.CorvinsOmen => "Corvin",
                QuestId.KaelsRecon => "Kael",
                QuestId.NessasSalve => "Nessa",
                QuestId.GarricksAnvil => "Garrick",
                QuestId.TovesChart => "Tove",
                _ => "Thalor"
            };
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

        /// <summary>
        /// Camp-wide digest of open tasks (ready → active → available). Used when a giver has only one quest
        /// so players still see other camp work at a glance.
        /// </summary>
        public static string BuildCampQuestDigest(QuestId focusId, int maxLines = 4)
        {
            var sb = new StringBuilder();
            var lines = 0;
            void AppendBucket(QuestProgress wanted)
            {
                for (var i = 0; i < AllQuests.Length && lines < maxLines; i++)
                {
                    var def = AllQuests[i];
                    var progress = GetProgress(def.Id);
                    if (progress != wanted) continue;
                    if (lines > 0) sb.Append('\n');
                    var mark = def.Id == focusId ? ">" : "-";
                    sb.Append(mark).Append(' ')
                        .Append(GetGiverDisplayName(def.Id)).Append(": ")
                        .Append(def.Title).Append(" — ").Append(ProgressLabel(progress));
                    lines++;
                }
            }

            AppendBucket(QuestProgress.ReadyToTurnIn);
            AppendBucket(QuestProgress.Active);
            AppendBucket(QuestProgress.Available);
            return sb.ToString();
        }

        public static int CountQuestsInProgress(QuestProgress progress)
        {
            var n = 0;
            for (var i = 0; i < AllQuests.Length; i++)
            {
                if (GetProgress(AllQuests[i].Id) == progress)
                    n++;
            }

            return n;
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
                case QuestId.CorvinsOmen:
                    GameSave.QuestCorvinsOmenAccepted = true;
                    return true;
                case QuestId.KaelsRecon:
                    GameSave.QuestKaelsReconAccepted = true;
                    return true;
                case QuestId.NessasSalve:
                    GameSave.QuestNessasSalveAccepted = true;
                    return true;
                case QuestId.GarricksAnvil:
                    GameSave.QuestGarricksAnvilAccepted = true;
                    return true;
                case QuestId.TovesChart:
                    GameSave.QuestTovesChartAccepted = true;
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

                case QuestId.CorvinsOmen:
                    if (!GameSave.QuestCorvinsOmenMilestone && GameSave.UnlimitedHighestRoundReached < 75)
                        return false;
                    GameSave.QuestCorvinsOmenMilestone = true;
                    GameSave.QuestCorvinsOmenCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.KaelsRecon:
                    if (!GameSave.QuestKaelsReconMilestone) return false;
                    GameSave.QuestKaelsReconCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.NessasSalve:
                    if (!GameSave.QuestNessasSalveMilestone) return false;
                    GameSave.QuestNessasSalveCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.GarricksAnvil:
                    if (!GameSave.QuestGarricksAnvilMilestone) return false;
                    GameSave.QuestGarricksAnvilCompleted = true;
                    AwardGold(def.GoldReward, out goldAwarded);
                    return true;

                case QuestId.TovesChart:
                    if (!GameSave.QuestTovesChartMilestone) return false;
                    GameSave.QuestTovesChartCompleted = true;
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
            if (round >= 50 && GetProgress(QuestId.BrensWatch) == QuestProgress.Active)
                GameSave.QuestBrensWatchMilestone = true;

            if (round >= 75 && GetProgress(QuestId.CorvinsOmen) == QuestProgress.Active)
                GameSave.QuestCorvinsOmenMilestone = true;
        }

        /// <summary>In-run banner when a Front quest milestone is first hit this clear.</summary>
        public static string TryBuildUnlimitedQuestBanner(int round)
        {
            if (round == 50 && GetProgress(QuestId.BrensWatch) == QuestProgress.Active)
                return "Bren's Watch — round 50 held! Report to Bren at camp.";
            if (round == 75 && GetProgress(QuestId.CorvinsOmen) == QuestProgress.Active)
                return "Corvin's Omen — round 75 held! Report to Corvin at camp.";
            return string.Empty;
        }

        /// <summary>Side-quest round milestones keyed to the map the player actually cleared.</summary>
        public static void NotifySurvivalRound(SurvivalMapKind mapKind, int round)
        {
            if (round <= 0) return;

            if (mapKind == SurvivalMapKind.Outside
                && round >= 15
                && GetProgress(QuestId.KaelsRecon) == QuestProgress.Active)
            {
                GameSave.QuestKaelsReconMilestone = true;
            }

            if (mapKind == SurvivalMapKind.Inside
                && round >= 15
                && GetProgress(QuestId.NessasSalve) == QuestProgress.Active)
            {
                GameSave.QuestNessasSalveMilestone = true;
            }

            if (mapKind == SurvivalMapKind.Dungeon
                && round >= 20
                && GetProgress(QuestId.GarricksAnvil) == QuestProgress.Active)
            {
                GameSave.QuestGarricksAnvilMilestone = true;
            }

            if (mapKind == SurvivalMapKind.Crypt
                && round >= 25
                && GetProgress(QuestId.TovesChart) == QuestProgress.Active)
            {
                GameSave.QuestTovesChartMilestone = true;
            }

            if (mapKind == SurvivalMapKind.Unlimited)
                NotifyUnlimitedRound(round);
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
            {
                var readyExtra = CountQuestsInProgress(QuestProgress.ReadyToTurnIn) - 1;
                var readyMore = readyExtra > 0 ? $"  (+{readyExtra} more)" : string.Empty;
                return $"Talk to {GetGiverDisplayName(readyDef.Id)} — {readyDef.Title} ready!{readyMore}";
            }

            if (TryFindHudQuest(QuestProgress.Active, out var activeDef, out _))
            {
                var hint = string.IsNullOrEmpty(activeDef.ActiveStatusHint)
                    ? activeDef.Title
                    : activeDef.ActiveStatusHint;
                var activeExtra = CountQuestsInProgress(QuestProgress.Active) - 1;
                var more = activeExtra > 0 ? $"  (+{activeExtra} more)" : string.Empty;
                return $"{hint}{more}";
            }

            if (TryFindHudQuest(QuestProgress.Available, out var availableDef, out _))
            {
                return $"New task — talk to {GetGiverDisplayName(availableDef.Id)} ({availableDef.Title})";
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

        public static bool TryFindHudQuestAvailable(out QuestDefinition def)
        {
            return TryFindHudQuest(QuestProgress.Available, out def, out _);
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
