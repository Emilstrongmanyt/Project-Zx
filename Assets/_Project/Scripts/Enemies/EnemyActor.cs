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
        const float FireBreathRange = 4.5f;
        const float FireBreathDuration = 3f;
        const float FireBreathCooldown = 12f;
        const float FireBreathTick = 0.45f;
        const float EnemySeparationRadius = 0.9f;
        const float EnemySeparationPush = 0.14f;

        public bool IsAlive { get; private set; } = true;
        public bool IsBoss { get; private set; }
        public bool IsRoundTwentyBoss { get; private set; }

        int _hp;
        int _attack;
        float _speed;
        int _round;
        Transform _player;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        float _contactCooldown;
        float _fireBreathCooldown;
        float _fireBreathTimer;
        float _fireBreathDamageTimer;
        bool _fireBreathing;
        int _fireAnimFrame;
        float _fireAnimTimer;
        Sprite _idleSprite;
        Sprite _attackSprite;
        GameObject _fireBreathFx;
        SpriteRenderer _fireBreathRenderer;
        float _blockedTimer;
        readonly List<RaycastHit2D> _castHits = new();
        readonly Collider2D[] _overlapBuffer = new Collider2D[12];

        public void Initialize(int round, bool isBoss, bool isRoundTwentyBoss = false)
        {
            _round = round;
            IsBoss = isBoss;
            IsRoundTwentyBoss = isRoundTwentyBoss;
            _hp = isBoss ? 220 + round * 30 : 18 + round * 6;
            _attack = isBoss ? 18 + round : 6 + Mathf.FloorToInt(round * 0.6f);
            _speed = isBoss ? 1.5f + round * 0.03f : 1.2f + round * 0.07f;

            if (!isBoss)
            {
                var roundScale = Mathf.Pow(1.02f, Mathf.Max(0, round - 1));
                _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * roundScale));
                _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * roundScale));
                _speed *= roundScale;
            }

            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (isRoundTwentyBoss)
            {
                _idleSprite = ArtLibrary.Boss;
                _attackSprite = ArtLibrary.BossAttacking;
            }
            else
            {
                _idleSprite = isBoss ? ArtLibrary.Boss : ArtLibrary.Zombie;
                _attackSprite = isBoss ? ArtLibrary.BossAttacking : ArtLibrary.Zombie;
            }

            if (_renderer != null) _renderer.sprite = _idleSprite;

            if (isBoss && isRoundTwentyBoss)
                SetupFireBreathFx();
        }

        void SetupFireBreathFx()
        {
            _fireBreathFx = new GameObject("FireBreath");
            _fireBreathFx.transform.SetParent(transform, false);
            _fireBreathFx.transform.localPosition = new Vector3(1.1f, 0.15f, 0f);
            _fireBreathFx.transform.localScale = Vector3.one * 2f;
            _fireBreathRenderer = _fireBreathFx.AddComponent<SpriteRenderer>();
            _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(0);
            _fireBreathRenderer.sortingOrder = 8;
            _fireBreathFx.SetActive(false);
        }

        void FixedUpdate()
        {
            if (!IsAlive || _player == null) return;
            if (_fireBreathing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            MoveByDelta(dir * (_speed * Time.fixedDeltaTime));
            UpdateFacingToward(_player.position);
            ApplyEnemySeparation();
        }

        void MoveByDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.00001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (TryMoveDelta(delta))
            {
                _blockedTimer = 0f;
                return;
            }

            _blockedTimer += Time.fixedDeltaTime;
            var direction = delta.normalized;
            var distance = delta.magnitude;
            var perp = new Vector2(-direction.y, direction.x) * distance;

            if (TryMoveDelta(perp) || TryMoveDelta(-perp))
            {
                _blockedTimer = 0f;
                return;
            }

            if (_blockedTimer > 0.25f)
            {
                var rng = Random.insideUnitCircle.normalized * distance;
                if (TryMoveDelta(rng))
                    _blockedTimer = 0f;
            }

            _rb.linearVelocity = Vector2.zero;
        }

        bool TryMoveDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.00001f) return false;

            var distance = delta.magnitude;
            var direction = delta / distance;
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.useLayerMask = false;

            _castHits.Clear();
            var hitCount = _rb.Cast(direction, filter, _castHits, distance);
            var allowed = distance;

            if (hitCount > 0)
            {
                var hit = _castHits[0];
                var otherEnemy = hit.collider != null ? hit.collider.GetComponent<EnemyActor>() : null;
                if (otherEnemy != null && otherEnemy.IsAlive)
                {
                    var perp = new Vector2(-direction.y, direction.x) * distance * 0.65f;
                    if (TryMoveDelta(perp) || TryMoveDelta(-perp))
                        return true;
                }

                allowed = Mathf.Max(0f, hit.distance - 0.04f);
            }

            if (allowed <= 0.0001f) return false;

            _rb.MovePosition(_rb.position + direction * allowed);
            _rb.linearVelocity = direction * _speed;
            return true;
        }

        void ApplyEnemySeparation()
        {
            var count = Physics2D.OverlapCircleNonAlloc(_rb.position, EnemySeparationRadius, _overlapBuffer);
            for (var i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                var otherEnemy = col.GetComponent<EnemyActor>();
                if (otherEnemy == null || otherEnemy == this || !otherEnemy.IsAlive) continue;

                var away = _rb.position - otherEnemy._rb.position;
                if (away.sqrMagnitude < 0.0001f)
                    away = Random.insideUnitCircle * 0.1f;

                var overlap = EnemySeparationRadius - away.magnitude;
                if (overlap <= 0f) continue;

                var push = away.normalized * Mathf.Min(overlap * 0.5f, EnemySeparationPush);
                TryMoveDelta(push);
            }
        }

        void Update()
        {
            if (!IsAlive || _player == null) return;

            _contactCooldown -= Time.deltaTime;
            _fireBreathCooldown -= Time.deltaTime;

            if (IsBoss && IsRoundTwentyBoss)
            {
                UpdateFireBreath();
                if (_fireBreathing) return;
            }

            if (_contactCooldown > 0f) return;
            if (Vector2.Distance(transform.position, _player.position) > 0.75f) return;

            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null || stats.IsDead) return;

            stats.TakeDamage(_attack);
            HitFlash.FlashSprite(gameObject);
            HitFlash.FlashSprite(_player.gameObject);
            _contactCooldown = 0.8f;
        }

        void UpdateFacingToward(Vector3 target)
        {
            if (_renderer == null) return;
            var dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.02f) return;
            _renderer.flipX = dx < 0f;
        }

        void UpdateFireBreath()
        {
            var dist = Vector2.Distance(transform.position, _player.position);
            var facingRight = _player.position.x >= transform.position.x;
            UpdateFacingToward(_player.position);

            if (_fireBreathing)
            {
                _fireBreathTimer -= Time.deltaTime;
                _fireAnimTimer -= Time.deltaTime;
                if (_fireAnimTimer <= 0f)
                {
                    _fireAnimTimer = 0.08f;
                    _fireAnimFrame++;
                }

                if (_renderer != null) _renderer.sprite = _attackSprite;

                if (_fireBreathRenderer != null && _fireBreathFx != null)
                {
                    _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(_fireAnimFrame);
                    _fireBreathFx.transform.localScale = new Vector3(facingRight ? 2f : -2f, 2f, 1f);
                    _fireBreathFx.transform.localPosition = new Vector3(facingRight ? 1.1f : -1.1f, 0.15f, 0f);
                }

                _fireBreathDamageTimer -= Time.deltaTime;
                if (_fireBreathDamageTimer <= 0f)
                {
                    _fireBreathDamageTimer = FireBreathTick;
                    var stats = _player.GetComponent<PlayerStats>();
                    if (stats != null && !stats.IsDead && dist <= FireBreathRange + 0.6f)
                    {
                        stats.TakeDamage(Mathf.RoundToInt(_attack * 0.55f));
                        HitFlash.FlashSprite(_player.gameObject);
                    }
                }

                if (_fireBreathTimer > 0f) return;

                EndFireBreath();
                return;
            }

            if (dist > FireBreathRange)
            {
                if (_renderer != null) _renderer.sprite = _idleSprite;
                return;
            }

            if (_renderer != null) _renderer.sprite = _attackSprite;

            if (_fireBreathCooldown > 0f) return;

            BeginFireBreath(facingRight);
        }

        void BeginFireBreath(bool facingRight)
        {
            _fireBreathing = true;
            _fireBreathTimer = FireBreathDuration;
            _fireBreathDamageTimer = 0.15f;
            _fireAnimFrame = 0;
            _fireAnimTimer = 0.08f;
            if (_renderer != null) _renderer.sprite = _attackSprite;
            if (_fireBreathFx != null)
            {
                _fireBreathFx.SetActive(true);
                _fireBreathFx.transform.localScale = new Vector3(facingRight ? 2f : -2f, 2f, 1f);
                _fireBreathFx.transform.localPosition = new Vector3(facingRight ? 1.1f : -1.1f, 0.15f, 0f);
                if (_fireBreathRenderer != null)
                    _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(0);
            }
        }

        void EndFireBreath()
        {
            _fireBreathing = false;
            _fireBreathCooldown = FireBreathCooldown;
            if (_renderer != null) _renderer.sprite = _idleSprite;
            if (_fireBreathFx != null) _fireBreathFx.SetActive(false);
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
            if (_fireBreathFx != null) _fireBreathFx.SetActive(false);

            var xp = 4 + _round + (IsBoss ? 25 : 0);
            var gold = 2 + _round / 2 + (IsBoss ? 15 : 0);
            var pos = (Vector2)transform.position;
            GameFactory.CreatePickup(pos + Vector2.left * 0.2f, PickupType.Xp, xp);
            GameFactory.CreatePickup(pos + Vector2.right * 0.2f, PickupType.Gold, gold);

            var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
            session?.NotifyEnemyKilled(this);

            if (IsRoundTwentyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Outside)
            {
                GameSave.SpearmanUnlocked = true;
                ArenaDoor.Spawn(pos + Vector2.up * 0.5f);
            }

            Destroy(gameObject);
        }
    }
}