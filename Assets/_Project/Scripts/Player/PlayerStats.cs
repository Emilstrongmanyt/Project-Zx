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
        XpBoost
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
        /// <summary>Permanent shop range × run talent range.</summary>
        public float AttackRangeMultiplier => GameSave.AttackRangeMultiplier * RunAttackRangeMultiplier;
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
        public bool RunIronVeil { get; private set; }

        public event Action<int> LevelUpChoiceRequired;
        public event Action<int> EpicChoiceRequired;

        const float IronVeilCooldownSeconds = 20f;
        const float IronVeilAbsorbFraction = 0.3f;
        const float PhoenixInvulnSeconds = 2f;
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
            RunCritChance = 0f;
            RunCritMultiplier = 1.5f;
            RunLifesteal = 0f;
            RunBossDamageBonus = 0f;
            RunExecuteBonus = 0f;
            RunGoldFindMultiplier = 1f;
            RunXpMultiplier = 1f;
            RunRegenPerSecond = 0f;
            RunShieldUnlocked = false;
            RunBerserkBonus = 0f;
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
            RunDamageTakenMultiplier = leader.RunDamageTakenMultiplier;
            RunEpicBossDamageBonus = leader.RunEpicBossDamageBonus;
            RunEpicNormalDamageBonus = leader.RunEpicNormalDamageBonus;
            RunExecutionEdgeBonus = leader.RunExecutionEdgeBonus;
            RunArcaneEcho = leader.RunArcaneEcho;
            RunBloodletting = leader.RunBloodletting;
            Level = leader.Level;
        }

        public bool CanAcceptEpicCrystal =>
            !IsCompanion
            && SurvivalMode
            && !IsDead
            && EpicPicksTaken + PendingEpicChoices < EpicTalentCatalog.MaxPicksPerRun;

        public bool HasEpicTalent(EpicTalentId id) =>
            EpicTalentCatalog.HasTalent(EpicOwnedMask, id);

        /// <summary>Boss crystal pickup — queues an epic talent choice panel.</summary>
        public void OfferEpicTalentChoice()
        {
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
                return;
            }

            if (GameSave.ThickHideLevel > 0)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * GameSave.ThickHideDamageTakenMultiplier));

            if (RunDamageTakenMultiplier > 1.001f)
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

            ApplyDirectHpLoss(amount, isBleed: false);
            ApplySelfBleedFromHit(amount);
        }

        void ApplyDirectHpLoss(int amount, bool isBleed)
        {
            if (IsDead || amount <= 0 || IsCompanion) return;
            if (_invulnTimer > 0f) return;

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
                    return;

                CurrentHp = 0;
                Die();
                return;
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
        }

        /// <summary>
        /// True if the run still has an unused Phoenix Heart (flag and/or owned epic mask).
        /// </summary>
        bool CanUsePhoenixHeart()
        {
            if (IsCompanion || PhoenixHeartUsed) return false;
            if (RunPhoenixHeart) return true;
            // Fallback: talent was applied via mask even if the bool was lost.
            return HasEpicTalent(EpicTalentId.PhoenixHeart);
        }

        /// <summary>
        /// Revive once at 40% HP with brief i-frames. Returns true if the death was prevented.
        /// </summary>
        bool TryTriggerPhoenixHeart()
        {
            if (!CanUsePhoenixHeart()) return false;

            RunPhoenixHeart = true;
            PhoenixHeartUsed = true;
            IsDead = false;
            CurrentHp = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, MaxHp) * 0.4f));
            _invulnTimer = PhoenixInvulnSeconds;
            _selfBleedDamageRemaining = 0;
            _selfBleedTicksRemaining = 0;
            _selfBleedTickTimer = 0f;
            GameHud.Instance?.ShowBanner("Phoenix Heart! Revived!", 2.5f);
            return true;
        }

        public void Heal(int amount)
        {
            if (!SurvivalMode || IsDead || amount <= 0) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public static int GetXpRequiredForLevel(int level) =>
            50 + level * 35 + level * level * 8;

        public void AddXp(int amount)
        {
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
            AttackRangeMultiplier * 1.1f <= StatCaps.RunMaxAttackRangeMultiplier + 0.001f;
        public bool CanOfferHpTalent => MaxHp + 15 <= StatCaps.RunMaxHp;
        public bool CanOfferCritChance => RunCritChance + 0.08f <= 0.55f;
        public bool CanOfferCritDamage => RunCritMultiplier + 0.25f <= 3f;
        public bool CanOfferLifesteal => RunLifesteal + 0.04f <= 0.2f;
        public bool CanOfferBossHunter => RunBossDamageBonus + 0.2f <= 0.8f;
        public bool CanOfferExecute => RunExecuteBonus + 0.3f <= 0.9f;
        public bool CanOfferGoldFind => RunGoldFindMultiplier * 1.15f <= 2f;
        public bool CanOfferRegen => RunRegenPerSecond + 2f <= 8f;
        public bool CanOfferShield => !RunShieldUnlocked;
        public bool CanOfferBerserk => RunBerserkBonus + 0.25f <= 0.5f;
        public bool CanOfferXpBoost => RunXpMultiplier * 1.15f <= 2f;

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
                if (stats.CanOfferSpeedTalent) pool.Add(RunLevelChoice.Speed);
                if (stats.CanOfferHpTalent) pool.Add(RunLevelChoice.Hp);
                if (stats.CanOfferAttackTalent) pool.Add(RunLevelChoice.Attack);
                pool.Add(RunLevelChoice.AttackSpeed);
                if (stats.CanOfferAttackRangeTalent) pool.Add(RunLevelChoice.AttackRange);
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
                RunLevelChoice.Hp => "+15 Max HP",
                RunLevelChoice.Attack => "+12% Attack Damage",
                RunLevelChoice.AttackSpeed => "+12% Attack Speed",
                RunLevelChoice.AttackRange => "+10% Attack Range",
                RunLevelChoice.LootRange => "+15% Loot Range",
                RunLevelChoice.CritChance => "+8% Crit Chance",
                RunLevelChoice.CritDamage => "+25% Crit Damage",
                RunLevelChoice.Lifesteal => "+4% Lifesteal",
                RunLevelChoice.BossHunter => "+20% Damage vs Bosses",
                RunLevelChoice.Execute => "+30% Damage under 25% HP",
                RunLevelChoice.GoldFind => "+15% Gold Find",
                RunLevelChoice.Regen => "+2 HP/sec out of combat",
                RunLevelChoice.Shield => "Block 1 hit every 12s",
                RunLevelChoice.Berserk => "+25% Damage under 40% HP",
                RunLevelChoice.XpBoost => "+15% XP Gain",
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
                    MaxHp = Mathf.Min(StatCaps.RunMaxHp, MaxHp + 15);
                    CurrentHp = Mathf.Min(MaxHp, CurrentHp + 15);
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
                        RunAttackRangeMultiplier * 1.1f);
                    break;
                case RunLevelChoice.LootRange:
                    RunLootRangeMultiplier *= 1.15f;
                    break;
                case RunLevelChoice.CritChance:
                    if (!CanOfferCritChance) break;
                    RunCritChance = Mathf.Min(0.55f, RunCritChance + 0.08f);
                    break;
                case RunLevelChoice.CritDamage:
                    if (!CanOfferCritDamage) break;
                    RunCritMultiplier = Mathf.Min(3f, RunCritMultiplier + 0.25f);
                    break;
                case RunLevelChoice.Lifesteal:
                    if (!CanOfferLifesteal) break;
                    RunLifesteal = Mathf.Min(0.2f, RunLifesteal + 0.04f);
                    break;
                case RunLevelChoice.BossHunter:
                    if (!CanOfferBossHunter) break;
                    RunBossDamageBonus = Mathf.Min(0.8f, RunBossDamageBonus + 0.2f);
                    break;
                case RunLevelChoice.Execute:
                    if (!CanOfferExecute) break;
                    RunExecuteBonus = Mathf.Min(0.9f, RunExecuteBonus + 0.3f);
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
            }

            PendingLevelUpChoices--;
            if (PendingLevelUpChoices > 0)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        public void ApplyEpicTalent(EpicTalentId id)
        {
            if (PendingEpicChoices <= 0 || id == EpicTalentId.None) return;
            if (EpicTalentCatalog.IsUnique(id) && HasEpicTalent(id))
            {
                PendingEpicChoices--;
                if (PendingEpicChoices > 0)
                    EpicChoiceRequired?.Invoke(PendingEpicChoices);
                return;
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
                    RunPhoenixHeart = true;
                    PhoenixHeartUsed = false;
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
        }

        public void AddRunGold(int amount)
        {
            if (!SurvivalMode || IsDead || amount <= 0 || _goldBanked) return;
            // GameSave.GoldFindMultiplier already includes equipped jewelry.
            var mult = GameSave.GoldFindMultiplier * RunGoldFindMultiplier;
            RunGold += Mathf.Max(1, Mathf.RoundToInt(amount * mult));
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

            IsDead = true;

            if (SurvivalMode)
            {
                GameSave.RecordDeath();
                var session = UnityEngine.Object.FindAnyObjectByType<SurvivalSession>();
                if (session != null)
                    GameSave.RecordHighestRound(session.CurrentRound);
            }

            BankRunGoldToSave();
        }

        public float Damage =>
            10f * GameSave.DamageMultiplier * EquipmentCatalog.CombinedDamageMultiplier()
            * RunDamageMultiplier * DamageOutputScale;

        public float EffectiveAttackSpeed =>
            RunAttackSpeedMultiplier * EquipmentCatalog.CombinedAttackSpeedMultiplier()
            * (IsBerserkActive ? 1f + RunBerserkBonus : 1f);

        public bool IsBerserkActive =>
            RunBerserkBonus > 0f && MaxHp > 0 && CurrentHp <= MaxHp * 0.4f;

        public int RollDamage(EnemyActor target, float weaponMultiplier = 1f)
        {
            var dmg = Damage * weaponMultiplier;

            if (IsBerserkActive)
                dmg *= 1f + RunBerserkBonus;

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
                    if (RunExecuteBonus > 0f && target.HpRatio <= 0.25f)
                        dmg *= 1f + RunExecuteBonus;
                }

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

            IsDead = CurrentHp <= 0;
        }
    }
}
