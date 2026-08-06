using System.Collections.Generic;
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
    /// Piercing Shot continues through up to <see cref="PierceMaxTargets"/> enemies.
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
        /// <summary>Piercing Shot: total enemies one arrow can hit.</summary>
        public const int PierceMaxTargets = 5;
        const float PierceSearchRange = 7.5f;

        PlayerStats _source;
        EnemyActor _target;
        float _baseDamageMultiplier;
        bool _canApplyFrost;
        bool _pierce;
        float _pierceMultiplier;
        int _hitsRemaining;
        bool _hasHitOnce;
        float _life;
        float _distanceLeft;
        Vector2 _velocity = Vector2.right;
        Vector2 _aimPoint;
        readonly HashSet<EnemyActor> _alreadyHit = new();
        SpriteRenderer _renderer;

        public static void Spawn(
            Vector3 origin,
            EnemyActor target,
            PlayerStats source,
            float damageMultiplier,
            bool canApplyFrost,
            bool pierce = false,
            float pierceMultiplier = 0.5f,
            int extraPierceHits = 0)
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
            var bonusHits = Mathf.Max(0, extraPierceHits);
            var maxHits = (pierce ? PierceMaxTargets : 1) + bonusHits;
            var canPierce = pierce || bonusHits > 0;

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
            proj._baseDamageMultiplier = damageMultiplier;
            proj._canApplyFrost = canApplyFrost;
            proj._pierce = canPierce;
            proj._pierceMultiplier = pierceMultiplier;
            proj._hitsRemaining = Mathf.Max(1, maxHits);
            proj._hasHitOnce = false;
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
            {
                // Target died mid-flight: chain to next pierce target or coast to expiry.
                if (_pierce && _hitsRemaining > 0)
                    TryContinuePierce(null);
                return;
            }

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
            if (_source != null && _target != null && _target.IsAlive && !_alreadyHit.Contains(_target))
            {
                var mul = _hasHitOnce
                    ? _baseDamageMultiplier * _pierceMultiplier
                    : _baseDamageMultiplier;
                CombatDamage.Apply(_source, _target, mul, canApplyFrost: _canApplyFrost);
                _alreadyHit.Add(_target);
                _hasHitOnce = true;
                _hitsRemaining = Mathf.Max(0, _hitsRemaining - 1);

                if (_pierce && _hitsRemaining > 0 && TryContinuePierce(_target))
                    return;
            }
            else if (_pierce && _hitsRemaining > 0 && TryContinuePierce(_target))
            {
                return;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Retargets the same arrow at the next living enemy along the flight direction.
        /// </summary>
        bool TryContinuePierce(EnemyActor exclude)
        {
            if (_source == null) return false;

            var origin = (Vector2)transform.position;
            var dir = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : Vector2.right;
            EnemyActor best = null;
            var bestProjection = float.MaxValue;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (enemy == exclude || _alreadyHit.Contains(enemy)) continue;

                var offset = (Vector2)enemy.transform.position - origin;
                if (offset.sqrMagnitude > PierceSearchRange * PierceSearchRange) continue;

                var projection = Vector2.Dot(offset, dir);
                if (projection <= 0.25f) continue;

                // Prefer the nearest enemy still ahead of the arrow.
                if (projection >= bestProjection) continue;

                bestProjection = projection;
                best = enemy;
            }

            if (best == null) return false;

            _target = best;
            _aimPoint = (Vector2)best.transform.position + Vector2.up * TorsoOffsetY;
            var toAim = _aimPoint - origin;
            if (toAim.sqrMagnitude > 0.0001f)
            {
                dir = toAim.normalized;
                _velocity = dir * DefaultSpeed;
            }

            _distanceLeft = Vector2.Distance(origin, _aimPoint);
            _life = Mathf.Max(_life, Mathf.Clamp(_distanceLeft / DefaultSpeed + 0.25f, 0.25f, MaxLifetime));
            transform.position = origin + dir * 0.2f;
            return true;
        }
    }
}
