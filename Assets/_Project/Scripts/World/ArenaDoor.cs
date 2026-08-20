using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.UI;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Door dropped after clearing Emberwilds (Outside) survival round 20.
    /// Requires talking to RowZi first, then shows a win recap (Camp / Enter Warded Halls).
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

            if (!GameSave.RowZiUnlocked)
            {
                GameHud.Instance?.ShowBanner("Talk to RowZi before entering the door!", 2.6f);
                return false;
            }

            _used = true;
            Achievements.UnlockDungeonDelver();
            GameSave.InsideMapUnlocked = true;

            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            var session = SurvivalSession.Instance;
            if (session != null)
            {
                session.BeginStageClearExit(
                    nextMap: SurvivalMapKind.Inside,
                    title: "Emberwilds Cleared!",
                    unlockSummary: "Warded Halls Survival unlocked!\nSpearman class unlocked at camp.");
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
