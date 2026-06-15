using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class SettingsView : IUIView
    {
        private List<UiButton> _buttons = new();
        private GameState _state;
        private Rectangle _bounds;
        private float _scrollY = 0;

        public SettingsView(GameState state) {
            _state = state;
        }

        public void Layout(Rectangle bounds, Stack<UiButton> buttonPool) {
            _bounds = bounds;
            _buttons.Clear();

            int currentY = bounds.Top;
            int pad = 20;
            int btnHeight = 100;

            _buttons.Add(new UiButton(new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), "HARD RESET (Wipe Save)", () => {
                System.Diagnostics.Debug.WriteLine("Hard Reset Clicked");
            }));

            currentY += btnHeight + pad;

            _buttons.Add(new UiButton(new Rectangle(pad, currentY, bounds.Width - pad * 2, btnHeight), "EXPORT SAVE (Log to Debug)", () => {
                System.Diagnostics.Debug.WriteLine("Export Save Clicked");
            }));
        }

        public void Update(double dt) { foreach (var btn in _buttons) btn.Update(dt, _state); }
        public void UpdateData(double dt) { } // Static text, no data updates needed
        public void HandleScroll(float deltaY) { } // Doesn't need to scroll

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel) {
            foreach (var btn in _buttons) btn.Draw(sb, font, pixel);
        }

        public bool HandleTap(Point p) {
            if (!_bounds.Contains(p)) return false;
            foreach (var btn in _buttons) {
                if (btn.HitTest(p)) {
                    btn.TriggerFlash();
                    btn.OnClick?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public void Cleanup(Stack<UiButton> buttonPool) {
            foreach (var btn in _buttons) if (buttonPool != null) buttonPool.Push(btn);
            _buttons.Clear();
        }
    }
}