using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>Straight-line bowman arrow (no mid-air tracking) that damages on impact.</summary>
    public class ArrowProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 16f;
        const float MaxLifetime = 1.6f;
        const float HitDistance = 0.4f;
        /// <summary>−25% visual size vs previous 0.72.</summary>
        const float VisualScale = 0.54f;

        PlayerStats _source;
        EnemyActor _target;
        float _damageMultiplier;
        bool _canApplyFrost;
        bool _pierce;
        float _pierceMultiplier;
        float _speed;
        float _life;
        Vector2 _velocity = Vector2.right;

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

            var go = GameFactory.CreateSprite("ArrowProjectile", ArtLibrary.Arrow, origin, VisualScale, 25);
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

            // Aim at torso, not feet, so the shot is level rather than downward-diagonal.
            var aimPoint = (Vector2)target.transform.position + Vector2.up * 0.25f;
            var dir = aimPoint - (Vector2)origin;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;
            proj._velocity = dir.normalized * DefaultSpeed;
            proj.ApplyFacing(proj._velocity);
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var step = _velocity * Time.deltaTime;
            transform.position += (Vector3)step;
            ApplyFacing(_velocity);

            if (_target == null || !_target.IsAlive)
                return;

            var aimPoint = (Vector2)_target.transform.position + Vector2.up * 0.25f;
            if (Vector2.Distance(transform.position, aimPoint) <= HitDistance)
                OnImpact();
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
            var dir = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : Vector2.right;
            EnemyActor best = null;
            var bestProjection = float.MinValue;
            const float pierceRange = 5.5f;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || enemy == primary || !enemy.IsAlive) continue;

                var offset = (Vector2)enemy.transform.position - origin;
                if (offset.sqrMagnitude > pierceRange * pierceRange) continue;

                var projection = Vector2.Dot(offset, dir);
                if (projection <= 0.2f) continue;
                if (projection <= bestProjection) continue;

                bestProjection = projection;
                best = enemy;
            }

            if (best == null) return;

            Spawn(
                primary.transform.position + (Vector3)(dir * 0.2f),
                best,
                _source,
                _damageMultiplier * _pierceMultiplier,
                _canApplyFrost,
                pierce: false);
        }

        void ApplyFacing(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            // Arrow art faces +X; rotate so the tip leads the velocity.
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(VisualScale, VisualScale, 1f);
        }
    }
}
