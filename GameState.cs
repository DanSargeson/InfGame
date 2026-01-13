using System;
using System.Collections.Generic;

namespace InfGame
{
    public class GameState
    {
        // --- 1. PERSISTENT DATA (Saved to Disk) ---
        public BigDouble Souls { get; set; }
        public BigDouble LifetimeSouls { get; set; }
        public BigDouble RebirthPoints { get; set; }
        public DateTimeOffset LastSavedUtc { get; set; } = DateTimeOffset.UtcNow;

        // Corruption System
        public double Corruption { get; set; } = 0.0;

        // Inventory
        public Dictionary<string, int> _generatorCounts = new();
        public Dictionary<string, int> _proceduralLevels = new();
        public HashSet<string> _purchasedUpgrades = new();

        // Settings / Toggles
        public int BuyAmount { get; set; } = 1;
        public HashSet<string> _disabledAutoBuyers = new();

        // --- 2. RUNTIME CACHE (Not Saved, Calculated by Simulator) ---
        public BigDouble SoulsPerSecond { get; set; }
        public BigDouble TapValue { get; set; } = new BigDouble(1.0);
        public BigDouble prestigeMult { get; set; } = BigDouble.One;

        // --- 3. CONFIGURATION (Constants) ---
        // These could be in GameData, but fine here for now
        public double TargetTicksPerSecond { get; private set; } = 10.0;
        public double TickDuration => 1.0 / TargetTicksPerSecond;

        public double PurificationAmount = 0.05;
        public double _BaseCorruptionGrowthRate = 0.001;
        public double _CorruptionGrowthAccleration = 0.0001;

        // Runtime simulation variables (Not crucial to save, but good to keep state)
        public double _CurrentCorruptionGrowth = 0.0;
        public double _autoBuyTimer = 0.0; // Moved back here if you want it accessible to Sim easily
        public double _autoBuyInterval = 1.0;

        // --- 4. CALCULATED PROPERTIES (Read-Only Rules) ---
        // These are fine to keep here as they describe the STATE of the world
        public double CorruptionBonus => 1.0 + (Math.Pow(Corruption, 4) * 3.0);
        public double TimeScale => Math.Max(0.01, 1.0 - Corruption);
        public double RebirthBonusPercent => 0.10;

        // --- 5. HELPER METHODS (Read-Only) ---
        public int GetCount(string id) => _generatorCounts.ContainsKey(id) ? _generatorCounts[id] : 0;
        public bool HasUpgrade(string id) => _purchasedUpgrades.Contains(id);
        public int GetProceduralLevel(string id) => _proceduralLevels.ContainsKey(id) ? _proceduralLevels[id] : 0;

        public bool IsAutoBuyerActive(string id) {
            return HasUpgrade(id) && !_disabledAutoBuyers.Contains(id);
        }

        public void ToggleAutoBuyer(string id) {
            if (_disabledAutoBuyers.Contains(id)) _disabledAutoBuyers.Remove(id);
            else _disabledAutoBuyers.Add(id);
        }

        public void MarkSaved(DateTimeOffset utcNow) {
            LastSavedUtc = utcNow;
        }

        // --- 6. SAVE SYSTEM ---
        public SaveData ToSaveData() {
            return new SaveData {
                Souls = Souls,
                LifetimeSouls = LifetimeSouls,
                RebirthPoints = RebirthPoints,
                LastSavedUtc = DateTimeOffset.UtcNow,

                Corruption = Corruption,

                GeneratorCounts = new Dictionary<string, int>(_generatorCounts),
                UpgradesBought = new List<string>(_purchasedUpgrades),
                ProceduralUpgradeLevels = new Dictionary<string, int>(_proceduralLevels),
                DisabledAutoBuyers = new List<string>(_disabledAutoBuyers)
            };
        }

        public void LoadFrom(SaveData data) {
            Souls = data.Souls;
            LifetimeSouls = data.LifetimeSouls;
            RebirthPoints = data.RebirthPoints;
            LastSavedUtc = data.LastSavedUtc;

            Corruption = data.Corruption;

            _generatorCounts = data.GeneratorCounts ?? new Dictionary<string, int>();
            _proceduralLevels = data.ProceduralUpgradeLevels ?? new Dictionary<string, int>();

            _purchasedUpgrades.Clear();
            if (data.UpgradesBought != null) {
                foreach (var id in data.UpgradesBought) _purchasedUpgrades.Add(id);
            }

            _disabledAutoBuyers.Clear();
            if (data.DisabledAutoBuyers != null) {
                foreach (var id in data.DisabledAutoBuyers) _disabledAutoBuyers.Add(id);
            }
        }
    }
}