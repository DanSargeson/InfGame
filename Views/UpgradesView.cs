using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class UpgradesView : IUIView
    {
        private List<UiButton> _buttons = new();
        private GameState _state;
        private GameSimulator _sim;
        private ViewMode _viewMode;
        private Action _rebuildLayout;

        private float _scrollY = 0;
        private float _maxScroll = 0;
        private Rectangle _bounds;

        public UpgradesView(GameState state, GameSimulator sim, ViewMode viewMode, Action rebuildLayout) {
            _state = state;
            _sim = sim;
            _viewMode = viewMode;
            _rebuildLayout = rebuildLayout;
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
            _bounds = bounds;
            _buttons.Clear();
            _scrollY = 0;

            int currentY = bounds.Top;
            int pad = 20;
            int btnHeight = 100;

            var targetCurrency = (_viewMode == ViewMode.RebirthShop) ? CurrencyType.RebirthPoints : CurrencyType.Souls;
            bool showAutoBuyers = (_viewMode == ViewMode.AutoBuyers);
            bool showUpgrades = (_viewMode == ViewMode.Upgrades);

            // A. Single Upgrades & Auto-Buyers
            foreach (var def in GameData.Upgrades) {
                if (_viewMode == ViewMode.RebirthShop && def.CostCurrency != CurrencyType.RebirthPoints) continue;
                if ((showUpgrades || showAutoBuyers) && def.CostCurrency != CurrencyType.Souls) continue;

                bool isAutoBuyer = (def.Type == UpgradeType.AutoBuyGenerator);
                if (showAutoBuyers && !isAutoBuyer) continue;
                if (showUpgrades && isAutoBuyer) continue;

                if (_state.HasUpgrade(def.Id)) {
                    if (isAutoBuyer) {
                        var btn = GetPooledButton(buttonPool, new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), "", () => {
                            _state.ToggleAutoBuyer(def.Id);
                            _rebuildLayout?.Invoke(); // Redraw to update ON/OFF text
                        });
                        btn.Tag = def;
                        _buttons.Add(btn);
                        currentY += btnHeight + pad;
                    }
                    continue; // Skip normal upgrades that are already owned
                }

                string id = def.Id;
                var buyBtn = GetPooledButton(buttonPool, new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), "", () => {
                    if (_sim.TryBuyUpgrade(id)) _rebuildLayout?.Invoke(); // Redraw so it vanishes
                });
                buyBtn.Tag = def;
                _buttons.Add(buyBtn);
                currentY += btnHeight + pad;
            }

            // B. Infinite Series Upgrades
            if (!showAutoBuyers) {
                foreach (var series in GameData.UpgradeSeries) {
                    if (series.CostCurrency != targetCurrency) continue;

                    string id = series.Id;
                    var btn = GetPooledButton(buttonPool, new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), "", () => {
                        if (_sim.TryBuyProceduralUpgrade(id)) _rebuildLayout?.Invoke();
                    });
                    btn.Tag = series;
                    _buttons.Add(btn);
                    currentY += btnHeight + pad;
                }
            }

            _maxScroll = Math.Max(0, currentY - bounds.Bottom + pad);
        }

        public void Update(double dt) {
            foreach (var btn in _buttons) btn.Update(dt, _state);
        }

        public void UpdateData(double dt) {
            foreach (var btn in _buttons) {
                if (btn.Tag is UpgradeDef upgDef) {
                    if (upgDef.Type == UpgradeType.AutoBuyGenerator && _state.HasUpgrade(upgDef.Id)) {
                        string status = _state.IsAutoBuyerActive(upgDef.Id) ? "ON" : "OFF";
                        btn.Text = $"{upgDef.Name} (Auto)\nStatus: {status}";
                        btn.IsActive = true;
                        continue;
                    }

                    string priceLabel = upgDef.CostCurrency == CurrencyType.Souls
                        ? NumberFormat.Compact(upgDef.Cost)
                        : $"{upgDef.Cost.ToDouble()} RP";

                    btn.Text = $"{upgDef.Name}\n{priceLabel}";
                    btn.IsActive = (upgDef.CostCurrency == CurrencyType.Souls) ? _state.Souls >= upgDef.Cost : _state.RebirthPoints >= upgDef.Cost;
                }
                else if (btn.Tag is UpgradeSeriesDef series) {
                    int nextLevel = _sim.GetProceduralLevel(series.Id) + 1;
                    var cost = _sim.Economy.GetProceduralCost(series.Id);

                    string name = string.Format(series.NameFormat, nextLevel);
                    string desc = $"(x{series.MultiplierPerLevel} effect)";
                    string price = series.CostCurrency == CurrencyType.Souls ? NumberFormat.Compact(cost) : $"{cost} RP";

                    btn.Text = $"{name} - {desc}\n{price}";
                    btn.IsActive = (series.CostCurrency == CurrencyType.Souls) ? _state.Souls >= cost : _state.RebirthPoints >= cost;
                }
            }
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel) {
            foreach (var btn in _buttons) {
                if (btn.Bounds.Bottom - _scrollY < _bounds.Top || btn.Bounds.Top - _scrollY > _bounds.Bottom) continue;
                btn.Draw(sb, font, pixel, (int)_scrollY);
            }
        }

        public bool HandleTap(Point p) {
            if (!_bounds.Contains(p)) return false;
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

        public void HandleScroll(float deltaY) {
            _scrollY -= deltaY;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > _maxScroll) _scrollY = _maxScroll;
        }

        public void Cleanup(Stack<UiButton> buttonPool) {
            foreach (var btn in _buttons) if (buttonPool != null) buttonPool.Push(btn);
            _buttons.Clear();
        }
    }
}