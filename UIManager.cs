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

        private double _uiUpdateTimer = 0.0;

        private GameState _state;
        private GameSimulator _sim;
        private readonly GraphicsDevice _graphicsDevice;

        // UI Components
        private ViewMode _viewMode = ViewMode.Generators;

        // REPLACED: _toggleButton with a List
        private List<UiButton> _navButtons = new();
        private UiButton _buyMultButton;
        private UiButton _prestigeButton;

        private List<UiButton> _genButtons = new();
        private Stack<UiButton> _buttonPool = new Stack<UiButton>();

        private Stack<FloatingText> _particlePool = new Stack<FloatingText>();

        private RasterizerState _scissorState = new RasterizerState { ScissorTestEnable = true };

        public UIManager(GameState state, GameSimulator sim, InputManager input, GraphicsDevice graphicsDevice) {
            _state = state;
            _sim = sim;
            _needsLayout = true;
            _graphicsDevice = graphicsDevice;
            input.OnTap += HandleTap;
            input.OnVerticalScroll += HandleScroll;
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

        public void ReturnToPool(FloatingText particle) {
            if (particle != null) _particlePool.Push(particle);
        }

       

        private void DrawHeader(Texture2D _pixel, SpriteBatch _spriteBatch, SpriteFont _font) {
            var currentCps = _state.SoulsPerSecond * _state.TimeScale;
            var soulsText = $"Souls: {NumberFormat.Compact(_state.Souls)}";
            var cpsText = $"Per Sec: {NumberFormat.Compact(currentCps, 2)}";
            var presText = $"Rebirth Pts: {NumberFormat.Compact(_state.RebirthPoints)}";
            var multiText = $"Multiplier: {NumberFormat.Compact(_state.prestigeMult, 2)}x";

            // Corruption Text
            var corruptionPct = (_state.Corruption * 100).ToString("F1"); // Fixed access to Property
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

            // Corruption Colors
            Color colour = Color.White;
            if (_state.Corruption > 0.5) colour = Color.Yellow;
            if (_state.Corruption > 0.65) colour = Color.Orange;
            if (_state.Corruption > 0.80) colour = Color.OrangeRed;
            if (_state.Corruption > 0.90) colour = Color.Red;

            _spriteBatch.DrawString(_font, status, new Vector2(50, 360), colour);
            _spriteBatch.DrawString(_font, bonus, new Vector2(50, 400), Color.Plum);

            // Draw Nav Buttons
            foreach (var btn in _navButtons) btn.Draw(_spriteBatch, _font, _pixel);

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

                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), Color.Black * 0.65f);
                var boxRect = new Rectangle(w / 2 - 200, h / 2 - 150, 400, 350);
                _spriteBatch.Draw(_pixel, boxRect, Color.DarkSlateGray);

                DrawCenteredString("WELCOME BACK!", h / 2 - 80, Color.Gold, _font, _spriteBatch);
                DrawCenteredString(_offlineTimeText, h / 2 - 40, Color.White, _font, _spriteBatch);
                DrawCenteredString($"+{NumberFormat.Compact(_offlineEarnings)}", h / 2, Color.LimeGreen, _font, _spriteBatch);

                if(_collectButton != null) {

                    _collectButton.Draw(_spriteBatch, _font, _pixel);
                }
                _spriteBatch.End();
            }
        }

        private void DrawCenteredString(string text, int y, Color color, SpriteFont _font, SpriteBatch _spriteBatch) {
            var size = _font.MeasureString(text);
            var x = (_graphicsDevice.Viewport.Width - size.X) / 2;
            _spriteBatch.DrawString(_font, text, new Vector2(x, y), color);
        }

        public void Update(GameTime gameTime) {
            var dt = gameTime.ElapsedGameTime.TotalSeconds;
            _accumulator += dt;

            // --- 1. ANIMATIONS (Run every frame for smooth 60 FPS) ---
            _prestigeButton?.Update(dt);
            _buyMultButton?.Update(dt);
            _collectButton?.Update(dt);

            foreach (var btn in _navButtons) btn.Update(dt);

            // Smooth flash animation for list buttons
            foreach (var btn in _genButtons) btn.Update(dt);

            // --- 2. LAYOUT & STATE (Handle changes) ---
            if (_needsLayout) {
                LayoutUI();
                _needsLayout = false;

                // CRITICAL FIX: Force an immediate state update.
                // This prevents the "Flash" where buttons briefly look active/default
                // before the next throttled update catches them.
                UpdateGeneratorButtons(0);
            }

            // --- 3. TEXT/DATA UPDATE (Throttled to 10 FPS) ---
            _uiUpdateTimer += dt;
            if (_uiUpdateTimer > 0.1) {
                _uiUpdateTimer = 0.0;
                UpdateGeneratorButtons(dt);
            }

          //  HandleInput();

            // --- 4. PARTICLES ---
            for (int i = _particles.Count - 1; i >= 0; i--) {
                _particles[i].Update(dt);
                if (!_particles[i].IsActive) {
                    ReturnToPool(_particles[i]);
                    _particles.RemoveAt(i);
                }
            }
        }

        private void UpdateGeneratorButtons(double dt) {
            // A. Update Prestige Button (Moved here so it updates reliably)
            var potentialGain = _sim.CalculateRebirthGain();
            if (_prestigeButton != null) {
                if (potentialGain > 0) {
                    _prestigeButton.Text = $"REBIRTH: +{NumberFormat.Compact(potentialGain)} PTS\n(+{potentialGain.ToDouble() * 10}% Bonus)";
                    _prestigeButton.IsActive = true;
                }
                else {
                    _prestigeButton.Text = "Rebirth Locked (1M Souls)";
                    _prestigeButton.IsActive = false;
                }
            }

            // B. Update Multi-Buy Button
            if (_buyMultButton != null) {
                string label = _state.BuyAmount == -1 ? "Max" : $"{_state.BuyAmount}x";
                _buyMultButton.Text = $"BUY: {label}";
            }

            // C. Update List Buttons
            foreach (var btn in _genButtons) {
                // Note: btn.Update(dt) is intentionally NOT called here (done in main Update)

                if (btn.Tag is GeneratorDef genDef) {
                    int amount = _state.BuyAmount;
                    string prefix = $"x{amount}";
                    if (amount == -1) {
                        amount = _sim.GetMaxBuyable(genDef.Id);
                        if (amount == 0) { amount = 1; prefix = "Max"; }
                        else { prefix = $"x{amount}"; }
                    }

                    var totalCost = _sim.GetBulkCost(genDef.Id, amount);
                    var currentCount = _state.GetCount(genDef.Id);

                    btn.Text = $"{genDef.Name} ({currentCount})\n{prefix}: {NumberFormat.Compact(totalCost)}";
                    btn.IsActive = _state.Souls >= totalCost;
                }
                else if (btn.Tag is UpgradeDef upgDef) {
                    if (upgDef.Type == UpgradeType.AutoBuyGenerator) {
                        if (_state.HasUpgrade(upgDef.Id)) {
                            string status = _state.IsAutoBuyerActive(upgDef.Id) ? "ON" : "OFF";
                            btn.Text = $"{upgDef.Name} (Auto)\nStatus: {status}";
                            btn.IsActive = true;
                            continue;
                        }
                    }

                    string priceLabel = upgDef.CostCurrency == CurrencyType.Souls
                        ? NumberFormat.Compact(upgDef.Cost)
                        : $"{upgDef.Cost.ToDouble()} RP";

                    btn.Text = $"{upgDef.Name}\n{priceLabel}";

                    bool canAfford = (upgDef.CostCurrency == CurrencyType.Souls)
                        ? _state.Souls >= upgDef.Cost
                        : _state.RebirthPoints >= upgDef.Cost;

                    bool isOwned = _state.HasUpgrade(upgDef.Id);
                    btn.IsActive = !isOwned && canAfford;
                    if (isOwned) btn.Text = "BOUGHT";
                }
                else if (btn.Tag is UpgradeSeriesDef series) {
                    var cost = _sim.GetProceduralCost(series.Id);
                    bool canAfford = (series.CostCurrency == CurrencyType.Souls)
                        ? _state.Souls >= cost
                        : _state.RebirthPoints >= cost;
                    btn.IsActive = canAfford;
                }
            }
        }

        private void LayoutUI() {
            // Cleanup
            foreach (var btn in _navButtons) ReturnToPool(btn);
            _navButtons.Clear();
            ReturnToPool(_prestigeButton);
            ReturnToPool(_buyMultButton);
            foreach (var btn in _genButtons) ReturnToPool(btn);
            _genButtons.Clear();

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int pad = 20;
            int navY = h / 2; // Place nav bar where Toggle used to be
            int navHeight = 80;


            int modalCenterY = h / 2;
            int btnW = 200;
            int btnH = 60;

            _collectButton = GetPooledButton(
                new Rectangle(w / 2 - btnW / 2, modalCenterY + 80, btnW, btnH),
                "COLLECT",
                () => {
                    _showWelcomeModal = false; // Close modal
                                               // You might want to trigger a save here just in case
                }
            );

            // --- 1. NAVIGATION BAR ---
            string[] navNames = { "GEN", "UPG", "AUTO", "SHOP", "SET" };
            ViewMode[] navModes = { ViewMode.Generators, ViewMode.Upgrades, ViewMode.AutoBuyers, ViewMode.RebirthShop, ViewMode.Settings };
            int navWidth = (w - (pad * 6)) / 5;

            for (int i = 0; i < 5; i++) {
                int index = i;
                int x = pad + (i * (navWidth + pad));
                var mode = navModes[i];
                string label = navNames[i];
                if (_viewMode == mode) label = $"[{label}]";

                var btn = GetPooledButton(new Rectangle(x, navY, navWidth, navHeight), label, () => {
                    _viewMode = mode;
                    _needsLayout = true;
                });
                _navButtons.Add(btn);
            }

            // --- 2. HEADER BUTTONS ---
            _prestigeButton = GetPooledButton(new Rectangle(pad, 100, w - pad * 2, 100), "", () => {
                _sim.DoRebirth();
                _needsLayout = true;
            });

            _buyMultButton = GetPooledButton(new Rectangle(pad + w / 2 + pad, 250, w / 2, 60), "BUY: 1x", () => {
                if (_state.BuyAmount == 1) _state.BuyAmount = 10;
                else if (_state.BuyAmount == 10) _state.BuyAmount = 100;
                else if (_state.BuyAmount == 100) _state.BuyAmount = -1;
                else _state.BuyAmount = 1;
                _needsLayout = true;
            });

            // --- 3. GENERATE LIST ---
            int btnHeight = 100;
            _itemHeight = btnHeight;
            _itemPadding = pad;
            int listStartY = navY + navHeight + pad;
            _listStartY = listStartY;
            _listBounds = new Rectangle(0, listStartY, w, h - listStartY);
            int currentY = listStartY;

            if (_viewMode == ViewMode.Generators) {
                foreach (var def in GameData.Generators) {
                    string id = def.Id;
                    var btn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), def.Name, () => _sim.TryBuyGenerator(id));
                    btn.Tag = def;
                    _genButtons.Add(btn);
                    currentY += btnHeight + pad;
                }
            }
            else if (_viewMode == ViewMode.Settings) {
                // HARD RESET BUTTON
                var resetBtn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), "HARD RESET (Wipe Save)", () => {
                    // _state.HardReset(); // Implement this in GameState!
                    // For now, just a placeholder action
                    System.Diagnostics.Debug.WriteLine("Hard Reset Clicked");
                });
                _genButtons.Add(resetBtn);
                currentY += btnHeight + pad;

                // EXPORT BUTTON
                var exportBtn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), "EXPORT SAVE (Log to Debug)", () => {
                    // System.Diagnostics.Debug.WriteLine(_state.GetSaveString());
                });
                _genButtons.Add(exportBtn);
            }
            else {
                // Determine view logic (Upgrades, Auto, or Shop)
                var targetCurrency = (_viewMode == ViewMode.RebirthShop) ? CurrencyType.RebirthPoints : CurrencyType.Souls;
                bool showAutoBuyers = (_viewMode == ViewMode.AutoBuyers);
                bool showUpgrades = (_viewMode == ViewMode.Upgrades);

                // A. Single Upgrades & Auto-Buyers Loop
                foreach (var def in GameData.Upgrades) {
                    // Filter: Currency
                    if (_viewMode == ViewMode.RebirthShop && def.CostCurrency != CurrencyType.RebirthPoints) continue;
                    if ((showUpgrades || showAutoBuyers) && def.CostCurrency != CurrencyType.Souls) continue;

                    // Filter: AutoBuyer vs Normal Upgrade
                    bool isAutoBuyer = (def.Type == UpgradeType.AutoBuyGenerator);
                    if (showAutoBuyers && !isAutoBuyer) continue; // Auto tab only shows auto
                    if (showUpgrades && isAutoBuyer) continue;    // Upgrade tab hides auto

                    // 1. Handle OWNED items
                    if (_state.HasUpgrade(def.Id)) {
                        if (isAutoBuyer) {
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
                        continue; // Skip owned normal upgrades
                    }

                    // 2. Handle UNOWNED items
                    string id = def.Id;
                    string text = $"{def.Name}\n{def.Description}";
                    var buyBtn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), text, () => {
                        if (_sim.TryBuyUpgrade(id)) _needsLayout = true;
                    });
                    buyBtn.Tag = def;
                    _genButtons.Add(buyBtn);
                    currentY += btnHeight + pad;
                }

                // B. Infinite Series Upgrades (Only in Upgrades or Shop)
                if (!showAutoBuyers) {
                    foreach (var series in GameData.UpgradeSeries) {
                        if (series.CostCurrency != targetCurrency) continue;

                        string id = series.Id;
                        int currentLevel = _sim.GetProceduralLevel(id);
                        int nextLevel = currentLevel + 1;
                        var cost = _sim.GetProceduralCost(id);

                        string name = string.Format(series.NameFormat, nextLevel);
                        string desc = $"(x{series.MultiplierPerLevel} effect)";
                        string price = series.CostCurrency == CurrencyType.Souls ? NumberFormat.Compact(cost) : $"{cost} RP";

                        string text = $"{name} - {desc}\n{price}";
                        var btn = GetPooledButton(new Rectangle(pad, currentY, w - pad * 2, btnHeight), text, () => {
                            if (_sim.TryBuyProceduralUpgrade(id)) _needsLayout = true;
                        });
                        btn.Tag = series;
                        _genButtons.Add(btn);
                        currentY += btnHeight + pad;
                    }
                }
            }

            _maxScroll = Math.Max(0, currentY - h + pad);
        }

        public void SpawnFloatingText(Vector2 pos, string text, Color color) {
            FloatingText ft;
            if (_particlePool.Count > 0) {
                ft = _particlePool.Pop();
                ft.Reset(pos, text, color);
            }
            else {
                ft = new FloatingText(pos, text, color);
            }
            _particles.Add(ft);
        }

        private void HandleTap(Point p) {
            // 1. Modal Check
            if (_showWelcomeModal) {
                if (_collectButton != null && _collectButton.HitTest(p)) {
                    _collectButton.TriggerFlash();
                    _collectButton.OnClick?.Invoke();
                }
                return; // Block other input
            }

            // 2. Header Buttons
            if (_prestigeButton.HitTest(p)) {
                _prestigeButton.TriggerFlash();
                _prestigeButton.OnClick?.Invoke();
                return;
            }
            else if (_buyMultButton.HitTest(p)) {
                _buyMultButton.TriggerFlash();
                _buyMultButton.OnClick?.Invoke();
            }
            // Check Nav Buttons
            foreach (var btn in _navButtons) {
                if (btn.HitTest(p)) {
                    btn.TriggerFlash();
                    btn.OnClick?.Invoke();
                }
            }

            // 3. List Buttons
            bool hitList = false;
            if (_listBounds.Contains(p)) {
                float relativeY = (p.Y - _listStartY) + _scrollY;
                int index = (int)(relativeY / (_itemHeight + _itemPadding));
                if (index >= 0 && index < _genButtons.Count) {
                    var btn = _genButtons[index];
                    if (btn.HitTest(new Point(p.X, p.Y + (int)_scrollY))) {
                        btn.TriggerFlash();
                        btn.OnClick?.Invoke();
                        hitList = true;
                    }
                }
            }

            // 4. Gameplay Tap (If nothing else hit)
            if (!hitList) {
                _sim.Tap();
                SpawnFloatingText(new Vector2(p.X, p.Y - 50), $"+{NumberFormat.Compact(_state.TapValue)}", Color.Lime);
            }
        }

        private void HandleScroll(float deltaY) {
            _scrollY -= deltaY;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > _maxScroll) _scrollY = _maxScroll;
        }

    }
}