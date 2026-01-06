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

        private HashSet<string> _purchasedUpgrades = new();
        public BigDouble TapValue { get; private set; } = new BigDouble(1.0);
        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

        public BigDouble LifetimeCoins { get; private set; }
        public BigDouble PrestigePoints { get; private set; }

        public double PrestigeBonusPercent => 0.10;


        public void Tick() {
            var income = CoinsPerSecond * TickDuration;
            Coins += income;
            LifetimeCoins += income; // <--- Track it!
        }

        // --- New Data-Driven Logic ---

        public BigDouble CalculatePrestigeGain() {
            // Threshold: Don't give points for pocket change
            if (LifetimeCoins < 1000000) return BigDouble.Zero;

            // Formula: (Lifetime / 1M) ^ (1/3)
            var baseVal = LifetimeCoins / 1000000.0;
            var gain = BigDouble.Pow(baseVal, 1.0 / 3.0);

            return BigDouble.Floor(gain);
        }

        public void DoPrestige() {
            var gain = CalculatePrestigeGain();
            if (gain <= 0.001) return; // Safety check

            // 1. Bank the Points
            PrestigePoints += gain;

            // 2. Reset the Run
            Coins = BigDouble.Zero;
            LifetimeCoins = BigDouble.Zero; // Reset run counter

            _generatorCounts.Clear();
            _purchasedUpgrades.Clear(); // Usually we wipe upgrades too

            // 3. Recalculate Logic
            RecalcTap();
            RecalcCps();
        }

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


        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);

        public bool TryBuyUpgrade(string id) {
            if (HasUpgrade(id)) return false; // Already owned

            var def = GameData.GetUpgrade(id);
            if (def == null) return false;

            if (Coins < def.Cost) return false;

            Coins -= def.Cost;
            _purchasedUpgrades.Add(id);

            // If it was a tap upgrade, recalc tap immediately
            if (def.Type == UpgradeType.TapMultiplier) RecalcTap();
            else RecalcCps();

            return true;
        }

        private void RecalcTap() {
            double mult = 1.0;
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.TapMultiplier) mult *= def.Multiplier;
            }
            TapValue = new BigDouble(1.0) * mult;
        }


        private void RecalcCps() {
            var total = BigDouble.Zero;
            var prestigeMult = 1.0 + (PrestigePoints.ToDouble() * PrestigeBonusPercent);


            // 1. Calculate Global Multipliers once
            double globalMult = 1.0;
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.GlobalMultiplier) globalMult *= def.Multiplier;
            }

            // 2. Loop Generators
            foreach (var kvp in _generatorCounts) {
                var def = GameData.GetGenerator(kvp.Key);
                if (def == null) continue;

                // 3. Calculate Specific Multiplier for this Generator
                double genMult = 1.0;
                foreach (var uid in _purchasedUpgrades) {
                    var uDef = GameData.GetUpgrade(uid);
                    if (uDef.Type == UpgradeType.GeneratorMultiplier && uDef.TargetId == def.Id) {
                        genMult *= uDef.Multiplier;
                    }
                }

                // Base * Count * GenMult * GlobalMult
                total += def.BaseRevenue * kvp.Value * genMult * globalMult;
            }
            CoinsPerSecond = total * prestigeMult;
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
            LastSavedUtc = data.LastSavedUtc;
            _generatorCounts = data.GeneratorCounts ?? new Dictionary<string, int>();
            LifetimeCoins = data.LifetimeCoins;
            PrestigePoints = data.PrestigePoints;
            // Load Upgrades
            _purchasedUpgrades.Clear();
            if (data.UpgradesBought != null) {
                foreach (var id in data.UpgradesBought) _purchasedUpgrades.Add(id);
            }

            RecalcTap();
            RecalcCps();
        }

        public SaveData ToSaveData() {
            return new SaveData {
                Coins = Coins,
                LastSavedUtc = DateTimeOffset.UtcNow,
                LifetimeCoins = LifetimeCoins,
                PrestigePoints = PrestigePoints,
                GeneratorCounts = new Dictionary<string, int>(_generatorCounts),
                UpgradesBought = new List<string>(_purchasedUpgrades) // Convert HashSet to List
            };
        }
    }
}