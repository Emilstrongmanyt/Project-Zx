using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.UI;
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
        /// <summary>Distance from boss center to the breath mouth along the aim direction.</summary>
        const float FireBreathMouthOffset = 0.95f;
        /// <summary>Slight vertical bias so breath leaves near the boss head.</summary>
        const float FireBreathMouthBiasY = 0.35f;
        const int FireBreathSortOffset = 40;
        const float FireBreathHitPadding = 0.9f;
        /// <summary>Cosine of half-angle for breath damage cone (~55° half-angle).</summary>
        const float FireBreathConeDot = 0.55f;
        const float CastSkin = 0.1f;
        /// <summary>Frost Tip: keep 40% speed for 1 second (−60% move).</summary>
        const float ChillSpeedMultiplier = 0.4f;
        const float ChillDuration = 1f;
        const float SprintDuration = 2f;
        const float SprintCooldown = 10f;
        const float SprintSpeedMultiplier = 2.1f;
        const float HpPotionDropChance = 0.05f;
        const float BossHpPotionDropChance = 0.12f;
        const float MapLootDropChance = 0.005f;
        /// <summary>Very rare ring/necklace drops for the camp treasure chest (halved from original rates).</summary>
        const float EquipmentDropChance = 0.00175f;
        const float BossEquipmentDropChance = 0.01f;
        /// <summary>Outside survival regular zombies: −25% HP and move speed.</summary>
        const float OutsideZombieStatScale = 0.75f;
        const float RoundFortyBossStatScale = 4.5f;
        const float BossProjectileInterval = 1.5f;
        const float BossProjectileLifetime = 5f;
        const float BossProjectileSpeed = 2.2f;
        const float IgniteDuration = 3f;
        const int IgniteTickCount = 3;

        public bool IsAlive { get; private set; } = true;
        public bool IsBoss { get; private set; }
        public bool IsRoundTwentyBoss { get; private set; }
        public bool IsRoundThirtyBoss { get; private set; }
        public bool IsRoundFortyBoss { get; private set; }

        int _hp;
        int _maxHp;
        int _attack;
        float _speed;
        float _chillTimer;
        Color _baseColor = Color.white;
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
        Vector2 _fireBreathAim = Vector2.left;
        float _blockedTimer;
        bool _canSprint;
        bool _sprinting;
        float _sprintTimer;
        float _sprintCooldown;
        float _bossProjectileCooldown;
        int _igniteTicksRemaining;
        float _igniteTickTimer;
        int _igniteDamageRemaining;
        FlameEnchantVfx _burnVfx;
        readonly List<RaycastHit2D> _castHits = new();

        public void Initialize(
            int round,
            bool isBoss,
            bool isRoundTwentyBoss = false,
            EnemyZombieKind zombieKind = EnemyZombieKind.Outside,
            bool isRoundThirtyBoss = false,
            bool isRoundFortyBoss = false)
        {
            _round = round;
            IsBoss = isBoss;
            IsRoundTwentyBoss = isRoundTwentyBoss;
            IsRoundThirtyBoss = isRoundThirtyBoss;
            IsRoundFortyBoss = isRoundFortyBoss;
            _hp = isBoss ? 220 + round * 30 : 18 + round * 6;
            _attack = isBoss ? 18 + round : 6 + Mathf.FloorToInt(round * 0.6f);
            _speed = isBoss ? 1.5f + round * 0.03f : 1.2f + round * 0.07f;

            // Inside R30 stage boss: same footprint as Outside R20 boss, 3× that boss's stats.
            if (isRoundThirtyBoss)
            {
                const int outsideR20Hp = 220 + 20 * 30;
                const int outsideR20Attack = 18 + 20;
                const float outsideR20Speed = 1.5f + 20 * 0.03f;
                _hp = outsideR20Hp * 3;
                _attack = outsideR20Attack * 3;
                _speed = outsideR20Speed;
            }

            // Dungeon R40 final boss: same footprint, 4.5× Outside R20 stats (no fire breath).
            if (isRoundFortyBoss)
            {
                const int outsideR20Hp = 220 + 20 * 30;
                const int outsideR20Attack = 18 + 20;
                const float outsideR20Speed = 1.5f + 20 * 0.03f;
                _hp = Mathf.RoundToInt(outsideR20Hp * RoundFortyBossStatScale);
                _attack = Mathf.RoundToInt(outsideR20Attack * RoundFortyBossStatScale);
                _speed = outsideR20Speed;
            }

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

                // Outside map zombies only (base kind, not Inside/Dungeon scaled packs).
                if (zombieKind == EnemyZombieKind.Outside)
                {
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * OutsideZombieStatScale));
                    _speed *= OutsideZombieStatScale;
                }
            }

            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            _maxHp = Mathf.Max(1, _hp);
            ApplySprites(isBoss, isRoundTwentyBoss || isRoundThirtyBoss || isRoundFortyBoss, zombieKind);

            if (_renderer != null)
            {
                _renderer.sprite = _idleSprite;
                _baseColor = _renderer.color;
            }

            // Classic bosses use fire breath; Dungeon R40 BossB uses fire projectiles only.
            if (isBoss && !isRoundFortyBoss)
                SetupFireBreathFx();

            if (isRoundFortyBoss)
                _bossProjectileCooldown = BossProjectileInterval * 0.5f;

            _attack = Mathf.Max(1, _attack * 2);

            _canSprint = !isBoss && round >= 10;
            _sprintCooldown = Random.Range(2f, SprintCooldown);
        }

        public float HpRatio => _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        public bool IsChilled => _chillTimer > 0f;

        /// <summary>
        /// Flame Enchant: total burn = 40% of hit, dealt over 3 seconds (1 tick/sec). New hits refresh.
        /// </summary>
        public void ApplyIgnite(int hitDamage)
        {
            if (!IsAlive || hitDamage <= 0) return;

            var totalBurn = Mathf.Max(1, Mathf.RoundToInt(hitDamage * 0.4f));
            _igniteDamageRemaining = totalBurn;
            _igniteTicksRemaining = IgniteTickCount;
            _igniteTickTimer = 1f;
            EnsureBurnVfx();
        }
        /// <summary>Legacy alias — Frost Tip now chills (slows) instead of hard-freezing.</summary>
        public bool IsFrozen => IsChilled;

        /// <summary>Chill non-boss zombies: −60% move speed for 1 second (Frost Tip).</summary>
        public void ApplyFreeze(float duration = ChillDuration)
        {
            if (!IsAlive || IsBoss || duration <= 0f) return;
            _chillTimer = Mathf.Max(_chillTimer, duration);
            if (_renderer != null)
                _renderer.color = new Color(0.55f, 0.82f, 1f, 1f);
        }

        public void ApplyChill(float duration = ChillDuration) => ApplyFreeze(duration);

        void ApplySprites(bool isBoss, bool isRoundTwentyBoss, EnemyZombieKind zombieKind)
        {
            if (IsRoundFortyBoss)
            {
                ApplyBossBPhaseSprites();
                return;
            }

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

        void ApplyBossBPhaseSprites()
        {
            var high = HpRatio > 0.5f;
            var body = high ? ArtLibrary.BossB : ArtLibrary.BossB2;
            _idleSprite = body;
            _attackSprite = body;
            _hitSprite = body;
            _hitSpriteAttack = ArtLibrary.BossB2;
        }

        void EnsureBurnVfx()
        {
            if (_burnVfx == null)
                _burnVfx = FlameEnchantVfx.Attach(transform, FlameEnchantVfx.FlameKind.EnemyBurn, new Vector3(0f, 0.35f, 0f), 0.85f);
            else
                _burnVfx.SetActive(true);
        }

        void ClearBurnVfx()
        {
            if (_burnVfx != null)
                _burnVfx.SetActive(false);
        }

        void SetupFireBreathFx()
        {
            _fireBreathFx = new GameObject("FireBreath");
            _fireBreathFx.transform.SetParent(transform, false);
            _fireBreathRenderer = _fireBreathFx.AddComponent<SpriteRenderer>();
            _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(0);
            // Large sort offset so breath draws above the boss body (higher world Y alone sorts behind).
            _fireBreathFx.AddComponent<YSortRenderer>().Configure(FireBreathSortOffset);
            ApplyFireBreathToward(_player != null ? _player.position : transform.position + Vector3.left);
            _fireBreathFx.SetActive(false);
        }

        Vector2 GetFireBreathAim(Vector3 target)
        {
            var toTarget = (Vector2)(target - transform.position);
            if (toTarget.sqrMagnitude < 0.0001f)
                return _fireBreathAim.sqrMagnitude > 0.0001f ? _fireBreathAim : Vector2.left;
            return toTarget.normalized;
        }

        /// <summary>
        /// Aim breath along the vector to the player (left/right/up/down and diagonals).
        /// Fire art tip is on the -X side of the texture; rotate so the stream leaves the mouth.
        /// </summary>
        void ApplyFireBreathToward(Vector3 target)
        {
            if (_fireBreathFx == null) return;

            _fireBreathAim = GetFireBreathAim(target);

            var mouth = _fireBreathAim * FireBreathMouthOffset
                        + new Vector2(0f, FireBreathMouthBiasY);
            _fireBreathFx.transform.localPosition = new Vector3(mouth.x, mouth.y, 0f);
            _fireBreathFx.transform.localScale = Vector3.one * FireBreathScale;

            // Unity 2D: 0° = +X. Authored tip points left (-X), so add 180° to aim at the player.
            var angle = Mathf.Atan2(_fireBreathAim.y, _fireBreathAim.x) * Mathf.Rad2Deg + 180f;
            _fireBreathFx.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (_fireBreathRenderer != null)
                _fireBreathRenderer.flipX = false;
        }

        bool IsPlayerInFireBreathCone(float maxRange)
        {
            if (_player == null) return false;
            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            var dist = toPlayer.magnitude;
            if (dist > maxRange) return false;
            if (dist < 0.35f) return true;
            return Vector2.Dot(toPlayer / dist, _fireBreathAim) >= FireBreathConeDot;
        }

        void FixedUpdate()
        {
            if (!IsAlive || _player == null) return;
            if (_fireBreathing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            // Path straight at the player — no enemy-enemy separation so packs can stack and all hit.
            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var dir = toPlayer.normalized;
            MoveByDelta(dir * (GetMoveSpeed() * Time.fixedDeltaTime));
            UpdateFacingToward(_player.position);
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

            // Slide around blockers with several angles (helps large BossJ on tree trunks).
            if (_blockedTimer > 0.12f)
            {
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * 45f * Mathf.Deg2Rad;
                    var slide = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (!TryMoveDelta(slide)) continue;
                    _blockedTimer = 0f;
                    return;
                }
            }

            if (_blockedTimer > 0.45f)
            {
                var rng = Random.insideUnitCircle.normalized * (distance * 1.5f);
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

        int FindFirstBlockingHit(int hitCount)
        {
            for (var i = 0; i < hitCount; i++)
            {
                var col = _castHits[i].collider;
                if (col == null) continue;
                // Pass through other enemies and the player so packs can stack on the hero.
                if (col.GetComponent<EnemyActor>() != null) continue;
                if (col.CompareTag("Player")) continue;
                // Large bosses clip through trees/rocks so R20 BossJ does not pin on trunks.
                if (IsBoss && IsSoftWorldObstacle(col)) continue;
                return i;
            }

            return -1;
        }

        static bool IsSoftWorldObstacle(Collider2D col)
        {
            if (col == null) return false;
            return col.GetComponent<TreeObstacle>() != null
                   || col.GetComponent<StoneObstacle>() != null
                   || col.GetComponent<ArenaObstacle>() != null;
        }

        void Update()
        {
            if (!IsAlive || _player == null) return;

            UpdateHitSpriteTimer();
            UpdateChill();
            UpdateSprint();
            UpdateIgnite();
            UpdateBossBPhase();
            _contactCooldown -= Time.deltaTime;
            _fireBreathCooldown -= Time.deltaTime;

            if (IsRoundFortyBoss)
                UpdateBossProjectiles();
            else if (IsBoss)
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

        float GetMoveSpeed()
        {
            var speed = _speed;
            if (_sprinting) speed *= SprintSpeedMultiplier;
            if (IsChilled) speed *= ChillSpeedMultiplier;
            return speed;
        }

        void UpdateChill()
        {
            if (_chillTimer <= 0f) return;
            _chillTimer -= Time.deltaTime;
            if (_chillTimer > 0f) return;
            _chillTimer = 0f;
            if (_renderer != null)
                _renderer.color = _baseColor;
        }

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

                // Keep stream aimed at the player (up/down/left/right/diagonals).
                if (_fireBreathRenderer != null && _fireBreathFx != null)
                {
                    _fireBreathRenderer.sprite = ArtLibrary.GetFireBreathFrame(_fireAnimFrame);
                    ApplyFireBreathToward(_player.position);
                }

                _fireBreathDamageTimer -= Time.deltaTime;
                if (_fireBreathDamageTimer <= 0f)
                {
                    _fireBreathDamageTimer = FireBreathTick;
                    var stats = _player.GetComponent<PlayerStats>();
                    if (stats != null && !stats.IsDead
                        && IsPlayerInFireBreathCone(FireBreathRange + FireBreathHitPadding))
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

            BeginFireBreath();
        }

        void BeginFireBreath()
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
                ApplyFireBreathToward(_player != null ? _player.position : transform.position + Vector3.left);
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
            FloatingDamageNumber.Spawn(transform.position, amount, isHeroHit: false);
            _hp -= amount;
            UpdateBossBPhase();
            if (_hp <= 0) Die();
        }

        void TakeBurnDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            ShowHitSprite();
            FloatingDamageNumber.SpawnBurn(transform.position, amount);
            _hp -= amount;
            UpdateBossBPhase();
            if (_hp <= 0) Die();
        }

        void UpdateIgnite()
        {
            if (_igniteTicksRemaining <= 0)
            {
                ClearBurnVfx();
                return;
            }

            _igniteTickTimer -= Time.deltaTime;
            if (_igniteTickTimer > 0f) return;

            _igniteTickTimer = 1f;
            var ticksLeft = _igniteTicksRemaining;
            var tickDamage = ticksLeft <= 1
                ? _igniteDamageRemaining
                : Mathf.Max(1, _igniteDamageRemaining / ticksLeft);
            _igniteDamageRemaining = Mathf.Max(0, _igniteDamageRemaining - tickDamage);
            _igniteTicksRemaining--;
            TakeBurnDamage(tickDamage);
            if (_igniteTicksRemaining <= 0)
                ClearBurnVfx();
        }

        void UpdateBossBPhase()
        {
            if (!IsRoundFortyBoss || _renderer == null) return;
            ApplyBossBPhaseSprites();
            if (_hitSpriteTimer <= 0f)
                _renderer.sprite = _idleSprite;
        }

        void UpdateBossProjectiles()
        {
            if (!IsAlive || _player == null) return;
            UpdateFacingToward(_player.position);
            _bossProjectileCooldown -= Time.deltaTime;
            if (_bossProjectileCooldown > 0f) return;

            _bossProjectileCooldown = BossProjectileInterval;
            var aim = (Vector2)(_player.position - transform.position);
            if (aim.sqrMagnitude < 0.0001f)
                aim = _renderer != null && _renderer.flipX ? Vector2.right : Vector2.left;

            // Leading hand: boss art faces left; flipX when player is to the right.
            var leading = _renderer != null && _renderer.flipX ? 1f : -1f;
            var hand = (Vector2)transform.position + new Vector2(leading * 0.85f, 0.45f);
            var damage = Mathf.Max(1, Mathf.RoundToInt(_attack * 0.5f));
            BossFireProjectile.Spawn(hand, aim, damage, BossProjectileSpeed, BossProjectileLifetime);
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
            // Gold coin yield halved from previous values.
            var gold = Mathf.Max(1, (2 + _round / 2 + (IsBoss ? 15 : 0)) / 2);
            var pos = (Vector2)transform.position;
            GameFactory.CreatePickup(pos + Vector2.left * 0.2f, PickupType.Xp, xp);
            GameFactory.CreatePickup(pos + Vector2.right * 0.2f, PickupType.Gold, gold);

            var potionChance = IsBoss ? BossHpPotionDropChance : HpPotionDropChance;
            if (Random.value < potionChance)
            {
                var healAmount = Mathf.Max(8, Mathf.RoundToInt(MaxHpForPotionDrop() * 0.25f));
                GameFactory.CreatePickup(pos + Vector2.up * 0.25f, PickupType.HpPotion, healAmount);
            }

            // Rare pink crystal: vacuum every loot pile currently on the map.
            if (Random.value < MapLootDropChance)
                GameFactory.CreatePickup(pos + Vector2.down * 0.3f, PickupType.MapLoot, 1);

            // Very low chance for equipment (rings / necklaces) usable from the camp chest.
            var equipmentChance = IsBoss ? BossEquipmentDropChance : EquipmentDropChance;
            if (Random.value < equipmentChance)
            {
                var equipId = EquipmentCatalog.RollRandomDrop();
                GameFactory.CreateEquipmentPickup(pos + Vector2.up * 0.45f + Vector2.left * 0.15f, equipId);
            }

            var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
            session?.NotifyEnemyKilled(this);

            if (IsRoundTwentyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Outside)
            {
                GameSave.SpearmanUnlocked = true;
                GameSave.InsideMapUnlocked = true;
                ArenaDoor.Spawn(pos + Vector2.up * 0.5f);
            }

            if (IsRoundThirtyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Inside)
            {
                GameSave.DungeonMapUnlocked = true;
                ArenaGateway.Spawn(pos + Vector2.up * 0.5f);
            }

            if (IsRoundFortyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Dungeon)
            {
                GameSave.SamuraiUnlocked = true;
                ArenaVictoryGate.Spawn(pos + Vector2.up * 0.5f);
            }

            Destroy(gameObject);
        }
    }
}