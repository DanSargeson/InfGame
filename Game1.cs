using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace InfGame
{

    public enum ViewMode { Generators, Upgrades }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;

       private UIManager _uiManager = new();

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
            
            LoadOrCreateSave();
        }

       
       

        protected override void Update(GameTime gameTime) {
           
            _uiManager.Update(gameTime);
            //_state.Tick();
            base.Update(gameTime);
        }

        

        

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            
            _uiManager.Draw(gameTime, _spriteBatch, _font);
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
                    _state.ApplyOfflineProgress(data.LastSavedUtc, DateTimeOffset.UtcNow);
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