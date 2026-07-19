using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Katana class: double 180° arc swipe by default (2 hits), whirlwind upgrades to 3 hits.
    /// Per-hit damage is ~30% lower than Batter.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class SamuraiCombat : MonoBehaviour
    {
        /// <summary>~30% less damage than Batter per attack line.</summary>
        const float DamageMultiplier = 0.7f;
        const float RestAngle = -70f;
        const float SwingAngle = 75f;
        /// <summary>Half-width of the slash cone (90° each side → 180° total).</summary>
        const float ArcHalfDegrees = 90f;

        [SerializeField] float attackRange = 2.45f;
        [SerializeField] float attackInterval = 0.58f;
        [SerializeField] float doubleSwipeDuration = 0.36f;
        [SerializeField] float tripleSwipeDuration = 0.48f;

        float _cooldown;
        float _attackTimer;
        float _attackDuration;
        bool _attacking;
        bool _useTripleHits;
        int _hitsApplied;
        int _hitsPlanned;
        bool _attackFacingRight = true;
        Vector2 _slashDir = Vector2.right;
        EnemyActor _primaryTarget;
        Transform _katanaPivot;
        SpriteRenderer _bodyRenderer;

        public bool IsSwiping => _attacking;

        float BaseAttackRange
        {
            get
            {
                var stats = GetComponent<PlayerStats>();
                var rangeMul = stats != null ? stats.RunAttackRangeMultiplier : 1f;
                return attackRange * rangeMul;
            }
        }

        bool UseTripleSlash =>
            GameSave.GetSelectedAttackMode(PlayerClass.Samurai) == AttackMode.Whirlwind
            && GameSave.WhirlwindUnlocked;

        void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            SetupKatana();
        }

        void SetupKatana()
        {
            var pivotGo = new GameObject("KatanaPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(0.1f, -0.3f, 0f);
            _katanaPivot = pivotGo.transform;

            var bladeGo = new GameObject("Katana");
            bladeGo.transform.SetParent(_katanaPivot, false);
            bladeGo.transform.localPosition = new Vector3(0.28f, 0.02f, 0f);
            bladeGo.transform.localScale = Vector3.one * 0.85f;

            var bladeRenderer = bladeGo.AddComponent<SpriteRenderer>();
            bladeRenderer.sprite = ArtLibrary.Katana;
            bladeGo.AddComponent<YSortRenderer>().Configure(1);

            _katanaPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            UpdateAttackAnimation();
            if (_attacking) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.EffectiveAttackSpeed : 1f;
            _cooldown -= Time.deltaTime * attackSpeed;
            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > BaseAttackRange) return;

            PerformSlash(enemy);
        }

        void PerformSlash(EnemyActor enemy)
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _attacking = true;
            _useTripleHits = UseTripleSlash;
            _hitsPlanned = _useTripleHits ? 3 : 2;
            _hitsApplied = 0;
            _attackDuration = _useTripleHits ? tripleSwipeDuration : doubleSwipeDuration;
            _attackTimer = _attackDuration;
            _primaryTarget = enemy;

            var toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
            _slashDir = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : Vector2.right;
            _attackFacingRight = _slashDir.x >= 0f;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_attackFacingRight;
        }

        void UpdateAttackAnimation()
        {
            if (!_attacking || _katanaPivot == null) return;

            _attackTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(_attackTimer / Mathf.Max(0.001f, _attackDuration));

            ApplySwipeAngle(progress);
            TryApplyHits(progress);

            if (_attackTimer > 0f) return;

            // Catch any remaining hits if animation ended early.
            while (_hitsApplied < _hitsPlanned)
                ApplyArcHit();

            _attacking = false;
            _primaryTarget = null;
            _katanaPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
            _katanaPivot.localScale = Vector3.one;
        }

        void ApplySwipeAngle(float progress)
        {
            // Double swipe: two full half-swings. Triple: three half-swings.
            var segments = _hitsPlanned;
            var segment = Mathf.Min(segments - 1, Mathf.FloorToInt(progress * segments));
            var local = Mathf.Clamp01(progress * segments - segment);
            var eased = Mathf.Sin(local * Mathf.PI);
            // Alternate slash direction each segment for a readable multi-swipe.
            var dir = segment % 2 == 0 ? 1f : -1f;
            var faceAngle = Mathf.Atan2(_slashDir.y, _slashDir.x) * Mathf.Rad2Deg;
            var swing = Mathf.Lerp(RestAngle, SwingAngle * dir, eased);

            _katanaPivot.localScale = Vector3.one;
            _katanaPivot.localRotation = Quaternion.Euler(0f, 0f, faceAngle + swing);
        }

        void TryApplyHits(float progress)
        {
            // Fire each hit near the peak of its swipe segment.
            for (var i = 0; i < _hitsPlanned; i++)
            {
                if (_hitsApplied > i) continue;
                var peak = (i + 0.5f) / _hitsPlanned;
                if (progress + 0.001f < peak) break;
                ApplyArcHit();
            }
        }

        void ApplyArcHit()
        {
            _hitsApplied++;
            if (_hitsApplied > 1)
                AudioManager.Instance?.PlaySwingSfx();

            var stats = GetComponent<PlayerStats>();
            var hitPrimary = false;
            foreach (var enemy in FindEnemiesInArc(BaseAttackRange))
            {
                CombatDamage.Apply(stats, enemy, DamageMultiplier, canApplyFrost: true);
                if (enemy == _primaryTarget) hitPrimary = true;
            }

            if (!hitPrimary && _primaryTarget != null && _primaryTarget.IsAlive)
            {
                var dist = Vector2.Distance(transform.position, _primaryTarget.transform.position);
                if (dist <= BaseAttackRange)
                    CombatDamage.Apply(stats, _primaryTarget, DamageMultiplier, canApplyFrost: true);
            }
        }

        List<EnemyActor> FindEnemiesInArc(float range)
        {
            var hits = new List<EnemyActor>();
            var rangeSq = range * range;
            var facing = _slashDir.sqrMagnitude > 0.0001f ? _slashDir.normalized : Vector2.right;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > rangeSq) continue;

                if (enemy != _primaryTarget)
                {
                    if (offset.sqrMagnitude <= 0.0001f) continue;
                    if (Vector2.Angle(facing, offset) > ArcHalfDegrees) continue;
                }

                hits.Add(enemy);
            }

            return hits;
        }

        EnemyActor FindClosestEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var d = Vector2.Distance(transform.position, enemy.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = enemy;
                }
            }

            return best;
        }
    }
}
