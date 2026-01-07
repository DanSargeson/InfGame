using Android.Media;
using Android.Nfc.Tech;
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

        private Dictionary<string, int> _proceduralLevels = new();

        public BigDouble Coins { get; private set; }
        public BigDouble CoinsPerSecond { get; private set; }


        // Inventory: Key = Generator ID, Value = Count owned
        private Dictionary<string, int> _generatorCounts = new();

        // 1 (x1), 10 (x10), 100 (x100), -1 (Max)
        public int BuyAmount { get; set; } = 1;

        private HashSet<string> _purchasedUpgrades = new();
        public BigDouble TapValue { get; private set; } = new BigDouble(1.0);
        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

        public BigDouble LifetimeCoins { get; private set; }
        public BigDouble PrestigePoints { get; private set; }
        public double PrestigeBonusPercent => 0.10;

        public BigDouble prestigeMult;


        // Helper: Get Level
        public int GetProceduralLevel(string seriesId) => _proceduralLevels.ContainsKey(seriesId) ? _proceduralLevels[seriesId] : 0;

        // Helper: Calculate Cost for the NEXT level
        public BigDouble GetProceduralCost(string seriesId) {
            var def = GameData.GetSeries(seriesId);
            if (def == null) return BigDouble.Zero;

            int currentLevel = GetProceduralLevel(seriesId);
            // Formula: Base * (Growth ^ Level)
            return def.BaseCost * BigDouble.Pow(def.CostMultiplier, currentLevel);
        }

        public bool TryBuyProceduralUpgrade(string seriesId) {
            var def = GameData.GetSeries(seriesId);
            if (def == null) return false;

            var cost = GetProceduralCost(seriesId);

            // Currency Check
            if (def.CostCurrency == CurrencyType.Coins) {
                if (Coins < cost) return false;
                Coins -= cost;
            }
            else {
                if (PrestigePoints < cost) return false;
                PrestigePoints -= cost;
            }

            // Increment Level
            if (!_proceduralLevels.ContainsKey(seriesId)) _proceduralLevels[seriesId] = 0;
            _proceduralLevels[seriesId]++;

            // Recalc
            if (def.Type == UpgradeType.TapMultiplier) RecalcTap();
            else RecalcCps();

            return true;
        }


        // Helper: Calculate Cost for 'count' items
        public BigDouble GetBulkCost(string id, int count) {
            var def = GameData.GetGenerator(id);
            if (def == null) return BigDouble.Zero;

            // Current price of the NEXT single unit
            var nextCost = GetCost(id);
            double r = def.CostMultiplier; // e.g., 1.15

            // If buying 1, standard logic
            if (count == 1) return nextCost;

            // Geometric Sum: Cost * (r^N - 1) / (r - 1)
            var numerator = BigDouble.Pow(r, count) - 1.0;
            var denominator = r - 1.0;

            return nextCost * (numerator / denominator);
        }

        // Helper: Calculate Max we can afford
        public int GetMaxBuyable(string id) {
            var def = GameData.GetGenerator(id);
            if (def == null) return 0;

            var nextCost = GetCost(id);
            if (Coins < nextCost) return 0;

            double r = def.CostMultiplier;

            // Formula derived from Geometric Sum Inverse:
            // Max = Log_r( (Coins * (r-1) / NextCost) + 1 )

            var term = (Coins * (r - 1.0)) / nextCost;
            var logValue = BigDouble.Log10(term + 1.0) / Math.Log10(r);

            return (int)Math.Floor(logValue);
        }


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
            if (gain < 1) return; // Safety check

            // 1. Bank the Points
            PrestigePoints += gain;

            // 2. Reset the Run
            Coins = BigDouble.Zero;
            LifetimeCoins = BigDouble.Zero; // Reset run counter

            var keptUpgrades = new List<string>();
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def != null && def.CostCurrency == CurrencyType.PrestigePoints) {
                    keptUpgrades.Add(id);
                }
            }
            _proceduralLevels.Clear();
            _purchasedUpgrades.Clear(); // Usually we wipe upgrades too
            foreach (var id in keptUpgrades) _purchasedUpgrades.Add(id);
            _generatorCounts.Clear();
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
            int amountToBuy = BuyAmount;

            // Handle "Max" mode
            if (BuyAmount == -1) {
                amountToBuy = GetMaxBuyable(id);
                if (amountToBuy <= 0) return false; // Can't afford even 1
            }

            var totalCost = GetBulkCost(id, amountToBuy);

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

            //

            if (def.CostCurrency == CurrencyType.Coins) {
                if (Coins < def.Cost) return false;
                Coins -= def.Cost;
            }
            else if (def.CostCurrency == CurrencyType.PrestigePoints) {
                if (PrestigePoints < def.Cost) return false;
                PrestigePoints -= def.Cost;

                // IMPORTANT: Spending points lowers your passive bonus!
                // This creates a strategic choice: "Do I want the bonus or the upgrade?"
                // (You must Recalc to reflect the lower bonus)
            }

            _purchasedUpgrades.Add(id);

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

            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.TapMultiplier) {
                    int lvl = GetProceduralLevel(series.Id);
                    if (lvl > 0) mult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
            }


            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.TapMultiplier) {
                    int lvl = GetProceduralLevel(series.Id);
                    if (lvl > 0) mult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
            }

            // --- FIX: Apply Prestige Bonus to Tap ---
            // Bonus = 1 + (Points * 0.10)
            prestigeMult = BigDouble.One + (PrestigePoints * PrestigeBonusPercent);

            TapValue = new BigDouble(1.0) * mult * prestigeMult;
        }


        private void RecalcCps() {
            var total = BigDouble.Zero;
            prestigeMult = BigDouble.One + (PrestigePoints * PrestigeBonusPercent);


            // 1. Calculate Global Multipliers once
            double globalMult = 1.0;
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.GlobalMultiplier) globalMult *= def.Multiplier;
            }

            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.GlobalMultiplier) {
                    int lvl = GetProceduralLevel(series.Id);
                    if (lvl > 0) globalMult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
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

                foreach (var series in GameData.UpgradeSeries) {
                    if (series.Type == UpgradeType.GeneratorMultiplier && series.TargetId == def.Id) {
                        int lvl = GetProceduralLevel(series.Id);
                        if (lvl > 0) genMult *= Math.Pow(series.MultiplierPerLevel, lvl);
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

            _proceduralLevels = data.ProceduralUpgradeLevels ?? new Dictionary<string, int>();

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
                UpgradesBought = new List<string>(_purchasedUpgrades), // Convert HashSet to List
                ProceduralUpgradeLevels = new Dictionary<string, int>(_proceduralLevels)
            };
        }
    }
}