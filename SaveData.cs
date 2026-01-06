using System;
using System.Collections.Generic;

namespace InfGame
{
    public sealed class SaveData
    {
        // Changed from double to BigDouble
        public BigDouble Coins { get; set; }

        public Dictionary<string, int> GeneratorCounts { get; set; } = new();

        public BigDouble TapValue { get; set; }
        public BigDouble GeneratorBaseCps { get; set; }

        public BigDouble GeneratorCostBase { get; set; }
        public double GeneratorCostGrowth { get; set; } // Growth factor (1.15) can stay double

        public DateTimeOffset LastSavedUtc { get; set; }
    }
}
