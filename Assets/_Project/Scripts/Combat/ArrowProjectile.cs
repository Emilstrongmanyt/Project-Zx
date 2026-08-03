using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Straight-line bowman arrow (no mid-air homing). Horizontal tip-+X art; rotation
    /// follows velocity so slight aim angles look natural rather than permanently diagonal.
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 16f;
        const float MaxLifetime = 1.6f;
        /// <summary>Generous enough for scaled sanctum demons; aim point is torso, not feet.</summary>
        const float HitDistance = 0.7f;
        /// <summary>−25% visual size vs previous 0.72.</summary>
        const float VisualScale = 0.54f;
        const float TorsoOffsetY = 0.28f;

        PlayerStats _source;
        EnemyActor _target;
        float _damageMultiplier;
        bool _canApplyFrost;
        bool _pierce;
        float _pierceMultiplier;
        float _life;
        float _distanceLeft;
        Vector2 _velocity = Vector2.right;
        Vector2 _aimPoint;
        SpriteRenderer _renderer;

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

            // Aim at torso so shots track enemies above/below the hero, not only same-Y X.
            var spawn = (Vector2)origin;
            var aim = (Vector2)target.transform.position + Vector2.up * TorsoOffsetY;
            var dir = aim - spawn;
            if (dir.sqrMagnitude < 0.0001f)
                dir = target.transform.position.x >= spawn.x ? Vector2.right : Vector2.left;
            else
                dir.Normalize();

            var dist = Vector2.Distance(spawn, aim);

            var go = new GameObject("ArrowProjectile");
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * VisualScale;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.Arrow;
            sr.sortingOrder = 25;
            go.AddComponent<YSortRenderer>().Configure(8);

            var proj = go.AddComponent<ArrowProjectile>();
            proj._source = source;
            proj._target = target;
            proj._damageMultiplier = damageMultiplier;
            proj._canApplyFrost = canApplyFrost;
            proj._pierce = pierce;
            proj._pierceMultiplier = pierceMultiplier;
            proj._life = Mathf.Clamp(dist / DefaultSpeed + 0.35f, 0.35f, MaxLifetime);
            proj._velocity = dir * DefaultSpeed;
            proj._aimPoint = aim;
            proj._distanceLeft = dist;
            proj._renderer = sr;
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
            var stepLen = step.magnitude;

            // Reach / pass aim point this frame → impact (avoids overshoot misses).
            if (stepLen >= _distanceLeft)
            {
                transform.position = _aimPoint;
                OnImpact();
                return;
            }

            transform.position += (Vector3)step;
            _distanceLeft -= stepLen;

            if (_velocity.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (_target == null || !_target.IsAlive)
                return;

            // Live aim follows a moving target; still straight-line velocity (no homing turn).
            var liveAim = (Vector2)_target.transform.position + Vector2.up * TorsoOffsetY;
            if (Vector2.Distance(transform.position, liveAim) <= HitDistance
                || Vector2.Distance(transform.position, _aimPoint) <= HitDistance * 0.65f)
            {
                OnImpact();
            }
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
                primary.transform.position + (Vector3)(dir * 0.25f),
                best,
                _source,
                _damageMultiplier * _pierceMultiplier,
                _canApplyFrost,
                pierce: false);
        }
    }
}
