using System.Collections;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.UI;
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
            CurrentRound = GameSessionContext.FreshSurvivalRun ? 0 : GameSessionContext.CarryRound;
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
                        yield return new WaitForSeconds(2f);
                        break;
                    }
                    yield return null;
                }

                if (_player == null) break;
                stats = _player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) break;
            }

            yield return new WaitForSeconds(2f);
            var finalStats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            finalStats?.BankRunGoldToSave();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        IEnumerator SpawnRound(int round)
        {
            _spawning = true;
            EnemiesRemaining = 0;
            _hud?.SetRound(round, MapKind);
            _hud?.ShowWaveIncoming();

            var total = 6 + round * 5;
            var bossRound = round % 10 == 0;
            var roundTwentyBoss = round == 20 && MapKind == SurvivalMapKind.Outside;
            if (bossRound) total = Mathf.Max(total - 1, 1);

            for (var i = 0; i < total; i++)
            {
                SpawnEnemy(round, false, false);
                if (i % 3 == 0) yield return null;
            }

            if (bossRound)
            {
                yield return new WaitForSeconds(0.35f);
                SpawnEnemy(round, true, roundTwentyBoss);
                _hud?.ShowBossWarning(roundTwentyBoss);
            }

            _spawning = false;
        }

        void SpawnEnemy(int round, bool boss, bool roundTwentyBoss)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(7f, 12f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            var origin = _player != null ? (Vector2)_player.position : Vector2.zero;
            GameFactory.CreateEnemy(origin + offset, round, boss, roundTwentyBoss);
            EnemiesRemaining++;
        }

        public void NotifyEnemyKilled(EnemyActor enemy)
        {
            if (!enemy.IsAlive) EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        }
    }
}