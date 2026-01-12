using System;
using System.Collections.Generic;

namespace InfGame
{
    public sealed class SaveData
    {

        public int Version { get; set; } = 1;

        public BigDouble Souls { get; set; }

        //Total money earned across this specific run (used for prestige math)
        public BigDouble LifetimeSouls { get; set; }

        //The permanent currency you keep after reset
        public BigDouble RebirthPoints { get; set; }

        public Dictionary<string, int> GeneratorCounts { get; set; } = new();
        public List<string> UpgradesBought { get; set; } = new();

        public Dictionary<string, int> ProceduralUpgradeLevels { get; set; } = new();

        public BigDouble TapValue { get; set; }
        public DateTimeOffset LastSavedUtc { get; set; }
        public double Corruption { get; set; }
        public List<string> DisabledAutoBuyers { get; set; } = new();
    }
}
