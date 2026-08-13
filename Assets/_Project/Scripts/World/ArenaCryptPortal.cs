using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Portal after clearing Dungeon survival round 40.
    /// Starts a fresh Crypt survival run (round 1, level 1).
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

            GameSessionContext.SurvivalMap = SurvivalMapKind.Crypt;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;

            GameFactory.LoadScene(GameScenes.SurvivalArena);
            return true;
        }
    }
}
