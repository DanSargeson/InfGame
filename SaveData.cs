using System;

namespace InfGame
{
    public sealed class SaveData
    {
        public double Coins { get; set; }
        public int Generators { get; set; }

        public double TapValue { get; set; }
        public double GeneratorBaseCps { get; set; }

        public double GeneratorCostBase { get; set; }
        public double GeneratorCostGrowth { get; set; }

        public DateTimeOffset LastSavedUtc { get; set; }
    }
}
