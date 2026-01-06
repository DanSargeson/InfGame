using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace InfGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private SpriteFont _font;
        private Texture2D _pixel;

        private readonly GameState _state = new();

        private UiButton _tapButton;
        private UiButton _buyGenButton;

        private string _savePath;
        private bool _needsLayout = true;

        private readonly JsonSerializerOptions _jsonOptions;


        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            TouchPanel.EnabledGestures = GestureType.Tap;

            _jsonOptions = new JsonSerializerOptions {
                WriteIndented = true
            };
            _jsonOptions.Converters.Add(new BigDoubleConverter());
        }

        protected override void Initialize() {
            base.Initialize();

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savePath = Path.Combine(dir, "infgame_save.json");
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _font = Content.Load<SpriteFont>("UIFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _needsLayout = true;

            LoadOrCreateSave();
        }

        protected override void OnActivated(object sender, EventArgs args) {
            base.OnActivated(sender, args);
            _needsLayout = true; 
        }


        protected override void Update(GameTime gameTime) {
            var dt = gameTime.ElapsedGameTime.TotalSeconds;

            if (_needsLayout && GraphicsDevice != null) {
                LayoutButtons();
                _needsLayout = false;
            }

            HandleTouch();

            _state.Tick(dt);

            base.Update(gameTime);
        }

        private void HandleTouch() {
            if (_tapButton == null || _buyGenButton == null)
                return;

            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();
                if (g.GestureType != GestureType.Tap) continue;

                var p = new Point((int)g.Position.X, (int)g.Position.Y);

                if (_tapButton.HitTest(p)) { _state.Tap(); continue; }
                if (_buyGenButton.HitTest(p)) { _state.TryBuyGenerator(); continue; }
            }
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            var coinsText = $"Coins: {NumberFormat.Compact(_state.Coins)}";
            var cpsText = $"CPS: {NumberFormat.Compact(_state.CoinsPerSecond, 2)}";
            var genCost = _state.GetNextGeneratorCost();
            var genText = $"Generators: {_state.Generators}  (Cost: {NumberFormat.Compact(genCost)})";

            _spriteBatch.DrawString(_font, coinsText, new Vector2(240, 240), Color.White);
            _spriteBatch.DrawString(_font, cpsText, new Vector2(240, 440), Color.White);
            _spriteBatch.DrawString(_font, genText, new Vector2(240, 640), Color.White);

            _tapButton.Draw(_spriteBatch, _font, _pixel);
            _buyGenButton.Draw(_spriteBatch, _font, _pixel);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void LayoutButtons() {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;

            int pad = 200;
            int btnH = 120;
            int btnW = (w - pad * 3) / 2;

            var y = h - pad - btnH;

            _tapButton = new UiButton(new Rectangle(pad, y, btnW, btnH), "TAP +1");
            _buyGenButton = new UiButton(new Rectangle(pad * 2 + btnW, y, btnW, btnH), "BUY GEN");
        }

        private void LoadOrCreateSave() {
            if (!File.Exists(_savePath)) {
                // default state; mark saved time for offline calc later
                _state.MarkSaved(DateTimeOffset.UtcNow);
                Save();
                return;
            }

            try {
                var json = File.ReadAllText(_savePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);

                if (data != null) {
                    _state.LoadFrom(data);
                    _state.ApplyOfflineProgress(data.LastSavedUtc, DateTimeOffset.UtcNow);
                }
            }
            catch {
                // If save is corrupted, start fresh (don’t brick launch)
            }
        }

        private void Save() {
            try {
                var json = File.ReadAllText(_savePath);
                // Pass _jsonOptions here
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
                //data = _state.ToSaveData();
                json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_savePath, json);
            }
            catch {
                // ignore; day 1
            }
        }

        protected override void OnDeactivated(object sender, EventArgs args) {
            Save();
            base.OnDeactivated(sender, args);
        }

        protected override void OnExiting(object sender, Microsoft.Xna.Framework.ExitingEventArgs args) {
            Save();
            base.OnExiting(sender, args);
        }
    }
}
