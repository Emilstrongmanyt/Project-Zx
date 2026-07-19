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

                        // Outside R20: hold until door used.
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

                // Player left via portal (scene unload) or died while waiting after stage clear.
                if (IsStageHoldRound(CurrentRound))
                    break;
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

        bool IsStageHoldRound(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside) return true;
            if (round == 30 && MapKind == SurvivalMapKind.Inside) return true;
            return false;
        }

        string GetStageHoldBanner(int round)
        {
            if (round == 20 && MapKind == SurvivalMapKind.Outside)
                return "Talk to RowZi, then enter the door!";
            if (round == 30 && MapKind == SurvivalMapKind.Inside)
                return "Enter the gateway to Dungeon Survival!";
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

            // Outside: ZombieJ · Inside: ZombieJ_Inside · Dungeon: ZombieJ_Inside2
            var zombieKind = MapKind switch
            {
                SurvivalMapKind.Inside => EnemyZombieKind.Inside,
                SurvivalMapKind.Dungeon => EnemyZombieKind.InsideElite,
                _ => EnemyZombieKind.Outside
            };

            GameFactory.CreateEnemy(spawnPos, round, boss, roundTwentyBoss, zombieKind, roundThirtyBoss, roundFortyBoss);
            EnemiesRemaining++;
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
