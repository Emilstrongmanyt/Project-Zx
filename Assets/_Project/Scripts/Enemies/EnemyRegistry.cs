using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Enemies
{
    /// <summary>
    /// Live enemy list for combat / audio / world-shift without FindObjectsByType.
    /// </summary>
    public static class EnemyRegistry
    {
        static readonly List<EnemyActor> Alive = new(128);

        public static IReadOnlyList<EnemyActor> All => Alive;

        public static void Register(EnemyActor enemy)
        {
            if (enemy == null) return;
            if (Alive.Contains(enemy)) return;
            Alive.Add(enemy);
        }

        public static void Unregister(EnemyActor enemy)
        {
            if (enemy == null) return;
            Alive.Remove(enemy);
        }

        public static void Clear() => Alive.Clear();
    }
}
