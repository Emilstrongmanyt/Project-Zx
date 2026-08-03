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
        /// <summary>Standard single-target jab: +40% base damage.</summary>
        const float StandardDamageBonus = 1.4f;
        const float RestAngle = -8f;
        const float ThrustAngle = -4f;
        const float ThrustExtend = 0.55f;
        const float WhirlwindRangeMultiplier = 1.15f;
        /// <summary>Whirlwind: half-width of the 180° arc (90° each side of facing).</summary>
        const float WhirlwindArcHalfDegrees = 90f;

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
                var rangeMul = stats != null ? stats.AttackRangeMultiplier : GameSave.AttackRangeMultiplier;
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
            spearGo.transform.localPosition = new Vector3(0.38f, 0.02f, 0f);
            // Weapon sprites are authored at combat world-length; keep near 1× under player scale.
            spearGo.transform.localScale = Vector3.one;

            var spearRenderer = spearGo.AddComponent<SpriteRenderer>();
            spearRenderer.sprite = ArtLibrary.Spear;
            spearRenderer.sortingOrder = 20;
            spearGo.AddComponent<YSortRenderer>().Configure(3);

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

            // Standard: single-target jab on the closest enemy in range.
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

            // Face the nearest enemy so the 180° arc sweeps the front.
            var nearest = FindClosestEnemy();
            if (nearest != null)
            {
                var to = (Vector2)nearest.transform.position - (Vector2)transform.position;
                _thrustDir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
            }
            else
            {
                _thrustDir = Vector2.right;
            }

            _attackFacingRight = _thrustDir.x >= 0f;
            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_attackFacingRight;

            if (_spearPivot != null)
                _spearPivot.localScale = Vector3.one;
        }

        void UpdateAttackAnimation()
        {
            if (!_attacking || _spearPivot == null) return;

            _attackTimer -= Time.deltaTime;

            if (_whirlwindSwing)
            {
                // 180° sweep in front of the hero (not a full 360 spin).
                var progress = 1f - Mathf.Clamp01(_attackTimer / whirlwindDuration);
                var faceAngle = Mathf.Atan2(_thrustDir.y, _thrustDir.x) * Mathf.Rad2Deg;
                var angle = faceAngle + Mathf.Lerp(-90f, 90f, progress);
                _spearPivot.localScale = Vector3.one;
                _spearPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (!_whirlwindDamageApplied && progress >= 0.5f)
                {
                    _whirlwindDamageApplied = true;
                    DamageEnemiesInWhirlwindArc(WhirlwindAttackRange);
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
                    DamagePrimaryTarget(BaseAttackRange);
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

        /// <summary>Standard jab: only the locked primary target.</summary>
        void DamagePrimaryTarget(float range)
        {
            if (_primaryTarget == null || !_primaryTarget.IsAlive) return;
            var dist = Vector2.Distance(transform.position, _primaryTarget.transform.position);
            if (dist > range) return;
            CombatDamage.Apply(
                GetComponent<PlayerStats>(),
                _primaryTarget,
                DamageMultiplier * StandardDamageBonus,
                canApplyFrost: true);
        }

        /// <summary>Whirlwind: 180° front arc cleave.</summary>
        void DamageEnemiesInWhirlwindArc(float range)
        {
            var stats = GetComponent<PlayerStats>();
            var rangeSq = range * range;
            var facing = _thrustDir.sqrMagnitude > 0.0001f ? _thrustDir.normalized : Vector2.right;

            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > rangeSq) continue;
                if (offset.sqrMagnitude > 0.0001f && Vector2.Angle(facing, offset) > WhirlwindArcHalfDegrees)
                    continue;
                CombatDamage.Apply(stats, enemy, DamageMultiplier, canApplyFrost: true);
            }
        }

        bool HasEnemyInRange(float range)
        {
            var rangeSq = range * range;
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude <= rangeSq)
                    return true;
            }

            return false;
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
