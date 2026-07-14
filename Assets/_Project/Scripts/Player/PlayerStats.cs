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
        public bool IsDead { get; private set; }
        public bool SurvivalMode { get; private set; }

        int _xpToNext = 30;

        public void ConfigureForRun(bool survivalMode)
        {
            SurvivalMode = survivalMode;
            MaxHp = GameSave.MaxHp;
            CurrentHp = MaxHp;
            RunXp = 0;
            RunGold = 0;
            Level = 1;
            IsDead = false;
            _xpToNext = 30;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp <= 0) Die();
        }

        public void AddXp(int amount)
        {
            if (!SurvivalMode || amount <= 0) return;
            RunXp += amount;
            while (RunXp >= _xpToNext)
            {
                RunXp -= _xpToNext;
                Level++;
                _xpToNext = 30 + Level * 12;
                MaxHp += 5;
                CurrentHp = Mathf.Min(CurrentHp + 8, MaxHp);
            }
        }

        public void AddRunGold(int amount)
        {
            if (!SurvivalMode || amount <= 0) return;
            RunGold += amount;
        }

        void Die()
        {
            IsDead = true;
            GameSave.AddGold(RunGold);
        }

        public float Damage => 10f * GameSave.DamageMultiplier * (1f + (Level - 1) * 0.05f);
    }
}