using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Straight-line bowman arrow. Sprite is horizontal tip-on-+X; rotation follows velocity only.
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 16f;
        const float MaxLifetime = 1.6f;
        const float HitDistance = 0.45f;
        /// <summary>−25% visual size vs previous 0.72.</summary>
        const float VisualScale = 0.54f;

        PlayerStats _source;
        EnemyActor _target;
        float _damageMultiplier;
        bool _canApplyFrost;
        bool _pierce;
        float _pierceMultiplier;
        float _life;
        Vector2 _velocity = Vector2.right;
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

            // Flat aim at same Y as spawn so shots do not read as “always diagonal”
            // when the enemy center sits below chest height.
            var spawn = origin;
            var aim = new Vector2(target.transform.position.x, spawn.y);
            var dir = aim - (Vector2)spawn;
            if (dir.sqrMagnitude < 0.0001f)
                dir = target.transform.position.x >= spawn.x ? Vector2.right : Vector2.left;
            dir.Normalize();

            var go = new GameObject("ArrowProjectile");
            go.transform.position = spawn;
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
            proj._life = MaxLifetime;
            proj._velocity = dir * DefaultSpeed;
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

            transform.position += (Vector3)(_velocity * Time.deltaTime);

            // Keep tip aligned with velocity (horizontal art ⇒ Atan2 is correct).
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (_target == null || !_target.IsAlive)
                return;

            // Hit when near the target’s X/Y (slight vertical forgiveness for body size).
            var to = (Vector2)_target.transform.position - (Vector2)transform.position;
            if (to.sqrMagnitude <= HitDistance * HitDistance)
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
                primary.transform.position + (Vector3)(dir * 0.25f),
                best,
                _source,
                _damageMultiplier * _pierceMultiplier,
                _canApplyFrost,
                pierce: false);
        }
    }
}
