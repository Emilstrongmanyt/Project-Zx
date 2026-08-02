using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal dropped after clearing Inside survival round 30.
    /// Starts a fresh Dungeon survival run (round 1, level 1).
    /// </summary>
    public class ArenaGateway : MonoBehaviour
    {
        bool _used;

        public static void Spawn(Vector3 position)
        {
            var gateway = GameFactory.CreateArenaGateway(position);
            gateway.AddComponent<ArenaGateway>();
        }

        public bool TryEnter(Transform player)
        {
            if (_used || player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > 2.2f) return false;

            _used = true;
            GameSave.DungeonMapUnlocked = true;
            GameSave.InsideSurvivalCleared = true;

            // Bank Inside run gold before wiping the run into a fresh Dungeon map.
            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            GameSessionContext.SurvivalMap = SurvivalMapKind.Dungeon;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;

            GameFactory.LoadScene(GameScenes.SurvivalArena);
            return true;
        }
    }
}
