using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfGame
{
    public class UpgradeSeriesDef
    {
        public string Id { get; set; }        // e.g. "series_intern_speed"
        public string NameFormat { get; set; } // e.g. "Intern Efficiency {0}"

        // Target
        public UpgradeType Type { get; set; }
        public string TargetId { get; set; } // e.g. "gen_t1"

        // Math: Cost = BaseCost * (CostMultiplier ^ Level)
        public BigDouble BaseCost { get; set; }
        public double CostMultiplier { get; set; } = 10.0; // Costs 10x more each time

        // Math: Effect = PerLevelMultiplier ^ Level
        // e.g. 2.0 means each level doubles the previous (x2, x4, x8)
        public double MultiplierPerLevel { get; set; } = 2.0;

        public CurrencyType CostCurrency { get; set; } = CurrencyType.Coins;
    }
}
