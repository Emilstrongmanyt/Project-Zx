using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal after Crypt R50 Minotaur. Win recap unlocks Unlimited, then Camp.
    /// </summary>
    public class ArenaVictoryGate : MonoBehaviour
    {
        bool _used;

        public static void Spawn(Vector3 position)
        {
            var gate = GameFactory.CreateArenaVictoryGate(position);
            gate.AddComponent<ArenaVictoryGate>();
        }

        public bool TryEnter(Transform player)
        {
            if (_used || player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > 2.2f) return false;

            _used = true;

            GameSave.CryptSurvivalCleared = true;
            GameSave.RecordCryptRound(StatCaps.CryptMaxRound);
            GameSave.UnlimitedMapUnlocked = true;
            Achievements.UnlockCryptClearer();
            Achievements.UnlockEndlessHorizon();
            Achievements.EvaluateWeaponTierAchievements();

            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            var session = SurvivalSession.Instance;
            if (session != null)
            {
                session.BeginStageClearExit(
                    nextMap: null,
                    title: "Silent Ossuary Conquered!",
                    unlockSummary: "The Endless Front unlocked at camp!\nReturn to Sister Lyra to finish Lyra's Vigil.");
            }
            else
            {
                GameSessionContext.ClearPendingNextMap();
                GameFactory.LoadScene(GameScenes.MainMenuMap);
            }

            return true;
        }
    }
}
