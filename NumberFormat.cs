using System;
using System.Globalization;

namespace InfGame
{
    public static class NumberFormat
    {
        private static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };

        public static string Compact(double value, int decimals = 2) {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            if (value < 0) return "-" + Compact(-value, decimals);

            if (value < 1000)
                return value.ToString("0.##", CultureInfo.InvariantCulture);

            int idx = 0;
            while (value >= 1000 && idx < Suffix.Length - 1) {
                value /= 1000;
                idx++;
            }

            string fmt = decimals switch {
                <= 0 => "0",
                1 => "0.0",
                _ => "0.##"
            };

            return value.ToString(fmt, CultureInfo.InvariantCulture) + Suffix[idx];
        }
    }
}
