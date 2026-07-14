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

        [SerializeField] float attackRange = 2.15f;
        [SerializeField] float attackInterval = 0.5f;
        [SerializeField] float swingDuration = 0.22f;

        float _cooldown;
        float _swingTimer;
        bool _swinging;
        bool _swingFacingRight = true;
        Transform _batPivot;
        SpriteRenderer _bodyRenderer;

        public bool IsSwinging => _swinging;

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

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > attackRange) return;

            PerformAttack(enemy);
        }

        void PerformAttack(EnemyActor enemy)
        {
            _cooldown = attackInterval;
            _swinging = true;
            _swingTimer = swingDuration;
            _swingFacingRight = enemy.transform.position.x >= transform.position.x;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = !_swingFacingRight;

            var stats = GetComponent<PlayerStats>();
            enemy.TakeDamage(Mathf.RoundToInt(stats.Damage));
        }

        void UpdateSwingAnimation()
        {
            if (!_swinging || _batPivot == null) return;

            _swingTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(_swingTimer / swingDuration);
            var eased = Mathf.Sin(progress * Mathf.PI);
            var angle = Mathf.Lerp(RestAngle, SwingAngle, eased);
            if (!_swingFacingRight) angle = -angle;

            _batPivot.localScale = new Vector3(_swingFacingRight ? 1f : -1f, 1f, 1f);
            _batPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (_swingTimer > 0f) return;

            _swinging = false;
            _batPivot.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
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