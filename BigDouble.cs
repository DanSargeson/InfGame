using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace InfGame
{
    /// <summary>
    /// A struct to handle massive numbers for incremental games.
    /// Stores value as Mantissa * 10^Exponent.
    /// Precision is ~15 digits (standard double). Range is effectively infinite.
    /// </summary>
    public struct BigDouble : IComparable<BigDouble>, IEquatable<BigDouble>
    {
        public double Mantissa;
        public long Exponent;

        public static readonly BigDouble Zero = new BigDouble(0, 0);
        public static readonly BigDouble One = new BigDouble(1, 0);

        public BigDouble(double mantissa, long exponent) {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize();
        }

        public BigDouble(double value) {
            Mantissa = value;
            Exponent = 0;
            Normalize();
        }

        /// <summary>
        /// Keeps Mantissa between [1, 10) or [-10, -1] to maintain precision.
        /// </summary>
        private void Normalize() {
            if (Mantissa == 0) {
                Exponent = 0;
                return;
            }

            // Fast check to see if normalization is needed
            if (Mantissa >= 1.0 && Mantissa < 10.0) return;
            if (Mantissa <= -1.0 && Mantissa > -10.0) return;

            // Log10 is expensive, so we use a loop for small shifts (common in games)
            // and Log10 only for massive jumps.

            double abs = Math.Abs(Mantissa);

            // Handle massive overflow/underflow cases directly
            if (abs >= 1e15 || abs < 1e-15) {
                long shift = (long)Math.Floor(Math.Log10(abs));
                Mantissa /= Math.Pow(10, shift);
                Exponent += shift;
                return;
            }

            // Simple loop for small adjustments (faster than Log10)
            while (Math.Abs(Mantissa) >= 10.0) {
                Mantissa /= 10.0;
                Exponent++;
            }
            while (Math.Abs(Mantissa) < 1.0 && Mantissa != 0.0) {
                Mantissa *= 10.0;
                Exponent--;
            }
        }

        #region Arithmetic Operators

        public static BigDouble operator +(BigDouble a, BigDouble b) {
            long diff = a.Exponent - b.Exponent;
            if (diff == 0) return new BigDouble(a.Mantissa + b.Mantissa, a.Exponent);

            // If difference is too big, the smaller number is lost in precision anyway
            if (Math.Abs(diff) > 16) return diff > 0 ? a : b;

            if (diff > 0) return new BigDouble(a.Mantissa + b.Mantissa / Math.Pow(10, diff), a.Exponent);
            else return new BigDouble(a.Mantissa / Math.Pow(10, -diff) + b.Mantissa, b.Exponent);
        }

        public static BigDouble operator -(BigDouble a, BigDouble b) {
            return a + new BigDouble(-b.Mantissa, b.Exponent);
        }

        public static BigDouble operator *(BigDouble a, BigDouble b) {
            return new BigDouble(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static BigDouble operator /(BigDouble a, BigDouble b) {
            return new BigDouble(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
        }

        public static BigDouble operator *(BigDouble a, double b) => a * new BigDouble(b);
        public static BigDouble operator /(BigDouble a, double b) => a / new BigDouble(b);
        public static BigDouble operator +(BigDouble a, double b) => a + new BigDouble(b);
        public static BigDouble operator -(BigDouble a, double b) => a - new BigDouble(b);

        #endregion

        #region Comparison Operators

        public int CompareTo(BigDouble other) {
            // 1. Compare Signs first (Quickest check)
            // 1 > 0 > -1
            int signA = Math.Sign(Mantissa);
            int signB = Math.Sign(other.Mantissa);

            if (signA != signB) return signA.CompareTo(signB);

            // 2. If both are Zero, they are equal
            if (signA == 0) return 0;

            // 3. Same signs (both positive or both negative)
            // Compare magnitude (Exponent)
            if (Exponent > other.Exponent) return signA > 0 ? 1 : -1;
            if (Exponent < other.Exponent) return signA > 0 ? -1 : 1;

            // 4. Same Exponent? Compare Mantissa
            return Mantissa.CompareTo(other.Mantissa);
        }

        public bool Equals(BigDouble other) => Exponent == other.Exponent && Math.Abs(Mantissa - other.Mantissa) < 1e-9;

        public static bool operator >(BigDouble a, BigDouble b) => a.CompareTo(b) > 0;
        public static bool operator <(BigDouble a, BigDouble b) => a.CompareTo(b) < 0;
        public static bool operator >=(BigDouble a, BigDouble b) => a.CompareTo(b) >= 0;
        public static bool operator <=(BigDouble a, BigDouble b) => a.CompareTo(b) <= 0;
        public static bool operator ==(BigDouble a, BigDouble b) => a.Equals(b);
        public static bool operator !=(BigDouble a, BigDouble b) => !a.Equals(b);

        // Paste inside BigDouble.cs, inside the #region Comparison Operators

        public static bool operator >(BigDouble a, double b) => a.CompareTo(new BigDouble(b)) > 0;
        public static bool operator <(BigDouble a, double b) => a.CompareTo(new BigDouble(b)) < 0;
        public static bool operator >=(BigDouble a, double b) => a.CompareTo(new BigDouble(b)) >= 0;
        public static bool operator <=(BigDouble a, double b) => a.CompareTo(new BigDouble(b)) <= 0;
        public static bool operator ==(BigDouble a, double b) => a.Equals(new BigDouble(b));
        public static bool operator !=(BigDouble a, double b) => !a.Equals(new BigDouble(b));

        // Optional: Compare (double, BigDouble) for cases like "if (1000 > coins)"
        public static bool operator >(double a, BigDouble b) => new BigDouble(a).CompareTo(b) > 0;
        public static bool operator <(double a, BigDouble b) => new BigDouble(a).CompareTo(b) < 0;
        public static bool operator >=(double a, BigDouble b) => new BigDouble(a).CompareTo(b) >= 0;
        public static bool operator <=(double a, BigDouble b) => new BigDouble(a).CompareTo(b) <= 0;
        public static bool operator ==(double a, BigDouble b) => new BigDouble(a).Equals(b);
        public static bool operator !=(double a, BigDouble b) => !new BigDouble(a).Equals(b);

        public static BigDouble operator -(BigDouble a) => new BigDouble(-a.Mantissa, a.Exponent);

        public override bool Equals(object obj) => obj is BigDouble bd && Equals(bd);
        public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

        #endregion

        #region Math Functions

        /// <summary>
        /// Calculates Base^Exponent.
        /// Essential for Cost Scaling: BaseCost * Growth^Count
        /// </summary>
        public static BigDouble Pow(BigDouble value, double power) {
            if (value.Mantissa == 0) return Zero;

            // log10(M * 10^E)^P  =  P * (log10(M) + E)
            double log10 = Math.Log10(Math.Abs(value.Mantissa)) + value.Exponent;
            double newLog = log10 * power;

            long newExponent = (long)Math.Floor(newLog);
            double newMantissa = Math.Pow(10, newLog - newExponent);

            if (value.Mantissa < 0 && (long)power % 2 != 0) newMantissa = -newMantissa;

            return new BigDouble(newMantissa, newExponent);
        }

        public static BigDouble Pow(double value, double power) => Pow(new BigDouble(value), power);

        public static double Log10(BigDouble value) {
            return Math.Log10(value.Mantissa) + value.Exponent;
        }

        public static BigDouble Floor(BigDouble value) {
            if (value.Exponent > 15) return value; // Already an integer effectively
            if (value.Exponent < 0) return new BigDouble(Math.Floor(value.ToDouble())); // Less than 1

            // Mixed case requires careful shifting, but usually we just want visual integer
            // For game logic, we rarely floor massive numbers, but for safety:
            return new BigDouble(Math.Floor(value.Mantissa * Math.Pow(10, value.Exponent)), 0);
        }

        public double ToDouble() {
            if (Exponent > 308) return double.PositiveInfinity;
            return Mantissa * Math.Pow(10, Exponent);
        }

        public override string ToString() {
            // Default naive string, use NumberFormat.Compact for UI
            return $"{Mantissa:F2}e{Exponent}";
        }

        #endregion
    }
}
