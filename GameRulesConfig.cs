namespace InfGame
{
    public class GameRulesConfig
    {
        public double BaseCorruptionGrowth { get; set; } = 0.001;
        public double PurificationAmount { get; set; } = 0.05;
        public double RebirthLifetimeThreshold { get; set; } = 1000000.0;
        public double RebirthExponent { get; set; } = 0.33333333; // 1/3
        public double MaxOfflineSeconds { get; set; } = 86400.0; // 24 hours
        public double RebirthBonusPercent { get; set; } = 0.10;
    }
}