using System;
using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.Player
{
    public enum RunLevelChoice
    {
        Speed,
        Hp,
        Attack,
        AttackSpeed
    }

    public class PlayerStats : MonoBehaviour
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int RunXp { get; private set; }
        public int RunGold { get; private set; }
        public int Level { get; private set; } = 1;
        public int XpToNext { get; private set; } = 30;
        public bool IsDead { get; private set; }
        public bool SurvivalMode { get; private set; }
        public int PendingLevelUpChoices { get; private set; }
        public float RunSpeedMultiplier { get; private set; } = 1f;
        public float RunDamageMultiplier { get; private set; } = 1f;
        public float RunAttackSpeedMultiplier { get; private set; } = 1f;

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
            XpToNext = 30;
            PendingLevelUpChoices = 0;
            RunSpeedMultiplier = 1f;
            RunDamageMultiplier = 1f;
            RunAttackSpeedMultiplier = 1f;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp <= 0) Die();
        }

        /// <summary>Run-only XP. Never written to persistent save.</summary>
        public void AddXp(int amount)
        {
            if (!SurvivalMode || IsDead || amount <= 0) return;
            RunXp += amount;

            var leveled = false;
            while (RunXp >= XpToNext)
            {
                RunXp -= XpToNext;
                Level++;
                XpToNext = 30 + Level * 12;
                PendingLevelUpChoices++;
                leveled = true;
            }

            if (leveled)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        public void ApplyRunLevelChoice(RunLevelChoice choice)
        {
            if (PendingLevelUpChoices <= 0) return;

            switch (choice)
            {
                case RunLevelChoice.Speed:
                    RunSpeedMultiplier *= 1.1f;
                    break;
                case RunLevelChoice.Hp:
                    MaxHp += 15;
                    CurrentHp += 15;
                    break;
                case RunLevelChoice.Attack:
                    RunDamageMultiplier *= 1.12f;
                    break;
                case RunLevelChoice.AttackSpeed:
                    RunAttackSpeedMultiplier *= 1.12f;
                    break;
            }

            PendingLevelUpChoices--;
            if (PendingLevelUpChoices > 0)
                LevelUpChoiceRequired?.Invoke(PendingLevelUpChoices);
        }

        /// <summary>Gold earned this run. Banked to camp savings on death or run end.</summary>
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
                RunAttackSpeedMultiplier = RunAttackSpeedMultiplier
            };
        }

        public void RestoreSnapshot(SurvivalRunSnapshot snapshot)
        {
            if (!snapshot.HasData) return;

            MaxHp = snapshot.MaxHp;
            CurrentHp = snapshot.CurrentHp;
            RunXp = snapshot.RunXp;
            RunGold = snapshot.RunGold;
            Level = snapshot.Level;
            XpToNext = snapshot.XpToNext;
            PendingLevelUpChoices = snapshot.PendingLevelUpChoices;
            RunSpeedMultiplier = snapshot.RunSpeedMultiplier;
            RunDamageMultiplier = snapshot.RunDamageMultiplier;
            RunAttackSpeedMultiplier = snapshot.RunAttackSpeedMultiplier > 0f
                ? snapshot.RunAttackSpeedMultiplier
                : 1f;
            IsDead = CurrentHp <= 0;
        }
    }
}