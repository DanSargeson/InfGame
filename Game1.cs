using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
        private InputManager _inputManager;

        private SaveManager _saveManager;


        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Enable Tap AND VerticalDrag for scrolling
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.VerticalDrag;
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
                using (var stream = TitleContainer.OpenStream("Content/GameData.json"))
                using (var reader = new StreamReader(stream)) {
                    var json = reader.ReadToEnd();
                    GameData.Load(json);
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to load GameData: {ex.Message}");
            }

            // Initialize Systems
            _saveManager = new SaveManager();
            _sim = new GameSimulator(_state);
            _inputManager = new InputManager();
            _uiManager = new UIManager(_state, _sim, _inputManager, _graphics.GraphicsDevice);

            _sim.OnAutoBuyTriggered += (msg) => {
                _uiManager.SpawnFloatingText(new Vector2(200, 200), msg, Color.Cyan);
            };

            // Process Save & Offline Progress
            var loadResult = _saveManager.Load(_state);

            _sim.RecalcCps();
            _sim.RecalcTap();

            if (!loadResult.IsNewSave && loadResult.SecondsOffline > 0) {
                var soulsBefore = _state.Souls;
                _sim.ApplyOfflineProgress(loadResult.SecondsOffline);
                var earned = _state.Souls - soulsBefore;

                if (loadResult.SecondsOffline > 60) {
                    TimeSpan timeOffline = TimeSpan.FromSeconds(loadResult.SecondsOffline);
                    _uiManager.ShowOfflineReport(earned, timeOffline);
                }
            }
        }




        protected override void Update(GameTime gameTime) {

            var dt = gameTime.ElapsedGameTime.TotalSeconds;

            _saveTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_saveTimer > 30.0) { // Save every 30s
                                     // Fire and forget off the main UI thread
                Task.Run(() => _saveManager.Save(_state));
                _saveTimer = 0;
            }

            _inputManager.Update();
            _sim.Update(dt);
            _uiManager.Update(dt);
            base.Update(gameTime);
        }

        

        

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            
            _uiManager.Draw(_pixel, _spriteBatch, _font);
            base.Draw(gameTime);
        }

      
        
        

        protected override void OnDeactivated(object sender, EventArgs args) { _saveManager.Save(_state); base.OnDeactivated(sender, args); }
        protected override void OnExiting(object sender, Microsoft.Xna.Framework.ExitingEventArgs args) { _saveManager.Save(_state); base.OnExiting(sender, args); }
    }
}