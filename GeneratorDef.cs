namespace InfGame
{
    /// <summary>
    /// Static data defining a type of generator (e.g., "Cursor", "Farm", "Mine").
    /// </summary>
    public class GeneratorDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public BigDouble BaseCost { get; set; }
        public BigDouble BaseRevenue { get; set; }
        public float CostMultiplier { get; set; } = 1.15f;

        // Visual helper (for later)
        // public string IconPath { get; set; } 
    }
}
