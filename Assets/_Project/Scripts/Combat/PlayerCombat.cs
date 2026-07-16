using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        const float RestAngle = -65f;
        const float SwingAngle = 75f;
        const float WhirlwindRangeMultiplier = 1.2f;

        [SerializeField] float attackRange = 2.15f;
        [SerializeField] float attackInterval = 0.5f;
        [SerializeField] float swingDuration = 0.22f;
        [SerializeField] float whirlwindDuration = 0.34f;

        float _cooldown;
        float _swingTimer;
        bool _swinging;
        bool _whirlwindSwing;
        bool _whirlwindDamageApplied;
        bool _swingFacingRight = true;
        Transform _batPivot;
        SpriteRenderer _bodyRenderer;

        public bool IsSwinging => _swinging;

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
            GameSave.GetSelectedAttackMode(PlayerClass.Batter) == AttackMode.Whirlwind
            && GameSave.WhirlwindUnlocked;

        void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            SetupBat();
        }

        void SetupBat()
        {
            var pivotGo = new GameObject("BatPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(0.1f, -0.34f, 0f);
            _batPivot = pivotGo.transform;

            var batGo = new GameObject("BaseballBat");
            batGo.transform.SetParent(_batPivot, false);
            batGo.transform.localPosition = new Vector3(0.12f, -0.02f, 0f);
            batGo.transform.localScale = Vector3.one * 0.75f;

            var batRenderer = batGo.AddComponent<SpriteRenderer>();
            batRenderer.sprite = ArtLibrary.BaseballBat;
            batRenderer.sortingOrder = 11;

            _batPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            UpdateSwingAnimation();
            if (_swinging) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.RunAttackSpeedMultiplier : 1f;
            _cooldown -= Time.deltaTime * attackSpeed;
            if (_cooldown > 0f) return;

            if (UseWhirlwind)
            {
                if (!HasEnemyInRange(WhirlwindAttackRange)) return;
                PerformWhirlwind();
                return;
            }

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > BaseAttackRange) return;

            PerformAttack(enemy);
        }

        void PerformAttack(EnemyActor enemy)
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _swinging = true;
            _whirlwindSwing = false;
            _whirlwindDamageApplied = false;
            _swingTimer = swingDuration;
            _swingFacingRight = enemy.transform.position.x >= transform.position.x;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_swingFacingRight;

            var stats = GetComponent<PlayerStats>();
            var damage = Mathf.RoundToInt(stats != null ? stats.Damage : 10f);
            enemy.TakeDamage(damage);
        }

        void PerformWhirlwind()
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _swinging = true;
            _whirlwindSwing = true;
            _whirlwindDamageApplied = false;
            _swingTimer = whirlwindDuration;
            _swingFacingRight = true;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = false;

            if (_batPivot != null)
                _batPivot.localScale = Vector3.one;
        }

        void UpdateSwingAnimation()
        {
            if (!_swinging || _batPivot == null) return;

            _swingTimer -= Time.deltaTime;

            if (_whirlwindSwing)
            {
                var progress = 1f - Mathf.Clamp01(_swingTimer / whirlwindDuration);
                var angle = Mathf.Lerp(0f, 360f, progress);
                _batPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (!_whirlwindDamageApplied && progress >= 0.55f)
                {
                    _whirlwindDamageApplied = true;
                    DamageEnemiesInRange(WhirlwindAttackRange);
                }
            }
            else
            {
                var progress = 1f - Mathf.Clamp01(_swingTimer / swingDuration);
                var eased = Mathf.Sin(progress * Mathf.PI);
                var angle = Mathf.Lerp(RestAngle, SwingAngle, eased);
                if (!_swingFacingRight) angle = -angle;

                _batPivot.localScale = new Vector3(_swingFacingRight ? 1f : -1f, 1f, 1f);
                _batPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (_swingTimer > 0f) return;

            _swinging = false;
            _whirlwindSwing = false;
            _batPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
            _batPivot.localScale = Vector3.one;
        }

        void DamageEnemiesInRange(float range)
        {
            var stats = GetComponent<PlayerStats>();
            var damage = Mathf.RoundToInt(stats != null ? stats.Damage : 10f);
            foreach (var enemy in FindEnemiesInRange(range))
                enemy.TakeDamage(damage);
        }

        bool HasEnemyInRange(float range)
        {
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyActor>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(transform.position, enemy.transform.position) <= range)
                    return true;
            }

            return false;
        }

        List<EnemyActor> FindEnemiesInRange(float range)
        {
            var hits = new List<EnemyActor>();
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyActor>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(transform.position, enemy.transform.position) <= range)
                    hits.Add(enemy);
            }

            return hits;
        }

        EnemyActor FindClosestEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyActor>();
            foreach (var enemy in enemies)
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