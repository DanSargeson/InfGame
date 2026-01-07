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

        private readonly GameState _state = new();

        // UI Components
        private ViewMode _viewMode = ViewMode.Generators;
        private UiButton _toggleButton;
        private UiButton _buyMultButton;
        //   private UiButton _tapButton;
        private List<UiButton> _genButtons = new();

        private UiButton _prestigeButton;

        private List<FloatingText> _particles = new();

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

        private void SpawnFloatingText(Vector2 pos, string text, Color color) {
            // Randomize X slightly (-20 to +20)
            var rnd = new Random();
            float xOffset = rnd.Next(-20, 21);

            var finalPos = new Vector2(pos.X + xOffset, pos.Y);
            _particles.Add(new FloatingText(finalPos, text, color));
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

            // Update Prestige Button
            var potentialGain = _state.CalculatePrestigeGain();

            if (potentialGain > 0) {
                _prestigeButton.Text = $"PRESTIGE: +{NumberFormat.Compact(potentialGain)} POINTS\n(+{potentialGain.ToDouble() * 10}% Bonus)";
                _prestigeButton.IsActive = true;
            }
            else {
                _prestigeButton.Text = "Prestige Locked (Need 1M Coins)";
                _prestigeButton.IsActive = false;
            }

            _prestigeButton.Update(0);

            // Update visual timers (animations) using Real Time, not Tick Time
            // _tapButton.Update(dt);
            UpdateGeneratorButtons(0);


            // Update Particles
            
            for (int i = _particles.Count - 1; i >= 0; i--) {
                _particles[i].Update(dt);
                if (!_particles[i].IsActive) {
                    _particles.RemoveAt(i);
                }
            }

            //_state.Tick();
            base.Update(gameTime);
        }

        private void UpdateGeneratorButtons(double dt) {
            foreach (var btn in _genButtons) {
                btn.Update(dt);

                // CASE 1: It is a Generator
                if (btn.Tag is GeneratorDef genDef) {
                    // 1. Determine how many we are trying to buy
                    int amount = _state.BuyAmount;
                    string prefix = $"x{amount}";

                    if (amount == -1) {
                        // Max Logic: Show how many we CAN buy
                        amount = _state.GetMaxBuyable(genDef.Id);
                        // If we can't afford any, show cost of 1 (grayed out) so they know goal
                        if (amount == 0) {
                            amount = 1;
                            prefix = "Max";
                        }
                        else {
                            prefix = $"x{amount}";
                        }
                    }

                    // 2. Calculate Cost
                    var totalCost = _state.GetBulkCost(genDef.Id, amount);
                    var currentCount = _state.GetCount(genDef.Id);

                    // 3. Update UI
                    btn.Text = $"{genDef.Name} ({currentCount})\n{prefix}: {NumberFormat.Compact(totalCost)}";
                    btn.IsActive = _state.Coins >= totalCost;
                    string label = _state.BuyAmount == -1 ? "Max" : $"{_state.BuyAmount}x";
                    _buyMultButton.Text = $"BUY: {label}";
                }
                // CASE 2: It is an Upgrade
                else if (btn.Tag is UpgradeDef upgDef) {
                    // Show Currency Symbol
                    string priceLabel = upgDef.CostCurrency == CurrencyType.Coins
                             ? NumberFormat.Compact(upgDef.Cost)      // Normal: "$150"
                            : $"{upgDef.Cost.ToDouble()} Points";   // Special: "1 Points"

                    btn.Text = $"{upgDef.Name}\n{priceLabel}";

                    // Check Affordability based on the correct currency
                    bool canAfford = false;
                    if (upgDef.CostCurrency == CurrencyType.Coins)
                        canAfford = _state.Coins >= upgDef.Cost;
                    else
                        canAfford = _state.PrestigePoints >= upgDef.Cost;

                    bool isOwned = _state.HasUpgrade(upgDef.Id);
                    btn.IsActive = !isOwned && canAfford;

                    if (isOwned) btn.Text = "BOUGHT";
                }

                else if (btn.Tag is UpgradeSeriesDef series) {
                    // Dynamic Text Update (in case you buy it)
                    int lvl = _state.GetProceduralLevel(series.Id);
                    var cost = _state.GetProceduralCost(series.Id);

                    // Check Affordability
                    if (series.CostCurrency == CurrencyType.Coins)
                        btn.IsActive = _state.Coins >= cost;
                    else
                        btn.IsActive = _state.PrestigePoints >= cost;
                }
            }
        }

        private void HandleInput() {

            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();
                if (g.GestureType == GestureType.Tap) {
                    var p = new Point((int)g.Position.X, (int)g.Position.Y);
                    bool uiHit = false;

                    // 1. Check Toggle Button
                    if (_toggleButton.HitTest(p)) {
                        _toggleButton.TriggerFlash();
                        _toggleButton.OnClick?.Invoke();
                        uiHit = true;
                    }

                    else if(_prestigeButton.HitTest(p)) {
                        _prestigeButton.TriggerFlash();
                        _prestigeButton.OnClick?.Invoke();
                        uiHit = true;
                    }
                    else if (_buyMultButton.HitTest(p)) {
                        _buyMultButton.TriggerFlash();
                        _buyMultButton.OnClick?.Invoke();
                        uiHit = true;
                    }

                    // 2. Check Scroll List (Only if inside the list area)
                    else if (_listBounds.Contains(p)) {
                        var scrollPoint = new Point(p.X, p.Y + (int)_scrollY);

                        foreach (var btn in _genButtons) {
                            if (btn.HitTest(scrollPoint)) {
                                btn.TriggerFlash();
                                btn.OnClick?.Invoke();
                                uiHit = true;
                                break; // Stop checking other buttons
                            }
                        }
                    }

                    // 3. The "Tap Anywhere" Fallback
                    // If we didn't hit any UI, it's a gameplay tap!
                    if (!uiHit) {

                        //MULTI TOUCH
                        TouchCollection tc = TouchPanel.GetState();
                        foreach (TouchLocation tl in tc) {

                            if ((tl.State == TouchLocationState.Pressed) || (tl.State == TouchLocationState.Moved)) {
                                _state.Tap();
                                SpawnFloatingText(new Vector2(p.X, p.Y - 50), $"+{NumberFormat.Compact(_state.TapValue)}", Color.Lime);
                            }
                        }

                        _state.Tap();
                        // Spawn text exactly where the finger is
                        SpawnFloatingText(new Vector2(p.X, p.Y - 50), $"+{NumberFormat.Compact(_state.TapValue)}", Color.Lime);
                    }
                }
                else if (g.GestureType == GestureType.VerticalDrag) {
                    // ... (Keep existing scroll logic) ...
                    _scrollY -= g.Delta.Y;
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
            foreach (var p in _particles) {
                p.Draw(_spriteBatch, _font);
            }

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
            var presText = $"Prestige Points: {NumberFormat.Compact(_state.PrestigePoints)}";
            var multiText = $"Prestige Multiplier: {NumberFormat.Compact(_state.prestigeMult, 2)}";
            _prestigeButton.Draw(_spriteBatch, _font, _pixel);
            _spriteBatch.DrawString(_font, coinsText, new Vector2(50, 200), Color.White);
            _spriteBatch.DrawString(_font, cpsText, new Vector2(50, 240), Color.White);
            _spriteBatch.DrawString(_font, presText, new Vector2(50, 280), Color.Gold);
            _spriteBatch.DrawString(_font, multiText, new Vector2(50, 320), Color.Green);
            _toggleButton.Draw(_spriteBatch, _font, _pixel);
            _buyMultButton.Draw(_spriteBatch, _font, _pixel);
            //_tapButton.Draw(_spriteBatch, _font, _pixel);
            var pad = 50;
            var w = GraphicsDevice.Viewport.Width;
        }

        private void LayoutUI() {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;
            int pad = 20;

            // 1. Header Area
           // int headerHeight = 350; // Increased slightly for Toggle Button
            int tapY = 800;
            int btnHeight = 80;

            // Tap Button
            //_tapButton = new UiButton(new Rectangle(pad, tapY, w - pad * 2, btnHeight), "TAP", () => _state.Tap());
            _prestigeButton = new UiButton(new Rectangle(pad, 100, w - pad * 2, 100), "", () => {
                _state.DoPrestige();
                _needsLayout = true; // Rebuild list (it's empty now!)
            });

            // Toggle Button (New)
            _toggleButton = new UiButton(new Rectangle(pad, tapY, w - pad * 2, btnHeight), "VIEW: GENERATORS", () => {
                // Swap Mode
                _viewMode = (_viewMode == ViewMode.Generators) ? ViewMode.Upgrades : ViewMode.Generators;
                _needsLayout = true; // Rebuild list
            });

            _buyMultButton = new UiButton(new Rectangle(pad + w/2 + pad, 250, w/2, 60), "BUY: 1x", () => {
                // Toggle Logic: 1 -> 10 -> 100 -> Max -> 1
                if (_state.BuyAmount == 1) _state.BuyAmount = 10;
                else if (_state.BuyAmount == 10) _state.BuyAmount = 100;
                else if (_state.BuyAmount == 100) _state.BuyAmount = -1; // Max
                else _state.BuyAmount = 1;

                _needsLayout = true; // Refresh text
            });


            // 2. List Area
            int listStartY = tapY + btnHeight + pad;
            _listBounds = new Rectangle(0, listStartY, w, h - listStartY);

            // 3. Generate List Content based on ViewMode
            _genButtons.Clear();
            int currentY = listStartY;
            int btnH = 100;

            if (_viewMode == ViewMode.Generators) {
                // Show Generators
                foreach (var def in GameData.Generators) {
                    string id = def.Id;
                    var btn = new UiButton(new Rectangle(pad, currentY, w - pad * 2, btnH), def.Name, () => _state.TryBuyGenerator(id));
                    btn.Tag = def;
                    _genButtons.Add(btn);
                    currentY += btnH + pad;
                }
            }
            else {
                // Show Upgrades
                foreach (var def in GameData.Upgrades) {
                    // Don't show if already bought!
                    if (_state.HasUpgrade(def.Id)) continue;

                    string id = def.Id;
                    // Add description to button text
                    string text = $"{def.Name}\n{def.Description}";

                    var btn = new UiButton(new Rectangle(pad, currentY, w - pad * 2, btnH), text, () => {
                        if (_state.TryBuyUpgrade(id)) {
                            _needsLayout = true; // Rebuild list to remove bought item
                        }
                    });
                    btn.Tag = def;
                    _genButtons.Add(btn);
                    currentY += btnH + pad;
                }

                // 2. NEW: Infinite Series Upgrades
    foreach (var series in GameData.UpgradeSeries) {
        // Create a dynamic button for the *Next* level
        string id = series.Id;
        int currentLevel = _state.GetProceduralLevel(id);
        int nextLevel = currentLevel + 1;
        var cost = _state.GetProceduralCost(id);

        string name = string.Format(series.NameFormat, nextLevel);
        string desc = $"(x{series.MultiplierPerLevel} effect)";
        
        // Currency formatting
        string price = series.CostCurrency == CurrencyType.Coins 
            ? NumberFormat.Compact(cost) 
            : $"{cost} Pts";

        string text = $"{name} - {desc}\n{price}";

        var btn = new UiButton(new Rectangle(pad, currentY, w - pad * 2, btnH), text, () => {
             // Use the new Buy Method
             if (_state.TryBuyProceduralUpgrade(id)) {
                 _needsLayout = true; // Rebuild to update cost/level text
             }
        });

        // Store series in tag for the Update loop
        btn.Tag = series; 
        
        _genButtons.Add(btn);
        currentY += btnH + pad;
    }
            }

            _maxScroll = Math.Max(0, currentY - h + pad);

            // Update Toggle Text
            _toggleButton.Text = _viewMode == ViewMode.Generators ? "SHOW UPGRADES" : "SHOW GENERATORS";
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