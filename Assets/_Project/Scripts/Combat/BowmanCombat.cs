using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class BowmanCombat : MonoBehaviour
    {
        const float DamageMultiplier = 0.9f;
        const float PierceSecondaryMultiplier = 0.5f;

        [SerializeField] float attackRange = 5.5f;
        [SerializeField] float attackInterval = 0.65f;
        [SerializeField] float drawDuration = 0.26f;

        float _cooldown;
        float _drawTimer;
        bool _drawing;
        bool _drawFacingRight = true;
        Transform _bowPivot;
        SpriteRenderer _bodyRenderer;

        public bool IsDrawing => _drawing;

        float AttackRange
        {
            get
            {
                var stats = GetComponent<PlayerStats>();
                var rangeMul = stats != null ? stats.RunAttackRangeMultiplier : 1f;
                return attackRange * rangeMul;
            }
        }

        void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            SetupBow();
        }

        void SetupBow()
        {
            var pivotGo = new GameObject("BowPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(0.06f, -0.22f, 0f);
            _bowPivot = pivotGo.transform;

            var bowGo = new GameObject("Bow");
            bowGo.transform.SetParent(_bowPivot, false);
            bowGo.transform.localPosition = new Vector3(0.18f, 0.04f, 0f);
            bowGo.transform.localScale = Vector3.one * 0.85f;

            var bowRenderer = bowGo.AddComponent<SpriteRenderer>();
            bowRenderer.sprite = ArtLibrary.Bow;
            bowRenderer.sortingOrder = 11;

            _bowPivot.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            UpdateDrawAnimation();
            if (_drawing) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.RunAttackSpeedMultiplier : 1f;
            _cooldown -= Time.deltaTime * attackSpeed;
            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > AttackRange) return;

            PerformShot(enemy);
        }

        void PerformShot(EnemyActor enemy)
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _drawing = true;
            _drawTimer = drawDuration;
            _drawFacingRight = enemy.transform.position.x >= transform.position.x;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_drawFacingRight;

            var stats = GetComponent<PlayerStats>();
            var damage = Mathf.RoundToInt((stats != null ? stats.Damage : 10f) * DamageMultiplier);
            enemy.TakeDamage(damage);

            if (GameSave.PiercingShotUnlocked)
                DamagePierceTarget(enemy, damage);
        }

        void DamagePierceTarget(EnemyActor primary, int primaryDamage)
        {
            var direction = ((Vector2)primary.transform.position - (Vector2)transform.position).normalized;
            EnemyActor best = null;
            var bestProjection = float.MinValue;

            foreach (var enemy in FindAllEnemies())
            {
                if (enemy == null || enemy == primary || !enemy.IsAlive) continue;

                var offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > AttackRange * AttackRange) continue;

                var projection = Vector2.Dot(offset, direction);
                if (projection <= 0.35f) continue;

                if (projection <= bestProjection) continue;
                bestProjection = projection;
                best = enemy;
            }

            if (best == null) return;
            best.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(primaryDamage * PierceSecondaryMultiplier)));
        }

        void UpdateDrawAnimation()
        {
            if (!_drawing || _bowPivot == null) return;

            _drawTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(_drawTimer / drawDuration);
            var eased = Mathf.Sin(progress * Mathf.PI);
            var angle = Mathf.Lerp(-12f, 18f, eased);
            if (!_drawFacingRight) angle = -angle;

            _bowPivot.localScale = new Vector3(_drawFacingRight ? 1f : -1f, 1f, 1f);
            _bowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (_drawTimer > 0f) return;

            _drawing = false;
            _bowPivot.localRotation = Quaternion.Euler(0f, 0f, -12f);
            _bowPivot.localScale = Vector3.one;
        }

        static List<EnemyActor> FindAllEnemies()
        {
            var hits = new List<EnemyActor>();
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy != null && enemy.IsAlive)
                    hits.Add(enemy);
            }

            return hits;
        }

        EnemyActor FindClosestEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            foreach (var enemy in FindAllEnemies())
            {
                var d = Vector2.Distance(transform.position, enemy.transform.position);
                if (d >= bestDist) continue;
                bestDist = d;
                best = enemy;
            }

            return best;
        }
    }
}