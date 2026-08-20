using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal dropped after clearing Warded Halls (Inside) survival round 30.
    /// Shows a win recap (Camp / Enter Ironvault) instead of jumping straight in.
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

            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            var session = SurvivalSession.Instance;
            if (session != null)
            {
                session.BeginStageClearExit(
                    nextMap: SurvivalMapKind.Dungeon,
                    title: "Warded Halls Cleared!",
                    unlockSummary: "Ironvault Survival unlocked!\nBowman class unlocked at camp.");
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
