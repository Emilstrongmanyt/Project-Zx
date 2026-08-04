using System.Collections.Generic;
using UnityEngine;

namespace ProjectZx.Core
{
    /// <summary>
    /// Sliding-window damage-per-second tracker for the survival HUD.
    /// Records every point of damage enemies take from the player team.
    /// </summary>
    public static class DpsTracker
    {
        const float WindowSeconds = 3f;
        const float IdleFadeSeconds = 1.15f;

        static readonly List<float> _times = new(64);
        static readonly List<int> _damages = new(64);
        static float _displayDps;
        static float _peakDps;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            _times.Clear();
            _damages.Clear();
            _displayDps = 0f;
            _peakDps = 0f;
        }

        public static void Record(int damage)
        {
            if (damage <= 0) return;
            // Use unscaled time so talent-pause frames don't freeze the meter forever.
            _times.Add(Time.unscaledTime);
            _damages.Add(damage);
        }

        /// <summary>Smoothed DPS for UI (decays after combat idles).</summary>
        public static float DisplayDps => _displayDps;

        /// <summary>Highest smoothed DPS seen this run (resets with <see cref="Reset"/>).</summary>
        public static float PeakDps => _peakDps;

        public static void Tick()
        {
            var now = Time.unscaledTime;
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
                var span = Mathf.Max(0.4f, now - oldest);
                if (span > WindowSeconds) span = WindowSeconds;
                raw = sum / span;
            }

            var dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);

            if (raw <= 0.01f)
            {
                var lastHitAge = _times.Count > 0 ? now - _times[^1] : IdleFadeSeconds + 1f;
                if (lastHitAge > IdleFadeSeconds)
                    _displayDps = Mathf.MoveTowards(_displayDps, 0f, dt * Mathf.Max(50f, _displayDps * 1.5f));
                else
                    _displayDps = Mathf.Lerp(_displayDps, 0f, 1f - Mathf.Exp(-3f * dt));
            }
            else
            {
                _displayDps = Mathf.Lerp(_displayDps, raw, 1f - Mathf.Exp(-10f * dt));
                if (_displayDps > _peakDps)
                    _peakDps = _displayDps;
            }
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
