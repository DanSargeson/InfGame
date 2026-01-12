using Android.Media;
using Android.Nfc.Tech;
using System;
using System.Collections.Generic;

namespace InfGame
{
    public sealed class GameState
    {
        // 0.0 = Clean, 1.0 = Fully Corrupted (Stopped)
        public double Corruption { get; private set; } = 0.0;

        // How fast it decays. 0.0001 per tick means it takes ~16 minutes to reach 10% corruption
        // You can balance this later or make upgrades lower this number.
        private double _corruptionRate = 0.00005;

        // The Risk/Reward Calculation
        public double TimeScale => Math.Max(0.1, 1.0 - Corruption); // Never drop below 10% speed
        public double CorruptionBonus => 1.0 + (Corruption * 2.0); // At 50% corruption, get 2x Rebirth Points

        // Later, you can upgrade this to 20, 30, etc. to speed up the game.
        public double TargetTicksPerSecond { get; private set; } = 10.0;

        // Helper: How long is one tick? (e.g., 0.1s)
        public double TickDuration => 1.0 / TargetTicksPerSecond;

        private Dictionary<string, int> _proceduralLevels = new();

        public BigDouble Souls { get; private set; }
        public BigDouble SoulsPerSecond { get; private set; }

        // Add a timer for automation
        private double _autoBuyTimer = 0.0;
        private double _autoBuyInterval = 1.0;


        // Inventory: Key = Generator ID, Value = Count owned
        private Dictionary<string, int> _generatorCounts = new();

        // 1 (x1), 10 (x10), 100 (x100), -1 (Max)
        public int BuyAmount { get; set; } = 1;

        private HashSet<string> _purchasedUpgrades = new();
        public BigDouble TapValue { get; private set; } = new BigDouble(1.0);
        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

        public BigDouble LifetimeSouls { get; private set; }
        public BigDouble RebirthPoints { get; private set; }
        public double RebirthBonusPercent => 0.10;

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
            if (def.CostCurrency == CurrencyType.Souls) {
                if (Souls < cost) return false;
                Souls -= cost;
            }
            else {
                if (RebirthPoints < cost) return false;
                RebirthPoints -= cost;
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
            if (Souls < nextCost) return 0;

            double r = def.CostMultiplier;

            // Formula derived from Geometric Sum Inverse:
            // Max = Log_r( (Coins * (r-1) / NextCost) + 1 )

            var term = (Souls * (r - 1.0)) / nextCost;
            var logValue = BigDouble.Log10(term + 1.0) / Math.Log10(r);

            return (int)Math.Floor(logValue);
        }


        public void Tick() {
            if (Corruption < 0.9) { // Cap at 90%
                Corruption += _corruptionRate;
            }

            var income = (SoulsPerSecond * TimeScale) * TickDuration;
            Souls += income;
            LifetimeSouls += income; // <--- Track it!

            _autoBuyTimer += TickDuration;
            if (_autoBuyTimer >= _autoBuyInterval) {
                _autoBuyTimer -= _autoBuyInterval;
                RunAutoBuyers();
            }
        }

        public bool IsAutoBuyerActive(string id) {
        
                var def = GameData.GetUpgrade(id);
                if (def != null && def.Type == UpgradeType.AutoBuyGenerator) {
                    return true;
                }
            
            return false;
        }

        public void ToggleAutoBuyer(string id) {
        
                var def = GameData.GetUpgrade(id);
                if (def != null && def.Type == UpgradeType.AutoBuyGenerator) {
                    if (HasUpgrade(id)) {
                        _purchasedUpgrades.Remove(id);
                    }
                    else {
                        _purchasedUpgrades.Add(id);
                    }
                }
        }

        private void RunAutoBuyers() {
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def == null) continue;

                // If this upgrade is an Auto-Buyer...
                if (def.Type == UpgradeType.AutoBuyGenerator && !string.IsNullOrEmpty(def.TargetId)) {

                    // Try to buy the target generator!
                    // We use '1' to buy one at a time, or you could make logic to buy Max

                    // NOTE: We duplicate the "Can I afford it?" check here to avoid 
                    // the overhead of the full TryBuyGenerator function if we are broke.
                    var cost = GetCost(def.TargetId);
                    if (Souls >= cost) {
                        // We reuse your existing method, forcing amount to 1
                        // You might need to temporarily store the player's BuyAmount preference
                        int oldBuyAmount = BuyAmount;
                        BuyAmount = 1;
                        TryBuyGenerator(def.TargetId);
                        BuyAmount = oldBuyAmount; // Restore player preference
                    }
                }
            }
        }

        // --- New Data-Driven Logic ---

        public BigDouble CalculateRebirthGain() {
            // Threshold: Don't give points for pocket change
            if (LifetimeSouls < 1000000) return BigDouble.Zero;

            // Formula: (Lifetime / 1M) ^ (1/3)
            var baseVal = LifetimeSouls / 1000000.0;
            var gain = BigDouble.Pow(baseVal, 1.0 / 3.0);

            gain += CorruptionBonus;

            return BigDouble.Floor(gain);
        }

        public void DoRebirth() {
            var gain = CalculateRebirthGain();
            if (gain < 1) return; // Safety check

            // 1. Bank the Points
            RebirthPoints += gain;

            // 2. Reset the Run
            Souls = BigDouble.Zero;
            LifetimeSouls = BigDouble.Zero; // Reset run counter

            var keptUpgrades = new List<string>();
            foreach (var id in _purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def != null && def.CostCurrency == CurrencyType.RebirthPoints) {
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

            Corruption = 0.0; // Reset Corruption
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
            int amountToBuy = BuyAmount;

            // Handle "Max" mode
            if (BuyAmount == -1) {
                amountToBuy = GetMaxBuyable(id);
                if (amountToBuy <= 0) return false; // Can't afford even 1
            }

            // 1. Calculate the REAL total cost
            var totalCost = GetBulkCost(id, amountToBuy);

            // 2. CHECK: Can we afford the TOTAL, not just one?
            // FIX: Changed 'cost' to 'totalCost'
            if (Souls < totalCost) return false;

            // 3. SPEND: Deduct the TOTAL
            // FIX: Changed 'cost' to 'totalCost'
            Souls -= totalCost;

            if (!_generatorCounts.ContainsKey(id)) _generatorCounts[id] = 0;

            // 4. ADD: Add the AMOUNT, not just ++
            // FIX: Changed '++' to '+= amountToBuy'
            _generatorCounts[id] += amountToBuy;

            RecalcCps();
            return true;
        }


        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);

        public bool TryBuyUpgrade(string id) {
            if (HasUpgrade(id)) return false; // Already owned

            var def = GameData.GetUpgrade(id);
            if (def == null) return false;

            //

            if (def.CostCurrency == CurrencyType.Souls) {
                if (Souls < def.Cost) return false;
                Souls -= def.Cost;
            }
            else if (def.CostCurrency == CurrencyType.RebirthPoints) {
                if (RebirthPoints < def.Cost) return false;
                RebirthPoints -= def.Cost;

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
            prestigeMult = BigDouble.One + (RebirthPoints * RebirthBonusPercent);

            TapValue = new BigDouble(1.0) * mult * prestigeMult;
        }


        private void RecalcCps() {
            var total = BigDouble.Zero;
            prestigeMult = BigDouble.One + (RebirthPoints * RebirthBonusPercent);


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
            SoulsPerSecond = total * prestigeMult;
        }

        // --- Standard Stuff ---

        public void Tap() {
            Souls += TapValue;
        }

        //TODO
        // Future Concept
        //void ApplyOfflineProgress(double seconds) {
        //    int ticksToRun = (int)(seconds * TargetTicksPerSecond);
        //    for (int i = 0; i < ticksToRun; i++) {
        //        Tick(); // Actually runs the game logic 50,000 times instantly
        //    }
        //}

        //public void ApplyOfflineProgress(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
        //    var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
        //    if (seconds <= 0) return;
        //    if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;

        //    // 1. If we have lots of time, simulate in "Chunks"
        //    // e.g. Run 1,000 ticks maximum to prevent the app freezing on load
        //    int maxTicks = 1000;

        //    // Calculate how much real time each simulated tick represents
        //    // If they were gone for 8 hours (28,800s) and we only run 1000 ticks,
        //    // each tick must represent 28.8 seconds of progress.
        //    double timePerTick = seconds / maxTicks;

        //    // Ensure we don't simulate smaller than a normal frame (0.1s)
        //    if (timePerTick < TickDuration) {
        //        timePerTick = TickDuration;
        //        maxTicks = (int)(seconds / TickDuration);
        //    }

        //    for (int i = 0; i < maxTicks; i++) {
        //        // Add coins for this chunk of time
        //        Souls += SoulsPerSecond * timePerTick;
        //        Souls += SoulsPerSecond * timePerTick;

        //        // OPTIONAL: If you add "Auto-Buyers" later, run their logic here!
        //        // TryAutoBuyGenerator(); 

        //        // IMPORTANT: Recalculate CPS because Auto-Buyers might have changed it
        //        // RecalcCps(); 
        //    }
        //}

        public BigDouble CalculateOfflineEarnings(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
            var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
            if (seconds <= 0) return BigDouble.Zero;

            if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;

            // Calculate potential earnings
            return SoulsPerSecond * seconds;
        }

        // 2. Apply (Action)
        public void AddCoins(BigDouble amount) {
            Souls += amount;
            LifetimeSouls += amount; // Don't forget lifetime!
        }

        public void MarkSaved(DateTimeOffset utcNow) {
            LastSavedUtc = utcNow;
        }

        public void LoadFrom(SaveData data) {
            Souls = data.Souls;
            LastSavedUtc = data.LastSavedUtc;
            _generatorCounts = data.GeneratorCounts ?? new Dictionary<string, int>();
            LifetimeSouls = data.LifetimeSouls;
            RebirthPoints = data.RebirthPoints;
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
                Souls = Souls,
                LastSavedUtc = DateTimeOffset.UtcNow,
                LifetimeSouls = LifetimeSouls,
                RebirthPoints = RebirthPoints,
                GeneratorCounts = new Dictionary<string, int>(_generatorCounts),
                UpgradesBought = new List<string>(_purchasedUpgrades), // Convert HashSet to List
                ProceduralUpgradeLevels = new Dictionary<string, int>(_proceduralLevels)
            };
        }
    }
}