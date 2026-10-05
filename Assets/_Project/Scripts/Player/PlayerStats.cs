using System;
using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.UI;
using ProjectZx.Waves;
using UnityEngine;

namespace ProjectZx.Player
{
    public enum RunLevelChoice
    {
        Speed,
        Hp,
        Attack,
        AttackSpeed,
        AttackRange,
        LootRange,
        CritChance,
        CritDamage,
        Lifesteal,
        BossHunter,
        Execute,
        GoldFind,
        Regen,
        Shield,
        Berserk,
        XpBoost,
        /// <summary>Defense category: reduce damage taken.</summary>
        Defense,
        /// <summary>Defense category: chance to fully block a hit.</summary>
        Block,
        /// <summary>Bowman only: chance to fire a second arrow.</summary>
        Multishot,
        /// <summary>Bowman only: +2 pierce hits per pick on arrows.</summary>
        Pierce,
        /// <summary>Paladin only: +10% damage and −5% damage taken while standing still.</summary>
        StandYourGround
    }

    public class PlayerStats : MonoBehaviour
    {
        const float ShieldCooldownSeconds = 10f;
        const float TalentSpeedMul = 1.10f;
        const float TalentAttackMul = 1.12f;
        const float TalentAttackSpeedMul = 1.12f;
        const float TalentRangeMul = 1.10f;
        const float TalentLootMul = 1.15f;
        const int TalentHpBase = 20;
        const int TalentHpPerLevel = 2;
        const float TalentCritChanceStep = 0.10f;
        const float TalentCritChanceCap = 0.90f;
        const float TalentCritDamageStep = 0.30f;
        const float TalentCritDamageCap = 4f;
        const float TalentLifestealStep = 0.04f;
        const float TalentLifestealCap = 0.20f;
        const float TalentBossStep = 0.25f;
        const float TalentBossCap = 0.80f;
        const float TalentExecuteStep = 0.50f;
        const float TalentExecuteCap = 1.5f;
        const float TalentGoldMul = 1.15f;
        const float TalentGoldCap = 2f;
        const float TalentRegenStep = 3f;
        const float TalentRegenCap = 8f;
        const float TalentBerserkStep = 0.25f;
        const float TalentBerserkCap = 0.50f;
        const float TalentXpMul = 1.15f;
        const float TalentXpCap = 2f;
        const float TalentDefenseStep = 0.10f;
        const float TalentDefenseCap = 0.60f;
        const float TalentBlockStep = 0.08f;
        const float TalentBlockCap = 0.50f;
        const float TalentMultishotStep = 0.33f;
        const float TalentMultishotCap = 0.99f;
        const int TalentPierceStep = 2;
        const int TalentPierceCap = 6;
        const float StandYourGroundDamageMultiplier = 1.10f;
        const float StandYourGroundDamageTakenMultiplier = 0.95f;
        const float PaladinBlockReflectFraction = 0.40f;
        const float ShatteringDamageMultiplier = 1.40f;
        const float RegenOutOfCombatDelay = 2f;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int RunXp { get; private set; }
        public int RunGold { get; private set; }
        public int Level { get; private set; } = 1;
        public int XpToNext { get; private set; }
        public bool IsDead { get; private set; }
        public bool SurvivalMode { get; private set; }
        /// <summary>Standby hero companion — invulnerable assist unit at reduced damage.</summary>
        public bool IsCompanion { get; private set; }
        /// <summary>Leader stats when this is a companion (loot / lifesteal credit).</summary>
        public PlayerStats CompanionLeader { get; private set; }
        /// <summary>1 for player, 0.2 for companion (80% damage reduction).</summary>
        public float DamageOutputScale { get; private set; } = 1f;
        public int PendingLevelUpChoices { get; private set; }
        public float RunSpeedMultiplier { get; private set; } = 1f;
        public float RunDamageMultiplier { get; private set; } = 1f;
        public float RunAttackSpeedMultiplier { get; private set; } = 1f;
        public float RunAttackRangeMultiplier { get; private set; } = 1f;
        /// <summary>Permanent shop range × run talent range (weapon materials do not add range).</summary>
        public float AttackRangeMultiplier =>
            GameSave.AttackRangeMultiplier * RunAttackRangeMultiplier;
        public float RunLootRangeMultiplier { get; private set; } = 1f;
        public float RunCritChance { get; private set; }
        public float RunCritMultiplier { get; private set; } = 1.5f;
        public float RunLifesteal { get; private set; }
        public float RunBossDamageBonus { get; private set; }
        public float RunExecuteBonus { get; private set; }
        public float RunGoldFindMultiplier { get; private set; } = 1f;
        public float RunXpMultiplier { get; private set; } = 1f;
        public float RunRegenPerSecond { get; private set; }
        public bool RunShieldUnlocked { get; private set; }
        public float RunBerserkBonus { get; private set; }
        /// <summary>Run talent additive damage reduction (0.10 per pick, max 0.60 from talents).</summary>
        public float RunDamageTakenReduction { get; private set; }
        /// <summary>Run talent block chance (0.08 per pick, max 0.50 from talents).</summary>
        public float RunBlockChance { get; private set; }
        /// <summary>Bowman Multishot: dual-arrow chance (0.33 per pick, max 0.99).</summary>
        public float RunMultishotChance { get; private set; }
        /// <summary>Bowman Pierce talent: extra enemies one arrow can pass through.</summary>
        public int RunPierceBonus { get; private set; }
        /// <summary>Paladin Stand Your Ground: bonuses while the player is not moving.</summary>
        public bool RunStandYourGround { get; private set; }

        // --- Boss epic crystal talents (run-scoped) ---
        public int EpicOwnedMask { get; private set; }
        public int PendingEpicChoices { get; private set; }
        public int EpicPicksTaken { get; private set; }
        public float RunDamageTakenMultiplier { get; private set; } = 1f;
        public float RunEpicBossDamageBonus { get; private set; }
        public float RunEpicNormalDamageBonus { get; private set; }
        public float RunExecutionEdgeBonus { get; private set; }
        public bool RunArcaneEcho { get; private set; }
        public bool RunBloodletting { get; private set; }
        public bool RunPhoenixHeart { get; private set; }
        public bool PhoenixHeartUsed { get; private set; }
        /// <summary>Unused Phoenix revive charges this run (0 or 1).</summary>
        public int PhoenixChargesRemaining => _phoenixChargesRemaining;
        public bool RunIronVeil { get; private set; }

        public event Action<int> LevelUpChoiceRequired;
        public event Action<int> EpicChoiceRequired;

        const float IronVeilCooldownSeconds = 20f;
        const float IronVeilAbsorbFraction = 0.3f;
        /// <summary>Long enough to walk out of multi-enemy contact after revive.</summary>
        const float PhoenixInvulnSeconds = 3.5f;
        const int SelfBleedTickCount = 2;

        bool _goldBanked;
        int _secondWindChargesUsed;
        bool _shieldReady;
        float _shieldCooldown;
        float _timeSinceDamaged = 99f;
        float _regenAccumulator;
        float _ironVeilAbsorb;
        float _ironVeilCooldown;
        float _invulnTimer;
        int _phoenixChargesRemaining;
        int _selfBleedDamageRemaining;
        int _selfBleedTicksRemaining;
        float _selfBleedTickTimer;

        public void ConfigureForRun(bool survivalMode)
        {
            SurvivalMode = survivalMode;
            IsCompanion = false;
            CompanionLeader = null;
            DamageOutputScale = 1f;
            MaxHp = GameSave.MaxHp + EquipmentCatalog.CombinedBonusMaxHp();
            CurrentHp = MaxHp;
            RunXp = 0;
            RunGold = 0;
            Level = 1;
            IsDead = false;
            _goldBanked = false;
            _secondWindChargesUsed = 0;
            _shieldReady = false;
            _shieldCooldown = 0f;
            _timeSinceDamaged = 99f;
            XpToNext = GetXpRequiredForLevel(1);
            PendingLevelUpChoices = survivalMode && GameSave.CampfireBlessingUnlocked ? 1 : 0;
            RunSpeedMultiplier = 1f;
            RunDamageMultiplier = 1f;
            RunAttackSpeedMultiplier = 1f;
            RunAttackRangeMultiplier = 1f;
            RunLootRangeMultiplier = 1f;
            // Bowman identity: +40% base crit chance, +40% base crit damage (1.5× → 2.1×).
            if (GameSessionContext.SelectedClass == PlayerClass.Bowman)
            {
                RunCritChance = 0.40f;
                RunCritMultiplier = 1.5f * 1.4f;
            }
            else
            {
                RunCritChance = 0f;
                RunCritMultiplier = 1.5f;
            }
            RunLifesteal = 0f;
            RunBossDamageBonus = 0f;
            RunExecuteBonus = 0f;
            RunGoldFindMultiplier = 1f;
            RunXpMultiplier = 1f;
            RunRegenPerSecond = 0f;
            RunShieldUnlocked = false;
            RunBerserkBonus = 0f;
            RunDamageTakenReduction = 0f;
            RunBlockChance = 0f;
            RunMultishotChance = 0f;
            RunPierceBonus = 0;
            RunStandYourGround = false;
            EpicOwnedMask = 0;
            PendingEpicChoices = 0;
            EpicPicksTaken = 0;
            RunDamageTakenMultiplier = 1f;
            RunEpicBossDamageBonus = 0f;
            RunEpicNormalDamageBonus = 0f;
            RunExecutionEdgeBonus = 0f;
            RunArcaneEcho = false;
            RunBloodletting = false;
            RunPhoenixHeart = false;
            PhoenixHeartUsed = false;
            _phoenixChargesRemaining = 0;
            RunIronVeil = false;
            _ironVeilAbsorb = 0f;
            _ironVeilCooldown = 0f;
            _invulnTimer = 0f;
            _selfBleedDamageRemaining = 0;
            _selfBleedTicksRemaining = 0;
            _selfBleedTickTimer = 0f;
        }

        /// <summary>
        /// Standby hero assist unit: mirrors the leader's run buffs, deals 20% damage, never dies.
        /// </summary>
        public void ConfigureAsCompanion(PlayerStats leader)
        {
            ConfigureForRun(true);
            IsCompanion = true;
            CompanionLeader = leader;
            DamageOutputScale = 0.2f;
            PendingLevelUpChoices = 0;
            PendingEpicChoices = 0;
            MaxHp = 9999;
            CurrentHp = MaxHp;
            SyncRunBuffsFromLeader();
        }

        public void SyncRunBuffsFromLeader()
        {
            if (!IsCompanion || CompanionLeader == null) return;
            var leader = CompanionLeader;
            RunSpeedMultiplier = leader.RunSpeedMultiplier;
            RunDamageMultiplier = leader.RunDamageMultiplier;
            RunAttackSpeedMultiplier = leader.RunAttackSpeedMultiplier;
            RunAttackRangeMultiplier = leader.RunAttackRangeMultiplier;
            RunLootRangeMultiplier = leader.RunLootRangeMultiplier;
            RunCritChance = leader.RunCritChance;
            RunCritMultiplier = leader.RunCritMultiplier;
            RunLifesteal = leader.RunLifesteal;
            RunBossDamageBonus = leader.RunBossDamageBonus;
            RunExecuteBonus = leader.RunExecuteBonus;
            RunGoldFindMultiplier = leader.RunGoldFindMultiplier;
            RunXpMultiplier = leader.RunXpMultiplier;
            RunRegenPerSecond = 0f;
            RunShieldUnlocked = false;
            RunBerserkBonus = leader.RunBerserkBonus;
            RunDamageTakenReduction = leader.RunDamageTakenReduction;
            RunBlockChance = leader.RunBlockChance;
            RunMultishotChance = leader.RunMultishotChance;
            RunPierceBonus = leader.RunPierceBonus;
            RunStandYourGround = leader.RunStandYourGround;
            RunDamageTakenMultiplier = leader.RunDamageTakenMultiplier;
            RunEpicBossDamageBonus = leader.RunEpicBossDamageBonus;
            RunEpicNormalDamageBonus = leader.RunEpicNormalDamageBonus;
            RunExecutionEdgeBonus = leader.RunExecutionEdgeBonus;
            RunArcaneEcho = leader.RunArcaneEcho;
            RunBloodletting = leader.RunBloodletting;
            Level = leader.Level;
        }

        public bool CanAcceptEpicCrystal
        {
            get
            {
                // Companion never owns the pick — evaluate the leader instead.
                if (IsCompanion)
                    return CompanionLeader != null && CompanionLeader.CanAcceptEpicCrystal;

                return SurvivalMode
                       && !IsDead
                       && EpicPicksTaken + PendingEpicChoices < EpicTalentCatalog.MaxPicksPerRun;
            }
        }

        public bool HasEpicTalent(EpicTalentId id) =>
            EpicTalentCatalog.HasTalent(EpicOwnedMask, id);

        /// <summary>Boss crystal pickup — queues an epic talent choice panel on the run leader.</summary>
        public void OfferEpicTalentChoice()
        {
            if (IsCompanion)
            {
                CompanionLeader?.OfferEpicTalentChoice();
                return;
            }

            if (!CanAcceptEpicCrystal) return;
            PendingEpicChoices++;
            EpicChoiceRequired?.Invoke(PendingEpicChoices);
        }

        void Update()
        {
            if (!SurvivalMode || IsDead) return;

            if (IsCompanion)
            {
                SyncRunBuffsFromLeader();
                return;
            }

            if (_invulnTimer > 0f)
                _invulnTimer -= Time.deltaTime;

            _timeSinceDamaged += Time.deltaTime;

            if (RunShieldUnlocked)
            {
                if (!_shieldReady)
                {
                    _shieldCooldown -= Time.deltaTime;
                    if (_shieldCooldown <= 0f)
                        _shieldReady = true;
                }
            }

            if (RunIronVeil)
            {
                if (_ironVeilAbsorb <= 0f)
                {
                    _ironVeilCooldown -= Time.deltaTime;
                    if (_ironVeilCooldown <= 0f)
                        RefreshIronVeilAbsorb();
                }
            }

            UpdateSelfBleed();

            if (RunRegenPerSecond > 0f && _timeSinceDamaged >= RegenOutOfCombatDelay && CurrentHp < MaxHp)
            {
                _regenAccumulator += RunRegenPerSecond * Time.deltaTime;
                if (_regenAccumulator >= 1f)
                {
                    var heal = Mathf.FloorToInt(_regenAccumulator);
                    _regenAccumulator -= heal;
                    Heal(heal);
                }
            }
            else
            {
                _regenAccumulator = 0f;
            }
        }

        void RefreshIronVeilAbsorb()
        {
            _ironVeilAbsorb = Mathf.Max(1f, MaxHp * IronVeilAbsorbFraction);
            _ironVeilCooldown = IronVeilCooldownSeconds;
        }

        void UpdateSelfBleed()
        {
            if (_selfBleedTicksRemaining <= 0) return;

            _selfBleedTickTimer -= Time.deltaTime;
            if (_selfBleedTickTimer > 0f) return;

            _selfBleedTickTimer = 1f;
            var ticksLeft = _selfBleedTicksRemaining;
            var tickDamage = ticksLeft <= 1
                ? _selfBleedDamageRemaining
                : Mathf.Max(1, _selfBleedDamageRemaining / ticksLeft);
            _selfBleedDamageRemaining = Mathf.Max(0, _selfBleedDamageRemaining - tickDamage);
            _selfBleedTicksRemaining--;
            ApplyDirectHpLoss(tickDamage, isBleed: true);
        }

        /// <summary>Bloodletting: 20% of a hit as self-bleed over 2 seconds (2 ticks).</summary>
        void ApplySelfBleedFromHit(int hitAmount)
        {
            if (!RunBloodletting || hitAmount <= 0 || IsDead) return;
            var total = Mathf.Max(1, Mathf.RoundToInt(hitAmount * 0.2f));
            _selfBleedDamageRemaining = total;
            _selfBleedTicksRemaining = SelfBleedTickCount;
            _selfBleedTickTimer = 1f;
        }

        bool IsPaladin => GetComponent<PlayerCombat>() is { BoundClass: PlayerClass.Paladin };

        /// <summary>Stand Your Ground is active only while the player (not a follower) is standing still.</summary>
        public bool IsStandYourGroundActive
        {
            get
            {
                if (!RunStandYourGround) return false;
                var body = IsCompanion && CompanionLeader != null ? CompanionLeader : this;
                var mover = body.GetComponent<TapMovement>();
                return mover != null && !mover.IsMoving;
            }
        }

        /// <summary>Paladin: send 40% of a blocked hit back to its source, or the nearest living enemy.</summary>
        void ReflectPaladinBlock(int incoming, EnemyActor source)
        {
            if (!IsPaladin || incoming <= 0) return;
            var reflected = Mathf.Max(1, Mathf.RoundToInt(incoming * PaladinBlockReflectFraction));
            var target = source != null && source.IsAlive ? source : FindNearestLivingEnemy();
            target?.TakeDamage(reflected);
        }

        static EnemyActor FindNearestLivingEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            var player = GameObject.FindGameObjectWithTag("Player");
            var origin = player != null ? (Vector2)player.transform.position : Vector2.zero;
            var enemies = EnemyRegistry.All;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                var dist = Vector2.Distance(origin, enemy.transform.position);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = enemy;
            }

            return best;
        }

        public void TakeDamage(int amount, EnemyActor source = null)
        {
            if (IsDead || amount <= 0 || IsCompanion) return;
            if (_invulnTimer > 0f) return;

            if (RunShieldUnlocked && _shieldReady)
            {
                _shieldReady = false;
                _shieldCooldown = ShieldCooldownSeconds;
                FloatingDamageNumber.SpawnBlock(transform.position);
                ReflectPaladinBlock(amount, source);
                return;
            }

            // Chance block: run talents + equipped capes (hard cap 50% total).
            var blockChance = Mathf.Min(0.50f, RunBlockChance + EquipmentCatalog.CombinedBlockChance());
            if (blockChance > 0f && UnityEngine.Random.value < blockChance)
            {
                _timeSinceDamaged = 0f;
                FloatingDamageNumber.SpawnBlock(transform.position);
                ReflectPaladinBlock(amount, source);
                return;
            }

            if (GameSave.ThickHideLevel > 0)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * GameSave.ThickHideDamageTakenMultiplier));

            // Additive DR from level-up Defense talent + equipment (cap 60% total reduction).
            var reduction = Mathf.Min(TalentDefenseCap, RunDamageTakenReduction + EquipmentCatalog.CombinedDamageReduction());
            if (reduction > 0f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - reduction)));

            if (RunDamageTakenMultiplier > 1.001f || RunDamageTakenMultiplier < 0.999f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * RunDamageTakenMultiplier));

            if (IsStandYourGroundActive)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * StandYourGroundDamageTakenMultiplier));

            if (RunIronVeil && _ironVeilAbsorb > 0f)
            {
                var absorbed = Mathf.Min(_ironVeilAbsorb, amount);
                _ironVeilAbsorb -= absorbed;
                amount -= Mathf.RoundToInt(absorbed);
                if (_ironVeilAbsorb <= 0f)
                    _ironVeilCooldown = IronVeilCooldownSeconds;
                if (amount <= 0)
                {
                    _timeSinceDamaged = 0f;
                    return;
                }
            }

            // If Phoenix consumed the lethal hit, do not re-apply Bloodletting from that blow.
            var revived = ApplyDirectHpLoss(amount, isBleed: false);
            if (!revived && !IsDead)
                ApplySelfBleedFromHit(amount);
        }

        /// <summary>
        /// Applies HP loss. Returns true if Phoenix Heart prevented death on this hit.
        /// </summary>
        bool ApplyDirectHpLoss(int amount, bool isBleed)
        {
            if (IsDead || amount <= 0 || IsCompanion) return false;
            if (_invulnTimer > 0f) return false;

            if (isBleed)
                FloatingDamageNumber.SpawnBleed(transform.position, amount);
            else
                FloatingDamageNumber.Spawn(transform.position, amount, isHeroHit: true);

            var nextHp = CurrentHp - amount;
            _timeSinceDamaged = 0f;

            // Lethal hit: spend Phoenix Heart before ever marking the player dead.
            if (nextHp <= 0)
            {
                if (TryTriggerPhoenixHeart())
                    return true;

                CurrentHp = 0;
                Die();
                return false;
            }

            CurrentHp = nextHp;

            var maxCharges = GameSave.SecondWindMaxCharges;
            if (maxCharges > 0
                && _secondWindChargesUsed < maxCharges
                && CurrentHp <= MaxHp * 0.2f)
            {
                _secondWindChargesUsed++;
                Heal(Mathf.Max(1, Mathf.RoundToInt(MaxHp * 0.3f)));
            }

            return false;
        }

        /// <summary>
        /// True if the run still has an unused Phoenix revive (charges, flag, or owned mask).
        /// </summary>
        bool CanUsePhoenixHeart()
        {
            if (IsCompanion) return false;
            if (_phoenixChargesRemaining > 0) return true;
            // Recover from flag/mask desync (e.g. snapshot or partial apply).
            if (!PhoenixHeartUsed && (RunPhoenixHeart || HasEpicTalent(EpicTalentId.PhoenixHeart)))
            {
                _phoenixChargesRemaining = 1;
                RunPhoenixHeart = true;
                return true;
            }

            return false;
        }

        void ArmPhoenixHeart()
        {
            RunPhoenixHeart = true;
            PhoenixHeartUsed = false;
            _phoenixChargesRemaining = 1;
            EpicOwnedMask = EpicTalentCatalog.WithTalent(EpicOwnedMask, EpicTalentId.PhoenixHeart);
        }

        /// <summary>
        /// Revive once at 40% HP with brief i-frames. Returns true if the death was prevented.
        /// </summary>
        bool TryTriggerPhoenixHeart()
        {
            if (!CanUsePhoenixHeart()) return false;

            // I-frames first so same-frame multi-hits cannot re-kill after revive.
            _invulnTimer = PhoenixInvulnSeconds;
            _phoenixChargesRemaining = 0;
            RunPhoenixHeart = true;
            PhoenixHeartUsed = true;
            IsDead = false;
            CurrentHp = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, MaxHp) * 0.4f));
            _selfBleedDamageRemaining = 0;
            _selfBleedTicksRemaining = 0;
            _selfBleedTickTimer = 0f;
            GameHud.Instance?.ShowBanner("Phoenix Heart! Revived!", 3f);
            return true;
        }

        public void Heal(int amount)
        {
            if (IsCompanion)
            {
                CompanionLeader?.Heal(amount);
                return;
            }

            if (!SurvivalMode || IsDead || amount <= 0) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public static int GetXpRequiredForLevel(int level) =>
            50 + level * 35 + level * level * 8;

        public void AddXp(int amount)
        {
            // Companion kills / vacuum must never level a silent companion unit.
            if (IsCompanion)
            {
                CompanionLeader?.AddXp(amount);
                return;
            }

            if (!SurvivalMode || IsDead || amount <= 0) return;
            if (Level >= StatCaps.MaxRunLevel) return;

            amount = Mathf.Max(1, Mathf.RoundToInt(
                amount * RunXpMultiplier * Achievements.AchievementXpMultiplier));
            RunXp += amount;

            var leveled = false;
            while (Level < StatCaps.MaxRunLevel && RunXp >= XpToNext)
            {
                RunXp -= XpToNext;
                Level++;
                if (Level >= StatCaps.MaxRunLevel)
                {
                    RunXp = 0;
                    XpToNext = GetXpRequiredForLevel(StatCaps.MaxRunLevel);
                    PendingLevelUpChoices++;
                    leveled = true;
                    break;
                }

                XpToNext = GetXpRequiredForLevel(Level);
                PendingLevelUpChoices++;
                leveled = true;
            }

            if (leveled)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        static bool RoomBelow(float current, float cap) => current < cap - 0.001f;

        public static int HpGrantForLevel(int level) =>
            TalentHpBase + TalentHpPerLevel * Mathf.Max(1, level);

        static int HpButtonAmount(PlayerStats stats)
        {
            var grant = HpGrantForLevel(stats != null ? stats.Level : 1);
            if (stats == null) return grant;
            return Mathf.Max(1, Mathf.Min(grant, StatCaps.RunMaxHp - stats.MaxHp));
        }

        public bool CanOfferSpeedTalent => RoomBelow(RunSpeedMultiplier, StatCaps.RunMaxSpeedMultiplier);
        public bool CanOfferAttackTalent => RoomBelow(RunDamageMultiplier, StatCaps.RunMaxDamageMultiplier);
        public bool CanOfferAttackRangeTalent =>
            RoomBelow(AttackRangeMultiplier, StatCaps.RunMaxAttackRangeMultiplier);
        public bool CanOfferHpTalent => MaxHp < StatCaps.RunMaxHp;
        // Higher caps so Bowman (starts 40% / 2.1×) still gains from crit talent picks.
        public bool CanOfferCritChance => RoomBelow(RunCritChance, TalentCritChanceCap);
        public bool CanOfferCritDamage => RoomBelow(RunCritMultiplier, TalentCritDamageCap);
        public bool CanOfferLifesteal => RoomBelow(RunLifesteal, TalentLifestealCap);
        public bool CanOfferBossHunter => RoomBelow(RunBossDamageBonus, TalentBossCap);
        public bool CanOfferExecute => RoomBelow(RunExecuteBonus, TalentExecuteCap);
        public bool CanOfferGoldFind => RoomBelow(RunGoldFindMultiplier, TalentGoldCap);
        public bool CanOfferRegen => RoomBelow(RunRegenPerSecond, TalentRegenCap);
        public bool CanOfferShield => !RunShieldUnlocked;
        public bool CanOfferBerserk => RoomBelow(RunBerserkBonus, TalentBerserkCap);
        public bool CanOfferXpBoost => RoomBelow(RunXpMultiplier, TalentXpCap);
        /// <summary>Defense talent: −10% damage taken per pick, max −60% from this talent and armor combined.</summary>
        public bool CanOfferDefenseTalent =>
            RoomBelow(RunDamageTakenReduction, TalentDefenseCap)
            && RoomBelow(RunDamageTakenReduction + EquipmentCatalog.CombinedDamageReduction(), TalentDefenseCap);
        /// <summary>Block talent: +8% block per pick, max 50% from this talent and armor combined.</summary>
        public bool CanOfferBlockTalent =>
            RoomBelow(RunBlockChance, TalentBlockCap)
            && RoomBelow(RunBlockChance + EquipmentCatalog.CombinedBlockChance(), TalentBlockCap);
        /// <summary>Bowman Multishot: +33% dual-shot chance per pick, max 99% (3 stacks).</summary>
        public bool CanOfferMultishotTalent =>
            GameSessionContext.SelectedClass == PlayerClass.Bowman
            && RoomBelow(RunMultishotChance, TalentMultishotCap);
        /// <summary>Bowman Pierce: +2 pierce hits per pick, max +6 (three picks).</summary>
        public bool CanOfferPierceTalent =>
            GameSessionContext.SelectedClass == PlayerClass.Bowman
            && RunPierceBonus < TalentPierceCap;
        /// <summary>Paladin Stand Your Ground: one pick per run.</summary>
        public bool CanOfferStandYourGround =>
            GameSessionContext.SelectedClass == PlayerClass.Paladin
            && !RunStandYourGround;

        public static List<RunLevelChoice> RollLevelUpChoices(PlayerStats stats, int count = 4)
        {
            var pool = new List<RunLevelChoice>();
            if (stats == null)
            {
                pool.AddRange(new[]
                {
                    RunLevelChoice.Speed,
                    RunLevelChoice.Hp,
                    RunLevelChoice.Attack,
                    RunLevelChoice.AttackSpeed,
                    RunLevelChoice.AttackRange,
                    RunLevelChoice.LootRange,
                    RunLevelChoice.CritChance,
                    RunLevelChoice.Lifesteal,
                    RunLevelChoice.BossHunter
                });
            }
            else
            {
                var isBowman = GameSessionContext.SelectedClass == PlayerClass.Bowman;
                if (stats.CanOfferSpeedTalent) pool.Add(RunLevelChoice.Speed);
                if (stats.CanOfferHpTalent) pool.Add(RunLevelChoice.Hp);
                if (stats.CanOfferAttackTalent) pool.Add(RunLevelChoice.Attack);
                pool.Add(RunLevelChoice.AttackSpeed);
                // Bowman swaps Attack Range for Multishot; other classes keep range.
                if (isBowman)
                {
                    if (stats.CanOfferMultishotTalent) pool.Add(RunLevelChoice.Multishot);
                    if (stats.CanOfferPierceTalent) pool.Add(RunLevelChoice.Pierce);
                }
                else if (stats.CanOfferAttackRangeTalent)
                {
                    pool.Add(RunLevelChoice.AttackRange);
                }

                pool.Add(RunLevelChoice.LootRange);
                if (stats.CanOfferCritChance) pool.Add(RunLevelChoice.CritChance);
                if (stats.CanOfferCritDamage) pool.Add(RunLevelChoice.CritDamage);
                if (stats.CanOfferLifesteal) pool.Add(RunLevelChoice.Lifesteal);
                if (stats.CanOfferBossHunter) pool.Add(RunLevelChoice.BossHunter);
                if (stats.CanOfferExecute) pool.Add(RunLevelChoice.Execute);
                if (stats.CanOfferGoldFind) pool.Add(RunLevelChoice.GoldFind);
                if (stats.CanOfferRegen) pool.Add(RunLevelChoice.Regen);
                if (stats.CanOfferShield) pool.Add(RunLevelChoice.Shield);
                if (stats.CanOfferBerserk) pool.Add(RunLevelChoice.Berserk);
                if (stats.CanOfferXpBoost) pool.Add(RunLevelChoice.XpBoost);
                if (stats.CanOfferDefenseTalent) pool.Add(RunLevelChoice.Defense);
                if (stats.CanOfferBlockTalent) pool.Add(RunLevelChoice.Block);
                if (stats.CanOfferStandYourGround) pool.Add(RunLevelChoice.StandYourGround);
            }

            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, Mathf.Min(count, pool.Count));
        }

        public static string GetChoiceLabel(RunLevelChoice choice, PlayerStats stats = null)
        {
            return choice switch
            {
                RunLevelChoice.Speed => "+10% Move Speed",
                RunLevelChoice.Hp => $"+{HpButtonAmount(stats)} Max HP",
                RunLevelChoice.Attack => "+12% Attack Damage",
                RunLevelChoice.AttackSpeed => "+12% Attack Speed",
                RunLevelChoice.AttackRange => "+10% Attack Range",
                RunLevelChoice.LootRange => "+15% Loot Range",
                RunLevelChoice.CritChance => "+10% Crit Chance",
                RunLevelChoice.CritDamage => "+30% Crit Damage",
                RunLevelChoice.Lifesteal => "+4% Lifesteal",
                RunLevelChoice.BossHunter => "+25% Damage vs Bosses",
                RunLevelChoice.Execute => "+50% Damage under 25% HP (yours)",
                RunLevelChoice.GoldFind => "+15% Gold Find",
                RunLevelChoice.Regen => "+3 HP/sec out of combat",
                RunLevelChoice.Shield => $"Block 1 hit every {ShieldCooldownSeconds:0}s",
                RunLevelChoice.Berserk => "+25% Damage over 90% HP",
                RunLevelChoice.XpBoost => "+15% XP Gain",
                RunLevelChoice.Defense => "−10% Damage Taken",
                RunLevelChoice.Block => "+8% Block Chance",
                RunLevelChoice.Multishot => "+33% Multishot Chance",
                RunLevelChoice.Pierce => "+2 Pierce",
                RunLevelChoice.StandYourGround => "Stand Your Ground\n+10% damage, −5% taken while still",
                _ => choice.ToString()
            };
        }

        public void ApplyRunLevelChoice(RunLevelChoice choice)
        {
            if (PendingLevelUpChoices <= 0) return;

            switch (choice)
            {
                case RunLevelChoice.Speed:
                    if (!CanOfferSpeedTalent) break;
                    RunSpeedMultiplier = Mathf.Min(StatCaps.RunMaxSpeedMultiplier, RunSpeedMultiplier * TalentSpeedMul);
                    break;
                case RunLevelChoice.Hp:
                    if (!CanOfferHpTalent) break;
                    var hpGrant = Mathf.Min(HpGrantForLevel(Level), StatCaps.RunMaxHp - MaxHp);
                    MaxHp += hpGrant;
                    CurrentHp = Mathf.Min(MaxHp, CurrentHp + hpGrant);
                    break;
                case RunLevelChoice.Attack:
                    if (!CanOfferAttackTalent) break;
                    RunDamageMultiplier = Mathf.Min(StatCaps.RunMaxDamageMultiplier, RunDamageMultiplier * TalentAttackMul);
                    break;
                case RunLevelChoice.AttackSpeed:
                    RunAttackSpeedMultiplier *= TalentAttackSpeedMul;
                    break;
                case RunLevelChoice.AttackRange:
                    if (!CanOfferAttackRangeTalent) break;
                    RunAttackRangeMultiplier = Mathf.Min(
                        StatCaps.RunMaxAttackRangeMultiplier / Mathf.Max(0.01f, GameSave.AttackRangeMultiplier),
                        RunAttackRangeMultiplier * TalentRangeMul);
                    break;
                case RunLevelChoice.LootRange:
                    RunLootRangeMultiplier *= TalentLootMul;
                    break;
                case RunLevelChoice.CritChance:
                    if (!CanOfferCritChance) break;
                    RunCritChance = Mathf.Min(TalentCritChanceCap, RunCritChance + TalentCritChanceStep);
                    break;
                case RunLevelChoice.CritDamage:
                    if (!CanOfferCritDamage) break;
                    RunCritMultiplier = Mathf.Min(TalentCritDamageCap, RunCritMultiplier + TalentCritDamageStep);
                    break;
                case RunLevelChoice.Lifesteal:
                    if (!CanOfferLifesteal) break;
                    RunLifesteal = Mathf.Min(TalentLifestealCap, RunLifesteal + TalentLifestealStep);
                    break;
                case RunLevelChoice.BossHunter:
                    if (!CanOfferBossHunter) break;
                    RunBossDamageBonus = Mathf.Min(TalentBossCap, RunBossDamageBonus + TalentBossStep);
                    break;
                case RunLevelChoice.Execute:
                    if (!CanOfferExecute) break;
                    RunExecuteBonus = Mathf.Min(TalentExecuteCap, RunExecuteBonus + TalentExecuteStep);
                    break;
                case RunLevelChoice.GoldFind:
                    if (!CanOfferGoldFind) break;
                    RunGoldFindMultiplier = Mathf.Min(TalentGoldCap, RunGoldFindMultiplier * TalentGoldMul);
                    break;
                case RunLevelChoice.Regen:
                    if (!CanOfferRegen) break;
                    RunRegenPerSecond = Mathf.Min(TalentRegenCap, RunRegenPerSecond + TalentRegenStep);
                    break;
                case RunLevelChoice.Shield:
                    if (!CanOfferShield) break;
                    RunShieldUnlocked = true;
                    _shieldReady = true;
                    _shieldCooldown = 0f;
                    break;
                case RunLevelChoice.Berserk:
                    if (!CanOfferBerserk) break;
                    RunBerserkBonus = Mathf.Min(TalentBerserkCap, RunBerserkBonus + TalentBerserkStep);
                    break;
                case RunLevelChoice.XpBoost:
                    if (!CanOfferXpBoost) break;
                    RunXpMultiplier = Mathf.Min(TalentXpCap, RunXpMultiplier * TalentXpMul);
                    break;
                case RunLevelChoice.Defense:
                    if (!CanOfferDefenseTalent) break;
                    RunDamageTakenReduction = Mathf.Min(TalentDefenseCap, RunDamageTakenReduction + TalentDefenseStep);
                    break;
                case RunLevelChoice.Block:
                    if (!CanOfferBlockTalent) break;
                    RunBlockChance = Mathf.Min(TalentBlockCap, RunBlockChance + TalentBlockStep);
                    break;
                case RunLevelChoice.Multishot:
                    if (!CanOfferMultishotTalent) break;
                    RunMultishotChance = Mathf.Min(TalentMultishotCap, RunMultishotChance + TalentMultishotStep);
                    break;
                case RunLevelChoice.Pierce:
                    if (!CanOfferPierceTalent) break;
                    RunPierceBonus = Mathf.Min(TalentPierceCap, RunPierceBonus + TalentPierceStep);
                    break;
                case RunLevelChoice.StandYourGround:
                    if (!CanOfferStandYourGround) break;
                    RunStandYourGround = true;
                    break;
            }

            PendingLevelUpChoices--;
            if (PendingLevelUpChoices > 0)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        /// <summary>Apply a boss-crystal talent. Returns false if the pick was ignored.</summary>
        public bool ApplyEpicTalent(EpicTalentId id)
        {
            if (PendingEpicChoices <= 0 || id == EpicTalentId.None) return false;
            if (EpicTalentCatalog.IsUnique(id) && HasEpicTalent(id))
            {
                // Already owned: still consume the crystal pick so the UI can close.
                PendingEpicChoices--;
                if (PendingEpicChoices > 0)
                    EpicChoiceRequired?.Invoke(PendingEpicChoices);
                // Re-arm Phoenix if the mask says we own it but the charge was lost.
                if (id == EpicTalentId.PhoenixHeart && !PhoenixHeartUsed)
                    ArmPhoenixHeart();
                return true;
            }

            EpicOwnedMask = EpicTalentCatalog.WithTalent(EpicOwnedMask, id);
            EpicPicksTaken++;

            switch (id)
            {
                case EpicTalentId.Bloodforged:
                    RunDamageMultiplier *= 1.25f;
                    RunDamageTakenMultiplier *= 1.1f;
                    break;
                case EpicTalentId.IronVeil:
                    RunIronVeil = true;
                    RefreshIronVeilAbsorb();
                    break;
                case EpicTalentId.ExecutionersEdge:
                    RunExecutionEdgeBonus = Mathf.Max(RunExecutionEdgeBonus, 0.4f);
                    break;
                case EpicTalentId.GildedGreed:
                    RunGoldFindMultiplier *= 1.4f;
                    RunXpMultiplier *= 1.2f;
                    break;
                case EpicTalentId.TempestStrikes:
                    RunAttackSpeedMultiplier *= 1.25f;
                    RunSpeedMultiplier *= 1.15f;
                    break;
                case EpicTalentId.SoulDrain:
                    RunLifesteal = Mathf.Min(0.25f, RunLifesteal + 0.08f);
                    MaxHp = Mathf.Min(StatCaps.RunMaxHp, MaxHp + 10);
                    CurrentHp = Mathf.Min(MaxHp, CurrentHp + 10);
                    break;
                case EpicTalentId.BossBreaker:
                    RunEpicBossDamageBonus = Mathf.Max(RunEpicBossDamageBonus, 0.35f);
                    RunEpicNormalDamageBonus = Mathf.Max(RunEpicNormalDamageBonus, 0.1f);
                    break;
                case EpicTalentId.ArcaneEcho:
                    RunArcaneEcho = true;
                    break;
                case EpicTalentId.PhoenixHeart:
                    ArmPhoenixHeart();
                    break;
                case EpicTalentId.TreasureMagnet:
                    RunLootRangeMultiplier *= 1.5f;
                    break;
                case EpicTalentId.Bloodletting:
                    RunBloodletting = true;
                    break;
            }

            PendingEpicChoices--;
            if (PendingEpicChoices > 0)
                EpicChoiceRequired?.Invoke(PendingEpicChoices);
            return true;
        }

        public void AddRunGold(int amount)
        {
            if (IsCompanion)
            {
                CompanionLeader?.AddRunGold(amount);
                return;
            }

            if (!SurvivalMode || IsDead || amount <= 0 || _goldBanked) return;
            // GameSave.GoldFindMultiplier already includes equipped jewelry.
            var mult = GameSave.GoldFindMultiplier * RunGoldFindMultiplier;
            RunGold += Mathf.Max(1, Mathf.RoundToInt(amount * mult));
        }

        /// <summary>Retreat / debug summary of live run stats and owned epic talents.</summary>
        public string BuildRunStatusSummary()
        {
            var sb = new System.Text.StringBuilder(512);
            sb.AppendLine($"Lv {Level}  ·  HP {CurrentHp}/{MaxHp}  ·  Gold {RunGold}");
            sb.AppendLine(
                $"DMG x{RunDamageMultiplier:0.##}  ·  SPD x{RunSpeedMultiplier:0.##}  ·  AS x{RunAttackSpeedMultiplier:0.##}");
            sb.AppendLine(
                $"Range x{AttackRangeMultiplier:0.##}  ·  Loot x{EffectiveLootRangeMultiplier:0.##}");
            sb.AppendLine(
                $"Crit {RunCritChance * 100f:0}% / x{RunCritMultiplier:0.##}  ·  LS {RunLifesteal * 100f:0}%");
            sb.AppendLine(
                $"DR {RunDamageTakenReduction * 100f:0}%  ·  Block {RunBlockChance * 100f:0}%");

            if (RunBossDamageBonus > 0f || RunEpicBossDamageBonus > 0f)
                sb.AppendLine(
                    $"Boss dmg +{(RunBossDamageBonus + RunEpicBossDamageBonus) * 100f:0}%");
            if (RunExecuteBonus > 0f || RunExecutionEdgeBonus > 0f)
                sb.AppendLine(
                    $"Execute +{RunExecuteBonus * 100f:0}% / Edge +{RunExecutionEdgeBonus * 100f:0}%");
            if (RunBerserkBonus > 0f)
                sb.AppendLine($"Berserk +{RunBerserkBonus * 100f:0}% over 90% HP");
            if (RunRegenPerSecond > 0f)
                sb.AppendLine($"Regen {RunRegenPerSecond:0.#}/s OOC");
            if (RunShieldUnlocked)
                sb.AppendLine($"Shield: armed every {ShieldCooldownSeconds:0}s");
            if (RunStandYourGround)
                sb.AppendLine("Stand Your Ground: +10% dmg, −5% taken while still");
            if (RunMultishotChance > 0f)
                sb.AppendLine($"Multishot {RunMultishotChance * 100f:0}%");
            if (RunPierceBonus > 0)
                sb.AppendLine($"Pierce +{RunPierceBonus}");
            if (RunArcaneEcho)
                sb.AppendLine("Arcane Echo");
            if (RunBloodletting)
                sb.AppendLine("Bloodletting");
            if (RunIronVeil)
                sb.AppendLine("Iron Veil");
            if (RunPhoenixHeart || HasEpicTalent(EpicTalentId.PhoenixHeart))
                sb.AppendLine(PhoenixHeartUsed || _phoenixChargesRemaining <= 0
                    ? "Phoenix Heart: spent"
                    : "Phoenix Heart: ready");

            sb.AppendLine();
            sb.Append("Epic talents: ");
            var anyEpic = false;
            foreach (var id in EpicTalentCatalog.All)
            {
                if (!HasEpicTalent(id)) continue;
                if (anyEpic) sb.Append(", ");
                sb.Append(EpicTalentCatalog.GetTitle(id));
                anyEpic = true;
            }

            if (!anyEpic)
                sb.Append("none");

            var pending = PendingLevelUpChoices + PendingEpicChoices;
            if (pending > 0)
                sb.Append($"\nPending picks: {pending}");

            return sb.ToString().TrimEnd();
        }

        public void BankRunGoldToSave()
        {
            if (_goldBanked || RunGold <= 0) return;
            GameSave.BankFromRun(RunGold);
            RunGold = 0;
            _goldBanked = true;
        }

        void Die()
        {
            if (IsDead) return;

            // Safety net: any death path still respects an unused Phoenix Heart.
            if (TryTriggerPhoenixHeart())
                return;

            // Last resort: mask says Phoenix is owned and unused — force arm + revive.
            if (!IsCompanion
                && !PhoenixHeartUsed
                && HasEpicTalent(EpicTalentId.PhoenixHeart))
            {
                ArmPhoenixHeart();
                if (TryTriggerPhoenixHeart())
                    return;
            }

            IsDead = true;
            CurrentHp = 0;
            _phoenixChargesRemaining = 0;

            if (SurvivalMode)
            {
                GameSave.RecordDeath();
                var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
                if (session != null)
                {
                    GameSave.RecordHighestRound(session.CurrentRound);
                    var weaponClass = GameSessionContext.SelectedClass;
                    if (session.MapKind == SurvivalMapKind.Unlimited)
                    {
                        GameSave.RecordUnlimitedRound(session.CurrentRound);
                        // Depth credit on death (even mid-round) — matches prior Bren milestone behavior.
                        QuestCatalog.NotifyUnlimitedRound(session.CurrentRound);
                        GameSave.RecordWeaponUnlimitedRound(weaponClass, session.CurrentRound);
                    }
                    else if (session.MapKind == SurvivalMapKind.Dungeon)
                    {
                        GameSave.RecordDungeonRound(session.CurrentRound);
                        GameSave.RecordWeaponDungeonRound(weaponClass, session.CurrentRound);
                    }
                    else if (session.MapKind == SurvivalMapKind.Crypt)
                        GameSave.RecordCryptRound(session.CurrentRound);
                    Achievements.EvaluateWeaponTierAchievements();
                }
            }

            BankRunGoldToSave();
        }

        /// <summary>
        /// Player world move speed matching <see cref="TapMovement"/> (base × permanent × run).
        /// Used to cap flying enemies so they cannot outrun the hero.
        /// </summary>
        public float EffectiveMoveSpeed
        {
            get
            {
                var tap = GetComponent<TapMovement>();
                if (tap != null) return tap.CurrentMoveSpeed;
                return TapMovement.DefaultBaseSpeed * GameSave.SpeedMultiplier * RunSpeedMultiplier
                       * EquipmentCatalog.CombinedMoveSpeedMultiplier();
            }
        }

        public float Damage =>
            10f * GameSave.DamageMultiplier * EquipmentCatalog.CombinedDamageMultiplier()
            * WeaponCatalog.DamageMultiplier() * RunDamageMultiplier * DamageOutputScale;

        public float EffectiveAttackSpeed =>
            RunAttackSpeedMultiplier * EquipmentCatalog.CombinedAttackSpeedMultiplier()
            * WeaponCatalog.AttackSpeedMultiplier()
            * (IsBerserkActive ? 1f + RunBerserkBonus : 1f);

        /// <summary>Brief i-frames after talent/epic picks so the player can reposition safely.</summary>
        public void GrantTalentSelectionIFrames(float seconds = 1f)
        {
            if (seconds > 0f)
                _invulnTimer = Mathf.Max(_invulnTimer, seconds);
        }

        /// <summary>Berserk: bonus while the hero is healthy (over 90% HP).</summary>
        public bool IsBerserkActive =>
            RunBerserkBonus > 0f && MaxHp > 0 && CurrentHp >= MaxHp * 0.9f;

        /// <summary>Execute talent: bonus while the hero is under 25% HP (not enemy HP).</summary>
        public bool IsExecuteActive =>
            RunExecuteBonus > 0f && MaxHp > 0 && CurrentHp <= MaxHp * 0.25f;

        public int RollDamage(EnemyActor target, float weaponMultiplier = 1f)
            => RollDamage(target, weaponMultiplier, out _);

        public int RollDamage(EnemyActor target, float weaponMultiplier, out bool isCrit)
        {
            isCrit = false;
            var dmg = Damage * weaponMultiplier;

            if (IsBerserkActive)
                dmg *= 1f + RunBerserkBonus;

            // Execute uses the hero's HP ratio, not the target's.
            if (IsExecuteActive)
                dmg *= 1f + RunExecuteBonus;

            if (IsStandYourGroundActive)
                dmg *= StandYourGroundDamageMultiplier;

            if (GameSave.ShatteringUnlocked && target != null && target.IsChilled)
                dmg *= ShatteringDamageMultiplier;

            if (target != null)
            {
                if (target.IsBoss)
                {
                    if (RunBossDamageBonus > 0f)
                        dmg *= 1f + RunBossDamageBonus;
                    if (RunEpicBossDamageBonus > 0f)
                        dmg *= 1f + RunEpicBossDamageBonus;
                }
                else
                {
                    if (RunEpicNormalDamageBonus > 0f)
                        dmg *= 1f + RunEpicNormalDamageBonus;
                }

                // Executioner's Edge epic still keys off enemy HP.
                if (RunExecutionEdgeBonus > 0f && target.HpRatio <= 0.3f)
                    dmg *= 1f + RunExecutionEdgeBonus;
            }

            if (RunCritChance > 0f && UnityEngine.Random.value < RunCritChance)
            {
                dmg *= RunCritMultiplier;
                isCrit = true;
            }

            return Mathf.Max(1, Mathf.RoundToInt(dmg));
        }

        public void OnDamageDealt(int damageDealt)
        {
            if (damageDealt <= 0 || RunLifesteal <= 0f) return;
            var healTarget = IsCompanion && CompanionLeader != null ? CompanionLeader : this;
            healTarget.Heal(Mathf.Max(1, Mathf.RoundToInt(damageDealt * RunLifesteal)));
        }

        /// <summary>Credit loot rewards to the real player (companions never bank their own run gold/xp).</summary>
        public PlayerStats LootCreditTarget =>
            IsCompanion && CompanionLeader != null ? CompanionLeader : this;

        public float EffectiveLootRangeMultiplier =>
            RunLootRangeMultiplier * GameSave.LootRangeMultiplier;

        public void CaptureSnapshot(out SurvivalRunSnapshot snapshot)
        {
            snapshot = new SurvivalRunSnapshot
            {
                HasData = true,
                MaxHp = MaxHp,
                CurrentHp = CurrentHp,
                RunXp = RunXp,
                RunGold = RunGold,
                Level = Level,
                XpToNext = XpToNext,
                PendingLevelUpChoices = PendingLevelUpChoices,
                RunSpeedMultiplier = RunSpeedMultiplier,
                RunDamageMultiplier = RunDamageMultiplier,
                RunAttackSpeedMultiplier = RunAttackSpeedMultiplier,
                RunAttackRangeMultiplier = RunAttackRangeMultiplier,
                RunLootRangeMultiplier = RunLootRangeMultiplier,
                RunCritChance = RunCritChance,
                RunCritMultiplier = RunCritMultiplier,
                RunLifesteal = RunLifesteal,
                RunBossDamageBonus = RunBossDamageBonus,
                RunExecuteBonus = RunExecuteBonus,
                RunGoldFindMultiplier = RunGoldFindMultiplier,
                RunXpMultiplier = RunXpMultiplier,
                RunRegenPerSecond = RunRegenPerSecond,
                RunShieldUnlocked = RunShieldUnlocked,
                RunBerserkBonus = RunBerserkBonus,
                RunDamageTakenReduction = RunDamageTakenReduction,
                RunBlockChance = RunBlockChance,
                RunMultishotChance = RunMultishotChance,
                RunPierceBonus = RunPierceBonus,
                RunStandYourGround = RunStandYourGround,
                SecondWindChargesUsed = _secondWindChargesUsed,
                SecondWindUsed = _secondWindChargesUsed > 0,
                EpicOwnedMask = EpicOwnedMask,
                PendingEpicChoices = PendingEpicChoices,
                EpicPicksTaken = EpicPicksTaken,
                PhoenixHeartUsed = PhoenixHeartUsed,
                RunIronVeil = RunIronVeil,
                IronVeilAbsorb = _ironVeilAbsorb,
                IronVeilCooldown = _ironVeilCooldown,
                RunDamageTakenMultiplier = RunDamageTakenMultiplier,
                RunEpicBossDamageBonus = RunEpicBossDamageBonus,
                RunEpicNormalDamageBonus = RunEpicNormalDamageBonus,
                RunExecutionEdgeBonus = RunExecutionEdgeBonus,
                RunArcaneEcho = RunArcaneEcho,
                RunBloodletting = RunBloodletting,
                RunPhoenixHeart = RunPhoenixHeart,
                InvulnTimer = _invulnTimer
            };
        }

        public void RestoreSnapshot(SurvivalRunSnapshot snapshot)
        {
            if (!snapshot.HasData) return;

            MaxHp = snapshot.MaxHp;
            CurrentHp = snapshot.CurrentHp;
            RunXp = snapshot.RunXp;
            RunGold = snapshot.RunGold;
            Level = Mathf.Min(StatCaps.MaxRunLevel, snapshot.Level);
            XpToNext = Level >= StatCaps.MaxRunLevel
                ? GetXpRequiredForLevel(StatCaps.MaxRunLevel)
                : snapshot.XpToNext > 0 ? snapshot.XpToNext : GetXpRequiredForLevel(Level);
            PendingLevelUpChoices = snapshot.PendingLevelUpChoices;
            RunSpeedMultiplier = snapshot.RunSpeedMultiplier;
            RunDamageMultiplier = snapshot.RunDamageMultiplier;
            RunAttackSpeedMultiplier = snapshot.RunAttackSpeedMultiplier > 0f
                ? snapshot.RunAttackSpeedMultiplier
                : 1f;
            RunAttackRangeMultiplier = snapshot.RunAttackRangeMultiplier > 0f
                ? snapshot.RunAttackRangeMultiplier
                : 1f;
            RunLootRangeMultiplier = snapshot.RunLootRangeMultiplier > 0f
                ? snapshot.RunLootRangeMultiplier
                : 1f;
            RunCritChance = snapshot.RunCritChance;
            RunCritMultiplier = snapshot.RunCritMultiplier > 0f ? snapshot.RunCritMultiplier : 1.5f;
            RunLifesteal = snapshot.RunLifesteal;
            RunBossDamageBonus = snapshot.RunBossDamageBonus;
            RunExecuteBonus = snapshot.RunExecuteBonus;
            RunGoldFindMultiplier = snapshot.RunGoldFindMultiplier > 0f ? snapshot.RunGoldFindMultiplier : 1f;
            RunXpMultiplier = snapshot.RunXpMultiplier > 0f ? snapshot.RunXpMultiplier : 1f;
            RunRegenPerSecond = snapshot.RunRegenPerSecond;
            RunShieldUnlocked = snapshot.RunShieldUnlocked;
            RunBerserkBonus = snapshot.RunBerserkBonus;
            RunDamageTakenReduction = Mathf.Clamp01(snapshot.RunDamageTakenReduction);
            RunBlockChance = Mathf.Clamp01(snapshot.RunBlockChance);
            RunMultishotChance = Mathf.Clamp(snapshot.RunMultishotChance, 0f, 0.99f);
            RunPierceBonus = Mathf.Clamp(snapshot.RunPierceBonus, 0, 6);
            RunStandYourGround = snapshot.RunStandYourGround;
            _secondWindChargesUsed = snapshot.SecondWindChargesUsed > 0
                ? snapshot.SecondWindChargesUsed
                : snapshot.SecondWindUsed ? 1 : 0;
            if (RunShieldUnlocked)
            {
                _shieldReady = true;
                _shieldCooldown = 0f;
            }

            EpicOwnedMask = snapshot.EpicOwnedMask;
            PendingEpicChoices = snapshot.PendingEpicChoices;
            EpicPicksTaken = snapshot.EpicPicksTaken;
            PhoenixHeartUsed = snapshot.PhoenixHeartUsed;
            RunIronVeil = snapshot.RunIronVeil;
            _ironVeilAbsorb = snapshot.IronVeilAbsorb;
            _ironVeilCooldown = snapshot.IronVeilCooldown;
            RunDamageTakenMultiplier = snapshot.RunDamageTakenMultiplier > 0f
                ? snapshot.RunDamageTakenMultiplier
                : 1f;
            RunEpicBossDamageBonus = snapshot.RunEpicBossDamageBonus;
            RunEpicNormalDamageBonus = snapshot.RunEpicNormalDamageBonus;
            RunExecutionEdgeBonus = snapshot.RunExecutionEdgeBonus;
            RunArcaneEcho = snapshot.RunArcaneEcho;
            RunBloodletting = snapshot.RunBloodletting;
            RunPhoenixHeart = snapshot.RunPhoenixHeart;
            _invulnTimer = snapshot.InvulnTimer;

            // Rebuild Phoenix charge from snapshot flags / mask (never leave a silent desync).
            if (!PhoenixHeartUsed
                && (RunPhoenixHeart || HasEpicTalent(EpicTalentId.PhoenixHeart)))
            {
                RunPhoenixHeart = true;
                _phoenixChargesRemaining = 1;
            }
            else
            {
                _phoenixChargesRemaining = 0;
            }

            // Do not mark dead if an unused Phoenix charge can still save this snapshot.
            if (CurrentHp <= 0)
            {
                if (TryTriggerPhoenixHeart())
                    return;
                IsDead = true;
            }
            else
            {
                IsDead = false;
            }
        }
    }
}
