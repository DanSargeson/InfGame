using System;
using System.IO;
using System.Text.Json;

namespace InfGame
{
    // A simple payload to hand back to Game1 so it knows what happened while offline
    public class LoadResult
    {
        public bool IsNewSave { get; set; }
        public double SecondsOffline { get; set; }
    }

    public class SaveManager
    {
        private readonly string _savePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public SaveManager() {
            // Android/Desktop agnostic save path
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savePath = Path.Combine(dir, "infgame_save.json");

            _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            _jsonOptions.Converters.Add(new BigDoubleConverter());
        }

        public void Save(GameState state) {
            try {
                // ToSaveData() internally sets LastSavedUtc
                var data = state.ToSaveData();
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to save: {ex.Message}");
            }
        }

        public LoadResult Load(GameState state) {
            var result = new LoadResult { IsNewSave = true, SecondsOffline = 0 };

            if (!File.Exists(_savePath)) {
                state.MarkSaved(DateTimeOffset.UtcNow);
                Save(state); // Generate the initial file
                return result;
            }

            try {
                var json = File.ReadAllText(_savePath);
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                if (data != null) {
                    state.LoadFrom(data);
                    result.IsNewSave = false;

                    // Calculate offline time here, keep this math out of Game1
                    var now = DateTimeOffset.UtcNow;
                    var timeSpan = now - data.LastSavedUtc;
                    result.SecondsOffline = timeSpan.TotalSeconds;
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to load save: {ex.Message}");
                // If it crashes, we just return a "new save" state so the game doesn't hang
            }

            return result;
        }
    }
}