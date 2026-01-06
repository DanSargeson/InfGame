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
        private UiButton _buyT1;
        private UiButton _buyT2;
        private UiButton _buyT3;

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

        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _font = Content.Load<SpriteFont>("UIFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _needsLayout = true;

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savePath = Path.Combine(dir, "infgame_save.json");
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
            if (_tapButton == null || _buyT1 == null)
                return;

            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();
                if (g.GestureType != GestureType.Tap) continue;

                var p = new Point((int)g.Position.X, (int)g.Position.Y);

                if (_tapButton.HitTest(p)) { _state.Tap(); continue; }
                if (_buyT1.HitTest(p)) { _state.TryBuyGenerator("gen_t1"); continue; }
                if (_buyT2.HitTest(p)) { _state.TryBuyGenerator("gen_t2"); continue; }
                if (_buyT3.HitTest(p)) { _state.TryBuyGenerator("gen_t3"); continue; }
            }
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            var coinsText = $"Coins: {NumberFormat.Compact(_state.Coins)}";
            var cpsText = $"CPS: {NumberFormat.Compact(_state.CoinsPerSecond, 2)}";
            //var genCost = _state.GetNextGeneratorCost();
            //var genText = $"Generators: {_state.Generators}  (Cost: {NumberFormat.Compact(genCost)})";

            _spriteBatch.DrawString(_font, coinsText, new Vector2(240, 240), Color.White);
            _spriteBatch.DrawString(_font, cpsText, new Vector2(240, 280), Color.White);
           // _spriteBatch.DrawString(_font, genText, new Vector2(240, 640), Color.White);

            _tapButton.Draw(_spriteBatch, _font, _pixel);
            // We need to manually construct the text for each button here (temporary)
            // Ideally, the Button class would hold a reference to the GeneratorDef later.
            UpdateButtonText(_buyT1, "gen_t1");
            UpdateButtonText(_buyT2, "gen_t2");
            UpdateButtonText(_buyT3, "gen_t3");

            _buyT1.Draw(_spriteBatch, _font, _pixel);
            _buyT2.Draw(_spriteBatch, _font, _pixel);
            _buyT3.Draw(_spriteBatch, _font, _pixel);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // Helper to show dynamic cost on the button
        private void UpdateButtonText(UiButton btn, string id) {
            var def = GameData.GetGenerator(id);
            var cost = _state.GetCost(id);
            var count = _state.GetCount(id);

            // e.g. "Intern (5)\n$150"
            btn.Text = $"{def.Name} ({count})\n{NumberFormat.Compact(cost)}";
        }

        private void LayoutButtons() {
           // int w = GraphicsDevice.Viewport.Width;
            //int h = GraphicsDevice.Viewport.Height;

            //int pad = 200;
            //int btnH = 120;
            //int btnW = (w - pad * 3) / 2;

          //  var y = h - pad - btnH;

        //    _tapButton = new UiButton(new Rectangle(pad, y, btnW, btnH), "TAP +1");
            int w = GraphicsDevice.Viewport.Width;
            int pad = 20;
            int h = 100; // button height
            int y = 600; // start Y

            _tapButton = new UiButton(new Rectangle(pad, y, w - pad * 2, h), "TAP");
            y += h + pad;

            _buyT1 = new UiButton(new Rectangle(pad, y, w - pad * 2, h), "");
            y += h + pad;

            _buyT2 = new UiButton(new Rectangle(pad, y, w - pad * 2, h), "");
            y += h + pad;

            _buyT3 = new UiButton(new Rectangle(pad, y, w - pad * 2, h), "");
        }

        private void LoadOrCreateSave() {
            if (!File.Exists(_savePath)) {
                _state.MarkSaved(DateTimeOffset.UtcNow);
                Save(); // This will create the first valid file
                return;
            }

            try {
                var json = File.ReadAllText(_savePath);

                // FIX: Pass _jsonOptions so it uses BigDoubleConverter
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                if (data != null) {
                    _state.LoadFrom(data);
                    _state.ApplyOfflineProgress(data.LastSavedUtc, DateTimeOffset.UtcNow);
                }
            }
            catch (Exception ex) {
                // Debugging tip: Print ex.Message here to see why it failed
                // For now, if load fails (e.g. old format), we just start fresh
                System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
            }
        }

        private void Save() {
            try {
                // 1. Get the data from the State
                var data = _state.ToSaveData();

                // 2. Serialize it using the Options (so BigDouble looks like { "m": 1, "e": 0 })
                var json = JsonSerializer.Serialize(data, _jsonOptions);

                // 3. Write to disk
                File.WriteAllText(_savePath, json);
            }
            catch {
                // Ignore errors during gameplay
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
