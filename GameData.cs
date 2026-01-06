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
                   new GeneratorDef {
                    Id = "gen_t6",
                    Name = "6",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                    new GeneratorDef {
                    Id = "gen_t7",
                    Name = "7",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                     new GeneratorDef {
                    Id = "gen_t8",
                    Name = "8",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                     new GeneratorDef {
                    Id = "gen_t9",
                    Name = "9",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                     new GeneratorDef {
                    Id = "gen_t10",
                    Name = "10",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                     new GeneratorDef {
                    Id = "gen_t11",
                    Name = "11",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                      new GeneratorDef {
                    Id = "gen_t12",
                    Name = "12",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                       new GeneratorDef {
                    Id = "gen_t13",
                    Name = "13",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                        new GeneratorDef {
                    Id = "gen_t14",
                    Name = "14",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                         new GeneratorDef {
                    Id = "gen_t15",
                    Name = "15",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                          new GeneratorDef {
                    Id = "gen_t16",
                    Name = "16",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                           new GeneratorDef {
                    Id = "gen_t17",
                    Name = "17",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                            new GeneratorDef {
                    Id = "gen_t18",
                    Name = "18",
                    BaseCost = new BigDouble(1100),
                    BaseRevenue = new BigDouble(32)
                },
                // Add "Progression Curve" here
            };
        }

        public static GeneratorDef GetGenerator(string id) => Generators.Find(g => g.Id == id);
    }
}