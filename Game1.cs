using System;
using System.Collections.Generic;
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

        // UI Components
        private UiButton _tapButton;
        private List<UiButton> _genButtons = new();

        //The Time Accumulator
        private double _accumulator = 0.0;

        // Scroll State
        private float _scrollY = 0;
        private float _maxScroll = 0;
        private Rectangle _listBounds; // The visible window for the list

        // Layout State
        private string _savePath;
        private bool _needsLayout = true;
        private readonly JsonSerializerOptions _jsonOptions;

        // Clipping State (New)
        private RasterizerState _scissorState = new RasterizerState { ScissorTestEnable = true };

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

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savePath = Path.Combine(dir, "infgame_save.json");
            _needsLayout = true;
            LoadOrCreateSave();
        }

        protected override void Update(GameTime gameTime) {
            var dt = gameTime.ElapsedGameTime.TotalSeconds;
            // 1. Add real time to the "Bank"
            _accumulator += gameTime.ElapsedGameTime.TotalSeconds;

            // 2. Spend time to run Ticks
            // While we have enough time banked for a tick, run it.
            var tickRate = _state.TickDuration;

            // Safety: Prevent "Spiral of Death" if game lags massively
            if (_accumulator > 1.0) _accumulator = 1.0;

            while (_accumulator >= tickRate) {
                _state.Tick(); // Logic runs here
                _accumulator -= tickRate;

                // UI Updates that rely on logic (like disabling buttons) 
                // can technically happen here or once per frame below.
                UpdateGeneratorButtons(tickRate);
            }

            // 3. Handle Input (Input is usually per-frame, not per-tick)
            if (_needsLayout) {
                LayoutUI();
                _needsLayout = false;
            }

            HandleInput();

            // Update visual timers (animations) using Real Time, not Tick Time
            _tapButton.Update(dt);
            UpdateGeneratorButtons(0);

            //_state.Tick();
            base.Update(gameTime);
        }

        private void UpdateGeneratorButtons(double dt) {
            foreach (var btn in _genButtons) {
                btn.Update(dt);

                // Find the definition matching this button
                // (In a bigger game, store 'Def' on the button itself)
                int index = _genButtons.IndexOf(btn);
                if (index < 0 || index >= GameData.Generators.Count) continue;

                var def = GameData.Generators[index];
                var cost = _state.GetCost(def.Id);
                var count = _state.GetCount(def.Id);

                // Dynamic Text & State
                btn.Text = $"{def.Name} ({count})\n{NumberFormat.Compact(cost)}";
                btn.IsActive = _state.Coins >= cost;
            }
        }

        private void HandleInput() {
            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();

                if (g.GestureType == GestureType.Tap) {
                    var p = new Point((int)g.Position.X, (int)g.Position.Y);

                    // 1. Check Header Buttons (Tap)
                    if (_tapButton.HitTest(p)) {
                        _tapButton.TriggerFlash();
                        _tapButton.OnClick?.Invoke();
                        continue;
                    }

                    // 2. Check Scroll List
                    // Only click if inside the list view
                    if (_listBounds.Contains(p)) {
                        // Offset the touch to match the scrolled buttons
                        var scrollPoint = new Point(p.X, p.Y + (int)_scrollY);

                        foreach (var btn in _genButtons) {
                            if (btn.HitTest(scrollPoint)) {
                                btn.TriggerFlash();
                                btn.OnClick?.Invoke();
                                break;
                            }
                        }
                    }
                }
                else if (g.GestureType == GestureType.VerticalDrag) {
                    // Scroll Logic
                    _scrollY -= g.Delta.Y;

                    // Clamp Scroll
                    if (_scrollY < 0) _scrollY = 0;
                    if (_scrollY > _maxScroll) _scrollY = _maxScroll;
                }
            }
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            // --- 1. Draw Header (Static) ---
            _spriteBatch.Begin();
            DrawHeader();
            _spriteBatch.End();

            // --- 2. Draw List (Scissor Clipped) ---
            // This ensures buttons don't draw over the header when scrolling
            _spriteBatch.Begin(rasterizerState: _scissorState);

            GraphicsDevice.ScissorRectangle = _listBounds;

            foreach (var btn in _genButtons) {
                // Optimization: Don't draw if off-screen
                if (btn.Bounds.Bottom - _scrollY < _listBounds.Top) continue;
                if (btn.Bounds.Top - _scrollY > _listBounds.Bottom) continue;

                btn.Draw(_spriteBatch, _font, _pixel, (int)_scrollY);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawHeader() {
            var coinsText = $"Coins: {NumberFormat.Compact(_state.Coins)}";
            var cpsText = $"CPS: {NumberFormat.Compact(_state.CoinsPerSecond, 2)}";

            _spriteBatch.DrawString(_font, coinsText, new Vector2(50, 200), Color.White);
            _spriteBatch.DrawString(_font, cpsText, new Vector2(50, 240), Color.White);

            _tapButton.Draw(_spriteBatch, _font, _pixel);
        }

        private void LayoutUI() {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;
            int pad = 20;

            // --- 1. The Fixed Elements ---

            // You wanted the TAP button at Y=400
            int tapY = 400;
            int btnH = 100;

            _tapButton = new UiButton(new Rectangle(pad, tapY, w - pad * 2, btnH), "TAP +1", () => _state.Tap());

            // --- 2. The List Area ---

            // The list should start AFTER the tap button (+ padding)
            int listStartY = tapY + btnH + pad;

            // The Scissor Rect defines the "Window" we can see through.
            // It starts below the TAP button and goes to the bottom of the screen.
            _listBounds = new Rectangle(0, listStartY, w, h - listStartY);

            // --- 3. Generate Content ---
            _genButtons.Clear();

            // IMPORTANT: The buttons must start at the same Y as the ListBounds
            int currentY = listStartY;

            foreach (var def in GameData.Generators) {
                string id = def.Id;

                var btnRect = new Rectangle(pad, currentY, w - pad * 2, btnH);
                var btn = new UiButton(btnRect, def.Name, () => _state.TryBuyGenerator(id));

                _genButtons.Add(btn);
                currentY += btnH + pad;
            }

            // Calculate max scroll range
            // If buttons end at 1200 and screen ends at 800, we need 400 scroll.
            _maxScroll = Math.Max(0, currentY - h + pad);
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
            catch { }
        }

        private void Save() {
            try {
                var data = _state.ToSaveData();
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(_savePath, json);
            }
            catch { }
        }

        protected override void OnDeactivated(object sender, EventArgs args) { Save(); base.OnDeactivated(sender, args); }
        protected override void OnExiting(object sender, Microsoft.Xna.Framework.ExitingEventArgs args) { Save(); base.OnExiting(sender, args); }
    }
}