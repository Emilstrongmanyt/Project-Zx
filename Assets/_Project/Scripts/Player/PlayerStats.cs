using System;
using System.Collections.Generic;
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
        /// <summary>Bowman only: +1 pierce hit per stack on arrows.</summary>
        Pierce
    }

    public class PlayerStats : MonoBehaviour
    {
        const float ShieldCooldownSeconds = 12f;
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
        /// <summary>Run talent additive damage reduction (0.08 per pick, max 0.40 from talents).</summary>
        public float RunDamageTakenReduction { get; private set; }
        /// <summary>Run talent block chance (0.05 per pick, max 0.50 from talents).</summary>
        public float RunBlockChance { get; private set; }
        /// <summary>Bowman Multishot: dual-arrow chance (0.33 per pick, max 0.99).</summary>
        public float RunMultishotChance { get; private set; }
        /// <summary>Bowman Pierce talent: extra enemies one arrow can pass through.</summary>
        public int RunPierceBonus { get; private set; }

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

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || IsCompanion) return;
            if (_invulnTimer > 0f) return;

            if (RunShieldUnlocked && _shieldReady)
            {
                _shieldReady = false;
                _shieldCooldown = ShieldCooldownSeconds;
                FloatingDamageNumber.SpawnBlock(transform.position);
                return;
            }

            // Chance block: run talents + equipped capes (hard cap 50% total).
            var blockChance = Mathf.Min(0.50f, RunBlockChance + EquipmentCatalog.CombinedBlockChance());
            if (blockChance > 0f && UnityEngine.Random.value < blockChance)
            {
                _timeSinceDamaged = 0f;
                FloatingDamageNumber.SpawnBlock(transform.position);
                return;
            }

            if (GameSave.ThickHideLevel > 0)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * GameSave.ThickHideDamageTakenMultiplier));

            // Additive DR from level-up Defense talent + equipment (cap 50% total reduction).
            var reduction = Mathf.Min(0.50f, RunDamageTakenReduction + EquipmentCatalog.CombinedDamageReduction());
            if (reduction > 0f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - reduction)));

            if (RunDamageTakenMultiplier > 1.001f || RunDamageTakenMultiplier < 0.999f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * RunDamageTakenMultiplier));

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

        public bool CanOfferSpeedTalent => RunSpeedMultiplier * 1.1f <= StatCaps.RunMaxSpeedMultiplier + 0.001f;
        public bool CanOfferAttackTalent => RunDamageMultiplier * 1.12f <= StatCaps.RunMaxDamageMultiplier + 0.001f;
        public bool CanOfferAttackRangeTalent =>
            AttackRangeMultiplier * 1.06f <= StatCaps.RunMaxAttackRangeMultiplier + 0.001f;
        public bool CanOfferHpTalent => MaxHp + 30 <= StatCaps.RunMaxHp;
        // Higher caps so Bowman (starts 40% / 2.1×) still gains from crit talent picks.
        public bool CanOfferCritChance => RunCritChance + 0.08f <= 0.90f;
        public bool CanOfferCritDamage => RunCritMultiplier + 0.25f <= 3.6f;
        public bool CanOfferLifesteal => RunLifesteal + 0.03f <= 0.2f;
        public bool CanOfferBossHunter => RunBossDamageBonus + 0.2f <= 0.8f;
        public bool CanOfferExecute => RunExecuteBonus + 0.5f <= 1.5f;
        public bool CanOfferGoldFind => RunGoldFindMultiplier * 1.15f <= 2f;
        public bool CanOfferRegen => RunRegenPerSecond + 2f <= 8f;
        public bool CanOfferShield => !RunShieldUnlocked;
        public bool CanOfferBerserk => RunBerserkBonus + 0.25f <= 0.5f;
        public bool CanOfferXpBoost => RunXpMultiplier * 1.15f <= 2f;
        /// <summary>Defense talent: −8% damage taken per pick, max −40% from this talent.</summary>
        public bool CanOfferDefenseTalent => RunDamageTakenReduction + 0.08f <= 0.40f + 0.001f;
        /// <summary>Block talent: +5% block per pick, max 50% from this talent.</summary>
        public bool CanOfferBlockTalent => RunBlockChance + 0.05f <= 0.50f + 0.001f;
        /// <summary>Bowman Multishot: +33% dual-shot chance per pick, max 99% (3 stacks).</summary>
        public bool CanOfferMultishotTalent =>
            GameSessionContext.SelectedClass == PlayerClass.Bowman
            && RunMultishotChance + 0.33f <= 0.99f + 0.001f;
        /// <summary>Bowman Pierce: +1 pierce hit per pick, max +3.</summary>
        public bool CanOfferPierceTalent =>
            GameSessionContext.SelectedClass == PlayerClass.Bowman
            && RunPierceBonus + 1 <= 3;

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
            }

            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, Mathf.Min(count, pool.Count));
        }

        public static string GetChoiceLabel(RunLevelChoice choice)
        {
            return choice switch
            {
                RunLevelChoice.Speed => "+10% Move Speed",
                RunLevelChoice.Hp => "+30 Max HP",
                RunLevelChoice.Attack => "+12% Attack Damage",
                RunLevelChoice.AttackSpeed => "+12% Attack Speed",
                RunLevelChoice.AttackRange => "+6% Attack Range",
                RunLevelChoice.LootRange => "+15% Loot Range",
                RunLevelChoice.CritChance => "+8% Crit Chance",
                RunLevelChoice.CritDamage => "+25% Crit Damage",
                RunLevelChoice.Lifesteal => "+3% Lifesteal",
                RunLevelChoice.BossHunter => "+20% Damage vs Bosses",
                RunLevelChoice.Execute => "+50% Damage under 25% HP (yours)",
                RunLevelChoice.GoldFind => "+15% Gold Find",
                RunLevelChoice.Regen => "+2 HP/sec out of combat",
                RunLevelChoice.Shield => "Block 1 hit every 12s",
                RunLevelChoice.Berserk => "+25% Damage over 90% HP",
                RunLevelChoice.XpBoost => "+15% XP Gain",
                RunLevelChoice.Defense => "−8% Damage Taken",
                RunLevelChoice.Block => "+5% Block Chance",
                RunLevelChoice.Multishot => "+33% Multishot Chance",
                RunLevelChoice.Pierce => "+1 Pierce",
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
                    RunSpeedMultiplier = Mathf.Min(StatCaps.RunMaxSpeedMultiplier, RunSpeedMultiplier * 1.1f);
                    break;
                case RunLevelChoice.Hp:
                    if (!CanOfferHpTalent) break;
                    MaxHp = Mathf.Min(StatCaps.RunMaxHp, MaxHp + 30);
                    CurrentHp = Mathf.Min(MaxHp, CurrentHp + 30);
                    break;
                case RunLevelChoice.Attack:
                    if (!CanOfferAttackTalent) break;
                    RunDamageMultiplier = Mathf.Min(StatCaps.RunMaxDamageMultiplier, RunDamageMultiplier * 1.12f);
                    break;
                case RunLevelChoice.AttackSpeed:
                    RunAttackSpeedMultiplier *= 1.12f;
                    break;
                case RunLevelChoice.AttackRange:
                    if (!CanOfferAttackRangeTalent) break;
                    RunAttackRangeMultiplier = Mathf.Min(
                        StatCaps.RunMaxAttackRangeMultiplier / Mathf.Max(0.01f, GameSave.AttackRangeMultiplier),
                        RunAttackRangeMultiplier * 1.06f);
                    break;
                case RunLevelChoice.LootRange:
                    RunLootRangeMultiplier *= 1.15f;
                    break;
                case RunLevelChoice.CritChance:
                    if (!CanOfferCritChance) break;
                    RunCritChance = Mathf.Min(0.90f, RunCritChance + 0.08f);
                    break;
                case RunLevelChoice.CritDamage:
                    if (!CanOfferCritDamage) break;
                    RunCritMultiplier = Mathf.Min(3.6f, RunCritMultiplier + 0.25f);
                    break;
                case RunLevelChoice.Lifesteal:
                    if (!CanOfferLifesteal) break;
                    RunLifesteal = Mathf.Min(0.2f, RunLifesteal + 0.03f);
                    break;
                case RunLevelChoice.BossHunter:
                    if (!CanOfferBossHunter) break;
                    RunBossDamageBonus = Mathf.Min(0.8f, RunBossDamageBonus + 0.2f);
                    break;
                case RunLevelChoice.Execute:
                    if (!CanOfferExecute) break;
                    RunExecuteBonus = Mathf.Min(1.5f, RunExecuteBonus + 0.5f);
                    break;
                case RunLevelChoice.GoldFind:
                    if (!CanOfferGoldFind) break;
                    RunGoldFindMultiplier = Mathf.Min(2f, RunGoldFindMultiplier * 1.15f);
                    break;
                case RunLevelChoice.Regen:
                    if (!CanOfferRegen) break;
                    RunRegenPerSecond = Mathf.Min(8f, RunRegenPerSecond + 2f);
                    break;
                case RunLevelChoice.Shield:
                    if (!CanOfferShield) break;
                    RunShieldUnlocked = true;
                    _shieldReady = true;
                    _shieldCooldown = 0f;
                    break;
                case RunLevelChoice.Berserk:
                    if (!CanOfferBerserk) break;
                    RunBerserkBonus = Mathf.Min(0.5f, RunBerserkBonus + 0.25f);
                    break;
                case RunLevelChoice.XpBoost:
                    if (!CanOfferXpBoost) break;
                    RunXpMultiplier = Mathf.Min(2f, RunXpMultiplier * 1.15f);
                    break;
                case RunLevelChoice.Defense:
                    if (!CanOfferDefenseTalent) break;
                    RunDamageTakenReduction = Mathf.Min(0.40f, RunDamageTakenReduction + 0.08f);
                    break;
                case RunLevelChoice.Block:
                    if (!CanOfferBlockTalent) break;
                    RunBlockChance = Mathf.Min(0.50f, RunBlockChance + 0.05f);
                    break;
                case RunLevelChoice.Multishot:
                    if (!CanOfferMultishotTalent) break;
                    RunMultishotChance = Mathf.Min(0.99f, RunMultishotChance + 0.33f);
                    break;
                case RunLevelChoice.Pierce:
                    if (!CanOfferPierceTalent) break;
                    RunPierceBonus = Mathf.Min(3, RunPierceBonus + 1);
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
                sb.AppendLine("Shield: armed every 12s");
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
        {
            var dmg = Damage * weaponMultiplier;

            if (IsBerserkActive)
                dmg *= 1f + RunBerserkBonus;

            // Execute uses the hero's HP ratio, not the target's.
            if (IsExecuteActive)
                dmg *= 1f + RunExecuteBonus;

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
                dmg *= RunCritMultiplier;

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
            RunPierceBonus = Mathf.Clamp(snapshot.RunPierceBonus, 0, 3);
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
