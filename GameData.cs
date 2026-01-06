using System.Collections.Generic;

namespace InfGame
{
    public static class GameData
    {
        public static List<GeneratorDef> Generators { get; private set; }
        public static List<UpgradeDef> Upgrades { get; private set; }

        static GameData() {
            Generators = new List<GeneratorDef> {
                new GeneratorDef {
                    Id = "gen_t1",
                    Name = "First",
                    BaseCost = new BigDouble(15),
                    BaseRevenue = new BigDouble(0.5)
                },
                new GeneratorDef {
                    Id = "gen_t2",
                    Name = "Second",
                    BaseCost = new BigDouble(100),
                    BaseRevenue = new BigDouble(4)
                },
                new GeneratorDef {
                    Id = "gen_t3",
                    Name = "Third",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                 new GeneratorDef {
                    Id = "gen_t4",
                    Name = "4",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                  new GeneratorDef {
                    Id = "gen_t5",
                    Name = "5",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                   
                // Add "Progression Curve" here
            };

            Upgrades = new List<UpgradeDef> {
                new UpgradeDef {
                    Id = "upg_tap_x2",
                    Name = "Tap x2",
                    Description = "Double your tap power.",
                    Cost = new BigDouble(100),
                    Type = UpgradeType.TapMultiplier,
                    Multiplier = 2.0
                },
                new UpgradeDef {
                    Id = "upg_gen_t1_x2",
                    Name = "First x2",
                    Description = "Double the revenue of First generators.",
                    Cost = new BigDouble(500),
                    Type = UpgradeType.GeneratorMultiplier,
                    TargetId = "gen_t1",
                    Multiplier = 2.0
                },
                new UpgradeDef {
                    Id = "upg_global_x2",
                    Name = "Global x2",
                    Description = "Double all generator revenue.",
                    Cost = new BigDouble(1000),
                    Type = UpgradeType.GlobalMultiplier,
                    Multiplier = 2.0
                },
                // Add more upgrades here
            };
        }

        public static GeneratorDef GetGenerator(string id) => Generators.Find(g => g.Id == id);
        public static UpgradeDef GetUpgrade(string id) => Upgrades.Find(u => u.Id == id);
    }
}