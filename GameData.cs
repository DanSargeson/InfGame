//using System.Collections.Generic;

//namespace InfGame
//{
//    public static class GameData
//    {
//        public static List<GeneratorDef> Generators { get; private set; }
//        public static List<UpgradeDef> Upgrades { get; private set; }

//        public static List<UpgradeSeriesDef> UpgradeSeries { get; private set; }

//        static GameData() {
//            Generators = new List<GeneratorDef> {
//                new GeneratorDef {
//                    Id = "gen_t1",
//                    Name = "First",
//                    BaseCost = new BigDouble(15),
//                    BaseRevenue = new BigDouble(0.5)
//                },
//                new GeneratorDef {
//                    Id = "gen_t2",
//                    Name = "Second",
//                    BaseCost = new BigDouble(100),
//                    BaseRevenue = new BigDouble(4)
//                },
//                new GeneratorDef {
//                    Id = "gen_t3",
//                    Name = "Third",
//                    BaseCost = new BigDouble(1100),
//                    BaseRevenue = new BigDouble(32)
//                },
//                 new GeneratorDef {
//                    Id = "gen_t4",
//                    Name = "4",
//                    BaseCost = new BigDouble(1100),
//                    BaseRevenue = new BigDouble(32)
//                },
//                  new GeneratorDef {
//                    Id = "gen_t5",
//                    Name = "5",
//                    BaseCost = new BigDouble(1),
//                    BaseRevenue = new BigDouble(1000000)
//                },



//                // Add "Progression Curve" here
//            };

//            UpgradeSeries = new List<UpgradeSeriesDef> {
//                // 1. Intern Infinite Upgrades
//                new UpgradeSeriesDef {
//                    Id = "series_t1",
//                    NameFormat = "T1 {0}", // Becomes "T1 1", "Intern Training 2"...
//                    TargetId = "gen_t1",
//                    Type = UpgradeType.GeneratorMultiplier,
//                    BaseCost = new BigDouble(1000), // Start expensive
//                    CostMultiplier = 5.0,           // Ramps fast
//                    MultiplierPerLevel = 2.0        // x2 power each time
//                },
//                // 2. Global Infinite Upgrades
//                new UpgradeSeriesDef {
//                    Id = "series_global",
//                    NameFormat = "Global x {0}",
//                    Type = UpgradeType.GlobalMultiplier,
//                    BaseCost = new BigDouble(50000),
//                    CostMultiplier = 10.0,
//                    MultiplierPerLevel = 1.5 // x1.5 global boost per level
//                },
//                 // 3. Tap Infinite Upgrades
//                new UpgradeSeriesDef {
//                    Id = "series_tap",
//                    NameFormat = "Tap x {0}",
//                    Type = UpgradeType.TapMultiplier,
//                    BaseCost = new BigDouble(500),
//                    CostMultiplier = 2.5,
//                    MultiplierPerLevel = 2.0
//                }
//            };

//            Upgrades = new List<UpgradeDef> {
//                new UpgradeDef {
//                    Id = "upg_tap_x2",
//                    Name = "Tap x2",
//                    Description = "Double your tap power.",
//                    Cost = new BigDouble(100),
//                    Type = UpgradeType.TapMultiplier,
//                    Multiplier = 2.0
//                },
//                new UpgradeDef {
//                    Id = "upg_gen_t1_x2",
//                    Name = "First x2",
//                    Description = "Double the revenue of First generators.",
//                    Cost = new BigDouble(500),
//                    Type = UpgradeType.GeneratorMultiplier,
//                    TargetId = "gen_t1",
//                    Multiplier = 2.0
//                },
//                new UpgradeDef {
//                    Id = "upg_global_x2",
//                    Name = "Global x2",
//                    Description = "Double all generator revenue.",
//                    Cost = new BigDouble(1000),
//                    Type = UpgradeType.GlobalMultiplier,
//                    Multiplier = 2.0
//                },

//                new UpgradeDef {
//                    Id = "asc_tick_1",
//                    Name = "Temporal Flux",
//                    Description = "Game runs 50% faster (Perm)",
//                    Cost = new BigDouble(1), // Costs 1 Prestige Point
//                    CostCurrency = CurrencyType.PrestigePoints, // <--- Costs Points
//                    Type = UpgradeType.GlobalMultiplier,
//                    Multiplier = 1.5
//                },
//                // Add more upgrades here
//            };
//        }
//        public static UpgradeSeriesDef GetSeries(string id) => UpgradeSeries.Find(s => s.Id == id);

//        public static GeneratorDef GetGenerator(string id) => Generators.Find(g => g.Id == id);
//        public static UpgradeDef GetUpgrade(string id) => Upgrades.Find(u => u.Id == id);
//    }
//}

using System.Collections.Generic;
using System.Text.Json;

namespace InfGame
{
    // Helper class to match the JSON root structure
    public class GameDataContainer
    {
        public GameRulesConfig Rules { get; set; }
        public List<GeneratorDef> Generators { get; set; }
        public List<UpgradeDef> Upgrades { get; set; }
        public List<UpgradeSeriesDef> UpgradeSeries { get; set; }
    }

    public static class GameData
    {
        public static List<GeneratorDef> Generators { get; private set; } = new();
        public static List<UpgradeDef> Upgrades { get; private set; } = new();
        public static List<UpgradeSeriesDef> UpgradeSeries { get; private set; } = new();

        public static GameRulesConfig Rules { get; private set; } = new GameRulesConfig();

        public static void Load(string jsonContent) {
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            // Register your existing converter
            options.Converters.Add(new BigDoubleConverter());
            // Add StringEnumConverter if you want to write "Coins" instead of 0 in JSON
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var data = JsonSerializer.Deserialize<GameDataContainer>(jsonContent, options);

            if (data != null) {

                if (data.Rules != null) Rules = data.Rules;

                Generators = data.Generators ?? new List<GeneratorDef>();
                Upgrades = data.Upgrades ?? new List<UpgradeDef>();
                UpgradeSeries = data.UpgradeSeries ?? new List<UpgradeSeriesDef>();
            }
        }

        public static GeneratorDef GetGenerator(string id) => Generators.Find(g => g.Id == id);
        public static UpgradeDef GetUpgrade(string id) => Upgrades.Find(u => u.Id == id);
        public static UpgradeSeriesDef GetSeries(string id) => UpgradeSeries.Find(s => s.Id == id);
    }
}