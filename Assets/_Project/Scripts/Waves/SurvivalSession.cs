using System.Collections;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.UI;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Waves
{
    public class SurvivalSession : MonoBehaviour
    {
        public int CurrentRound { get; private set; }
        public int EnemiesRemaining { get; private set; }
        public int RunKills { get; private set; }
        public SurvivalMapKind MapKind { get; private set; }

        Transform _player;
        GameHud _hud;
        bool _spawning;
        bool _roundActive;
        SurvivalMapKind _activeBiome;
        bool _darkBirdSpawned;
        bool _stageHoldActive;
        bool _stageClearExitStarted;

        public static SurvivalSession Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(Transform player, GameHud hud, SurvivalMapKind mapKind)
        {
            _player = player;
            _hud = hud;
            MapKind = mapKind;
            RunKills = 0;
            _stageHoldActive = false;
            CurrentRound = GameSessionContext.FreshSurvivalRun
                ? Mathf.Max(0, GameSessionContext.StartingRound)
                : GameSessionContext.CarryRound;
            _activeBiome = GameSessionContext.GetVisualBiome(mapKind, Mathf.Max(1, CurrentRound + 1));
            StartCoroutine(RunLoop());
        }

        public bool IsStageHoldActive => _stageHoldActive;

        public string GetStageHoldObjective()
            => _stageHoldActive ? GetStageHoldBanner(CurrentRound) : string.Empty;

        IEnumerator RunLoop()
        {
            yield return null;

            while (true)
            {
                if (_player == null) break;
                var stats = _player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) break;

                CurrentRound++;
                if (MapKind == SurvivalMapKind.Crypt && CurrentRound > StatCaps.CryptMaxRound)
                    break;
                if (MapKind == SurvivalMapKind.Unlimited && CurrentRound > StatCaps.UnlimitedMaxRound)
                    break;

                yield return StartCoroutine(EnsureBiomeForRoundRoutine(CurrentRound));
                TrySpawnDarkBird(CurrentRound);
                _roundActive = true;
                yield return StartCoroutine(SpawnRound(CurrentRound));

                while (_roundActive)
                {
                    if (_player == null) break;
                    stats = _player.GetComponent<PlayerStats>();
                    if (stats != null && stats.IsDead) break;
                    if (EnemiesRemaining <= 0 && !_spawning)
                    {
                        _roundActive = false;
                        _hud?.SetRoundComplete(CurrentRound);
                        GameSave.RecordHighestRound(CurrentRound);
                        TryRecordWeaponProgress(CurrentRound);
                        QuestCatalog.NotifySurvivalRound(MapKind, CurrentRound);
                        TryUnlockBowman(CurrentRound);
                        TryUnlockMagician(CurrentRound);

                        if (IsStageHoldRound(CurrentRound))
                        {
                            _stageHoldActive = true;
                            _hud?.ShowStickyBanner(GetStageHoldBanner(CurrentRound));
                            while (_player != null)
                            {
                                stats = _player.GetComponent<PlayerStats>();
                                if (stats != null && stats.IsDead) break;
                                yield return null;
                            }
                            break;
                        }

                        yield return new WaitForSeconds(2f);
                        break;
                    }
                    yield return null;
                }

                if (_player == null) break;
                stats = _player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) break;

                if (IsStageHoldRound(CurrentRound))
                    break;

                if (MapKind == SurvivalMapKind.Crypt && CurrentRound >= StatCaps.CryptMaxRound
                    && !IsStageHoldRound(CurrentRound))
                {
                    _hud?.ShowBanner("Silent Ossuary complete! Returning to camp…", 4f);
                    yield return new WaitForSeconds(3f);
                    break;
                }

                if (MapKind == SurvivalMapKind.Unlimited && CurrentRound >= StatCaps.UnlimitedMaxRound)
                {
                    _hud?.ShowBanner("The Endless Front complete! Returning to camp…", 4f);
                    yield return new WaitForSeconds(3f);
                    break;
                }
            }

            var finalStats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            var died = finalStats != null && finalStats.IsDead;
            finalStats?.BankRunGoldToSave();
            var goldBanked = GameSave.LastRunGoldBanked;
            GameSave.RecordLastRunSummary(goldBanked, CurrentRound, RunKills, died);

            if (died && _hud != null)
            {
                // Death results panel owns scene transition (Retry / Camp).
                yield return _hud.ShowRunResultsAndWait(
                    died: true,
                    CurrentRound,
                    RunKills,
                    goldBanked,
                    MapKind);
                yield break;
            }

            // Natural map completion (e.g. Unlimited R100) — win recap, then Camp.
            if (_hud != null)
            {
                var title = MapKind == SurvivalMapKind.Unlimited
                    ? "Endless Front Complete!"
                    : "Run Complete";
                yield return _hud.ShowRunResultsAndWait(
                    died: false,
                    CurrentRound,
                    RunKills,
                    goldBanked,
                    MapKind,
                    nextMap: null,
                    titleOverride: title,
                    unlockSummary: null);
                yield break;
            }

            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        /// <summary>
        /// Stage portal / victory gate: stop the run loop and show win recap
        /// (Camp, and optionally Enter Next Map).
        /// </summary>
        public void BeginStageClearExit(
            SurvivalMapKind? nextMap,
            string title,
            string unlockSummary)
        {
            if (_stageClearExitStarted) return;
            _stageClearExitStarted = true;
            _roundActive = false;
            _stageHoldActive = false;
            StopAllCoroutines();
            StartCoroutine(StageClearExitRoutine(nextMap, title, unlockSummary));
        }

        IEnumerator StageClearExitRoutine(
            SurvivalMapKind? nextMap,
            string title,
            string unlockSummary)
        {
            TryRecordWeaponProgress(CurrentRound);
            var goldBanked = GameSave.LastRunGoldBanked;
            GameSave.RecordLastRunSummary(goldBanked, CurrentRound, RunKills, died: false);

            if (nextMap.HasValue)
                GameSessionContext.SetPendingNextMap(nextMap.Value);
            else
                GameSessionContext.ClearPendingNextMap();

            if (_hud != null)
            {
                yield return _hud.ShowRunResultsAndWait(
                    died: false,
                    CurrentRound,
                    RunKills,
                    goldBanked,
                    MapKind,
                    nextMap,
                    title,
                    unlockSummary);
                yield break;
            }

            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        void TrySpawnDarkBird(int round)
        {
            if (_darkBirdSpawned) return;
            if (!QuestCatalog.ShouldSpawnDarkBird(MapKind, round)) return;

            _darkBirdSpawned = true;
            var origin = _player != null ? (Vector2)_player.position : Vector2.zero;
            // Prefer a far/edge pick so the crow is a find, not a spawn-camp target.
            var pos = ArenaBounds.RandomWaveSpawn(origin, preferDistance: true);
            DarkBirdRescue.Spawn(pos);
            _hud?.ShowBanner("A dark crow is watching in the distance…", 2.8f);
        }

        IEnumerator EnsureBiomeForRoundRoutine(int round)
        {
            if (MapKind != SurvivalMapKind.Unlimited) yield break;
            var biome = GameSessionContext.GetUnlimitedBiome(round);
            if (biome == _activeBiome) yield break;

            var crossing = biome switch
            {
                SurvivalMapKind.Inside => "Crossing into the Warded Halls…",
                SurvivalMapKind.Dungeon => "The Front shifts — Ironvault rises…",
                _ => "The Front shifts — Emberwilds ahead…"
            };
            _hud?.ShowStickyBanner(crossing);
            yield return new WaitForSeconds(0.85f);

            _activeBiome = biome;
            GameFactory.RebuildSurvivalEnvironment(biome);
            switch (biome)
            {
                case SurvivalMapKind.Inside:
                    AudioManager.Instance?.PlayInsideBgm();
                    break;
                case SurvivalMapKind.Dungeon:
                    AudioManager.Instance?.PlayDungeonBgm();
                    break;
                default:
                    AudioManager.Instance?.PlayOutsideBgm();
                    break;
            }

            _hud?.ClearStickyBanner();
            _hud?.ShowBanner(biome switch
            {
                SurvivalMapKind.Inside => "Entering the Warded Halls…",
                SurvivalMapKind.Dungeon => "Descending into Ironvault…",
                _ => "Back to the Emberwilds…"
            }, 2.5f);
            yield return new WaitForSeconds(0.35f);
        }

        bool IsStageHoldRound(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside) return true;
            if (round == 30 && MapKind == SurvivalMapKind.Inside) return true;
            if (round == 40 && MapKind == SurvivalMapKind.Dungeon) return true;
            if (round == StatCaps.CryptMaxRound && MapKind == SurvivalMapKind.Crypt) return true;
            return false;
        }

        string GetStageHoldBanner(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside)
                return "Talk to RowZi, then enter the door to the Warded Halls!";
            if (round == 30 && MapKind == SurvivalMapKind.Inside)
                return "Enter the gateway to Ironvault Survival!";
            if (round == 40 && MapKind == SurvivalMapKind.Dungeon)
                return "Enter the portal to Silent Ossuary Survival!";
            if (round == StatCaps.CryptMaxRound && MapKind == SurvivalMapKind.Crypt)
                return "Enter the victory portal — The Endless Front awaits!";
            return "Stage cleared!";
        }

        IEnumerator SpawnRound(int round)
        {
            _spawning = true;
            EnemiesRemaining = 0;
            _hud?.SetRound(round, MapKind);

            var bossRound = round % 10 == 0;
            var roundTwentyBoss = round == 20 && MapKind == SurvivalMapKind.Outside;
            var roundThirtyBoss = round == 30 && MapKind == SurvivalMapKind.Inside;
            var roundFortyBoss = round == 40 && MapKind == SurvivalMapKind.Dungeon;
            var roundFiftyBoss = round == StatCaps.CryptMaxRound && MapKind == SurvivalMapKind.Crypt;
            // Unlimited uses standard bosses on every 10th round (not stage portals).
            if (MapKind == SurvivalMapKind.Unlimited && round == 100)
                bossRound = true;
            if (roundFiftyBoss)
                bossRound = true;

            if (ChallengeWaveCatalog.TryGet(MapKind, round, out var challenge))
            {
                if (!string.IsNullOrEmpty(challenge.Banner))
                    _hud?.ShowBanner(challenge.Banner, 2.6f);

                yield return StartCoroutine(SpawnChallengeTrash(round, challenge));
            }
            else
            {
                yield return StartCoroutine(SpawnProbabilisticTrash(round, bossRound));
            }

            if (bossRound)
            {
                yield return new WaitForSeconds(0.35f);
                SpawnEnemy(round, true, roundTwentyBoss, roundThirtyBoss, roundFortyBoss, roundFiftyBoss);
                _hud?.ShowBossWarning(
                    roundTwentyBoss || roundThirtyBoss || roundFortyBoss || roundFiftyBoss);
            }

            _spawning = false;
        }

        IEnumerator SpawnChallengeTrash(int round, ChallengeRoundDef challenge)
        {
            if (challenge.Specs == null || challenge.Specs.Length == 0) yield break;

            for (var s = 0; s < challenge.Specs.Length; s++)
            {
                var spec = challenge.Specs[s];
                var count = Mathf.Max(1, spec.Count);
                for (var i = 0; i < count; i++)
                {
                    SpawnEnemy(
                        round,
                        boss: false,
                        roundTwentyBoss: false,
                        roundThirtyBoss: false,
                        roundFortyBoss: false,
                        roundFiftyBoss: false,
                        forcedKind: spec.Kind,
                        forcedRanged: spec.Ranged || spec.Mode == EnemyMovementMode.Kite,
                        forcedElite: spec.Elite,
                        forcedMode: spec.Mode);
                    if (i % 3 == 0) yield return null;
                }

                if (s < challenge.Specs.Length - 1)
                    yield return new WaitForSeconds(GetWaveDelay(round) * 0.85f);
            }
        }

        IEnumerator SpawnProbabilisticTrash(int round, bool bossRound)
        {
            // R1–20: original density. R21+: slower growth so late rounds are tougher packs, not walls.
            var total = round <= 20
                ? 6 + round * 5
                : 6 + 20 * 5 + (round - 20) * 2;
            if (bossRound) total = Mathf.Max(total - 1, 1);

            var waveCount = GetWaveCount(round);
            var waveBonus = round > 10
                ? (round <= 20 ? (round - 10) / 4 : 2 + (round - 20) / 8)
                : 0;
            var basePerWave = Mathf.Max(1, total / waveCount);
            var remainder = total % waveCount;

            for (var wave = 0; wave < waveCount; wave++)
            {
                var count = basePerWave + (wave < remainder ? 1 : 0) + waveBonus;

                for (var i = 0; i < count; i++)
                {
                    SpawnEnemy(round, false, false, false, false, false);
                    if (i % 3 == 0) yield return null;
                }

                if (wave < waveCount - 1)
                    yield return new WaitForSeconds(GetWaveDelay(round));
            }
        }

        static int GetWaveCount(int round)
        {
            if (round <= 5) return 1;
            return Mathf.Min(8, 2 + (round - 6) / 2);
        }

        static float GetWaveDelay(int round) => Mathf.Clamp(2.8f - round * 0.03f, 1.2f, 2.8f);

        void TryUnlockBowman(int round)
        {
            if (MapKind != SurvivalMapKind.Inside || round < 30 || GameSave.BowmanUnlocked) return;
            GameSave.BowmanUnlocked = true;
            Achievements.UnlockInsideArcher();
            _hud?.ShowBanner("Bowman unlocked!", 3.5f);
        }

        void TryUnlockMagician(int round)
        {
            if (MapKind != SurvivalMapKind.Unlimited || round < 80 || GameSave.MagicianUnlocked) return;
            GameSave.MagicianUnlocked = true;
            _hud?.ShowBanner("Magician unlocked!", 3.5f);
        }

        void SpawnEnemy(
            int round,
            bool boss,
            bool roundTwentyBoss,
            bool roundThirtyBoss,
            bool roundFortyBoss,
            bool roundFiftyBoss,
            EnemyZombieKind? forcedKind = null,
            bool? forcedRanged = null,
            bool? forcedElite = null,
            EnemyMovementMode? forcedMode = null)
        {
            var origin = _player != null ? (Vector2)_player.position : Vector2.zero;
            // Mix rings, edges, open field, and flanks so wave entries stay hard to camp.
            var spawnPos = ArenaBounds.RandomWaveSpawn(origin, preferDistance: boss);
            var zombieKind = forcedKind ?? ResolveZombieKind(round);
            var ranged = forcedRanged ?? (!boss && ShouldSpawnRanged(round));
            // After R20: occasional elites (1.3× size, stronger stats/loot). Early rounds stay clean.
            var elite = forcedElite ?? (!boss && round > 20 && Random.value < EliteSpawnChance(round));

            GameFactory.CreateEnemy(
                spawnPos,
                round,
                boss,
                roundTwentyBoss,
                zombieKind,
                roundThirtyBoss,
                roundFortyBoss,
                isRanged: ranged,
                isRoundFiftyBoss: roundFiftyBoss,
                isElite: elite,
                forcedMovementMode: forcedMode);
            EnemiesRemaining++;
        }

        /// <summary>R21 ~12%, climbing toward ~28% at very late rounds.</summary>
        static float EliteSpawnChance(int round)
        {
            if (round <= 20) return 0f;
            return Mathf.Clamp(0.12f + (round - 21) * 0.008f, 0.12f, 0.28f);
        }

        /// <summary>
        /// Late Dungeon / Crypt + Unlimited: mix in warlock/bat casters that fire projectiles.
        /// Chance ramps with round so early dungeon stays melee-heavy.
        /// </summary>
        bool ShouldSpawnRanged(int round)
        {
            if (MapKind == SurvivalMapKind.Dungeon)
            {
                if (round < 12) return false;
                // Half prior rates: R12 ~6%, R25 ~16%, R40 ~24% (capped).
                var chance = Mathf.Clamp(0.06f + (round - 12) * 0.0075f, 0.06f, 0.24f);
                return Random.value < chance;
            }

            if (MapKind == SurvivalMapKind.Crypt)
            {
                if (round < 8) return false;
                // Slightly more ranged than Dungeon: R8 ~10%, R25 ~22%, R50 ~32%.
                var chance = Mathf.Clamp(0.1f + (round - 8) * 0.008f, 0.1f, 0.32f);
                return Random.value < chance;
            }

            if (MapKind == SurvivalMapKind.Unlimited)
            {
                if (round < 20) return false;
                // R20 ~15%, R50 ~36%, R80+ ~50%.
                var chance = Mathf.Clamp(0.15f + (round - 20) * 0.007f, 0.15f, 0.5f);
                return Random.value < chance;
            }

            return false;
        }

        EnemyZombieKind ResolveZombieKind(int round)
        {
            if (MapKind == SurvivalMapKind.Unlimited)
            {
                // Mix of all enemy types; harder kinds more common later.
                var roll = Random.value;
                if (round <= 20)
                    return roll < 0.85f ? EnemyZombieKind.Outside : EnemyZombieKind.Inside;
                if (round <= 50)
                {
                    if (roll < 0.35f) return EnemyZombieKind.Outside;
                    if (roll < 0.8f) return EnemyZombieKind.Inside;
                    return EnemyZombieKind.InsideElite;
                }

                if (roll < 0.2f) return EnemyZombieKind.Outside;
                if (roll < 0.5f) return EnemyZombieKind.Inside;
                return EnemyZombieKind.InsideElite;
            }

            // Inside Survival: mix Outside + Inside zombies so the map is less brutal than pure Inside packs.
            if (MapKind == SurvivalMapKind.Inside)
                return Random.value < 0.45f ? EnemyZombieKind.Outside : EnemyZombieKind.Inside;

            return MapKind switch
            {
                SurvivalMapKind.Dungeon => EnemyZombieKind.InsideElite,
                SurvivalMapKind.Crypt => EnemyZombieKind.InsideElite,
                _ => EnemyZombieKind.Outside
            };
        }

        public void NotifyEnemyKilled(EnemyActor enemy)
        {
            if (!enemy.IsAlive)
            {
                EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
                RunKills++;
                var goldBanner = GameSave.RecordEnemyKillForWeapon(
                    GameSessionContext.SelectedClass, enemy.IsBoss);
                if (!string.IsNullOrEmpty(goldBanner))
                    _hud?.ShowBanner(goldBanner, 4.5f);
            }
        }

        public void RetreatToCamp()
        {
            StopAllCoroutines();
            _roundActive = false;
            _stageHoldActive = false;

            var stats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            var goldBefore = 0;
            if (stats != null && !stats.IsDead)
            {
                GameSave.RecordHighestRound(CurrentRound);
                TryRecordWeaponProgress(CurrentRound);
                goldBefore = stats.RunGold;
                stats.BankRunGoldToSave();
            }

            var goldBanked = GameSave.LastRunGoldBanked > 0 ? GameSave.LastRunGoldBanked : goldBefore;
            GameSave.RecordLastRunSummary(goldBanked, CurrentRound, RunKills, died: false);

            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        /// <summary>
        /// Tracks Dungeon / Unlimited depth for weapon material unlocks (per class / weapon type).
        /// </summary>
        void TryRecordWeaponProgress(int round)
        {
            if (round <= 0) return;

            var weaponClass = GameSessionContext.SelectedClass;

            if (MapKind == SurvivalMapKind.Dungeon)
            {
                // Global map depth (doors / achievements) still uses the shared best.
                GameSave.RecordDungeonRound(round);

                var previous = GameSave.GetWeaponDungeonBest(weaponClass);
                if (GameSave.RecordWeaponDungeonRound(weaponClass, round))
                {
                    var banner = WeaponCatalog.TryNotifyDungeonIronUnlock(
                        weaponClass, previous, GameSave.GetWeaponDungeonBest(weaponClass));
                    if (!string.IsNullOrEmpty(banner))
                        _hud?.ShowBanner(banner, 4.5f);
                }

                Achievements.EvaluateWeaponTierAchievements();
                return;
            }

            if (MapKind == SurvivalMapKind.Crypt)
            {
                if (GameSave.RecordCryptRound(round) && round >= StatCaps.CryptMaxRound)
                    _hud?.ShowBanner("Silent Ossuary conquered! The Endless Front unlocked at camp.", 4.5f);
                Achievements.EvaluateWeaponTierAchievements();
                return;
            }

            if (MapKind != SurvivalMapKind.Unlimited) return;

            GameSave.RecordUnlimitedRound(round);
            // Bren milestone is also covered by NotifySurvivalRound on round clear.

            var prevUnlimited = GameSave.GetWeaponUnlimitedBest(weaponClass);
            if (GameSave.RecordWeaponUnlimitedRound(weaponClass, round))
            {
                var unlimitedBanner = WeaponCatalog.TryNotifyUnlimitedTierUnlock(
                    weaponClass, prevUnlimited, GameSave.GetWeaponUnlimitedBest(weaponClass));
                if (!string.IsNullOrEmpty(unlimitedBanner))
                    _hud?.ShowBanner(unlimitedBanner, 4.5f);
            }

            Achievements.EvaluateWeaponTierAchievements();
        }
    }
}
