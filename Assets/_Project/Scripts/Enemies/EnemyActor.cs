using System.Collections.Generic;
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
        readonly List<RaycastHit2D> _castHits = new();

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
            MoveByDelta(dir * (_speed * Time.fixedDeltaTime));

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && dir.x != 0f) renderer.flipX = dir.x < 0f;
        }

        void MoveByDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.00001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var distance = delta.magnitude;
            var direction = delta / distance;
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.useLayerMask = false;

            _castHits.Clear();
            var hitCount = _rb.Cast(direction, filter, _castHits, distance);
            var allowed = hitCount > 0 ? Mathf.Max(0f, _castHits[0].distance - 0.02f) : distance;

            if (allowed <= 0.0001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.MovePosition(_rb.position + direction * allowed);
            _rb.linearVelocity = direction * (_speed);
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

            var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
            session?.NotifyEnemyKilled(this);
            Destroy(gameObject);
        }
    }
}