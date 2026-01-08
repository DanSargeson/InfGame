using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfGame
{

    public enum CurrencyType { Souls, RebirthPoints }
    public enum UpgradeType
    {
        GeneratorMultiplier, // Buffs a specific ID (e.g. "T1 x2")
        GlobalMultiplier,    // Buffs everything (e.g. "All Profit x2")
        TapMultiplier        // Buffs clicking (e.g. "Tap x2")
    }

    public class UpgradeDef
    {

        public CurrencyType CostCurrency { get; set; } = CurrencyType.Souls;
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BigDouble Cost { get; set; }

        public UpgradeType Type { get; set; }
        public string TargetId { get; set; } // Null if Global/Tap
        public double Multiplier { get; set; } // e.g. 2.0 for x2
    }
}
