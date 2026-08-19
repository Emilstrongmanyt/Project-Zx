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
        /// <summary>Max distance to start / sustain fire breath (engagement).</summary>
        const float FireBreathEngageRange = 6.75f;
        /// <summary>Damage only within the visible stream length (matches VFX). +20% vs prior 4.0.</summary>
        const float FireBreathDamageRange = 4.8f;
        const float FireBreathDuration = 3f;
        const float FireBreathCooldown = 12f;
        const float FireBreathTick = 0.45f;
        /// <summary>World-space breath size (independent of huge boss transform scale). +20% vs prior 2.55.</summary>
        const float FireBreathWorldScale = 3.06f;
        /// <summary>World distance from boss center to the breath mouth along the aim direction.</summary>
        const float FireBreathMouthWorldOffset = 1.35f;
        /// <summary>Slight vertical bias so breath leaves near the boss head (world units).</summary>
        const float FireBreathMouthBiasWorldY = 0.55f;
        const int FireBreathSortOffset = 40;
        /// <summary>Cosine of half-angle for breath damage cone (~35° half-angle — tighter to VFX).</summary>
        const float FireBreathConeDot = 0.82f;
        /// <summary>Bosses keep chasing while breathing, at reduced speed.</summary>
        const float FireBreathMoveSpeedMultiplier = 0.6f;
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
        /// <summary>Very rare ring/necklace/cape/helm drops for the camp treasure chest.</summary>
        // ~half prior rates so jewelry/capes stay rare.
        const float EquipmentDropChance = 0.000875f;
        const float BossEquipmentDropChance = 0.05f;
        /// <summary>Outside survival regular zombies: −25% HP and move speed.</summary>
        const float OutsideZombieStatScale = 0.75f;
        /// <summary>All enemies on Inside Survival map: −15% move speed.</summary>
        const float InsideMapSpeedScale = 0.85f;
        const float RoundFortyBossStatScale = 4.5f;
        /// <summary>Crypt R50 Minotaur: slightly above Dungeon R40 footprint before map mult.</summary>
        const float RoundFiftyBossStatScale = 5f;
        /// <summary>Crypt trash packs vs Dungeon elite baseline.</summary>
        const float CryptTrashStatScale = 1.15f;
        const float BossProjectileInterval = 1.5f;
        const float BossProjectileLifetime = 5f;
        const float BossProjectileSpeed = 2.2f;
        const float RangedPreferredMin = 4.2f;
        const float RangedPreferredMax = 7.5f;
        const float RangedShootRange = 9f;
        const float ChargeWindupSeconds = 0.35f;
        const float ChargeDashSeconds = 0.42f;
        const float ChargeRecoverSeconds = 0.55f;
        const float ChargeDashSpeedMultiplier = 3.6f;
        const float ChargeMinRange = 2.4f;
        const float ChargeMaxRange = 8.5f;
        const float ChargeCooldown = 4.5f;
        const float OrbitPreferredMin = 2.8f;
        const float OrbitPreferredMax = 5.2f;
        const float OrbitBiteSeconds = 1.1f;
        const float OrbitCircleSeconds = 2.4f;
        const float StrafeFlipSeconds = 0.55f;
        const float RangedProjectileInterval = 2.1f;
        const float RangedProjectileLifetime = 3.2f;
        const float RangedProjectileSpeed = 5.5f;
        const float RangedAttackAnimSeconds = 0.45f;
        const float MeleeAttackAnimSeconds = 0.5f;
        const float BaseContactRange = 0.75f;
        const float IgniteDuration = 3f;
        const int IgniteTickCount = 3;
        const int BleedTickCount = 2;
        /// <summary>First boss crystal guaranteed when the player can still pick; later bosses 45%.</summary>
        const float BossEpicCrystalDropChance = 0.45f;

        public bool IsAlive { get; private set; } = true;
        public bool IsBoss { get; private set; }
        public bool IsRanged { get; private set; }
        /// <summary>Bats / winged demons — chill/slow immune with bosses.</summary>
        public bool IsFlying { get; private set; }
        public bool IsSlowImmune => IsBoss || IsFlying;
        public bool IsRoundTwentyBoss { get; private set; }
        public bool IsRoundThirtyBoss { get; private set; }
        public bool IsRoundFortyBoss { get; private set; }
        public bool IsRoundFiftyBoss { get; private set; }
        /// <summary>Post-R20 elite trash: stronger stats/rewards, 1.3× visual scale applied by factory.</summary>
        public bool IsElite { get; private set; }
        public EnemyMovementMode MovementMode { get; private set; } = EnemyMovementMode.Chase;

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
        Sprite[] _standFrames = System.Array.Empty<Sprite>();
        Sprite[] _walkFrames = System.Array.Empty<Sprite>();
        Sprite[] _attackFrames = System.Array.Empty<Sprite>();
        int _bodyAnimFrame;
        float _bodyAnimTimer;
        bool _facesRightByDefault = true;
        bool _bossBLowPhase;
        bool _wasMoving;
        GameObject _fireBreathFx;
        SpriteRenderer _fireBreathRenderer;
        Vector2 _fireBreathAim = Vector2.left;
        float _blockedTimer;
        bool _canSprint;
        bool _sprinting;
        float _sprintTimer;
        float _sprintCooldown;
        enum ChargePhase { Ready, Windup, Dash, Recover }
        ChargePhase _chargePhase = ChargePhase.Ready;
        float _chargeTimer;
        float _chargeCooldown;
        Vector2 _chargeDir = Vector2.right;
        float _orbitSign = 1f;
        float _orbitPhaseTimer;
        bool _orbitBiting;
        float _strafeSign = 1f;
        float _strafeFlipTimer;
        float _bossProjectileCooldown;
        float _rangedProjectileCooldown;
        float _rangedAttackAnimTimer;
        float _meleeAttackAnimTimer;
        float _contactRange = BaseContactRange;
        int _igniteTicksRemaining;
        float _igniteTickTimer;
        int _igniteDamageRemaining;
        int _bleedTicksRemaining;
        float _bleedTickTimer;
        int _bleedDamageRemaining;

        readonly List<RaycastHit2D> _castHits = new();
        const float BodyAnimFrameSeconds = 0.1f;

        public void Initialize(
            int round,
            bool isBoss,
            bool isRoundTwentyBoss = false,
            EnemyZombieKind zombieKind = EnemyZombieKind.Outside,
            bool isRoundThirtyBoss = false,
            bool isRoundFortyBoss = false,
            bool isRanged = false,
            bool isRoundFiftyBoss = false,
            bool isElite = false,
            EnemyMovementMode? forcedMovementMode = null)
        {
            _round = round;
            IsBoss = isBoss;
            IsRanged = isRanged && !isBoss;
            IsElite = isElite && !isBoss;
            IsRoundTwentyBoss = isRoundTwentyBoss;
            IsRoundThirtyBoss = isRoundThirtyBoss;
            IsRoundFortyBoss = isRoundFortyBoss;
            IsRoundFiftyBoss = isRoundFiftyBoss;
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

            // Crypt R50 Minotaur: heavier footprint than R40 before map mult.
            if (isRoundFiftyBoss)
            {
                const int outsideR20Hp = 220 + 20 * 30;
                const int outsideR20Attack = 18 + 20;
                const float outsideR20Speed = 1.5f + 20 * 0.03f;
                _hp = Mathf.RoundToInt(outsideR20Hp * RoundFiftyBossStatScale);
                _attack = Mathf.RoundToInt(outsideR20Attack * RoundFiftyBossStatScale);
                _speed = outsideR20Speed * 1.05f;
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

                // Crypt trash: ~15% tougher than Dungeon elite packs.
                if (GameSessionContext.SurvivalMap == SurvivalMapKind.Crypt)
                {
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * CryptTrashStatScale));
                    _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * CryptTrashStatScale));
                    _speed *= CryptTrashStatScale;
                }

                // Dungeon R20–40: pure InsideElite + per-round speed was outpacing player power.
                // Soften speed hardest; trim HP/ATK slightly so mid–late dungeon stays fair.
                if (GameSessionContext.SurvivalMap == SurvivalMapKind.Dungeon && round >= 15)
                {
                    var late = Mathf.InverseLerp(15f, 40f, round);
                    var speedSoft = Mathf.Lerp(0.94f, 0.76f, late);
                    var statSoft = Mathf.Lerp(0.97f, 0.88f, late);
                    _speed *= speedSoft;
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * statSoft));
                    _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * statSoft));
                }

                // Post-R20: fewer trash packs overall, but each enemy hits harder and tanks more.
                // Early rounds (≤20) keep the original curve.
                if (round > 20)
                {
                    var latePower = 1f + (round - 20) * 0.045f;
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * latePower));
                    _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * latePower));
                }

                // Named elites: denser threats with better loot (visual scale applied in factory).
                if (IsElite)
                {
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * 1.55f));
                    _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * 1.35f));
                    _speed *= 1.08f;
                }
            }

            // Inside Survival map: every enemy (including bosses) moves 15% slower.
            if (GameSessionContext.SurvivalMap == SurvivalMapKind.Inside)
                _speed *= InsideMapSpeedScale;

            // Inside Survival bosses: 1.5× HP and damage (including R30 stage boss).
            if (isBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Inside)
            {
                _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * 1.5f));
                _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * 1.5f));
            }

            // Dungeon Survival bosses: 2× HP and damage (including R40 stage boss).
            if (isBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Dungeon)
            {
                _hp = Mathf.Max(1, _hp * 2);
                _attack = Mathf.Max(1, _attack * 2);
            }

            // Crypt Survival bosses: 2.5× HP and damage (including R50 Minotaur).
            if (isBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Crypt)
            {
                _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * 2.5f));
                _attack = Mathf.Max(1, Mathf.RoundToInt(_attack * 2.5f));
            }

            // Unlimited Survival bosses: 5× HP and damage (every 10th-round boss + R100).
            if (isBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Unlimited)
            {
                _hp = Mathf.Max(1, _hp * 5);
                _attack = Mathf.Max(1, _attack * 5);
            }

            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            _maxHp = Mathf.Max(1, _hp);
            ApplySprites(
                isBoss,
                isRoundTwentyBoss || isRoundThirtyBoss || isRoundFortyBoss || isRoundFiftyBoss,
                zombieKind,
                IsRanged,
                forcedMovementMode);

            ResolveMovementMode(forcedMovementMode, round);

            if (_renderer != null)
            {
                _renderer.sprite = _idleSprite;
                _baseColor = _renderer.color;
            }

            // Classic bosses use fire breath; R40 Lord + R50 Minotaur use projectiles instead.
            if (isBoss && !isRoundFortyBoss && !isRoundFiftyBoss)
                SetupFireBreathFx();

            if (isRoundFortyBoss || isRoundFiftyBoss)
                _bossProjectileCooldown = BossProjectileInterval * 0.5f;

            if (IsRanged || MovementMode == EnemyMovementMode.Kite)
            {
                // Slightly squishier casters that hang back and shoot.
                if (IsRanged)
                {
                    _hp = Mathf.Max(1, Mathf.RoundToInt(_hp * 0.85f));
                    _maxHp = Mathf.Max(1, _hp);
                    _speed *= 0.9f;
                }

                _rangedProjectileCooldown = Random.Range(0.4f, RangedProjectileInterval * 0.6f);
            }

            _attack = Mathf.Max(1, _attack * 2);

            // Melee touch range follows the body-sized world collider (not full sprite extent).
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                var worldRadius = col.radius * Mathf.Abs(transform.lossyScale.x);
                _contactRange = Mathf.Max(BaseContactRange, worldRadius + 0.25f);
            }
            else
            {
                _contactRange = IsBoss ? 1.2f : BaseContactRange;
            }

            // Sprint mode (and legacy chase with sprint unlocked) may burst.
            _canSprint = !isBoss
                        && MovementMode is EnemyMovementMode.Sprint or EnemyMovementMode.Chase
                        && !IsRanged
                        && !IsFlying
                        && (MovementMode == EnemyMovementMode.Sprint || round >= 10);
            if (MovementMode == EnemyMovementMode.Sprint)
                _canSprint = !isBoss && !IsFlying;
            _sprintCooldown = Random.Range(1.2f, SprintCooldown * 0.6f);
            _chargeCooldown = Random.Range(0.8f, ChargeCooldown * 0.5f);
            _orbitSign = Random.value < 0.5f ? -1f : 1f;
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
            _strafeFlipTimer = StrafeFlipSeconds;
            _orbitPhaseTimer = OrbitCircleSeconds;

            if (IsFlying)
                CapFlyingMoveSpeed();
        }

        void ResolveMovementMode(EnemyMovementMode? forced, int round)
        {
            if (IsBoss)
            {
                MovementMode = EnemyMovementMode.Chase;
                return;
            }

            if (forced.HasValue)
            {
                MovementMode = forced.Value;
            }
            else if (IsRanged)
            {
                MovementMode = EnemyMovementMode.Kite;
            }
            else if (IsFlying)
            {
                MovementMode = EnemyMovementMode.Fly;
            }
            else
            {
                MovementMode = RollAmbientMovementMode(round);
            }

            // Explicit mode wins over art-name flying for ground pressure packs.
            if (MovementMode == EnemyMovementMode.Fly)
                IsFlying = true;
            else if (MovementMode is EnemyMovementMode.Chase or EnemyMovementMode.Sprint
                     or EnemyMovementMode.Charge or EnemyMovementMode.Orbit or EnemyMovementMode.Strafe)
                IsFlying = false;

            if (MovementMode == EnemyMovementMode.Kite)
                IsRanged = true;
        }

        static EnemyMovementMode RollAmbientMovementMode(int round)
        {
            if (round < 6) return EnemyMovementMode.Chase;
            var roll = Random.value;
            if (round < 10)
                return roll < 0.7f ? EnemyMovementMode.Chase : EnemyMovementMode.Strafe;
            if (round < 20)
            {
                if (roll < 0.45f) return EnemyMovementMode.Chase;
                if (roll < 0.7f) return EnemyMovementMode.Sprint;
                if (roll < 0.85f) return EnemyMovementMode.Strafe;
                return EnemyMovementMode.Charge;
            }

            if (roll < 0.35f) return EnemyMovementMode.Chase;
            if (roll < 0.55f) return EnemyMovementMode.Sprint;
            if (roll < 0.7f) return EnemyMovementMode.Charge;
            if (roll < 0.85f) return EnemyMovementMode.Strafe;
            return EnemyMovementMode.Orbit;
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

        /// <summary>
        /// Bloodletting epic: 20% of hit as bleed over 2 seconds (1 tick/sec). Refreshes on new hits.
        /// </summary>
        public void ApplyBleed(int hitDamage)
        {
            if (!IsAlive || hitDamage <= 0) return;

            var totalBleed = Mathf.Max(1, Mathf.RoundToInt(hitDamage * 0.2f));
            _bleedDamageRemaining = totalBleed;
            _bleedTicksRemaining = BleedTickCount;
            _bleedTickTimer = 1f;
            // Soft red tint (~50% strength) so bleed is readable without washing out the sprite.
            if (_renderer != null)
                _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.45f, 0.45f, 1f), 0.5f);
        }
        /// <summary>Legacy alias — Frost Tip now chills (slows) instead of hard-freezing.</summary>
        public bool IsFrozen => IsChilled;

        /// <summary>Chill: −60% move for 1s (Frost Tip). Bosses + flying immune.</summary>
        public void ApplyFreeze(float duration = ChillDuration)
        {
            if (!IsAlive || IsSlowImmune || duration <= 0f) return;
            _chillTimer = Mathf.Max(_chillTimer, duration);
            if (_renderer != null)
                _renderer.color = new Color(0.55f, 0.82f, 1f, 1f);
        }

        public void ApplyChill(float duration = ChillDuration) => ApplyFreeze(duration);

        void ApplySprites(
            bool isBoss,
            bool isStageBoss,
            EnemyZombieKind zombieKind,
            bool isRanged,
            EnemyMovementMode? forcedMode = null)
        {
            if (IsRoundFiftyBoss)
            {
                ApplyAnimSet(ArtLibrary.GetMinotaurBossAnimSet());
                return;
            }

            if (IsRoundFortyBoss)
            {
                _bossBLowPhase = HpRatio <= 0.5f;
                ApplyAnimSet(ArtLibrary.GetLordBossAnimSet(highPhase: !_bossBLowPhase));
                return;
            }

            if (isBoss)
            {
                // Decade Rogue Adventure bosses when available; else classic golem.
                if (BossArtCatalog.TryGetDecadeBossSet(
                        GameSessionContext.SurvivalMap,
                        _round,
                        IsRoundTwentyBoss,
                        IsRoundThirtyBoss,
                        IsRoundFortyBoss,
                        IsRoundFiftyBoss,
                        out var rogueSet))
                {
                    ApplyAnimSet(rogueSet);
                    return;
                }

                ApplyAnimSet(ArtLibrary.GetGolemBossAnimSet());
                return;
            }

            if (forcedMode == EnemyMovementMode.Fly)
            {
                ApplyAnimSet(ArtLibrary.GetFlyingEnemyAnimSet());
                return;
            }

            if (isRanged || forcedMode == EnemyMovementMode.Kite)
            {
                ApplyAnimSet(ArtLibrary.GetRangedEnemyAnimSet());
                return;
            }

            // Ground pressure packs must not accidentally roll flying art.
            var forbidFlying = forcedMode is EnemyMovementMode.Chase or EnemyMovementMode.Sprint
                or EnemyMovementMode.Charge or EnemyMovementMode.Orbit or EnemyMovementMode.Strafe;
            ApplyAnimSet(ArtLibrary.GetEnemyAnimSet(zombieKind, forbidFlying: forbidFlying));
        }

        void ApplyAnimSet(MonsterAnimSet set)
        {
            if (!set.IsValid)
            {
                // Legacy fallback if Resources/Monsters is missing (avoid ArtLibrary.Boss recursion).
                ArtLibrary.GetZombieSprites(EnemyZombieKind.Outside, out _idleSprite, out _hitSprite);
                if (_idleSprite == null)
                    _idleSprite = ArtLibrary.PlayerIdle;
                _attackSprite = _idleSprite;
                _hitSpriteAttack = _hitSprite ?? _idleSprite;
                _standFrames = _idleSprite != null ? new[] { _idleSprite } : System.Array.Empty<Sprite>();
                _walkFrames = _standFrames;
                _attackFrames = _standFrames;
                _facesRightByDefault = true;
                IsFlying = false;
                return;
            }

            _idleSprite = set.Idle;
            _attackSprite = set.Attack ?? set.Idle;
            _hitSprite = set.Hit ?? set.Idle;
            _hitSpriteAttack = set.HitAttack ?? _hitSprite;
            _standFrames = set.StandFrames != null && set.StandFrames.Length > 0
                ? set.StandFrames
                : new[] { set.Idle };
            _walkFrames = set.WalkFrames != null && set.WalkFrames.Length > 0
                ? set.WalkFrames
                : _standFrames;
            _attackFrames = set.AttackFrames != null && set.AttackFrames.Length > 0
                ? set.AttackFrames
                : new[] { _attackSprite };
            _facesRightByDefault = set.FacesRightByDefault;
            IsFlying = set.IsFlying;
            _bodyAnimFrame = 0;
            _bodyAnimTimer = BodyAnimFrameSeconds;
        }

        void ApplyBossBPhaseSprites()
        {
            if (!IsRoundFortyBoss) return;
            var low = HpRatio <= 0.5f;
            if (low == _bossBLowPhase && _idleSprite != null) return;
            _bossBLowPhase = low;
            ApplyAnimSet(ArtLibrary.GetLordBossAnimSet(highPhase: !low));
            if (_renderer != null && _hitSpriteTimer <= 0f && !_fireBreathing)
                _renderer.sprite = _idleSprite;
        }

        void EnsureBurnVfx()
        {
            // Soft red tint (~25%) — same idea as chill blue, not a large flame sprite.
            if (_renderer == null || IsChilled) return;
            _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.32f, 0.28f, 1f), 0.25f);
        }

        void ClearBurnVfx()
        {
            if (_renderer == null) return;
            if (IsChilled) return;
            if (_bleedTicksRemaining > 0)
            {
                _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.45f, 0.45f, 1f), 0.5f);
                return;
            }

            _renderer.color = _baseColor;
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

            // Boss transform is huge; convert world mouth offset / breath size into local space.
            var parentScale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            var inv = 1f / parentScale;
            var mouth = (_fireBreathAim * FireBreathMouthWorldOffset
                         + new Vector2(0f, FireBreathMouthBiasWorldY)) * inv;
            _fireBreathFx.transform.localPosition = new Vector3(mouth.x, mouth.y, 0f);
            _fireBreathFx.transform.localScale = Vector3.one * (FireBreathWorldScale * inv);

            // Unity 2D: 0° = +X. Authored tip points left (-X), so add 180° to aim at the player.
            var angle = Mathf.Atan2(_fireBreathAim.y, _fireBreathAim.x) * Mathf.Rad2Deg + 180f;
            _fireBreathFx.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (_fireBreathRenderer != null)
                _fireBreathRenderer.flipX = false;
        }

        bool IsPlayerInFireBreathCone(float maxRange)
        {
            if (_player == null) return false;
            // Measure from the breath mouth so side/back hits outside the stream do not damage.
            var mouthWorld = (Vector2)transform.position
                             + _fireBreathAim * FireBreathMouthWorldOffset
                             + new Vector2(0f, FireBreathMouthBiasWorldY);
            var toPlayer = (Vector2)_player.position - mouthWorld;
            var dist = toPlayer.magnitude;
            if (dist > maxRange) return false;
            if (dist < 0.2f) return true;
            return Vector2.Dot(toPlayer / dist, _fireBreathAim) >= FireBreathConeDot;
        }

        void FixedUpdate()
        {
            if (!IsAlive || _player == null) return;

            // Hold still while casting a ranged bolt or swinging in melee.
            // Fire-breath bosses still move (at FireBreathMoveSpeedMultiplier via GetMoveSpeed).
            if (!_fireBreathing
                && ((IsRanged && _rangedAttackAnimTimer > 0f) || _meleeAttackAnimTimer > 0f)
                && _chargePhase != ChargePhase.Dash)
            {
                _rb.linearVelocity = Vector2.zero;
                UpdateFacingToward(_player.position);
                return;
            }

            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude < 0.0001f && _chargePhase != ChargePhase.Dash)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (!TryComputeMoveDirection(toPlayer, out var dir, out var speedMul))
            {
                _rb.linearVelocity = Vector2.zero;
                UpdateFacingToward(_player.position);
                return;
            }

            MoveByDelta(dir * (GetMoveSpeed() * speedMul * Time.fixedDeltaTime));
            UpdateFacingToward(_player.position);
        }

        /// <summary>
        /// Returns false when the unit should stand still this tick (e.g. kite band / charge windup).
        /// </summary>
        bool TryComputeMoveDirection(Vector2 toPlayer, out Vector2 dir, out float speedMul)
        {
            dir = Vector2.zero;
            speedMul = 1f;
            var dist = toPlayer.magnitude;
            var toward = dist > 0.0001f ? toPlayer / dist : Vector2.right;

            if (_fireBreathing)
            {
                dir = toward;
                return true;
            }

            switch (MovementMode)
            {
                case EnemyMovementMode.Kite:
                    if (dist > RangedPreferredMax) { dir = toward; return true; }
                    if (dist < RangedPreferredMin) { dir = -toward; return true; }
                    return false;

                case EnemyMovementMode.Charge:
                    return TryComputeChargeDirection(toward, dist, out dir, out speedMul);

                case EnemyMovementMode.Orbit:
                    return TryComputeOrbitDirection(toward, dist, out dir);

                case EnemyMovementMode.Strafe:
                {
                    _strafeFlipTimer -= Time.fixedDeltaTime;
                    if (_strafeFlipTimer <= 0f)
                    {
                        _strafeSign = -_strafeSign;
                        _strafeFlipTimer = StrafeFlipSeconds * Random.Range(0.75f, 1.25f);
                    }

                    var side = new Vector2(-toward.y, toward.x) * _strafeSign;
                    dir = (toward * 0.72f + side * 0.85f).normalized;
                    return true;
                }

                case EnemyMovementMode.Fly:
                    // Flying melee chase; flying kite uses Kite mode + IsFlying.
                    dir = toward;
                    return true;

                case EnemyMovementMode.Sprint:
                case EnemyMovementMode.Chase:
                default:
                    dir = toward;
                    return true;
            }
        }

        bool TryComputeChargeDirection(Vector2 toward, float dist, out Vector2 dir, out float speedMul)
        {
            dir = toward;
            speedMul = 1f;
            _chargeCooldown -= Time.fixedDeltaTime;

            switch (_chargePhase)
            {
                case ChargePhase.Ready:
                    if (_chargeCooldown <= 0f && dist >= ChargeMinRange && dist <= ChargeMaxRange)
                    {
                        _chargePhase = ChargePhase.Windup;
                        _chargeTimer = ChargeWindupSeconds;
                        _chargeDir = toward;
                    }

                    dir = toward;
                    return true;

                case ChargePhase.Windup:
                    _chargeTimer -= Time.fixedDeltaTime;
                    _chargeDir = toward;
                    if (_chargeTimer <= 0f)
                    {
                        _chargePhase = ChargePhase.Dash;
                        _chargeTimer = ChargeDashSeconds;
                    }

                    return false;

                case ChargePhase.Dash:
                    _chargeTimer -= Time.fixedDeltaTime;
                    dir = _chargeDir;
                    speedMul = ChargeDashSpeedMultiplier;
                    if (_chargeTimer <= 0f)
                    {
                        _chargePhase = ChargePhase.Recover;
                        _chargeTimer = ChargeRecoverSeconds;
                    }

                    return true;

                case ChargePhase.Recover:
                    _chargeTimer -= Time.fixedDeltaTime;
                    if (_chargeTimer <= 0f)
                    {
                        _chargePhase = ChargePhase.Ready;
                        _chargeCooldown = ChargeCooldown;
                    }

                    dir = toward * 0.35f;
                    return dir.sqrMagnitude > 0.01f;
            }

            return true;
        }

        bool TryComputeOrbitDirection(Vector2 toward, float dist, out Vector2 dir)
        {
            _orbitPhaseTimer -= Time.fixedDeltaTime;
            if (_orbitPhaseTimer <= 0f)
            {
                _orbitBiting = !_orbitBiting;
                _orbitPhaseTimer = _orbitBiting
                    ? OrbitBiteSeconds * Random.Range(0.8f, 1.2f)
                    : OrbitCircleSeconds * Random.Range(0.8f, 1.2f);
                if (!_orbitBiting && Random.value < 0.35f)
                    _orbitSign = -_orbitSign;
            }

            if (_orbitBiting || dist > OrbitPreferredMax)
            {
                dir = toward;
                return true;
            }

            if (dist < OrbitPreferredMin)
            {
                dir = -toward;
                return true;
            }

            var tangent = new Vector2(-toward.y, toward.x) * _orbitSign;
            dir = (tangent * 0.9f + toward * 0.15f).normalized;
            return true;
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

            var proposed = _rb.position + direction * allowed;
            if (ArenaBounds.WorldWrapEnabled)
            {
                ArenaBounds.ConstrainPosition(proposed, out var wrapped, out _);
                proposed = wrapped;
            }

            _rb.position = proposed;
            _rb.MovePosition(proposed);
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
            UpdateBodyAnimation();
            UpdateChill();
            UpdateSprint();
            UpdateIgnite();
            UpdateBleed();
            UpdateBossBPhase();
            _contactCooldown -= Time.deltaTime;
            _fireBreathCooldown -= Time.deltaTime;
            if (_rangedAttackAnimTimer > 0f)
                _rangedAttackAnimTimer -= Time.deltaTime;
            if (_meleeAttackAnimTimer > 0f)
                _meleeAttackAnimTimer -= Time.deltaTime;

            if (IsRoundFortyBoss || IsRoundFiftyBoss)
                UpdateBossProjectiles();
            else if (IsBoss)
            {
                UpdateFireBreath();
                if (_fireBreathing) return;
            }
            else if (IsRanged || MovementMode == EnemyMovementMode.Kite)
            {
                UpdateRangedAttack();
            }

            if (_contactCooldown > 0f) return;
            if (Vector2.Distance(transform.position, _player.position) > _contactRange) return;

            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null || stats.IsDead) return;

            // Melee touch: play attack anim and apply contact damage.
            BeginMeleeAttackAnim();
            stats.TakeDamage(_attack);
            HitFlash.FlashSprite(gameObject);
            HitFlash.FlashSprite(_player.gameObject);
            _contactCooldown = 0.8f;
        }

        float GetMoveSpeed()
        {
            var speed = _speed;
            if (_sprinting || (MovementMode == EnemyMovementMode.Sprint && _sprinting))
                speed *= SprintSpeedMultiplier;
            if (IsChilled) speed *= ChillSpeedMultiplier;
            if (_fireBreathing) speed *= FireBreathMoveSpeedMultiplier;
            if (IsFlying)
            {
                var cap = ResolvePlayerMoveSpeedCap();
                if (cap > 0f)
                    speed = Mathf.Min(speed, cap);
            }

            return speed;
        }

        void CapFlyingMoveSpeed()
        {
            var cap = ResolvePlayerMoveSpeedCap();
            if (cap > 0f)
                _speed = Mathf.Min(_speed, cap);
        }

        float ResolvePlayerMoveSpeedCap()
        {
            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_player == null) return 0f;

            var stats = _player.GetComponent<PlayerStats>();
            if (stats != null && !stats.IsDead)
                return Mathf.Max(0.5f, stats.EffectiveMoveSpeed);

            return TapMovement.DefaultBaseSpeed * GameSave.SpeedMultiplier;
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
            // Sanctum demon/golem/lord packs face right by default.
            _renderer.flipX = _facesRightByDefault ? dx < 0f : dx > 0f;
        }

        bool IsFacingTarget(Vector3 target)
        {
            var dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.02f) return true;
            var wantsFlip = _facesRightByDefault ? dx < 0f : dx > 0f;
            return _renderer == null || _renderer.flipX == wantsFlip;
        }

        bool IsPlayingAttackAnim()
        {
            return _fireBreathing
                   || _rangedAttackAnimTimer > 0f
                   || _meleeAttackAnimTimer > 0f
                   || IsInMeleeAttackPoseRange()
                   || IsInBossAttackPoseRange();
        }

        void UpdateBodyAnimation()
        {
            if (_renderer == null) return;

            // Attack anims always win over hit flash so brief player hits do not cancel swings.
            if (IsPlayingAttackAnim())
            {
                _hitSpriteTimer = 0f;
                AdvanceAnimFrames(_attackFrames, ref _bodyAnimFrame, ref _bodyAnimTimer);
                if (_attackFrames.Length > 0)
                    _renderer.sprite = _attackFrames[Mathf.Clamp(_bodyAnimFrame, 0, _attackFrames.Length - 1)];
                return;
            }

            if (_hitSpriteTimer > 0f) return;

            var moving = _rb != null && _rb.linearVelocity.sqrMagnitude > 0.0004f;
            if (moving != _wasMoving)
            {
                _wasMoving = moving;
                _bodyAnimFrame = 0;
                _bodyAnimTimer = 0f;
            }

            var frames = moving ? _walkFrames : _standFrames;
            AdvanceAnimFrames(frames, ref _bodyAnimFrame, ref _bodyAnimTimer);
            if (frames.Length > 0)
                _renderer.sprite = frames[Mathf.Clamp(_bodyAnimFrame, 0, frames.Length - 1)];
            else if (_idleSprite != null)
                _renderer.sprite = _idleSprite;
        }

        void BeginMeleeAttackAnim()
        {
            _meleeAttackAnimTimer = MeleeAttackAnimSeconds;
            _hitSpriteTimer = 0f;
            _bodyAnimFrame = 0;
            _bodyAnimTimer = 0f;
        }

        /// <summary>Near / in touch range — play attack loop (melee grunts, casters, Lord, golems).</summary>
        bool IsInMeleeAttackPoseRange()
        {
            if (_player == null || _fireBreathing) return false;
            var dist = Vector2.Distance(transform.position, _player.position);
            return dist <= _contactRange * 1.25f;
        }

        /// <summary>Boss wind-up only while breathing, about to breathe, or in melee — not the whole engage range.</summary>
        bool IsInBossAttackPoseRange()
        {
            if (!IsBoss || IsRoundFortyBoss || IsRoundFiftyBoss || _player == null || _fireBreathing) return false;
            var dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= _contactRange * 1.25f) return true;
            return dist <= FireBreathEngageRange && _fireBreathCooldown <= 0.35f;
        }

        static void AdvanceAnimFrames(Sprite[] frames, ref int frame, ref float timer)
        {
            if (frames == null || frames.Length == 0) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = BodyAnimFrameSeconds;
            frame = (frame + 1) % frames.Length;
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

                // Body attack frames driven by UpdateBodyAnimation; keep breath VFX cycling.
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
                        && IsPlayerInFireBreathCone(FireBreathDamageRange))
                    {
                        stats.TakeDamage(Mathf.RoundToInt(_attack * 0.55f));
                        HitFlash.FlashSprite(_player.gameObject);
                    }
                }

                if (_fireBreathTimer > 0f) return;

                EndFireBreath();
                return;
            }

            if (dist > FireBreathEngageRange)
                return;

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

        public void TakeDamage(int amount, bool isCrit = false)
        {
            if (!IsAlive || amount <= 0) return;
            DpsTracker.Record(amount);
            ShowHitSprite();
            FloatingDamageNumber.Spawn(transform.position, amount, isHeroHit: false, isCrit: isCrit);
            _hp -= amount;
            UpdateBossBPhase();
            if (_hp <= 0) Die();
        }

        void TakeBurnDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            DpsTracker.Record(amount);
            ShowHitSprite();
            FloatingDamageNumber.SpawnBurn(transform.position, amount);
            _hp -= amount;
            UpdateBossBPhase();
            if (_hp <= 0) Die();
        }

        void TakeBleedDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            DpsTracker.Record(amount);
            ShowHitSprite();
            FloatingDamageNumber.SpawnBleed(transform.position, amount);
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

        void UpdateBleed()
        {
            if (_bleedTicksRemaining <= 0)
            {
                if (_renderer != null && _chillTimer <= 0f)
                    _renderer.color = _baseColor;
                return;
            }

            _bleedTickTimer -= Time.deltaTime;
            if (_bleedTickTimer > 0f) return;

            _bleedTickTimer = 1f;
            var ticksLeft = _bleedTicksRemaining;
            var tickDamage = ticksLeft <= 1
                ? _bleedDamageRemaining
                : Mathf.Max(1, _bleedDamageRemaining / ticksLeft);
            _bleedDamageRemaining = Mathf.Max(0, _bleedDamageRemaining - tickDamage);
            _bleedTicksRemaining--;
            TakeBleedDamage(tickDamage);
            if (_bleedTicksRemaining <= 0 && _renderer != null && _chillTimer <= 0f)
                _renderer.color = _baseColor;
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

            // Leading hand follows facing (art faces right by default).
            var leading = _renderer != null && _renderer.flipX ? -1f : 1f;
            var hand = (Vector2)transform.position + new Vector2(leading * 0.85f, 0.45f);
            var damage = Mathf.Max(1, Mathf.RoundToInt(_attack * 0.5f));
            BossFireProjectile.Spawn(hand, aim, damage, BossProjectileSpeed, BossProjectileLifetime);
        }

        void UpdateRangedAttack()
        {
            if (!IsAlive || _player == null) return;
            UpdateFacingToward(_player.position);

            _rangedProjectileCooldown -= Time.deltaTime;
            if (_rangedProjectileCooldown > 0f) return;

            var dist = Vector2.Distance(transform.position, _player.position);
            if (dist > RangedShootRange || dist < 0.6f) return;

            _rangedProjectileCooldown = RangedProjectileInterval + Random.Range(-0.25f, 0.35f);
            _rangedAttackAnimTimer = RangedAttackAnimSeconds;
            _bodyAnimFrame = 0;
            _bodyAnimTimer = 0f;

            var aim = (Vector2)(_player.position - transform.position);
            if (aim.sqrMagnitude < 0.0001f)
                aim = _renderer != null && _renderer.flipX ? Vector2.right : Vector2.left;

            var leading = _renderer != null && _renderer.flipX ? -1f : 1f;
            // Hand offset scales with large demon footprint so bolts leave the sprite, not the center.
            var handScale = Mathf.Max(1f, Mathf.Abs(transform.lossyScale.x) * 0.22f);
            var hand = (Vector2)transform.position + new Vector2(leading * 0.55f * handScale, 0.35f * handScale);
            var damage = Mathf.Max(1, Mathf.RoundToInt(_attack * 0.65f));
            EnemyRangedProjectile.Spawn(hand, aim, damage, RangedProjectileSpeed, RangedProjectileLifetime);
        }

        void ShowHitSprite()
        {
            if (_renderer == null || _hitSprite == null) return;
            // Do not interrupt an active attack pose — flash only via HitFlash color.
            if (IsPlayingAttackAnim()) return;
            var useAttackHit = _fireBreathing || _renderer.sprite == _attackSprite;
            _renderer.sprite = useAttackHit && _hitSpriteAttack != null ? _hitSpriteAttack : _hitSprite;
            _hitSpriteTimer = 0.35f;
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

            if (IsPlayingAttackAnim() && _attackFrames.Length > 0)
            {
                _renderer.sprite = _attackFrames[Mathf.Clamp(_bodyAnimFrame, 0, _attackFrames.Length - 1)];
                return;
            }

            _renderer.sprite = _idleSprite;
        }

        int MaxHpForPotionDrop()
        {
            var stats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            return stats != null ? stats.MaxHp : 100;
        }

        void TryDropEpicCrystal(Vector2 pos)
        {
            var stats = _player != null ? _player.GetComponent<PlayerStats>() : null;
            if (stats == null || !stats.CanAcceptEpicCrystal) return;

            // R10 / R20 bosses always drop elite talents when the player can still pick.
            var guaranteedDecade = _round == 10 || _round == 20 || IsRoundTwentyBoss;
            // First available pick of the run always drops; later bosses roll.
            var guaranteed = guaranteedDecade || stats.EpicPicksTaken + stats.PendingEpicChoices == 0;
            if (!guaranteed && Random.value > BossEpicCrystalDropChance) return;

            GameFactory.CreatePickup(pos + Vector2.up * 0.55f + Vector2.right * 0.2f, PickupType.EpicCrystal, 1);
        }

        void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            _rb.linearVelocity = Vector2.zero;
            if (_fireBreathFx != null) _fireBreathFx.SetActive(false);

            var xp = 4 + _round + (IsBoss ? 25 : 0) + (IsElite ? 8 + _round / 2 : 0);
            // Gold coin yield halved from previous values; elites pay a bit more.
            var gold = Mathf.Max(1, (2 + _round / 2 + (IsBoss ? 15 : 0) + (IsElite ? 6 : 0)) / 2);
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

            // Very low chance for equipment (rings / necklaces / capes / helms) for the camp chest.
            // Owned/discovered items are excluded from the roll pool and never re-drop.
            var equipmentChance = IsBoss ? BossEquipmentDropChance : EquipmentDropChance;
            if (Random.value < equipmentChance)
            {
                var equipId = EquipmentCatalog.RollRandomDrop();
                if (equipId != EquipmentId.None)
                    GameFactory.CreateEquipmentPickup(pos + Vector2.up * 0.45f + Vector2.left * 0.15f, equipId);
            }

            // Boss epic crystal → talent pick (capped per run on the player side).
            if (IsBoss)
                TryDropEpicCrystal(pos);

            // Grand Wizard's Peril: Outside R20 golem drops the Twin Lightning Pendant.
            if (QuestCatalog.ShouldDropTwinLightningPendant(
                    IsRoundTwentyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Outside))
            {
                GameFactory.CreatePickup(
                    pos + Vector2.up * 0.7f + Vector2.left * 0.35f,
                    PickupType.TwinLightningPendant,
                    1);
            }

            // A Knight's Best Friend: Dungeon R40 boss drops the lost greatsword.
            if (QuestCatalog.ShouldDropKnightsGreatsword(
                    IsRoundFortyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Dungeon))
            {
                GameFactory.CreatePickup(
                    pos + Vector2.up * 0.75f + Vector2.right * 0.4f,
                    PickupType.KnightsGreatsword,
                    1);
            }

            var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
            session?.NotifyEnemyKilled(this);

            if (IsRoundTwentyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Outside)
            {
                GameSave.SpearmanUnlocked = true;
                GameSave.InsideMapUnlocked = true;
                ArenaDoor.Spawn(pos + Vector2.up * 0.5f);
                GameHud.Instance?.ShowBanner("Spearman unlocked!", 3.5f);
            }

            if (IsRoundThirtyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Inside)
            {
                GameSave.DungeonMapUnlocked = true;
                ArenaGateway.Spawn(pos + Vector2.up * 0.5f);
            }

            if (IsRoundFortyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Dungeon)
            {
                GameSave.SamuraiUnlocked = true;
                ArenaCryptPortal.Spawn(pos + Vector2.up * 0.5f);
                GameHud.Instance?.ShowBanner("Samurai unlocked!", 3.5f);
            }

            if (IsRoundFiftyBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Crypt)
                ArenaVictoryGate.Spawn(pos + Vector2.up * 0.5f);

            Destroy(gameObject);
        }
    }
}