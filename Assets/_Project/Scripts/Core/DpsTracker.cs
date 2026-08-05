using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Calm combat DPS for the survival HUD: average damage over the last
    /// <see cref="WindowSeconds"/> seconds, lightly smoothed. Freezes while
    /// talent / retreat menus pause combat (timeScale 0 + explicit pause).
    /// </summary>
    public static class DpsTracker
    {
        public const float WindowSeconds = 3f;

        static readonly List<float> _times = new(64);
        static readonly List<int> _damages = new(64);

        static bool _paused;
        static float _displayDps;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            _times.Clear();
            _damages.Clear();
            _paused = false;
            _displayDps = 0f;
        }

        /// <summary>Freeze meter during talent picks / retreat menus.</summary>
        public static void SetPaused(bool paused) => _paused = paused;

        public static bool IsPaused => _paused;

        public static void Record(int damage)
        {
            if (damage <= 0 || _paused) return;
            // Scaled time freezes when menus set timeScale = 0.
            _times.Add(Time.time);
            _damages.Add(damage);
        }

        /// <summary>Calm 3-second combat window average (lightly smoothed for display).</summary>
        public static float DisplayDps => _displayDps;

        public static void Tick()
        {
            if (_paused) return;
            if (Time.timeScale <= 0.001f) return;

            var dt = Time.deltaTime;
            if (dt <= 0f) return;

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
            _displayDps = Mathf.Lerp(_displayDps, raw, 1f - Mathf.Exp(-3.5f * dt));
            if (raw <= 0.01f && _displayDps < 1f)
                _displayDps = 0f;
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
