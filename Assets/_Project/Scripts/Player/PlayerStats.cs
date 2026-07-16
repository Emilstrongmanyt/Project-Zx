using System;
using System.Collections.Generic;
using ProjectZx.Core;
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
        LootRange
    }

    public class PlayerStats : MonoBehaviour
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int RunXp { get; private set; }
        public int RunGold { get; private set; }
        public int Level { get; private set; } = 1;
        public int XpToNext { get; private set; }
        public bool IsDead { get; private set; }
        public bool SurvivalMode { get; private set; }
        public int PendingLevelUpChoices { get; private set; }
        public float RunSpeedMultiplier { get; private set; } = 1f;
        public float RunDamageMultiplier { get; private set; } = 1f;
        public float RunAttackSpeedMultiplier { get; private set; } = 1f;
        public float RunAttackRangeMultiplier { get; private set; } = 1f;
        public float RunLootRangeMultiplier { get; private set; } = 1f;

        public event Action<int> LevelUpChoiceRequired;

        bool _goldBanked;

        public void ConfigureForRun(bool survivalMode)
        {
            SurvivalMode = survivalMode;
            MaxHp = GameSave.MaxHp;
            CurrentHp = MaxHp;
            RunXp = 0;
            RunGold = 0;
            Level = 1;
            IsDead = false;
            _goldBanked = false;
            XpToNext = GetXpRequiredForLevel(1);
            PendingLevelUpChoices = 0;
            RunSpeedMultiplier = 1f;
            RunDamageMultiplier = 1f;
            RunAttackSpeedMultiplier = 1f;
            RunAttackRangeMultiplier = 1f;
            RunLootRangeMultiplier = 1f;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
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
                    RunLevelChoice.LootRange
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
            }

            PendingLevelUpChoices--;
            if (PendingLevelUpChoices > 0)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        public void AddRunGold(int amount)
        {
            if (!SurvivalMode || IsDead || amount <= 0 || _goldBanked) return;
            RunGold += amount;
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

        public float Damage => 10f * GameSave.DamageMultiplier * RunDamageMultiplier;

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
                RunLootRangeMultiplier = RunLootRangeMultiplier
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
            IsDead = CurrentHp <= 0;
        }
    }
}