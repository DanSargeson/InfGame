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
