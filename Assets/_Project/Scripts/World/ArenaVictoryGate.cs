using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal after Dungeon R40 boss. Returns to campfire and unlocks endgame progression.
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

            GameSave.DungeonSurvivalCleared = true;
            GameSave.FlameEnchantUnlocked = true;
            GameSave.UnlimitedMapUnlocked = true;
            GameSave.SamuraiUnlocked = true;
            Achievements.UnlockDungeonClearer();
            Achievements.UnlockEndlessHorizon();
            // RollZy_two sheet is selected automatically via DungeonSurvivalCleared.

            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;

            var session = SurvivalSession.Instance;
            if (session != null)
                session.RetreatToCamp();
            else
                GameFactory.LoadScene(GameScenes.MainMenuMap);

            return true;
        }
    }
}
