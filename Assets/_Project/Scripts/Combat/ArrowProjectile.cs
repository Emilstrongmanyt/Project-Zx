using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>Visible bowman arrow that flies to a target and applies damage on impact.</summary>
    public class ArrowProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 14f;
        const float MaxLifetime = 1.8f;
        const float HitDistance = 0.35f;

        PlayerStats _source;
        EnemyActor _target;
        float _damageMultiplier;
        bool _canApplyFrost;
        bool _pierce;
        float _pierceMultiplier;
        float _speed;
        float _life;
        Vector2 _lastDir = Vector2.right;

        public static void Spawn(
            Vector3 origin,
            EnemyActor target,
            PlayerStats source,
            float damageMultiplier,
            bool canApplyFrost,
            bool pierce = false,
            float pierceMultiplier = 0.5f)
        {
            if (target == null || source == null) return;

            var go = GameFactory.CreateSprite("ArrowProjectile", ArtLibrary.Arrow, origin, 0.72f, 25);
            go.AddComponent<YSortRenderer>().Configure(8);
            var proj = go.AddComponent<ArrowProjectile>();
            proj._source = source;
            proj._target = target;
            proj._damageMultiplier = damageMultiplier;
            proj._canApplyFrost = canApplyFrost;
            proj._pierce = pierce;
            proj._pierceMultiplier = pierceMultiplier;
            proj._speed = DefaultSpeed;
            proj._life = MaxLifetime;

            var toTarget = (Vector2)(target.transform.position - origin);
            if (toTarget.sqrMagnitude > 0.0001f)
                proj._lastDir = toTarget.normalized;
            proj.ApplyFacing(proj._lastDir);
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 aimPoint;
            if (_target != null && _target.IsAlive)
                aimPoint = _target.transform.position;
            else
                aimPoint = (Vector2)transform.position + _lastDir * 2f;

            var pos = (Vector2)transform.position;
            var delta = aimPoint - pos;
            var dist = delta.magnitude;

            if (dist > 0.0001f)
            {
                _lastDir = delta / dist;
                ApplyFacing(_lastDir);
            }

            var step = _speed * Time.deltaTime;
            if (dist <= HitDistance || step >= dist)
            {
                transform.position = aimPoint;
                OnImpact();
                return;
            }

            transform.position = pos + _lastDir * step;
        }

        void OnImpact()
        {
            if (_source != null && _target != null && _target.IsAlive)
            {
                CombatDamage.Apply(_source, _target, _damageMultiplier, canApplyFrost: _canApplyFrost);

                if (_pierce)
                    DamagePierceTarget(_target);
            }

            Destroy(gameObject);
        }

        void DamagePierceTarget(EnemyActor primary)
        {
            if (_source == null || primary == null) return;

            var origin = (Vector2)primary.transform.position;
            EnemyActor best = null;
            var bestProjection = float.MinValue;
            const float pierceRange = 5.5f;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || enemy == primary || !enemy.IsAlive) continue;

                var offset = (Vector2)enemy.transform.position - origin;
                if (offset.sqrMagnitude > pierceRange * pierceRange) continue;

                var projection = Vector2.Dot(offset, _lastDir);
                if (projection <= 0.2f) continue;
                if (projection <= bestProjection) continue;

                bestProjection = projection;
                best = enemy;
            }

            if (best == null) return;

            // Second visible arrow for the pierce target.
            Spawn(
                primary.transform.position,
                best,
                _source,
                _damageMultiplier * _pierceMultiplier,
                _canApplyFrost,
                pierce: false);
        }

        void ApplyFacing(Vector2 dir)
        {
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
