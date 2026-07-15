using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class SpearmanCombat : MonoBehaviour
    {
        const float RestAngle = -8f;
        const float ThrustAngle = -4f;
        const float ThrustExtend = 0.55f;

        [SerializeField] float attackRange = 3.4f;
        [SerializeField] float attackInterval = 0.55f;
        [SerializeField] float thrustDuration = 0.24f;

        float _cooldown;
        float _thrustTimer;
        bool _thrusting;
        bool _thrustFacingRight = true;
        Transform _spearPivot;
        Transform _spearTip;
        SpriteRenderer _bodyRenderer;

        public bool IsThrusting => _thrusting;
        public float AttackRange
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
            spearRenderer.sortingOrder = 11;

            _spearTip = spearGo.transform;
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            UpdateThrustAnimation();
            if (_thrusting) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.RunAttackSpeedMultiplier : 1f;
            _cooldown -= Time.deltaTime * attackSpeed;
            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > AttackRange) return;

            PerformThrust(enemy);
        }

        void PerformThrust(EnemyActor enemy)
        {
            _cooldown = attackInterval;
            _thrusting = true;
            _thrustTimer = thrustDuration;
            _thrustFacingRight = enemy.transform.position.x >= transform.position.x;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_thrustFacingRight;

            var stats = GetComponent<PlayerStats>();
            var damage = Mathf.RoundToInt(stats != null ? stats.Damage : 10f);
            enemy.TakeDamage(damage);
        }

        void UpdateThrustAnimation()
        {
            if (!_thrusting || _spearPivot == null) return;

            _thrustTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(_thrustTimer / thrustDuration);
            var eased = Mathf.Sin(progress * Mathf.PI);
            var angle = Mathf.Lerp(RestAngle, ThrustAngle, eased);
            if (!_thrustFacingRight) angle = -angle;

            _spearPivot.localScale = new Vector3(_thrustFacingRight ? 1f : -1f, 1f, 1f);
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (_spearTip != null)
            {
                var extend = Mathf.Lerp(0f, ThrustExtend, eased);
                _spearTip.localPosition = new Vector3(0.42f + extend, 0.02f, 0f);
            }

            if (_thrustTimer > 0f) return;

            _thrusting = false;
            _spearPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
            _spearPivot.localScale = Vector3.one;
            if (_spearTip != null)
                _spearTip.localPosition = new Vector3(0.42f, 0.02f, 0f);
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