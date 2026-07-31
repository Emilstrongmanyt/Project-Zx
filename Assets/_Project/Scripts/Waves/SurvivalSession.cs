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
        public SurvivalMapKind MapKind { get; private set; }

        Transform _player;
        GameHud _hud;
        bool _spawning;
        bool _roundActive;
        SurvivalMapKind _activeBiome;

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
            CurrentRound = GameSessionContext.FreshSurvivalRun
                ? Mathf.Max(0, GameSessionContext.StartingRound)
                : GameSessionContext.CarryRound;
            _activeBiome = GameSessionContext.GetVisualBiome(mapKind, Mathf.Max(1, CurrentRound + 1));
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop()
        {
            yield return null;

            while (true)
            {
                if (_player == null) break;
                var stats = _player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) break;

                CurrentRound++;
                if (MapKind == SurvivalMapKind.Unlimited && CurrentRound > StatCaps.UnlimitedMaxRound)
                    break;

                EnsureBiomeForRound(CurrentRound);
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
                        TryUnlockBowman(CurrentRound);

                        if (IsStageHoldRound(CurrentRound))
                        {
                            _hud?.ShowBanner(GetStageHoldBanner(CurrentRound), 5f);
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

                if (MapKind == SurvivalMapKind.Unlimited && CurrentRound >= StatCaps.UnlimitedMaxRound)
                {
                    _hud?.ShowBanner("Unlimited Survival complete! Returning to camp…", 4f);
                    yield return new WaitForSeconds(3f);
                    break;
                }
            }

            yield return new WaitForSeconds(2f);
            var finalStats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            finalStats?.BankRunGoldToSave();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        void EnsureBiomeForRound(int round)
        {
            if (MapKind != SurvivalMapKind.Unlimited) return;
            var biome = GameSessionContext.GetUnlimitedBiome(round);
            if (biome == _activeBiome) return;
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
            _hud?.ShowBanner(biome switch
            {
                SurvivalMapKind.Inside => "Entering the Inside…",
                SurvivalMapKind.Dungeon => "Descending into the Dungeon…",
                _ => "Back Outside…"
            }, 2.5f);
        }

        bool IsStageHoldRound(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside) return true;
            if (round == 30 && MapKind == SurvivalMapKind.Inside) return true;
            if (round == 40 && MapKind == SurvivalMapKind.Dungeon) return true;
            return false;
        }

        string GetStageHoldBanner(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside)
                return "Talk to RowZi, then enter the door!";
            if (round == 30 && MapKind == SurvivalMapKind.Inside)
                return "Enter the gateway to Dungeon Survival!";
            if (round == 40 && MapKind == SurvivalMapKind.Dungeon)
                return "Enter the victory portal to return to camp!";
            return "Stage cleared!";
        }

        IEnumerator SpawnRound(int round)
        {
            _spawning = true;
            EnemiesRemaining = 0;
            _hud?.SetRound(round, MapKind);

            var total = 6 + round * 5;
            var bossRound = round % 10 == 0;
            var roundTwentyBoss = round == 20 && MapKind == SurvivalMapKind.Outside;
            var roundThirtyBoss = round == 30 && MapKind == SurvivalMapKind.Inside;
            var roundFortyBoss = round == 40 && MapKind == SurvivalMapKind.Dungeon;
            // Unlimited uses standard bosses on every 10th round (not stage portals).
            if (MapKind == SurvivalMapKind.Unlimited && round == 100)
                bossRound = true;

            if (bossRound) total = Mathf.Max(total - 1, 1);

            var waveCount = GetWaveCount(round);
            var waveBonus = round > 10 ? (round - 10) / 4 : 0;
            var basePerWave = Mathf.Max(1, total / waveCount);
            var remainder = total % waveCount;

            for (var wave = 0; wave < waveCount; wave++)
            {
                var count = basePerWave + (wave < remainder ? 1 : 0) + waveBonus;
                _hud?.ShowWaveIncoming(wave + 1, waveCount);

                for (var i = 0; i < count; i++)
                {
                    SpawnEnemy(round, false, false, false, false);
                    if (i % 3 == 0) yield return null;
                }

                if (wave < waveCount - 1)
                    yield return new WaitForSeconds(GetWaveDelay(round));
            }

            if (bossRound)
            {
                yield return new WaitForSeconds(0.35f);
                SpawnEnemy(round, true, roundTwentyBoss, roundThirtyBoss, roundFortyBoss);
                _hud?.ShowBossWarning(roundTwentyBoss || roundThirtyBoss || roundFortyBoss);
            }

            _spawning = false;
        }

        static int GetWaveCount(int round)
        {
            if (round <= 5) return 1;
            return Mathf.Min(8, 2 + (round - 6) / 2);
        }

        static float GetWaveDelay(int round) => Mathf.Clamp(2.8f - round * 0.03f, 1.2f, 2.8f);

        void TryUnlockBowman(int round)
        {
            if (MapKind != SurvivalMapKind.Inside || round < 50 || GameSave.BowmanUnlocked) return;
            GameSave.BowmanUnlocked = true;
            Achievements.UnlockInsideArcher();
        }

        void SpawnEnemy(int round, bool boss, bool roundTwentyBoss, bool roundThirtyBoss, bool roundFortyBoss)
        {
            var origin = _player != null ? (Vector2)_player.position : Vector2.zero;
            var spawnPos = ArenaBounds.RandomSpawnAround(origin, 7f, 12f);
            var zombieKind = ResolveZombieKind(round);

            GameFactory.CreateEnemy(spawnPos, round, boss, roundTwentyBoss, zombieKind, roundThirtyBoss, roundFortyBoss);
            EnemiesRemaining++;
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
                _ => EnemyZombieKind.Outside
            };
        }

        public void NotifyEnemyKilled(EnemyActor enemy)
        {
            if (!enemy.IsAlive)
            {
                EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
                GameSave.RecordEnemyKill(enemy.IsBoss);
            }
        }

        public void RetreatToCamp()
        {
            StopAllCoroutines();
            _roundActive = false;

            var stats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            if (stats != null && !stats.IsDead)
            {
                GameSave.RecordHighestRound(CurrentRound);
                stats.BankRunGoldToSave();
            }

            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }
    }
}
