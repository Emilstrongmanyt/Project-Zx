using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Combat DPS tracker for the survival HUD.
    /// Live = calm average over the last <see cref="WindowSeconds"/> seconds of combat time.
    /// Round = average DPS for the current survival round (resets each round).
    /// Both freeze while talent / retreat menus pause combat (timeScale 0 + explicit pause).
    /// </summary>
    public static class DpsTracker
    {
        public const float WindowSeconds = 3f;

        static readonly List<float> _times = new(64);
        static readonly List<int> _damages = new(64);

        static bool _paused;
        static float _liveDps;
        static float _roundDamage;
        static float _roundCombatSeconds;
        static float _roundAvgDps;
        static float _lastCompletedRoundAvg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            _times.Clear();
            _damages.Clear();
            _paused = false;
            _liveDps = 0f;
            _roundDamage = 0f;
            _roundCombatSeconds = 0f;
            _roundAvgDps = 0f;
            _lastCompletedRoundAvg = 0f;
        }

        /// <summary>Freeze meters during talent picks / retreat menus.</summary>
        public static void SetPaused(bool paused) => _paused = paused;

        public static bool IsPaused => _paused;

        public static void BeginRound()
        {
            // Keep last completed average for the brief between-rounds gap.
            if (_roundCombatSeconds > 0.25f && _roundDamage > 0f)
                _lastCompletedRoundAvg = _roundDamage / _roundCombatSeconds;

            _roundDamage = 0f;
            _roundCombatSeconds = 0f;
            _roundAvgDps = 0f;
            _times.Clear();
            _damages.Clear();
            _liveDps = 0f;
        }

        public static void EndRound()
        {
            if (_roundCombatSeconds > 0.25f && _roundDamage > 0f)
                _lastCompletedRoundAvg = _roundDamage / _roundCombatSeconds;
            _roundAvgDps = _lastCompletedRoundAvg;
        }

        public static void Record(int damage)
        {
            if (damage <= 0 || _paused) return;
            // Scaled time freezes when menus set timeScale = 0.
            var now = Time.time;
            _times.Add(now);
            _damages.Add(damage);
            _roundDamage += damage;
        }

        /// <summary>Calm 3-second combat window average (lightly smoothed for display).</summary>
        public static float LiveDps => _liveDps;

        /// <summary>Average DPS for the active round (or last completed if between rounds).</summary>
        public static float RoundAvgDps =>
            _roundCombatSeconds > 0.25f ? _roundAvgDps : _lastCompletedRoundAvg;

        public static void Tick()
        {
            if (_paused) return;
            if (Time.timeScale <= 0.001f) return;

            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            _roundCombatSeconds += dt;
            if (_roundCombatSeconds > 0.01f)
                _roundAvgDps = _roundDamage / _roundCombatSeconds;

            var now = Time.time;
            Prune(now);

            var sum = 0;
            for (var i = 0; i < _damages.Count; i++)
                sum += _damages[i];

            float raw;
            if (_damages.Count == 0)
            {
                raw = 0f;
            }
            else
            {
                var oldest = _times[0];
                var span = Mathf.Max(0.5f, now - oldest);
                if (span > WindowSeconds) span = WindowSeconds;
                raw = sum / span;
            }

            // Gentle blend so the number does not flicker every hit.
            _liveDps = Mathf.Lerp(_liveDps, raw, 1f - Mathf.Exp(-3.5f * dt));
            if (raw <= 0.01f && _liveDps < 1f)
                _liveDps = 0f;
        }

        static void Prune(float now)
        {
            var cutoff = now - WindowSeconds;
            var remove = 0;
            while (remove < _times.Count && _times[remove] < cutoff)
                remove++;

            if (remove <= 0) return;
            _times.RemoveRange(0, remove);
            _damages.RemoveRange(0, remove);
        }
    }
}
