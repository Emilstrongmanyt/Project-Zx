using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] float attackRange = 1.35f;
        [SerializeField] float attackInterval = 0.55f;
        float _cooldown;
        SpriteRenderer _renderer;
        Sprite _attackSprite;
        Sprite _idleSprite;
        float _attackAnimTimer;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _attackSprite = ArtLibrary.PlayerAttack;
            _idleSprite = ArtLibrary.PlayerIdle;
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            _cooldown -= Time.deltaTime;
            if (_attackAnimTimer > 0f)
            {
                _attackAnimTimer -= Time.deltaTime;
                if (_attackAnimTimer <= 0f && _renderer != null)
                    _renderer.sprite = _idleSprite;
            }

            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > attackRange) return;

            _cooldown = attackInterval;
            _attackAnimTimer = 0.2f;
            if (_renderer != null) _renderer.sprite = _attackSprite;

            var stats = GetComponent<PlayerStats>();
            enemy.GetComponent<EnemyActor>().TakeDamage(Mathf.RoundToInt(stats.Damage));
        }

        EnemyActor FindClosestEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            var enemies = Object.FindObjectsByType<EnemyActor>();
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