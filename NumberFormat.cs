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
                return value.ToDouble().ToString($"F{decimals}", CultureInfo.InvariantCulture);
            }

            // 2. Suffixes (1,000 to 1 Quintillion)
            // Exponent 3->K (Index 1), 6->M (Index 2). formula: index = Exponent / 3
            int index = (int)(value.Exponent / 3);

            if (index < Suffix.Length) {
                // Calculate the "visual" number. 
                // e.g. 1.25e4 (12,500) -> Index 1 (K). 
                // We want to see "12.50". 
                // Math: 1.25 * 10^(4 - 1*3) = 1.25 * 10^1 = 12.5
                double divisorPower = index * 3;
                double scaledMantissa = value.Mantissa * Math.Pow(10, value.Exponent - divisorPower);

                return $"{scaledMantissa.ToString($"F{decimals}", CultureInfo.InvariantCulture)}{Suffix[index]}";
            }

            // 3. Massive Numbers (Scientific Notation)
            // e.g. 1.50e50
            return $"{value.Mantissa.ToString($"F{decimals}", CultureInfo.InvariantCulture)}e{value.Exponent}";
        }
    }
}