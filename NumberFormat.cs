using System;
using System.Globalization;

namespace InfGame
{
    public static class NumberFormat
    {
        // Standard suffixes up to Quintillion. 
        // After this, we switch to Scientific Notation (e.g., 1.50e20)
        private static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };

        public static string Compact(BigDouble value, int decimals = 2) {
            // Handle Negative
            if (value.Mantissa < 0) return "-" + Compact(-value, decimals);

            // 1. Small Numbers (Under 1000)
            if (value.Exponent < 3) {
                // Avoid string interpolation allocation
                string formatStr = decimals == 2 ? "F2" : decimals == 1 ? "F1" : "F0";
                return value.ToDouble().ToString(formatStr, CultureInfo.InvariantCulture);
            }

            // 2. Suffixes (1,000 to 1 Quintillion)
            // Exponent 3->K (Index 1), 6->M (Index 2). formula: index = Exponent / 3
            int index = (int)(value.Exponent / 3);

            if (index < Suffix.Length) {
                double divisorPower = index * 3;
                double scaledMantissa = value.Mantissa * Math.Pow(10, value.Exponent - divisorPower);

                string formatStr = decimals == 2 ? "F2" : decimals == 1 ? "F1" : "F0";
                return $"{scaledMantissa.ToString(formatStr, CultureInfo.InvariantCulture)}{Suffix[index]}";
            }

            // 3. Massive Numbers (Scientific Notation)
            // e.g. 1.50e50
            return $"{value.Mantissa.ToString($"F{decimals}", CultureInfo.InvariantCulture)}e{value.Exponent}";
        }
    }
}