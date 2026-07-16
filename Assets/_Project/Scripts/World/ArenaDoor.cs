using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
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
            GameSessionContext.SurvivalMap = SurvivalMapKind.Inside;
            GameSessionContext.FreshSurvivalRun = false;
            GameSessionContext.CarryRound = SurvivalSession.Instance != null
                ? SurvivalSession.Instance.CurrentRound
                : 20;

            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
                stats.CaptureSnapshot(out GameSessionContext.RunSnapshot);

            GameFactory.LoadScene(GameScenes.SurvivalArena);
            return true;
        }
    }
}