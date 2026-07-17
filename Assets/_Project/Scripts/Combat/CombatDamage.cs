using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Shared hit pipeline for crits, execute, boss hunter, lifesteal, and frost tip.
    /// </summary>
    public static class CombatDamage
    {
        public static void Apply(PlayerStats attacker, EnemyActor target, float weaponMultiplier = 1f, bool canApplyFrost = false)
        {
            if (attacker == null || target == null || !target.IsAlive || attacker.IsDead) return;

            var damage = attacker.RollDamage(target, weaponMultiplier);
            target.TakeDamage(damage);
            attacker.OnDamageDealt(damage);

            if (canApplyFrost && GameSave.FrostTipUnlocked && !target.IsBoss)
                target.ApplyFreeze(Random.Range(0.5f, 1f));
        }
    }
}
