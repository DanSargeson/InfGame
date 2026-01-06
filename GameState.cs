using System;

namespace InfGame
{
    public sealed class GameState
    {
        public BigDouble Coins { get; private set; }
        public BigDouble CoinsPerSecond { get; private set; }

        public int Generators { get; private set; }
        public double GeneratorBaseCps { get; private set; } = 0.2;

        public double TapValue { get; private set; } = 1.0;

        public BigDouble GeneratorCostBase { get; private set; } = new BigDouble(15.0);
        public double GeneratorCostGrowth { get; private set; } = 1.15;

        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

        public void Tick(double dtSeconds) {
            if (dtSeconds <= 0) return;
            Coins += CoinsPerSecond * dtSeconds;
        }

        public BigDouble GetNextGeneratorCost() {
            // cost = base * growth^Generators
            return GeneratorCostBase * Math.Pow(GeneratorCostGrowth, Generators);
        }

        public bool TryBuyGenerator() {
            var cost = GetNextGeneratorCost();
            if (Coins + 1e-9 < cost) return false;

            Coins -= cost;
            Generators += 1;
            RecalcCps();
            return true;
        }

        public void Tap() {
            Coins += TapValue;
        }

        public void ApplyOfflineProgress(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 8 * 60 * 60) {
            var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
            if (seconds <= 0) return;

            if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;
            Coins += CoinsPerSecond * seconds;
        }

        public void MarkSaved(DateTimeOffset utcNow) {
            LastSavedUtc = utcNow;
        }

        public void LoadFrom(SaveData data) {
            Coins = data.Coins;
            Generators = data.Generators;
            TapValue = data.TapValue;
            GeneratorBaseCps = data.GeneratorBaseCps;
            GeneratorCostBase = data.GeneratorCostBase;
            GeneratorCostGrowth = data.GeneratorCostGrowth;
            LastSavedUtc = data.LastSavedUtc;

            RecalcCps();
        }

        public SaveData ToSaveData() {
            return new SaveData {
                Coins = Coins,
                Generators = Generators,
                TapValue = TapValue,
                GeneratorBaseCps = GeneratorBaseCps,
                GeneratorCostBase = GeneratorCostBase,
                GeneratorCostGrowth = GeneratorCostGrowth,
                LastSavedUtc = DateTimeOffset.UtcNow
            };
        }

        private void RecalcCps() {
            CoinsPerSecond = Generators * GeneratorBaseCps;
        }
    }
}
