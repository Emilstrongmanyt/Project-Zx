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
        const float FireBreathRange = 6.75f;
        const float FireBreathDuration = 3f;
        const float FireBreathCooldown = 12f;
        const float FireBreathTick = 0.45f;
        const float FireBreathScale = 3f;
        const float FireBreathOffsetX = 1.65f;
        const float FireBreathOffsetY = 0.55f;
        const int FireBreathSortOffset = 40;
        const float FireBreathHitPadding = 0.9f;
        const float EnemySeparationRadius = 1.15f;
        const float EnemySeparationPush = 0.24f;
        const float CastSkin = 0.1f;
        const float SprintDuration = 2f;
        const float SprintCooldown = 10f;
        const float SprintSpeedMultiplier = 2.1f;
        const float HpPotionDropChance = 0.05f;
        const float BossHpPotionDropChance = 0.12f;

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
        Sprite _hitSprite;
        Sprite _hitSpriteAttack;
        float _hitSpriteTimer;
        GameObject _fireBreathFx;
        SpriteRenderer _fireBreathRenderer;
        float _blockedTimer;
        bool _canSprint;
        bool _sprinting;
        float _sprintTimer;
        float _sprintCooldown;
        readonly List<RaycastHit2D> _castHits = new();
        readonly Collider2D[] _overlapBuffer = new Collider2D[12];

        public void Initialize(int round, bool isBoss, bool isRoundTwentyBoss = false, EnemyZombieKind zombieKind = EnemyZombieKind.Outside)
        {
            _round = round;
            IsBoss = isBoss;
            IsRoundTwentyBoss = isRoundTwentyBoss;
            _hp = isBoss ? 220 + round * 30 : 18 + round * 6;
            _attack = isBoss ? 18 + round : 6 + Mathf.FloorToInt(round * 0.6f);
            _speed = isBoss ? 1.5f + round * 0.03f : 1.2f + round * 0.07f;

            if (!isBoss)
            {
                var kindScale = zombieKind switch
                {
                    EnemyZombieKind.InsideElite => 2f,
                    EnemyZombieKind.Inside => 1.5f,
                    _ => 1f
                };

                _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * kindScale));
                _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * kindScale));
                _speed *= kindScale;

                var roundScale = Mathf.Pow(1.02f, Mathf.Max(0, round - 1));
                _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * roundScale));
                _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * roundScale));
                _speed *= roundScale;
            }

            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            ApplySprites(isBoss, isRoundTwentyBoss, zombieKind);

            if (_renderer != null) _renderer.sprite = _idleSprite;

            if (isBoss)
                SetupFireBreathFx();

            _attack = Mathf.Max(1, _attack * 2);

            _canSprint = !isBoss && round >= 10;
            _sprintCooldown = Random.Range(2f, SprintCooldown);
        }

        void ApplySprites(bool isBoss, bool isRoundTwentyBoss, EnemyZombieKind zombieKind)
        {
            if (isBoss)
            {
                _idleSprite = ArtLibrary.Boss;
                _attackSprite = ArtLibrary.BossAttacking;
                _hitSprite = ArtLibrary.BossHit;
                _hitSpriteAttack = ArtLibrary.BossAttackingHit;
                return;
            }

            ArtLibrary.GetZombieSprites(zombieKind, out _idleSprite, out _hitSprite);
            _attackSprite = _idleSprite;
            _hitSpriteAttack = _hitSprite;
        }

        void SetupFireBreathFx()
        {
            _fireBreathFx = new GameObject("FireBreath");
            _fireBreathFx.transform.SetParent(transform, false);
            _fireBreathRenderer = _fireBreathFx.AddComponent<SpriteRenderer>();
            _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(0);
            // Large sort offset so breath draws above the boss body (higher world Y alone sorts behind).
            _fireBreathFx.AddComponent<YSortRenderer>().Configure(FireBreathSortOffset);
            ApplyFireBreathTransform(true);
            _fireBreathFx.SetActive(false);
        }

        void ApplyFireBreathTransform(bool facingRight)
        {
            if (_fireBreathFx == null) return;

            _fireBreathFx.transform.localScale = Vector3.one * FireBreathScale;
            _fireBreathFx.transform.localPosition = new Vector3(
                facingRight ? FireBreathOffsetX : -FireBreathOffsetX,
                FireBreathOffsetY,
                0f);

            if (_fireBreathRenderer != null)
                // Fire breath art faces left by default, same as BossJ.
                _fireBreathRenderer.flipX = facingRight;
        }

        void FixedUpdate()
        {
            if (!IsAlive || _player == null) return;
            if (_fireBreathing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            var dir = GetSteeredDirection(toPlayer).normalized;
            MoveByDelta(dir * (GetMoveSpeed() * Time.fixedDeltaTime));
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
                var blockingIndex = FindFirstBlockingHit(hitCount);
                if (blockingIndex < 0)
                    allowed = distance;
                else
                    allowed = Mathf.Max(0f, _castHits[blockingIndex].distance - CastSkin);
            }

            if (allowed <= 0.0001f) return false;

            _rb.MovePosition(_rb.position + direction * allowed);
            _rb.linearVelocity = direction * GetMoveSpeed();
            return true;
        }

        Vector2 GetSteeredDirection(Vector2 toPlayer)
        {
            if (toPlayer.sqrMagnitude < 0.0001f) return Vector2.zero;

            var desired = toPlayer.normalized;
            var count = Physics2D.OverlapCircleNonAlloc(_rb.position, EnemySeparationRadius, _overlapBuffer);
            var avoid = Vector2.zero;

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

                avoid += away.normalized * (overlap / EnemySeparationRadius);
            }

            if (avoid.sqrMagnitude < 0.0001f) return desired;
            return (desired + avoid * 1.8f).normalized;
        }

        int FindFirstBlockingHit(int hitCount)
        {
            for (var i = 0; i < hitCount; i++)
            {
                var col = _castHits[i].collider;
                if (col == null) continue;
                if (col.GetComponent<EnemyActor>() != null) continue;
                return i;
            }

            return -1;
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

                var push = away.normalized * Mathf.Min(overlap * 0.75f, EnemySeparationPush);
                TryMoveDelta(push);
            }
        }

        void Update()
        {
            if (!IsAlive || _player == null) return;

            UpdateHitSpriteTimer();
            UpdateSprint();
            _contactCooldown -= Time.deltaTime;
            _fireBreathCooldown -= Time.deltaTime;

            if (IsBoss)
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

        float GetMoveSpeed() => _speed * (_sprinting ? SprintSpeedMultiplier : 1f);

        void UpdateSprint()
        {
            if (!_canSprint || _fireBreathing || _player == null) return;

            _sprintCooldown -= Time.deltaTime;
            if (_sprinting)
            {
                _sprintTimer -= Time.deltaTime;
                if (_sprintTimer <= 0f)
                {
                    _sprinting = false;
                    _sprintCooldown = SprintCooldown;
                }

                return;
            }

            if (_sprintCooldown > 0f) return;

            var dist = Vector2.Distance(transform.position, _player.position);
            if (dist < 2f || dist > 8.5f) return;

            _sprinting = true;
            _sprintTimer = SprintDuration;
        }

        void UpdateFacingToward(Vector3 target)
        {
            if (_renderer == null) return;
            var dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.02f) return;
            // BossJ art faces left by default; zombies/player sprites face right.
            _renderer.flipX = IsBoss ? dx > 0f : dx < 0f;
        }

        bool IsFacingTarget(Vector3 target)
        {
            var dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.02f) return IsBoss;
            return IsBoss ? dx > 0f : dx >= 0f;
        }

        void UpdateFireBreath()
        {
            var dist = Vector2.Distance(transform.position, _player.position);
            UpdateFacingToward(_player.position);
            var facingRight = IsFacingTarget(_player.position);

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
                    ApplyFireBreathTransform(facingRight);
                }

                _fireBreathDamageTimer -= Time.deltaTime;
                if (_fireBreathDamageTimer <= 0f)
                {
                    _fireBreathDamageTimer = FireBreathTick;
                    var stats = _player.GetComponent<PlayerStats>();
                    if (stats != null && !stats.IsDead && dist <= FireBreathRange + FireBreathHitPadding)
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
                ApplyFireBreathTransform(facingRight);
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
            ShowHitSprite();
            _hp -= amount;
            if (_hp <= 0) Die();
        }

        void ShowHitSprite()
        {
            if (_renderer == null || _hitSprite == null) return;
            var useAttackHit = _fireBreathing || _renderer.sprite == _attackSprite;
            _renderer.sprite = useAttackHit && _hitSpriteAttack != null ? _hitSpriteAttack : _hitSprite;
            _hitSpriteTimer = 0.5f;
        }

        void UpdateHitSpriteTimer()
        {
            if (_hitSpriteTimer <= 0f) return;
            _hitSpriteTimer -= Time.deltaTime;
            if (_hitSpriteTimer > 0f) return;
            RestoreSpriteAfterHit();
        }

        void RestoreSpriteAfterHit()
        {
            if (_renderer == null) return;

            if (_fireBreathing)
            {
                _renderer.sprite = _attackSprite;
                return;
            }

            if (IsBoss && _player != null)
            {
                var dist = Vector2.Distance(transform.position, _player.position);
                if (dist <= FireBreathRange)
                {
                    _renderer.sprite = _attackSprite;
                    return;
                }
            }

            _renderer.sprite = _idleSprite;
        }

        int MaxHpForPotionDrop()
        {
            var stats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            return stats != null ? stats.MaxHp : 100;
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

            var potionChance = IsBoss ? BossHpPotionDropChance : HpPotionDropChance;
            if (Random.value < potionChance)
            {
                var healAmount = Mathf.Max(8, Mathf.RoundToInt(MaxHpForPotionDrop() * 0.25f));
                GameFactory.CreatePickup(pos + Vector2.up * 0.25f, PickupType.HpPotion, healAmount);
            }

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