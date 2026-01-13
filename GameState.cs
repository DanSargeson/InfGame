using Android.Media;
using Android.Nfc.Tech;
using System;
using System.Collections.Generic;

namespace InfGame
{
    //    public sealed class GameState
    //    {
    //        // 0.0 = Clean, 1.0 = Fully Corrupted (Stopped)
    //        public double _Corruption { get; set; } = 0.0;

    //        public double _CurrentCorruptionGrowth = 0.0;
    //        public double _BaseCorruptionGrowthRate = 0.001;   //
    //        public double _CorruptionGrowthAccleration = 0.0001; // Growth rate increases over time



    //        private double PurificationAmount = 0.05; // 5% reduction per purification

    //        //Exponential bonus
    //        //50% Corruption = 1.25x Bonus
    //        //90% Corruption = 1.8x Bonus
    //        //99% Corruption = 2.5x Bonus
    //        public double CorruptionBonus => 1.0 + (Math.Pow(_Corruption, 4) * 3.0);

    //        // The Risk/Reward Calculation
    //        public double TimeScale => Math.Max(0.01, 1.0 - _Corruption);


    //        private HashSet<string> _disabledAutoBuyers = new();

    //        // Later, you can upgrade this to 20, 30, etc. to speed up the game.
    //        public double TargetTicksPerSecond { get; private set; } = 10.0;

    //        // Helper: How long is one tick? (e.g., 0.1s)
    //        public double TickDuration => 1.0 / TargetTicksPerSecond;

    //        private Dictionary<string, int> _proceduralLevels = new();

    //        public BigDouble Souls { get; set; }
    //        public BigDouble SoulsPerSecond { get; set; }

    //        // Add a timer for automation
    //        private double _autoBuyTimer = 0.0;
    //        private double _autoBuyInterval = 1.0;


    //        // Inventory: Key = Generator ID, Value = Count owned
    //        private Dictionary<string, int> _generatorCounts = new();

    //        // 1 (x1), 10 (x10), 100 (x100), -1 (Max)
    //        public int BuyAmount { get; set; } = 1;

    //        public HashSet<string> _purchasedUpgrades = new();
    //        public BigDouble TapValue { get; private set; } = new BigDouble(1.0);
    //        public DateTimeOffset LastSavedUtc { get; private set; } = DateTimeOffset.UtcNow;

    //        public BigDouble LifetimeSouls { get; set; }
    //        public BigDouble RebirthPoints { get; set; }
    //        public double RebirthBonusPercent => 0.10;

    //        public BigDouble prestigeMult;











    //        public bool IsAutoBuyerActive(string id) {

    //            return HasUpgrade(id) && !_disabledAutoBuyers.Contains(id);
    //            //    var def = GameData.GetUpgrade(id);
    //            //    if (def != null && def.Type == UpgradeType.AutoBuyGenerator) {
    //            //        return true;
    //            //    }

    //            //return false;
    //        }

    //        public void ToggleAutoBuyer(string id) {

    //            if (_disabledAutoBuyers.Contains(id))
    //                _disabledAutoBuyers.Remove(id);
    //            else
    //                _disabledAutoBuyers.Add(id);
    //        }

    //        // --- New Data-Driven Logic ---

    //        public BigDouble CalculateRebirthGain() {
    //            // Threshold: Don't give points for pocket change
    //            if (LifetimeSouls < 1000000) return BigDouble.Zero;

    //            // Formula: (Lifetime / 1M) ^ (1/3)
    //            var baseVal = LifetimeSouls / 1000000.0;
    //            var gain = BigDouble.Pow(baseVal, 1.0 / 3.0);

    //            gain += CorruptionBonus;

    //            return BigDouble.Floor(gain);
    //        }



    //        public int GetCount(string id) {
    //            return _generatorCounts.ContainsKey(id) ? _generatorCounts[id] : 0;
    //        }

    //        public BigDouble GetCost(string id) {
    //            var def = GameData.GetGenerator(id);
    //            if (def == null) return BigDouble.Zero;

    //            int count = GetCount(id);
    //            // Math: BaseCost * (1.15 ^ Count)
    //            return def.BaseCost * Math.Pow(def.CostMultiplier, count);
    //        }






    //        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);








    //        // --- Standard Stuff ---

    //        public void Tap() {
    //            Souls += TapValue;
    //        }

    //        //TODO
    //        // Future Concept
    //        //void ApplyOfflineProgress(double seconds) {
    //        //    int ticksToRun = (int)(seconds * TargetTicksPerSecond);
    //        //    for (int i = 0; i < ticksToRun; i++) {
    //        //        Tick(); // Actually runs the game logic 50,000 times instantly
    //        //    }
    //        //}

    //        //public void ApplyOfflineProgress(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
    //        //    var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
    //        //    if (seconds <= 0) return;
    //        //    if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;

    //        //    // 1. If we have lots of time, simulate in "Chunks"
    //        //    // e.g. Run 1,000 ticks maximum to prevent the app freezing on load
    //        //    int maxTicks = 1000;

    //        //    // Calculate how much real time each simulated tick represents
    //        //    // If they were gone for 8 hours (28,800s) and we only run 1000 ticks,
    //        //    // each tick must represent 28.8 seconds of progress.
    //        //    double timePerTick = seconds / maxTicks;

    //        //    // Ensure we don't simulate smaller than a normal frame (0.1s)
    //        //    if (timePerTick < TickDuration) {
    //        //        timePerTick = TickDuration;
    //        //        maxTicks = (int)(seconds / TickDuration);
    //        //    }

    //        //    for (int i = 0; i < maxTicks; i++) {
    //        //        // Add coins for this chunk of time
    //        //        Souls += SoulsPerSecond * timePerTick;
    //        //        Souls += SoulsPerSecond * timePerTick;

    //        //        // OPTIONAL: If you add "Auto-Buyers" later, run their logic here!
    //        //        // TryAutoBuyGenerator(); 

    //        //        // IMPORTANT: Recalculate CPS because Auto-Buyers might have changed it
    //        //        // RecalcCps(); 
    //        //    }
    //        //}

    //        public BigDouble CalculateOfflineEarnings(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
    //            var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
    //            if (seconds <= 0) return BigDouble.Zero;

    //            if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;

    //            // Calculate potential earnings
    //            return SoulsPerSecond * seconds;
    //        }

    //        // 2. Apply (Action)
    //        public void AddCoins(BigDouble amount) {
    //            Souls += amount;
    //            LifetimeSouls += amount; // Don't forget lifetime!
    //        }

    //        public void MarkSaved(DateTimeOffset utcNow) {
    //            LastSavedUtc = utcNow;
    //        }

    //        public void LoadFrom(SaveData data) {
    //            Souls = data.Souls;
    //            LastSavedUtc = data.LastSavedUtc;
    //            _generatorCounts = data.GeneratorCounts ?? new Dictionary<string, int>();
    //            LifetimeSouls = data.LifetimeSouls;
    //            RebirthPoints = data.RebirthPoints;
    //            // Load Upgrades
    //            _purchasedUpgrades.Clear();
    //            if (data.UpgradesBought != null) {
    //                foreach (var id in data.UpgradesBought) _purchasedUpgrades.Add(id);
    //            }

    //            _proceduralLevels = data.ProceduralUpgradeLevels ?? new Dictionary<string, int>();

    //            _Corruption = data.Corruption;
    //            _disabledAutoBuyers.Clear();
    //            if (data.DisabledAutoBuyers != null) {
    //                foreach (var id in data.DisabledAutoBuyers) _disabledAutoBuyers.Add(id);
    //            }

    //            RecalcTap();
    //            RecalcCps();
    //        }

    //        public SaveData ToSaveData() {
    //            return new SaveData {
    //                Souls = Souls,
    //                LastSavedUtc = DateTimeOffset.UtcNow,
    //                LifetimeSouls = LifetimeSouls,
    //                RebirthPoints = RebirthPoints,
    //                GeneratorCounts = new Dictionary<string, int>(_generatorCounts),
    //                UpgradesBought = new List<string>(_purchasedUpgrades), // Convert HashSet to List
    //                ProceduralUpgradeLevels = new Dictionary<string, int>(_proceduralLevels),
    //                Corruption = _Corruption,
    //                DisabledAutoBuyers = new List<string>(_disabledAutoBuyers)
    //            };
    //        }
    //    }
    //}

    using InfGame;
    using System.Collections.Generic;

    public class GameState
    {
        // --- DATA (Keep all properties) ---
        public BigDouble Souls { get; set; }
        public BigDouble LifetimeSouls { get; set; }
        public BigDouble SoulsPerSecond { get; set; }// Cached value for UI to read
        public double Corruption { get; set; } = 0.0;

        public Dictionary<string, int> _proceduralLevels = new();

        /* HERE*/

         //0.0 = Clean, 1.0 = Fully Corrupted(Stopped)
                public double _Corruption { get; set; } = 0.0;

        public double _CurrentCorruptionGrowth = 0.0;
        public double _BaseCorruptionGrowthRate = 0.001;   //
        public double _CorruptionGrowthAccleration = 0.0001; // Growth rate increases over time



        public double PurificationAmount = 0.05; // 5% reduction per purification

        //Exponential bonus
        //50% Corruption = 1.25x Bonus
        //90% Corruption = 1.8x Bonus
        //99% Corruption = 2.5x Bonus
        public double CorruptionBonus => 1.0 + (Math.Pow(_Corruption, 4) * 3.0);

        // The Risk/Reward Calculation
        public double TimeScale => Math.Max(0.01, 1.0 - _Corruption);


        public HashSet<string> _disabledAutoBuyers = new();

        // Later, you can upgrade this to 20, 30, etc. to speed up the game.
        public double TargetTicksPerSecond { get; private set; } = 10.0;

        // Helper: How long is one tick? (e.g., 0.1s)
        public double TickDuration => 1.0 / TargetTicksPerSecond;

        // Add a timer for automation
        public double _autoBuyTimer = 0.0;
        public double _autoBuyInterval = 1.0;


        // Inventory: Key = Generator ID, Value = Count owned
        public Dictionary<string, int> _generatorCounts = new();

        // 1 (x1), 10 (x10), 100 (x100), -1 (Max)
        public int BuyAmount { get; set; } = 1;

        public HashSet<string> _purchasedUpgrades = new();
        public BigDouble TapValue { get; set; } = new BigDouble(1.0);
        public DateTimeOffset LastSavedUtc { get; set; } = DateTimeOffset.UtcNow;
        public BigDouble RebirthPoints { get; set; }
        public double RebirthBonusPercent => 0.10;

        public BigDouble prestigeMult;

        /* SPLIT*/


        // ... all other variables ...

        // --- COLLECTIONS ---
       // public Dictionary<string, int> _generatorCounts = new();
        //public HashSet<string> _purchasedUpgrades = new();

        // --- READ-ONLY HELPERS (Keep these!) ---
        // These are fine because they don't *change* anything, they just look up data.
        public int GetCount(string id) => _generatorCounts.ContainsKey(id) ? _generatorCounts[id] : 0;
        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);

        public bool IsAutoBuyerActive(string id) {

            return HasUpgrade(id) && !_disabledAutoBuyers.Contains(id);
            //    var def = GameData.GetUpgrade(id);
            //    if (def != null && def.Type == UpgradeType.AutoBuyGenerator) {
            //        return true;
            //    }

            //return false;
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

        public BigDouble GetCost(string id) {
            var def = GameData.GetGenerator(id);
            if (def == null) return BigDouble.Zero;

            int count = GetCount(id);
            // Math: BaseCost * (1.15 ^ Count)
            return def.BaseCost * Math.Pow(def.CostMultiplier, count);
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

        public BigDouble CalculateOfflineEarnings(DateTimeOffset lastSavedUtc, DateTimeOffset nowUtc, double maxOfflineSeconds = 28800) {
            var seconds = (nowUtc - lastSavedUtc).TotalSeconds;
            if (seconds <= 0) return BigDouble.Zero;

            if (seconds > maxOfflineSeconds) seconds = maxOfflineSeconds;

            // Calculate potential earnings
            return SoulsPerSecond * seconds;
        }

        public void AddCoins(BigDouble amount) {
            Souls += amount;
            LifetimeSouls += amount; // Don't forget lifetime!
        }

        // Helper: Get Level
        public int GetProceduralLevel(string seriesId) => _proceduralLevels.ContainsKey(seriesId) ? _proceduralLevels[seriesId] : 0;

        public void MarkSaved(DateTimeOffset utcNow) {
            LastSavedUtc = utcNow;
        }

        // Helper: Calculate Cost for the NEXT level
        public BigDouble GetProceduralCost(string seriesId) {
            var def = GameData.GetSeries(seriesId);
            if (def == null) return BigDouble.Zero;

            int currentLevel = GetProceduralLevel(seriesId);
            // Formula: Base * (Growth ^ Level)
            return def.BaseCost * BigDouble.Pow(def.CostMultiplier, currentLevel);
        }


        // --- SAVE/LOAD (Keep these!) ---
        public SaveData ToSaveData() {

            return new SaveData {
                Souls = Souls,
                LastSavedUtc = DateTimeOffset.UtcNow,
                LifetimeSouls = LifetimeSouls,
                RebirthPoints = RebirthPoints,
                GeneratorCounts = new Dictionary<string, int>(_generatorCounts),
                UpgradesBought = new List<string>(_purchasedUpgrades), // Convert HashSet to List
                ProceduralUpgradeLevels = new Dictionary<string, int>(_proceduralLevels),
                Corruption = _Corruption,
                DisabledAutoBuyers = new List<string>(_disabledAutoBuyers)
            };
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

            _Corruption = data.Corruption;
            _disabledAutoBuyers.Clear();
            if (data.DisabledAutoBuyers != null) {
                foreach (var id in data.DisabledAutoBuyers) _disabledAutoBuyers.Add(id);
            }

            //RecalcTap();
            //RecalcCps();
        }
    }
}