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
        const float RestAngle = -8f;
        const float ThrustAngle = -4f;
        const float ThrustExtend = 0.55f;
        const float WhirlwindRangeMultiplier = 1.15f;

        [SerializeField] float attackRange = 3.4f;
        [SerializeField] float attackInterval = 0.55f;
        [SerializeField] float thrustDuration = 0.24f;
        [SerializeField] float whirlwindDuration = 0.34f;

        float _cooldown;
        float _attackTimer;
        bool _attacking;
        bool _whirlwindSwing;
        bool _whirlwindDamageApplied;
        bool _attackFacingRight = true;
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
                if (!HasEnemyInArc(WhirlwindAttackRange)) return;
                PerformWhirlwind();
                return;
            }

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
            _attackTimer = thrustDuration;
            _attackFacingRight = enemy.transform.position.x >= transform.position.x;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_attackFacingRight;

            CombatDamage.Apply(GetComponent<PlayerStats>(), enemy, canApplyFrost: true);
        }

        void PerformWhirlwind()
        {
            var enemy = FindClosestEnemy();
            _attackFacingRight = enemy == null || enemy.transform.position.x >= transform.position.x;

            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _attacking = true;
            _whirlwindSwing = true;
            _whirlwindDamageApplied = false;
            _attackTimer = whirlwindDuration;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_attackFacingRight;
        }

        void UpdateAttackAnimation()
        {
            if (!_attacking || _spearPivot == null) return;

            _attackTimer -= Time.deltaTime;

            if (_whirlwindSwing)
            {
                var progress = 1f - Mathf.Clamp01(_attackTimer / whirlwindDuration);
                var startAngle = _attackFacingRight ? -90f : 90f;
                var endAngle = _attackFacingRight ? 90f : -90f;
                var angle = Mathf.Lerp(startAngle, endAngle, progress);
                _spearPivot.localScale = new Vector3(_attackFacingRight ? 1f : -1f, 1f, 1f);
                _spearPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (!_whirlwindDamageApplied && progress >= 0.5f)
                {
                    _whirlwindDamageApplied = true;
                    DamageEnemiesInArc(WhirlwindAttackRange);
                }
            }
            else
            {
                var progress = 1f - Mathf.Clamp01(_attackTimer / thrustDuration);
                var eased = Mathf.Sin(progress * Mathf.PI);
                var angle = Mathf.Lerp(RestAngle, ThrustAngle, eased);
                if (!_attackFacingRight) angle = -angle;

                _spearPivot.localScale = new Vector3(_attackFacingRight ? 1f : -1f, 1f, 1f);
                _spearPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (_spearTip != null)
                {
                    var extend = Mathf.Lerp(0f, ThrustExtend, eased);
                    _spearTip.localPosition = new Vector3(0.42f + extend, 0.02f, 0f);
                }
            }

            if (_attackTimer > 0f) return;

            _attacking = false;
            _whirlwindSwing = false;
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
            _spearPivot.localScale = Vector3.one;
            if (_spearTip != null)
                _spearTip.localPosition = new Vector3(0.42f, 0.02f, 0f);
        }

        void DamageEnemiesInArc(float range)
        {
            var stats = GetComponent<PlayerStats>();
            foreach (var enemy in FindEnemiesInArc(range))
                CombatDamage.Apply(stats, enemy, canApplyFrost: true);
        }

        bool HasEnemyInArc(float range)
        {
            return FindEnemiesInArc(range).Count > 0;
        }

        List<EnemyActor> FindEnemiesInArc(float range)
        {
            var hits = new List<EnemyActor>();
            var forward = _attackFacingRight ? Vector2.right : Vector2.left;
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > range * range) continue;
                if (Vector2.Dot(offset.normalized, forward) < 0f) continue;
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