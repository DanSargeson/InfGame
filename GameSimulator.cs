using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfGame
{
    public class GameSimulator
    {
        private readonly GameState _state;

        // Time Banking
        private double _accumulator = 0.0;

        // EVENTS: The UI subscribes to these!
        public event Action<BigDouble> OnCurrencyGained;
        public event Action<string> OnAutoBuyTriggered;
        public event Action OnRebirth;

        public GameSimulator(GameState state) {
            _state = state;
        }

        public void Update(double dt) {
            // 1. Add to Bank
            _accumulator += dt;

            // 2. Tick Logic (Fixed Step)
            var tickRate = _state.TickDuration;

            // Safety cap for lag (max 10 ticks per frame)
            int loops = 0;
            while (_accumulator >= tickRate && loops < 10) {
                ProcessTick();
                _accumulator -= tickRate;
                loops++;
            }
        }

        // The core math engine
        private void ProcessTick() {
            // A. Corruption Logic
            // Calculate new growth rate, apply to state
            if (_state._Corruption < 1.0) {
                // Logic moved here from GameState
                _state._Corruption += _state._CurrentCorruptionGrowth * _state.TickDuration;
            }

            // B. Income Logic
            var income = (_state.SoulsPerSecond * _state.TimeScale) * _state.TickDuration;
            if (income > 0) {
                _state.Souls += income;
                _state.LifetimeSouls += income;

                // Fire Event (optional, maybe only fire once per second to save performance)
                // OnCurrencyGained?.Invoke(income); 
            }

            // C. Auto-Buyers
            ProcessAutomation();
        }

        private void ProcessAutomation() {
            // Run your existing auto-buyer logic here
            // But now, when a buy happens:

            int oldBuyAmount = _state.BuyAmount;

            _state.BuyAmount = 1; // Force to 1 for auto-buyers

            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def == null) continue;

                // If this upgrade is an Auto-Buyer...
                if (def.Type == UpgradeType.AutoBuyGenerator && !string.IsNullOrEmpty(def.TargetId)) {

                    if (!_state.IsAutoBuyerActive(id)) continue; // Skip if disabled
                    //_state.TryBuyGenerator(def.TargetId);
                    OnAutoBuyTriggered?.Invoke("Generator X Bought!");
                    
                }
            }
            _state.BuyAmount = oldBuyAmount; // Restore player preference
        }

        public bool TryBuyUpgrade(string id) {
            if (_state.HasUpgrade(id)) return false; // Already owned

            var def = GameData.GetUpgrade(id);
            if (def == null) return false;

            //

            if (def.CostCurrency == CurrencyType.Souls) {
                if (_state.Souls < def.Cost) return false;
                _state.Souls -= def.Cost;
            }
            else if (def.CostCurrency == CurrencyType.RebirthPoints) {
                if (_state.RebirthPoints < def.Cost) return false;
                _state.RebirthPoints -= def.Cost;

                // IMPORTANT: Spending points lowers your passive bonus!
                // This creates a strategic choice: "Do I want the bonus or the upgrade?"
                // (You must Recalc to reflect the lower bonus)
            }

            _state._purchasedUpgrades.Add(id);

            if (def.Type == UpgradeType.TapMultiplier) RecalcTap();
            else RecalcCps();

            return true;
        }

        public bool TryBuyGenerator(string id) {
            int amountToBuy = _state.BuyAmount;

            // Handle "Max" mode
            if (_state.BuyAmount == -1) {
                amountToBuy = _state.GetMaxBuyable(id);
                if (amountToBuy <= 0) return false; // Can't afford even 1
            }

            // 1. Calculate the REAL total cost
            var totalCost = _state.GetBulkCost(id, amountToBuy);

            // 2. CHECK: Can we afford the TOTAL, not just one?
            // FIX: Changed 'cost' to 'totalCost'
            if (_state.Souls < totalCost) return false;

            // 3. SPEND: Deduct the TOTAL

            PurifyCorruption(amountToBuy);
            // FIX: Changed 'cost' to 'totalCost'
            _state.Souls -= totalCost;

            if (!_state._generatorCounts.ContainsKey(id)) _state._generatorCounts[id] = 0;

            // 4. ADD: Add the AMOUNT, not just ++
            // FIX: Changed '++' to '+= amountToBuy'
            _state._generatorCounts[id] += amountToBuy;

            RecalcCps();
            return true;
        }

        public bool TryBuyProceduralUpgrade(string seriesId) {
            var def = GameData.GetSeries(seriesId);
            if (def == null) return false;

            var cost = _state.GetProceduralCost(seriesId);

            // Currency Check
            if (def.CostCurrency == CurrencyType.Souls) {
                if (_state.Souls < cost) return false;
                _state.Souls -= cost;
            }
            else {
                if (_state.RebirthPoints < cost) return false;
                _state.RebirthPoints -= cost;
            }

            // Increment Level
            if (!_state._proceduralLevels.ContainsKey(seriesId)) _state._proceduralLevels[seriesId] = 0;
            _state._proceduralLevels[seriesId]++;

            // Recalc
            if (def.Type == UpgradeType.TapMultiplier) RecalcTap();
            else RecalcCps();

            return true;
        }

        private void PurifyCorruption(int amount) {
            double reduction = _state.PurificationAmount + (amount * 0.01);
            _state._Corruption -= reduction;
            if (_state._Corruption < 0.0) _state._Corruption = 0.0;
            _state._CurrentCorruptionGrowth = _state._BaseCorruptionGrowthRate; // Reset growth rate
        }

        private void RecalcCps() {
            var total = BigDouble.Zero;
            _state.prestigeMult = BigDouble.One + (_state.RebirthPoints * _state.RebirthBonusPercent);


            // 1. Calculate Global Multipliers once
            double globalMult = 1.0;
            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.GlobalMultiplier) globalMult *= def.Multiplier;
            }

            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.GlobalMultiplier) {
                    int lvl = _state.GetProceduralLevel(series.Id);
                    if (lvl > 0) globalMult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
            }

            // 2. Loop Generators
            foreach (var kvp in _state._generatorCounts) {
                var def = GameData.GetGenerator(kvp.Key);
                if (def == null) continue;

                // 3. Calculate Specific Multiplier for this Generator
                double genMult = 1.0;
                foreach (var uid in _state._purchasedUpgrades) {
                    var uDef = GameData.GetUpgrade(uid);
                    if (uDef.Type == UpgradeType.GeneratorMultiplier && uDef.TargetId == def.Id) {
                        genMult *= uDef.Multiplier;
                    }
                }

                foreach (var series in GameData.UpgradeSeries) {
                    if (series.Type == UpgradeType.GeneratorMultiplier && series.TargetId == def.Id) {
                        int lvl = _state.GetProceduralLevel(series.Id);
                        if (lvl > 0) genMult *= Math.Pow(series.MultiplierPerLevel, lvl);
                    }
                }

                // Base * Count * GenMult * GlobalMult
                total += def.BaseRevenue * kvp.Value * genMult * globalMult;
            }
            _state.SoulsPerSecond = total * _state.prestigeMult;
        }

        public void Tick() {
            _state._CurrentCorruptionGrowth += _state._CorruptionGrowthAccleration * _state.TickDuration;
            if (_state._Corruption < 0.9900000) { // Cap at 99%
                _state._Corruption += _state._CurrentCorruptionGrowth * _state.TickDuration;
                if (_state._Corruption > 0.9900000) _state._Corruption = 0.9900000;
            }

            var income = (_state.SoulsPerSecond * _state.TimeScale) * _state.TickDuration;
            _state.Souls += income;
            _state.LifetimeSouls += income;

            _state._autoBuyTimer += _state.TickDuration;
            if (_state._autoBuyTimer >= _state._autoBuyInterval) {
                _state._autoBuyTimer -= _state._autoBuyInterval;
                // RunAutoBuyers();
            }
        }

        private void RecalcTap() {
            double mult = 1.0;
            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.TapMultiplier) mult *= def.Multiplier;
            }

            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.TapMultiplier) {
                    int lvl = _state.GetProceduralLevel(series.Id);
                    if (lvl > 0) mult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
            }


            foreach (var series in GameData.UpgradeSeries) {
                if (series.Type == UpgradeType.TapMultiplier) {
                    int lvl = _state.GetProceduralLevel(series.Id);
                    if (lvl > 0) mult *= Math.Pow(series.MultiplierPerLevel, lvl);
                }
            }

            // --- FIX: Apply Prestige Bonus to Tap ---
            // Bonus = 1 + (Points * 0.10)
            _state.prestigeMult = BigDouble.One + (_state.RebirthPoints * _state.RebirthBonusPercent);

            _state.TapValue = new BigDouble(1.0) * mult * _state.prestigeMult;
        }

        public BigDouble CalculateRebirthGain() {
            // Threshold: Don't give points for pocket change
            if (_state.LifetimeSouls < 1000000) return BigDouble.Zero;

            // Formula: (Lifetime / 1M) ^ (1/3)
            var baseVal = _state.LifetimeSouls / 1000000.0;
            var gain = BigDouble.Pow(baseVal, 1.0 / 3.0);

            gain += _state.CorruptionBonus;

            return BigDouble.Floor(gain);
        }


        public void DoRebirth() {
            var gain = CalculateRebirthGain();
            if (gain < 1) return; // Safety check

            // 1. Bank the Points
            _state.RebirthPoints += gain;

            // 2. Reset the Run
            _state.Souls = BigDouble.Zero;
            _state.LifetimeSouls = BigDouble.Zero; // Reset run counter

            var keptUpgrades = new List<string>();
            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def != null && def.CostCurrency == CurrencyType.RebirthPoints) {
                    keptUpgrades.Add(id);
                }
            }
            _state._proceduralLevels.Clear();
            _state._purchasedUpgrades.Clear(); // Usually we wipe upgrades too
            foreach (var id in keptUpgrades) _state._purchasedUpgrades.Add(id);
            _state._generatorCounts.Clear();
            // 3. Recalculate Logic
            RecalcTap();
            RecalcCps();

            _state._Corruption = 0.0; // Reset Corruption
            _state._CurrentCorruptionGrowth = _state._BaseCorruptionGrowthRate; // Reset growth rate
        }

        // Helper for Offline Progress
        public void SimulateTimeChunk(double totalSeconds) {
            // Run the logic loop without the graphics
            // This ensures offline progress respects Corruption, AutoBuyers, etc.
            double simulatedDt = 0.1; // 10 ticks per simulated second
            for (double t = 0; t < totalSeconds; t += simulatedDt) {
                ProcessTick();
            }
        }
    }
}
