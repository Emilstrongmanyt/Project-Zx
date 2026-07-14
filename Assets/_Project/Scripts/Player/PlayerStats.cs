using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.Player
{
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
            while (RunXp >= XpToNext)
            {
                RunXp -= XpToNext;
                Level++;
                XpToNext = 30 + Level * 12;
                MaxHp += 5;
                CurrentHp = Mathf.Min(CurrentHp + 8, MaxHp);
            }
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

        public float Damage => 10f * GameSave.DamageMultiplier * (1f + (Level - 1) * 0.05f);
    }
}