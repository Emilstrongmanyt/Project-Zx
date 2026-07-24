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

        public event Action<int> LevelUpChoiceRequired;

        bool _goldBanked;
        bool _secondWindUsed;
        bool _shieldReady;
        float _shieldCooldown;
        float _timeSinceDamaged = 99f;
        float _regenAccumulator;

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
            _secondWindUsed = false;
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
            Level = leader.Level;
        }

        void Update()
        {
            if (!SurvivalMode || IsDead) return;

            if (IsCompanion)
            {
                SyncRunBuffsFromLeader();
                return;
            }

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

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || IsCompanion) return;

            if (RunShieldUnlocked && _shieldReady)
            {
                _shieldReady = false;
                _shieldCooldown = ShieldCooldownSeconds;
                return;
            }

            if (GameSave.ThickHideUnlocked)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.85f));

            FloatingDamageNumber.Spawn(transform.position, amount, isHeroHit: true);
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            _timeSinceDamaged = 0f;

            if (GameSave.SecondWindUnlocked && !_secondWindUsed && CurrentHp > 0 && CurrentHp <= MaxHp * 0.2f)
            {
                _secondWindUsed = true;
                Heal(Mathf.Max(1, Mathf.RoundToInt(MaxHp * 0.3f)));
            }

            if (CurrentHp <= 0) Die();
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

            amount = Mathf.Max(1, Mathf.RoundToInt(amount * RunXpMultiplier));
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
                pool.Add(RunLevelChoice.AttackRange);
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
                    RunAttackRangeMultiplier *= 1.1f;
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
                if (target.IsBoss && RunBossDamageBonus > 0f)
                    dmg *= 1f + RunBossDamageBonus;
                else if (!target.IsBoss && RunExecuteBonus > 0f && target.HpRatio <= 0.25f)
                    dmg *= 1f + RunExecuteBonus;
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
                SecondWindUsed = _secondWindUsed
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
            _secondWindUsed = snapshot.SecondWindUsed;
            if (RunShieldUnlocked)
            {
                _shieldReady = true;
                _shieldCooldown = 0f;
            }

            IsDead = CurrentHp <= 0;
        }
    }
}
