using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace InfGame
{
    internal class UIManager
    {
        private List<UIElement> _rootElements = new();
        private Rectangle _listBounds;

        private GameState _state;
        private GameSimulator _sim;
        private GraphicsDevice _graphicsDevice;


        private bool _showWelcomeModal = false;
        private BigDouble _offlineEarnings = BigDouble.Zero;
        private string _offlineTimeText = "";

        private ViewMode _viewMode = ViewMode.Generators;
        private IUIView _activeView; // Replaces _mainScrollList temporarily until you build all views
        private GeneratorsView _generatorsView;

        private RasterizerState _scissorState = new RasterizerState { ScissorTestEnable = true };

        // A dedicated list for particles so they draw over everything else
        private List<FloatingText> _particles = new();


        // 1. The new bridge method for Game1.cs
        public void ShowOfflineReport(BigDouble earnings, TimeSpan timeOffline) {
            _offlineEarnings = earnings;
            _offlineTimeText = $"Offline for: {(int)timeOffline.TotalHours}h {timeOffline.Minutes}m";
            _showWelcomeModal = true;
        }

        // 2. Restore the Floating Text spawner for your AutoBuyers and Taps
        public void SpawnFloatingText(Vector2 pos, string text, Color color) {
            _particles.Add(new FloatingText(pos, text, color));
        }


        public UIManager(GameState state, GameSimulator sim, InputManager input, GraphicsDevice graphicsDevice) {
            _state = state;
            _sim = sim;
            _graphicsDevice = graphicsDevice;

            input.OnTap += HandleTap;
            input.OnVerticalScroll += HandleScroll;

            InitializeLayout();
        }

        private void InitializeLayout() {
            _rootElements.Clear();

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int pad = 20;

            // --- 1. REBIRTH BUTTON ---
            var prestigeBtn = new DynamicButton(
                new Rectangle(pad, 80, w - pad * 2, 80),
                textFunc: () => {
                    var gain = _sim.CalculateRebirthGain();
                    return gain > 0
                        ? $"REBIRTH: +{NumberFormat.Compact(gain)} PTS\n(+{gain.ToDouble() * 10}% Bonus)"
                        : "Rebirth Locked (1M Souls)";
                },
                activeFunc: () => _sim.CalculateRebirthGain() > 0,
                onClick: () => _sim.DoRebirth()
            );
            _rootElements.Add(prestigeBtn);

            // --- 2. STATUS BOARD ---
            // Places the text block right below the Rebirth button
            _rootElements.Add(new UIStatusBoard(new Rectangle(50, 180, w, 250), _state));

            // --- 3. MULTI-BUY BUTTON ---
            var buyMultBtn = new DynamicButton(
                new Rectangle(pad + w / 2 + pad, 250, w / 2 - pad * 2, 60),
                textFunc: () => _state.BuyAmount == -1 ? "BUY: Max" : $"BUY: {_state.BuyAmount}x",
                activeFunc: () => true,
                onClick: () => {
                    if (_state.BuyAmount == 1) _state.BuyAmount = 10;
                    else if (_state.BuyAmount == 10) _state.BuyAmount = 100;
                    else if (_state.BuyAmount == 100) _state.BuyAmount = -1;
                    else _state.BuyAmount = 1;
                }
            );
            _rootElements.Add(buyMultBtn);

            // --- 4. NAVIGATION BAR ---
            int navY = h / 2;
            int navHeight = 80;
            string[] navNames = { "GEN", "UPG", "AUTO", "SHOP", "SET" };
            ViewMode[] navModes = { ViewMode.Generators, ViewMode.Upgrades, ViewMode.AutoBuyers, ViewMode.RebirthShop, ViewMode.Settings };
            int navWidth = (w - (pad * 6)) / 5;

            for (int i = 0; i < 5; i++) {
                var mode = navModes[i];
                string label = navNames[i];

                var navBtn = new DynamicButton(
                    new Rectangle(pad + (i * (navWidth + pad)), navY, navWidth, navHeight),
                    textFunc: () => _viewMode == mode ? $"[{label}]" : label,
                    activeFunc: () => true,
                    onClick: () => SwitchView(mode) // Wires the tab switching
                );
                _rootElements.Add(navBtn);
            }

            // --- 5. INITIALIZE VIEWS ---
            int listStartY = navY + navHeight + pad;
            _listBounds = new Rectangle(0, listStartY, w, h - listStartY); // Now saved to class

            _generatorsView = new GeneratorsView(_state, _sim);
            _generatorsView.Layout(_listBounds, null); // Pass the class variable

            SwitchView(ViewMode.Generators);
        }

        private void SwitchView(ViewMode mode) {
            _viewMode = mode;

            // In the future, add switch cases here for UpgradesView, SettingsView, etc.
            if (mode == ViewMode.Generators) {
                _activeView = _generatorsView;
            }
        }

        public void Update(double dt) {
            foreach (var element in _rootElements) {
                element.Update(dt, _state);
            }

            _activeView?.UpdateData(dt);
            _activeView?.Update(dt);

            // Update Particles
            for (int i = _particles.Count - 1; i >= 0; i--) {
                _particles[i].Update(dt);
                if (!_particles[i].IsActive) {
                    _particles.RemoveAt(i);
                }
            }
        }

        public void Draw(Texture2D pixel, SpriteBatch sb, SpriteFont font) {
            // 1. Draw static UI elements (Header, Nav buttons, etc.)
            sb.Begin();
            foreach (var element in _rootElements) {
                element.Draw(sb, font, pixel);
            }
            // Draw particles
            foreach (var p in _particles) p.Draw(sb, font);
            sb.End();

            // 2. Draw the Active List (Generators, Upgrades, etc.)
            if (_activeView != null) {
                sb.Begin(rasterizerState: _scissorState);

                // THE FIX: Define the exact pixel area where drawing is allowed
                _graphicsDevice.ScissorRectangle = _listBounds;

                _activeView.Draw(sb, font, pixel);
                sb.End();
            }

            // 3. Draw Modal over everything else
            if (_showWelcomeModal) {
                sb.Begin();

                var w = _graphicsDevice.Viewport.Width;
                var h = _graphicsDevice.Viewport.Height;

                sb.Draw(pixel, new Rectangle(0, 0, w, h), Color.Black * 0.65f);
                var boxRect = new Rectangle(w / 2 - 200, h / 2 - 150, 400, 350);
                sb.Draw(pixel, boxRect, Color.DarkSlateGray);

                sb.DrawString(font, "WELCOME BACK!", new Vector2(w / 2 - 100, h / 2 - 80), Color.Gold);
                sb.DrawString(font, _offlineTimeText, new Vector2(w / 2 - 100, h / 2 - 40), Color.White);
                sb.DrawString(font, $"+{NumberFormat.Compact(_offlineEarnings)}", new Vector2(w / 2 - 50, h / 2), Color.LimeGreen);
                sb.DrawString(font, "[Tap to Collect]", new Vector2(w / 2 - 80, h / 2 + 80), Color.Gray);

                sb.End();
            }
        }

        private void HandleTap(Point p) {
            if (_showWelcomeModal) { _showWelcomeModal = false; return; }

            foreach (var element in _rootElements) {
                if (element.HandleTap(p)) return;
            }

            // Pass tap down to the active view list!
            if (_activeView != null && _activeView.HandleTap(p)) return;

            _sim.Tap();
            SpawnFloatingText(new Vector2(p.X, p.Y - 50), $"+{NumberFormat.Compact(_state.TapValue)}", Color.Lime);
        }

        private void HandleScroll(float deltaY) {
            
            _activeView?.HandleScroll(deltaY);
        }
    }
}