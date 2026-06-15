using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace InfGame
{
    public class UIStackPanel : UIElement
    {
        private List<UIElement> _children = new();
        private bool _isHorizontal;
        private int _padding;

        public UIStackPanel(Rectangle bounds, bool isHorizontal, int padding) {
            Bounds = bounds;
            _isHorizontal = isHorizontal;
            _padding = padding;
        }

        public void AddChild(UIElement child) {
            _children.Add(child);
            RecalculateLayout();
        }

        private void RecalculateLayout() {
            if (_children.Count == 0) return;

            if (_isHorizontal) {
                // Automatically divide the width among children
                int totalPadding = _padding * (_children.Count - 1);
                int itemWidth = (Bounds.Width - totalPadding) / _children.Count;

                for (int i = 0; i < _children.Count; i++) {
                    _children[i].Bounds = new Rectangle(
                        Bounds.X + (i * (itemWidth + _padding)),
                        Bounds.Y,
                        itemWidth,
                        Bounds.Height
                    );
                }
            }
            else {
                // Vertical layout math here...
            }
        }

        public override void Update(double dt, GameState state) {
            foreach (var child in _children) child.Update(dt, state);
        }

        public override void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int offset) {
            foreach (var child in _children) child.Draw(sb, font, pixel);
        }

        public override bool HandleTap(Point p) {
            foreach (var child in _children) {
                if (child.HandleTap(p)) return true; // Consume the tap
            }
            return false;
        }
    }
}