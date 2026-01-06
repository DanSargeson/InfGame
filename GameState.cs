using Android.Media;
using System;
using System.Collections.Generic;

namespace InfGame
{
    public sealed class GameState
    {
        // Later, you can upgrade this to 20, 30, etc. to speed up the game.
        public double TargetTicksPerSecond { get; private set; } = 10.0;

        // Helper: How long is one tick? (e.g., 0.1s)
        public double TickDuration => 1.0 / TargetTicksPerSecond;

        public BigDouble Coins { get; private set; }
        public BigDouble CoinsPerSecond { get; private set; }

        // Inventory: Key = Generator ID, Value = Count owned
        private Dictionary<string, int> _generatorCounts = new();

        public BigDouble TapValue { get; private set; } = new BigDouble(1.0);
        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

        public void Tick() {
            //add exactly one "Tick's worth" of production
        // Math: (Coins/Sec) * (Seconds/Tick)
        Coins += CoinsPerSecond * TickDuration;

            // FUTURE: This is where you add probability logic
            // if (Random.NextDouble() < 0.01) FindGem();
        }

        // --- New Data-Driven Logic ---

        public int GetCount(string id) {
            return _generatorCounts.ContainsKey(id) ? _generatorCounts[id] : 0;
        }

        public BigDouble GetCost(string id) {
            var def = GameData.GetGenerator(id);
            if (def == null) return BigDouble.Zero;

            int count = GetCount(id);
            // Math: BaseCost * (1.15 ^ Count)
            return def.BaseCost * Math.Pow(def.CostMultiplier, count);
        }

        public bool TryBuyGenerator(string id) {
            var cost = GetCost(id);
            if (Coins < cost) return false;

            Coins -= cost;

            if (!_generatorCounts.ContainsKey(id)) _generatorCounts[id] = 0;
            _generatorCounts[id]++;

            RecalcCps();
            return true;
        }

        private void RecalcCps() {
            var total = BigDouble.Zero;

            // Loop through what we own and sum up the revenue
            foreach (var kvp in _generatorCounts) {
                var def = GameData.GetGenerator(kvp.Key);
                if (def != null) {
                    total += def.BaseRevenue * kvp.Value;
                }
            }
            CoinsPerSecond = total;
        }

        // --- Standard Stuff ---

        public void Tap() {
            Coins += TapValue;
        }

        //TODO
        // Future Concept
        //void ApplyOfflineProgress(double seconds) {
        //    int ticksToRun = (int)(seconds * TargetTicksPerSecond);
        //    for (int i = 0; i < ticksToRun; i++) {
        //        Tick(); // Actually runs the game logic 50,000 times instantly
        //    }
        //}

        public void ApplyOfflineProgress(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
            var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
            if (seconds > 0) {
                if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;
                Coins += CoinsPerSecond * seconds;
            }
        }

        public void MarkSaved(DateTimeOffset utcNow) {
            LastSavedUtc = utcNow;
        }

        public void LoadFrom(SaveData data) {
            Coins = data.Coins;
            TapValue = data.TapValue;
            LastSavedUtc = data.LastSavedUtc;

            // Safety: Ensure dictionary exists even if save was empty
            _generatorCounts = data.GeneratorCounts ?? new Dictionary<string, int>();

            RecalcCps();
        }

        public SaveData ToSaveData() {
            return new SaveData {
                Coins = Coins,
                TapValue = TapValue,
                LastSavedUtc = DateTimeOffset.UtcNow,
                // Create a copy to prevent reference issues
                GeneratorCounts = new Dictionary<string, int>(_generatorCounts)
            };
        }
    }
}