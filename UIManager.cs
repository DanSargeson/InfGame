using Android.Nfc.Tech;
using Android.Widget;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;

namespace InfGame
{
    internal class UIManager
    {
        private int _itemHeight = 100;
        private int _itemPadding = 20;
        private int _listStartY = 0;

        private List<FloatingText> _particles = new();
        private double _accumulator = 0.0;

        public bool _showWelcomeModal { get; set; } = false;
        public BigDouble _offlineEarnings { get; set; } = BigDouble.Zero;
        public string _offlineTimeText { get; set; } = "";
        public UiButton _collectButton { get; set; }

        // Scroll State
        private float _scrollY = 0;
        private float _maxScroll = 0;
        private Rectangle _listBounds;
        private bool _needsLayout = true;

        private GameState _state;
        private readonly GraphicsDevice _graphicsDevice;

        // UI Components
        private ViewMode _viewMode = ViewMode.Generators;
        private UiButton _toggleButton;
        private UiButton _buyMultButton;

        // REMOVED: private UiButton _prestigeShop; // We don't need this anymore

        private List<UiButton> _genButtons = new();
        private Stack<UiButton> _buttonPool = new Stack<UiButton>();
        private UiButton _prestigeButton;

        private RasterizerState _scissorState = new RasterizerState { ScissorTestEnable = true };

        public UIManager(GameState state, GraphicsDevice graphicsDevice) {
            _state = state;
            _needsLayout = true;
            _graphicsDevice = graphicsDevice;
        }

        public UiButton GetPooledButton(Rectangle bounds, string text, Action onClick) {
            UiButton btn;
            if (_buttonPool.Count > 0) {
                btn = _buttonPool.Pop();
                btn.Configure(bounds, text, onClick);
            }
            else {
                btn = new UiButton(bounds, text, onClick);
            }
            return btn;
        }

        public void ReturnToPool(UiButton btn) {
            if (btn != null) _buttonPool.Push(btn);
        }

        public void Update(GameTime gameTime) {
            var dt = gameTime.ElapsedGameTime.TotalSeconds;
            _accumulator += dt;

            var tickRate = _state.TickDuration;
            if (_accumulator > 1.0) _accumulator = 1.0;

            while (_accumulator >= tickRate) {
                _state.Tick();
                _accumulator -= tickRate;
            }

            // --- FIX: Update all standalone buttons so they flash correctly ---
            _prestigeButton?.Update(dt);
            _toggleButton?.Update(dt);
            _buyMultButton?.Update(dt);

            // Update List Buttons
            UpdateGeneratorButtons(dt);

            if (_needsLayout) {
                LayoutUI();
                _needsLayout = false;
            }

            HandleInput();

            // Logic for Prestige Button Text
            var potentialGain = _state.CalculateRebirthGain();
            if (potentialGain > 0) {
                _prestigeButton.Text = $"REBIRTH: +{NumberFormat.Compact(potentialGain)} POINTS\n(+{potentialGain.ToDouble() * 10}% Bonus)";
                _prestigeButton.IsActive = true;
            }
            else {
                _prestigeButton.Text = "Rebirth Locked (1M Souls)";
                _prestigeButton.IsActive = false;
            }

            // Update Particles
            for (int i = _particles.Count - 1; i >= 0; i--) {
                _particles[i].Update(dt);
                if (!_particles[i].IsActive) {
                    _particles.RemoveAt(i);
                }
            }
        }

        private void DrawHeader(Texture2D _pixel, SpriteBatch _spriteBatch, SpriteFont _font) {

            var currentCps = _state.SoulsPerSecond * _state.TimeScale;
            var soulsText = $"Souls: {NumberFormat.Compact(_state.Souls)}";
            var cpsText = $"Per Sec: {NumberFormat.Compact(currentCps, 2)}";
            var presText = $"Rebirth Pts: {NumberFormat.Compact(_state.RebirthPoints)}";
            var multiText = $"Multiplier: {NumberFormat.Compact(_state.prestigeMult, 2)}x";

            var corruptionPct = (_state._Corruption * 100).ToString("F1");
            var speedPct = (_state.TimeScale * 100).ToString("F1");
            var bonusPct = ((_state.CorruptionBonus - 1.0) * 100).ToString("F0");

            string status = $"Integrity: {speedPct}% (Corruption: {corruptionPct}%)";
            string bonus = $"Rebirth Bonus: +{bonusPct}%";

            _prestigeButton.Draw(_spriteBatch, _font, _pixel);

            // Draw Stats
            _spriteBatch.DrawString(_font, soulsText, new Vector2(50, 200), Color.White);
            _spriteBatch.DrawString(_font, cpsText, new Vector2(50, 240), Color.White);
            _spriteBatch.DrawString(_font, presText, new Vector2(50, 280), Color.Gold);
            _spriteBatch.DrawString(_font, multiText, new Vector2(50, 320), Color.Green);


            Color colour = Color.White;
            if (_state._Corruption > 0.5) colour = Color.Yellow;
            if (_state._Corruption > 0.65) colour = Color.Orange;
            if (_state._Corruption > 0.80) colour = Color.OrangeRed;
            if (_state._Corruption > 0.90) colour = Color.Red;

            // Draw these strings in Red or Purple to look "Corrupted"
            _spriteBatch.DrawString(_font, status, new Vector2(50, 360), colour);
            _spriteBatch.DrawString(_font, bonus, new Vector2(50, 400), Color.Plum);

            _toggleButton.Draw(_spriteBatch, _font, _pixel);
            _buyMultButton.Draw(_spriteBatch, _font, _pixel);
        }

        public void Draw(Texture2D _pixel, SpriteBatch _spriteBatch, SpriteFont _font) {
            _spriteBatch.Begin();
            DrawHeader(_pixel, _spriteBatch, _font);
            foreach (var p in _particles) p.Draw(_spriteBatch, _font);
            _spriteBatch.End();

            _spriteBatch.Begin(rasterizerState: _scissorState);
            _graphicsDevice.ScissorRectangle = _listBounds;

            foreach (var btn in _genButtons) {
                if (btn.Bounds.Bottom - _scrollY < _listBounds.Top) continue;
                if (btn.Bounds.Top - _scrollY > _listBounds.Bottom) continue;
                btn.Draw(_spriteBatch, _font, _pixel, (int)_scrollY);
            }
            _spriteBatch.End();

            if (_showWelcomeModal) {
                _spriteBatch.Begin();

                var w = _graphicsDevice.Viewport.Width;
                var h = _graphicsDevice.Viewport.Height;

                // A. Dim Background (Full Screen)
                // Uses your existing 1x1 pixel stretched to screen size with alpha
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), Color.Black * 0.65f);

                // B. Modal Box (Center)
                var boxRect = new Rectangle(w / 2 - 200, h / 2 - 150, 400, 350);
                _spriteBatch.Draw(_pixel, boxRect, Color.DarkSlateGray);

                // C. Text
                // Helper to center text
                DrawCenteredString("WELCOME BACK!", h / 2 - 80, Color.Gold, _font, _spriteBatch);
                DrawCenteredString(_offlineTimeText, h / 2 - 40, Color.White, _font, _spriteBatch);
                DrawCenteredString($"+{NumberFormat.Compact(_offlineEarnings)}", h / 2, Color.LimeGreen, _font, _spriteBatch);

                // D. Button
                _collectButton.Draw(_spriteBatch, _font, _pixel);

                _spriteBatch.End();
            }
        }

        private void DrawCenteredString(string text, int y, Color color, SpriteFont _font, SpriteBatch _spriteBatch) {
            var size = _font.MeasureString(text);
            var x = (_graphicsDevice.Viewport.Width - size.X) / 2;
            _spriteBatch.DrawString(_font, text, new Vector2(x, y), color);
        }

        private void UpdateGeneratorButtons(double dt) {
            foreach (var btn in _genButtons) {
                btn.Update(dt);

                // CASE 1: Generators
                if (btn.Tag is GeneratorDef genDef) {
                    int amount = _state.BuyAmount;
                    string prefix = $"x{amount}";

                    if (amount == -1) {
                        amount = _state.GetMaxBuyable(genDef.Id);
                        if (amount == 0) { amount = 1; prefix = "Max"; }
                        else { prefix = $"x{amount}"; }
                    }

                    var totalCost = _state.GetBulkCost(genDef.Id, amount);
                    var currentCount = _state.GetCount(genDef.Id);

                    btn.Text = $"{genDef.Name} ({currentCount})\n{prefix}: {NumberFormat.Compact(totalCost)}";
                    btn.IsActive = _state.Souls >= totalCost;

                    string label = _state.BuyAmount == -1 ? "Max" : $"{_state.BuyAmount}x";
                    _buyMultButton.Text = $"BUY: {label}";
                }
                // CASE 2: Upgrades & Auto-Buyers
                else if (btn.Tag is UpgradeDef upgDef) {

                    // --- FIX START: Handle Auto-Buyers Separately ---
                    if (upgDef.Type == UpgradeType.AutoBuyGenerator) {
                        // If we own it, it's a Toggle, not a "Bought" label
                        if (_state.HasUpgrade(upgDef.Id)) {
                            string status = _state.IsAutoBuyerActive(upgDef.Id) ? "ON" : "OFF";
                            btn.Text = $"{upgDef.Name} (Auto)\nStatus: {status}";
                            btn.IsActive = true; // Always clickable so we can toggle it!
                            continue; // Skip the standard logic below
                        }
                    }
                    // --- FIX END ---

                    string priceLabel = upgDef.CostCurrency == CurrencyType.Souls
                                ? NumberFormat.Compact(upgDef.Cost)
                            : $"{upgDef.Cost.ToDouble()} RP";

                    btn.Text = $"{upgDef.Name}\n{priceLabel}";

                    bool canAfford = false;
                    if (upgDef.CostCurrency == CurrencyType.Souls)
                        canAfford = _state.Souls >= upgDef.Cost;
                    else
                        canAfford = _state.RebirthPoints >= upgDef.Cost;

                    bool isOwned = _state.HasUpgrade(upgDef.Id);

                    // Standard upgrades become inactive/gray when bought
                    btn.IsActive = !isOwned && canAfford;

                    if (isOwned) btn.Text = "BOUGHT";
                }
                // CASE 3: Infinite Series
                else if (btn.Tag is UpgradeSeriesDef series) {
                    // (Your existing series logic is fine)
                    var cost = _state.GetProceduralCost(series.Id);
                    if (series.CostCurrency == CurrencyType.Souls)
                        btn.IsActive = _state.Souls >= cost;
                    else
                        btn.IsActive = _state.RebirthPoints >= cost;
                }
            }
        }

        private void LayoutUI() {
            // ... (Pooling and Setup code remains the same) ...
            ReturnToPool(_toggleButton);
            ReturnToPool(_prestigeButton);
            ReturnToPool(_buyMultButton);
            foreach (var btn in _genButtons) ReturnToPool(btn);
            _genButtons.Clear();

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int pad = 20;
            int tapY = h / 2;
            int btnHeight = 100;

            _itemHeight = btnHeight;
            _itemPadding = pad;

            // ... (Button Creation for Rebirth, BuyMult, Toggle remains the same) ...

            // 1. Rebirth Button
            _prestigeButton = GetPooledButton(new Rectangle(pad, 100, w - pad * 2, 100), "", () => {
                _state.DoRebirth();
                _needsLayout = true;
            });

            // 2. Buy Multiplier Button
            _buyMultButton = GetPooledButton(new Rectangle(pad + w / 2 + pad, 250, w / 2, 60), "BUY: 1x", () => {
                if (_state.BuyAmount == 1) _state.BuyAmount = 10;
                else if (_state.BuyAmount == 10) _state.BuyAmount = 100;
                else if (_state.BuyAmount == 100) _state.BuyAmount = -1;
                else _state.BuyAmount = 1;
                _needsLayout = true;
            });

            // 3. View Toggle Button
            _toggleButton = GetPooledButton(new Rectangle(pad, tapY, w - pad * 2, btnHeight), "", () => {
                if (_viewMode == ViewMode.Generators) _viewMode = ViewMode.Upgrades;
                else if (_viewMode == ViewMode.Upgrades) _viewMode = ViewMode.RebirthShop;
                else _viewMode = ViewMode.Generators;
                _needsLayout = true;
            });

            // Set Toggle Text
            string viewName = "GENERATORS";
            if (_viewMode == ViewMode.Upgrades) viewName = "UPGRADES";
            if (_viewMode == ViewMode.RebirthShop) viewName = "REBIRTH SHOP";
            _toggleButton.Text = $"VIEW: {viewName}";

            // 4. Generate List Content
            int listStartY = tapY + btnHeight + pad;
            _listStartY = listStartY;
            _listBounds = new Rectangle(0, listStartY, w, h - listStartY);
            int currentY = listStartY;

            if (_viewMode == ViewMode.Generators) {
                // --- Generators ---
                foreach (var def in GameData.Generators) {
                    string id = def.Id;
                    var btn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), def.Name, () => _state.TryBuyGenerator(id));
                    btn.Tag = def;
                    _genButtons.Add(btn);
                    currentY += btnHeight + pad;
                }
            }
            else {
                // --- Upgrades (Both Normal & Shop) ---
                var targetCurrency = (_viewMode == ViewMode.Upgrades) ? CurrencyType.Souls : CurrencyType.RebirthPoints;

                // A. Single Upgrades & Auto-Buyers
                foreach (var def in GameData.Upgrades) {
                    if (def.CostCurrency != targetCurrency) continue;

                    // 1. Handle OWNED items
                    if (_state.HasUpgrade(def.Id)) {
                        // If it is an Auto-Buyer, we switch to "Toggle Mode"
                        if (def.Type == UpgradeType.AutoBuyGenerator) {
                            string status = _state.IsAutoBuyerActive(def.Id) ? "ON" : "OFF";
                            string t = $"{def.Name} (Auto)\nStatus: {status}";

                            var btn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), t, () => {
                                _state.ToggleAutoBuyer(def.Id);
                                _needsLayout = true;
                            });

                            btn.Tag = def;
                            _genButtons.Add(btn);
                            currentY += btnHeight + pad;
                        }

                        // CRITICAL FIX: If we own it, we stop here for this item.
                        // We do NOT want to fall through and draw a "Buy" button below.
                        continue;
                    }

                    // 2. Handle UNOWNED items (The "Buy" Button)
                    // This is now outside the 'if (HasUpgrade)' block, so it actually runs!
                    string id = def.Id;
                    string text = $"{def.Name}\n{def.Description}";

                    var buyBtn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), text, () => {
                        if (_state.TryBuyUpgrade(id)) _needsLayout = true;
                    });

                    buyBtn.Tag = def;
                    _genButtons.Add(buyBtn);
                    currentY += btnHeight + pad;
                }
                // CRITICAL FIX: The loop for Upgrades ends HERE. 
                // The Series loop must be OUTSIDE of it.

                // B. Infinite Series Upgrades
                foreach (var series in GameData.UpgradeSeries) {
                    if (series.CostCurrency != targetCurrency) continue;

                    string id = series.Id;
                    int currentLevel = _state.GetProceduralLevel(id);
                    int nextLevel = currentLevel + 1;
                    var cost = _state.GetProceduralCost(id);

                    string name = string.Format(series.NameFormat, nextLevel);
                    string desc = $"(x{series.MultiplierPerLevel} effect)";
                    string price = series.CostCurrency == CurrencyType.Souls ? NumberFormat.Compact(cost) : $"{cost} RP";

                    string text = $"{name} - {desc}\n{price}";
                    var btn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), text, () => {
                        if (_state.TryBuyProceduralUpgrade(id)) _needsLayout = true;
                    });
                    btn.Tag = series;
                    _genButtons.Add(btn);
                    currentY += btnHeight + pad;
                }

                _maxScroll = Math.Max(0, currentY - h + pad);
            }
        }

        private void SpawnFloatingText(Vector2 pos, string text, Color color) {
            var rnd = new Random();
            float xOffset = rnd.Next(-20, 21);
            _particles.Add(new FloatingText(new Vector2(pos.X + xOffset, pos.Y), text, color));
        }

        private void HandleInput() {
            while (TouchPanel.IsGestureAvailable) {
                var g = TouchPanel.ReadGesture();

                if (_showWelcomeModal) {
                    if (g.GestureType == GestureType.Tap) {
                        var p = new Point((int)g.Position.X, (int)g.Position.Y);
                        // Only allow clicking the Collect button
                        if (_collectButton != null && _collectButton.HitTest(p)) {
                            _collectButton.TriggerFlash();
                            _collectButton.OnClick?.Invoke();
                        }
                    }
                    continue; // SKIP everything else
                }

                if (g.GestureType == GestureType.Tap) {
                    var p = new Point((int)g.Position.X, (int)g.Position.Y);
                    //bool uiHit = false;

                    // Check Header Buttons
                    if (_toggleButton.HitTest(p)) {
                        _toggleButton.TriggerFlash();
                        _toggleButton.OnClick?.Invoke();
                        // uiHit = true;
                    }
                    else if (_prestigeButton.HitTest(p)) {
                        _prestigeButton.TriggerFlash();
                        _prestigeButton.OnClick?.Invoke();
                        //uiHit = true;
                    }
                    else if (_buyMultButton.HitTest(p)) {
                        _buyMultButton.TriggerFlash();
                        _buyMultButton.OnClick?.Invoke();
                        //uiHit = true;
                    }
                    // Check Scroll List
                    else if (_listBounds.Contains(p)) {
                        float relativeY = (p.Y - _listStartY) + _scrollY;
                        int totalItemHeight = _itemHeight + _itemPadding;
                        int index = (int)(relativeY / totalItemHeight);

                        if (index >= 0 && index < _genButtons.Count) {
                            var btn = _genButtons[index];
                            var scrollPoint = new Point(p.X, p.Y + (int)_scrollY);
                            if (btn.HitTest(scrollPoint)) {
                                btn.TriggerFlash();
                                btn.OnClick?.Invoke();
                                //      uiHit = true;
                            }
                        }
                    }
                }
                else if (g.GestureType == GestureType.VerticalDrag) {
                    _scrollY -= g.Delta.Y;
                    if (_scrollY < 0) _scrollY = 0;
                    if (_scrollY > _maxScroll) _scrollY = _maxScroll;
                }
            }


            //Tap anywhere logic
            var touchstate = TouchPanel.GetState();
            foreach (var touch in touchstate) {

                if (touch.State == TouchLocationState.Pressed) {
                    if (touch.State == TouchLocationState.Pressed) {
                        var p = new Point((int)touch.Position.X, (int)touch.Position.Y);

                        // 1. Did we hit a specific UI Button?
                        bool hitButton = false;

                        // Check Header Buttons
                        if (_prestigeButton.HitTest(p) || _toggleButton.HitTest(p) || _buyMultButton.HitTest(p))
                            hitButton = true;

                        // Check List Buttons (Manual Calculation)
                        if (_listBounds.Contains(p)) {
                            float relativeY = (p.Y - _listStartY) + _scrollY;
                            int index = (int)(relativeY / (_itemHeight + _itemPadding));
                            if (index >= 0 && index < _genButtons.Count) {
                                // We clicked a valid row... but did we hit the button?
                                // (Since buttons are full width, yes, basically)
                                hitButton = true;
                            }
                        }

                        // 2. If we didn't hit a button, it is a Gameplay Tap!
                        // (Even if we clicked the empty background of the list)
                        if (!hitButton) {
                            _state.Tap();
                            SpawnFloatingText(new Vector2(p.X, p.Y - 50), $"+{NumberFormat.Compact(_state.TapValue)}", Color.Lime);
                        }
                    }
                }
            }
        }
    }
}