using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfGame
{
    public class UIVerticalList : UIElement
    {
        private List<UIElement> _children = new List<UIElement>();
        private float _scrollY = 0;
        private float _maxScroll = 0;
        private int _padding;

        public UIVerticalList(Rectangle bounds, int padding) {
            Bounds = bounds;
            _padding = padding;
        }

        public void AddChild(UIElement child) {
            _children.Add(child);
            RecalculateLayout();
        }

        public void Clear() {
            _children.Clear();
            _scrollY = 0;
        }

        private void RecalculateLayout() {
            int currentY = Bounds.Y;
            foreach (var child in _children) {
                child.Bounds.X = Bounds.X + _padding;
                child.Bounds.Y = currentY;
                currentY += child.Bounds.Height + _padding;
            }
            _maxScroll = System.Math.Max(0, currentY - Bounds.Bottom);
        }

        public void HandleScroll(float deltaY) {
            _scrollY -= deltaY;
            if (_scrollY < 0) _scrollY = 0;
            if (_scrollY > _maxScroll) _scrollY = _maxScroll;
        }

        public override bool HandleTap(Point p) {
            // Adjust point for scroll before checking children
            Point scrolledPoint = new Point(p.X, p.Y + (int)_scrollY);
            if (Bounds.Contains(p)) {
                foreach (var child in _children) {
                    if (child.HandleTap(scrolledPoint)) return true;
                }
            }
            return false;
        }

        public override void Update(double dt, GameState state) {
            foreach (var child in _children) child.Update(dt, state);
        }

        public override void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int scrollOffset = 0) {
            // NOTE: Do your ScissorRect / GraphicsDevice clipping logic here before drawing children!
            foreach (var child in _children) {
                // Only draw if within visual bounds
                if (child.Bounds.Bottom - _scrollY >= Bounds.Top &&
                    child.Bounds.Top - _scrollY <= Bounds.Bottom) {
                    child.Draw(sb, font, pixel, (int)_scrollY);
                }
            }
        }
    }
}