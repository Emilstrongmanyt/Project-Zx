using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyActor : MonoBehaviour
    {
        public bool IsAlive { get; private set; } = true;
        public bool IsBoss { get; private set; }

        int _hp;
        int _attack;
        float _speed;
        int _round;
        Transform _player;
        Rigidbody2D _rb;
        float _contactCooldown;

        public void Initialize(int round, bool isBoss)
        {
            _round = round;
            IsBoss = isBoss;
            _hp = isBoss ? 180 + round * 25 : 18 + round * 6;
            _attack = isBoss ? 18 + round : 6 + Mathf.FloorToInt(round * 0.6f);
            _speed = isBoss ? 1.6f + round * 0.04f : 1.2f + round * 0.07f;
            _rb = GetComponent<Rigidbody2D>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        void FixedUpdate()
        {
            if (!IsAlive || _player == null) return;
            var dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * _speed;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && dir.x != 0f) renderer.flipX = dir.x < 0f;
        }

        void Update()
        {
            if (!IsAlive || _player == null) return;
            _contactCooldown -= Time.deltaTime;
            if (_contactCooldown > 0f) return;
            if (Vector2.Distance(transform.position, _player.position) > 0.75f) return;

            var stats = _player.GetComponent<PlayerStats>();
            if (stats != null && !stats.IsDead)
            {
                stats.TakeDamage(_attack);
                _contactCooldown = 0.8f;
            }
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            _hp -= amount;
            if (_hp <= 0) Die();
        }

        void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            _rb.linearVelocity = Vector2.zero;

            var xp = 4 + _round + (IsBoss ? 25 : 0);
            var gold = 2 + _round / 2 + (IsBoss ? 15 : 0);
            var pos = (Vector2)transform.position;
            GameFactory.CreatePickup(pos + Vector2.left * 0.2f, PickupType.Xp, xp);
            GameFactory.CreatePickup(pos + Vector2.right * 0.2f, PickupType.Gold, gold);

            var session = Object.FindAnyObjectByType<SurvivalSession>();
            session?.NotifyEnemyKilled(this);
            Destroy(gameObject);
        }
    }
}