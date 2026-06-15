using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class GeneratorsView : IUIView
    {
        private List<UiButton> _buttons = new();
        private GameState _state;
        private GameSimulator _sim;

        private int _lastBuyAmount = -999;
        private int _lastCount = -1;

        // --- NEW: Internal Scroll State ---
        private float _scrollY = 0;
        private float _maxScroll = 0;
        private Rectangle _bounds;

        public GeneratorsView(GameState state, GameSimulator sim) {
            _state = state;
            _sim = sim;
        }

        private UiButton GetPooledButton(Stack<UiButton> pool, Rectangle bounds, string text, Action onClick) {
            if (pool != null && pool.Count > 0) {
                var btn = pool.Pop();
                btn.Configure(bounds, text, onClick);
                return btn;
            }
            return new UiButton(bounds, text, onClick);
        }

        public void Layout(Rectangle bounds, Stack<UiButton> buttonPool) {
            _bounds = bounds; // Save the bounds for tap detection later
            _buttons.Clear();
            _scrollY = 0; // Reset scroll when laid out

            int currentY = bounds.Top;
            int pad = 20;
            int btnHeight = 100;

            foreach (var def in GameData.Generators) {
                var btn = GetPooledButton(buttonPool, new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), def.Name, () => _sim.TryBuyGenerator(def.Id));
                btn.Tag = def;
                _buttons.Add(btn);
                currentY += btnHeight + pad;
            }

            // Calculate max scroll just like the old UIManager did
            _maxScroll = Math.Max(0, currentY - bounds.Bottom + pad);
        }

        public void Update(double dt) {
            foreach (var btn in _buttons) btn.Update(dt, _state);
        }

        public void UpdateData(double dt) {
            foreach (var btn in _buttons) {
                // We stored the definition in the Tag during Layout()
                if (btn.Tag is GeneratorDef genDef) {

                    // 1. Determine how many we are trying to buy
                    int amount = _state.BuyAmount;
                    string prefix = string.Empty;

                    // Handle "Max" mode specifically
                    if (amount == -1) {
                        amount = _sim.Economy.GetMaxBuyable(genDef.Id);
                        if (amount == 0) {
                            amount = 1;
                            prefix = "Max";
                        }
                        else {
                            prefix = $"x{amount}";
                        }
                    }

                    // 2. Fetch the live numbers from Simulator and State
                    var totalCost = _sim.Economy.GetBulkCost(genDef.Id, amount);
                    var currentCount = _state.GetCount(genDef.Id);

                    if (_lastBuyAmount != amount || _lastCount != currentCount) {
                        prefix = (amount == -1) ? "Max" : $"x{amount}";
                        btn.Text = $"{genDef.Name} ({currentCount})\n{prefix}: {NumberFormat.Compact(totalCost)}";

                        _lastBuyAmount = amount;
                        _lastCount = currentCount;
                    }

                    // Turn red if we can't afford it
                    btn.IsActive = _state.Souls >= totalCost;
                }
            }
        }

        // --- UPDATED: Apply _scrollY internally ---
        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel) {
            foreach (var btn in _buttons) {
                // Optional: You can add clipping math here so off-screen buttons don't draw
                if (btn.Bounds.Bottom - _scrollY < _bounds.Top) continue;
                if (btn.Bounds.Top - _scrollY > _bounds.Bottom) continue;

                btn.Draw(sb, font, pixel, (int)_scrollY);
            }
        }

        // --- UPDATED: Account for _scrollY during taps ---
        public bool HandleTap(Point p) {
            // If they tapped outside the list area, ignore it
            if (!_bounds.Contains(p)) return false;

            // Offset the physical tap by the scroll position to find the logical button
            Point scrolledPoint = new Point(p.X, p.Y + (int)_scrollY);

            foreach (var btn in _buttons) {
                if (btn.HitTest(scrolledPoint)) {
                    btn.TriggerFlash();
                    btn.OnClick?.Invoke();
                    return true;
                }
            }
            return false;
        }

        // --- NEW: Receive scroll events ---
        public void HandleScroll(float deltaY) {
            _scrollY -= deltaY;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > _maxScroll) _scrollY = _maxScroll;
        }

        public void Cleanup(Stack<UiButton> buttonPool) {
            foreach (var btn in _buttons) {
                if (buttonPool != null) buttonPool.Push(btn);
            }
            _buttons.Clear();
        }
    }
}