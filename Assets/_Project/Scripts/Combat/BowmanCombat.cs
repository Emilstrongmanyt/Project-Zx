using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
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
        Transform _bowVisual;

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
            pivotGo.transform.localPosition = new Vector3(0.08f, -0.18f, 0f);
            _bowPivot = pivotGo.transform;

            var bowGo = new GameObject("Bow");
            bowGo.transform.SetParent(_bowPivot, false);
            bowGo.transform.localPosition = new Vector3(0.22f, 0.06f, 0f);
            // Large enough to read clearly over the hero sprite.
            bowGo.transform.localScale = Vector3.one * 1.55f;
            _bowVisual = bowGo.transform;

            var bowRenderer = bowGo.AddComponent<SpriteRenderer>();
            bowRenderer.sprite = ArtLibrary.Bow;
            bowRenderer.color = Color.white;
            bowGo.AddComponent<YSortRenderer>().Configure(3);

            _bowPivot.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            UpdateDrawAnimation();
            if (_drawing) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.EffectiveAttackSpeed : 1f;
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
            var origin = GetArrowSpawnPoint();
            var pierce = GameSave.GetSelectedAttackMode(PlayerClass.Bowman) == AttackMode.PiercingShot
                         && GameSave.PiercingShotUnlocked;

            ArrowProjectile.Spawn(
                origin,
                enemy,
                stats,
                DamageMultiplier,
                canApplyFrost: true,
                pierce: pierce,
                pierceMultiplier: PierceSecondaryMultiplier);
        }

        Vector3 GetArrowSpawnPoint()
        {
            if (_bowVisual != null)
                return _bowVisual.position + (_drawFacingRight ? Vector3.right : Vector3.left) * 0.35f;
            return transform.position + new Vector3(_drawFacingRight ? 0.45f : -0.45f, 0.1f, 0f);
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
