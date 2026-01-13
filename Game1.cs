using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using static Android.Provider.Contacts.Intents;

namespace InfGame
{

    public enum ViewMode
    {
        Generators,
        Upgrades,
        AutoBuyers,   // New
        RebirthShop,
        Settings      // New
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;

        private double _saveTimer = 0;

        private readonly GameState _state = new();

        private UIManager _uiManager;
        private GameSimulator _sim;

        // Layout State
        private string _savePath;
        private readonly JsonSerializerOptions _jsonOptions;

       

        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Enable Tap AND VerticalDrag for scrolling
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.VerticalDrag;

            _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            _jsonOptions.Converters.Add(new BigDoubleConverter());

        }

        protected override void Initialize() {
            base.Initialize();

            // Uncomment once to reset if you have corrupted saves
            // if (File.Exists(_savePath)) File.Delete(_savePath); 
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("UIFont");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });


            try {
                // "Content/GameData.json" matches the file path in your project
                using (var stream = TitleContainer.OpenStream("Content/GameData.json"))
                using (var reader = new StreamReader(stream)) {
                    var json = reader.ReadToEnd();
                    GameData.Load(json);
                }
            }
            catch (Exception ex) {
                // Fallback or Crash if critical data is missing
                System.Diagnostics.Debug.WriteLine($"Failed to load GameData: {ex.Message}");
            }

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savePath = Path.Combine(dir, "infgame_save.json");
            
            _uiManager = new UIManager(_state, _graphics.GraphicsDevice);
            _sim = new GameSimulator(_state);

            _sim.OnAutoBuyTriggered += (msg) => {
                _uiManager.SpawnFloatingText(new Vector2(200, 200), msg, Color.Cyan);
            };
            LoadOrCreateSave();
        }

       
       

        protected override void Update(GameTime gameTime) {

            _saveTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_saveTimer > 30.0) { // Save every 30s
                Save();
                _saveTimer = 0;
            }

            _sim.Update(gameTime.ElapsedGameTime.TotalSeconds);
            _uiManager.Update(gameTime);
            base.Update(gameTime);
        }

        

        

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            
            _uiManager.Draw(_pixel, _spriteBatch, _font);
            base.Draw(gameTime);
        }

       

        

        // --- Save/Load Boilerplate (Unchanged) ---
        private void LoadOrCreateSave() {
            if (!File.Exists(_savePath)) {
                _state.MarkSaved(DateTimeOffset.UtcNow);
                Save();
                return;
            }
            try {
                var json = File.ReadAllText(_savePath);
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
                if (data != null) {
                    _state.LoadFrom(data);

                    // --- NEW OFFLINE LOGIC ---
                    var now = DateTimeOffset.UtcNow;
                    _uiManager._offlineEarnings = _state.CalculateOfflineEarnings(data.LastSavedUtc, now);

                    if (_uiManager._offlineEarnings > 0) {
                        _uiManager._showWelcomeModal = true;

                        
                        var timeSpan = now - data.LastSavedUtc;
                        _uiManager._offlineTimeText = $"Offline for: {(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

                        // Create the Collect Button (Use Object Pooling!)
                        var w = GraphicsDevice.Viewport.Width;
                        var h = GraphicsDevice.Viewport.Height;

                        // Center button
                        _uiManager._collectButton = _uiManager.GetPooledButton(
                            new Rectangle(w / 2 - 100, h / 2 + 50, 200, 80),
                            "COLLECT",
                            () => {
                                _state.AddCoins(_uiManager._offlineEarnings);
                                _uiManager._showWelcomeModal = false;
                               _uiManager.ReturnToPool(_uiManager._collectButton); // Clean up
                            }
                        );
                    }
                }
            }
            catch (Exception ex){

                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void Save() {
            try {
                var data = _state.ToSaveData();
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(_savePath, json);
            }
            catch(Exception ex) {

                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void OnDeactivated(object sender, EventArgs args) { Save(); base.OnDeactivated(sender, args); }
        protected override void OnExiting(object sender, Microsoft.Xna.Framework.ExitingEventArgs args) { Save(); base.OnExiting(sender, args); }
    }
}