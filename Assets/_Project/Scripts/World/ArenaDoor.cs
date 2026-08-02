using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Door dropped after clearing Outside survival round 20.
    /// Starts a fresh Inside survival run (round 1, level 1).
    /// </summary>
    public class ArenaDoor : MonoBehaviour
    {
        bool _used;

        public static void Spawn(Vector3 position)
        {
            var door = GameFactory.CreateArenaDoor(position);
            door.AddComponent<ArenaDoor>();

            if (!GameSave.RowZiUnlocked)
                GameFactory.CreateRowZiUnlockNpc(position + Vector3.left * 2.2f);
        }

        public bool TryEnter(Transform player)
        {
            if (_used || player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > 2.2f) return false;

            _used = true;
            Achievements.UnlockDungeonDelver();
            GameSave.InsideMapUnlocked = true;

            // Bank Outside run gold before wiping the run into a fresh Inside map.
            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            // Fresh Inside run — round 1 / level 1, not a continuation of Outside.
            GameSessionContext.SurvivalMap = SurvivalMapKind.Inside;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;

            GameFactory.LoadScene(GameScenes.SurvivalArena);
            return true;
        }
    }
}
