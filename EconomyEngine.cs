using System;

namespace InfGame
{
    public class EconomyEngine
    {
        private readonly GameState _state;

        public EconomyEngine(GameState state) {
            _state = state;
        }

        // --- Generator Costs ---
        public BigDouble GetCost(string id) {
            var def = GameData.GetGenerator(id);
            if (def == null) return BigDouble.Zero;

            int count = _state.GetCount(id);
            return def.BaseCost * Math.Pow(def.CostMultiplier, count);
        }

        public BigDouble GetBulkCost(string id, int count) {
            var def = GameData.GetGenerator(id);
            if (def == null) return BigDouble.Zero;

            var nextCost = GetCost(id);
            double r = def.CostMultiplier;

            if (count == 1) return nextCost;

            var numerator = BigDouble.Pow(r, count) - 1.0;
            var denominator = r - 1.0;

            return nextCost * (numerator / denominator);
        }

        public int GetMaxBuyable(string id) {
            var def = GameData.GetGenerator(id);
            if (def == null) return 0;

            var nextCost = GetCost(id);
            if (_state.Souls < nextCost) return 0;

            double r = def.CostMultiplier;
            var term = (_state.Souls * (r - 1.0)) / nextCost;
            var logValue = BigDouble.Log10(term + 1.0) / Math.Log10(r);

            return (int)Math.Floor(logValue);
        }

        // --- Procedural Costs ---
        public BigDouble GetProceduralCost(string seriesId) {
            var def = GameData.GetSeries(seriesId);
            if (def == null) return BigDouble.Zero;

            int currentLevel = _state.GetProceduralLevel(seriesId);
            return def.BaseCost * BigDouble.Pow(def.CostMultiplier, currentLevel);
        }

        // --- Prestige Math ---
        public BigDouble CalculateRebirthGain() {
            var threshold = GameData.Rules.RebirthLifetimeThreshold;

            if (_state.LifetimeSouls < threshold) return BigDouble.Zero;

            var baseVal = _state.LifetimeSouls / threshold;
            var gain = BigDouble.Pow(baseVal, GameData.Rules.RebirthExponent);

            gain += _state.CorruptionBonus;

            return BigDouble.Floor(gain);
        }
    }
}