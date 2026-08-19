using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal after clearing Dungeon survival round 40.
    /// Shows a win recap (Camp / Enter Crypt) instead of jumping straight into Crypt.
    /// </summary>
    public class ArenaCryptPortal : MonoBehaviour
    {
        bool _used;

        public static void Spawn(Vector3 position)
        {
            var portal = GameFactory.CreateArenaCryptPortal(position);
            portal.AddComponent<ArenaCryptPortal>();
        }

        public bool TryEnter(Transform player)
        {
            if (_used || player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > 2.2f) return false;

            _used = true;
            GameSave.CryptMapUnlocked = true;
            GameSave.DungeonSurvivalCleared = true;
            GameSave.RecordDungeonRound(40);
            // Flame Enchant is awarded by the knight quest (A Knight's Best Friend), not map clear.
            Achievements.EvaluateWeaponTierAchievements();
            Achievements.UnlockDungeonClearer();

            var stats = player.GetComponent<PlayerStats>();
            stats?.BankRunGoldToSave();

            var session = SurvivalSession.Instance;
            if (session != null)
            {
                session.BeginStageClearExit(
                    nextMap: SurvivalMapKind.Crypt,
                    title: "Dungeon Cleared!",
                    unlockSummary: "Crypt Survival unlocked!\nSamurai class unlocked at camp.");
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
