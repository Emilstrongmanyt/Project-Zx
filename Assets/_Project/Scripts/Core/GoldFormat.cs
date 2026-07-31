using System.Globalization;

namespace ProjectZx.Core
{
    /// <summary>Compact gold display: 115 → "115", 3000 → "3k", 13500 → "13.5k".</summary>
    public static class GoldFormat
    {
        public static string Abbreviate(int amount)
        {
            var value = amount < 0 ? 0 : amount;
            if (value < 1000)
                return value.ToString(CultureInfo.InvariantCulture);

            if (value < 1_000_000)
            {
                var thousands = value / 1000f;
                return FormatScaled(thousands, "k");
            }

            var millions = value / 1_000_000f;
            return FormatScaled(millions, "m");
        }

        static string FormatScaled(float scaled, string suffix)
        {
            // One decimal when needed (13.5k), otherwise whole (3k).
            var rounded = System.Math.Round(scaled, 1);
            if (System.Math.Abs(rounded - System.Math.Round(rounded)) < 0.05)
                return ((int)System.Math.Round(rounded)).ToString(CultureInfo.InvariantCulture) + suffix;
            return rounded.ToString("0.#", CultureInfo.InvariantCulture) + suffix;
        }
    }
}
