using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class SpearmanCombat : MonoBehaviour
    {
        /// <summary>~15% more base damage than Batter (1.0).</summary>
        const float DamageMultiplier = 1.15f;
        const float RestAngle = -8f;
        const float ThrustAngle = -4f;
        const float ThrustExtend = 0.55f;
        const float WhirlwindRangeMultiplier = 1.15f;
        /// <summary>Half-width of the standard thrust cone (90° each side of target → 180° total).</summary>
        const float StandardArcHalfDegrees = 90f;

        [SerializeField] float attackRange = 3.4f;
        [SerializeField] float attackInterval = 0.55f;
        [SerializeField] float thrustDuration = 0.24f;
        [SerializeField] float whirlwindDuration = 0.4f;

        float _cooldown;
        float _attackTimer;
        bool _attacking;
        bool _whirlwindSwing;
        bool _whirlwindDamageApplied;
        bool _standardDamageApplied;
        bool _attackFacingRight = true;
        Vector2 _thrustDir = Vector2.right;
        EnemyActor _primaryTarget;
        Transform _spearPivot;
        Transform _spearTip;
        SpriteRenderer _bodyRenderer;

        public bool IsThrusting => _attacking;

        float BaseAttackRange
        {
            get
            {
                var stats = GetComponent<PlayerStats>();
                var rangeMul = stats != null ? stats.RunAttackRangeMultiplier : 1f;
                return attackRange * rangeMul;
            }
        }

        float WhirlwindAttackRange => BaseAttackRange * WhirlwindRangeMultiplier;

        bool UseWhirlwind =>
            GameSave.GetSelectedAttackMode(PlayerClass.Spearman) == AttackMode.Whirlwind
            && GameSave.WhirlwindUnlocked;

        void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            SetupSpear();
        }

        void SetupSpear()
        {
            var pivotGo = new GameObject("SpearPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(0.08f, -0.28f, 0f);
            _spearPivot = pivotGo.transform;

            var spearGo = new GameObject("Spear");
            spearGo.transform.SetParent(_spearPivot, false);
            spearGo.transform.localPosition = new Vector3(0.42f, 0.02f, 0f);
            spearGo.transform.localScale = Vector3.one * 0.9f;

            var spearRenderer = spearGo.AddComponent<SpriteRenderer>();
            spearRenderer.sprite = ArtLibrary.Spear;
            spearGo.AddComponent<YSortRenderer>().Configure(1);

            _spearTip = spearGo.transform;
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
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

            if (UseWhirlwind)
            {
                if (!HasEnemyInRange(WhirlwindAttackRange)) return;
                PerformWhirlwind();
                return;
            }

            // Can lock any enemy in full range; damage only applies in the 180° facing arc.
            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > BaseAttackRange) return;

            PerformThrust(enemy);
        }

        void PerformThrust(EnemyActor enemy)
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _attacking = true;
            _whirlwindSwing = false;
            _standardDamageApplied = false;
            _attackTimer = thrustDuration;
            _primaryTarget = enemy;

            var toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
            _thrustDir = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : Vector2.right;
            _attackFacingRight = _thrustDir.x >= 0f;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_attackFacingRight;
        }

        void PerformWhirlwind()
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _attacking = true;
            _whirlwindSwing = true;
            _whirlwindDamageApplied = false;
            _attackTimer = whirlwindDuration;
            _attackFacingRight = true;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = false;

            if (_spearPivot != null)
                _spearPivot.localScale = Vector3.one;
        }

        void UpdateAttackAnimation()
        {
            if (!_attacking || _spearPivot == null) return;

            _attackTimer -= Time.deltaTime;

            if (_whirlwindSwing)
            {
                var progress = 1f - Mathf.Clamp01(_attackTimer / whirlwindDuration);
                var angle = Mathf.Lerp(0f, 360f, progress);
                _spearPivot.localScale = Vector3.one;
                _spearPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (!_whirlwindDamageApplied && progress >= 0.5f)
                {
                    _whirlwindDamageApplied = true;
                    DamageEnemiesInFullRange(WhirlwindAttackRange);
                }
            }
            else
            {
                var progress = 1f - Mathf.Clamp01(_attackTimer / thrustDuration);
                var eased = Mathf.Sin(progress * Mathf.PI);
                var faceAngle = Mathf.Atan2(_thrustDir.y, _thrustDir.x) * Mathf.Rad2Deg;
                var swing = Mathf.Lerp(RestAngle, ThrustAngle, eased);
                _spearPivot.localScale = Vector3.one;
                _spearPivot.localRotation = Quaternion.Euler(0f, 0f, faceAngle + swing);

                if (_spearTip != null)
                {
                    var extend = Mathf.Lerp(0f, ThrustExtend, eased);
                    _spearTip.localPosition = new Vector3(0.42f + extend, 0.02f, 0f);
                }

                if (!_standardDamageApplied && progress >= 0.45f)
                {
                    _standardDamageApplied = true;
                    DamageEnemiesInFrontArc(BaseAttackRange);
                }
            }

            if (_attackTimer > 0f) return;

            _attacking = false;
            _whirlwindSwing = false;
            _primaryTarget = null;
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
            _spearPivot.localScale = Vector3.one;
            if (_spearTip != null)
                _spearTip.localPosition = new Vector3(0.42f, 0.02f, 0f);
        }

        void DamageEnemiesInFullRange(float range)
        {
            var stats = GetComponent<PlayerStats>();
            foreach (var enemy in FindEnemiesInRange(range, useFrontArc: false))
                CombatDamage.Apply(stats, enemy, DamageMultiplier, canApplyFrost: true);
        }

        /// <summary>
        /// Standard thrust: damages the locked target plus any other living enemies inside a
        /// 180° cone (90° left/right of the thrust direction toward the primary target).
        /// </summary>
        void DamageEnemiesInFrontArc(float range)
        {
            var stats = GetComponent<PlayerStats>();
            var hitPrimary = false;
            foreach (var enemy in FindEnemiesInRange(range, useFrontArc: true))
            {
                CombatDamage.Apply(stats, enemy, DamageMultiplier, canApplyFrost: true);
                if (enemy == _primaryTarget) hitPrimary = true;
            }

            // Always credit the locked target if still in range (handles edge float error).
            if (!hitPrimary && _primaryTarget != null && _primaryTarget.IsAlive)
            {
                var dist = Vector2.Distance(transform.position, _primaryTarget.transform.position);
                if (dist <= range)
                    CombatDamage.Apply(stats, _primaryTarget, DamageMultiplier, canApplyFrost: true);
            }
        }

        bool HasEnemyInRange(float range) => FindEnemiesInRange(range, useFrontArc: false).Count > 0;

        List<EnemyActor> FindEnemiesInRange(float range, bool useFrontArc)
        {
            var hits = new List<EnemyActor>();
            var rangeSq = range * range;
            var facing = _thrustDir.sqrMagnitude > 0.0001f ? _thrustDir.normalized : Vector2.right;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > rangeSq) continue;

                if (useFrontArc)
                {
                    // Primary lock is always valid; others must sit inside the 180° facing cone.
                    if (enemy != _primaryTarget)
                    {
                        if (offset.sqrMagnitude <= 0.0001f) continue;
                        if (Vector2.Angle(facing, offset) > StandardArcHalfDegrees) continue;
                    }
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
