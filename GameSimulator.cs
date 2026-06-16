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

        public EconomyEngine Economy { get; private set; }

        // EVENTS: The UI subscribes to these!
        public event Action<BigDouble> OnCurrencyGained;
        public event Action<string> OnAutoBuyTriggered;
        public event Action OnRebirth;

        private Dictionary<string, double> _cachedGenMults = new Dictionary<string, double>(64);

        // Add a timer for automation
        public double _autoBuyTimer = 0.0;
        public double _autoBuyInterval = 1.0;

        public GameSimulator(GameState state) {
            _state = state;
            Economy = new EconomyEngine(_state);    
        }

        public void Update(double dt) {
            // 1. Add to Bank
            _accumulator += dt;

            // 2. Tick Logic (Fixed Step)
            var tickRate = _state.TickDuration;

            // Safety cap for lag (max 10 ticks per frame)
            int loops = 0;
            while (_accumulator >= tickRate && loops < 10) {
                ProcessTick(tickRate);
                _accumulator -= tickRate;
                loops++;
            }
        }

        // Helper: Get Level
        public int GetProceduralLevel(string seriesId) => _state._proceduralLevels.ContainsKey(seriesId) ? _state._proceduralLevels[seriesId] : 0;

        // The core math engine
        private void ProcessTick(double dt) {
            // A. Corruption Logic
            if (_state.Corruption < 1.0) {
                _state.Corruption += _state._CurrentCorruptionGrowth * dt;
                if (_state.Corruption > 1.0) _state.Corruption = 1.0;
            }

            // B. Income Logic
            var income = (_state.SoulsPerSecond * _state.TimeScale) * dt;
            if (income > 0) {
                _state.Souls += income;
                _state.LifetimeSouls += income;
            }

            // C. Auto-Buyers (With Timer!)
            _autoBuyTimer += dt;
            if (_autoBuyTimer >= _autoBuyInterval) {
                _autoBuyTimer -= _autoBuyInterval;

                // Only run automation when the timer fires!
                ProcessAutomation();
            }
        }

        private void ProcessAutomation() {
            // 1. Temporarily force "Buy 1" mode
            int oldBuyAmount = _state.BuyAmount;
            _state.BuyAmount = 1;

            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def == null) continue;

                // If this upgrade is an Auto-Buyer...
                if (def.Type == UpgradeType.AutoBuyGenerator && !string.IsNullOrEmpty(def.TargetId)) {

                    // Skip if the player toggled it OFF
                    if (!_state.IsAutoBuyerActive(id)) continue;

                    // FIX: Call the local TryBuyGenerator (not _state.TryBuy...)
                    // Capture the result to see if we actually bought something
                    bool bought = TryBuyGenerator(def.TargetId);

                    if (bought) {
                        OnAutoBuyTriggered?.Invoke(def.Name); // Fire event!
                    }
                }
            }

            // 2. Restore player's buy preference
            _state.BuyAmount = oldBuyAmount;
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
                amountToBuy = Economy.GetMaxBuyable(id);
                if (amountToBuy <= 0) return false; // Can't afford even 1
            }

            // 1. Calculate the REAL total cost
            var totalCost = Economy.GetBulkCost(id, amountToBuy);

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

            var cost = Economy.GetProceduralCost(seriesId);

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
            double reduction = GameData.Rules.PurificationAmount + (amount * 0.01);
            _state.Corruption -= reduction;
            if (_state.Corruption < 0.0) _state.Corruption = 0.0;
            _state._CurrentCorruptionGrowth = GameData.Rules.BaseCorruptionGrowth;
        }

        public void RecalcCps() {
            var total = BigDouble.Zero;
            _state.prestigeMult = BigDouble.One + (_state.RebirthPoints * _state.RebirthBonusPercent);

            // 1. Calculate Global Multipliers and populate Cache
            double globalMult = 1.0;
            _cachedGenMults.Clear();

            foreach (var id in _state._purchasedUpgrades) {
                var def = GameData.GetUpgrade(id);
                if (def.Type == UpgradeType.GlobalMultiplier) {
                    globalMult *= def.Multiplier;
                }
                else if (def.Type == UpgradeType.GeneratorMultiplier) {
                    if (!_cachedGenMults.ContainsKey(def.TargetId)) _cachedGenMults[def.TargetId] = 1.0;
                    _cachedGenMults[def.TargetId] *= def.Multiplier;
                }
            }

            foreach (var series in GameData.UpgradeSeries) {
                int lvl = GetProceduralLevel(series.Id);
                if (lvl > 0) {
                    double mult = Math.Pow(series.MultiplierPerLevel, lvl);
                    if (series.Type == UpgradeType.GlobalMultiplier) {
                        globalMult *= mult;
                    }
                    else if (series.Type == UpgradeType.GeneratorMultiplier) {
                        if (!_cachedGenMults.ContainsKey(series.TargetId)) _cachedGenMults[series.TargetId] = 1.0;
                        _cachedGenMults[series.TargetId] *= mult;
                    }
                }
            }

            // 2. Single Loop through Generators
            foreach (var kvp in _state._generatorCounts) {
                var def = GameData.GetGenerator(kvp.Key);
                if (def == null) continue;

                double specificMult = _cachedGenMults.ContainsKey(def.Id) ? _cachedGenMults[def.Id] : 1.0;

                // Base * Count * GenMult * GlobalMult
                total += def.BaseRevenue * kvp.Value * specificMult * globalMult;
            }
            _state.SoulsPerSecond = total * _state.prestigeMult;
        }


        public void RecalcTap() {
            double mult = 1.0;
            foreach (var id in _state._purchasedUpgrades) {
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
            _state.prestigeMult = BigDouble.One + (_state.RebirthPoints * _state.RebirthBonusPercent);

            _state.TapValue = new BigDouble(1.0) * mult * _state.prestigeMult;
        }

        public void AddCoins(BigDouble amount) {
            _state.Souls += amount;
            _state.LifetimeSouls += amount; // Don't forget lifetime!
        }


        public void Tap() {
            _state.Souls += _state.TapValue;
        }


        public void DoRebirth() {
            var gain = Economy.CalculateRebirthGain();
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

            _state.Corruption = 0.0; // Reset Corruption
            _state._CurrentCorruptionGrowth = GameData.Rules.BaseCorruptionGrowth;
        }

        public void ApplyOfflineProgress(double seconds) {
            // Cap offline time (e.g. 24 hours)
            if (seconds > GameData.Rules.MaxOfflineSeconds) {
                seconds = GameData.Rules.MaxOfflineSeconds;
            }

            // Simulate in chunks (e.g. 1000 ticks) to prevent freezing
            double simulatedDt = 1.0; // Simulate 1 second per tick for speed
            int ticks = (int)(seconds / simulatedDt);

            // Safety cap
            if (ticks > 1000) {
                simulatedDt = seconds / 1000.0;
                ticks = 1000;
            }

            for (int i = 0; i < ticks; i++) {
                ProcessTick(simulatedDt);
            }
        }

        // Helper for Offline Progress
        public void SimulateTimeChunk(double totalSeconds) {
            // Run the logic loop without the graphics
            // This ensures offline progress respects Corruption, AutoBuyers, etc.
            double simulatedDt = 0.1; // 10 ticks per simulated second
            for (double t = 0; t < totalSeconds; t += simulatedDt) {
                ProcessTick(simulatedDt);
            }
        }
    }
}
