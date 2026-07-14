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

        Transform _player;
        GameHud _hud;
        bool _spawning;
        bool _roundActive;

        public static SurvivalSession Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public void Begin(Transform player, GameHud hud)
        {
            _player = player;
            _hud = hud;
            CurrentRound = 0;
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop()
        {
            yield return new WaitForSeconds(0.5f);
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
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        IEnumerator SpawnRound(int round)
        {
            _spawning = true;
            EnemiesRemaining = 0;
            _hud?.SetRound(round);

            var total = 25 * round;
            var bossRound = round % 10 == 0;
            if (bossRound) total = Mathf.Max(total - 1, 1);

            for (var i = 0; i < total; i++)
            {
                SpawnEnemy(round, false);
                if (i % 4 == 0) yield return null;
            }

            if (bossRound)
            {
                yield return new WaitForSeconds(0.5f);
                SpawnEnemy(round, true);
                _hud?.ShowBossWarning();
            }

            _spawning = false;
        }

        void SpawnEnemy(int round, bool boss)
        {
            var offset = Random.insideUnitCircle.normalized * Random.Range(8f, 14f);
            var origin = _player != null ? (Vector2)_player.position : Vector2.zero;
            GameFactory.CreateEnemy(origin + offset, round, boss);
            EnemiesRemaining++;
        }

        public void NotifyEnemyKilled(EnemyActor enemy)
        {
            if (!enemy.IsAlive) EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        }
    }
}