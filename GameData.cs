using System.Collections.Generic;

namespace InfGame
{
    public static class GameData
    {
        public static List<GeneratorDef> Generators { get; private set; }

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
        }

        public static GeneratorDef GetGenerator(string id) => Generators.Find(g => g.Id == id);
    }
}